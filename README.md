# Wirelessly Controlled Robot Through Advanced Arm Motion Capture Tracking and Gesture Recognition

### This submission includes:

- C code for both the transmitter Arduino on the glove, 
as well as the receiver on the robot. Pitch and roll are used for driving motion.
Abrupt Yaw changes are used for requesting song playback. Every time a
byte is received, a random number is generated to determine whether a tune should
play, indexed 3-8. 

- Two versions (for both KinectV1 and V2) of the part responsible for interfacing and driving the Arm
through the use of the Kinect API as well as the Leap API. Further integration
with the Oculus Rift is possible through a third party software.

	- Note: The RS232 WiFi communication protocol source code is the result
		of decompiling and reverse engineering an existing piece of 
		software that achieved WiFi connectivity 
		(Arm manufacturer Software - AREXX (*1) ). As such, my
		involvement in designing CommBase.cs, CommLine.cs, RS232.cs
		and Win32Com.cs was minimal.  

- Front-end communicating to an App (*2) on my IPhone, in order to retrieve a video
stream and turn the torch on and off. 
and the front-end represents a heavily modified version. Integration into
the WPF application is done using the Awesomium.NET framework.


### Additional note:

The AVR program (.hex file) operating on the Robotic Arm microcontroller is the one provided
by AREXX themselves. (*1)

*1 http://www.arexx.com/robot_arm/html/en/software.htm#windows
*2 https://itunes.apple.com/gb/app/ipcamera-high-end-network/id570912928?mt=8)


### USAGE:

- The Kinect V1 or V2 should be connected before the App is launched. If the video
stream is not displaying, please make sure the drivers and installed.
- The Leap motion should be Plug and Play, and should be automatically recognized
by the App even if plugged in after launched.
- In order to connect to the ARM, make sure the RS232 transceiver is plugged in,
and that the Robot has power. You will need to click the power button on the
ARM before being able to connect (there should be a blue LED).
- The Servos can be controlled manually, or automatically through either the
Leap or Kinect as indicated, by ticking the appropriate checkboxes.


####  Connecting to the Phone APP:

Make sure your phone is visible (on the network) to your computer and viceversa.
Download the App above, install it and then leave it running. You now need
to modify all entries in "camera.html" so that it points to the address running
on the server (replace "http://192.168.1.126:8080").

Now, provided the phone is running the app, the address in camera.html is correct,
and both devices can see one another, the stream should appear on the app once
started.

The phone is useful in order to see where the robot is heading towards, to
let the user know the position of the robotic arm, as well as being able
to turn the light on if needed.

![alt tag](https://raw.github.com/radu-chc/Wirelessly-Controlled-Robot-Through-Advanced-Arm-Motion-Capture-Tracking-and-Gesture-Recognition/master/Report/finallook.jpg)