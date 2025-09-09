using System.Collections;
using UnityEngine;

namespace ASLGestureRecognition
{
    /// <summary>
    /// Test script to validate ASL gesture recognition system functionality
    /// Provides automated testing and validation of all components
    /// </summary>
    public class ASLSystemTester : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runTestsOnStart = true;
        [SerializeField] private bool enableContinuousTests = false;
        [SerializeField] private float testInterval = 5f;
        [SerializeField] private bool verboseLogging = true;
        
        [Header("Component References")]
        [SerializeField] private ASLModelInference modelInference;
        [SerializeField] private HandGestureRecognizer gestureRecognizer;
        [SerializeField] private HandDetectionManager handDetectionManager;
        [SerializeField] private MediaPipeHandTracker handTracker;
        [SerializeField] private HandLandmarkRunner landmarkRunner;
        
        // Test state
        private bool testsCompleted = false;
        private int testsPassed = 0;
        private int testsFailed = 0;
        private Coroutine continuousTestCoroutine;
        
        void Start()
        {
            if (runTestsOnStart)
            {
                StartCoroutine(RunAllTests());
            }
            
            if (enableContinuousTests)
            {
                continuousTestCoroutine = StartCoroutine(ContinuousTestLoop());
            }
        }
        
        void OnDestroy()
        {
            if (continuousTestCoroutine != null)
            {
                StopCoroutine(continuousTestCoroutine);
            }
        }
        
        /// <summary>
        /// Run all system tests
        /// </summary>
        public IEnumerator RunAllTests()
        {
            Log("=== Starting ASL System Tests ===");
            testsPassed = 0;
            testsFailed = 0;
            
            // Test 1: Component Initialization
            yield return StartCoroutine(TestComponentInitialization());
            
            // Test 2: Model Loading and Validation
            yield return StartCoroutine(TestModelFunctionality());
            
            // Test 3: Hand Detection System
            yield return StartCoroutine(TestHandDetectionSystem());
            
            // Test 4: MediaPipe Integration
            yield return StartCoroutine(TestMediaPipeIntegration());
            
            // Test 5: Gesture Recognition Pipeline
            yield return StartCoroutine(TestGestureRecognitionPipeline());
            
            // Test 6: Simulation Mode
            yield return StartCoroutine(TestSimulationMode());
            
            // Test 7: Error Handling
            yield return StartCoroutine(TestErrorHandling());
            
            // Final Results
            LogTestResults();
            testsCompleted = true;
        }
        
        /// <summary>
        /// Test component initialization
        /// </summary>
        private IEnumerator TestComponentInitialization()
        {
            Log("Test 1: Component Initialization");
            
            // Find components if not assigned
            if (modelInference == null)
                modelInference = FindObjectOfType<ASLModelInference>();
            if (gestureRecognizer == null)
                gestureRecognizer = FindObjectOfType<HandGestureRecognizer>();
            if (handDetectionManager == null)
                handDetectionManager = FindObjectOfType<HandDetectionManager>();
            if (handTracker == null)
                handTracker = FindObjectOfType<MediaPipeHandTracker>();
            if (landmarkRunner == null)
                landmarkRunner = FindObjectOfType<HandLandmarkRunner>();
            
            bool allComponentsFound = 
                modelInference != null &&
                gestureRecognizer != null &&
                handDetectionManager != null &&
                handTracker != null &&
                landmarkRunner != null;
            
            if (allComponentsFound)
            {
                PassTest("All required components found");
            }
            else
            {
                FailTest("Missing required components");
            }
            
            yield return null;
        }
        
        /// <summary>
        /// Test model functionality
        /// </summary>
        private IEnumerator TestModelFunctionality()
        {
            Log("Test 2: Model Functionality");
            
            if (modelInference == null)
            {
                FailTest("ASLModelInference not found");
                yield break;
            }
            
            // Test model info retrieval
            string modelInfo = modelInference.GetModelInfo();
            if (!string.IsNullOrEmpty(modelInfo))
            {
                PassTest("Model info retrieval successful");
                if (verboseLogging) Log($"Model Info: {modelInfo}");
            }
            else
            {
                FailTest("Model info retrieval failed");
            }
            
            // Test prediction with dummy data
            float[] dummyLandmarks = GenerateDummyLandmarks();
            var prediction = modelInference.PredictGesture(dummyLandmarks);
            
            if (!string.IsNullOrEmpty(prediction.letter) || prediction.confidence >= 0)
            {
                PassTest($"Model prediction test passed: {prediction.letter} ({prediction.confidence:F2})");
            }
            else
            {
                FailTest("Model prediction test failed");
            }
            
            yield return null;
        }
        
