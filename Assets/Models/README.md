# ONNX Model Configuration Guide

This folder should contain your ONNX model for ASL gesture recognition.

## Model Requirements

### Input Specifications
- **Input Shape**: (1, 63) - representing 21 hand landmarks with x, y, z coordinates
- **Input Layer Name**: `input` (configurable in ASLModelInference)
- **Data Type**: Float32
- **Normalization**: Coordinates should be normalized between 0-1 for x,y and -1 to 1 for z

### Output Specifications
- **Output Shape**: (1, 26) - representing probabilities for each ASL letter A-Z
- **Output Layer Name**: `probabilities` (configurable in ASLModelInference)
- **Data Type**: Float32
- **Activation**: Softmax (probabilities sum to 1.0)

## Unity Import Settings

When importing your ONNX model into Unity:

1. **Model Import Settings**:
   - ✅ **Optimize Model**: Checked (for better performance)
   - ⚠️ **Force Arbitrary Batch Size**: Uncheck if you have issues, check for flexibility
   - **Backend**: Auto (Unity will choose best available)

2. **Input/Output Layer Configuration**:
   - Verify input layer name matches your model
   - Verify output layer name matches your model
   - Update `inputLayerName` and `outputLayerName` in ASLModelInference script if needed

## Common Issues and Solutions

### Issue: "Model inference error: The given key 'output' was not present in the dictionary"
**Solution**: Check that your output layer name matches what's configured in the script. Common names:
- `probabilities`
- `output`
- `dense_1`
- `predictions`

### Issue: "Model contains broken links"
**Solution**: 
1. Re-export your model with proper layer names
2. Use tools like Netron to visualize your model structure
3. Update layer names in the ASLModelInference script

### Issue: "Current platform does not support GPU inference mode"
**Solution**: 
- This is normal and the system will automatically fallback to CPU
- Ensure Unity.Barracuda is properly installed
- GPU inference requires compatible hardware and drivers

## Hand Landmark Format

The model expects 21 hand landmarks in the following order:
```
0:  WRIST
1:  THUMB_CMC      5:  INDEX_FINGER_MCP    9:  MIDDLE_FINGER_MCP   13: RING_FINGER_MCP    17: PINKY_MCP
2:  THUMB_MCP      6:  INDEX_FINGER_PIP    10: MIDDLE_FINGER_PIP   14: RING_FINGER_PIP    18: PINKY_PIP  
3:  THUMB_IP       7:  INDEX_FINGER_DIP    11: MIDDLE_FINGER_DIP   15: RING_FINGER_DIP    19: PINKY_DIP
4:  THUMB_TIP      8:  INDEX_FINGER_TIP    12: MIDDLE_FINGER_TIP   16: RING_FINGER_TIP    20: PINKY_TIP
```

Each landmark has 3 coordinates (x, y, z), making 63 total values.

## Testing Your Model

1. Place your ONNX model file in this folder
2. Assign it to the ASLModelInference component in the Unity inspector
3. Use the simulation mode to test without a camera
4. Check the Unity console for any error messages
5. Use the editor menu items under "ASL/" for debugging

## Model Training Tips

If you're training your own model:
- Use MediaPipe hand landmarks as input features
- Normalize landmark coordinates relative to the wrist
- Use data augmentation to improve generalization
- Export with explicit input/output names for easier integration