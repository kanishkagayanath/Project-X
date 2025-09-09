using UnityEngine;

namespace ASLGestureRecognition
{
    /// <summary>
    /// Configuration class for MediaPipe Hand Landmark Detection
    /// Defines parameters and settings for hand tracking accuracy and performance
    /// </summary>
    [CreateAssetMenu(fileName = "HandLandmarkDetectionConfig", menuName = "ASL/Hand Detection Config")]
    public class HandLandmarkDetectionConfig : ScriptableObject
    {
        [Header("Detection Parameters")]
        [Tooltip("Minimum confidence value for hand detection to be considered successful")]
        [Range(0.1f, 1.0f)]
        public float detectionConfidence = 0.7f;
        
        [Tooltip("Minimum confidence value for hand landmarks tracking to be considered successful")]
        [Range(0.1f, 1.0f)]
        public float trackingConfidence = 0.5f;
        
        [Tooltip("Maximum number of hands to detect")]
        [Range(1, 2)]
        public int maxNumHands = 1;
        
        [Header("Processing Settings")]
        [Tooltip("Whether to treat input images as static images (slower but more accurate)")]
        public bool staticImageMode = false;
        
        [Tooltip("Whether to refine landmarks around the hand")]
        public bool refineLandmarks = true;
        
        [Tooltip("Processing interval in seconds (lower = more frequent processing)")]
        [Range(0.016f, 0.1f)]
        public float processingInterval = 0.033f; // ~30 FPS
        
        [Header("Input Image Settings")]
        [Tooltip("Input image width for processing")]
        public int inputWidth = 640;
        
        [Tooltip("Input image height for processing")]
        public int inputHeight = 480;
        
        [Tooltip("Whether to flip the input image horizontally")]
        public bool flipHorizontally = true;
        
        [Header("Smoothing and Filtering")]
        [Tooltip("Enable landmark smoothing to reduce jitter")]
        public bool enableSmoothing = true;
        
        [Tooltip("Smoothing factor (0 = no smoothing, 1 = maximum smoothing)")]
        [Range(0.0f, 1.0f)]
        public float smoothingFactor = 0.7f;
        
        [Tooltip("Enable outlier filtering to remove invalid landmarks")]
        public bool enableOutlierFilter = true;
        
        [Tooltip("Maximum allowed movement between frames (normalized coordinates)")]
        [Range(0.01f, 0.5f)]
        public float maxMovementThreshold = 0.1f;
        
        [Header("Performance Optimization")]
        [Tooltip("Enable multi-threading for processing (if supported)")]
        public bool enableMultiThreading = true;
        
        [Tooltip("Use GPU acceleration when available")]
        public bool useGPUAcceleration = true;
        
        [Tooltip("Reduce processing quality for better performance")]
        public bool enablePerformanceMode = false;
        
        [Header("Debug and Visualization")]
        [Tooltip("Enable debug logging")]
        public bool enableDebugLogging = false;
        
        [Tooltip("Show landmark visualization")]
        public bool showLandmarkVisualization = false;
        
        [Tooltip("Show hand bounding box")]
        public bool showBoundingBox = false;
        
        [Tooltip("Color for landmark visualization")]
        public Color landmarkColor = Color.red;
        
        [Tooltip("Color for hand connections")]
        public Color connectionColor = Color.blue;
        
        [Header("Advanced Settings")]
        [Tooltip("Minimum hand detection area (normalized)")]
        [Range(0.01f, 0.5f)]
        public float minDetectionArea = 0.05f;
        
        [Tooltip("Maximum hand detection area (normalized)")]
        [Range(0.1f, 1.0f)]
        public float maxDetectionArea = 0.8f;
        
        [Tooltip("Hand presence confidence threshold")]
        [Range(0.1f, 1.0f)]
        public float presenceThreshold = 0.5f;
        
        [Tooltip("Tracking timeout in seconds")]
        [Range(1.0f, 10.0f)]
        public float trackingTimeout = 3.0f;
        
        /// <summary>
        /// Validate configuration values
        /// </summary>
        void OnValidate()
        {
            // Ensure detection confidence is not higher than tracking confidence by too much
            if (detectionConfidence > trackingConfidence + 0.3f)
            {
                trackingConfidence = Mathf.Min(1.0f, detectionConfidence + 0.1f);
            }
            
            // Ensure reasonable processing interval
            processingInterval = Mathf.Max(0.016f, processingInterval);
            
            // Ensure min area is less than max area
            minDetectionArea = Mathf.Min(minDetectionArea, maxDetectionArea - 0.01f);
        }
        
