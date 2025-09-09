using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Barracuda;

namespace ASLGestureRecognition
{
    /// <summary>
    /// Handles ONNX model inference for ASL gesture recognition using Unity Barracuda
    /// Processes hand landmark data (21 landmarks x 3 coordinates = 63 features) to predict ASL letters A-Z
    /// </summary>
    public class ASLModelInference : MonoBehaviour
    {
        [Header("Model Configuration")]
        [SerializeField] private NNModel modelAsset;
        [SerializeField] private WorkerFactory.Type workerType = WorkerFactory.Type.Auto;
        [SerializeField] private bool verbose = false;
        
        [Header("Input Configuration")]
        [SerializeField] private string inputLayerName = "input";
        [SerializeField] private string outputLayerName = "probabilities";
        [SerializeField] private int expectedInputSize = 63; // 21 landmarks * 3 coordinates
        
        [Header("Output Configuration")]
        [SerializeField] private float confidenceThreshold = 0.5f;
        [SerializeField] private string[] aslLabels = new string[26] 
        {
            "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M",
            "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"
        };
        
        private Model model;
        private IWorker worker;
        private bool isModelLoaded = false;
        private bool isInitialized = false;
        
        public bool IsReady => isInitialized && isModelLoaded && worker != null;
        public event System.Action<string, float> OnGesturePredicted;
        public event System.Action<string> OnError;
        
        void Start()
        {
            InitializeModel();
        }
        
        void OnDestroy()
        {
            CleanupModel();
        }
        
        /// <summary>
        /// Initialize the ONNX model for inference
        /// </summary>
        public void InitializeModel()
        {
            try
            {
                if (modelAsset == null)
                {
                    LogError("Model asset is not assigned. Please assign the ONNX model in the inspector.");
                    return;
                }
                
                // Load the model
                model = ModelLoader.Load(modelAsset, verbose);
                
                if (model == null)
                {
                    LogError("Failed to load the ONNX model.");
                    return;
                }
                
                // Validate model structure
                ValidateModel();
                
                // Create worker with fallback logic
                CreateWorker();
                
                isModelLoaded = model != null;
                isInitialized = worker != null;
                
                if (IsReady)
                {
                    Debug.Log($"ASL Model initialized successfully. Worker type: {workerType}");
                }
                else
                {
                    LogError("Failed to initialize ASL model inference system.");
                }
            }
            catch (System.Exception e)
            {
                LogError($"Error initializing model: {e.Message}");
            }
        }
        
        /// <summary>
        /// Validate the loaded model structure
        /// </summary>
        private void ValidateModel()
        {
            if (model.inputs.Count == 0)
            {
                LogError("Model has no input layers defined.");
                return;
            }
            
            if (model.outputs.Count == 0)
            {
                LogError("Model has no output layers defined.");
                return;
            }
            
            // Check if expected input layer exists
            var inputFound = model.inputs.Any(input => input.name == inputLayerName);
            if (!inputFound)
            {
                // Try to find alternative input layer names
                var firstInput = model.inputs.FirstOrDefault();
                if (firstInput != null)
                {
                    inputLayerName = firstInput.name;
                    Debug.LogWarning($"Input layer '{inputLayerName}' not found. Using '{firstInput.name}' instead.");
                }
                else
                {
                    LogError("No valid input layers found in model.");
                    return;
                }
            }
            
            // Check if expected output layer exists
            var outputFound = model.outputs.Any(output => output == outputLayerName);
            if (!outputFound)
            {
                // Try to find alternative output layer names
                var firstOutput = model.outputs.FirstOrDefault();
                if (!string.IsNullOrEmpty(firstOutput))
                {
                    outputLayerName = firstOutput;
                    Debug.LogWarning($"Output layer '{outputLayerName}' not found. Using '{firstOutput}' instead.");
                }
                else
                {
                    LogError("No valid output layers found in model.");
                    return;
                }
            }
            
            Debug.Log($"Model validated - Input: {inputLayerName}, Output: {outputLayerName}");
        }
        
