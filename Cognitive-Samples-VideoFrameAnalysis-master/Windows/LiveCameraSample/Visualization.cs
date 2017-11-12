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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.ProjectOxford.Vision.Contract;

namespace LiveCameraSample
{
    public class Visualization
    {
        private static SolidColorBrush agentColour = new SolidColorBrush(new System.Windows.Media.Color { R = 0, G = 244, B = 0, A = 255 });
        private static SolidColorBrush customerColour = new SolidColorBrush(new System.Windows.Media.Color { R = 255, G = 255, B = 0, A = 255 });
        private static Typeface s_typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

        private static BitmapSource DrawOverlay(BitmapSource baseImage, Action<DrawingContext, double> drawAction)
        {
            double annotationScale = baseImage.PixelHeight / 370;

            DrawingVisual visual = new DrawingVisual();
            DrawingContext drawingContext = visual.RenderOpen();

            drawingContext.DrawImage(baseImage, new Rect(0, 0, baseImage.Width, baseImage.Height));

            drawAction(drawingContext, annotationScale);

            drawingContext.Close();

            RenderTargetBitmap outputBitmap = new RenderTargetBitmap(
                baseImage.PixelWidth, baseImage.PixelHeight,
                baseImage.DpiX, baseImage.DpiY, PixelFormats.Pbgra32);

            outputBitmap.Render(visual);

            return outputBitmap;
        }

        public static Tuple<BitmapSource, String, String, String, String, String, string> DrawFaces(BitmapSource baseImage, Microsoft.ProjectOxford.Face.Contract.Face[] faces, EmotionScores[] emotionScores, string[] celebName, MainWindow mainWindowInstance)
        {
            string agentEmotion = "";
            string customerAge = "";
            string customerGender = "";
            string customerEmotion = "";
            String returnString = "";
            String returnCode = "";
            
            if (faces == null)
            {
                return Tuple.Create(baseImage, "", "", "", "", "", "");
            }

            Action<DrawingContext, double> drawAction = (drawingContext, annotationScale) =>
            {

               

                int face1Index = -1;
                int face1Area = -1;
                int face1x = -1;
                int face2Index = -1;
                int face2Area = -1;
                int face2x = -1;
                for (int i = 0; i < faces.Length; i++)
                {
                    var face = faces[i];
                    int faceArea = face.FaceRectangle.Width * face.FaceRectangle.Height;
                    if (face2Area > face1Area) //face1 is smaller, so edit that
                    {
                        if (face1Area < faceArea) //if the face1area smaller then face area
                        {
                            face1Area = faceArea;
                            face1Index = i;
                            face1x = face.FaceRectangle.Left;
                        }
                    } else //face 2 is smaller, so edit that
                    {
                        if (face2Area < faceArea) //if the face2area smaller then face area
                        {
                            face2Area = faceArea;
                            face2Index = i;
                            face2x = face.FaceRectangle.Left;
                        }
                    }

                }

                int facesFound = ((face1Index != -1) ? 1 : 0) + ((face2Index != -1) ? 1 : 0);
                for (int i = 0; i < facesFound; i++)
                {
                    Boolean isAgent = false;
                    int currentIndex = 0;
                    if (i == 0 && face1Index != -1)
                    {
                        currentIndex = face1Index;
                        isAgent = face1x < face2x; //agent is to the left of user
                    } else
                    {
                        currentIndex = face2Index;
                        isAgent = face2x < face1x; //agent is to the left of user
                    }

                    var face = faces[currentIndex];
                    if (face.FaceRectangle == null) { continue; }

                    Rect faceRect = new Rect(
                        face.FaceRectangle.Left, face.FaceRectangle.Top,
                        face.FaceRectangle.Width, face.FaceRectangle.Height);
                    string text = "";

                    text = isAgent ? "Agent (You):\n" : "Customer:\n";
                    string emotionString = face.FaceAttributes.Emotion.ToRankedList().ElementAt(0).Key.ToString().Trim();
                    if (isAgent)
                    {
                        agentEmotion = emotionString;
                        if (emotionString != "Neutral" && emotionString != "Happiness")
                        {
                            mainWindowInstance.updateStatusText("");
                            text += "Be Happy or \n";
                            text += "Neutral. Big Brother \n";
                            text += "is Watching.";
                        }
                    } else //customer
                    {
                        customerAge = face.FaceAttributes.Age.ToString();
                        customerGender = face.FaceAttributes.Gender.ToString();
                        customerEmotion = emotionString;
                        text += "Age: " + face.FaceAttributes.Age.ToString() + "\n";
                        text += "Gender: " + face.FaceAttributes.Gender.ToString() + "\n";
                        text += "Emotion: " + emotionString +  "\n";

                        if (emotionString == "Anger" || emotionString == "Disgust" || emotionString == "Contempt")
                        {
                            mainWindowInstance.angerCount++;
                            if (mainWindowInstance.angerCount == 2)
                            {
                                mainWindowInstance.angerCount = 0;
                                returnCode = "Anger";
                                returnString = "Angry for a while";
                                mainWindowInstance.updateStatusText("The customer is still angry - your superior has been notified");
                            }
                        } else
                        {
                            mainWindowInstance.angerCount = 0;
                            mainWindowInstance.updateStatusText("Currently Running");
                        }
                        
                    }

                    faceRect.Inflate(6 * annotationScale, 6 * annotationScale);

                    double lineThickness = 4 * annotationScale;

                    drawingContext.DrawRectangle(
                        Brushes.Transparent,
                        new Pen(isAgent ? agentColour : customerColour, lineThickness),
                        faceRect);

                    if (text != "")
                    {
                        FormattedText ft = new FormattedText(text,
                            CultureInfo.CurrentCulture, FlowDirection.LeftToRight, s_typeface,
                            16 * annotationScale, Brushes.Black);

                        var pad = 3 * annotationScale;

                        var ypad = pad;
                        var xpad = pad + 4 * annotationScale;
                        var origin = new System.Windows.Point(
                            faceRect.Left + xpad - lineThickness / 2,
                            faceRect.Top + (isAgent ? -ft.Height : faceRect.Height) - ypad + lineThickness / 2);
                        var rect = ft.BuildHighlightGeometry(origin).GetRenderBounds(null);
                        rect.Inflate(xpad, ypad);

                        drawingContext.DrawRectangle(isAgent ? agentColour : customerColour, null, rect);
                        drawingContext.DrawText(ft, origin);
                    }
                }
            };

            return Tuple.Create(DrawOverlay(baseImage, drawAction), 
                returnString, 
                returnCode,
                customerAge,
                customerGender,
                customerEmotion,
                agentEmotion);
        }
    }
}
