/*

ESP8266_ArtNetNode_DMX - webServer.ino
Copyright (c) 2015, Matthew Tong    
https://github.com/mtongnz/ESP8266_ArtNetNode_DMX

This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public
License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any
later version.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied
warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with this program.
If not, see http://www.gnu.org/licenses/

*/


/////////  Web Page & CSS - stored to flash memory ////////////

const char css[] PROGMEM = "body {text-align:center;background:#333;}\n"
    "table {margin-left:auto;margin-right:auto;max-width:500px;width:100%;border-collapse:collapse;border:none;}\n"
    "th {height:40px;background:#666;color:white;font-weight:bold;border:none;}\n"
    ".mini_head {height:20px;background:#999;}\n"
    "td {padding:6px;border:1px solid #ccc;background:#eee;text-align:left;border:none; position: relative;}\n"
    ".left {width:120px;text-align:right;vertical-align:top;}\n"
    ".centre {text-align: center;}\n"
    "input:not(.button) {float: left;}\n"
    "input:not(.number):not(.radio):not(.button):not(.checkbox) {width: 100%;}\n"
    "#viewWifiPass {width:18px, height:18px; position: absolute; top: 7px; right: 10px; }\n"
    ".number {width:50px;}\n"
    ".button {width:150px;margin:10px;}\n"
    ".smallButton {width:80px;}\n"
    ".static_table {border-collapse:collapse;}\n"
    "p {padding:0px;margin:0px;font-size:14px; clear: both;}\n"
    "a {color:#00A;text-decoration:none;}\n"
    "a:hover {color:#00F;text-decoration:underline;}\n"
    ".bigLink {color: white;}\n"
    ".round-button {display:block; float: left; margin-right: 7px; width:14px; height:14px; font-size:14px; line-height:14px; border-radius: 50%; color:#ffffff; text-align:center; text-decoration:none; background: #5555ff; box-shadow: 0 0 2px #9999ff; font-weight:bold; font-family: 'Comic Sans', serif;}";
    
const char page_head[] PROGMEM = "<html><head><title>NK2Tray Display</title>\n"
    "<link rel='stylesheet' type='text/css' href='/style.css'>\n"
    "<meta name='viewport' content='width=400'>"
    "</head><body>\n"
    "<table id='wrap'>\n";

const char home_top[] PROGMEM = "<tr><th colspan=5><a href='/' class='bigLink'>Home</a></th></tr>"
    "<form method='POST' action='/save' name='Settings'>\n";

const char save_top[] PROGMEM = "<tr><th><a href='/' class='bigLink'>Home</a></th></tr><tr><td><center>";

const char save_tail[] PROGMEM = "</center></td></tr></table></body></html>";
    
const char form_tail[] PROGMEM = "<tr><th colspan=5 class='centre'>\n"
    "<input type='hidden' name='savechanges' value='yes'>\n"
    "<input type='submit' value='Save Changes' class='button'>\n"
    "</th></tr></form>\n"
    "</table><br /></body>"
    "</html>";

/* getFlashString()
 *  Get our strings stored in flash memory
 */
String getFlashString(const char *fStr) {
  int len = strlen_P(fStr);
  char buffer[len+1];
  int k;
  
  for (k = 0; k < len; k++)
    buffer[k] =  pgm_read_byte_near(fStr + k);
  
  buffer[k] = 0;
  
  return String(buffer);
}



/* startWebServer()
 *  Very self explanitory - it starts our webserver
 *  Sets the handlers for the various pages we will serve
 */
void startWebServer() {
  webServer.on("/", webHome);
  webServer.on("/save", webSave);
  webServer.on("/style.css", webCSS);
  webServer.onNotFound(webNotFound);

  //MDNS.begin(wifiSSID);
  webServer.begin();
  //MDNS.addService("http", "tcp", 80);
  
  #ifdef VERBOSE
    Serial.println("HTTP server started");
  #endif
}



/* webHome()
 *  Our main web page.
 */
void webHome() {
  
  #ifdef VERBOSE
    Serial.println("HTTP Request Received");
  #endif
  
  // Initialize our page from our flash strings
  String message = getFlashString(page_head);
  message += getFlashString(home_top);

  // Our MAC Address
  message += "<tr><td class='left'>Mac Address</td><td colspan='4'>"
        + MAC_address
        + "</td></tr>";

  // Always on:
  message += "<tr><td class='left'>Always on Mode</td><td colspan='4'>";
  message += "<input type='checkbox' name='always_on' value='yes'";
  if (always_on)
  {
    message += " checked";
  }
  message += "></td></tr>";
  
  // IP
  message += "<tr class='static_table'>";
  message += "<td class='left'>Remote IP</td>\n";
  for (int x = 0; x < 4; x++) {
    message += "<td><input type='number' name='ip_";
    message += char(x+48);
    message += "' value='"
        + String(remote_ip[x])
        + "' min=0 max=255 class='number'></td>\n";
  }
  message += "</tr>\n";

  message += "<tr><td class='left'>Ping interval</td><td colspan='4'>";
  message += "<input type='number' name='pint' value='" + String(interval / 1000) + "' min='1' max='600' class='number'></td>\n</tr>\n";

  message += "<tr class='disp_config'>";
  message += "<td align='center' colspan='5'><div align='center'>Display Config</div></td></tr>\n<tr>";
  
  for (int i = 0; i <= 7; i++)
  {
    message += "<td class='left' colspan='2'>Display "+ String(i) + ": </td><td align='center' colspan='3'><select name='displayconfig"+String(i)+"' id='"+String(i)+"'>\n";
    for (int x = 0; x <= 17; x++)
    {
      message += "<option value='"+String(x)+"'";
      if (display[i] == x) {
        message += " selected='selected'";
      }
      message += ">"
      + icons[x]
      + "</option>\n";
    }
    message += "</select></td></tr><br><br>\n";
  }
  
  message += "</table>\n<table>\n";
  
  // Add the end of the form & page
  message += getFlashString(form_tail);

  // Send to the client
  webServer.sendHeader("Connection", "close");
  webServer.send(200, "text/html", message);
  
}



