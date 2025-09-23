#include <DS1302.h>
#include <Servo.h>
#include <HX711_ADC.h>
#if defined(ESP8266)|| defined(ESP32) || defined(AVR)
#include <EEPROM.h>
#endif

const int dipSwitch = 9;
const int servoPin = 8;
const int hx711_DT = 11;      // HX711 data pin
const int hx711_SCK = 12;     // HX711 clock pin

const int trigPin = 2;
const int echoPin = 3;

// DS1302 pins: RST, DAT, CLK
DS1302 rtc(5, 6, 7);

Servo myServo;
HX711_ADC LoadCell(hx711_DT, hx711_SCK);

// HX711 calibration system - EXACTLY from working example
const int calVal_eepromAdress = 0;
unsigned long t = 0;

// Pet feeder specific settings
const int weightThreshold = 5;    // grams - threshold for detecting weight
const int distanceThreshold = 7;  // cm - NEW: 7cm and above = empty/no food, below 7cm = food available
const int hysteresis = 1;         // cm margin to avoid flicker (reduced for tighter control)

// HX711 settings
float baselineWeight = 0;     // Baseline reading (set during startup)
bool scaleReady = false;      // Track if scale is properly initialized

// Servo control variables
bool servoActive = false;
unsigned long servoStartTime = 0;
unsigned long servoDuration = 0;
const int SERVO_OPEN_POSITION = 180;  // Position to dispense food
const int SERVO_CLOSED_POSITION = 0;  // Position to stop dispensing

enum StorageState { STORAGE_UNKNOWN, STORAGE_ADEQUATE, STORAGE_LOW };
StorageState lastStorageState = STORAGE_UNKNOWN;

void setup() {
  Serial.begin(9600); delay(10);  // Changed back to 9600 for GUI compatibility
  Serial.println();
  Serial.println(F("Pet Feeder Starting..."));
  
  // initialize RTC
  rtc.halt(false);
  rtc.writeProtect(false);
  
  pinMode(dipSwitch, INPUT_PULLUP);
  pinMode(trigPin, OUTPUT);
  pinMode(echoPin, INPUT);

  myServo.attach(servoPin);
  myServo.write(SERVO_CLOSED_POSITION);  // Start with servo closed
  delay(500);  // Give servo time to move to position
  
  Serial.println(F("Servo initialized"));
  
  // EXACT HX711 initialization from working example
  LoadCell.begin();
  //LoadCell.setReverseOutput(); //uncomment to turn a negative output value to positive
  
  // Load calibration value from EEPROM
  float calibrationValue = 696.0; // Default value if nothing in EEPROM
#if defined(ESP8266)|| defined(ESP32)
  EEPROM.begin(512);
#endif
  
  // Try to get calibration value from EEPROM
  EEPROM.get(calVal_eepromAdress, calibrationValue);
  
  // Check if EEPROM value is valid (not NaN or extreme values)
  if (isnan(calibrationValue) || calibrationValue < -10000 || calibrationValue > 10000) {
    calibrationValue = 696.0; // Use default if EEPROM value is invalid
    Serial.println(F("Using default calibration value: 696.0"));
  } else {
    Serial.print(F("Loaded calibration from EEPROM: "));
    Serial.println(calibrationValue);
  }
  
  unsigned long stabilizingtime = 2000; // preciscion right after power-up can be improved by adding a few seconds of stabilizing time
  boolean _tare = true; //set this to false if you don't want tare to be performed in the next step
  LoadCell.start(stabilizingtime, _tare);
  if (LoadCell.getTareTimeoutFlag() || LoadCell.getSignalTimeoutFlag()) {
    Serial.println("Timeout, check MCU>HX711 wiring and pin designations");
    scaleReady = false;
  }
  else {
    LoadCell.setCalFactor(calibrationValue); // Set the loaded calibration value
    Serial.println("Startup is complete");
    Serial.print(F("Scale ready with calibration factor: "));
    Serial.println(calibrationValue);
    scaleReady = true;
  }
  while (!LoadCell.update());
  
  Serial.println(F("Ready for commands"));
  Serial.println(F("Commands: PING, DISPENSE_X, FEED_X, TARE, CAL, WEIGHT, DISTANCE"));
  Serial.println(F("Calibration: send 'r' for calibrate, 't' for tare, 'c' for manual cal"));
  
  // Initial sensor tests
  Serial.println(F("=== INITIAL SENSOR TEST ==="));
  int testDistance = getDistance();
  Serial.print(F("Initial distance reading: "));
  if (testDistance == -1) {
    Serial.println(F("ERROR - Check ultrasonic sensor wiring"));
  } else {
    Serial.print(testDistance);
    Serial.print(F("cm"));
    if (testDistance < 7) {
      Serial.println(F(" (Food Available)"));
    } else {
      Serial.println(F(" (Empty/Low)"));
    }
  }
  Serial.println(F("============================="));
  
  Serial.flush();
}