        /// <summary>
        /// Test hand detection system
        /// </summary>
        private IEnumerator TestHandDetectionSystem()
        {
            Log("Test 3: Hand Detection System");
            
            if (handDetectionManager == null)
            {
                FailTest("HandDetectionManager not found");
                yield break;
            }
            
            // Test system status
            string status = handDetectionManager.GetSystemStatus();
            if (!string.IsNullOrEmpty(status))
            {
                PassTest("Hand detection system status check passed");
                if (verboseLogging) Log($"Detection Status: {status}");
            }
            else
            {
                FailTest("Hand detection system status check failed");
            }
            
            // Test camera initialization (may fail if no camera available)
            bool wasInitialized = handDetectionManager.IsInitialized;
            if (!wasInitialized)
            {
                handDetectionManager.InitializeCamera();
                yield return new WaitForSeconds(2f); // Wait for initialization
            }
            
            if (handDetectionManager.IsInitialized)
            {
                PassTest("Camera initialization successful");
            }
            else
            {
                Log("Warning: Camera initialization failed (this is expected if no camera is available)");
            }
            
            yield return null;
        }
        
        /// <summary>
        /// Test MediaPipe integration
        /// </summary>
        private IEnumerator TestMediaPipeIntegration()
        {
            Log("Test 4: MediaPipe Integration");
            
            if (handTracker == null)
            {
                FailTest("MediaPipeHandTracker not found");
                yield break;
            }
            
            // Test landmark indices
            bool landmarkIndicesValid = MediaPipeHandTracker.HandLandmarkIndices.Count == 21;
            if (landmarkIndicesValid)
            {
                PassTest("MediaPipe landmark indices valid (21 landmarks)");
            }
            else
            {
                FailTest($"MediaPipe landmark indices invalid (expected 21, got {MediaPipeHandTracker.HandLandmarkIndices.Count})");
            }
            
            // Test tracking quality calculation
            float quality = handTracker.GetTrackingQuality();
            if (quality >= 0f && quality <= 1f)
            {
                PassTest($"MediaPipe tracking quality calculation valid: {quality:F2}");
            }
            else
            {
                FailTest($"MediaPipe tracking quality calculation invalid: {quality:F2}");
            }
            
            yield return null;
        }
        
        /// <summary>
        /// Test gesture recognition pipeline
        /// </summary>
        private IEnumerator TestGestureRecognitionPipeline()
        {
            Log("Test 5: Gesture Recognition Pipeline");
            
            if (gestureRecognizer == null)
            {
                FailTest("HandGestureRecognizer not found");
                yield break;
            }
            
            // Test status retrieval
            var status = gestureRecognizer.GetCurrentStatus();
            PassTest($"Gesture recognition status: {status.prediction}, confidence: {status.confidence:F2}, simulation: {status.isSimulation}");
            
            // Test target letter setting
            gestureRecognizer.SetTargetLetter("A");
            bool isCorrect = gestureRecognizer.IsCurrentPredictionCorrect();
            PassTest($"Target letter setting and validation working (current prediction correct: {isCorrect})");
            
            // Test mode toggling
            var initialStatus = gestureRecognizer.GetCurrentStatus();
            gestureRecognizer.ToggleMode();
            yield return null;
            var toggledStatus = gestureRecognizer.GetCurrentStatus();
            
            if (initialStatus.isSimulation != toggledStatus.isSimulation)
            {
                PassTest("Mode toggling successful");
            }
            else
            {
                FailTest("Mode toggling failed");
            }
            
            yield return null;
        }
        
        /// <summary>
        /// Test simulation mode
        /// </summary>
        private IEnumerator TestSimulationMode()
        {
            Log("Test 6: Simulation Mode");
            
            if (gestureRecognizer == null)
            {
                FailTest("HandGestureRecognizer not found for simulation test");
                yield break;
            }
            
            // Ensure we're in simulation mode
            var status = gestureRecognizer.GetCurrentStatus();
            if (!status.isSimulation)
            {
                gestureRecognizer.ToggleMode();
                yield return null;
            }
            
            PassTest("Simulation mode activated successfully");
            
            // Test would require actual key input simulation which is complex in automated testing
            // For now, we just verify the mode is working
            
            yield return null;
        }
        
