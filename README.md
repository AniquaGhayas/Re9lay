# 🚀 Re9lay — Gamified IoT Neuro-Rehabilitation System

<div align="center">

![Unity](https://img.shields.io/badge/Unity-2022.3%2B%20LTS-blue?logo=unity)
![Hardware](https://img.shields.io/badge/Hardware-ESP32--WROOM--32-red?logo=espressif)
![Bluetooth](https://img.shields.io/badge/Wireless-Bluetooth%20Classic%20SPP-blue?logo=bluetooth)
![C#](https://img.shields.io/badge/Language-C%23-239120?logo=csharp)
![Python](https://img.shields.io/badge/Backend-FastAPI-009688?logo=fastapi)
![Cloud](https://img.shields.io/badge/Cloud-Render-46E3B7?logo=render)
![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Android-green)
![License](https://img.shields.io/badge/License-MIT-brightgreen)

**Transforming upper-limb physical rehabilitation into an engaging, biofeedback-driven 2D space shooter powered by wearable IMU and sEMG sensors.**

</div>

---

## 📖 Overview

Traditional upper-extremity physical therapy after stroke, spinal cord injury, or orthopedic surgery often involves repetitive, tedious exercises leading to low patient adherence and subjective clinical evaluations. 

**Re9lay** is an IoT-enabled gamified rehabilitation platform that bridges interactive gaming with clinical neuro-rehabilitation:
* **Wearable IoT Sensor Glove:** An **ESP32-WROOM-32** equipped with an **MPU-9250** (9-DOF IMU) and **Muscle Sensor v3 / Surface EMG** captures wrist kinematics and muscle contractions in real time.
* **Onboard High-Speed Bluetooth & MAV DSP:** Streams telemetry wirelessly via onboard Classic Bluetooth (SPP) at **115,200 baud** with **12-bit ADC** resolution ($0 - 4095$), utilizing on-chip windowed Mean Absolute Value (MAV) oversampling at 5kHz.
* **Biofeedback Gameplay:** Real-time wrist pitch and roll kinematics steer the player's spacecraft relative to an auto-calibrated neutral resting baseline, while active muscle contractions fire defensive lasers.
* **Mandatory Neuromuscular Relaxation Cycle:** Enforces an active contraction-relaxation cycle before subsequent laser firing can occur, directly inhibiting post-stroke spastic hypertonia.
* **Clinical Station HUD & ROM Envelopes:** Real-time visual feedback displaying dynamic capacitive EMG bars, peak left/right pitch and peak up/down roll telemetry, and an animated milestone reward system.
* **Clinically-Safe Dynamic Difficulty Adaptation (DDA):** An automated clinical algorithm tracks patient accuracy across a 10-shot rolling window, progressively adjusting target speed and spawn frequency without causing patient fatigue or frustration.
* **Cloud Telemetry & PDF Medical Reports:** High-resolution 20Hz session data is streamed and uploaded to a cloud-based **FastAPI** service on Render, generating downloadable PDF clinical analytics for therapists.

---

## 🌟 Key Features

### 🎮 1. Kinematic Spatial Navigation & Neutral Auto-Calibration (MPU-9250 IMU)
* **Automatic 2-Second Neutral Baseline Calibration:** At the start of each rehabilitation session, the system automatically records the patient's resting hand position for ~2 seconds, computing reference baseline angles (`pitch0`, `roll0`) via circular mean statistical averaging.
* **Relative Angular Displacements ($\Delta\text{Pitch}, \Delta\text{Roll}$):** Movement thresholds operate entirely on angular deltas from the calibrated rest posture ($\Delta\theta = \text{NormalizeAngle}(\theta - \theta_0)$), completely eliminating errors caused by sensor orientation shifts or variations in how the glove is worn:
  * **Horizontal Steering (Pitch Delta):** $\Delta\text{Pitch} > +30^\circ$ (Right), $\Delta\text{Pitch} < -30^\circ$ (Left).
  * **Vertical Steering (Roll Delta):** $\Delta\text{Roll} > +40^\circ$ (Up), $\Delta\text{Roll} < -40^\circ$ (Down).
* **Circular Wraparound Normalization:** Angles are wrapped to $[-180^\circ, +180^\circ]$, ensuring smooth navigation without glitches near the $\pm 180^\circ$ seam.
* **Simultaneous Diagonal Movement:** Pitch and roll are evaluated independently each frame, allowing responsive, normalized diagonal maneuvering.

### 💪 2. Neuromuscular Triggering & Relaxation Cycle (sEMG)
* **High-Precision 12-Bit EMG:** Auto-detects 12-bit ESP32 ADC data ($0 - 4095$ range) with dynamic threshold scaling ($1600$ default).
* **Single-Shot Firing:** Firing requires exceeding the muscle contraction threshold ($\text{EMG} \ge \text{Threshold}$).
* **Mandatory Relaxation Reset:** To prevent sustained spasticity and muscle fatigue, the player **must consciously relax the forearm muscle** below the rest baseline before firing another shot.

### 🏆 3. Animated Trophy Gamification & Clinical Station HUD
* **Active Range of Motion (ROM) Telemetry:** Real-time HUD indicators track peak left/right pitch and peak up/down roll envelopes.
* **Dynamic Capacitive Charge Gauge:** Visualizes real-time muscle contraction status with color-coded feedback (Green = Relaxed/Ready, Red = Contracted/Lockout).
* **5-Tier Animated Trophy Rack:**
  * 🥉 **Bronze Trophy** (10 Pts) — 2 Frames (Metallic gleam shimmer)
  * 🥈 **Silver Trophy** (20 Pts) — 2 Frames (High-contrast silver reflection)
  * 🥇 **Gold Trophy** (30 Pts) — 2 Frames (Warm radiant gold pulse)
  * 💠 **Platinum Trophy** (40 Pts) — 2 Frames (Cyan crystal glint)
  * 💎 **Diamond Trophy** (50 Pts) — 4 Frames (Prismatic multi-stage sparkle)
* **Celebration Sequence:** Milestone popups feature animated flying trophies that scale, play celebration text, and glide smoothly into the persistent HUD rack.

### 🧠 4. Adaptive Difficulty Engine (DDA)
* **10-Shot Rolling Window:** Evaluates live hitting performance.
* **Progressive Challenge:** Achieving $\ge 80\%$ accuracy increases game pace by reducing alien spawn intervals ($5.0\text{s} \rightarrow 4.5\text{s} \rightarrow 4.0\text{s} \rightarrow 3.5\text{s} \rightarrow 3.0\text{s}$) and increasing velocity ($1.0\times \rightarrow 1.8\times$).
* **Clinical Safety Ceilings:** Minimum spawn interval clamped at $3.0\text{s}$ to avoid overwhelming the patient. Successful hits are protected and cannot trigger difficulty drops.

### 📊 5. High-Frequency 20Hz Telemetry & Cloud Reporting
* **Local Logging:** `SessionLogger.cs` logs timestamped kinematic angles, raw EMG amplitude, player coordinates, and hit/miss events at 20Hz to a local `.csv` file.
* **FastAPI Cloud Pipeline:** Upon session completion, the session log is automatically uploaded via multipart HTTP POST to our Render backend (`https://report-maker-re9lay.onrender.com`), generating an official clinical PDF report with recovery metrics.

---

## 🛠️ System Architecture

```mermaid
graph TD
    subgraph Wearable Glove / IoT Hardware
        IMU[MPU-9250 9-DOF IMU<br/>Wrist Pitch & Roll] -->|I2C: SDA=GPIO2, SCL=GPIO15| ESP[ESP32-WROOM-32<br/>Dual-Core 240MHz]
        BAT[Dual 9V Battery Supply<br/>±9V Split Rails] -->|±Vs Power| EMG[Muscle Sensor v3 sEMG<br/>Differential Bioamp]
        BAT -->|Center-Tap GND| ESP
        EMG -->|Analog SIG (0-3.3V Tuned)<br/>ADC1: GPIO34| ESP
        ESP -->|On-Chip DSP: 32-Sample MAV<br/>Full-Wave Rectification + EMA| DSP[12-Bit Envelope Generator]
        DSP -->|Bluetooth Classic SPP<br/>115200 Baud| RFCOMM[RFCOMM Wireless Link]
    end

    subgraph Unity Game Client Re9lay
        RFCOMM -->|Serial / JNI Socket| BIM[BluetoothInputManager.cs]
        BIM -->|Registry BTHENUM Resolution| REG[Windows PnP / COM Resolver]
        BIM -->|Angular Deltas ΔPitch, ΔRoll| PC[playerController.cs]
        BIM -->|12-Bit EMG & Reset State| PC
        PC -->|Performance Feedback| DM[DifficultyManager.cs]
        DM -->|Adaptive Spawn & Velocity| EG[enemyGenerator.cs]
        BIM & PC -->|Active Envelopes & Milestones| HUD[ClinicalStationHUD.cs]
        BIM & PC & DM -->|20Hz Telemetry Stream| SL[SessionLogger.cs]
    end

    subgraph Cloud Medical Analytics
        SL -->|Session CSV Upload| RU[ReportUploader.cs]
        RU -->|HTTP POST| FASTAPI[FastAPI Backend on Render]
        FASTAPI -->|Pandas / Matplotlib / ReportLab| PDF[Medical PDF Report]
    end
```

---

## 🔌 Hardware Wiring & Bill of Materials

### Bill of Materials (BOM)
| Component | Function | Interface | Operating Voltage |
| :--- | :--- | :--- | :--- |
| **ESP32-WROOM-32** | Master IoT Microcontroller | Onboard Bluetooth Classic | 3.3V / 5V USB |
| **MPU-9250** (or MPU-6050) | 9-DOF Inertial Measurement Unit | I2C (SDA, SCL) @ 400kHz | 3.3V |
| **Muscle Sensor v3 sEMG** | Differential Surface Electromyography | Analog (ADC1) | Dual $\pm 9\text{V}$ Battery Supply |
| **2x 9V Batteries & Snaps** | Dual Power Supply with Center-Tap GND | Common Reference Ground | $\pm 9\text{V}$ (Split $\pm 3.5\text{V} - \pm 9\text{V}$) |
| **Power Ground** | System Common Reference Ground | Tied to ESP32 GND | 0V Reference |

### Pinout Table
| Module Pin | Target Pin | Description |
| :--- | :--- | :--- |
| **MPU VCC** | `ESP32 3.3V` | Sensor clean power supply |
| **MPU GND** | `ESP32 GND` | Common Ground |
| **MPU SDA** | **`GPIO 2` (`D2`)** | I2C Serial Data (custom layout) |
| **MPU SCL** | **`GPIO 15` (`D15`)** | I2C Serial Clock (custom layout) |
| **EMG SIG** | **`GPIO 34`** | ADC1 input-only analog channel (12-bit) |
| **EMG GND** | `ESP32 GND` | **Center-tap common ground** (bonds battery GND to ESP32 GND) |
| **EMG +Vs** | `Battery 1 (+)` | $+9\text{V}$ positive supply rail |
| **EMG -Vs** | `Battery 2 (-)` | $-9\text{V}$ negative supply rail |
| **Battery Mid-point** | `ESP32 GND` | Battery 1 (-) connected to Battery 2 (+) $\rightarrow$ Common GND |

> [!IMPORTANT]
> **1. Common Ground is Mandatory:** The center connection point between the two 9V batteries (GND) **must be connected to the ESP32's GND pin**. Without this common ground, the analog signal floats and the reading will be permanently stuck at 4095.  
> **2. Voltage Protection (3.3V Limit):** Because the sensor is powered by $\pm 9\text{V}$, turn the sensor's blue on-board gain potentiometer **counter-clockwise** so that peak voluntary contraction voltages stay under $3.3\text{V}$ to prevent overvoltage saturation.  
> **3. Why GPIO 34 for EMG?** GPIO 34 is part of **ADC1**. In the ESP32, ADC2 pins are shared with the Wi-Fi and Bluetooth radio and become disabled or unstable during wireless transmission. ADC1 pins are completely isolated, ensuring clean, continuous EMG acquisition.

---

## 💻 Software Stack

* **Unity Engine:** Unity 2022.3+ LTS, 2D Physics, Immediate GUI, Android JNI, Windows PnP Registry integration.
* **Firmware:** ESP32 Arduino C++ (`Firmware/Re9lay_ESP32.ino`).
* **Cloud Reporting Backend:** Python 3.11, FastAPI, ReportLab, Matplotlib, Pandas, hosted on Render.
* **Communication Protocols:**
  * **Wireless:** Bluetooth Classic RFCOMM (SPP - Serial Port Profile) at **115,200 baud**.
  * **Wired/Editor:** USB Serial CP210x / CH340 at **115,200 baud**.
  * **Packet Format:** `Pitch,Roll,EMG\n` (e.g. `-2.45,12.80,1850`).

---

## 📁 Project Structure

```text
├── Assets/
│   ├── Editor/
│   │   └── WindowsBuildScript.cs     # 1-click Windows 64-bit standalone build menu
│   ├── Script/
│   │   ├── BluetoothInputManager.cs  # Windows registry resolver & Android JNI Bluetooth
│   │   ├── ClinicalStationHUD.cs     # Real-time ROM envelopes, animated trophy rack & HUD
│   │   ├── EmgCalibrator.cs          # Interactive rest/max contraction calibration
│   │   ├── playerController.cs       # Kinematic steering & single-shot relaxation logic
│   │   ├── DifficultyManager.cs      # 10-shot rolling-window DDA engine
│   │   ├── GUI.cs                    # Rehabilitation menu, device selection & calibration
│   │   ├── SessionLogger.cs          # 20Hz clinical CSV telemetry logger
│   │   ├── ReportUploader.cs         # Cloud sync & medical PDF report fetcher
│   │   ├── enemyGenerator.cs         # Adaptive alien spawner
│   │   ├── alienScript.cs            # Enemy kinematics & collision handling
│   │   ├── laserScript.cs            # Player bullet logic & hit registration
│   │   └── GameSettings.cs           # Global threshold & session configuration
│   ├── Resources/
│   │   ├── bronzetrophy_*.png        # Sliced 64x64 Bronze trophy animation frames
│   │   ├── silvertrophy_*.png        # Sliced 64x64 Silver trophy animation frames
│   │   ├── goldtrophy_*.png          # Sliced 64x64 Gold trophy animation frames
│   │   ├── plattrophy_*.png          # Sliced 64x64 Platinum trophy animation frames
│   │   ├── diamondtrophy_*.png       # Sliced 64x64 Diamond trophy animation frames
│   │   └── main_menu_logo.png        # In-game branding
│   └── Sprites/                      # Spaceships, aliens, lasers, HUD assets
├── Builds/
│   └── Windows/                      # Standalone 64-bit executable (Re9lay.exe)
├── Firmware/
│   ├── Re9lay_ESP32.ino              # Modern ESP32 firmware (I2C SDA=D2, SCL=D15, 115200 baud)
│   └── NeuroPlay_Arduino.ino         # Legacy Arduino Uno firmware (archived reference)
├── PROJECT_REPORT.md                 # Full technical project report & clinical documentation
└── README.md                         # Project documentation
```

---

## 🚀 Getting Started

### 1. Flashing the ESP32 Firmware
1. Open [`Firmware/Re9lay_ESP32.ino`](Firmware/Re9lay_ESP32.ino) in the **Arduino IDE**.
2. Install the **ESP32** board package by Espressif (`Tools` $\rightarrow$ `Board` $\rightarrow$ `Boards Manager...`).
3. Install the required libraries via the Arduino Library Manager:
   * **`MPU9250`** by hideakitai (or compatible MPU9250 library)
   * `Wire` & `BluetoothSerial` (included with ESP32 core)
4. Select your board: **`ESP32 Dev Module`** (or your specific ESP32-WROOM-32 board).
5. Connect your ESP32 via USB, select its COM port, and click **Upload**.

---

### 2. Playing on Windows PC

1. **Power on your ESP32**.
2. Open Windows **Settings** $\rightarrow$ **Bluetooth & devices** $\rightarrow$ **Add device**.
3. Select **`Re9lay-Glove`** and pair it.
4. Launch the game in the **Unity Editor** or run the standalone build (`Builds/Windows/Re9lay.exe`).
5. On the Main Menu, click **🔍 SCAN PAIRED DEVICES**.
6. The game automatically queries the Windows registry and displays:
   $$\mathbf{\bigstar\text{ CONNECT TO Re9lay-Glove (COM9) }\bigstar}$$
7. Click the starred button to connect!

> [!TIP]
> **Troubleshooting *"Port error: The port does not exist"***:
> * Make sure the ESP32 is **powered ON** before connecting. Windows only completes the virtual serial link when the physical device responds to the RFCOMM handshake.
> * If you see duplicate port numbers or connection errors, remove **`Re9lay-Glove`** in Windows Bluetooth Settings and pair it fresh once.

---

### 3. Playing on Android Tablet / Phone

1. Power on your ESP32.
2. In your Android device's **Bluetooth Settings**, pair with **`Re9lay-Glove`**.
3. Install and open the Re9lay APK.
4. Grant the requested **Nearby Devices / Bluetooth permissions**.
5. Tap **🔍 SCAN PAIRED DEVICES**, select **`Re9lay-Glove`**, and begin your session!

---

### 4. Testing with Keyboard Simulation

If you do not have the physical glove connected:
1. In the Unity Inspector on `_NeuroPlayManagers` $\rightarrow$ `BluetoothInputManager`, check **`Use Simulation`**.
2. Controls:
   * **`WASD`** or **Arrow Keys**: Steer spacecraft.
   * **`Spacebar`**: Hold to contract muscle (shoot laser), release to consciously relax (ready next shot).

---

## 🎮 Controls & Clinical Calibration

| Action | Sensor Glove Control | Keyboard Simulation | Clinical Goal |
| :--- | :--- | :--- | :--- |
| **Neutral Calibration** | **Hold hand at rest for ~2s** at session start | Automatic | Establishes zero-strain reference baseline |
| **Move Left / Right** | Wrist Pitch tilt ($\Delta\text{Pitch} < -30^\circ$ / $> +30^\circ$) | `A` / `D` or `←` / `→` | Promotes wrist flexion / extension |
| **Move Up / Down** | Wrist Roll tilt ($\Delta\text{Roll} > +40^\circ$ / $< -40^\circ$) | `W` / `S` or `↑` / `↓` | Promotes forearm pronation / supination |
| **Shoot Laser** | Contract forearm muscle ($\text{EMG} \ge \text{Threshold}$) | Hold `Spacebar` | Target voluntary motor unit recruitment |
| **Reload / Ready** | **Consciously relax forearm muscle** below baseline | Release `Spacebar` | Inhibits spastic co-contraction & hypertonia |

---

## 📈 Clinical Telemetry & Reporting

During every rehabilitation session, `SessionLogger.cs` streams 20 data points per second:
```csv
Timestamp,GameTime,PlayerX,PlayerY,Pitch,Roll,EMG,IsContracted,Score,AlienCount,SpawnInterval,SpeedMultiplier
2026-09-14 14:12:05.102,1.20,0.00,-3.50,-1.07,0.99,1850,False,0,1,5.0,1.0
2026-09-14 14:12:05.152,1.25,0.00,-3.50,-3.58,3.36,1920,False,0,1,5.0,1.0
```

When the session concludes, the log is transmitted to the **Re9lay Cloud API**:
* **Endpoint:** `POST https://report-maker-re9lay.onrender.com/upload-session`
* **Clinical Outputs:** 
  * Patient Accuracy & Target Hit-Rate curves.
  * Active Range of Motion (ROM) in wrist pitch & roll axes.
  * Muscle contraction latency, peak voluntary contraction, and fatigue curves.
  * Downloadable PDF clinical report for medical records and insurance compliance.

---

## 👥 Contributors & Documentation

* **Technical Project Report:** Refer to [`PROJECT_REPORT.md`](PROJECT_REPORT.md) for full engineering architecture and clinical trial methodology.
* **Firmware Migration Guide:** Refer to [`re9lay_esp32_migration_instructions.md`](re9lay_esp32_migration_instructions.md) for hardware setup details.
* **Lead Developers & Researchers:** Re9lay Development Team.

---

<div align="center">
<b>Empowering Motor Recovery Through Play. 🚀</b>
</div>
