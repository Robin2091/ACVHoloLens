# ACVHoloLens

An Active Computer Vision Augmented Reality Platform. 

The goal of this project is to develop a platform that will allow for the deployment and collection of weakly annotated data in an augmented reality setting. This is a tool for obtaining video, audio, and eye-gaze data from a user wearing the Microsoft HoloLens 2. 

The project is split up into two main parts:

* An app for the Hololens 2 which is built using Unity with the help of several different APIs and toolkits.
* Server side app developed in Python which can be executed on any machine.
 
Data is collected on the HoloLens 2 and then streamed to the server using TCP/IP sockets. 

## Requirements

A Windows 10 machine is required to develop and deploy applications for the HoloLens 2. Any OS can be used for the server side Python app.

* Python >= 3.7.6
  * ffmpeg_python==0.2.0
  * numpy==1.21.6
  * opencv==4.5.5.64
  * PyAudio==0.2.11
  * vosk==0.3.42
 
* For HoloLens 2 app development
  * Unity Hub and Unity 2020.3.33f1 LTS
  * Visual Studio Code 2019
  * Microsoft Mixed Reality Toolkit 2.8.2
  * Mixed Reality OpenXR Plugin 1.4.4

## Installation
Clone this repository to your machine and open the Unity project from Unity Hub. 

Follow the instructions found in this link to build the project and generate a Visual Studio Solution: https://docs.microsoft.com/en-us/windows/mixed-reality/develop/unity/build-and-deploy-to-hololens

Then follow the instructions in this link to deploy the application to the Hololens 2: https://docs.microsoft.com/en-us/windows/mixed-reality/develop/advanced-concepts/using-visual-studio?tabs=hl2

Deploying via USB is recommended as its faster than deploying over remote connection. Once the deployment is done you should see an app titled "ACVHololensUnity" in the list of apps on the HoloLens. 

On the server side, cd into the project directory and 'pip install -r requirements.txt'

## Usage

Firstly, cd into the server side project directory if you have not already. Create a new folder where the data collected from the HoloLens will be stored. 
Run the following command 'python Server.py --data_folder {YOUR_FOLDER}'. You will see that the server sockets are listening on some IP address. 

If both the machine the server is running on and the HoloLens are connected to Wifi (and the user wont be going into areas without Wifi signal) then no USB-C connection is required between the HoloLens and the machine. Otherwise, a portable machine would have to be used e.g. Raspberry Pi or a laptop which would be connected to the HoloLens via USB-C. 

Now, go onto the HoloLens and open the ACVHololensUnity app. You should see a welcome screen with a start button. Once you press the start button, a dialog box will appear asking you to input the IP Address of the server. If the default IP address is different than the one assigned to your server then select yes to change it. Note that the app will not allow you to continue until you have entered a valid IP address. After an IP address has been entered, a countdown will begin before you can start collecting data in your environment. You should see a message appearing that the client sockets have connected to the server sockets and you can being collecting data. 

If you lift your hand with your palm facing towards you, a menu screen will pop-up which showing several buttons: Exit, Pause (pause data collection and streaming to the server), Resume (resume collecting and streaming data), Toggle (turns on/off the eye gaze cursor) and adjust gaze angle*.

Once data collection is finished, you can exit the app. 

Now on the server side, you will notice a new folder has been created inside your data collection folder. Its named to the date and time you started the server for a particular data collection run. Inside this folder, you will find the following folders/files:

* "frames" folder containing all of the video frames collected.
* "audio_data.pkl" the serialized audio data.
* "audio_start_timestamp.txt" contains the timestamp when the HoloLens microphone started recording audio.
* "frame_timestamps.csv" contains the name of a video frame and tge corresponding timestamp of when it was captured.
* "gaze_data.csv" contains the eye gaze data (in (x,y) pixel coordinates) and the corresponding timestamps when a certain eye gaze point was collected"

You can run the following command: 'python Process_And_Visualize.py --data_folder {path_to_run_folder}' to visualize the data collected for the run. 

This command will: 
* generate the wav audio files from the serialized data
* append the wav audio files to create one large audio file
* sync and place the eye gaze data onto the video frames (these new frames will be in a newly created "gaze_frames" folder)
* create a variable frame rate video from the frames
* sync the audio and video and generate an mkv file

Now you can create a new text file named 'categories.txt' where each row contains the name of an object you annotated during the data collection run. You should also create a json file with the list of categories in the COCO dataset format named 'categories.json'. 

Run the following command: 'python GenerateDataset.py --data_folder {path_to_run_folder}' to create a weakly annotated dataset in COCO format from the data. The Vosk speech to text API is used to parse through the audio data to look for the categories listed in the text file. For each detected instance of a cetgory, a video frame that includes the category object and an eye gaze point that is located on the object are selected. A 'dataset.json' file is created that contains all of the annotations.


## Additional Resources

Create a new Unity project and add the Microsoft MRTK and OpenXR Plugin to the project: https://docs.microsoft.com/en-us/learn/modules/mixed-reality-toolkit-project-unity/1-introduction

Enable eye-tracking in project (if you are creating a new Unity project from scratch): https://docs.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/input/eye-tracking/eye-tracking-basic-setup?view=mrtkunity-2022-05. Make sure to check "Enable Eye Gaze" in the Pointers section of Input settings. 



