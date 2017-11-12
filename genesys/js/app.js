document.addEventListener('DOMContentLoaded', function () {

  var config = {
    apiKey: "AIzaSyDF0zuudHb44qxJI6osHtpqbh2XWXTCI5g",
    authDomain: "genesyshackathon.firebaseapp.com",
    databaseURL: "https://genesyshackathon.firebaseio.com",
    projectId: "genesyshackathon",
    storageBucket: "genesyshackathon.appspot.com",
    messagingSenderId: "989847808310"
  };
  firebase.initializeApp(config);
  var database = firebase.database();

  // get customer age & gender
  var dataAge = firebase.database().ref('data');
  dataAge.on('value', function(snapshot) {
    var age = snapshot.val().Age;
    document.getElementById('age').innerText = 'Age: ' + age;
  });

  var dataGender = firebase.database().ref('data');
  dataGender.on('value', function(snapshot) {
    var gender = snapshot.val().Gender;
    document.getElementById('gender').innerText = 'Gender: ' + gender;
  });

  var starCountRef = firebase.database().ref('data');
  starCountRef.on('value', function(snapshot) {
    current_customer_emotion = snapshot.val().emotion;
    avg_customer_emotion = snapshot.val().Common_Emotion;
    if (current_customer_emotion == "Happiness") {
      document.getElementById('current-emotion-happy').innerText = 'Current Emotion: ' + current_customer_emotion;
      document.getElementById('current-emotion-sad').innerText = "";
      document.getElementById('current-emotion-neutral').innerText = "";
    } else if (current_customer_emotion == "Sadness" || current_customer_emotion == "Contempt" || current_customer_emotion == "Disgust" || current_customer_emotion == "Anger") {
      document.getElementById('current-emotion-sad').innerText = 'Current Emotion: ' + current_customer_emotion;
      document.getElementById('current-emotion-happy').innerText = "";
      document.getElementById('current-emotion-neutral').innerText = "";
    } else {
      document.getElementById('current-emotion-neutral').innerText = 'Current Emotion: ' + current_customer_emotion;
      document.getElementById('current-emotion-happy').innerText = "";
      document.getElementById('current-emotion-sad').innerText = "";
    }
    if (avg_customer_emotion == "Happiness") {
      document.getElementById('avg-emotion-happy').innerText = 'Common Emotion: ' + avg_customer_emotion;
      document.getElementById('avg-emotion-sad').innerText = "";
      document.getElementById('avg-emotion-neutral').innerText = "";
    } else if (avg_customer_emotion == "Sadness" || avg_customer_emotion == "Contempt" || avg_customer_emotion == "Disgust" || avg_customer_emotion == "Anger" ) {
      document.getElementById('avg-emotion-sad').innerText = 'Common Emotion: ' + avg_customer_emotion;
      document.getElementById('avg-emotion-happy').innerText = "";
      document.getElementById('avg-emotion-neutral').innerText = "";
    } else {
      document.getElementById('avg-emotion-neutral').innerText = 'Common Emotion: ' + avg_customer_emotion;
      document.getElementById('avg-emotion-happy').innerText = "";
      document.getElementById('avg-emotion-sad').innerText = "";
    }

    if (current_customer_emotion == "Angry" || current_customer_emotion == "Contempt" || current_customer_emotion == "Disgust") {
      // Get the snackbar DIV
      var x = document.getElementById("snackbar")

      // Add the "show" class to DIV
      x.className = "show";

      // After 3 seconds, remove the show class from DIV
      setTimeout(function(){ x.className = x.className.replace("show", ""); }, 5000);
    }
    //console.log(snapshot.val().value);
    //updateStarCount(postElement, snapshot.val());
  });

  let call_count = 0;
  let female_count = 0;
  let male_count = 0;
  let sad = 0;
  let angry = 0;
  let neutral = 0;
  let happy = 0;
  var commentsRef = firebase.database().ref('History');
    commentsRef.on('child_added', function(data) {
      ++call_count;
      document.getElementById('total_calls').innerText = call_count
      var tr = document.createElement('tr');
      for (var i = 0; i < 7; ++i) {
        tr.appendChild(document.createElement('td'));
        if (i == 0) {
          tr.cells[i].appendChild(document.createTextNode(data.val().Date));
        }
        if (i == 1) {
          tr.cells[i].appendChild(document.createTextNode(data.val().Agent_Name));
        }
        if (i == 2) {
          tr.cells[i].appendChild(document.createTextNode(data.val().Agent_Emotion));
        }
        if (i == 3) {
          tr.cells[i].appendChild(document.createTextNode(data.val().Customer_Age));
        }
        if (i == 4) {
          tr.cells[i].appendChild(document.createTextNode(data.val().Customer_Gender));
          if (data.val().Customer_Gender == 'male') {
            ++male_count;
          } else {
            ++female_count;
          }
        }
        if (i == 5) {
          tr.cells[i].appendChild(document.createTextNode(data.val().Customer_Emotion));
          if (data.val().Customer_Emotion == 'Sadness') {
            ++sad;
          }
          if (data.val().Customer_Emotion == 'Anger') {
            ++angry;
          }
          if (data.val().Customer_Emotion == 'Neutral') {
            ++neutral;
          }
          if (data.val().Customer_Emotion == 'Happiness') {
            ++happy;
          }
        }
        if (i == 6) {
          tr.cells[i].appendChild(document.createTextNode(data.val().Stars));
        }
      }

      var ctx = document.getElementById("myChart");
      var myChart = new Chart(ctx, {
          type: 'doughnut',
          data: {
              labels: ["Female", "Male", ],
              datasets: [{
                  label: 'Gender',
                  data: [female_count, male_count],
                  backgroundColor: [
                      'rgba(255, 99, 132, 0.2)',
                      'rgba(54, 162, 235, 0.2)',
                  ],
                  borderColor: [
                      'rgba(255,99,132,1)',
                      'rgba(54, 162, 235, 1)',
                      'rgba(255, 206, 86, 1)',
                      'rgba(75, 192, 192, 1)',
                      'rgba(153, 102, 255, 1)',
                      'rgba(255, 159, 64, 1)'
                  ],
                  borderWidth: 1
              }]
          }
      });

      var ctx2 = document.getElementById("myChart2");
      var myChart2 = new Chart(ctx2, {
          type: 'bar',
          data: {
              labels: ["Sad", "Angry", "Neutral", "Happy" ],
              datasets: [{
                  label: 'Emotions',
                  data: [sad, angry, neutral, happy],
                  backgroundColor: [
                    'rgba(255, 99, 132, 0.2)',
                    'rgba(54, 162, 235, 0.2)',
                    'rgba(255, 206, 86, 0.2)',
                    'rgba(75, 192, 192, 0.2)',
                  ],
                  borderColor: [
                      'rgba(255,99,132,1)',
                      'rgba(54, 162, 235, 1)',
                      'rgba(255, 206, 86, 1)',
                      'rgba(75, 192, 192, 1)',
                      'rgba(153, 102, 255, 1)',
                      'rgba(255, 159, 64, 1)'
                  ],
                  borderWidth: 1
              }]
          }
      });


      document.getElementById('history').append(tr);
  });

  const platformClient = require('platformClient');
  var client = platformClient.ApiClient.instance;
  const redirectUri = 'https://localhost:3000';

  client.loginImplicitGrant('2944fd6e-28c1-4f7b-8f3a-203328d70950', redirectUri)
    .then(function() {
      // Do authenticated things
    })
    .catch(function(response) {
      console.log(`${response.status} - ${response.error.message}`);
      console.log(response.error);
    });
  var apiInstance = new platformClient.UsersApi();


  var opts = {
  'id': ["59b84de1e0eb6e1c27e46cc3", "59b836a19e049e1c09f0092d", "59b2ef7c49a7611c21fd4113", "59b84de15814a31c0020071c", "59fbc883eedfcd1cb3bad0b9"], // [String] | id
  'sortOrder': "ASC", // String | Ascending or descending sort order
};
apiInstance.getUsers(opts)
  .then(function(data) {
    data = data.entities;
    let length = data.length;
    let listView = document.getElementById('profiles');
    for (var i = 0; i < length; ++i) {

      var listViewItem = document.createElement('li');
      var element = document.createElement('i');
      element.className = 'material-icons';
      element.style.lineHeight = '31px';
      element.innerHTML = 'person';
      listViewItem.appendChild(element);
      var p1 = document.createElement('p');
      var p2 = document.createElement('p');
      p1.appendChild(document.createTextNode("Name: " + data[i].name));
      p2.appendChild(document.createTextNode("Department: " + data[i].department))
      listViewItem.appendChild(p1);
      listViewItem.appendChild(p2);
      if (i % 2 == 0) {
        listViewItem.style.backgroundColor = '#EEEEEE';

      }
      listView.appendChild(listViewItem);
    }


  })
  .catch(function(error) {
  	console.log('There was a failure calling getUsers');
    console.error(error);
  });



});
