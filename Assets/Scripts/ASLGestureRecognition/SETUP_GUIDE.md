# ASL Gesture Recognition Setup Guide

This guide walks you through setting up the ASL gesture recognition system in your Unity project.

## Prerequisites

1. **Unity Version**: 2022.3 LTS or newer
2. **Unity Packages**: 
   - Unity.Barracuda 3.0.0+ (for ONNX model inference)
   - Unity.TextMeshPro (for UI text)
3. **Hardware**: 
   - Camera/webcam (for real-time detection)
   - ONNX model file (for gesture recognition)

## Quick Setup (5 Minutes)

### Step 1: Import the Scripts
1. Copy all scripts from `Assets/Scripts/ASLGestureRecognition/` and `Assets/Scripts/MediaPipe/`
2. Unity will automatically compile them with the assembly definitions

### Step 2: Install Required Packages
1. Open Window → Package Manager
2. Install "Barracuda" package (version 3.0.0 or newer)
3. Install "TextMeshPro" if not already present

### Step 3: Create the ASL System GameObject
1. Create empty GameObject named "ASL_GestureSystem"
2. Add these components in order:
   ```
   - ASLModelInference
   - HandGestureRecognizer
   - HandDetectionManager
   - MediaPipeHandTracker
   - HandLandmarkRunner
   - ASLSystemTester (optional, for testing)
   ```

### Step 4: Configure Components

**ASLModelInference**:
- Assign your ONNX model to "Model Asset" field
- Set "Worker Type" to "Auto" (recommended)
- Adjust "Confidence Threshold" (0.5-0.8 recommended)

**HandGestureRecognizer**:
- Link "Model Inference" to ASLModelInference component
- Link "Hand Detection Manager" to HandDetectionManager component
- Enable "Use Simulation Mode" for testing without camera
- Configure "Gesture Hold Time" (2-3 seconds recommended)

**HandDetectionManager**:
- Set "Target Width/Height" (640x480 recommended)
- Set "Detection Confidence" (0.7 recommended)
- Set "Tracking Confidence" (0.5 recommended)
- Link "Hand Tracker" to MediaPipeHandTracker component

### Step 5: Test the System
1. Press Play in Unity
2. Check Console for initialization messages
3. Use A-Z keys in simulation mode to test gesture recognition
4. Use ASL → Run System Tests from menu for comprehensive testing

## Detailed Configuration

### Camera Settings

For optimal performance:
```
Target Width: 640
Target Height: 480
Target FPS: 30
Camera Timeout: 10 seconds
```

For better accuracy (higher resource usage):
```
Target Width: 1280
Target Height: 720
Target FPS: 30
```

For better performance (lower accuracy):
```
Target Width: 320
Target Height: 240
Target FPS: 15
```

### Model Settings

**GPU Inference** (if supported):
- Worker Type: Auto or ComputePrecompiled
- Optimize Model: Checked
- Force Arbitrary Batch Size: Unchecked

**CPU Inference** (fallback):
- Worker Type: CSharpBurst
- Optimize Model: Checked

### Detection Settings

**High Accuracy**:
- Detection Confidence: 0.8
- Tracking Confidence: 0.7
- Enable Smoothing: True
- Smoothing Factor: 0.8

**Balanced**:
- Detection Confidence: 0.7
- Tracking Confidence: 0.5
- Enable Smoothing: True
- Smoothing Factor: 0.7

**High Performance**:
- Detection Confidence: 0.6
- Tracking Confidence: 0.4
- Enable Smoothing: False
- Processing Interval: 0.05 (20 FPS)

## UI Integration

### Basic UI Setup

Create Canvas with these UI elements:

```
Canvas
├── PredictionPanel
│   ├── PredictionLabel (Text: "Prediction:")
│   └── PredictionValue (Text: "---")
├── ConfidencePanel
│   ├── ConfidenceLabel (Text: "Confidence:")
│   └── ConfidenceValue (Text: "0%")
├── StatusPanel
│   └── StatusText (Text: "Initializing...")
└── ConfidenceSlider (Slider: 0-1)
```

