// Display Firmware V2, made by piLo, tweaked by ZeeuwsGamertje
// I2C Adressen:
// Oled: 0x78
// Multi:0x70

// Directive for Serial Debugging
//#define VERBOSE

#include "Wire.h"               // Wire.h for I2c
#include <ssd1306.h>            // 128x64 Display Ansteuerung, https://github.com/lexus2k/ssd1306
#include <ESP8266WiFi.h>        // Include the Wi-Fi library
#include <ESP8266Ping.h>        // Include the Ping library, https://github.com/dancol90/ESP8266Ping
#include <DNSServer.h>          // For WiFi Manager
#include <ESP8266WebServer.h>   // For Settings from Webinterface
#include <WiFiManager.h>        // WiFiManager for AP and Client related Stuff
#include <ArduinoOTA.h>         // For flashing over the Air
#include <EEPROM.h>             // EEPROM Stuff
#include "logos.h"

#define MUX_Address 0x70        // TCA9548A Multiplexer Adresse

ESP8266WebServer webServer(80); // Init Webserver

WiFiManager wifiManager;  // Init WiFiManager
    
IPAddress remote_ip(192, 168, 178, 69);       // Default value for Remote Host
bool rem_host_online;                         // Online Status Remote Host
unsigned long previousMillis = 0;             // For periodically Checking Host
long interval = 10000;                  // default value for checking host in s
  
bool always_on = true;                        // Default value for Always On mode

bool refresh_displays = false;                // Flag for refreshing displays

String local_ip;                              // String for esp8266 IP

String MAC_address;                           // String for MAC-Address
uint8_t MAC_array[6];                         // Char Array for MAC Adress

char chartmp[50];                             // CharTemp for converting Strings to CharArrays

int display[] = { 0,0,0,0,0,0,0,0};           // Which Disp show what - default value
String icons[] = { "Chrome", "Discord", "Edge", "Empty", "Firefox", "Focus", "Gaming", "Master", "Mic", "Miscellaneous", "Mumble", "OperaGX", "Plex", "Safari", "Spotify", "System Sounds", "Teams", "Teamspeak", "Unassigned", "World of Warcraft", "YT-Music" }; //Possible Values for Icons

// Function for initializing I2C buses using TCA9548A I2C Multiplexer
void tcaselect(uint8_t i2c_bus) 
{
    if (i2c_bus > 7) return;
    Wire.beginTransmission(MUX_Address);
    Wire.write(1 << i2c_bus);
    Wire.endTransmission(); 
}

// Function to set specified display to icon
void set_display(int display, int icon)
{
      tcaselect(display);
      switch (icon) {
        case 0: 
          ssd1306_printFixed(45,  56, "Chrome", STYLE_NORMAL);
          ssd1306_drawBitmap(32, 0, 64, 56, chromeLogo);
          break;
        case 1:
          ssd1306_printFixed(42,  56, "Discord", STYLE_NORMAL);
          ssd1306_drawBitmap(32, 0, 64, 56, discordLogo);
          break;
        case 2:
          ssd1306_printFixed(50,  56, "Edge", STYLE_NORMAL);
          ssd1306_drawBitmap(32, 0, 64, 56, edgeLogo);
          break;
        case 3:
          ssd1306_printFixed(62,  56, "", STYLE_NORMAL);
          ssd1306_drawBitmap(32, 0, 64, 56, noLogo);
          break;
        case 4:
          ssd1306_printFixed(42,  56, "Firefox", STYLE_NORMAL);
          ssd1306_drawBitmap(32, 0, 64, 56, firefoxLogo);
          break;
        case 5:
          ssd1306_printFixed(49,  56, "Focus", STYLE_NORMAL);
          ssd1306_drawBitmap(32, 0, 64, 56, focusLogo);
          break;
        case 6:
          ssd1306_printFixed(45,  56, "Gaming", STYLE_NORMAL);
          ssd1306_drawBitmap(32, 0, 64, 56, gamingLogo);
          break;
        case 7:
          ssd1306_printFixed(45,  56, "Master", STYLE_NORMAL);
          ssd1306_drawBitmap(32, 0, 64, 56, masterLogo);
          break;
        case 8:
          ssd1306_printFixed(54,  56, "Mic", STYLE_NORMAL);
          ssd1306_drawBitmap(32, 0, 64, 56, micLogo);
          break;
        case 9:
          ssd1306_printFixed(50,  56, "Miscellaneous", STYLE_NORMAL);
          ssd1306_drawBitmap(32, 0, 64, 56, miscLogo);
          break;
        case 10:
          ssd1306_printFixed(45,  56, "Mumble", STYLE_NORMAL);
          ssd1306_drawBitmap(32, 0, 64, 56, mumbleLogo);
          break;
        case 11:
          ssd1306_printFixed(42,  56, "OperaGX", STYLE_NORMAL);
          ssd1306_drawBitmap(32, 0, 64, 56, operagxLogo);
          break;
        case 12:
          ssd1306_printFixed(50,  56, "Plex", STYLE_NORMAL);
          ssd1306_drawBitmap(32, 0, 64, 56, plexLogo);
          break;
        case 13:
          ssd1306_printFixed(45,  56, "Safari", STYLE_NORMAL);
          ssd1306_drawBitmap(32, 0, 64, 56, safariLogo);
          break;
        case 14:
          ssd1306_printFixed(42,  56, "Spotify", STYLE_NORMAL);
          ssd1306_drawBitmap(32, 0, 64, 56, spotifyLogo);
          break;        
        case 15:
          ssd1306_printFixed(23,  56, "System Sounds", STYLE_NORMAL);
          ssd1306_drawBitmap(32, 0, 64, 56, windowsLogo);
          break;
        case 16:
          ssd1306_printFixed(49,  56, "Teams", STYLE_NORMAL);
          ssd1306_drawBitmap(32, 0, 64, 56, teamsLogo);
          break;
        case 17:
          ssd1306_printFixed(38,  56, "Teamspeak", STYLE_NORMAL);
          ssd1306_drawBitmap(32, 0, 64, 56, teamspeakLogo);
          break;
        case 18:
          ssd1306_printFixed(36,  56, "Unassigned", STYLE_NORMAL);
          ssd1306_drawBitmap(32, 0, 64, 56, unassignedLogo);
          break;
        case 19:
          ssd1306_printFixed(54,  56, "WoW", STYLE_NORMAL);
          ssd1306_drawBitmap(32, 0, 64, 56, wowLogo);
          break;
        case 20:
          ssd1306_printFixed(50,  56, "YT-Music", STYLE_NORMAL);
          ssd1306_drawBitmap(32, 0, 64, 56, ytmusicLogo);
          break;
              
      }
}

