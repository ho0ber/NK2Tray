// function to save our variables into EEPROM
// Writes OK to 500 & 501 - use this to verify a write
bool saveSettings() {
  
  #ifdef VERBOSE
    Serial.print("Saving Settings... ");
  #endif

  // Wipe our OK and check
  EEPROM.write(500, '\0');
  EEPROM.write(501, '\0');
  EEPROM.commit();
  if(EEPROM.read(500) != '\0'|| EEPROM.read(501) != '\0') {
      #ifdef VERBOSE
        Serial.println("fail!");  
      #endif  
    return false;
  }
  
  delay(100);
  
  EEPROM.write(100, remote_ip[0]);
  EEPROM.write(101, remote_ip[1]);
  EEPROM.write(102, remote_ip[2]);
  EEPROM.write(103, remote_ip[3]);
 
  if (always_on) {
    EEPROM.write(104, 1);
  } else {
    EEPROM.write(104, 0);
  }
  
  for (int i = 0; i <= 7; i++)
  {
    EEPROM.write(105+i, display[i]);
  }

  EEPROM.write(115, interval/1000);
    
  EEPROM.write(500, 'O');
  EEPROM.write(501, 'K');

  delay(100);
  
  EEPROM.commit();
  
  delay (100);

  // Verify our OK was written & return false if not
  if(EEPROM.read(500) != 'O' || EEPROM.read(501) != 'K') {
    #ifdef VERBOSE
      Serial.println("fail!");
    #endif
    return false;
  }
  #ifdef VERBOSE
    Serial.println("success!");
  #endif

  // Return true if all went well
  return true;
}



// Function to load our settings from EEPROM to our variables.
bool loadSettings() {
  #ifdef VERBOSE
    Serial.print("Loading Settings... ");
  #endif
  

// Check if we have previous saves.  If not, return false
  if(EEPROM.read(500) != 'O' || EEPROM.read(501) != 'K') {
    #ifdef VERBOSE
      Serial.println("No previous saves.");
    #endif    
    return false;
  }

   remote_ip[0] = EEPROM.read(100);
   remote_ip[1] = EEPROM.read(101);
   remote_ip[2] = EEPROM.read(102);
   remote_ip[3] = EEPROM.read(103);

   if (EEPROM.read(104) == 0)
   {
      always_on = false;
   } else {
      always_on = true;
   }
    
   for (int i = 0; i <= 7; i++)
   {
     display[i] = EEPROM.read(105+i);
   }

   interval = EEPROM.read(115) * 1000;
   
   #ifdef VERBOSE
     Serial.println("success!");
   #endif
   // Return
   return true;
}
