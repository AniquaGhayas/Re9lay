# NeuroPlay 2.0 --- Gamified IoT Rehabilitation System

## 1. Project Overview

**NeuroPlay 2.0** is an Android-based rehabilitation game that combines
a wearable EMG/motion-sensing glove with a Unity space-shooter game. The
system converts muscle activity into game actions, making repetitive
rehabilitation exercises more engaging and measurable.

The current hackathon version focuses on a **single-level space
shooter**:

-   Muscle contraction → spacecraft shoots.
-   Muscle relaxation → spacecraft stops shooting.
-   Each successful target hit → points.
-   Every **10--15 points**, game speed increases gradually.
-   Difficulty increases conservatively to remain suitable for
    rehabilitation.
-   Session data is recorded as CSV and processed using Python to
    generate a progress report.

The project builds on the existing **NeuroPlay** system, which used an
MPU9250, MyoWare EMG sensor, Arduino Uno, HC-05 Bluetooth and an MIT App
Inventor game.

------------------------------------------------------------------------

## 2. Problem & Proposed Solution

### Problem

Traditional rehabilitation exercises can be repetitive and difficult to
sustain. Patients and therapists also need objective measurements of
movement and performance over multiple sessions.

### Solution

NeuroPlay 2.0 turns rehabilitation movements into an interactive game
while collecting quantitative performance data.

**Wearable sensors → Arduino → Bluetooth → Unity Android Game → CSV →
Python Analytics → Progress Report**

The system provides:

-   Real-time muscle-controlled gameplay
-   Gamified rehabilitation
-   Adaptive game difficulty
-   Session-level performance tracking
-   Quantitative movement metrics
-   Future camera-based movement tracking using MediaPipe

> **Important:** NeuroPlay is intended as a rehabilitation-support and
> motivation system, not as a diagnostic or clinical treatment device.
> Difficulty and exercise limits should be configurable and validated
> with rehabilitation professionals.

------------------------------------------------------------------------

# 3. Existing System

## NeuroPlay --- Previous Architecture

### Hardware

-   Arduino Uno
-   MPU9250 IMU
-   MyoWare EMG sensor
-   HC-05 Bluetooth module
-   Wearable strap/glove
-   Android smartphone

### Software

-   Arduino IDE
-   MIT App Inventor
-   Bluetooth Terminal / Serial Monitor

### Previous Data Flow

``` text
Hand Movement / Muscle Contraction
              ↓
       MPU9250 + MyoWare
              ↓
          Arduino Uno
              ↓
          HC-05 Bluetooth
              ↓
       MIT App Inventor Game
              ↓
          Game Action
```

The previous system demonstrated the feasibility of converting hand
movement and muscle activity into wireless game controls.

------------------------------------------------------------------------

# 4. NeuroPlay 2.0 Architecture

## High-Level Architecture

``` text
┌───────────────────────────────┐
│       Wearable Glove          │
│                               │
│  MyoWare EMG + MPU9250        │
└───────────────┬───────────────┘
                │ Sensor Data
                ↓
┌───────────────────────────────┐
│          Arduino Uno          │
│                               │
│ • Read sensors                │
│ • Filter/normalize EMG        │
│ • Detect contraction          │
│ • Package sensor data         │
└───────────────┬───────────────┘
                │ Serial Data
                ↓
┌───────────────────────────────┐
│        HC-05 Bluetooth         │
└───────────────┬───────────────┘
                │ Wireless Input
                ↓
┌───────────────────────────────┐
│       Unity Android Game       │
│                               │
│ Bluetooth Input Manager       │
│        ↓                      │
│ Input Processing              │
│        ↓                      │
│ Game Controller               │
│        ↓                      │
│ Space Shooter                 │
│        ↓                      │
│ Metrics Logger                │
└───────────────┬───────────────┘
                │ Session CSV
                ↓
┌───────────────────────────────┐
│       Python Analytics         │
│                               │
│ • Distance                    │
│ • Duration                    │
│ • Speed                       │
│ • Acceleration                │
│ • Jerk                        │
│ • Reaction time               │
│ • Path straightness           │
│ • Peak speed                  │
│ • Score                       │
└───────────────┬───────────────┘
                ↓
┌───────────────────────────────┐
│       Progress Report          │
│                               │
│ Session summary + trends       │
│ + performance metrics          │
└───────────────────────────────┘
```