// function to clear all screens
void clear_screens()
{
  for (int i = 0; i <= 7; i++)
  {
    tcaselect(i);
    ssd1306_clearScreen();
  }
}

// function for checking remote host
bool check_remote_host()
{
  unsigned long currentMillis = millis();
  if (currentMillis - previousMillis >= interval)
  {
    previousMillis = currentMillis; 
    if (Ping.ping(remote_ip)) {
      #ifdef VERBOSE
        Serial.println("Host ONLINE!");
        Serial.println();
        int avg_time_ms = Ping.averageTime();
        Serial.print("Ping: ");
        Serial.print(avg_time_ms);
        Serial.println("ms");
        Serial.println();
      #endif
      rem_host_online = true;
    } else {
      #ifdef VERBOSE
        Serial.println("Host offline!");
      #endif
      rem_host_online=false;
    }
  }
}

// function for getting mac address
void getMac() {
  char MAC_char[30] = "";
  
  WiFi.macAddress(MAC_array);
  
  // Format the MAC address into string
  sprintf(MAC_char, "%02X", MAC_array[0]);
  for (int i = 1; i < 6; ++i) {
    sprintf(MAC_char, "%s : %02X", MAC_char, MAC_array[i]);
  }
  MAC_address = String(MAC_char);
}

// Setup Code
void setup()
{
    EEPROM.begin(512);
    #ifdef VERBOSE
      Serial.begin(9600);
      Serial.println("Setup Started");
    #endif
      
    wifiManager.autoConnect("NK2Tray_Display"); 

    local_ip = WiFi.localIP().toString();
    local_ip.toCharArray(chartmp, 50);

    getMac();
    
    startWebServer();

    ArduinoOTA.setHostname("NK2Tray-Display");
    ArduinoOTA.onProgress([](unsigned int progress, unsigned int total) {
    for ( int i = 0; i <= 7; i++)
    {
      tcaselect(i);
      ssd1306_drawBitmap(32, 0, 64, 64, firmwareLogo);
      ssd1306_printFixed(14, 56, "updating firmware", STYLE_NORMAL);
      #ifdef VERBOSE
        Serial.printf("Progress: %u%%\r", (progress / (total / 100)));
      #endif
    }
    });
    ArduinoOTA.begin();

    loadSettings();

  for (int i = 0; i <= 7; i++)
  {
    tcaselect(i);
    delay(10);
    ssd1306_128x64_i2c_init();
    delay(10);
    ssd1306_setFixedFont(ssd1306xled_font6x8);
  }

  clear_screens();
  
  tcaselect(0);  
  delay(10);
  ssd1306_128x64_i2c_init();
  delay(10);
  ssd1306_drawBitmap(0, 0, 128, 64, piloLogo);
  tcaselect(1);
  ssd1306_printFixed(34, 32, "Booting...", STYLE_NORMAL);
  tcaselect(2);
  ssd1306_printFixed(54, 32, "IP: ", STYLE_NORMAL);
  tcaselect(3);
  ssd1306_printFixed(20, 32, chartmp, STYLE_NORMAL);
  tcaselect(4);
  ssd1306_printFixed(20, 32, "NK2Tray-Display", STYLE_NORMAL);  
  tcaselect(5);
  ssd1306_printFixed(15, 32, "Firmware by piLo", STYLE_NORMAL);
  tcaselect(6);
  ssd1306_printFixed(0, 30, "Tweaked by", STYLE_NORMAL);
  ssd1306_printFixed(0, 40, "ZeeuwsGamertje", STYLE_NORMAL);
  tcaselect(7);
  ssd1306_drawBitmap(0, 0, 128, 64, zeeuwsLogo);
  
  delay(10000);

  clear_screens();

  refresh_displays = true;
}

//Schleifencode
void loop()
{
    if(always_on == true) {
      if(refresh_displays)
      {
        clear_screens();
        for (int i = 0; i <= 7; i++)
        {
          set_display(i, display[i]);
        }
        refresh_displays = false;
      }
    
    } else if (rem_host_online) {
      if (refresh_displays)
      {  
        clear_screens();
        for (int i = 0; i <= 7; i++)
        {
          set_display(i, display[i]);
        }
        refresh_displays = false;      
      }
    } else {
        clear_screens();
        refresh_displays=true;
    }
    
    if(!always_on)
    {
      check_remote_host();
    }
    webServer.handleClient();
    ArduinoOTA.handle();
}