Link these to HandGestureRecognizer:
- Prediction Text → PredictionValue
- Confidence Text → ConfidenceValue
- Status Text → StatusText
- Confidence Slider → ConfidenceSlider

### Advanced UI Features

Add these for better user experience:

1. **Mode Toggle Button**:
   ```csharp
   Button.onClick.AddListener(() => gestureRecognizer.ToggleMode());
   ```

2. **Reset Button**:
   ```csharp
   Button.onClick.AddListener(() => gestureRecognizer.ResetSystem());
   ```

3. **Target Letter Display** (for games):
   ```csharp
   gestureRecognizer.SetTargetLetter("A");
   targetLetterText.text = "Sign: A";
   ```

## Troubleshooting

### Common Issues

**"No camera devices found"**
- Check camera permissions
- Ensure camera not in use by other apps
- Try different Camera Index values (0, 1, 2...)

**"Model inference error"**
- Verify ONNX model is assigned
- Check model input/output layer names
- Ensure Unity.Barracuda is installed

**"Assembly reference errors"**
- Check that assembly definition files (.asmdef) are present
- Verify Unity.Barracuda package is installed
- Try reimporting scripts

**Low performance**
- Reduce camera resolution
- Increase processing interval
- Use CPU inference mode
- Disable landmark smoothing

**Poor accuracy**
- Improve lighting conditions
- Increase detection confidence thresholds
- Use higher camera resolution
- Enable landmark smoothing

### Debug Tools

Use these for debugging:

1. **Console Logging**: Enable verbose logging in components
2. **Editor Menu**: Use ASL menu items for testing
3. **System Tester**: Add ASLSystemTester component for automated tests
4. **Performance Metrics**: Check FPS in HandLandmarkRunner

### Editor Menu Items

Access these from Unity menu bar under "ASL":
- Test Model Info
- Test Recognition System
- Test Hand Detection
- Test MediaPipe Tracker
- Test Landmark Runner
- Run System Tests
- Create Hand Detection Configs

## Performance Optimization

### For Mobile Devices

```csharp
// Recommended mobile settings
config.inputWidth = 320;
config.inputHeight = 240;
config.processingInterval = 0.05f; // 20 FPS
config.enablePerformanceMode = true;
config.enableSmoothing = false;
```

### For Desktop/Console

```csharp
// Recommended desktop settings
config.inputWidth = 640;
config.inputHeight = 480;
config.processingInterval = 0.033f; // 30 FPS
config.enablePerformanceMode = false;
config.enableSmoothing = true;
```

## Integration with Existing Games

### Event-Driven Integration

```csharp
public class GameController : MonoBehaviour {
    void Start() {
        var recognizer = FindObjectOfType<HandGestureRecognizer>();
        recognizer.OnGestureConfirmed += OnPlayerGesture;
    }
    
    void OnPlayerGesture(string letter) {
        // Handle player input
        ProcessPlayerInput(letter);
    }
}
```

### Polling Integration

```csharp
public class GameController : MonoBehaviour {
    private HandGestureRecognizer recognizer;
    
    void Update() {
        var status = recognizer.GetCurrentStatus();
        if (!string.IsNullOrEmpty(status.prediction)) {
            // Process current gesture
            ProcessCurrentGesture(status.prediction, status.confidence);
        }
    }
}
```

## Next Steps

1. **Train Custom Model**: Use TensorFlow/PyTorch with MediaPipe data
2. **Add Gesture Sequences**: Implement word-level recognition
3. **Multiplayer Support**: Synchronize gestures over network
4. **Platform Optimization**: Mobile-specific optimizations
5. **Accessibility Features**: Voice feedback, haptic confirmation

For advanced usage, see the full implementation guide and component documentation.

## Support

If you encounter issues:
1. Check the console for error messages
2. Run the ASL System Tests
3. Verify all prerequisites are met
4. Check component configurations match this guide
5. Review the troubleshooting section above