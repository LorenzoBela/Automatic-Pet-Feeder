#include <DS1302.h>
#include <Servo.h>

const int dipSwitch = 9;
const int servoPin = 8;
const int weightPin = A0;     // RPL 110 weight sensor
const int ledStatus = 11;

const int trigPin = 2;
const int echoPin = 3;

// DS1302 pins: RST, DAT, CLK
DS1302 rtc(5, 6, 7);

Servo myServo;
int weightThreshold = 20;     // Lowered threshold for sensitive detection
const int distanceThreshold = 10; // cm - for food storage level
const int hysteresis = 2;         // cm margin to avoid flicker

// RPL 110 calibration values (needs to be calibrated with known weights)
const float weightCalibrationFactor = 1.0;  // grams per ADC unit (needs calibration)
const int weightZeroOffset = 0;              // ADC reading when no weight (needs calibration)

// Sensitivity settings for RPL 110
bool diagnosticMode = true;   // Enable detailed weight diagnostics
int baselineWeight = 0;       // Baseline reading (set during startup)

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
  
  // Wait for serial to initialize
  while (!Serial) {
    ; // wait for serial port to connect
  }
  
  Serial.println("?? Arduino Pet Feeder Starting...");
  
  // initialize RTC
  rtc.halt(false);
  rtc.writeProtect(false);
  // Optional: set RTC time once (uncomment/update if you want to overwrite)
  // Time t0(2025, 1, 14, 12, 0, 0, 2);  // Updated to current date
  // rtc.time(t0);
  
  pinMode(dipSwitch, INPUT_PULLUP);
  pinMode(ledStatus, OUTPUT);
  pinMode(trigPin, OUTPUT);
  pinMode(echoPin, INPUT);

  myServo.attach(servoPin);
  myServo.write(SERVO_CLOSED_POSITION);  // Start with servo closed
  delay(500);  // Give servo time to move to position
  
  Serial.println("?? Servo initialized to closed position");
  
  // Calibrate baseline weight reading
  Serial.println("?? Calibrating RPL 110 baseline...");
  delay(1000);  // Reduced wait time
  
  long total = 0;
  for (int i = 0; i < 10; i++) {  // Reduced samples for faster startup
    total += analogRead(weightPin);
    delay(50);
  }
  baselineWeight = total / 10;
  
  Serial.print("? Baseline weight set to: "); Serial.println(baselineWeight);
  Serial.println("?? RPL 110 setup complete. Place weight to test...");
  Serial.println("?? Ready for PC commands (PING, DISPENSE_X, FEED_X)");
  Serial.println("?? Arduino setup complete - waiting for commands...");
  Serial.flush();  // Ensure all data is sent
}

// Function to handle PC commands
void processSerialCommand() {
  if (Serial.available()) {
    String command = Serial.readStringUntil('\n');
    command.trim();  // Remove whitespace
    
    Serial.print("?? Received command: "); Serial.println(command);
    
    if (command == "PING") {
      Serial.println("PONG");
      Serial.flush();  // Ensure immediate response
    }
    else if (command.startsWith("DISPENSE_") || command.startsWith("FEED_")) {
      // Extract duration from command (DISPENSE_5 or FEED_5)
      int underscoreIndex = command.indexOf('_');
      if (underscoreIndex != -1) {
        int duration = command.substring(underscoreIndex + 1).toInt();
        if (duration > 0 && duration <= 30) {  // Limit to 30 seconds max
          startDispensing(duration);
          Serial.print("? Dispensing for "); Serial.print(duration); Serial.println(" seconds");
        } else {
          Serial.println("? Invalid duration (1-30 seconds allowed)");
        }
      } else {
        Serial.println("? Invalid command format");
      }
    }
    else if (command.length() > 0) {
      Serial.print("? Unknown command: "); Serial.println(command);
    }
    
    Serial.flush();  // Ensure all responses are sent immediately
  }
}

// Function to start dispensing for specified duration
void startDispensing(int seconds) {
  if (servoActive) {
    Serial.println("?? Servo already active - ignoring command");
    return;
  }
  
  servoActive = true;
  servoStartTime = millis();
  servoDuration = seconds * 1000UL;  // Convert to milliseconds
  myServo.write(SERVO_OPEN_POSITION);
  Serial.println("??? Servo opened - dispensing food");
  Serial.flush();
}

// Function to check if dispensing should stop
void checkServoControl() {
  if (servoActive) {
    if (millis() - servoStartTime >= servoDuration) {
      // Time to stop dispensing
      servoActive = false;
      myServo.write(SERVO_CLOSED_POSITION);
      Serial.println("? Dispensing complete - servo closed");
      Serial.flush();
    }
  }
}

int getDistance() {
  const int samples = 3;  // Reduced samples for faster processing
  long readings[samples];
  int valid = 0;

  for (int i = 0; i < samples; i++) {
    digitalWrite(trigPin, LOW);
    delayMicroseconds(2);
    digitalWrite(trigPin, HIGH);
    delayMicroseconds(10);
    digitalWrite(trigPin, LOW);

    unsigned long duration = pulseIn(echoPin, HIGH, 25000UL);  // Reduced timeout
    if (duration != 0) {
      long distCm = (long)(duration * 0.034 / 2);
      if (distCm > 0 && distCm < 400) {  // Valid range check
        readings[valid++] = distCm;
      }
    }
    delay(5);  // Reduced delay
  }

  if (valid == 0) {
    return -1; // indicate timeout / no valid reading
  }

  // Simple median calculation for fewer samples
  if (valid == 1) return readings[0];
  if (valid == 2) return (readings[0] + readings[1]) / 2;
  
  // Sort for median of 3
  for (int i = 0; i < valid - 1; i++) {
    for (int j = i + 1; j < valid; j++) {
      if (readings[j] < readings[i]) {
        long t = readings[i];
        readings[i] = readings[j];
        readings[j] = t;
      }
    }
  }
  return (int)readings[valid / 2];
}