------------------------------------------------------------------------

# 5. Hackathon MVP

The first version should remain deliberately simple.

## Game Requirements

### Gameplay

-   One playable level.
-   Space-shooter environment.
-   Player controls a spacecraft.
-   Targets/obstacles move toward or across the player.
-   Muscle contraction triggers shooting.
-   Muscle relaxation stops shooting.
-   Successful hits increase score.
-   No unnecessary levels, menus or complex mechanics.

### Scoring

Recommended initial configuration:

  Event                         Score
  -------------------- --------------
  Target hit                       +1
  Miss                              0
  Collision              Configurable
  Session completion         Optional

The score threshold for difficulty adjustment should be configurable
rather than hard-coded.

### Adaptive Speed

Every **10--15 points**, increase the game speed slightly.

Example:

``` text
0–14 points    → Speed 1.0x
15–29 points   → Speed 1.1x
30–44 points   → Speed 1.2x
45–59 points   → Speed 1.3x
60+ points     → Speed 1.4x maximum
```

These values are starting parameters only. They should be validated
through testing and adjusted according to the intended rehabilitation
difficulty.

### Rehabilitation Safety Principle

Difficulty should increase gradually, not aggressively.

The game should support:

-   Maximum speed limit
-   Configurable acceleration
-   Configurable session duration
-   Pause button
-   Stop/exit control
-   Adjustable EMG activation threshold
-   Optional rest intervals
-   Therapist/researcher configuration

------------------------------------------------------------------------

# 6. Unity Game Architecture

## Recommended Unity Structure

``` text
Assets/
├── Scenes/
│   └── MainGame.unity
│
├── Scripts/
│   ├── Bluetooth/
│   │   └── BluetoothInputManager.cs
│   ├── Player/
│   │   └── PlayerController.cs
│   ├── Gameplay/
│   │   ├── GameManager.cs
│   │   ├── TargetController.cs
│   │   ├── ProjectileController.cs
│   │   └── DifficultyManager.cs
│   ├── Data/
│   │   └── SessionLogger.cs
│   └── UI/
│       └── GameUI.cs
│
├── Prefabs/
│   ├── Player
│   ├── Target
│   └── Projectile
│
├── Audio/
├── Materials/
├── Sprites/
└── UI/
```

## Core Unity Components

### BluetoothInputManager

Responsible for:

-   Bluetooth connection
-   Receiving serial sensor data
-   Parsing EMG values
-   Detecting contraction/relaxation
-   Exposing input events to the game

Example logical states:

``` text
EMG < threshold → RELAXED
EMG ≥ threshold → CONTRACTED
```

A calibration phase should be used to determine an appropriate threshold
rather than assuming one fixed EMG value for every patient.

### PlayerController

Responsible for:

-   Player movement
-   Shooting
-   Movement boundaries
-   Input response

### DifficultyManager

Responsible for:

-   Tracking score
-   Increasing speed after configured score intervals
-   Applying maximum speed limits
-   Maintaining consistent difficulty progression

### SessionLogger

Records gameplay and sensor data at timestamps.

### GameManager

Controls:

-   Game start
-   Game state
-   Score
-   Session completion
-   Pause/resume
-   Game over
-   CSV export

------------------------------------------------------------------------

# 7. Bluetooth Input

## Existing Communication

The existing system uses:

**Arduino Uno → HC-05 → Android**

The Arduino sends sensor readings through serial communication.

Recommended logical packet:

``` text
timestamp,x,y,emg,score
```

Example:

``` text
1250,0.42,0.68,731,4
```

Where:

-   `timestamp` = elapsed time/session timestamp
-   `x` = movement position/value
-   `y` = movement position/value
-   `emg` = EMG sensor value
-   `score` = current game score