        /// <summary>
        /// Create default configuration
        /// </summary>
        public static HandLandmarkDetectionConfig CreateDefault()
        {
            var config = CreateInstance<HandLandmarkDetectionConfig>();
            
            // Set default values
            config.detectionConfidence = 0.7f;
            config.trackingConfidence = 0.5f;
            config.maxNumHands = 1;
            config.staticImageMode = false;
            config.refineLandmarks = true;
            config.processingInterval = 0.033f;
            config.inputWidth = 640;
            config.inputHeight = 480;
            config.flipHorizontally = true;
            config.enableSmoothing = true;
            config.smoothingFactor = 0.7f;
            config.enableOutlierFilter = true;
            config.maxMovementThreshold = 0.1f;
            config.enableMultiThreading = true;
            config.useGPUAcceleration = true;
            config.enablePerformanceMode = false;
            config.enableDebugLogging = false;
            config.showLandmarkVisualization = false;
            config.showBoundingBox = false;
            config.landmarkColor = Color.red;
            config.connectionColor = Color.blue;
            config.minDetectionArea = 0.05f;
            config.maxDetectionArea = 0.8f;
            config.presenceThreshold = 0.5f;
            config.trackingTimeout = 3.0f;
            
            return config;
        }
        
        /// <summary>
        /// Create performance-optimized configuration
        /// </summary>
        public static HandLandmarkDetectionConfig CreatePerformanceConfig()
        {
            var config = CreateDefault();
            
            config.detectionConfidence = 0.6f;
            config.trackingConfidence = 0.4f;
            config.processingInterval = 0.05f; // 20 FPS
            config.inputWidth = 320;
            config.inputHeight = 240;
            config.enableSmoothing = false;
            config.enableOutlierFilter = false;
            config.enablePerformanceMode = true;
            config.useGPUAcceleration = true;
            
            return config;
        }
        
        /// <summary>
        /// Create accuracy-optimized configuration
        /// </summary>
        public static HandLandmarkDetectionConfig CreateAccuracyConfig()
        {
            var config = CreateDefault();
            
            config.detectionConfidence = 0.8f;
            config.trackingConfidence = 0.7f;
            config.staticImageMode = true;
            config.refineLandmarks = true;
            config.processingInterval = 0.02f; // 50 FPS
            config.inputWidth = 1280;
            config.inputHeight = 720;
            config.enableSmoothing = true;
            config.smoothingFactor = 0.8f;
            config.enableOutlierFilter = true;
            config.maxMovementThreshold = 0.05f;
            
            return config;
        }
        
        /// <summary>
        /// Get configuration summary as string
        /// </summary>
        public string GetConfigSummary()
        {
            return $"Hand Detection Config:\n" +
                   $"- Detection Confidence: {detectionConfidence:F2}\n" +
                   $"- Tracking Confidence: {trackingConfidence:F2}\n" +
                   $"- Max Hands: {maxNumHands}\n" +
                   $"- Input Size: {inputWidth}x{inputHeight}\n" +
                   $"- Processing Interval: {processingInterval:F3}s\n" +
                   $"- Smoothing: {(enableSmoothing ? $"Enabled ({smoothingFactor:F2})" : "Disabled")}\n" +
                   $"- Performance Mode: {enablePerformanceMode}\n" +
                   $"- GPU Acceleration: {useGPUAcceleration}";
        }
        
        /// <summary>
        /// Copy configuration values from another config
        /// </summary>
        public void CopyFrom(HandLandmarkDetectionConfig other)
        {
            if (other == null) return;
            
            detectionConfidence = other.detectionConfidence;
            trackingConfidence = other.trackingConfidence;
            maxNumHands = other.maxNumHands;
            staticImageMode = other.staticImageMode;
            refineLandmarks = other.refineLandmarks;
            processingInterval = other.processingInterval;
            inputWidth = other.inputWidth;
            inputHeight = other.inputHeight;
            flipHorizontally = other.flipHorizontally;
            enableSmoothing = other.enableSmoothing;
            smoothingFactor = other.smoothingFactor;
            enableOutlierFilter = other.enableOutlierFilter;
            maxMovementThreshold = other.maxMovementThreshold;
            enableMultiThreading = other.enableMultiThreading;
            useGPUAcceleration = other.useGPUAcceleration;
            enablePerformanceMode = other.enablePerformanceMode;
            enableDebugLogging = other.enableDebugLogging;
            showLandmarkVisualization = other.showLandmarkVisualization;
            showBoundingBox = other.showBoundingBox;
            landmarkColor = other.landmarkColor;
            connectionColor = other.connectionColor;
            minDetectionArea = other.minDetectionArea;
            maxDetectionArea = other.maxDetectionArea;
            presenceThreshold = other.presenceThreshold;
            trackingTimeout = other.trackingTimeout;
        }
        
        #if UNITY_EDITOR
        [UnityEditor.MenuItem("ASL/Create Hand Detection Configs")]
        private static void CreateHandDetectionConfigs()
        {
            // Create default config
            var defaultConfig = CreateDefault();
            UnityEditor.AssetDatabase.CreateAsset(defaultConfig, "Assets/ASL_HandDetectionConfig_Default.asset");
            
            // Create performance config
            var performanceConfig = CreatePerformanceConfig();
            UnityEditor.AssetDatabase.CreateAsset(performanceConfig, "Assets/ASL_HandDetectionConfig_Performance.asset");
            
            // Create accuracy config
            var accuracyConfig = CreateAccuracyConfig();
            UnityEditor.AssetDatabase.CreateAsset(accuracyConfig, "Assets/ASL_HandDetectionConfig_Accuracy.asset");
            
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            
            Debug.Log("Hand detection configuration assets created!");
        }
        #endif
    }
}