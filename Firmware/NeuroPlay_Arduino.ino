#include <Wire.h>
#include <math.h>
#include <MPU9250.h>
#include <SoftwareSerial.h>

MPU9250 mpu;
int emgPin = A0;
SoftwareSerial BTSerial(10, 11); // RX, TX

void setup() {
  Serial.begin(115200);
  BTSerial.begin(9600);   // SoftwareSerial Bluetooth baud rate: 9600
  pinMode(emgPin, INPUT);
  Wire.begin();

  Serial.println("Checking MPU connection...");
  if (mpu.setup(0x68)) {
    Serial.println("MPU connected successfully!");
  } else {
    Serial.println("MPU not detected. Check wiring or I2C address.");
  }
}

void loop() {
  if (mpu.update()) {
    static uint32_t prev_ms = millis();
    if (millis() > prev_ms + 25) {
      print_roll_pitch_yaw();
      prev_ms = millis();
    }
  }
}

void print_roll_pitch_yaw() {
  int emgValue = analogRead(emgPin);

  float pitch = mpu.getPitch();
  float roll = mpu.getRoll();

  String dataString = String(pitch, 2) + "," + String(roll, 2) + "," + String(emgValue);

  Serial.println(dataString);
  BTSerial.println(dataString);

  delay(250);
}
