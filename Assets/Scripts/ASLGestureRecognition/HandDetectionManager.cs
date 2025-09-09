using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASLGestureRecognition
{
    /// <summary>
    /// Manages hand detection and coordinates with MediaPipe components
    /// Handles webcam initialization, landmark processing, and data normalization
    /// </summary>
    public class HandDetectionManager : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private int cameraIndex = 0;
        [SerializeField] private int targetWidth = 640;
        [SerializeField] private int targetHeight = 480;
        [SerializeField] private int targetFPS = 30;
        [SerializeField] private float cameraTimeout = 10f;
        
        [Header("Detection Settings")]
        [SerializeField] private float detectionConfidence = 0.7f;
        [SerializeField] private float trackingConfidence = 0.5f;
        [SerializeField] private bool enableMultiHand = false;
        [SerializeField] private int maxNumHands = 1;
        
        [Header("MediaPipe Integration")]
        [SerializeField] private bool useMediaPipe = true;
        [SerializeField] private MediaPipeHandTracker handTracker;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private RenderTexture debugRenderTexture;
        
        // Camera management
        private WebCamTexture webCamTexture;
        private bool isWebCamInitialized = false;
        private bool isDetectionActive = false;
        private Coroutine initializationCoroutine;
        
        // Hand landmark data (21 landmarks * 3 coordinates = 63 values)
        private float[] currentLandmarks = new float[63];
        private bool hasValidData = false;
        private float lastDetectionTime = 0f;
        
        // Events
        public System.Action<float[]> OnHandLandmarksDetected;
        public System.Action<string> OnDetectionError;
        public System.Action<bool> OnCameraStatusChanged;
        
        public bool IsInitialized => isWebCamInitialized && isDetectionActive;
        public bool HasValidHandData => hasValidData && Time.time - lastDetectionTime < 1f;
        public WebCamTexture CameraTexture => webCamTexture;
        
        void Start()
        {
            InitializeCamera();
        }
        
        void Update()
        {
            if (showDebugInfo && hasValidData)
            {
                DebugLogLandmarks();
            }
        }
        
        void OnDestroy()
        {
            CleanupCamera();
        }
        
        /// <summary>
        /// Initialize camera and detection system
        /// </summary>
        public void InitializeCamera()
        {
            if (initializationCoroutine != null)
            {
                StopCoroutine(initializationCoroutine);
            }
            
            initializationCoroutine = StartCoroutine(InitializeCameraCoroutine());
        }
        
        /// <summary>
        /// Coroutine to handle camera initialization with timeout
        /// </summary>
        private IEnumerator InitializeCameraCoroutine()
        {
            Debug.Log("Initializing camera for hand detection...");
            
            try
            {
                // Get available camera devices
                WebCamDevice[] devices = WebCamTexture.devices;
                
                if (devices.Length == 0)
                {
                    OnDetectionError?.Invoke("No camera devices found");
                    yield break;
                }
                
                // Use specified camera index or first available
                string deviceName = cameraIndex < devices.Length ? devices[cameraIndex].name : devices[0].name;
                
                Debug.Log($"Using camera: {deviceName}");
                
                // Create and configure WebCamTexture
                webCamTexture = new WebCamTexture(deviceName, targetWidth, targetHeight, targetFPS);
                
                // Start camera
                webCamTexture.Play();
                
                // Wait for camera to start with timeout
                float startTime = Time.time;
                while (!webCamTexture.isPlaying && Time.time - startTime < cameraTimeout)
                {
                    yield return null;
                }
                
                if (!webCamTexture.isPlaying)
                {
                    OnDetectionError?.Invoke($"Camera failed to start within {cameraTimeout} seconds");
                    yield break;
                }
                
                // Wait for camera to provide valid frames
                while (webCamTexture.width <= 16 && Time.time - startTime < cameraTimeout)
                {
                    yield return null;
                }
                
                if (webCamTexture.width <= 16)
                {
                    OnDetectionError?.Invoke("Camera failed to provide valid frames");
                    yield break;
                }
                
                isWebCamInitialized = true;
                Debug.Log($"Camera initialized successfully: {webCamTexture.width}x{webCamTexture.height} @ {webCamTexture.requestedFPS}fps");
                
                // Initialize MediaPipe hand tracking
                if (useMediaPipe)
                {
                    InitializeMediaPipe();
                }
                else
                {
                    // Start basic detection without MediaPipe
                    StartDetection();
                }
                
                OnCameraStatusChanged?.Invoke(true);
            }
            catch (System.Exception e)
            {
                OnDetectionError?.Invoke($"Camera initialization error: {e.Message}");
            }
        }
        
        /// <summary>
        /// Initialize MediaPipe hand tracking system
        /// </summary>
        private void InitializeMediaPipe()
        {
            try
            {
                if (handTracker == null)
                {
                    handTracker = GetComponent<MediaPipeHandTracker>();
                    if (handTracker == null)
                    {
                        handTracker = gameObject.AddComponent<MediaPipeHandTracker>();
                    }
                }
                
                // Configure MediaPipe settings
                handTracker.InitializeWithCamera(webCamTexture, detectionConfidence, trackingConfidence, maxNumHands);
                handTracker.OnLandmarksDetected += OnMediaPipeLandmarksReceived;
                handTracker.OnError += OnMediaPipeError;
                
                StartDetection();
                Debug.Log("MediaPipe hand tracking initialized");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"MediaPipe initialization failed: {e.Message}. Using fallback detection.");
                StartDetection();
            }
        }
        
        /// <summary>
        /// Start hand detection processing
        /// </summary>
        private void StartDetection()
        {
            isDetectionActive = true;
            Debug.Log("Hand detection started");
        }
        
        /// <summary>
        /// Handle landmarks received from MediaPipe
        /// </summary>
        private void OnMediaPipeLandmarksReceived(List<Vector3> landmarks)
        {
            if (landmarks == null || landmarks.Count != 21)
            {
                Debug.LogWarning($"Invalid landmarks received: expected 21, got {landmarks?.Count ?? 0}");
                return;
            }
            
            // Convert landmarks to normalized array (21 landmarks * 3 coordinates = 63 values)
            for (int i = 0; i < 21; i++)
            {
                var landmark = landmarks[i];
                currentLandmarks[i * 3] = landmark.x;     // X coordinate
                currentLandmarks[i * 3 + 1] = landmark.y; // Y coordinate  
                currentLandmarks[i * 3 + 2] = landmark.z; // Z coordinate
            }
            
            hasValidData = true;
            lastDetectionTime = Time.time;
            
            // Notify listeners
            OnHandLandmarksDetected?.Invoke(currentLandmarks);
        }
        
        /// <summary>
        /// Handle MediaPipe errors
        /// </summary>
        private void OnMediaPipeError(string error)
        {
            Debug.LogError($"MediaPipe Error: {error}");
            OnDetectionError?.Invoke($"MediaPipe: {error}");
        }
        
        /// <summary>
        /// Get current normalized landmarks for model inference
        /// </summary>
        public float[] GetNormalizedLandmarks()
        {
            if (!HasValidHandData)
                return null;
            
            // Return a copy to prevent external modification
            float[] result = new float[currentLandmarks.Length];
            System.Array.Copy(currentLandmarks, result, currentLandmarks.Length);
            return result;
        }
        
        /// <summary>
        /// Get specific landmark position
        /// </summary>
        public Vector3 GetLandmark(int landmarkIndex)
        {
            if (!HasValidHandData || landmarkIndex < 0 || landmarkIndex >= 21)
                return Vector3.zero;
            
            int baseIndex = landmarkIndex * 3;
            return new Vector3(
                currentLandmarks[baseIndex],
                currentLandmarks[baseIndex + 1], 
                currentLandmarks[baseIndex + 2]
            );
        }
        
        /// <summary>
        /// Check if a specific landmark is valid (within normal bounds)
        /// </summary>
        public bool IsLandmarkValid(int landmarkIndex)
        {
            if (!HasValidHandData || landmarkIndex < 0 || landmarkIndex >= 21)
                return false;
            
            var landmark = GetLandmark(landmarkIndex);
            
            // Basic validity check - landmarks should be in normalized space [0, 1] for x,y
            // Z can be negative (depth)
            return landmark.x >= 0f && landmark.x <= 1f && 
                   landmark.y >= 0f && landmark.y <= 1f &&
                   landmark.z >= -1f && landmark.z <= 1f;
        }
        
        /// <summary>
        /// Get hand confidence/quality score
        /// </summary>
        public float GetHandConfidence()
        {
            if (!HasValidHandData)
                return 0f;
            
            // Simple heuristic: check how many landmarks are valid
            int validCount = 0;
            for (int i = 0; i < 21; i++)
            {
                if (IsLandmarkValid(i))
                    validCount++;
            }
            
            return (float)validCount / 21f;
        }
        
        /// <summary>
        /// Reset detection system
        /// </summary>
        public void ResetDetection()
        {
            hasValidData = false;
            lastDetectionTime = 0f;
            
            if (handTracker != null)
            {
                handTracker.Reset();
            }
            
            Debug.Log("Hand detection reset");
        }
        
        /// <summary>
        /// Cleanup camera resources
        /// </summary>
        private void CleanupCamera()
        {
            try
            {
                isDetectionActive = false;
                isWebCamInitialized = false;
                
                if (initializationCoroutine != null)
                {
                    StopCoroutine(initializationCoroutine);
                }
                
                if (handTracker != null)
                {
                    handTracker.OnLandmarksDetected -= OnMediaPipeLandmarksReceived;
                    handTracker.OnError -= OnMediaPipeError;
                }
                
                if (webCamTexture != null)
                {
                    webCamTexture.Stop();
                    DestroyImmediate(webCamTexture);
                    webCamTexture = null;
                }
                
                OnCameraStatusChanged?.Invoke(false);
                Debug.Log("Camera resources cleaned up");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during camera cleanup: {e.Message}");
            }
        }
        
        /// <summary>
        /// Debug function to log current landmarks
        /// </summary>
        private void DebugLogLandmarks()
        {
            if (Time.frameCount % 30 == 0) // Log every 30 frames
            {
                string landmarkInfo = $"Hand Landmarks (confidence: {GetHandConfidence():F2}):\n";
                for (int i = 0; i < 21; i++)
                {
                    var landmark = GetLandmark(i);
                    landmarkInfo += $"  L{i:D2}: ({landmark.x:F3}, {landmark.y:F3}, {landmark.z:F3})\n";
                }
                Debug.Log(landmarkInfo);
            }
        }
        
        /// <summary>
        /// Get system status for debugging
        /// </summary>
        public string GetSystemStatus()
        {
            return $"Hand Detection Status:\n" +
                   $"- Camera: {(isWebCamInitialized ? "Ready" : "Not Ready")}\n" +
                   $"- Detection: {(isDetectionActive ? "Active" : "Inactive")}\n" +
                   $"- Valid Data: {hasValidData}\n" +
                   $"- MediaPipe: {(handTracker != null ? "Enabled" : "Disabled")}\n" +
                   $"- Hand Confidence: {GetHandConfidence():F2}";
        }
        
        #if UNITY_EDITOR
        [UnityEditor.MenuItem("ASL/Test Hand Detection")]
        private static void TestHandDetection()
        {
            var handDetection = FindObjectOfType<HandDetectionManager>();
            if (handDetection != null)
            {
                Debug.Log(handDetection.GetSystemStatus());
            }
            else
            {
                Debug.Log("No HandDetectionManager found in scene");
            }
        }
        #endif
    }
}