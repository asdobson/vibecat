# VibeCat 🐱

[![Build](https://github.com/asdobson/vibecat/actions/workflows/build.yml/badge.svg)](https://github.com/asdobson/vibecat/actions/workflows/build.yml)
[![Release](https://img.shields.io/github/v/release/asdobson/vibecat)](https://github.com/asdobson/vibecat/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/asdobson/vibecat/total)](https://github.com/asdobson/vibecat/releases)
[![License](https://img.shields.io/github/license/asdobson/vibecat)](LICENSE)

An animated desktop cat that vibes to your music - a transparent, always-on-top companion that syncs with Spotify.

## 🚀 Quick Start

### 1. Download & Run
**[⬇️ Download VibeCat.exe](https://github.com/asdobson/vibecat/releases/latest/download/VibeCat.exe)** (150MB, portable)

### 2. First Launch
- Download the exe and run it (no installation needed)
- Windows SmartScreen may appear - click "More info" → "Run anyway"
- The cat appears on your desktop, dancing at 115 BPM

### 3. Connect Spotify (Optional)
- Double-click the cat to show controls
- Click Settings → Connect Spotify
- Log in when prompted
- The cat now syncs to your music's BPM! 🎵

## ✨ Features

### Core Features
- **Animated Cat** - Smooth 30 FPS animation that lives on your desktop
- **Spotify BPM Sync** - Cat automatically dances to the beat of your music
- **Click-Through Mode** - Make the cat non-interactive (Ctrl+Alt+T)
- **Always On Top** - Stays visible above all windows
- **Portable** - Single exe file, no installation required

### Window Modes
- **Cat Mode** (default) - Just the cat, draggable
- **UI Mode** (double-click) - Shows controls and settings
- **Click-Through Mode** (Ctrl+Alt+T) - Cat becomes non-interactive

### Customization
- **Window Opacity** - Make the cat more or less transparent
- **Edge Snapping** - Cat snaps to screen edges when dragged
- **Horizontal Flipping** - Cat faces the right direction
- **Manual BPM Control** - Set custom speed (60-180 BPM)
- **Resizable** - Adjust size while maintaining aspect ratio

## 📖 How to Use

### Basic Controls
| Action | Control |
|--------|---------|
| Show/Hide UI | Double-click the cat |
| Move window | Click and drag |
| Toggle click-through | Ctrl+Alt+T or system tray |
| Resize | Drag corner grip (UI mode) |
| Close | Click X button (UI mode) |

### UI Mode Controls
- **Settings** - Configure opacity, snapping, BPM
- **Hotkeys** - View all keyboard shortcuts
- **Minimize/Close** - Standard window controls
- **Resize Grip** - Bottom-right corner

### Click-Through Mode
When enabled, the cat becomes purely decorative:
- Cannot be clicked or dragged
- Mouse clicks pass through to windows below
- Toggle via system tray or Ctrl+Alt+T
- Icon changes in system tray (filled = interactive, outline = click-through)

## 🎵 Spotify Integration

### Setup
1. Open Settings (double-click cat → Settings button)
2. Click "Connect Spotify"
3. Log in with your Spotify account
4. Grant permission for "user-read-playback-state"
5. Return to VibeCat - you're connected!

### How It Works
- Monitors your currently playing track
- Fetches BPM data for each song
- Automatically adjusts cat animation speed
- Falls back to manual BPM when disconnected

### Privacy
- Only reads current playback state
- No data is stored except your refresh token (encrypted locally)
- Disconnect anytime via Settings

## ⌨️ Keyboard Shortcuts

### Global Hotkeys (work anytime)
- **Ctrl+Alt+T** - Toggle click-through mode

### Window Hotkeys (when VibeCat has focus)
- **Escape** - Hide UI panels
- **Alt+F4** - Close application

### While Dragging
- **Hold Alt** - Disable edge snapping temporarily

## 🔧 Troubleshooting

### Windows SmartScreen Warning
This is normal for new software. The exe is safe but not code-signed.
- Click "More info"
- Click "Run anyway"

### Spotify Not Connecting
- Ensure you're logged into Spotify in your browser
- Try disconnecting and reconnecting
- Check if Spotify is playing music
- Free accounts work, but need active playback

### Cat Not Animating
- Check if BPM is set too low (minimum 60)
- Try resetting to default (115 BPM)
- Restart the application

### Can't Click the Cat
- You're in click-through mode
- Check system tray icon (outline = click-through)
- Press Ctrl+Alt+T to toggle
- Right-click system tray icon → "Toggle Click-Through"

## 💻 System Requirements

### For Users
- **OS**: Windows 10/11 (64-bit)

### For Developers
- **OS**: Windows 10/11
- **.NET**: 8.0 SDK
- **IDE**: Visual Studio 2022 or VS Code
- **Build**: Run `.\build.ps1`

## 🛠️ Development

### Building from Source
```powershell
# Clone the repository
git clone https://github.com/asdobson/vibecat.git
cd vibecat

# Build the project
.\build.ps1

# Or create portable exe
.\publish.ps1
```

### Project Structure
```
VibeCat/
├── VibeCat.sln                    # Solution file
├── build.ps1                       # Build script
├── publish.ps1                     # Portable exe creator
└── VibeCat/
    ├── MainWindow.xaml[.cs]       # Main application logic
    ├── Controls/                  # Custom controls
    ├── Services/                  # Spotify, Settings, BPM
    ├── Views/                     # Settings & Hotkeys panels
    └── Resources/
        └── frames/               # 330 animation frames
```

### Technical Details
- **Framework**: WPF on .NET 8
- **Animation**: 330 PNG frames @ 30 FPS
- **Transparency**: Chromakey removal (#1bba14)
- **Display Size**: 920x690px (4:3 aspect)
- **BPM Range**: 60-180 (default 115)
- **Dependencies**: SpotifyAPI.Web, HtmlAgilityPack, NotifyIcon.Wpf

## 📄 License

MIT License - See [LICENSE](LICENSE) file for details.

## 🤝 Contributing

Contributions welcome! Feel free to submit issues and pull requests.

### Acknowledgments
- Original cat animation source: [TBD]
- Spotify Web API for playback integration
- SongBPM.com for tempo data

---

**Made with ❤️ for desktop cat enthusiasts**