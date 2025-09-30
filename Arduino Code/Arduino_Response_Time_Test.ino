/*
  Arduino Response Time Test Sketch for Pet Feeder
  Supports timing measurement and LED control for C# app testing
*/

#include <Servo.h>

// Pin definitions
const int ledPin = 13;          // Built-in LED for testing
const int servoPin = 9;         // Servo motor pin
const int trigPin = 2;          // Ultrasonic sensor trigger
const int echoPin = 3;          // Ultrasonic sensor echo
const int weightPin = A0;       // Weight sensor (analog)

// Servo control
Servo dispenserServo;
bool servoActive = false;
unsigned long servoStartTime = 0;
unsigned long servoDuration = 0;

// Response time tracking
unsigned long totalCommands = 0;
unsigned long totalResponseTime = 0;
unsigned long maxResponseTime = 0;
unsigned long minResponseTime = 999999;

// Command timing
unsigned long commandStartTime = 0;

void setup() {
  Serial.begin(9600);
  
  // Initialize pins
  pinMode(ledPin, OUTPUT);
  pinMode(trigPin, OUTPUT);
  pinMode(echoPin, INPUT);
  
  // Initialize servo
  dispenserServo.attach(servoPin);
  dispenserServo.write(0); // Closed position
  
  // Startup message
  Serial.println("Arduino Pet Feeder Response Time Test Ready");
  Serial.println("Commands: PING, LED_ON, LED_OFF, LED_TOGGLE, DISPENSE_X, FEED_X, STATUS, STATS");
  Serial.println("All commands support response time measurement");
  Serial.flush();
}

void loop() {
  // Process serial commands
  if (Serial.available()) {
    commandStartTime = micros(); // Record when command processing starts
    
    String command = Serial.readStringUntil('\n');
    command.trim();
    
    processCommand(command);
  }
  
  // Handle servo control
  handleServoControl();
  
  // Small delay for stability
  delay(1);
}

void processCommand(String command) {
  unsigned long processingTime = micros() - commandStartTime;
  
  if (command == "PING") {
    respondWithTiming("PONG", processingTime);
  }
  else if (command == "LED_ON") {
    digitalWrite(ledPin, HIGH);
    respondWithTiming("LED_ON_OK", processingTime);
  }
  else if (command == "LED_OFF") {
    digitalWrite(ledPin, LOW);
    respondWithTiming("LED_OFF_OK", processingTime);
  }
  else if (command == "LED_TOGGLE") {
    digitalWrite(ledPin, !digitalRead(ledPin));
    String state = digitalRead(ledPin) ? "ON" : "OFF";
    respondWithTiming("LED_TOGGLE_OK_" + state, processingTime);
  }
  else if (command.startsWith("DISPENSE_") || command.startsWith("FEED_")) {
    int duration = extractDuration(command);
    if (duration > 0 && duration <= 30) {
      startDispensing(duration);
      respondWithTiming("DISPENSE_STARTED_" + String(duration) + "S", processingTime);
    } else {
      respondWithTiming("ERROR_INVALID_DURATION", processingTime);
    }
  }
  else if (command == "STATUS") {
    sendStatusWithTiming(processingTime);
  }
  else if (command == "STATS") {
    sendStatsWithTiming(processingTime);
  }
  else if (command == "RESET_STATS") {
    resetStats();
    respondWithTiming("STATS_RESET_OK", processingTime);
  }
  else if (command == "GET_DISTANCE") {
    int distance = measureDistance();
    respondWithTiming("DISTANCE_" + String(distance) + "CM", processingTime);
  }
  else if (command == "GET_WEIGHT") {
    int weight = measureWeight();
    respondWithTiming("WEIGHT_" + String(weight), processingTime);
  }
  else if (command.length() > 0) {
    respondWithTiming("ERROR_UNKNOWN_COMMAND", processingTime);
  }
  
  updateStats(processingTime);
}

void respondWithTiming(String response, unsigned long processingTime) {
  Serial.print(response);
  Serial.print("#RT:");
  Serial.print(processingTime);
  Serial.print("#TS:");
  Serial.println(millis());
  Serial.flush();
}

void sendStatusWithTiming(unsigned long processingTime) {
  Serial.print("STATUS_OK#");
  Serial.print("LED:");
  Serial.print(digitalRead(ledPin) ? "ON" : "OFF");
  Serial.print("#SERVO:");
  Serial.print(servoActive ? "ACTIVE" : "IDLE");
  Serial.print("#UPTIME:");
  Serial.print(millis());
  Serial.print("#RT:");
  Serial.print(processingTime);
  Serial.print("#TS:");
  Serial.println(millis());
  Serial.flush();
}

void sendStatsWithTiming(unsigned long processingTime) {
  Serial.print("STATS#");
  Serial.print("TOTAL:");
  Serial.print(totalCommands);
  Serial.print("#AVG:");
  if (totalCommands > 0) {
    Serial.print(totalResponseTime / totalCommands);
  } else {
    Serial.print("0");
  }
  Serial.print("#MIN:");
  Serial.print(minResponseTime == 999999 ? 0 : minResponseTime);
  Serial.print("#MAX:");
  Serial.print(maxResponseTime);
  Serial.print("#RT:");
  Serial.print(processingTime);
  Serial.print("#TS:");
  Serial.println(millis());
  Serial.flush();
}

void updateStats(unsigned long responseTime) {
  totalCommands++;
  totalResponseTime += responseTime;
  
  if (responseTime > maxResponseTime) {
    maxResponseTime = responseTime;
  }
  
  if (responseTime < minResponseTime) {
    minResponseTime = responseTime;
  }
}

void resetStats() {
  totalCommands = 0;
  totalResponseTime = 0;
  maxResponseTime = 0;
  minResponseTime = 999999;
}

int extractDuration(String command) {
  int underscoreIndex = command.indexOf('_');
  if (underscoreIndex != -1) {
    return command.substring(underscoreIndex + 1).toInt();
  }
  return 0;
}

void startDispensing(int duration) {
  if (!servoActive) {
    servoActive = true;
    servoStartTime = millis();
    servoDuration = duration * 1000UL;
    dispenserServo.write(90); // Open position
  }
}

void handleServoControl() {
  if (servoActive) {
    if (millis() - servoStartTime >= servoDuration) {
      servoActive = false;
      dispenserServo.write(0); // Closed position
      Serial.println("DISPENSE_COMPLETE#TS:" + String(millis()));
      Serial.flush();
    }
  }
}

int measureDistance() {
  digitalWrite(trigPin, LOW);
  delayMicroseconds(2);
  digitalWrite(trigPin, HIGH);
  delayMicroseconds(10);
  digitalWrite(trigPin, LOW);
  
  unsigned long duration = pulseIn(echoPin, HIGH, 25000);
  if (duration == 0) {
    return -1; // Timeout
  }
  
  return (duration * 0.034) / 2; // Convert to cm
}

int measureWeight() {
  // Average multiple readings for stability
  long total = 0;
  for (int i = 0; i < 5; i++) {
    total += analogRead(weightPin);
    delay(2);
  }
  return total / 5;
}