The exact packet format should be standardized between Arduino firmware
and Unity.

------------------------------------------------------------------------

# 8. CSV Data Specification

The Unity game should generate one CSV file per session.

## Required Fields

  Field       Description
  ----------- -------------------------
  timestamp   Time of recorded sample
  x_axis      Player/movement X value
  y_axis      Player/movement Y value
  emf_value   EMG/EMF sensor reading
  score       Score at that timestamp

Recommended filename:

``` text
session_YYYYMMDD_HHMMSS.csv
```

Example:

``` text
session_20260901_210000.csv
```

## Optional Future Fields

``` text
target_id
target_hit
reaction_time
game_speed
contraction_state
```

These can make later analysis significantly easier.

------------------------------------------------------------------------

# 9. Performance Metrics

Python should calculate session-level rehabilitation/gameplay metrics
from the raw CSV.

## Core Metrics

### Duration

Total time between first and last valid timestamp.

### Distance

Cumulative movement distance:

``` text
distance = Σ √((x₂-x₁)² + (y₂-y₁)²)
```

### Speed

Movement distance divided by time.

### Peak Speed

Maximum calculated instantaneous speed.

### Acceleration

Rate of change of speed.

### Jerk

Rate of change of acceleration.

Jerk can help identify sudden or unstable movements.

### Reaction Time

Time between target/event appearance and the corresponding player
response.

### Path Straightness

A measure comparing direct displacement with total travelled distance:

``` text
straightness = direct_distance / travelled_distance
```

Values closer to 1 indicate a straighter path.

### Score

Total number of successful target hits.

### Score Rate

``` text
score_rate = score / session_duration
```

### EMG Activity

Useful statistics may include:

-   Mean EMG
-   Maximum EMG
-   Contraction count
-   Contraction duration
-   Relaxation duration

------------------------------------------------------------------------

# 10. Python Analytics Pipeline

``` text
Unity Session CSV
        ↓
Load with pandas
        ↓
Clean missing/invalid samples
        ↓
Sort by timestamp
        ↓
Calculate movement metrics
        ↓
Calculate EMG metrics
        ↓
Calculate gameplay metrics
        ↓
Generate visualizations
        ↓
Generate session report
```

## Recommended Python Stack

-   Python
-   pandas
-   NumPy
-   SciPy
-   matplotlib
-   Optional: ReportLab for PDF reports

## Report Sections

Each session report should contain:

1.  Session ID
2.  Session duration
3.  Total score
4.  Total movement distance
5.  Average speed
6.  Peak speed
7.  Average acceleration
8.  Jerk statistics
9.  Reaction time
10. Path straightness
11. EMG activity summary
12. Trajectory visualization
13. Score progression
14. Comparison with previous sessions, when available

------------------------------------------------------------------------

# 11. Progress Report Design

The report should prioritize **progress over raw data**.

Example:

``` text
NEUROPLAY SESSION REPORT

Session: #05
Duration: 8 min 42 sec
Score: 67
Average Speed: 0.42 units/s
Peak Speed: 0.91 units/s
Path Straightness: 0.82
Average Reaction Time: 1.24 sec

EMG Activity
Contractions: 54
Average EMG: 612
Peak EMG: 924

Performance
Score        ███████████████
Movement     ████████████
Straightness ██████████████

Compared with Previous Session:
Score             ↑ 18%
Reaction Time     ↓ 12%
Path Straightness ↑ 7%
```

For actual rehabilitation use, trends should be interpreted cautiously
and ideally reviewed by a qualified professional.

------------------------------------------------------------------------

# 12. Game Controls Customization

The system should provide a settings/configuration layer for:

-   EMG activation threshold
-   Sensitivity
-   Movement sensitivity
-   Game speed
-   Maximum speed
-   Difficulty increment
-   Points required for speed increase
-   Session duration
-   Sound volume
-   Vibration/haptic feedback, if supported
-   Control mode

Example:

