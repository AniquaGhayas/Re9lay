/*
  ==============================================================
  Re9lay - Simple EMG Sensor Diagnostic & Test Tool (ESP32)
  ==============================================================
  This sketch tests your Muscle Sensor v3 / EMG module on the ESP32.
  It works with both the Arduino Serial Monitor and Serial Plotter!

  Wiring:
    EMG SIG   -> GPIO 34 (ADC1, input-only pin)
    EMG GND   -> ESP32 GND (common ground is mandatory!)
    EMG Power -> Connect your dual power supply (+Vs, -Vs, GND)
                 or 3.3V/5V depending on your specific module.

  How to use:
    1. Upload this sketch to your ESP32.
    2. Open Tools > Serial Plotter (or Tools > Serial Monitor) at 115200 baud.
    3. Keep your arm relaxed for the first 2 seconds during auto-zeroing.
    4. Flex your muscle: you will see the envelope spike upwards!
*/

const int EMG_PIN = 34;

// Baseline DC bias point (nominal midpoint for a dual-supply AC signal is ~2048)
int emgBaseline = 2048;
float emgEnvelope = 0.0;
const float EMA_ALPHA = 0.20; // Smoothing factor (0.1 = smoother, 0.3 = faster)

void setup() {
  Serial.begin(115200);
  delay(1000);

  // Configure ESP32 12-bit ADC (0 to 4095) with 11dB attenuation (0 to 3.3V)
  analogSetPinAttenuation(EMG_PIN, ADC_11db);
  analogReadResolution(12);
  pinMode(EMG_PIN, INPUT);

  Serial.println("\n--- EMG Sensor Test Initialized ---");
  Serial.println("Keep muscle relaxed for 2 seconds to calibrate baseline...");

  // Calibrate baseline: average 200 samples over 1 second
  long sum = 0;
  for (int i = 0; i < 200; i++) {
    sum += analogRead(EMG_PIN);
    delay(5);
  }
  emgBaseline = sum / 200;

  if (emgBaseline < 500 || emgBaseline > 3500) {
    Serial.println("Notice: Baseline was outside normal range. Defaulting to 2048.");
    emgBaseline = 2048;
  }

  Serial.print("Calibrated DC Baseline: ");
  Serial.println(emgBaseline);
  Serial.println("Starting live stream... Open 'Tools > Serial Plotter' for a live waveform graph!\n");
  delay(1000);
}

void loop() {
  // 1. Take a 16-sample burst window to capture the AC frequency spectrum
  long sumRect = 0;
  int latestRaw = 0;
  for (int i = 0; i < 16; i++) {
    latestRaw = analogRead(EMG_PIN);
    sumRect += abs(latestRaw - emgBaseline);
    delayMicroseconds(250);
  }

  // 2. Mean Absolute Value (MAV)
  float mav = (float)sumRect / 16.0;

  // 3. Scale to full 12-bit range (0 - 4095)
  float scaled = mav * 2.0;
  if (scaled > 4095.0) scaled = 4095.0;

  // 4. Low-pass filter (Exponential Moving Average)
  emgEnvelope = (EMA_ALPHA * scaled) + ((1.0 - EMA_ALPHA) * emgEnvelope);

  // 5. Output formatted for Arduino Serial Plotter:
  // Shows: Raw_ADC, DC_Baseline, and the Smoothed_Envelope
  Serial.print("Raw_ADC:");
  Serial.print(latestRaw);
  Serial.print(" ");
  Serial.print("Baseline:");
  Serial.print(emgBaseline);
  Serial.print(" ");
  Serial.print("Envelope:");
  Serial.print((int)emgEnvelope);
  Serial.print(" ");
  Serial.print("Threshold:");
  Serial.print(1200); // Visual reference line for triggering a shot

  // Indicate state in text if using standard Serial Monitor
  if (emgEnvelope > 1200) {
    Serial.print(" [CONTRACTED]");
  } else {
    Serial.print(" [RELAXED]");
  }
  Serial.println();

  delay(25); // ~40Hz refresh rate
}
