#include <DS1302.h>
#include <Servo.h>
#include <HX711.h>

const int dipSwitch = 9;
const int servoPin = 8;
const int hx711_DT = A1;      // HX711 data pin
const int hx711_SCK = A0;     // HX711 clock pin

const int trigPin = 2;
const int echoPin = 3;

// DS1302 pins: RST, DAT, CLK
DS1302 rtc(5, 6, 7);

Servo myServo;
HX711 scale;

// HX711 calibration values (needs to be calibrated with known weights)
float calibrationFactor = -7050;  // This value needs to be calibrated for your specific load cell
const int weightThreshold = 5;    // grams - threshold for detecting weight
const int distanceThreshold = 10; // cm - for food storage level
const int hysteresis = 2;         // cm margin to avoid flicker

// HX711 settings
bool diagnosticMode = false;  // Disable diagnostics to save memory
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
  Serial.begin(9600);
  
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
  
  // Initialize HX711 with basic setup
  Serial.println(F("Initializing HX711..."));
  scale.begin(hx711_DT, hx711_SCK);
  delay(100);
  
  // Simple HX711 initialization
  for (int attempt = 0; attempt < 3; attempt++) {
    if (scale.is_ready()) {
      long rawReading = scale.read();
      if (rawReading != 0) {
        // Reset and calibrate scale
        scale.set_scale();
        scale.tare();
        scale.set_scale(calibrationFactor);
        
        // Take baseline reading
        delay(500);
        float total = 0;
        int validReadings = 0;
        for (int i = 0; i < 5; i++) {
          if (scale.is_ready()) {
            float reading = scale.get_units();
            if (reading > -5000 && reading < 5000) {
              total += reading;
              validReadings++;
            }
            delay(50);
          }
        }
        
        if (validReadings > 0) {
          baselineWeight = total / validReadings;
          scaleReady = true;
          Serial.println(F("HX711 initialized"));
          break;
        }
      }
    }
    delay(500);
  }
  
  if (!scaleReady) {
    Serial.println(F("HX711 failed - check wiring"));
  }
  
  Serial.println(F("Ready for commands"));
  Serial.println(F("Use CAL command to calibrate"));
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
      if (scaleReady && scale.is_ready()) {
        scale.tare();
        baselineWeight = 0;
        Serial.println(F("Scale tared"));
      } else {
        Serial.println(F("Scale not ready"));
      }
    }
    else if (command == "HX_TEST") {
      testHX711();
    }
    else if (command == "CAL") {
      calibrateScale();
    }
    else if (command.startsWith("SET_CAL_")) {
      // Set calibration factor: SET_CAL_-7050
      int underscoreIndex = command.lastIndexOf('_');
      if (underscoreIndex != -1) {
        float newCalFactor = command.substring(underscoreIndex + 1).toFloat();
        calibrationFactor = newCalFactor;
        scale.set_scale(calibrationFactor);
        Serial.print(F("Calibration factor set to: "));
        Serial.println(calibrationFactor);
      }
    }
    
    Serial.flush();
  }
}