/* webSave()
 *  Handle the Save buttons being pressed on web page.
 *  Copy data into our global variables.
 *  Verifies data then calls our saveSettings function.
 *  Resets node if the save and reset button clicked
 */
void webSave() {
 
  
  #ifdef VERBOSE
    Serial.println("HTTP Save Request includes arguements");
  #endif
  
  char * split;
  char tmpArg[30];
  String message = "";

  if (webServer.arg("savechanges") == "yes") {
    
      #ifdef VERBOSE
    Serial.println("HTTP Post check ok");
  #endif
  
    bool err = 0;
    IPAddress tmp1(0,0,0,0);
    IPAddress tmp2(0,0,0,0);
    IPAddress tmp3(0,0,0,0);
    int x;

 
    for (x = 0; x < 4; x++) {
      char id[5] = "ip_";
      id[3] = x+48;
      id[4] = 0;
      webServer.arg(id).toCharArray(tmpArg, 30);
      tmp1[x] = atoi(tmpArg);
      if (tmp1[x] < 0 || tmp1[x] > 255) {
        err = 1;
      } 
    }
    
    if (err == 1) {
      message += "- Invalid IP Address<br />";
    err = 0;
    }
    // If we dont have error message, set the IP variables
    if (message.length() == 0) {
      // All IPs are valid, store them
      remote_ip = tmp1;
    }
    
    int newinterval = webServer.arg("pint").toInt();
    if (newinterval < 0 || newinterval > 600) {
      message += "- Invalid Ping interval!<br />";
      #ifdef VERBOSE
        Serial.print("Interval error: ");
        Serial.println(interval);
      #endif  
    } else {
      interval = newinterval * 1000;
      #ifdef VERBOSE
        Serial.print("new interval: ");
        Serial.println(interval);
      #endif
    }
        
  for (int x = 0; x <= 7; x++)
  {
 
    char displayconfigi[14] = "displayconfig";
    displayconfigi[13] = x+48;
    displayconfigi[14] = 0;
    display[x] = webServer.arg(displayconfigi).toInt();
  }
  if (webServer.arg("always_on") == "yes")
  {
    always_on = true;
  } else {
    always_on = false;
  }
  // Save settings to EEPROM
  if (saveSettings()) {
    // IP issues from above - the IPs didn't get saved but the rest did
    if (message.length() > 0) {
      message = "Some changes saved.  There were the following issues:<br />\n<br />\n" + message;
    } else {
      message += "Changes Saved.";
      refresh_displays = true;
    }
  // Error saving our settings to EEPROM
  } else {
    message = "Error saving settings.  Please try again.";
  }

  //Generate final page using flash strings
  String tmp = getFlashString(page_head);
  tmp += getFlashString(save_top);
  tmp += message;
  tmp += getFlashString(save_tail);

  // Send page
  webServer.sendHeader("Connection", "close");
  webServer.send(200, "text/html", tmp);
  
  } else {
    #ifdef VERBOSE
    Serial.println("HTTP POST check not ok");
  #endif
}
}



/* webCSS()
 *  Send our style sheet to the web client
 */
void webCSS() {
  webServer.sendHeader("Connection", "close");
  webServer.send(200, "text/html", getFlashString(css));
}

/* webNotFound()
 *  display a 404 page
 */
void webNotFound() {
  // Check if we're recalling a stored DMX state
  char charBuf[webServer.uri().length() + 1];
  webServer.uri().toCharArray(charBuf, webServer.uri().length() + 1);
  char* tmp = strtok(charBuf, "/.");
    
  #ifdef VERBOSE
    Serial.println("Sending 404");
    Serial.print("URI: ");
    Serial.println(webServer.uri());
    Serial.print("Method: ");
    Serial.println(( webServer.method() == HTTP_GET ) ? "GET" : "POST");
    Serial.print("Arguments: ");
    Serial.println(webServer.args());
  #endif

  // Generate page from flash strings
  String message = getFlashString(page_head);
  message += getFlashString(save_top);
  message += "404: File Not Found\n<br />\n<br />";
  message += "URI: ";
  message += webServer.uri();
  message += "<br />\n<br />\n<a href='/'>Go to settings page</a>";
  message += getFlashString(save_tail);

  // Send page
  webServer.sendHeader("Connection", "close");
  webServer.send(200, "text/html", message);
}