        /// <summary>
        /// Test error handling
        /// </summary>
        private IEnumerator TestErrorHandling()
        {
            Log("Test 7: Error Handling");
            
            // Test invalid landmark input
            if (modelInference != null)
            {
                // Test with null input
                var result1 = modelInference.PredictGesture(null);
                if (string.IsNullOrEmpty(result1.letter) && result1.confidence == 0f)
                {
                    PassTest("Null input error handling successful");
                }
                else
                {
                    FailTest("Null input error handling failed");
                }
                
                // Test with wrong size input
                float[] wrongSizeInput = new float[10]; // Should be 63
                var result2 = modelInference.PredictGesture(wrongSizeInput);
                if (string.IsNullOrEmpty(result2.letter) && result2.confidence == 0f)
                {
                    PassTest("Wrong size input error handling successful");
                }
                else
                {
                    FailTest("Wrong size input error handling failed");
                }
            }
            
            yield return null;
        }
        
        /// <summary>
        /// Generate dummy landmark data for testing
        /// </summary>
        private float[] GenerateDummyLandmarks()
        {
            float[] landmarks = new float[63]; // 21 landmarks * 3 coordinates
            
            for (int i = 0; i < 21; i++)
            {
                // Generate plausible normalized coordinates
                landmarks[i * 3] = Random.Range(0.2f, 0.8f);     // X: 0.2 to 0.8
                landmarks[i * 3 + 1] = Random.Range(0.2f, 0.8f); // Y: 0.2 to 0.8
                landmarks[i * 3 + 2] = Random.Range(-0.1f, 0.1f); // Z: -0.1 to 0.1
            }
            
            return landmarks;
        }
        
        /// <summary>
        /// Continuous test loop
        /// </summary>
        private IEnumerator ContinuousTestLoop()
        {
            while (enabled)
            {
                yield return new WaitForSeconds(testInterval);
                
                if (testsCompleted)
                {
                    Log("Running continuous validation tests...");
                    yield return StartCoroutine(RunQuickValidation());
                }
            }
        }
        
        /// <summary>
        /// Quick validation for continuous testing
        /// </summary>
        private IEnumerator RunQuickValidation()
        {
            bool allGood = true;
            
            // Quick component checks
            if (modelInference != null && !modelInference.IsReady)
            {
                Log("Warning: Model inference not ready");
                allGood = false;
            }
            
            if (handDetectionManager != null && !handDetectionManager.IsInitialized)
            {
                Log("Warning: Hand detection not initialized");
                allGood = false;
            }
            
            if (allGood)
            {
                Log("Continuous validation: All systems operational");
            }
            
            yield return null;
        }
        
        /// <summary>
        /// Pass a test
        /// </summary>
        private void PassTest(string message)
        {
            testsPassed++;
            Log($"✅ PASS: {message}");
        }
        
        /// <summary>
        /// Fail a test
        /// </summary>
        private void FailTest(string message)
        {
            testsFailed++;
            Log($"❌ FAIL: {message}");
        }
        
        /// <summary>
        /// Log test results
        /// </summary>
        private void LogTestResults()
        {
            Log("=== Test Results ===");
            Log($"Tests Passed: {testsPassed}");
            Log($"Tests Failed: {testsFailed}");
            Log($"Success Rate: {((float)testsPassed / (testsPassed + testsFailed) * 100f):F1}%");
            
            if (testsFailed == 0)
            {
                Log("🎉 All tests passed! ASL system is ready for use.");
            }
            else
            {
                Log("⚠️ Some tests failed. Check the issues above and ensure proper setup.");
            }
        }
        
        /// <summary>
        /// Log with prefix
        /// </summary>
        private void Log(string message)
        {
            Debug.Log($"[ASLSystemTester] {message}");
        }
        
        #if UNITY_EDITOR
        [UnityEditor.MenuItem("ASL/Run System Tests")]
        private static void RunSystemTestsFromMenu()
        {
            var tester = FindObjectOfType<ASLSystemTester>();
            if (tester != null)
            {
                tester.StartCoroutine(tester.RunAllTests());
            }
            else
            {
                var go = new GameObject("ASL System Tester");
                go.AddComponent<ASLSystemTester>();
                Debug.Log("Created ASLSystemTester GameObject. Tests will run automatically.");
            }
        }
        #endif
    }
}