<img src="https://media.discordapp.net/attachments/532233406482743300/653946714561970176/IMG_20190927_114511.jpg?width=841&height=631">

# NK2Tray_Display
A simple addon for the Korg nanoKONTROL2 providing 8 tiny OLED displays to show the software that is controlled by the particular fader. The displays are controlled by a ESP8266 via an I2C multiplexer.

## alternate Firmware
alternative firmware in the Subfolder "NK2Tray_DisplayFW_v2". See readme in this folder.

#### The good
* Easy to read even in daylight
* OLED displays do not use energy when the don´t show anything, so standby is pretty low
* Will automatically switch off the displays when the host can no longer be pinged

#### The bad
* Displays might burn in, have not noticed that yet though
* No fixation between the NK2 and the display yet

#### The ugly
* ~~The shown icons are hardcoded in the arduino sketch~~
* ~~The host ip is hardcoded in the arduino sketch~~

  (Solved thanks to the new firmware V1 from piLo)

## How to build you own
You will need a handfull of modules easily available online, the PCB and a few 3d printed parts.
I provide links to the used modules in the BOM as well as an description in case those links die. 
The 3d files are in this folder, as well as the gerber files for the PCB and the arduino sketch.


## Disclaimer
I am not a software developer neither am i an electrical engineer. So basically i have no clue on what the best practices, general rules or even the correct terms regarding software/hardware are.
If you do know those things please tell me, i will try to learn.
I am also not an native English speaker.







