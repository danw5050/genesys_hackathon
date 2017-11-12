// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// 
// Microsoft Cognitive Services: http://www.microsoft.com/cognitive
// 
// Microsoft Cognitive Services Github:
// https://github.com/Microsoft/Cognitive
// 
// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 

using Firebase.Database;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.ProjectOxford.Vision;
using Newtonsoft.Json;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using VideoFrameAnalyzer;

namespace LiveCameraSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private EmotionServiceClient _emotionClient = null;
        private FaceServiceClient _faceClient = null;
        private VisionServiceClient _visionClient = null;
        private readonly FrameGrabber<LiveCameraResult> _grabber = null;
        private static readonly ImageEncodingParam[] s_jpegParams = {
            new ImageEncodingParam(ImwriteFlags.JpegQuality, 60)
        };
        private readonly CascadeClassifier _localFaceDetector = new CascadeClassifier();
        private bool _fuseClientRemoteResults = false;
  
        private LiveCameraResult _latestResultsToDisplay = null;
        private DateTime _startTime;

        private static int secondsBetweenAnalysis = 4;

        Dictionary<string, int> storeEmotions = new Dictionary<string, int>();
        Dictionary<string, int> storeAgentEmotions = new Dictionary<string, int>();

        string saveCustomerEmotion = "";
        string saveCustomerAge = "";
        string saveCustomerGender = "";
        string saveAgentEmotion = "";



        public MainWindow()
        {
            InitializeComponent();

            // Create grabber. 
            _grabber = new FrameGrabber<LiveCameraResult>();
            _grabber.AnalysisFunction = FacesAnalysisFunction;

            // Set up a listener for when the client receives a new frame.
            _grabber.NewFrameProvided += (s, e) =>
            {
                // The callback may occur on a different thread, so we must use the
                // MainWindow.Dispatcher when manipulating the UI. 
                this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    // Display the image in the left pane.
                   // LeftImage.Source = e.Frame.Image.ToBitmapSource();

                    // If we're fusing client-side face detection with remote analysis, show the
                    // new frame now with the most recent analysis available. 
                    if (_fuseClientRemoteResults)
                    {
                        RightImage.Source = VisualizeResult(e.Frame);
                    }
                }));

                // See if auto-stop should be triggered. 
                if (Properties.Settings.Default.AutoStopEnabled && (DateTime.Now - _startTime) > Properties.Settings.Default.AutoStopTime)
                {
                    _grabber.StopProcessingAsync();
                }
            };

            // Set up a listener for when the client receives a new result from an API call. 
            _grabber.NewResultAvailable += (s, e) =>
            {
                this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (e.TimedOut)
                    {
                        MessageArea.Text = "API call timed out.";
                    }
                    else if (e.Exception != null)
                    {
                        string apiName = "";
                        string message = e.Exception.Message;
                        var faceEx = e.Exception as FaceAPIException;
                        var emotionEx = e.Exception as Microsoft.ProjectOxford.Common.ClientException;
                        var visionEx = e.Exception as Microsoft.ProjectOxford.Vision.ClientException;
                        if (faceEx != null)
                        {
                            apiName = "Face";
                            message = faceEx.ErrorMessage;
                        }
                        else if (emotionEx != null)
                        {
                            apiName = "Emotion";
                            message = emotionEx.Error.Message;
                        }
                        else if (visionEx != null)
                        {
                            apiName = "Computer Vision";
                            message = visionEx.Error.Message;
                        }
                        MessageArea.Text = string.Format("{0} API call failed on frame {1}. Exception: {2}", apiName, e.Frame.Metadata.Index, message);
                    }
                    else
                    {
                        _latestResultsToDisplay = e.Analysis;

                        // Display the image and visualization in the right pane. 
                        if (!_fuseClientRemoteResults)
                        {
                            RightImage.Source = VisualizeResult(e.Frame);
                        }
                    }
                }));
            };

            // Create local face detector. 
            _localFaceDetector.Load("Data/haarcascade_frontalface_alt2.xml");
        }

        /// <summary> Function which submits a frame to the Face API. </summary>
        /// <param name="frame"> The video frame to submit. </param>
        /// <returns> A <see cref="Task{LiveCameraResult}"/> representing the asynchronous API call,
        ///     and containing the faces returned by the API. </returns>
        private async Task<LiveCameraResult> FacesAnalysisFunction(VideoFrame frame)
        {
            // Encode image. 
            var jpg = frame.Image.ToMemoryStream(".jpg", s_jpegParams);
            // Submit image to API. 
            var attrs = new List<FaceAttributeType> { FaceAttributeType.Age,
                FaceAttributeType.Gender, FaceAttributeType.HeadPose, FaceAttributeType.Glasses, FaceAttributeType.Emotion };
            var faces = await _faceClient.DetectAsync(jpg, returnFaceAttributes: attrs);
            // Count the API call. 
            Properties.Settings.Default.FaceAPICallCount++;
            // Output. 
            return new LiveCameraResult { Faces = faces };
        }

        /// <summary> Function which submits a frame to the Emotion API. </summary>
        /// <param name="frame"> The video frame to submit. </param>
        /// <returns> A <see cref="Task{LiveCameraResult}"/> representing the asynchronous API call,
        ///     and containing the emotions returned by the API. </returns>
        private async Task<LiveCameraResult> EmotionAnalysisFunction(VideoFrame frame)
        {
            // Encode image. 
            var jpg = frame.Image.ToMemoryStream(".jpg", s_jpegParams);
            // Submit image to API. 
            Emotion[] emotions = null;

            // See if we have local face detections for this image.
            var localFaces = (OpenCvSharp.Rect[])frame.UserData;
            if (localFaces == null)
            {
                // If localFaces is null, we're not performing local face detection.
                // Use Cognigitve Services to do the face detection.
                Properties.Settings.Default.EmotionAPICallCount++;
                emotions = await _emotionClient.RecognizeAsync(jpg);
            }
            else if (localFaces.Count() > 0)
            {
                // If we have local face detections, we can call the API with them. 
                // First, convert the OpenCvSharp rectangles. 
                var rects = localFaces.Select(
                    f => new Microsoft.ProjectOxford.Common.Rectangle
                    {
                        Left = f.Left,
                        Top = f.Top,
                        Width = f.Width,
                        Height = f.Height
                    });
                Properties.Settings.Default.EmotionAPICallCount++;
                emotions = await _emotionClient.RecognizeAsync(jpg, rects.ToArray());
            }
            else
            {
                // Local face detection found no faces; don't call Cognitive Services.
                emotions = new Emotion[0];
            }

            // Output. 
            return new LiveCameraResult
            {
                Faces = emotions.Select(e => CreateFace(e.FaceRectangle)).ToArray(),
                // Extract emotion scores from results. 
                EmotionScores = emotions.Select(e => e.Scores).ToArray()
            };
        }

       

        /// <summary> Function which submits a frame to the Computer Vision API for celebrity
        ///     detection. </summary>
        /// <param name="frame"> The video frame to submit. </param>
        /// <returns> A <see cref="Task{LiveCameraResult}"/> representing the asynchronous API call,
        ///     and containing the celebrities returned by the API. </returns>
       

        private BitmapSource VisualizeResult(VideoFrame frame)
        {
            // Draw any results on top of the image. 
            BitmapSource visImage = frame.Image.ToBitmapSource();

            var result = _latestResultsToDisplay;

            if (result != null)
            {
                // See if we have local face detections for this image.
                var clientFaces = (OpenCvSharp.Rect[])frame.UserData;
                if (clientFaces != null && result.Faces != null)
                {
                    // If so, then the analysis results might be from an older frame. We need to match
                    // the client-side face detections (computed on this frame) with the analysis
                    // results (computed on the older frame) that we want to display. 
                    MatchAndReplaceFaceRectangles(result.Faces, clientFaces);
                }

                var returnVal = Visualization.DrawFaces(visImage, result.Faces, result.EmotionScores, result.CelebrityNames, this);
                visImage = returnVal.Item1;

                // if (returnVal.Item2 != "" || returnVal.Item3 != "")
                // {
                //returnString, 
                //returnCode,
                //customerAge,
                //customerGender,
                //customerEmotion

                    RESTPost(returnVal.Item2, returnVal.Item3,returnVal.Item4, returnVal.Item5, returnVal.Item6, returnVal.Item7);
               // }

            }

            return visImage;
        }


        private void RESTPost(string message, string code, string age, string gender, string emotion,
            string agentEmotion)
        {
            //find most common emotion for the customer
            if (emotion != "")
            {
                if (storeEmotions.ContainsKey(emotion))
                {
                    storeEmotions[emotion] += 1;
                }
                else
                {
                    storeEmotions.Add(emotion, 1);
                }
            }

            string mostCommonEmotion = "";
            int mostCommonEmotionCount = -1;

            foreach (KeyValuePair<string, int> entry in storeEmotions)
            {
                if (mostCommonEmotionCount < entry.Value)
                {
                    mostCommonEmotionCount = entry.Value;
                    mostCommonEmotion = entry.Key;
                }
            }




            //find most common emotion for agent
            if (agentEmotion != "")
            {
                if (storeAgentEmotions.ContainsKey(agentEmotion))
                {
                    storeAgentEmotions[agentEmotion] += 1;
                }
                else
                {
                    storeAgentEmotions.Add(agentEmotion, 1);
                }
            }

            string mostCommonAgentEmotion = "";
            int mostCommonAgentEmotionCount = -1;

            foreach (KeyValuePair<string, int> entry in storeAgentEmotions)
            {
                if (mostCommonAgentEmotionCount < entry.Value)
                {
                    mostCommonAgentEmotionCount = entry.Value;
                    mostCommonAgentEmotion = entry.Key;
                }
            }

            //update the values that will be saved
            saveCustomerEmotion = mostCommonEmotion;
            saveCustomerAge = age;
            saveCustomerGender = gender;
            saveAgentEmotion = mostCommonAgentEmotion;

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                Message = message,
                Angry = code == "Anger",
                Age = age,
                Gender = gender,
                emotion = emotion,
                Common_Emotion = mostCommonEmotion,
                Agent_Emotion = agentEmotion,
                Agent_Common_Emotion = mostCommonAgentEmotion,
            });

            var request = WebRequest.CreateHttp("https://genesyshackathon.firebaseio.com/data.json");
            request.Method = "PATCH";
            request.ContentType = "application/json";
            var buffer = Encoding.UTF8.GetBytes(json);
            request.ContentLength = buffer.Length;
            request.GetRequestStream().Write(buffer, 0, buffer.Length);
            var response = request.GetResponse();
            json = (new StreamReader(response.GetResponseStream())).ReadToEnd();
        }


        public void saveConversation() 
        {

            int starsCount = 0;
            switch (saveCustomerEmotion) {
                case ("Anger"):
                    starsCount = 1;
                    break;
                case ("Sadness"):
                    starsCount = 2;
                    break;
                case ("Neutral"):
                    starsCount = 4;
                    break;
                case ("Happiness"):
                    starsCount = 5;
                    break;
            }

            string starString = "";
            for (int i = 0; i < starsCount; i++)
            {
                starString += "★";
            }


            var json = Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                Agent_Name = "Daniel Weisberg",
                Agent_Emotion = saveAgentEmotion,
                Customer_Age = saveCustomerAge,
                Customer_Gender = saveCustomerGender,
                Customer_Emotion = saveCustomerEmotion,
                Stars = starString,
                Date = "Nov. 12 2017"

            });

            var request = WebRequest.CreateHttp("https://genesyshackathon.firebaseio.com/History/"+ Guid.NewGuid() +".json");
            request.Method = "PATCH";
            request.ContentType = "application/json";
            var buffer = Encoding.UTF8.GetBytes(json);
            request.ContentLength = buffer.Length;
            request.GetRequestStream().Write(buffer, 0, buffer.Length);
            var response = request.GetResponse();
            json = (new StreamReader(response.GetResponseStream())).ReadToEnd();

            updateStatusText("Performance Save. Customer Emotion: " + saveCustomerEmotion);
        }



        /// <summary> Populate CameraList in the UI, once it is loaded. </summary>
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Routed event information. </param>
        //private void CameraList_Loaded(object sender, RoutedEventArgs e)
        //{
        //    int numCameras = _grabber.GetNumCameras();

        //    if (numCameras == 0)
        //    {
        //        MessageArea.Text = "No cameras found!";
        //    }

        //    var comboBox = sender as ComboBox;
        //    comboBox.ItemsSource = Enumerable.Range(0, numCameras).Select(i => string.Format("Camera {0}", i + 1));
        //    comboBox.SelectedIndex = 0;
        //}

        /// <summary> Populate ModeList in the UI, once it is loaded. </summary>
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Routed event information. </param>

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {

            ProgramStatusMessage.Text = "Currently Running";
            // if (!CameraList.HasItems)
            //   {
            //        MessageArea.Text = "No cameras found; cannot start processing";
            //        return;
            //   }

            _faceClient = new FaceServiceClient("ff1f62c7ab4c4295838ec290e9a85300", "https://westcentralus.api.cognitive.microsoft.com/face/v1.0");
            _grabber.TriggerAnalysisOnInterval(new TimeSpan( secondsBetweenAnalysis * TimeSpan.TicksPerSecond));
            MessageArea.Text = "Starting Genesys Hackathon Project - No errors yet!";
            _startTime = DateTime.Now;

            await _grabber.StartProcessingCameraAsync(); // CameraList.SelectedIndex);
        }

        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {
            ProgramStatusMessage.Text = "Stopped Running";
            RightImage.Source = null;
            saveConversation();
            await _grabber.StopProcessingAsync();

            storeAgentEmotions.Clear();
            storeEmotions.Clear();
        }

        public int angerCount = 0;
        public int agentAngerCount = 0;

        public void updateStatusText(String text)
        {
            ProgramStatusMessage.Text = text;
        }

        private Face CreateFace(FaceRectangle rect)
        {
            return new Face
            {
                FaceRectangle = new FaceRectangle
                {
                    Left = rect.Left,
                    Top = rect.Top,
                    Width = rect.Width,
                    Height = rect.Height
                }
            };
        }

        private Face CreateFace(Microsoft.ProjectOxford.Vision.Contract.FaceRectangle rect)
        {
            return new Face
            {
                FaceRectangle = new FaceRectangle
                {
                    Left = rect.Left,
                    Top = rect.Top,
                    Width = rect.Width,
                    Height = rect.Height
                }
            };
        }

        private Face CreateFace(Microsoft.ProjectOxford.Common.Rectangle rect)
        {
            return new Face
            {
                FaceRectangle = new FaceRectangle
                {
                    Left = rect.Left,
                    Top = rect.Top,
                    Width = rect.Width,
                    Height = rect.Height
                }
            };
        }

        private void MatchAndReplaceFaceRectangles(Face[] faces, OpenCvSharp.Rect[] clientRects)
        {
            // Use a simple heuristic for matching the client-side faces to the faces in the
            // results. Just sort both lists left-to-right, and assume a 1:1 correspondence. 

            // Sort the faces left-to-right. 
            var sortedResultFaces = faces
                .OrderBy(f => f.FaceRectangle.Left + 0.5 * f.FaceRectangle.Width)
                .ToArray();

            // Sort the clientRects left-to-right.
            var sortedClientRects = clientRects
                .OrderBy(r => r.Left + 0.5 * r.Width)
                .ToArray();

            // Assume that the sorted lists now corrrespond directly. We can simply update the
            // FaceRectangles in sortedResultFaces, because they refer to the same underlying
            // objects as the input "faces" array. 
            for (int i = 0; i < Math.Min(faces.Length, clientRects.Length); i++)
            {
                // convert from OpenCvSharp rectangles
                OpenCvSharp.Rect r = sortedClientRects[i];
                sortedResultFaces[i].FaceRectangle = new FaceRectangle { Left = r.Left, Top = r.Top, Width = r.Width, Height = r.Height };
            }
        }
    }
}
