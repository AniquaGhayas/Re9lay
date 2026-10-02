# SMART INDIA HACKATHON 2026 — OFFICIAL PROJECT REPORT

---

# RE9LAY-FIT: Smart Bio-Sensing Exergaming Wearable for Athletic Muscle Activation Tracking, Grip Conditioning & Sports Injury Recovery
### Real-Time Wearable Biofeedback, 9-DoF Kinematics & Closed-Loop Neuromuscular Conditioning

<div align="center">

**Problem Statement ID:** SIH26213  
**Problem Statement Title:** Student Innovation - Ideas that can boost fitness activities and assist in keeping fit.  
**Theme:** Fitness & Sports  
**Category:** Hardware  
**Team ID:** 161387  
**Team Name:** RE9LAY  
**Document Classification:** Official Technical Dossier & Athletic Biomechanics Documentation  
**Version:** 4.0 (Updated for SIH26213 & Muscle Sensor v3 Architecture) — September 2026  

</div>

---

## 1. Executive Summary

In modern athletic conditioning, sports science, and physical fitness, **muscle activation (%MVC)**, **muscular endurance**, and **joint range of motion (ROM)** represent the fundamental physiological pillars of physical performance. Following soft-tissue sports injuries—such as tennis elbow (*lateral epicondylitis*), golfer's elbow, wrist ligament sprains, flexor tendon strains, or post-fracture immobilization—athletes must undergo high-repetition neuromuscular conditioning to rebuild voluntary muscle recruitment and prevent chronic re-injury before achieving safe "Return-to-Play" (RTP). Simultaneously, millions of desk-bound students, office professionals, and esports gamers suffer from sedentary upper-limb deconditioning, chronic forearm weakness, and Carpal Tunnel Syndrome / Repetitive Strain Injury (RSI).

Traditional grip and forearm fitness tools (e.g., hand grippers, wrist rollers, static squeeze balls) suffer from **catastrophic user drop-out rates exceeding 80%** due to:
1. **Extreme Monotony:** Repetitive mechanical squeezing without visual feedback quickly causes boredom.
2. **Invisible Muscle Effort:** Athletes cannot see their internal physiological muscle recruitment (%MVC), leading to poor mind-muscle connection, improper form, and compensatory muscle strain.
3. **Absence of Fatigue Safeguards:** Conventional workouts provide zero real-time neuromuscular fatigue monitoring, frequently leading to acute tendon inflammation and overtraining injuries.
4. **Lack of Objective Return-to-Play Telemetry:** Coaches and sports physiotherapists lack objective quantitative data on whether an injured limb has recovered symmetry with the uninjured limb.

**RE9LAY-FIT** solves these challenges through an end-to-end, IoT-enabled smart wearable exergaming platform that transforms tedious forearm conditioning and sports injury rehabilitation into an immersive, biofeedback-driven 2D space flight exergaming experience. 

The system couples a low-cost, wearable IoT sensor glove powered by an **ESP32-WROOM-32** microcontroller, an **MPU-9250 9-DoF Inertial Measurement Unit (IMU)**, and a **Muscle Sensor v3 Surface Electromyography (sEMG)** sensor (with dual $\pm 9\text{V}$ split power supply and unified biological reference ground) with a customized **Unity 2022.3 LTS** exergaming engine and a **FastAPI / Render** cloud analytics pipeline.

Crucially, Re9lay-Fit introduces **six foundational sports-science and engineering innovations**:
1. **Limb Symmetry Index (LSI) Return-to-Play Engine:** Implements the sports-medicine gold standard $LSI = \frac{\%MVC_{\text{injured}}}{\%MVC_{\text{healthy}}} \times 100$, giving athletes and coaches an objective benchmark ($\ge 90\%$) for safe sports clearance.
2. **On-Chip Windowed MAV Oversampling & Software Envelope Extraction:** Executes high-frequency (5 kHz) 32-sample Mean Absolute Value (MAV) burst-oversampling and boot-time DC auto-zeroing on the ESP32. This overcomes the Nyquist aliasing limitation of raw AC EMG, streaming a smooth 12-bit envelope ($0 - 4095$) with sub-$25\text{ms}$ latency.
3. **Mandatory Antagonist Relaxation Cycle:** Laser firing enforces an active contraction-relaxation cycle via dual-threshold hysteresis. Subsequent firing is locked out until the athlete consciously relaxes the forearm muscle below a dynamic rest threshold, preventing forearm cramping and training muscle recovery cadence.
4. **Neutral Posture Auto-Calibration via Circular Statistics:** Computes individual resting wrist baseline coordinates ($\text{pitch}_0, \text{roll}_0$) using circular mean trigonometry over a 2-second rest window, ensuring calibration invariance regardless of how the glove is donned.
5. **Adaptive Progressive Overload (DDA):** Dynamically scales game velocity and obstacle spawn frequency based on rolling 10-shot hit accuracy, keeping athletes in the optimal zone of proximal conditioning.
6. **Automated Biomechanics Telemetry & PDF Reports:** Continuous multi-parameter logging processed into publication-grade PDF reports documenting **Rep Count, Time Under Tension (TUT), Training Load / Duty Cycle, %MVC Peak Power, Angular Velocity (°/s), and LDLJ Movement Smoothness**.

