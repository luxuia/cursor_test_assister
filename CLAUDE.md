# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a game assistant tool built with C# and WPF that provides real-time game advice based on image recognition. The system captures game screenshots, recognizes game elements (heroes, equipment, skills, health), and generates contextual advice.

## Build and Run

```bash
# Build the project
dotnet build

# Run the application (main UI)
dotnet run --project GameAssistant/GameAssistant.csproj

# Run image recognition test on screenshots in test/ directory
dotnet run --project GameAssistant/GameAssistant.csproj -- test-recognition

# Generate DOTA 2 hero data
dotnet run --project GameAssistant/GameAssistant.csproj -- generate-hero-data
```

## Architecture

The system follows a layered architecture:

1. **Core Layer** (`GameAssistant/Core/`)
   - `Interfaces/`: Service contracts (IImageRecognizer, IScreenCapture, IConfigurationService, etc.)
   - `Models/`: Data models (GameState, HeroRosterResult, MinimapResult, EquipmentResult, StatusResult, Advice, AdviceRule)

2. **Services Layer** (`GameAssistant/Services/`)
   - `ImageRecognition/`: Core image processing using OpenCvSharp4
   - `ScreenCapture/`: Windows screen capture implementation
   - `Configuration/`: JSON-based configuration management
   - `Database/`: Advice rules database
   - `DecisionEngine/`: Game state analysis and advice generation

3. **Presentation Layer** (`GameAssistant/Views/`)
   - WPF windows: MainWindow, OverlayWindow, RegionSelectorWindow, TemplateCaptureWindow
   - ViewModels following MVVM pattern

## Image Recognition System

The `ImageRecognizer` class (Services/ImageRecognition/ImageRecognizer.cs) implements four recognition methods:

1. **Hero Roster Recognition**: Template matching for hero avatars in `Templates/Heroes/`
2. **Minimap Recognition**: HSV color detection for hero position markers
3. **Equipment Recognition**: Template matching for equipment icons in `Templates/Equipment/`
4. **Status Recognition**: Health bar detection (length/pixel count) and skill availability (brightness threshold)

### Template Matching

The `TemplateMatcher` class handles template-based recognition:
- Preloads templates from the `Templates/` directory on initialization
- Supports multi-scale matching (0.8x to 1.2x) for size variations
- Uses OpenCV's `CCoeffNormed` template matching method
- Removes duplicate matches within 20-pixel threshold

### Configuration System

All recognition parameters are stored as JSON files in `Config/`:
- `recognition_regions.json`: Rectangle coordinates for recognition areas (HeroRosterRegion, MinimapRegion, EquipmentPanelRegion, StatusBarRegion)
- `recognition_parameters.json`: Thresholds and algorithm parameters for each recognition module
- `game_window.json`: Target window configuration

Key configuration classes:
- `RecognitionParameters`: Root parameter container
- `HeroRecognitionParameters`: Hero matching threshold (default 0.75), position tolerance (20px)
- `HealthRecognitionParameters`: Detection method (PixelCount/LengthMeasurement/Combined), color ranges in HSV
- `MinimapRecognitionParameters`: Color ranges for ally/enemy markers (HSV), marker area constraints

### Game State Flow

1. `WindowsScreenCapture` captures game window frames
2. `ImageRecognizer` processes each frame on background threads
3. Recognition results populate a `GameState` object
4. `DecisionEngine` analyzes `GameState` against advice rules
5. `OverlayWindow` displays generated advice

## Adding New Recognition Features

1. Add method to `IImageRecognizer` interface
2. Implement in `ImageRecognizer` class using `TemplateMatcher` for template-based detection or direct OpenCV operations
3. Add corresponding region to `RecognitionRegions` model
4. Add parameters to `RecognitionParameters` class if needed
5. Update `GameState` model to store results

## Dependencies

- .NET 8.0 Windows
- WPF
- OpenCvSharp4 (4.10.0.20240103)
- OpenCvSharp4.runtime.win
- Newtonsoft.Json (13.0.3)
- System.Drawing.Common (8.0.0)

## Entry Point

`GameAssistant/Program.cs` contains command line argument handling:
- No args: Launch WPF application
- `test-recognition`: Run recognition tests on images in `test/` directory
- `generate-hero-data`: Generate DOTA 2 hero data

The `App.xaml.cs` routes to these handlers based on command line arguments.