// Function to handle PC commands
void processSerialCommand() {
  if (Serial.available()) {
    String command = Serial.readStringUntil('\n');
    command.trim();
    
    if (command == "PING") {
      Serial.println("PONG");
    }
    else if (command.startsWith("DISPENSE_") || command.startsWith("FEED_")) {
      int underscoreIndex = command.indexOf('_');
      if (underscoreIndex != -1) {
        int duration = command.substring(underscoreIndex + 1).toInt();
        if (duration > 0 && duration <= 30) {
          startDispensing(duration);
          Serial.print(F("Dispensing for "));
          Serial.print(duration);
          Serial.println(F(" seconds"));
        } else {
          Serial.println(F("Invalid duration"));
        }
      }
    }
    else if (command == "TARE") {
      LoadCell.tareNoDelay(); //tare
      Serial.println(F("Taring..."));
    }
    else if (command == "CAL" || command == "CALIBRATE") {
      calibrate(); //calibrate
    }
    else if (command == "WEIGHT") {
      // Get current weight reading immediately
      if (scaleReady && LoadCell.update()) {
        float weight = LoadCell.getData();
        Serial.print(F("Load_cell output val: "));
        Serial.println(weight);
        Serial.print(F("Weight: "));
        Serial.print(weight, 1);
        Serial.println(F("g"));
        Serial.print(F("Calibration factor: "));
        Serial.println(LoadCell.getCalFactor());
      } else {
        Serial.println(F("Weight not available"));
        Serial.println(F("Scale ready: "));
        Serial.println(scaleReady ? F("YES") : F("NO"));
      }
    }
    else if (command == "HX_TEST") {
      testHX711();
    }
    else if (command == "STATUS") {
      // Comprehensive status report
      Serial.println(F("=== SYSTEM STATUS ==="));
      Serial.print(F("Scale Ready: "));
      Serial.println(scaleReady ? F("YES") : F("NO"));
      
      if (scaleReady) {
        Serial.print(F("Calibration Factor: "));
        Serial.println(LoadCell.getCalFactor());
        
        if (LoadCell.update()) {
          float weight = LoadCell.getData();
          Serial.print(F("Current Weight: "));
          Serial.print(weight, 1);
          Serial.println(F("g"));
        }
      }
      
      Serial.print(F("Servo: "));
      Serial.println(servoActive ? F("ACTIVE") : F("IDLE"));
      
      int distance = getDistance();
      Serial.print(F("Storage Distance: "));
      if (distance == -1) {
        Serial.println(F("ERROR"));
      } else {
        Serial.print(distance);
        Serial.print(F("cm"));
        
        // Add interpretation based on new thresholds
        if (distance < 7) {
          Serial.println(F(" (Food Available)"));
        } else {
          Serial.println(F(" (Empty/Low)"));
        }
      }
      Serial.println(F("Distance Thresholds: <7cm=Food Available, >=7cm=Empty/Low"));
      Serial.println(F("===================="));
    }
    else if (command == "DISTANCE") {
      // Get immediate distance reading
      int distance = getDistance();
      if (distance != -1) {
        Serial.print("Distance: ");
        Serial.print(distance);
        Serial.println("cm");
      } else {
        Serial.println("Distance: ERROR");
      }
    }
    
    Serial.flush();
  }
}

