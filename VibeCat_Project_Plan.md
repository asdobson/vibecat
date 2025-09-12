# VibeCat - Desktop Music Visualizer
## Project Plan & Technical Specification

### Overview
VibeCat is a Windows desktop application that displays an animated cat video overlay that synchronizes its movement speed to the BPM of currently playing Spotify tracks. The cat appears to "vibe" to the music by bobbing its head at the song's tempo.

### Core Features
- Transparent, borderless cat video overlay on desktop
- Green screen (chroma key) removal from source video
- Real-time BPM synchronization via Spotify Web API
- Click-through window (non-interactive overlay)
- Adjustable position, size, and transparency

### Technology Stack
- **Platform**: Windows 10/11 (x64)
- **Framework**: .NET 8 with WPF (Windows Presentation Foundation)
- **Language**: C# 12
- **Video Processing**: FFMpegCore for decoding and chroma key
- **Spotify Integration**: SpotifyAPI-NET library
- **Audio Fallback**: NAudio for system audio detection (non-Spotify sources)
- **Rendering**: WPF hardware-accelerated composition

---

## Development Phases

### Phase 1: Basic Video Display with Transparency
**Goal**: Display the cat video as a transparent overlay on the desktop

**Tasks**:
1. Create WPF application with borderless, transparent window
2. Implement video loading and playback using FFMpegCore
3. Apply chroma key filter to remove green screen
4. Set window properties:
   - `Topmost = true` (always on top)
   - `AllowsTransparency = true`
   - `WindowStyle = None`
   - Click-through using `WS_EX_TRANSPARENT`
5. Basic playback controls (play/pause/loop)

**Deliverable**: Cat video playing in loop on desktop with transparent background

**Key Code Components**:
```csharp
// MainWindow.xaml.cs
- TransparentWindow class
- VideoPlayer component
- ChromaKeyProcessor
```

---

### Phase 2: Spotify Integration and BPM Retrieval
**Goal**: Connect to Spotify and retrieve real-time track BPM data

**Tasks**:
1. Register Spotify app and obtain Client ID/Secret
2. Implement OAuth 2.0 authentication flow
3. Set up SpotifyAPI-NET client
4. Create polling service for current track:
   - Poll `/v1/me/player/currently-playing` every 2 seconds
   - Detect track changes
   - Fetch audio features via `/v1/audio-features/{id}`
5. Handle edge cases:
   - No active Spotify session
   - Paused playback
   - Local files (no BPM data)
   - Podcasts/non-music content

**Deliverable**: Service that provides real-time BPM values from Spotify

**Key Code Components**:
```csharp
// SpotifyService.cs
- Authentication manager
- Track polling loop
- BPM cache (avoid redundant API calls)
- Event system for track changes
```

**Required Spotify Scopes**:
- `user-read-currently-playing`
- `user-read-playback-state`

---

### Phase 3: Video Speed Synchronization
**Goal**: Dynamically adjust video playback speed to match song BPM

**Tasks**:
1. Implement BPM-to-speed mapping algorithm:
   - Base BPM (e.g., 120 BPM = 1.0x speed)
   - Calculate speed multiplier: `speed = currentBPM / baseBPM`
   - Clamp to reasonable range (0.5x - 2.0x)
2. Smooth speed transitions:
   - Interpolate between speeds over 500ms
   - Prevent jarring changes
3. Frame interpolation for smooth slow-motion
4. Ensure seamless looping at all speeds
5. Implement manual BPM override for testing

**Deliverable**: Cat animation perfectly synchronized to music tempo

**Key Code Components**:
```csharp
// VideoSyncEngine.cs
- BPM to speed calculator
- Speed interpolation system
- Frame timing controller
```

---

### Phase 4: Desktop Overlay Polish & Controls
**Goal**: Add user controls and polish the experience

**Tasks**:
1. System tray application:
   - Minimize to tray
   - Right-click context menu
   - Exit option
2. Settings window:
   - Cat position (drag or coordinate input)
   - Size slider (50% - 200%)
   - Opacity slider (50% - 100%)
   - Base BPM adjustment
   - Spotify reconnect button
3. Hotkeys:
   - Show/hide cat (Win+Shift+C)
   - Reset position (Win+Shift+R)
4. Multiple animation support:
   - Different cats for different BPM ranges
   - Smooth transitions between animations
5. Auto-start on Windows boot option

**Deliverable**: Polished, user-friendly application

**Key Code Components**:
```csharp
// TrayManager.cs
// SettingsWindow.xaml
// HotkeyManager.cs
// ConfigurationManager.cs
```

---

## Technical Considerations

### Performance Optimization
- Target <2% CPU usage during idle
- <5% CPU during video playback
- Hardware acceleration via WPF render pipeline
- Dispose video frames properly to prevent memory leaks
- Limit Spotify API calls (use caching)

### Error Handling
- Graceful fallback when Spotify unavailable
- Handle network interruptions
- Validate video file on startup
- Recover from render failures

### Configuration Storage
- Store settings in `%APPDATA%\VibeCat\settings.json`
- Remember window position between sessions
- Store Spotify refresh token securely (DPAPI)

### Distribution
- Single executable with embedded resources
- MSIX installer for Microsoft Store (optional)
- Auto-updater for new versions

---

## Project Structure
```
VibeCat/
├── VibeCat.Core/              # Business logic
│   ├── Models/
│   ├── Services/
│   │   ├── SpotifyService.cs
│   │   ├── VideoService.cs
│   │   └── SyncEngine.cs
│   └── Interfaces/
├── VibeCat.WPF/               # UI layer
│   ├── MainWindow.xaml
│   ├── SettingsWindow.xaml
│   ├── Controls/
│   └── Resources/
│       └── cat-green.mp4
├── VibeCat.Tests/             # Unit tests
└── VibeCat.sln
```

---

## MVP Definition
**Minimum Viable Product includes**:
- Phase 1: Video overlay working
- Phase 2: Spotify BPM retrieval
- Phase 3: Basic speed sync (no interpolation)
- Simple exit button (no full settings UI)

**Estimated Timeline**: 
- MVP: 2-3 days
- Full Version: 1-2 weeks

---

## Future Enhancements (Post-v1.0)
- Support for other music services (Apple Music, YouTube Music)
- Audio visualizer effects on the cat
- Community cat animations (workshop)
- Multi-monitor support
- Cat reacts to drops/buildups (audio analysis)
- RGB sync with Razer/Corsair devices