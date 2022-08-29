# ACVHoloLens

An Active Computer Vision Augmented Reality Platform. 

The goal of this project is to develop a platform that will allow for the deployment and collection of weakly annotated data in an augmented reality setting. This is a tool for obtaining video, audio, and eye-gaze data from a user wearing the Microsoft HoloLens 2. 

The project is split up into two main parts:

* An app for the Hololens 2 which is built using Unity with the help of several different APIs and toolkits.
* Server side app developed in Python which can be executed on any machine.
 
Data is collected on the HoloLens 2 and then streamed to the server using TCP/IP sockets. 

## Installation and Setup

A Windows 10 machine is required to develop and deploy applications for the HoloLens 2. Any OS can be used for the server side Python app.

* Python >= 3.7.6
  * ffmpeg==1.4
  * ffmpeg_python==0.2.0
  * numpy==1.21.6
  * opencv==4.5.5.64
  * PyAudio==0.2.11
  * vosk==0.3.42
 
* For HoloLens 2 app development
  * Unity 2020.3.33f1 LTS
  * Visual Studio Code 2019
  * Microsoft Mixed Reality Toolkit 2.8.2
  * Mixed Reality OpenXR Plugin 1.4.4