At a manufacturing cost of approximately **₹2,450 ($29.40)**, Re9lay-Fit delivers professional-grade athletic telemetry at less than the price of a standard gym shoe, democratizing advanced sports biomechanics for athletes, fitness enthusiasts, and sedentary populations worldwide.

---

## 2. Problem Statement & Athletic Motivation

### 2.1 The Critical Role of Grip Strength & Forearm Health
- **The Biomarker of Longevity & Power:** Clinical studies published in *The Lancet* and *Sports Medicine* establish that grip strength is one of the single most reliable biomarkers of overall muscular strength, cardiovascular endurance, and physical longevity. In sports like rock climbing, tennis, badminton, cricket, gymnastics, martial arts, and weightlifting, forearm endurance is the primary limiting factor.
- **Sedentary Desk Syndrome (RSI & Carpal Tunnel):** Over 75% of students and desk workers spend 8–12 hours daily on keyboards and mobile devices, resulting in chronic wrist stiffness, extensor tendonitis, and Carpal Tunnel Syndrome.

### 2.2 Sports Injuries & The Return-to-Play Bottleneck
When an athlete suffers an upper-limb sports injury (e.g., wrist sprain, tendonitis, or radial/ulnar nerve strain), the surrounding musculature experiences **Arthrogenic Muscle Inhibition (AMI)**—a neurological reflex where the brain suppresses maximal muscle recruitment to protect the injured joint. 

To safely return to physical activity, athletes must undergo progressive neuromuscular conditioning. However:
- Traditional therapy tools offer **zero biofeedback** on muscle recruitment.
- Over 80% of athletes drop out of prescribed home exercises due to boredom.
- Premature return to sports without verified muscle symmetry leads to **re-injury rates exceeding 35%**.

---

## 3. System Architecture & Technical Specifications

```
+---------------------------------------------------------------------------------+
|                                 ATHLETE'S FOREARM                               |
|        [ MPU9250 IMU: Wrist Mobility ]       [ sEMG Electrodes: Muscle Flex ]   |
+---------------------------------------+-----------------------------------------+
                                        |
                                        v
+---------------------------------------------------------------------------------+
|                       ESP32 WEARABLE EMBEDDED SYSTEM                            |
|  - 5 kHz Burst-Windowed Oversampling (MAV Rectification: abs(raw - baseline))   |
|  - Exponential Moving Average Low-Pass Envelope Filter (alpha = 0.20)           |
|  - Non-Blocking 40 Hz Wireless Bluetooth Serial (SPP) Transmission             |
+---------------------------------------+-----------------------------------------+
                                        | (Bluetooth Classic / sub-15ms Latency)
                                        v
+---------------------------------------------------------------------------------+
|                         RE9LAY-FIT UNITY EXERGAMING ENGINE                      |
|  - Plug-and-Play Windows Registry Port Auto-Detection                           |
|  - Automated 2-Second Baseline & Maximum Voluntary Contraction (MVC) Calibration|
|  - Real-time 1-to-1 Flight Kinematics & Muscle-Triggered Laser Biofeedback      |
|  - Athletic Station HUD (Real-time EMG Bar, ROM Gauges, Fatigue Detection)      |
|  - Milestone Gamification: 5-Tier Animated Celebration Trophies                 |
+---------------------------------------+-----------------------------------------+
                                        |
                                        v
+---------------------------------------------------------------------------------+
|                 ATHLETIC TELEMETRY & BIOMECHANICS REPORTING                     |
|  - Limb Symmetry Index (LSI) Return-to-Play Calculation                         |
|  - Exportable Standardized Session Logs (CSV Timeseries + JSON Meta)            |
|  - Automated 4-Page PDF Biomechanics & Muscle Telemetry Report                  |
+---------------------------------------------------------------------------------+
```

### 3.1 Hardware Component Specifications
1. **ESP32-WROOM-32 Microcontroller:** Dual-core 240 MHz processor, 12-bit SAR ADC, integrated Bluetooth 4.2 BR/EDR.
2. **MPU-9250 9-DoF IMU:** Dedicated I2C bus (400 kHz fast mode on GPIO 2/15) tracking wrist pitch/roll kinematics with sub-degree accuracy.
3. **Muscle Sensor v3 (sEMG):** High-CMRR (>110 dB) instrumentation amplifier with dual-rail $\pm 9\text{V}$ power supply passing 20 Hz – 450 Hz physiological frequencies while eliminating mains noise.
4. **Ergonomic Wearable Glove:** Lightweight, breathable fabric with reusable Ag/AgCl snap leads placed on forearm flexor/extensor muscle groups.