``` text
Settings
├── EMG Sensitivity
├── Movement Sensitivity
├── Difficulty
├── Maximum Speed
├── Session Duration
├── Sound
└── Calibration
```

------------------------------------------------------------------------

# 13. Audio & Feedback

The game should provide clear feedback without overwhelming the user.

### Sound Effects

-   Target hit
-   Shooting
-   Score increase
-   Difficulty increase
-   Session completion

### Optional Feedback

-   Visual contraction indicator
-   Screen flash on successful hit
-   Score animation
-   Haptic feedback where supported

Audio should be independently controllable and optional.

------------------------------------------------------------------------

# 14. MediaPipe --- Next Phase

The next phase will add camera-based movement tracking using
**MediaPipe**.

## Proposed Flow

``` text
Android Camera
      ↓
MediaPipe Hand/Landmark Tracking
      ↓
Hand/Arm Movement Detection
      ↓
Movement Classification
      ↓
Game Action
      ↓
Score + Analytics
```

Potential movements:

-   Hand raise
-   Hand lower
-   Wrist movement
-   Finger movement
-   Arm extension
-   Targeted directional movement

Each correctly detected movement can trigger an in-game action and award
points.

This allows NeuroPlay to evolve from a primarily EMG-controlled game
into a **multi-modal rehabilitation platform** combining muscle activity
and visual movement tracking.

------------------------------------------------------------------------

# 15. Technology Stack

  Layer                      Technology
  -------------------------- ----------------------
  Game Engine                Unity
  Game Language              C#
  Mobile Platform            Android
  Existing Microcontroller   Arduino Uno
  EMG                        MyoWare
  IMU                        MPU9250
  Bluetooth                  HC-05
  Firmware                   Arduino IDE / C++
  Data Format                CSV
  Analytics                  Python
  Data Processing            pandas, NumPy, SciPy
  Visualization              matplotlib
  Report Generation          ReportLab / HTML
  Future Pose Tracking       MediaPipe
  Version Control            Git + GitHub

------------------------------------------------------------------------

# 16. Functional Requirements

## Hardware

-   [ ] Read EMG data.
-   [ ] Read movement data.
-   [ ] Process sensor readings.
-   [ ] Establish Bluetooth connection.
-   [ ] Transmit sensor data reliably.

## Unity

-   [ ] Android-compatible game.
-   [ ] Single playable level.
-   [ ] Bluetooth input.
-   [ ] Muscle contraction detection.
-   [ ] Shooting mechanism.
-   [ ] Target spawning.
-   [ ] Collision detection.
-   [ ] Score system.
-   [ ] Adaptive speed.
-   [ ] Speed ceiling.
-   [ ] Pause/resume.
-   [ ] Settings/customization.
-   [ ] Sound effects.
-   [ ] Session CSV generation.

## Analytics

-   [ ] Read session CSV.
-   [ ] Clean sensor data.
-   [ ] Calculate performance metrics.
-   [ ] Plot trajectory.
-   [ ] Plot score progression.
-   [ ] Generate session report.
-   [ ] Compare sessions when historical data exists.

------------------------------------------------------------------------

# 17. Non-Functional Requirements

### Performance

-   Responsive input with minimal latency.
-   Stable frame rate on target Android devices.
-   Efficient Bluetooth data handling.
-   No unnecessary background processing.

### Reliability

-   Handle Bluetooth disconnection.
-   Handle malformed sensor packets.
-   Prevent corrupted CSV files.
-   Save session data safely.
-   Allow graceful game exit.

### Usability

-   Large, readable UI.
-   Simple controls.
-   Clear visual feedback.
-   Minimal menus.
-   Adjustable difficulty.
-   Calibration before gameplay.

### Safety

-   Configurable difficulty limits.
-   Gradual speed progression.
-   Pause/stop controls.
-   Avoid forcing movement.
-   Session duration limits.
-   Human/clinical validation before real rehabilitation deployment.

------------------------------------------------------------------------

# 18. Design Principles

## Rehabilitation First