// EXACT calibration function from working example
void calibrate() {
  Serial.println("***");
  Serial.println("Start calibration:");
  Serial.println("Place the load cell an a level stable surface.");
  Serial.println("Remove any load applied to the load cell.");
  Serial.println("Send 't' from serial monitor to set the tare offset.");

  boolean _resume = false;
  while (_resume == false) {
    LoadCell.update();
    if (Serial.available() > 0) {
      if (Serial.available() > 0) {
        char inByte = Serial.read();
        if (inByte == 't') LoadCell.tareNoDelay();
      }
    }
    if (LoadCell.getTareStatus() == true) {
      Serial.println("Tare complete");
      _resume = true;
    }
  }

  Serial.println("Now, place your known mass on the loadcell.");
  Serial.println("Then send the weight of this mass (i.e. 100.0) from serial monitor.");

  float known_mass = 0;
  _resume = false;
  while (_resume == false) {
    LoadCell.update();
    if (Serial.available() > 0) {
      known_mass = Serial.parseFloat();
      if (known_mass != 0) {
        Serial.print("Known mass is: ");
        Serial.println(known_mass);
        _resume = true;
      }
    }
  }

  LoadCell.refreshDataSet(); //refresh the dataset to be sure that the known mass is measured correct
  float newCalibrationValue = LoadCell.getNewCalibration(known_mass); //get the new calibration value

  Serial.print("New calibration value has been set to: ");
  Serial.print(newCalibrationValue);
  Serial.println(", use this as calibration value (calFactor) in your project sketch.");
  Serial.print("Save this value to EEPROM adress ");
  Serial.print(calVal_eepromAdress);
  Serial.println("? y/n");

  _resume = false;
  while (_resume == false) {
    if (Serial.available() > 0) {
      char inByte = Serial.read();
      if (inByte == 'y') {
#if defined(ESP8266)|| defined(ESP32)
        EEPROM.begin(512);
#endif
        EEPROM.put(calVal_eepromAdress, newCalibrationValue);
#if defined(ESP8266)|| defined(ESP32)
        EEPROM.commit();
#endif
        EEPROM.get(calVal_eepromAdress, newCalibrationValue);
        Serial.print("Value ");
        Serial.print(newCalibrationValue);
        Serial.print(" saved to EEPROM address: ");
        Serial.println(calVal_eepromAdress);
        _resume = true;

      }
      else if (inByte == 'n') {
        Serial.println("Value not saved to EEPROM");
        _resume = true;
      }
    }
  }

  Serial.println("End calibration");
  Serial.println("***");
  Serial.println("To re-calibrate, send 'r' from serial monitor.");
  Serial.println("For manual edit of the calibration value, send 'c' from serial monitor.");
  Serial.println("***");
}

