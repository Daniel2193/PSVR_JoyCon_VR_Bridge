# NOT FINISHED YET

# Overview

This Repo is my attempt on making VR Games playable without actual VR Hardware

For this Project im using a PSVR Headset and 2 Nintendo Switch Joycons


# Known issues

- Small drift on left Joycon
- Small drift on right Joycon when moving


# Setup

If you want to use it you need these 2 programs

- TrinusPSVR (https://www.trinusvirtualreality.com/psvr/)

    -> Set Mode to "SteamVR"
    
    -> Go to the Tracking Tab
    
    -> Set "Head Device" to "FreePieHead Position"
    
    -> Set "Left Hand Device" to "FreePieLeft"
    
    -> Set "Right Hand Device" to "FreePieRight"
    
    -> [Temporary] Enable "Follow Headset"
    
    -> [Temporary] Enable "Rotate With Headset"
    
    -> [Temporary] Play around with the "Controller Position" Sliders until they are where you want them to be
    
- FreePie (https://github.com/AndersMalmgren/FreePIE)

    -> Install
    
    [Optional] -> Open and run the FreePieIODebug.py Script
    
    
- Then copy the "hidapi.dll" file from "PSVR_JoyCon_VR_Bridge/FreePieIO_Module/PrecompiledResources/hidapi.dll" to your FreePie install directory (default: C:\Program Files (x86)\FreePie\). You can build the DLL file from this (https://github.com/libusb/hidapi) repository if you want to do it yourself or have problems with the precompiled DLL.


# Changelog


# Upcoming stuff

- Cleaning and adding JoyCon communication into this Repository

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
- Raspberry Pi 4B (Camera 1)
- Raspberry Pi 3B+ (Camera 2) [not in use yet]


# Credits

-hidapi.dll from https://github.com/libusb/hidapi

-Joycon interface based on https://github.com/gb2111/JoyconLib-4-CS