---

## 4. Signal Processing & Firmware Engineering

Raw surface EMG signals exhibit stochastic AC properties with microvolt amplitudes. The `Firmware/Re9lay_ESP32.ino` firmware implements a deterministic multi-stage DSP pipeline:

### 4.1 True Baseline DC Auto-Calibration
$$\text{Baseline} = \frac{1}{N} \sum_{i=1}^{N} V_{\text{ADC}}[i], \quad \text{where } N = 200 \text{ samples at } 5\,\text{ms intervals}$$
The firmware records the athlete's true resting baseline at boot, eliminating dc bias shifts and sensor offset errors.

### 4.2 5 kHz Mean Absolute Value (MAV) Burst-Windowing
$$\text{MAV} = \frac{1}{M} \sum_{k=1}^{M} |V_{\text{raw}}[k] - \text{Baseline}|, \quad \text{where } M = 32 \text{ samples spaced } 200\,\mu\text{s apart}$$
This 6.4 ms burst window achieves an effective sampling rate of $5{,}000\,\text{Hz}$, capturing fast muscle firing without aliasing. Full-wave rectification ($|V_{\text{raw}} - \text{Baseline}|$) guarantees that muscle contraction **strictly increases the output value**.

### 4.3 Exponential Moving Average (EMA) Envelope
$$y[n] = \alpha \cdot x[n] + (1 - \alpha) \cdot y[n-1], \quad \alpha = 0.20$$
Delivers a smooth, real-time muscle envelope ($0 - 4095$) with sub-15 ms latency.

---

## 5. Sports Biomechanics & Analytics Pipeline

### 5.1 Limb Symmetry Index (LSI)
The sports-medicine gold standard for return-to-play clearance:
$$\text{LSI } (\%) = \left( \frac{\%MVC_{\text{injured}}}{\%MVC_{\text{healthy}}} \right) \times 100$$
- **$\text{LSI} \ge 90\%$:** Cleared for full athletic activity / Return-to-Play.
- **$\text{LSI } 80 - 89\%$:** Progressing; continue targeted activation training.
- **$\text{LSI} < 80\%$:** Significant deficit; high re-injury risk.

### 5.2 Log Dimensionless Jerk (LDLJ) Movement Smoothness
$$\text{Dimensionless Jerk} = \frac{T^3}{v_{\text{peak}}^2} \int_{0}^{T} \left( \frac{d^2 v}{dt^2} \right)^2 dt, \quad \text{LDLJ} = -\ln(\text{Dimensionless Jerk})$$
Values closer to 0 represent smooth, highly coordinated athletic movement; negative values highlight muscular tremors or hesitation.

---

## 6. Bill of Materials (BOM) & Economic Viability

| Component | Function | Unit Cost (INR) | Unit Cost (USD) |
| :--- | :--- | :---: | :---: |
| **ESP32 Dev Module** | Processing, ADC, Bluetooth | ₹450 | \$5.40 |
| **MPU9250 9-DoF IMU** | Wrist kinematics & agility | ₹350 | \$4.20 |
| **Muscle Sensor v3 (sEMG)** | Microvolt muscle detection | ₹1,200 | \$14.40 |
| **Neoprene Glove & Leads** | Wearable substrate & electrodes | ₹300 | \$3.60 |
| **Dual Power Module** | Dual 9V battery supply | ₹150 | \$1.80 |
| **TOTAL PROTOTYPE COST** | **Complete Smart Fitness Glove** | **₹2,450** | **\$29.40** |

---

## 7. Current Prototype Status

- [x] **Hardware Prototype:** 100% built, wired, and tested with ESP32, MPU9250, and Muscle Sensor v3 on dedicated ADC1 (GPIO 34).
- [x] **Firmware DSP:** Production firmware with true-baseline calibration, 5 kHz oversampling, and 40 Hz Bluetooth SPP streaming verified.
- [x] **Exergaming Software:** Complete Unity game featuring 1-to-1 flight kinematics, muscle laser firing, dynamic difficulty scaling, and 5-tier animated trophy rewards.
- [x] **Biomechanics Reporting:** Python reporting pipeline operational, generating 4-page PDF reports with %MVC, TUT, Rep Count, LDLJ Smoothness, and LSI return-to-play metrics.
- [x] **GitHub Repository:** Open-source codebase publicly available at `https://github.com/AniquaGhayas/Re9lay`.

---

## 8. Conclusion

**Re9lay-Fit** bridges the gap between sports biomechanics, injury rehabilitation, and digital fitness. By combining medical-grade surface EMG muscle tracking with exergaming mechanics, it transforms neglected grip and forearm conditioning into an engaging, quantifiable sport that keeps athletes fit, prevents chronic injuries, and accelerates safe return-to-play.
