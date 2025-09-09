using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ASLGestureRecognition
{
    /// <summary>
    /// Main controller for ASL gesture recognition system
    /// Integrates MediaPipe hand tracking with ONNX model inference
    /// Provides simulation mode as fallback when camera/model is unavailable
    /// </summary>
    public class HandGestureRecognizer : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private ASLModelInference modelInference;
        [SerializeField] private HandDetectionManager handDetectionManager;
        
        [Header("Operation Modes")]
        [SerializeField] private bool useSimulationMode = true;
        [SerializeField] private bool enableRealTimeInference = true;
        [SerializeField] private float inferenceInterval = 0.1f; // 10 FPS inference
        
        [Header("UI Components")]
        [SerializeField] private Text predictionText;
        [SerializeField] private Text confidenceText;
        [SerializeField] private Text statusText;
        [SerializeField] private Slider confidenceSlider;
        
        [Header("Game Integration")]
        [SerializeField] private float gestureHoldTime = 2.0f;
        [SerializeField] private string currentTargetLetter = "A";
        [SerializeField] private bool enableGameIntegration = true;
        
        [Header("Simulation Settings")]
        [SerializeField] private KeyCode[] simulationKeys = new KeyCode[26];
        [SerializeField] private float simulationConfidence = 0.95f;
        
        // State management
        private string currentPrediction = "";
        private float currentConfidence = 0f;
        private float lastInferenceTime = 0f;
        private bool isProcessing = false;
        private Coroutine gestureHoldCoroutine;
        
        // Events for game integration
        public System.Action<string, float> OnGestureRecognized;
        public System.Action<string> OnGestureConfirmed; // After hold time
        public System.Action<bool> OnModelStatusChanged;
        
        // Simulation data
        private readonly string[] aslLetters = new string[26] 
        {
            "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M",
            "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"
        };
        
        void Start()
        {
            InitializeSystem();
        }
        
        void Update()
        {
            HandleInput();
            
            if (enableRealTimeInference && !useSimulationMode)
            {
                ProcessRealTimeInference();
            }
        }
        
        /// <summary>
        /// Initialize the gesture recognition system
        /// </summary>
        private void InitializeSystem()
        {
            // Setup simulation keys if not configured
            SetupSimulationKeys();
            
            // Initialize model inference
            if (modelInference == null)
            {
                modelInference = GetComponent<ASLModelInference>();
            }
            
            if (handDetectionManager == null)
            {
                handDetectionManager = GetComponent<HandDetectionManager>();
            }
            
            // Subscribe to events
            if (modelInference != null)
            {
                modelInference.OnGesturePredicted += OnModelPrediction;
                modelInference.OnError += OnModelError;
            }
            
            if (handDetectionManager != null)
            {
                handDetectionManager.OnHandLandmarksDetected += OnHandLandmarksReceived;
                handDetectionManager.OnDetectionError += OnDetectionError;
            }
            
            // Check if we should use simulation mode
            CheckSystemAvailability();
            
            UpdateStatusDisplay();
        }
        
        /// <summary>
        /// Setup default simulation keys (A-Z mapped to keyboard)
        /// </summary>
        private void SetupSimulationKeys()
        {
            if (simulationKeys.Length != 26)
            {
                simulationKeys = new KeyCode[26];
            }
            
            // Map A-Z to corresponding keys
            for (int i = 0; i < 26; i++)
            {
                simulationKeys[i] = (KeyCode)((int)KeyCode.A + i);
            }
        }
        
        /// <summary>
        /// Check if real inference system is available, fallback to simulation if needed
        /// </summary>
        private void CheckSystemAvailability()
        {
            bool modelReady = modelInference != null && modelInference.IsReady;
            bool cameraReady = handDetectionManager != null && handDetectionManager.IsInitialized;
            
            if (!modelReady || !cameraReady)
            {
                Debug.LogWarning("Real inference system not available, using simulation mode");
                useSimulationMode = true;
            }
            
            OnModelStatusChanged?.Invoke(modelReady && cameraReady);
        }
        
        /// <summary>
        /// Handle keyboard input for simulation and manual control
        /// </summary>
        private void HandleInput()
        {
            if (useSimulationMode)
            {
                HandleSimulationInput();
            }
            
            // Manual controls
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetSystem();
            }
            
            if (Input.GetKeyDown(KeyCode.T))
            {
                ToggleMode();
            }
        }
        
        /// <summary>
        /// Handle simulation mode keyboard input
        /// </summary>
        private void HandleSimulationInput()
        {
            for (int i = 0; i < simulationKeys.Length; i++)
            {
                if (Input.GetKeyDown(simulationKeys[i]))
                {
                    string letter = aslLetters[i];
                    ProcessGesturePrediction(letter, simulationConfidence);
                    break;
                }
            }
        }
        
        /// <summary>
        /// Process real-time inference from camera data
        /// </summary>
        private void ProcessRealTimeInference()
        {
            if (isProcessing || Time.time - lastInferenceTime < inferenceInterval)
                return;
            
            if (handDetectionManager != null && handDetectionManager.HasValidHandData)
            {
                var landmarks = handDetectionManager.GetNormalizedLandmarks();
                if (landmarks != null && landmarks.Length == 63)
                {
                    StartCoroutine(RunInferenceAsync(landmarks));
                }
            }
        }
        
        /// <summary>
        /// Run model inference asynchronously to avoid blocking main thread
        /// </summary>
        private IEnumerator RunInferenceAsync(float[] landmarks)
        {
            isProcessing = true;
            lastInferenceTime = Time.time;
            
            // Run inference on next frame to avoid blocking
            yield return null;
            
            if (modelInference != null)
            {
                var (letter, confidence) = modelInference.PredictGesture(landmarks);
                if (!string.IsNullOrEmpty(letter))
                {
                    ProcessGesturePrediction(letter, confidence);
                }
            }
            
            isProcessing = false;
        }
        
        /// <summary>
        /// Process gesture prediction from either model or simulation
        /// </summary>
        private void ProcessGesturePrediction(string letter, float confidence)
        {
            currentPrediction = letter;
            currentConfidence = confidence;
            
            UpdateUI();
            
            // Trigger immediate recognition event
            OnGestureRecognized?.Invoke(letter, confidence);
            
            // Handle gesture hold logic for game integration
            if (enableGameIntegration)
            {
                HandleGestureHold(letter, confidence);
            }
        }
        
        /// <summary>
        /// Handle gesture hold confirmation for game actions
        /// </summary>
        private void HandleGestureHold(string letter, float confidence)
        {
            // Stop previous hold coroutine if running
            if (gestureHoldCoroutine != null)
            {
                StopCoroutine(gestureHoldCoroutine);
            }
            
            // Start new hold timer
            gestureHoldCoroutine = StartCoroutine(GestureHoldTimer(letter, confidence));
        }
        
        /// <summary>
        /// Coroutine to handle gesture hold timing
        /// </summary>
        private IEnumerator GestureHoldTimer(string letter, float confidence)
        {
            float startTime = Time.time;
            
            while (Time.time - startTime < gestureHoldTime)
            {
                // Check if gesture changed (cancel hold)
                if (currentPrediction != letter)
                {
                    yield break;
                }
                yield return null;
            }
            
            // Gesture held for required time, confirm it
            OnGestureConfirmed?.Invoke(letter);
            Debug.Log($"Gesture '{letter}' confirmed with confidence {confidence:F2}");
        }
        
        /// <summary>
        /// Update UI elements with current prediction
        /// </summary>
        private void UpdateUI()
        {
            if (predictionText != null)
            {
                predictionText.text = string.IsNullOrEmpty(currentPrediction) ? "---" : currentPrediction;
            }
            
            if (confidenceText != null)
            {
                confidenceText.text = $"{currentConfidence:P1}";
            }
            
            if (confidenceSlider != null)
            {
                confidenceSlider.value = currentConfidence;
            }
        }
        
        /// <summary>
        /// Update status display
        /// </summary>
        private void UpdateStatusDisplay()
        {
            string status = "";
            
            if (useSimulationMode)
            {
                status = "Simulation Mode (Press A-Z keys)";
            }
            else if (modelInference != null && modelInference.IsReady)
            {
                status = "Real-time Recognition Active";
            }
            else
            {
                status = "System Initializing...";
            }
            
            if (statusText != null)
            {
                statusText.text = status;
            }
        }
        
        /// <summary>
        /// Event handler for model predictions
        /// </summary>
        private void OnModelPrediction(string letter, float confidence)
        {
            ProcessGesturePrediction(letter, confidence);
        }
        
        /// <summary>
        /// Event handler for model errors
        /// </summary>
        private void OnModelError(string error)
        {
            Debug.LogError($"Model Error: {error}");
            
            // Fallback to simulation mode on error
            if (!useSimulationMode)
            {
                Debug.LogWarning("Falling back to simulation mode due to model error");
                useSimulationMode = true;
                UpdateStatusDisplay();
            }
        }
        
        /// <summary>
        /// Event handler for hand landmarks detection
        /// </summary>
        private void OnHandLandmarksReceived(float[] landmarks)
        {
            // Hand landmarks are processed in ProcessRealTimeInference()
            // This event could be used for additional processing if needed
        }
        
        /// <summary>
        /// Event handler for detection errors
        /// </summary>
        private void OnDetectionError(string error)
        {
            Debug.LogError($"Detection Error: {error}");
        }
        
        /// <summary>
        /// Reset the recognition system
        /// </summary>
        public void ResetSystem()
        {
            currentPrediction = "";
            currentConfidence = 0f;
            
            if (gestureHoldCoroutine != null)
            {
                StopCoroutine(gestureHoldCoroutine);
                gestureHoldCoroutine = null;
            }
            
            if (modelInference != null)
            {
                modelInference.ReloadModel();
            }
            
            CheckSystemAvailability();
            UpdateUI();
            UpdateStatusDisplay();
            
            Debug.Log("Gesture recognition system reset");
        }
        
        /// <summary>
        /// Toggle between simulation and real inference modes
        /// </summary>
        public void ToggleMode()
        {
            useSimulationMode = !useSimulationMode;
            UpdateStatusDisplay();
            Debug.Log($"Switched to {(useSimulationMode ? "Simulation" : "Real-time")} mode");
        }
        
        /// <summary>
        /// Set target letter for game integration
        /// </summary>
        public void SetTargetLetter(string letter)
        {
            currentTargetLetter = letter.ToUpper();
        }
        
        /// <summary>
        /// Check if current prediction matches target
        /// </summary>
        public bool IsCurrentPredictionCorrect()
        {
            return currentPrediction == currentTargetLetter;
        }
        
        /// <summary>
        /// Get current system status for external queries
        /// </summary>
        public (string prediction, float confidence, bool isSimulation) GetCurrentStatus()
        {
            return (currentPrediction, currentConfidence, useSimulationMode);
        }
        
        void OnDestroy()
        {
            // Cleanup event subscriptions
            if (modelInference != null)
            {
                modelInference.OnGesturePredicted -= OnModelPrediction;
                modelInference.OnError -= OnModelError;
            }
            
            if (handDetectionManager != null)
            {
                handDetectionManager.OnHandLandmarksDetected -= OnHandLandmarksReceived;
                handDetectionManager.OnDetectionError -= OnDetectionError;
            }
        }
        
        #if UNITY_EDITOR
        [UnityEditor.MenuItem("ASL/Test Recognition System")]
        private static void TestRecognitionSystem()
        {
            var recognizer = FindObjectOfType<HandGestureRecognizer>();
            if (recognizer != null)
            {
                var status = recognizer.GetCurrentStatus();
                Debug.Log($"Recognition Status - Prediction: {status.prediction}, Confidence: {status.confidence:F2}, Simulation: {status.isSimulation}");
            }
            else
            {
                Debug.Log("No HandGestureRecognizer found in scene");
            }
        }
        #endif
    }
}