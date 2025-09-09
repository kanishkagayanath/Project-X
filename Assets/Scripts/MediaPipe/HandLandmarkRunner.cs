using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASLGestureRecognition
{
    /// <summary>
    /// Main runner class for MediaPipe hand landmark detection
    /// Orchestrates the hand detection pipeline using configuration settings
    /// </summary>
    public class HandLandmarkRunner : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private HandLandmarkDetectionConfig config;
        [SerializeField] private MediaPipeHandTracker handTracker;
        
        [Header("Input Source")]
        [SerializeField] private Camera inputCamera;
        [SerializeField] private WebCamTexture webCamera;
        [SerializeField] private Texture2D staticImage;
        [SerializeField] private InputSourceType inputSource = InputSourceType.WebCamera;
        
        [Header("Output")]
        [SerializeField] private RenderTexture outputTexture;
        [SerializeField] private Material visualizationMaterial;
        
        // Runtime state
        private bool isRunning = false;
        private bool isInitialized = false;
        private Coroutine runnerCoroutine;
        private Texture2D processingTexture;
        private RenderTexture tempRenderTexture;
        
        // Landmark data
        private List<Vector3> latestLandmarks = new List<Vector3>();
        private float landmarkConfidence = 0f;
        private bool hasValidDetection = false;
        private float lastDetectionTime = 0f;
        
        // Performance metrics
        private float processingTime = 0f;
        private int frameCount = 0;
        private float fps = 0f;
        
        // Events
        public System.Action<List<Vector3>, float> OnLandmarksDetected;
        public System.Action OnDetectionLost;
        public System.Action<string> OnError;
        public System.Action<bool> OnRunningStateChanged;
        
        public enum InputSourceType
        {
            WebCamera,
            StaticImage,
            Camera,
            RenderTexture
        }
        
        public bool IsRunning => isRunning;
        public bool IsInitialized => isInitialized;
        public bool HasValidDetection => hasValidDetection && (Time.time - lastDetectionTime) < config.trackingTimeout;
        public List<Vector3> LatestLandmarks => new List<Vector3>(latestLandmarks);
        public float LandmarkConfidence => landmarkConfidence;
        public float FPS => fps;
        
        void Start()
        {
            InitializeRunner();
        }
        
        void Update()
        {
            UpdatePerformanceMetrics();
            
            if (config != null && config.enableDebugLogging && Time.frameCount % 60 == 0)
            {
                LogStatus();
            }
        }
        
        void OnDestroy()
        {
            StopRunner();
            CleanupResources();
        }
        
        /// <summary>
        /// Initialize the hand landmark runner
        /// </summary>
        public void InitializeRunner()
        {
            try
            {
                // Load default config if none assigned
                if (config == null)
                {
                    config = HandLandmarkDetectionConfig.CreateDefault();
                    Debug.LogWarning("No config assigned, using default configuration");
                }
                
                // Initialize hand tracker
                if (handTracker == null)
                {
                    handTracker = GetComponent<MediaPipeHandTracker>();
                    if (handTracker == null)
                    {
                        handTracker = gameObject.AddComponent<MediaPipeHandTracker>();
                    }
                }
                
                // Setup event handlers
                handTracker.OnLandmarksDetected += OnHandLandmarksReceived;
                handTracker.OnError += OnHandTrackerError;
                handTracker.OnTrackingStatusChanged += OnTrackingStatusChanged;
                
                // Initialize input source
                InitializeInputSource();
                
                // Initialize processing resources
                InitializeProcessingResources();
                
                isInitialized = true;
                Debug.Log("Hand landmark runner initialized successfully");
            }
            catch (System.Exception e)
            {
                OnError?.Invoke($"Runner initialization failed: {e.Message}");
                Debug.LogError($"HandLandmarkRunner initialization error: {e.Message}");
            }
        }
        
        /// <summary>
        /// Initialize the input source based on configuration
        /// </summary>
        private void InitializeInputSource()
        {
            switch (inputSource)
            {
                case InputSourceType.WebCamera:
                    InitializeWebCamera();
                    break;
                case InputSourceType.Camera:
                    InitializeCamera();
                    break;
                case InputSourceType.StaticImage:
                    InitializeStaticImage();
                    break;
                case InputSourceType.RenderTexture:
                    InitializeRenderTexture();
                    break;
            }
        }
        
        /// <summary>
        /// Initialize web camera input
        /// </summary>
        private void InitializeWebCamera()
        {
            if (webCamera == null)
            {
                WebCamDevice[] devices = WebCamTexture.devices;
                if (devices.Length > 0)
                {
                    webCamera = new WebCamTexture(devices[0].name, config.inputWidth, config.inputHeight, 30);
                }
                else
                {
                    throw new System.Exception("No web camera devices found");
                }
            }
            
            if (!webCamera.isPlaying)
            {
                webCamera.Play();
            }
            
            Debug.Log($"Web camera initialized: {webCamera.deviceName}");
        }
        
        /// <summary>
        /// Initialize Unity Camera input
        /// </summary>
        private void InitializeCamera()
        {
            if (inputCamera == null)
            {
                inputCamera = Camera.main;
                if (inputCamera == null)
                {
                    throw new System.Exception("No input camera assigned and no main camera found");
                }
            }
            
            // Create render texture for camera output
            tempRenderTexture = new RenderTexture(config.inputWidth, config.inputHeight, 24);
            inputCamera.targetTexture = tempRenderTexture;
            
            Debug.Log($"Camera input initialized: {inputCamera.name}");
        }
        
        /// <summary>
        /// Initialize static image input
        /// </summary>
        private void InitializeStaticImage()
        {
            if (staticImage == null)
            {
                throw new System.Exception("No static image assigned");
            }
            
            Debug.Log($"Static image initialized: {staticImage.width}x{staticImage.height}");
        }
        
        /// <summary>
        /// Initialize render texture input
        /// </summary>
        private void InitializeRenderTexture()
        {
            if (outputTexture == null)
            {
                outputTexture = new RenderTexture(config.inputWidth, config.inputHeight, 24);
            }
            
            Debug.Log($"Render texture initialized: {outputTexture.width}x{outputTexture.height}");
        }
        
        /// <summary>
        /// Initialize processing resources
        /// </summary>
        private void InitializeProcessingResources()
        {
            // Create processing texture
            processingTexture = new Texture2D(config.inputWidth, config.inputHeight, TextureFormat.RGB24, false);
            
            // Initialize visualization material if needed
            if (config.showLandmarkVisualization && visualizationMaterial == null)
            {
                visualizationMaterial = new Material(Shader.Find("Unlit/Texture"));
            }
        }
        
        /// <summary>
        /// Start the hand landmark detection runner
        /// </summary>
        public void StartRunner()
        {
            if (!isInitialized)
            {
                OnError?.Invoke("Runner not initialized");
                return;
            }
            
            if (isRunning)
            {
                Debug.LogWarning("Runner is already running");
                return;
            }
            
            try
            {
                // Initialize hand tracker with current input
                InitializeHandTracker();
                
                // Start processing coroutine
                runnerCoroutine = StartCoroutine(ProcessingLoop());
                
                isRunning = true;
                OnRunningStateChanged?.Invoke(true);
                
                Debug.Log("Hand landmark runner started");
            }
            catch (System.Exception e)
            {
                OnError?.Invoke($"Failed to start runner: {e.Message}");
                Debug.LogError($"HandLandmarkRunner start error: {e.Message}");
            }
        }
        
        /// <summary>
        /// Initialize hand tracker with current configuration
        /// </summary>
        private void InitializeHandTracker()
        {
            WebCamTexture cameraSource = null;
            
            switch (inputSource)
            {
                case InputSourceType.WebCamera:
                    cameraSource = webCamera;
                    break;
                case InputSourceType.Camera:
                case InputSourceType.RenderTexture:
                case InputSourceType.StaticImage:
                    // For non-webcam sources, we'll handle frame processing differently
                    break;
            }
            
            if (cameraSource != null)
            {
                handTracker.InitializeWithCamera(
                    cameraSource,
                    config.detectionConfidence,
                    config.trackingConfidence,
                    config.maxNumHands
                );
            }
        }
        
        /// <summary>
        /// Main processing loop
        /// </summary>
        private IEnumerator ProcessingLoop()
        {
            while (isRunning && isInitialized)
            {
                float startTime = Time.realtimeSinceStartup;
                
                try
                {
                    // Process current frame based on input source
                    yield return StartCoroutine(ProcessCurrentFrame());
                }
                catch (System.Exception e)
                {
                    OnError?.Invoke($"Processing error: {e.Message}");
                }
                
                // Calculate processing time
                processingTime = Time.realtimeSinceStartup - startTime;
                
                // Wait for next processing interval
                yield return new WaitForSeconds(Mathf.Max(0f, config.processingInterval - processingTime));
            }
        }
        
        /// <summary>
        /// Process the current frame from the input source
        /// </summary>
        private IEnumerator ProcessCurrentFrame()
        {
            Texture2D frameTexture = GetCurrentFrameTexture();
            
            if (frameTexture == null)
            {
                yield break;
            }
            
            // For non-webcam sources, we need to manually process the frame
            // This would normally be handled by MediaPipe integration
            if (inputSource != InputSourceType.WebCamera)
            {
                yield return StartCoroutine(ProcessFrameTexture(frameTexture));
            }
            
            frameCount++;
        }
        
        /// <summary>
        /// Get current frame texture based on input source
        /// </summary>
        private Texture2D GetCurrentFrameTexture()
        {
            switch (inputSource)
            {
                case InputSourceType.WebCamera:
                    if (webCamera != null && webCamera.isPlaying)
                    {
                        return ConvertWebCamToTexture2D(webCamera);
                    }
                    break;
                    
                case InputSourceType.Camera:
                    if (inputCamera != null && tempRenderTexture != null)
                    {
                        return ConvertRenderTextureToTexture2D(tempRenderTexture);
                    }
                    break;
                    
                case InputSourceType.StaticImage:
                    return staticImage;
                    
                case InputSourceType.RenderTexture:
                    if (outputTexture != null)
                    {
                        return ConvertRenderTextureToTexture2D(outputTexture);
                    }
                    break;
            }
            
            return null;
        }
        
        /// <summary>
        /// Convert WebCamTexture to Texture2D
        /// </summary>
        private Texture2D ConvertWebCamToTexture2D(WebCamTexture webCam)
        {
            if (processingTexture.width != webCam.width || processingTexture.height != webCam.height)
            {
                DestroyImmediate(processingTexture);
                processingTexture = new Texture2D(webCam.width, webCam.height, TextureFormat.RGB24, false);
            }
            
            processingTexture.SetPixels(webCam.GetPixels());
            processingTexture.Apply();
            
            return processingTexture;
        }
        
        /// <summary>
        /// Convert RenderTexture to Texture2D
        /// </summary>
        private Texture2D ConvertRenderTextureToTexture2D(RenderTexture renderTexture)
        {
            RenderTexture.active = renderTexture;
            
            if (processingTexture.width != renderTexture.width || processingTexture.height != renderTexture.height)
            {
                DestroyImmediate(processingTexture);
                processingTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
            }
            
            processingTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            processingTexture.Apply();
            
            RenderTexture.active = null;
            
            return processingTexture;
        }
        
        /// <summary>
        /// Process frame texture (placeholder for actual MediaPipe processing)
        /// </summary>
        private IEnumerator ProcessFrameTexture(Texture2D frameTexture)
        {
            // This would normally send the frame to MediaPipe for processing
            // For now, we rely on the MediaPipeHandTracker simulation
            yield return null;
        }
        
        /// <summary>
        /// Stop the hand landmark detection runner
        /// </summary>
        public void StopRunner()
        {
            if (!isRunning)
                return;
            
            try
            {
                isRunning = false;
                
                if (runnerCoroutine != null)
                {
                    StopCoroutine(runnerCoroutine);
                    runnerCoroutine = null;
                }
                
                if (handTracker != null)
                {
                    handTracker.Stop();
                }
                
                OnRunningStateChanged?.Invoke(false);
                
                Debug.Log("Hand landmark runner stopped");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error stopping runner: {e.Message}");
            }
        }
        
        /// <summary>
        /// Event handler for hand landmarks received
        /// </summary>
        private void OnHandLandmarksReceived(List<Vector3> landmarks)
        {
            latestLandmarks = new List<Vector3>(landmarks);
            landmarkConfidence = handTracker.GetTrackingQuality();
            hasValidDetection = true;
            lastDetectionTime = Time.time;
            
            OnLandmarksDetected?.Invoke(latestLandmarks, landmarkConfidence);
        }
        
        /// <summary>
        /// Event handler for hand tracker errors
        /// </summary>
        private void OnHandTrackerError(string error)
        {
            OnError?.Invoke($"Hand Tracker: {error}");
        }
        
        /// <summary>
        /// Event handler for tracking status changes
        /// </summary>
        private void OnTrackingStatusChanged(bool isTracking)
        {
            if (!isTracking)
            {
                hasValidDetection = false;
                OnDetectionLost?.Invoke();
            }
        }
        
        /// <summary>
        /// Update performance metrics
        /// </summary>
        private void UpdatePerformanceMetrics()
        {
            if (frameCount > 0 && Time.time > 1f)
            {
                fps = frameCount / Time.time;
            }
        }
        
        /// <summary>
        /// Log current status for debugging
        /// </summary>
        private void LogStatus()
        {
            string status = $"HandLandmarkRunner Status:\n" +
                           $"- Running: {isRunning}\n" +
                           $"- Valid Detection: {hasValidDetection}\n" +
                           $"- FPS: {fps:F1}\n" +
                           $"- Processing Time: {processingTime * 1000:F1}ms\n" +
                           $"- Landmark Confidence: {landmarkConfidence:F2}";
            
            Debug.Log(status);
        }
        
        /// <summary>
        /// Cleanup resources
        /// </summary>
        private void CleanupResources()
        {
            try
            {
                if (processingTexture != null)
                {
                    DestroyImmediate(processingTexture);
                }
                
                if (tempRenderTexture != null)
                {
                    tempRenderTexture.Release();
                    DestroyImmediate(tempRenderTexture);
                }
                
                if (webCamera != null && webCamera.isPlaying)
                {
                    webCamera.Stop();
                }
                
                // Cleanup event handlers
                if (handTracker != null)
                {
                    handTracker.OnLandmarksDetected -= OnHandLandmarksReceived;
                    handTracker.OnError -= OnHandTrackerError;
                    handTracker.OnTrackingStatusChanged -= OnTrackingStatusChanged;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during resource cleanup: {e.Message}");
            }
        }
        
        /// <summary>
        /// Get current runner status
        /// </summary>
        public string GetRunnerStatus()
        {
            return $"Runner Status:\n" +
                   $"- Initialized: {isInitialized}\n" +
                   $"- Running: {isRunning}\n" +
                   $"- Input Source: {inputSource}\n" +
                   $"- Valid Detection: {hasValidDetection}\n" +
                   $"- FPS: {fps:F1}\n" +
                   $"- Confidence: {landmarkConfidence:F2}\n" +
                   $"- Config: {(config != null ? "Loaded" : "None")}";
        }
        
        #if UNITY_EDITOR
        [UnityEditor.MenuItem("ASL/Test Landmark Runner")]
        private static void TestLandmarkRunner()
        {
            var runner = FindObjectOfType<HandLandmarkRunner>();
            if (runner != null)
            {
                Debug.Log(runner.GetRunnerStatus());
            }
            else
            {
                Debug.Log("No HandLandmarkRunner found in scene");
            }
        }
        #endif
    }
}