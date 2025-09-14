# VibeCat 🐱

An animated desktop cat overlay for Windows - a transparent, always-on-top companion that lives on your desktop.

## Features

- **Animated Cat**: Smooth 30 FPS animation using pre-rendered frames
- **Transparent Overlay**: Click-through transparent window with chromakey background removal
- **UI Mode Toggle**: Double-click to toggle between cat-only and UI control modes
- **Edge Snapping**: Automatically snaps to screen edges when dragging (hold Alt to disable)
- **Settings Panel**: Adjust window opacity and snapping behavior
- **Hotkeys Menu**: Built-in documentation for keyboard shortcuts
- **Resizable**: Maintain aspect ratio while resizing (UI mode)
- **Always On Top**: Stays above all other windows

## Prerequisites

- Windows 10/11
- .NET 8 SDK

## Building the Project

### Quick Build (Windows)
.\build.ps1

### Manual Build
```powershell
# Install .NET 8 SDK if needed
winget install Microsoft.DotNet.SDK.8

# Build the project
.\build.ps1

# Run the application
VibeCat\bin\Release\net8.0-windows\VibeCat.exe
```

## How to Use

### Controls
- **Double-click**: Toggle between Cat Mode and UI Mode
- **Single-click + Drag**: Move the window
- **Hold Alt while dragging**: Disable edge snapping temporarily
- **UI Mode Controls**:
  - Settings button: Open settings panel
  - Hotkeys button: View available keyboard shortcuts
  - Minimize/Close buttons: Standard window controls
  - Resize grip: Resize window (maintains aspect ratio)

### Settings
- **Window Opacity**: Adjust transparency of the cat animation
- **Edge Snapping**: Toggle automatic snapping to screen edges
- **Snap Distance**: Configure how close to edges before snapping occurs

## Project Structure
```
VibeCat/
├── VibeCat.sln                    # Solution file
├── build.ps1                       # Build script
├── CLAUDE.md                       # Development guidelines
└── VibeCat/
    ├── VibeCat.csproj             # Project file
    ├── App.xaml[.cs]              # Application entry
    ├── MainWindow.xaml[.cs]       # Main window logic
    ├── Controls/                  # Custom controls
    │   ├── CatAnimationView       # Cat animation display
    │   ├── CustomTitleBar         # Custom title bar
    │   └── ResizeGrip            # Resize handle
    ├── Views/                     # UI panels
    │   ├── SettingsPanel         # Settings interface
    │   └── HotkeysPanel          # Hotkeys documentation
    └── Resources/
        └── frames/               # Animation frames
            └── frame_0001.png    # to frame_0329.png
```

## Technical Details

- **Framework**: WPF (.NET 8)
- **Animation**: 330 pre-extracted PNG frames @ 30 FPS
- **Transparency**: Chromakey removal of green screen (#1bba14)
- **Display Size**: 920x690px (4:3 aspect ratio)
- **Memory Usage**: ~1GB (330 frames loaded in memory)
- **System Tray**: Hardcodet.NotifyIcon.Wpf for tray integration
- **No Video Dependencies**: All assets pre-extracted, no runtime processing

## License

This project is currently under development. License pending.