// EXACT changeSavedCalFactor function from working example
void changeSavedCalFactor() {
  float oldCalibrationValue = LoadCell.getCalFactor();
  boolean _resume = false;
  Serial.println("***");
  Serial.print("Current value is: ");
  Serial.println(oldCalibrationValue);
  Serial.println("Now, send the new value from serial monitor, i.e. 696.0");
  float newCalibrationValue;
  while (_resume == false) {
    if (Serial.available() > 0) {
      newCalibrationValue = Serial.parseFloat();
      if (newCalibrationValue != 0) {
        Serial.print("New calibration value is: ");
        Serial.println(newCalibrationValue);
        LoadCell.setCalFactor(newCalibrationValue);
        _resume = true;
      }
    }
  }
  _resume = false;
  Serial.print("Save this value to EEPROM adress ");
  Serial.print(calVal_eepromAdress);
  Serial.println("? y/n");
  while (_resume == false) {
    if (Serial.available() > 0) {
      char inByte = Serial.read();
      if (inByte == 'y') {
#if defined(ESP8266)|| defined(ESP32)
        EEPROM.begin(512);
#endif
        EEPROM.put(calVal_eepromAdress, newCalibrationValue);
#if defined(ESP8266)|| defined(ESP32)
        EEPROM.commit();
#endif
        EEPROM.get(calVal_eepromAdress, newCalibrationValue);
        Serial.print("Value ");
        Serial.print(newCalibrationValue);
        Serial.print(" saved to EEPROM address: ");
        Serial.println(calVal_eepromAdress);
        _resume = true;
      }
      else if (inByte == 'n') {
        Serial.println("Value not saved to EEPROM");
        _resume = true;
      }
    }
  }
  Serial.println("End change calibration value");
  Serial.println("***");
}

// HX711 diagnostic function
void testHX711() {
  Serial.println(F("=== HX711 TEST ==="));
  
  if (scaleReady) {
    Serial.println(F("HX711 ready"));
    
    if (LoadCell.update()) {
      float weight = LoadCell.getData();
      Serial.print(F("Weight: "));
      Serial.print(weight, 2);
      Serial.println(F("g"));
    }
    
    Serial.print(F("Cal factor: "));
    Serial.println(LoadCell.getCalFactor());
  } else {
    Serial.println(F("HX711 NOT ready"));
    Serial.println(F("Check: DT->11, SCK->12"));
  }
  Serial.println(F("=================="));
}

void startDispensing(int seconds) {
  if (servoActive) {
    Serial.println(F("Servo busy"));
    return;
  }
  
  servoActive = true;
  servoStartTime = millis();
  servoDuration = seconds * 1000UL;
  myServo.write(SERVO_OPEN_POSITION);
  Serial.println(F("Dispensing started"));
}

void checkServoControl() {
  if (servoActive) {
    if (millis() - servoStartTime >= servoDuration) {
      servoActive = false;
      myServo.write(SERVO_CLOSED_POSITION);
      Serial.println(F("Dispensing complete"));
    }
  }
}

int getDistance() {
  digitalWrite(trigPin, LOW);
  delayMicroseconds(2);
  digitalWrite(trigPin, HIGH);
  delayMicroseconds(10);
  digitalWrite(trigPin, LOW);

  unsigned long duration = pulseIn(echoPin, HIGH, 25000UL);
  if (duration != 0) {
    long distCm = (long)(duration * 0.034 / 2);
    if (distCm > 0 && distCm < 400) {
      return (int)distCm;
    }
  }
  return -1;
}

float getWeight() {
  if (!scaleReady) {
    return -999.0;
  }
  
  if (LoadCell.update()) {
    return LoadCell.getData();
  }
  
  return -999.0; // No new data available
}

bool isFoodInBowl(float weightReading) {
  if (weightReading < -500) return false;
  return abs(weightReading - baselineWeight) > weightThreshold;
}

void setStorageState(StorageState state, int distance) {
  if (state == STORAGE_ADEQUATE) {
    Serial.print(F("Food Available: "));
    Serial.print(distance);
    Serial.println(F("cm"));
  } else if (state == STORAGE_LOW) {
    Serial.print(F("Storage Empty/Low: "));
    Serial.print(distance);
    Serial.println(F("cm"));
  } else {
    Serial.println(F("Storage ERROR"));
  }
}

