# ASL Gesture Recognition Implementation Guide

This guide explains how to set up and use the ASL gesture recognition system in your Unity project.

## System Overview

The ASL gesture recognition system consists of several components:

1. **ASLModelInference**: Handles ONNX model loading and inference
2. **HandGestureRecognizer**: Main controller that orchestrates the system
3. **HandDetectionManager**: Manages camera and hand detection
4. **MediaPipeHandTracker**: Provides hand landmark detection
5. **HandLandmarkRunner**: Runs the detection pipeline
6. **HandLandmarkDetectionConfig**: Configuration for detection parameters

## Quick Setup

### 1. Scene Setup

Create a new GameObject and add these components:
```
GameObject: ASL_GestureSystem
├── ASLModelInference
├── HandGestureRecognizer  
├── HandDetectionManager
├── MediaPipeHandTracker
└── HandLandmarkRunner
```

### 2. Component Configuration

**ASLModelInference**:
- Assign your ONNX model to `Model Asset`
- Set `Input Layer Name` (default: "input")
- Set `Output Layer Name` (default: "probabilities")
- Choose `Worker Type` (Auto recommended)

**HandGestureRecognizer**:
- Link to ASLModelInference component
- Link to HandDetectionManager component
- Configure UI elements (optional)
- Set simulation keys for testing

**HandDetectionManager**:
- Configure camera settings
- Link to MediaPipeHandTracker component
- Set detection confidence thresholds

### 3. UI Setup (Optional)

Create UI elements for feedback:
```
Canvas
├── PredictionText (Text component)
├── ConfidenceText (Text component) 
├── StatusText (Text component)
└── ConfidenceSlider (Slider component)
```

Link these UI elements to the HandGestureRecognizer component.

## Usage Modes

### Simulation Mode (Default)

Perfect for testing without camera or model:
- Press A-Z keys to simulate gesture recognition
- Automatically enabled if camera/model unavailable
- Toggle with 'T' key at runtime

### Real-time Mode

Uses camera and ONNX model for actual recognition:
- Requires working camera
- Requires trained ONNX model
- Automatically processes hand landmarks at 10 FPS

### Game Integration

The system provides events for game integration:

```csharp
// Subscribe to gesture events
gestureRecognizer.OnGestureRecognized += (letter, confidence) => {
    Debug.Log($"Recognized: {letter} ({confidence:P1})");
};

gestureRecognizer.OnGestureConfirmed += (letter) => {
    Debug.Log($"Confirmed gesture: {letter}");
    // Trigger game action
};
```

## Configuration

### Performance Settings

For better performance:
- Lower `inferenceInterval` for faster recognition
- Reduce camera resolution
- Use `WorkerFactory.Type.CSharpBurst` for CPU inference

### Accuracy Settings  

For better accuracy:
- Increase `confidenceThreshold`
- Enable landmark smoothing
- Use higher camera resolution
- Increase `gestureHoldTime` for confirmation

## Troubleshooting

### Common Issues

**"Model not ready for inference"**
- Check that ONNX model is assigned
- Verify Unity.Barracuda package is installed
- Check console for model loading errors

**"No camera devices found"**  
- Grant camera permissions
- Check camera is not in use by other apps
- Try different `cameraIndex`

**"Invalid input: expected 63 values"**
- Verify hand landmark detection is working
- Check MediaPipe integration
- Enable debug logging to see landmark data

**Low recognition accuracy**
- Ensure good lighting
- Keep hand clearly visible in camera
- Train model with more diverse data
- Adjust confidence thresholds

### Debug Tools

Use the editor menu items under **ASL/** for debugging:
- Test Model Info
- Test Recognition System  
- Test Hand Detection
- Test MediaPipe Tracker
- Test Landmark Runner

### Debug Logging

Enable debug logging in components for detailed information:
- Set `verbose = true` in ASLModelInference
- Set `showDebugInfo = true` in HandDetectionManager
- Set `enableDebugLogging = true` in HandLandmarkDetectionConfig

## Events and Callbacks

### ASLModelInference Events
```csharp
OnGesturePredicted(string letter, float confidence)
OnError(string error)
```

### HandGestureRecognizer Events  
```csharp
OnGestureRecognized(string letter, float confidence)
OnGestureConfirmed(string letter)  // After hold time
OnModelStatusChanged(bool isReady)
```

### HandDetectionManager Events
```csharp
OnHandLandmarksDetected(float[] landmarks)
OnDetectionError(string error)
OnCameraStatusChanged(bool isActive)
```

## API Reference

### Key Methods

**ASLModelInference**:
```csharp
InitializeModel()                    // Load and setup model
PredictGesture(float[] landmarks)    // Run inference
ReloadModel()                        // Reload model
GetModelInfo()                       // Get debug info
```

**HandGestureRecognizer**:
```csharp
ResetSystem()                        // Reset all components
ToggleMode()                         // Switch simulation/real mode
SetTargetLetter(string letter)       // Set target for game
GetCurrentStatus()                   // Get current state
```

**HandDetectionManager**:
```csharp
InitializeCamera()                   // Start camera
GetNormalizedLandmarks()             // Get current landmarks
GetLandmark(int index)               // Get specific landmark
ResetDetection()                     // Reset detection
```

## Performance Optimization

### CPU Usage
- Reduce `processingInterval` (lower FPS)
- Use `WorkerFactory.Type.CSharpBurst`
- Lower camera resolution
- Disable landmark smoothing

### Memory Usage
- Release unused textures
- Use object pooling for frequent allocations
- Monitor WebCamTexture usage

### Battery Life (Mobile)
- Reduce processing frequency
- Lower camera FPS
- Use performance detection config
- Implement sleep mode when inactive

## Integration Examples

### Simple Recognition
```csharp
void Start() {
    var recognizer = GetComponent<HandGestureRecognizer>();
    recognizer.OnGestureConfirmed += letter => {
        Debug.Log($"User signed: {letter}");
    };
}
```

### Game Challenge
```csharp
public class ASLChallenge : MonoBehaviour {
    private string targetLetter = "A";
    private HandGestureRecognizer recognizer;
    
    void Start() {
        recognizer = GetComponent<HandGestureRecognizer>();
        recognizer.OnGestureConfirmed += OnGestureRecognized;
        recognizer.SetTargetLetter(targetLetter);
    }
    
    void OnGestureRecognized(string letter) {
        if (letter == targetLetter) {
            Debug.Log("Correct!");
            NextChallenge();
        }
    }
    
    void NextChallenge() {
        targetLetter = GetRandomLetter();
        recognizer.SetTargetLetter(targetLetter);
    }
}
```

### Custom Model Integration
```csharp
public class CustomASLModel : MonoBehaviour {
    public NNModel customModel;
    private ASLModelInference inference;
    
    void Start() {
        inference = GetComponent<ASLModelInference>();
        // Replace model at runtime
        inference.modelAsset = customModel;
        inference.ReloadModel();
    }
}
```

## Next Steps

1. **Training Custom Models**: Use TensorFlow/PyTorch with MediaPipe landmarks
2. **Advanced Features**: Add gesture sequences, continuous recognition
3. **Platform Optimization**: Mobile-specific optimizations
4. **Multiplayer**: Network synchronization of gestures
5. **Accessibility**: Voice feedback, haptic confirmation

For more advanced usage, see the individual script documentation and Unity's Barracuda documentation.