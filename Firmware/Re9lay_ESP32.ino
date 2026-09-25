/*
  Re9lay - ESP32 Sensor Streamer (MPU9250 + Muscle Sensor v3 + Onboard Bluetooth)
  Target Board: ESP32 Dev Module / ESP32-WROOM-32

  Wiring:
    EMG sensor SIG       -> GPIO34 (ADC1, input-only pin, no WiFi/BT radio conflict)
    EMG sensor GND       -> GND (signal-side ground, common with ESP32 GND)
    EMG sensor +Vs / -Vs -> Dual/Split power supply (e.g., two 9V batteries or bipolar source;
                            NOT powered from the ESP32's 3.3V rail!)
    MPU9250 VCC/GND      -> 3.3V / GND (DO NOT CONNECT TO 5V!)
    MPU9250 SDA          -> GPIO2  (D2)
    MPU9250 SCL          -> GPIO15 (D15)

  NOTE ON STRAPPING PINS:
    GPIO2 and GPIO15 are ESP32 strapping pins sampled at boot. The custom pin
    mapping (Wire.begin(2, 15)) works reliably on standard dev boards with internal
    or external pull-ups.

  EMG SIGNAL PROCESSING:
    The Muscle Sensor v3 outputs an amplified AC-coupled bipolar EMG waveform centered
    at a virtual DC bias (~2048). This sketch performs:
      1. Boot baseline auto-calibration to determine the true DC midpoint.
      2. 32-sample windowed Mean Absolute Value (MAV) oversampling at 5kHz to satisfy
         the Nyquist criterion for 50-250Hz muscle firing.
      3. Full-wave rectification (|raw - baseline|).
      4. Exponential Moving Average (EMA) low-pass filtering for a smooth 0..4095 envelope.
*/

#include <Wire.h>
#include <math.h>
#include <MPU9250.h>
#include "BluetoothSerial.h"

#if !defined(CONFIG_BT_ENABLED) || !defined(CONFIG_BLUEDROID_ENABLED)
#error Bluetooth is not enabled! Enable it in Tools > Partition Scheme.
#endif

MPU9250 mpu;
BluetoothSerial SerialBT;

const int EMG_PIN = 34;
const unsigned long SEND_INTERVAL_MS = 25;  // 40Hz stream

// Custom I2C pins for MPU9250
const int I2C_SDA_PIN = 2;
const int I2C_SCL_PIN = 15;

// ESP32 ADC: 12-bit gives 0-4095 range (~0.8mV/step)
const int ADC_RESOLUTION_BITS = 12;

// --- EMG Envelope Processing State ---
const float EMA_ALPHA = 0.20;         // Smoothing factor (0.15 = smoother, 0.25 = snappier)
float emgEnvelope = 0.0;              // Running smoothed envelope
int emgBaseline = 2048;               // DC bias midpoint (calibrated at boot)
const int CALIBRATION_SAMPLES = 200;  // 200 samples @ 5ms = 1.0s boot calibration

void calibrateEMGBaseline() {
  Serial.println("Calibrating EMG baseline - keep muscle relaxed...");
  long sum = 0;
  for (int i = 0; i < CALIBRATION_SAMPLES; i++) {
    sum += analogRead(EMG_PIN);
    delay(5);
  }
  emgBaseline = (int)(sum / CALIBRATION_SAMPLES);

  // Safety clamp: if baseline is out of nominal range (e.g. noise or flex during boot)
  if (emgBaseline < 1000 || emgBaseline > 3000) {
    Serial.println("Warning: Baseline out of nominal range. Defaulting to 2048.");
    emgBaseline = 2048;
  }

  Serial.print("EMG baseline set to: ");
  Serial.println(emgBaseline);
}

int processEMG() {
  // Burst-sample 32 readings spaced 200us apart (5kHz effective sampling rate over 6.4ms).
  // This captures the 50-250Hz AC frequency spectrum of muscle contraction without aliasing.
  long sumRectified = 0;
  for (int i = 0; i < 32; i++) {
    int raw = analogRead(EMG_PIN);
    sumRectified += abs(raw - emgBaseline);
    delayMicroseconds(200);  // 32 * 200us = 6.4ms
  }
  float currentMAV = (float)sumRectified / 32.0;

  // Scale by 2.0 so 0..~2048 rectified signal spans the full 0..~4095 12-bit range
  float scaledMAV = currentMAV * 2.0;
  if (scaledMAV > 4095.0) scaledMAV = 4095.0;

  // Exponential Moving Average (EMA) low-pass filter
  emgEnvelope = (EMA_ALPHA * scaledMAV) + ((1.0 - EMA_ALPHA) * emgEnvelope);

  return (int)emgEnvelope;
}

void setup() {
  Serial.begin(115200);

  // Set 11dB attenuation so ADC measures full 0 to 3.3V without clipping
  analogSetPinAttenuation(EMG_PIN, ADC_11db);
  analogReadResolution(ADC_RESOLUTION_BITS);
  pinMode(EMG_PIN, INPUT);

  // Calibrate DC bias before starting Bluetooth radio (cleanest signal)
  calibrateEMGBaseline();

  // Initialize Bluetooth Classic (SPP)
  SerialBT.begin("Re9lay-Glove");
  Serial.println("Bluetooth started - device name: Re9lay-Glove");

  // Fast 400kHz I2C bus on custom pins (D2/D15)
  Wire.begin(I2C_SDA_PIN, I2C_SCL_PIN);
  Wire.setClock(400000);

  Serial.println("Checking MPU connection on SDA=GPIO2, SCL=GPIO15...");
  if (mpu.setup(0x68)) {
    Serial.println("MPU connected successfully at 0x68!");
  } else {
    Serial.println("MPU not detected at 0x68. Trying 0x69...");
    if (mpu.setup(0x69)) {
      Serial.println("MPU connected successfully at 0x69!");
    } else {
      Serial.println("MPU not detected. Streaming will continue with neutral angles fallback.");
    }
  }
}

void loop() {
  // Keep AHRS sensor-fusion filter updated continuously
  mpu.update();

  // Non-blocking 40Hz transmission
  static uint32_t prev_ms = millis();
  if (millis() - prev_ms >= SEND_INTERVAL_MS) {
    sendSensorLine();
    prev_ms = millis();
  }
}

void sendSensorLine() {
  int emgValue = processEMG();

  float pitch = mpu.getPitch();
  float roll = mpu.getRoll();

  String dataString = String(pitch, 2) + "," + String(roll, 2) + "," + String(emgValue);

  SerialBT.println(dataString);  // Bluetooth to Unity
  Serial.println(dataString);    // USB Serial Monitor
}
