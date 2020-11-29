# ABANDONED PROJECT
If anyone would like to continue this project feel free to do so


# Overview

This Repo is my attempt on making VR Games playable without actual VR Hardware

For this Project im using a PSVR Headset and 2 Nintendo Switch Joycons


# Known issues

- Resetting the Joycon orientation (Yaw Axis) sometimes not work as supposed

    -> Easy Fix: Aling your Joycon to the rotation you see in VR and press Reset again
    
    -> If this doesnt work just restart the FreePieIO_Module.exe

- One Joycon sometimes dont communicate even though they are connected correctly

    -> Easy Fix: Just Restart the program
    

# Setup / How to Run it

If you want to use it you need these 2 programs

- TrinusPSVR (https://www.trinusvirtualreality.com/psvr/)

    -> Set Mode to "SteamVR"
    
    -> Go to the Tracking Tab
    
    -> Set "Head Device" to "FreePieHead Position"
    
    -> Set "Left Hand Device" to "FreePieLeft"
    
    -> Set "Right Hand Device" to "FreePieRight"
    
    -> Disable "Follow Headset"
    
    -> Disable "Rotate With Headset"
    
    -> Play around with the "Controller Position" Sliders until they are as close as possible to your elbows
    
    Here is how they look on my setup:
    
    <img width="99" alt="aaaaaaaaaaaaa" src="https://user-images.githubusercontent.com/23408743/82271441-12dfbf00-9978-11ea-9f06-9e569b8a1a1b.PNG">
    
- FreePie (https://github.com/AndersMalmgren/FreePIE)

    -> Install
    
    [Optional] -> Open and run the FreePieIODebug.py Script
    
    
- Then copy the "hidapi.dll" file from "PSVR_JoyCon_VR_Bridge/FreePieIO_Module/PrecompiledResources/hidapi.dll" to your FreePie install directory (default: C:\Program Files (x86)\FreePie\). You can build the DLL file from this (https://github.com/libusb/hidapi) repository if you want to do it yourself or have problems with the precompiled DLL.

- Make sure your Joycons are connected to your Computer. Then start the "FreePieIO_Module.exe". After a few seconds the last line should be "Awaiting calibration...". Place the Joycons on a flat surface and Press ENTER/RETURN to start the calibration. After the calibration is done you should be good to go.


# Mapping

- Touchpad Touch -> Analog Stick

- Touchpad Press -> Analog Stick Press

- Trigger -> ZL / ZR

- Grip -> (Left)SR / (Right)SL

- System -> - / +

- Menu -> DpadLeft / Y

- [Reset Orientation] -> Home/Capture


# Changelog

- 19.05.2020

    -> Added Relative Posiotioning


- 15.04.2020

    -> Added adaptive Drift Filter / calibration
    
    -> Added Reset Orientation mapped to Home/Capture Button (for each Joycon)


- 13.04.2020
    
    -> Added Joycon communication
    
    -> Added basic Drift filter

# Upcoming stuff

- Adding GUI

- Adding external Position Tracking

- Add Kalman filter

- Cleaning and adding the Raspberry Pi Scripts to this Repository



# Development
Im just a solo developer so if you want to help me feel free to start an issue or a pull request

If you want to contact me for some reason here's my Discord Tag: Daniel2193#2154 but if you do please tell me that you came from github


# My Setup
- Windows 10 Home x64
- Intel Core i7-6700k
- 16GB RAM
- AMD Radeon RX 590 (same Power as NVidias GTX 1070)
- 2 JoyCons
- PSVR Headset (Nothing else just the Headset)
- Raspberry Pi 4B (Camera 1) [not in use yet]
- Raspberry Pi 3B+ (Camera 2) [not in use yet]


# Credits

-hidapi.dll from https://github.com/libusb/hidapi

-Joycon interface based on https://github.com/gb2111/JoyconLib-4-CS
