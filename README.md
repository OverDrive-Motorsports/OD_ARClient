<div align="center">

# OverDrive – VR Experience for Motorsport

**A Meta Quest VR application that reinvents the motorsport viewing experience with total immersion, interactivity, and customization.**

</div>

<br>

## Project Vision

Today, motorsport viewing remains mostly linear and passive, despite the wealth of available data. OverDrive transforms the spectator into an active participant by integrating telemetry and race information directly into a virtual environment.

**Key Objectives:**
- Make motorsport **explorable, understandable, and alive**.
- Combine **immersion, comprehension, and customization**.
- Provide an innovative alternative to traditional broadcasts.

<br>

## Key Features

| Category               | Details                                                                                     |
|-------------------------|---------------------------------------------------------------------------------------------|
| **VR Experience**       | Dedicated environment, 3D mini-circuit, cars updated in near real-time.                    |
| **Real-Time Data**      | Standings, gaps, strategies, key events (stops, penalties, incidents).                      |
| **Customization**       | Panel movement and resizing, saving user configurations.                                     |
| **Mobile Application**  | Simplified/expert mode, notifications, parallel tracking with VR.                           |

<br>

## Target Personas

OverDrive is designed for three main user profiles:
- **The analytical fan**: In-depth data analysis.
- **The curious viewer**: Simple understanding of race dynamics.
- **The tech enthusiast**: Seeking immersion and innovation.

The application adapts information density and hierarchy according to the user profile.

<br>

## Technical Architecture

**Recommended Stack:**
- **VR**: Unity (native Meta Quest)
- **Backend**: Go or Node.js/TypeScript
- **Database**: PostgreSQL + Redis
- **Mobile**: Flutter
- **Communication**: WebSocket

**Features:**
- Scalable and modular.
- Real-time oriented.
- Compatible with progressive scaling.

<br>

## Contributors

Project developed by the **OverDrive Team – Epitech Paris (2026)**.

| Name            |
|-----------------|
| Anthony El Achkar |
| Clément-Alexis Fournier |
| Mariia Semenchenko |
| Batien Leroux |
| Corto Morrow |

<br>

## Installation & Usage

### Prerequisites
- Unity Editor
- Unity Hub to manage Unity versions
- Meta Quest Build Support (including Meta SDKs)
- Oculus XR Plugin support for Meta Quest

### Clone the repository
```bash
git clone https://github.com/OverDrive-Motorsports/OD_ARClient.git
cd OD_ARClient/Overdrive
```

### Open the project
1. Launch Unity Hub.
2. Click **Add** or **Open**.
3. Select the `Overdrive` folder.
4. Open the project with Unity **6000.3.9f1**.

### Build for Meta Quest
1. In Unity, open `File > Build Settings`.
2. Select **Meta Quest** as the target platform.
3. Click **Switch Platform** if needed.
4. Make sure the XR plugin and Quest/Oculus settings are configured.
5. Click **Build** or **Build and Run** to generate the APK.

### Notes
- The project is in development and some scenes or features may still change.
- If you work on a branch, use clear commit messages and push your changes to GitHub once tested.

<br>

## License

To be determined.