        /// <summary>
        /// Create worker with fallback from GPU to CPU if needed
        /// </summary>
        private void CreateWorker()
        {
            try
            {
                // Try to create worker with specified type
                worker = WorkerFactory.CreateWorker(workerType, model, verbose);
                
                if (worker == null && workerType != WorkerFactory.Type.CSharpBurst)
                {
                    // Fallback to CPU if GPU failed
                    Debug.LogWarning("GPU inference not supported, falling back to CPU mode.");
                    workerType = WorkerFactory.Type.CSharpBurst;
                    worker = WorkerFactory.CreateWorker(workerType, model, verbose);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to create {workerType} worker: {e.Message}. Falling back to CPU.");
                try
                {
                    workerType = WorkerFactory.Type.CSharpBurst;
                    worker = WorkerFactory.CreateWorker(workerType, model, verbose);
                }
                catch (System.Exception fallbackException)
                {
                    LogError($"Failed to create CPU worker: {fallbackException.Message}");
                }
            }
        }
        
        /// <summary>
        /// Predict ASL gesture from hand landmark data
        /// </summary>
        /// <param name="landmarks">Array of 21 hand landmarks, each with x, y, z coordinates (63 total values)</param>
        /// <returns>Predicted ASL letter and confidence score</returns>
        public (string letter, float confidence) PredictGesture(float[] landmarks)
        {
            if (!IsReady)
            {
                LogError("Model is not ready for inference.");
                return ("", 0f);
            }
            
            if (landmarks == null || landmarks.Length != expectedInputSize)
            {
                LogError($"Invalid input: expected {expectedInputSize} values, got {landmarks?.Length ?? 0}");
                return ("", 0f);
            }
            
            try
            {
                // Create input tensor
                using (var inputTensor = new Tensor(1, expectedInputSize, landmarks))
                {
                    // Set input and execute
                    worker.Execute(inputTensor);
                    
                    // Get output tensor
                    using (var outputTensor = worker.PeekOutput(outputLayerName))
                    {
                        if (outputTensor == null)
                        {
                            LogError($"Failed to get output tensor '{outputLayerName}'");
                            return ("", 0f);
                        }
                        
                        // Convert to array and find prediction
                        var predictions = outputTensor.ToReadOnlyArray();
                        var (predictedIndex, confidence) = GetMaxPrediction(predictions);
                        
                        if (predictedIndex >= 0 && predictedIndex < aslLabels.Length && confidence >= confidenceThreshold)
                        {
                            var predictedLetter = aslLabels[predictedIndex];
                            OnGesturePredicted?.Invoke(predictedLetter, confidence);
                            return (predictedLetter, confidence);
                        }
                        else
                        {
                            return ("", confidence);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                LogError($"Model inference error: {e.Message}");
                return ("", 0f);
            }
        }
        
        /// <summary>
        /// Find the prediction with highest confidence
        /// </summary>
        private (int index, float confidence) GetMaxPrediction(float[] predictions)
        {
            if (predictions == null || predictions.Length == 0)
                return (-1, 0f);
            
            int maxIndex = 0;
            float maxValue = predictions[0];
            
            for (int i = 1; i < predictions.Length; i++)
            {
                if (predictions[i] > maxValue)
                {
                    maxValue = predictions[i];
                    maxIndex = i;
                }
            }
            
            return (maxIndex, maxValue);
        }
        
        /// <summary>
        /// Cleanup model resources
        /// </summary>
        private void CleanupModel()
        {
            try
            {
                worker?.Dispose();
                worker = null;
                model = null;
                isModelLoaded = false;
                isInitialized = false;
                
                Debug.Log("ASL Model resources cleaned up.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during model cleanup: {e.Message}");
            }
        }
        
        /// <summary>
        /// Reload the model (useful for runtime model switching)
        /// </summary>
        public void ReloadModel()
        {
            CleanupModel();
            InitializeModel();
        }
        
        /// <summary>
        /// Get model information for debugging
        /// </summary>
        public string GetModelInfo()
        {
            if (!isModelLoaded) return "Model not loaded";
            
            var info = $"Model Info:\n" +
                      $"- Inputs: {string.Join(", ", model.inputs.Select(i => i.name))}\n" +
                      $"- Outputs: {string.Join(", ", model.outputs)}\n" +
                      $"- Worker Type: {workerType}\n" +
                      $"- Ready: {IsReady}";
            
            return info;
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[ASLModelInference] {message}");
            OnError?.Invoke(message);
        }
        
        #if UNITY_EDITOR
        [UnityEditor.MenuItem("ASL/Test Model Info")]
        private static void TestModelInfo()
        {
            var inference = FindObjectOfType<ASLModelInference>();
            if (inference != null)
            {
                Debug.Log(inference.GetModelInfo());
            }
            else
            {
                Debug.Log("No ASLModelInference found in scene");
            }
        }
        #endif
    }
}