void loop() {
  static boolean newDataReady = 0;
  const int serialPrintInterval = 5000; //print every 5 seconds instead of constantly
  static unsigned long lastDistanceReading = 0;
  const int distanceReadingInterval = 3000; // Send distance every 3 seconds

  // Process PC commands first
  processSerialCommand();
  checkServoControl();
  
  // EXACT load cell handling from working example
  // check for new data/start next conversion:
  if (LoadCell.update()) newDataReady = true;

  // get smoothed value from the dataset:
  if (newDataReady) {
    if (millis() > t + serialPrintInterval) {
      float i = LoadCell.getData();
      Serial.print("Load_cell output val: ");
      Serial.println(i);
      
      // Also send weight in GUI-friendly format
      Serial.print("Weight: ");
      Serial.print(i, 1);
      Serial.println("g");
      
      newDataReady = 0;
      t = millis();
    }
  }

  // Send distance readings regularly for GUI
  if (millis() - lastDistanceReading > distanceReadingInterval) {
    int distance = getDistance();
    if (distance != -1) {
      Serial.print("Distance: ");
      Serial.print(distance);
      Serial.println("cm");
    } else {
      Serial.println("Distance: ERROR");
    }
    lastDistanceReading = millis();
  }

  // receive command from serial terminal - EXACT from working example
  if (Serial.available() > 0) {
    char inByte = Serial.read();
    if (inByte == 't') LoadCell.tareNoDelay(); //tare
    else if (inByte == 'r') calibrate(); //calibrate
    else if (inByte == 'c') changeSavedCalFactor(); //edit calibration value manually
  }

  // check if last tare operation is complete
  if (LoadCell.getTareStatus() == true) {
    Serial.println("Tare complete");
    baselineWeight = 0; // Reset baseline after tare
  }
  
  // Pet feeder specific functionality
  bool buttonState = digitalRead(dipSwitch);
  float weightReading = getWeight();
  bool foodInBowl = isFoodInBowl(weightReading);
  int distance = getDistance();

  // Manual override
  if (buttonState == LOW && !servoActive) {
    myServo.write(SERVO_OPEN_POSITION);
    Serial.println(F("Manual override"));
  } else if (buttonState == HIGH && !servoActive) {
    myServo.write(SERVO_CLOSED_POSITION);
  }

  // Storage level check
  StorageState currentStorage = lastStorageState;

  if (distance == -1) {
    currentStorage = STORAGE_UNKNOWN;
  } else {
    // NEW LOGIC: 7cm+ = empty/low, below 7cm = adequate food
    int lowThreshold = distanceThreshold - hysteresis;   // 6cm (7-1)
    int highThreshold = distanceThreshold + hysteresis;  // 8cm (7+1)

    if (distance < lowThreshold) {  // Less than 6cm = Food Available
      currentStorage = STORAGE_ADEQUATE;
    } else if (distance >= highThreshold) {  // 8cm or more = Empty/Low
      currentStorage = STORAGE_LOW;
    }
    // Between 6-8cm maintains previous state (hysteresis zone)
  }

  if (currentStorage != lastStorageState) {
    setStorageState(currentStorage, distance);
    lastStorageState = currentStorage;
  }

  // Simple status output every 20 seconds
  static unsigned long lastDbg = 0;
  if (millis() - lastDbg > 20000) {
    Serial.println(F("=== STATUS ==="));
    Serial.print(F("Button: "));
    Serial.println(buttonState == LOW ? F("PRESSED") : F("RELEASED"));
    Serial.print(F("Servo: "));
    Serial.println(servoActive ? F("ACTIVE") : F("IDLE"));
    
    if (scaleReady && weightReading > -500) {
      Serial.print(F("Weight: "));
      Serial.print(weightReading, 1);
      Serial.println(F("g"));
      Serial.print(F("Food: "));
      Serial.println(foodInBowl ? F("YES") : F("NO"));
    } else {
      Serial.println(F("Weight: ERROR"));
    }
    
    Serial.print(F("Distance: "));
    if (distance == -1) {
      Serial.println(F("ERROR"));
    } else {
      Serial.print(distance);
      Serial.print(F("cm"));
      
      // Add status interpretation
      if (distance < 7) {
        Serial.println(F(" (Food Available)"));
      } else {
        Serial.println(F(" (Empty/Low)"));
      }
    }
    
    Serial.println(F("=============="));
    lastDbg = millis();
  }

  delay(50);
}