// Load cell calibration routine
void calibrateScale() {
  if (!scale.is_ready()) {
    Serial.println(F("HX711 not ready for calibration"));
    return;
  }
  
  Serial.println(F("=== LOAD CELL CALIBRATION ==="));
  Serial.println(F("Step 1: Remove all weight from scale"));
  Serial.println(F("Press any key when ready..."));
  
  // Wait for user input
  while (!Serial.available()) {
    delay(100);
  }
  Serial.readString(); // Clear buffer
  
  // Tare the scale
  Serial.println(F("Taring scale..."));
  scale.set_scale();
  scale.tare();
  Serial.println(F("Scale tared to zero"));
  
  Serial.println(F("Step 2: Place known weight on scale"));
  Serial.println(F("Common weights: 100g, 500g, 1000g"));
  Serial.println(F("Enter weight in grams (e.g., 500):"));
  
  // Wait for weight input
  float knownWeight = 0;
  while (knownWeight == 0) {
    if (Serial.available()) {
      String input = Serial.readStringUntil('\n');
      input.trim();
      knownWeight = input.toFloat();
      if (knownWeight <= 0) {
        Serial.println(F("Invalid weight. Enter positive number:"));
        knownWeight = 0;
      }
    }
    delay(100);
  }
  
  Serial.print(F("Using known weight: "));
  Serial.print(knownWeight);
  Serial.println(F("g"));
  Serial.println(F("Place weight on scale and press any key..."));
  
  // Wait for user to place weight
  while (!Serial.available()) {
    delay(100);
  }
  Serial.readString(); // Clear buffer
  
  Serial.println(F("Reading scale..."));
  delay(1000);
  
  // Get raw reading with weight
  long rawWithWeight = 0;
  for (int i = 0; i < 10; i++) {
    if (scale.is_ready()) {
      rawWithWeight += scale.read();
    }
    delay(100);
  }
  rawWithWeight /= 10;
  
  // Calculate calibration factor
  float newCalibrationFactor = (float)rawWithWeight / knownWeight;
  
  Serial.println(F("=== CALIBRATION RESULTS ==="));
  Serial.print(F("Raw reading: "));
  Serial.println(rawWithWeight);
  Serial.print(F("Known weight: "));
  Serial.print(knownWeight);
  Serial.println(F("g"));
  Serial.print(F("Calculated factor: "));
  Serial.println(newCalibrationFactor);
  
  // Apply new calibration factor
  calibrationFactor = newCalibrationFactor;
  scale.set_scale(calibrationFactor);
  
  // Test the calibration
  Serial.println(F("Testing calibration..."));
  delay(500);
  float measuredWeight = scale.get_units();
  Serial.print(F("Measured weight: "));
  Serial.print(measuredWeight, 1);
  Serial.println(F("g"));
  
  float error = abs(measuredWeight - knownWeight);
  Serial.print(F("Error: "));
  Serial.print(error, 1);
  Serial.println(F("g"));
  
  if (error < (knownWeight * 0.05)) { // 5% tolerance
    Serial.println(F("CALIBRATION SUCCESSFUL!"));
  } else {
    Serial.println(F("CALIBRATION NEEDS ADJUSTMENT"));
    Serial.println(F("Try again or use SET_CAL command"));
  }
  
  Serial.println(F("========================"));
  Serial.print(F("NEW CALIBRATION FACTOR: "));
  Serial.println(calibrationFactor);
  Serial.println(F("Update your code with this value!"));
  Serial.println(F("========================"));
}

// Simplified HX711 diagnostic function
void testHX711() {
  Serial.println(F("=== HX711 TEST ==="));
  
  if (scale.is_ready()) {
    Serial.println(F("HX711 ready"));
    long rawReading = scale.read();
    Serial.print(F("Raw: "));
    Serial.println(rawReading);
    
    float weight = scale.get_units();
    Serial.print(F("Weight: "));
    Serial.print(weight, 1);
    Serial.println(F("g"));
    
    Serial.print(F("Current cal factor: "));
    Serial.println(calibrationFactor);
  } else {
    Serial.println(F("HX711 NOT ready"));
    Serial.println(F("Check: DT->A1, SCK->A0"));
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
  if (!scaleReady || !scale.is_ready()) {
    return -999.0;
  }
  
  float total = 0;
  int validReadings = 0;
  
  for (int i = 0; i < 3; i++) {
    if (scale.is_ready()) {
      float reading = scale.get_units();
      if (reading > -5000 && reading < 5000) {
        total += reading;
        validReadings++;
      }
    }
    delay(5);
  }
  
  return (validReadings > 0) ? total / validReadings : -999.0;
}

bool isFoodInBowl(float weightReading) {
  if (weightReading < -500) return false;
  return abs(weightReading - baselineWeight) > weightThreshold;
}

void setStorageState(StorageState state, int distance) {
  if (state == STORAGE_ADEQUATE) {
    Serial.print(F("Storage OK: "));
    Serial.print(distance);
    Serial.println(F("cm"));
  } else if (state == STORAGE_LOW) {
    Serial.print(F("Storage LOW: "));
    Serial.print(distance);
    Serial.println(F("cm"));
  } else {
    Serial.println(F("Storage ERROR"));
  }
}

void loop() {
  processSerialCommand();
  checkServoControl();
  
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
    int lowThreshold = distanceThreshold - hysteresis;
    int highThreshold = distanceThreshold + hysteresis;

    if (distance <= lowThreshold) {
      currentStorage = STORAGE_ADEQUATE;
    } else if (distance >= highThreshold) {
      currentStorage = STORAGE_LOW;
    }
  }

  if (currentStorage != lastStorageState) {
    setStorageState(currentStorage, distance);
    lastStorageState = currentStorage;
  }

  // Simple status output every 15 seconds
  static unsigned long lastDbg = 0;
  if (millis() - lastDbg > 15000) {
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
      Serial.println(F("cm"));
    }
    
    Serial.println(F("=============="));
    lastDbg = millis();
  }

  delay(50);
}