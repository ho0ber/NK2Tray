# NK2Tray_Display Firmware Upgrade

### Almost complete rewrite of the firmware, addressing some points by chaosgabe

## New Features:
- WiFi Connection Manager added (no need to edit things like wifi settings in the code)
- Webinterface added (you can choose which display should show which icon)
- Always on mode added (displays are always on, can be disabled in order to get the old "ping style")
- Firmware Update OTA implemented
- Settings are stored on chip
- Old "Ping style" got a timeout feature (variable seconds without Ping response will disable the displays)
- more Icons added (Plex, Microphone, Speaker symbol, misc symbol, focus symbol, gaming symbol)

## Using

After first flashing of this firmware, the device will set up his own ssid (NK2Tray-Display) to configure the connection to wifi.
When connected, you should get pointed to the config page. if not, navigate to http://192.168.4.1
after setting up the wifi, the device will restart and show its IP address on the displays.
The booting up screen will disappear after 10 seconds.
After that, you can access the webinterface by opening the ip via browser.
Here you can do all your settings. after pressing "save changes", all settings will be stored to the eeprom.
The stored settings are loaded on every bootup.

If you experience bugs, please let me know.


 