The game is designed around the patient's movement capability, not
conventional gaming difficulty.

## Simple Interaction

The player should understand the core loop immediately:

``` text
Move / Contract → Shoot → Hit Target → Score → Progress
```

## Gradual Progression

Difficulty should increase slowly and remain bounded.

## Measurable Progress

Every session should generate structured data that can show changes over
time.

## Accessible UI

Use:

-   Large buttons
-   High contrast
-   Minimal text
-   Clear status indicators
-   Simple feedback

------------------------------------------------------------------------

# 19. Development Phases

## Phase 1 --- Existing System Integration

-   Understand previous C# implementation.
-   Reuse suitable gameplay logic/assets.
-   Establish Arduino → HC-05 → Android communication.
-   Verify EMG input.

## Phase 2 --- Unity MVP

-   Build one-level space shooter.
-   Implement shooting from EMG contraction.
-   Implement targets and scoring.
-   Implement adaptive speed.
-   Add sound effects and UI.

## Phase 3 --- Data Collection

-   Implement timestamped CSV logging.
-   Validate sensor/game synchronization.
-   Test data integrity.

## Phase 4 --- Python Analytics

-   Build CSV processing pipeline.
-   Calculate metrics.
-   Generate trajectory and performance graphs.
-   Generate session reports.

## Phase 5 --- Testing & Calibration

-   Test different EMG thresholds.
-   Test Bluetooth latency.
-   Tune game speed.
-   Test usability.
-   Validate the game with controlled user trials.

## Phase 6 --- MediaPipe Expansion

-   Add camera-based hand/arm tracking.
-   Detect predefined movements.
-   Map movements to gameplay.
-   Add movement-based scoring.
-   Combine EMG + vision data.

------------------------------------------------------------------------

# 20. Target MVP Architecture

``` text
                  NEUROPLAY 2.0

        ┌──────────────────────────┐
        │       WEARABLE GLOVE     │
        │                          │
        │    EMG + Motion Sensor   │
        └────────────┬─────────────┘
                     ↓
        ┌──────────────────────────┐
        │       ARDUINO UNO        │
        │ Sensor Processing        │
        └────────────┬─────────────┘
                     ↓
        ┌──────────────────────────┐
        │       HC-05 BLUETOOTH    │
        └────────────┬─────────────┘
                     ↓
        ┌──────────────────────────┐
        │       UNITY ANDROID      │
        │                          │
        │ Input → Player → Shoot   │
        │           ↓              │
        │        Targets           │
        │           ↓              │
        │         Score             │
        │           ↓              │
        │    Difficulty Manager    │
        │           ↓              │
        │      Session Logger      │
        └────────────┬─────────────┘
                     ↓
                Session CSV
                     ↓
        ┌──────────────────────────┐
        │      PYTHON ANALYTICS    │
        │                          │
        │ Metrics + Graphs + Trends│
        └────────────┬─────────────┘
                     ↓
              Progress Report
```

------------------------------------------------------------------------

# 21. Success Criteria

The hackathon MVP is successful when:

1.  The wearable reliably sends sensor input to Android.
2.  Unity receives Bluetooth input with acceptable latency.
3.  Muscle contraction reliably triggers shooting.
4.  Targets can be hit and scored.
5.  Game speed increases gradually every configured 10--15 points.
6.  A maximum rehabilitation-appropriate speed can be enforced.
7.  Every session generates a valid CSV.
8.  Python converts the CSV into a meaningful progress report.
9.  Controls, sensitivity and sound can be customized.
10. The architecture can later accommodate MediaPipe movement tracking.

------------------------------------------------------------------------

# 22. Final Vision

**NeuroPlay 2.0** transforms rehabilitation from a repetitive exercise
into a measurable interactive experience.

The immediate goal is a reliable EMG-controlled Unity game. The larger
platform can combine:

**EMG + Motion Sensors + Bluetooth + Unity + Computer Vision +
Analytics**

to create personalized, gamified rehabilitation sessions where patient
movement becomes both the **input to the game** and the **data used to
measure progress**.
