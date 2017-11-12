const express = require('express')
var https = require('https');
var http = require('http');
var fs = require('fs');

const app = express()
var path = require('path');



// This line is from the Node.js HTTPS documentation.
var options = {
  key: fs.readFileSync('client-key.pem'),
  cert: fs.readFileSync('client-cert.pem')
};

app.get('/js/jquery-3.2.1.min.js',function(req,res){
    res.sendFile(path.join(__dirname + '/js/jquery-3.2.1.min.js'));
});

app.get('/js/app.js',function(req,res){
    res.sendFile(path.join(__dirname + '/js/app.js'));
});



// js
app.get('/js/bootstrap-notify.js',function(req,res){
    res.sendFile(path.join(__dirname + '/js/bootstrap-notify.js'));
});
app.get('/js/bootstrap.min.js',function(req,res){
    res.sendFile(path.join(__dirname + '/js/bootstrap.min.js'));
});

app.get('/js/chartist.min.js',function(req,res){
    res.sendFile(path.join(__dirname + '/js/chartist.min.js'));
});
app.get('/js/demo.js',function(req,res){
    res.sendFile(path.join(__dirname + '/js/demo.js'));
});
app.get('/js/material-dashboard.js',function(req,res){
    res.sendFile(path.join(__dirname + '/js/material-dashboard.js'));
});
app.get('/js/material.min.js',function(req,res){
    res.sendFile(path.join(__dirname + '/js/material.min.js'));
});

// css
app.get('/css/bootstrap.min.css',function(req,res){
    res.sendFile(path.join(__dirname + '/css/bootstrap.min.css'));
});
app.get('/css/demo.css',function(req,res){
    res.sendFile(path.join(__dirname + '/css/demo.css'));
});
app.get('/css/material-dashboard.css',function(req,res){
    res.sendFile(path.join(__dirname + '/css/material-dashboard.css'));
});

app.get('/', function(req, res) {
    res.sendFile(path.join(__dirname + '/index.html'));
});


// Create an HTTP service.
http.createServer(app).listen(8080);
// Create an HTTPS service identical to the HTTP service.
https.createServer(options, app).listen(3000);
//app.listen(3000, () => console.log('Example app listening on port 3000!'))
