# VibeCat - Phase 1 Complete 🐱

A desktop overlay application that displays an animated cat synchronized to your music's BPM.

## Current Status: Phase 1 ✅

### Completed Features:
- ✅ Transparent, borderless WPF window
- ✅ Green screen (chroma key) removal 
- ✅ Video frame extraction and playback
- ✅ Click-through window capability
- ✅ Always-on-top overlay

## Prerequisites

- Windows 10/11
- .NET 8 SDK
- FFmpeg binaries (automatically downloaded by FFMpegCore)

## Building the Project

1. **Install .NET 8 SDK** (if not already installed):
   ```powershell
   winget install Microsoft.DotNet.SDK.8
   ```

2. **Navigate to project directory** (in Windows Command Prompt or PowerShell):
   ```powershell
   cd C:\path\to\VibeCat
   ```

3. **Restore NuGet packages**:
   ```powershell
   dotnet restore
   ```

4. **Build the project**:
   ```powershell
   dotnet build
   ```

5. **Run the application**:
   ```powershell
   dotnet run --project VibeCat\VibeCat.csproj
   ```

## How to Use

1. **Place your video file**: 
   - Create a `Resources` folder in `VibeCat\VibeCat\`
   - Put your green screen cat video (MP4) in this folder
   - Name it `cat-green.mp4`

2. **Run the app**: The cat will appear on your desktop with the green screen removed

3. **Controls**:
   - Drag the window to reposition (when not in click-through mode)
   - Click the red X button to close
   - The window can be made click-through via code

## Testing Phase 1

The application currently shows a placeholder animation (animated circle with cat emoji) to demonstrate:
- Window transparency working
- Chroma key algorithm functioning
- Animation loop playing

To test with your actual video:
1. Add your `cat-green.mp4` to the Resources folder
2. Modify `MainWindow.xaml.cs` to call `LoadVideoFile()` with your video path

## Project Structure
```
VibeCat/
├── VibeCat.sln
└── VibeCat/
    ├── VibeCat.csproj
    ├── App.xaml
    ├── App.xaml.cs
    ├── MainWindow.xaml
    ├── MainWindow.xaml.cs
    ├── Services/
    │   └── VideoProcessor.cs
    └── Resources/
        └── (place cat-green.mp4 here)
```

## Known Issues / TODOs

- FFmpeg binary download happens on first run (may take a moment)
- Video loading from file not yet connected in MainWindow (using placeholder)
- Need to add Resources folder creation to build process

## Next: Phase 2
- Spotify API integration for BPM detection
- OAuth authentication flow
- Real-time track monitoring