int getWeight() {
  // Take more readings for better stability with low sensitivity sensor
  const int samples = 5;  // Reduced for faster processing
  long total = 0;
  
  for (int i = 0; i < samples; i++) {
    total += analogRead(weightPin);
    delay(2);  // Reduced delay
  }
  
  return total / samples;
}

int getWeightHighRes() {
  // High resolution reading for diagnostics
  const int samples = 10;  // Reduced samples
  long total = 0;
  int minVal = 1023, maxVal = 0;
  
  for (int i = 0; i < samples; i++) {
    int reading = analogRead(weightPin);
    total += reading;
    if (reading < minVal) minVal = reading;
    if (reading > maxVal) maxVal = reading;
    delay(2);
  }
  
  // Print diagnostic info
  Serial.print("?? Weight Diagnostics - Min: "); Serial.print(minVal);
  Serial.print(" Max: "); Serial.print(maxVal);
  Serial.print(" Range: "); Serial.print(maxVal - minVal);
  Serial.print(" Avg: "); Serial.println(total / samples);
  
  return total / samples;
}

float getWeightInGrams(int rawReading) {
  // Convert raw ADC reading to grams
  // Formula: Weight = (rawReading - zeroOffset) * calibrationFactor
  return (rawReading - weightZeroOffset) * weightCalibrationFactor;
}

bool isPetPresent(int weightReading) {
  // RPL 110 typically gives higher readings when weight is applied
  return weightReading > weightThreshold;
}

bool isFoodInBowl(int weightReading) {
  // Check if there's food in the bowl based on weight change from baseline
  int weightDifference = abs(weightReading - baselineWeight);
  return weightDifference > weightThreshold;
}

void setStorageState(StorageState state, int distance) {
  if (state == STORAGE_ADEQUATE) {
    digitalWrite(ledStatus, LOW); // adequate storage ? LED OFF
    Serial.print("? Food storage adequate");
    Serial.print(" | Distance: "); Serial.print(distance); Serial.println(" cm");
  } else if (state == STORAGE_LOW) {
    digitalWrite(ledStatus, HIGH); // low storage ? LED ON
    Serial.print("?? Food storage low/empty - REFILL NEEDED");
    Serial.print(" | Distance: "); Serial.print(distance); Serial.println(" cm");
  } else { // STORAGE_UNKNOWN
    digitalWrite(ledStatus, LOW); // default LED off on timeout
    Serial.println("?? Storage sensor timeout (no echo)");
  }
}

void loop() {
  // FIRST: Process any incoming PC commands (highest priority)
  processSerialCommand();
  
  // SECOND: Check servo control (auto-stop after duration)
  checkServoControl();
  
  bool buttonState = digitalRead(dipSwitch);
  int weightReading = getWeight();
  float weightInGrams = getWeightInGrams(weightReading);
  bool foodInBowl = isFoodInBowl(weightReading);
  int distance = getDistance();

  // Manual override - button pressed (for testing/emergency)
  // Only if servo is not already active from PC command
  if (buttonState == LOW && !servoActive) {
    myServo.write(SERVO_OPEN_POSITION); // Manual dispense for testing
    Serial.println("?? Manual override: Button pressed");
  } else if (buttonState == HIGH && !servoActive) {
    myServo.write(SERVO_CLOSED_POSITION);  // Stop servo when button released (if not PC controlled)
  }

  // Check food storage level with hysteresis
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
    } // else keep previous state (within hysteresis band)
  }

  if (currentStorage != lastStorageState) {
    setStorageState(currentStorage, distance == -1 ? -1 : distance);
    lastStorageState = currentStorage;
  }

  // Enhanced monitoring output with RPL 110 diagnostics
  static unsigned long lastDbg = 0;
  if (millis() - lastDbg > 10000) { // every 10s for less spam but still informative
    Serial.println("=== ?? FEEDER MONITORING STATUS ===");
    Serial.print("?? Manual Button: "); Serial.println(buttonState == LOW ? "PRESSED" : "RELEASED");
    Serial.print("?? Servo Status: "); 
    if (servoActive) {
      unsigned long remaining = (servoDuration - (millis() - servoStartTime)) / 1000;
      Serial.print("DISPENSING ("); 
      Serial.print(remaining); 
      Serial.println("s remaining)");
    } else {
      Serial.println("IDLE");
    }
    Serial.print("?? Bowl Weight Raw: "); Serial.print(weightReading); Serial.println(" (ADC)");
    Serial.print("?? Weight: "); Serial.print(weightInGrams, 1); Serial.println("g");
    Serial.print("??? Food in Bowl: "); Serial.println(foodInBowl ? "YES" : "NO");
    
    // Run diagnostics every 30 seconds
    static unsigned long lastDiag = 0;
    if (diagnosticMode && (millis() - lastDiag > 30000)) {
      Serial.println("--- ?? RPL 110 DIAGNOSTICS ---");
      getWeightHighRes();
      Serial.println("------------------------------");
      lastDiag = millis();
    }
    
    Serial.print("?? Storage Distance: ");
    if (distance == -1) Serial.println("ERROR");
    else { Serial.print(distance); Serial.println(" cm"); }
    Serial.print("?? Storage Status: ");
    if (currentStorage == STORAGE_ADEQUATE) Serial.println("ADEQUATE");
    else if (currentStorage == STORAGE_LOW) Serial.println("LOW");
    else Serial.println("UNKNOWN");
    Serial.println("==================================");
    lastDbg = millis();
  }

  delay(50);  // Very short delay for maximum responsiveness to PC commands
}