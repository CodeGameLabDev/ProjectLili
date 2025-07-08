using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace LetterMatch
{
    public class DrawingController : MonoBehaviour
    {
        [TabGroup("Drawing Settings")]
        public LineRenderer linePrefab;  // Assign a line renderer prefab in inspector
        public RawImage drawingCanvas;
        public Camera drawingCamera;
        public float lineWidth = 50f;
        public ColorPalette colorPalette; // Reference to the color palette
        public float drawingSpeed = 1f;
        public bool syncToMusic = true;
    
        [TabGroup("Music Sync")]
        public float syncThreshold = 0.1f;
        public bool playSampleWhileDrawing = true;
        public float sampleVolumeMultiplier = 0.5f;
    
        [TabGroup("Visual Effects")]
        public bool showDrawingTrail = true;
        public ParticleSystem drawingParticles;
        public bool flashOnDraw = false;
        public Color flashColor = Color.yellow;
        public GameObject circlePrefab; // Prefab for music marker circles
        public float circleSize = 20f; // Size of the circles
        public float pulseSpeed = 2f; // Speed of pulse animation
        public float pulseScale = 1.5f; // How much the circle scales during pulse
    
        [TabGroup("Debug"), ReadOnly]
        public bool isDrawing = false;
        public bool isInitialized = false;
        public Vector2 currentDrawPosition;
        public List<Vector2> currentDrawPoints = new List<Vector2>();
        public LetterMatchController currentDraggedLetter;
    
        private Canvas canvas;
        private RectTransform canvasRect;
        private float lastDrawTime;
        private Coroutine drawingCoroutine;
        private LetterMatchGameManager gameManager;
        private float canvasScaleFactor = 1f;
        private Dictionary<string, LineRenderer> activeLines = new Dictionary<string, LineRenderer>();
        private LineRenderer currentLine;
        private List<HitPoint> hitPoints = new List<HitPoint>(); // Track all hit points

        public static DrawingController Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        void Start()
        {
            InitializeDrawing();
        }
    
        void InitializeDrawing()
        {
            canvas = GetComponentInParent<Canvas>();
            canvasRect = canvas?.GetComponent<RectTransform>();
            
            if (canvas != null)
            {
                canvasScaleFactor = canvas.scaleFactor;
                Debug.Log($"Canvas scale factor: {canvasScaleFactor}");
            }

            if (linePrefab == null)
            {
                Debug.LogError("Line prefab is not assigned!");
                return;
            }
        
            gameManager = LetterMatchGameManager.Instance;
            isInitialized = true;
        
            Debug.Log("DrawingController initialized");
        }

        LineRenderer CreateNewLine()
        {
            LineRenderer newLine = Instantiate(linePrefab, transform);
            SetupLineRenderer(newLine);
            return newLine;
        }
    
       void SetupLineRenderer(LineRenderer lineRenderer)
{
    if (lineRenderer != null)
    {
        // Use a higher quality shader for better rendering
        var material = new Material(Shader.Find("Sprites/Default"));
        
        // Enable proper alpha blending for smooth edges
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        
        // Enable alpha test for better edge quality
        material.EnableKeyword("_ALPHATEST_ON");
        material.SetFloat("_Cutoff", 0.01f);
        
        // Get color from palette based on letter ID
        Color lineColor = GetColorForLetter(currentDraggedLetter);
        material.SetColor("_Color", lineColor);
        lineRenderer.material = material;

        // Keep your existing width settings
        lineRenderer.startWidth = lineWidth / canvasScaleFactor;
        lineRenderer.endWidth = lineWidth / canvasScaleFactor;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;
        
        // Increase vertices for smoother curves and better quality
        lineRenderer.numCapVertices = 16;      // Increased from 5
        lineRenderer.numCornerVertices = 16;   // Increased from 5
        
        // Quality settings
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.textureMode = LineTextureMode.Stretch;  // Changed from Tile for better quality
        lineRenderer.alignment = LineAlignment.TransformZ;   // Changed for better alignment
        
        // Additional quality improvements
        lineRenderer.generateLightingData = true;  // Better lighting interaction
        lineRenderer.allowOcclusionWhenDynamic = false;  // Prevent occlusion issues
        
        // Force high quality rendering
        lineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
    }
}
    
        Color GetColorForLetter(LetterMatchController letter)
        {
            if (colorPalette == null || letter == null)
                return Color.yellow; // Default color if no palette or letter

            // Get letter index from its ID
            // Assuming letter IDs are like "A", "B", "C" or "a", "b", "c"
            string id = letter.GetId().ToUpper();
            int index = id[0] - 'A'; // Convert letter to index (A=0, B=1, etc.)
            
            return colorPalette.GetColor(index);
        }
    
        Vector3 ConvertToWorldSpace(Vector2 screenPosition)
        {
            if (drawingCamera == null) return Vector3.zero;
            
            Vector3 worldPos = drawingCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
            return worldPos;
        }
    
        public void OnLetterDragStart(LetterMatchController letter)
        {
            if (letter == null) return;

            currentDraggedLetter = letter;
            isDrawing = true;
            currentDrawPoints.Clear();
            lastDrawTime = Time.time;

            // Create new line for this drag
            string lineKey = $"line_{letter.GetId()}_{Time.time}";
            currentLine = CreateNewLine();
            activeLines[lineKey] = currentLine;
        
            if (drawingCoroutine != null)
                StopCoroutine(drawingCoroutine);
            drawingCoroutine = StartCoroutine(DrawingRoutine());
        
            if (playSampleWhileDrawing)
            {
                letter.PlayChannelMusic();
            }
            
            // Notify audio manager that this line is starting
            NotifyLineStarted(letter.GetChannelId());
        
            Debug.Log($"Started drawing with letter: {letter.letterId}");
        }
    
        private void NotifyLineStarted(string channelId)
        {
            var audioManager = FindObjectOfType<KoreographerAudioManager>();
            if (audioManager != null)
            {
                audioManager.OnLineStarted(channelId);
            }
        }
    
        public void OnLetterDragging(LetterMatchController letter, Vector2 screenPosition)
        {
            if (!isDrawing || letter != currentDraggedLetter || currentLine == null) return;
        
            currentDrawPosition = screenPosition;
            Vector3 worldPosition = ConvertToWorldSpace(screenPosition);
        
            if (currentDrawPoints.Count == 0 || Vector3.Distance(worldPosition, ConvertToWorldSpace(currentDrawPoints[currentDrawPoints.Count - 1])) > (lineWidth * 0.1f))
            {
                currentDrawPoints.Add(screenPosition);
                UpdateCurrentLine();
                TriggerDrawingEffects(screenPosition);
            }
        }
    
        void UpdateCurrentLine()
        {
            if (currentLine == null || currentDrawPoints.Count == 0) return;
        
            currentLine.positionCount = currentDrawPoints.Count;
            
            for (int i = 0; i < currentDrawPoints.Count; i++)
            {
                Vector3 worldPos = ConvertToWorldSpace(currentDrawPoints[i]);
                currentLine.SetPosition(i, worldPos);
            }
        }
    
        public void OnLetterDragEnd(LetterMatchController letter)
        {
            if (!isDrawing || letter != currentDraggedLetter) return;

            isDrawing = false;
            currentDraggedLetter = null;

            if (drawingCoroutine != null)
                StopCoroutine(drawingCoroutine);

            FinalizeDrawing();
            
            // Notify audio manager that this line is completed
            NotifyLineCompleted(letter.GetChannelId());
            
            Debug.Log($"Finished drawing with letter: {letter.letterId}");
        }
        
        private void NotifyLineCompleted(string channelId)
        {
            var audioManager = FindObjectOfType<KoreographerAudioManager>();
            if (audioManager != null)
            {
                audioManager.OnLineCompleted(channelId);
            }
        }
    
        IEnumerator DrawingRoutine()
        {
            while (isDrawing)
            {
                if (showDrawingTrail && currentDraggedLetter != null)
                {
                    UpdateDrawingTrail();
                }
            
                yield return new WaitForSeconds(0.016f); // ~60 FPS
            }
        }
    
        void UpdateDrawingTrail()
        {
            if (currentDraggedLetter != null && gameManager != null)
            {
                float musicTime = gameManager.currentMusicTime;
                float trailIntensity = Mathf.Sin(musicTime * 2f) * 0.5f + 0.5f;
            
                if (currentLine != null)
                {
                    currentLine.startColor = Color.Lerp(GetColorForLetter(currentDraggedLetter), flashColor, trailIntensity);
                    currentLine.endColor = currentLine.startColor;
                }
            }
        }
    
        public void SpawnMusicCircleOnLine(string channelId)
        {
            if (currentDraggedLetter == null) {
                Debug.LogWarning($"[Circle] No letter is being drawn. Event for channel: {channelId}");
                return;
            }
            if (currentDraggedLetter.channelId != channelId) {
                Debug.LogWarning($"[Circle] Channel mismatch: Drawing {currentDraggedLetter.channelId}, Event {channelId}");
                return;
            }

            if (currentLine == null || circlePrefab == null) return;

            // Use current cursor position instead of random point
            Vector2 circlePosition = currentDrawPosition;
            
            // Create the circle as a UI element on the canvas
            GameObject circle = Instantiate(circlePrefab, canvas.transform);
            
            // Position the circle on the canvas using the screen position
            RectTransform circleRect = circle.GetComponent<RectTransform>();
            if (circleRect != null)
            {
                // Convert screen position to canvas position
                Vector2 canvasPosition;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, 
                    circlePosition, 
                    canvas.worldCamera, 
                    out canvasPosition
                );
                circleRect.anchoredPosition = canvasPosition;
            }
            
            // Set circle size for UI
            RectTransform circleRectTransform = circle.GetComponent<RectTransform>();
            if (circleRectTransform != null)
            {
                circleRectTransform.sizeDelta = Vector2.one * circleSize;
            }
            
            // Get or add HitPoint component and initialize it
            HitPoint hitPoint = circle.GetComponent<HitPoint>();
            if (hitPoint == null)
            {
                hitPoint = circle.AddComponent<HitPoint>();
            }
            
            // Get current music time for this channel
            float musicTime = GetCurrentMusicTimeForChannel(channelId);
            
            Color lineColor = GetColorForLetter(currentDraggedLetter);
            hitPoint.Initialize(channelId, musicTime, lineColor);
            
            // Add to tracking list
            hitPoints.Add(hitPoint);
            
            Debug.Log($"Registered {channelId} hit point on UI at cursor position {circlePosition} at music time {musicTime}");
        }
        
        private float GetCurrentMusicTimeForChannel(string channelId)
        {
            // Try to get the current music time from the audio manager
            var audioManager = FindObjectOfType<KoreographerAudioManager>();
            if (audioManager != null)
            {
                return audioManager.GetCurrentMusicTimeForChannel(channelId);
            }
            
            // Fallback to game time if we can't get music time
            return Time.time;
        }

        public void AnimateHitPointPulse(HitPoint hitPoint)
        {
            if (hitPoint != null && !hitPoint.isAnimating)
            {
                hitPoint.TriggerPulse();
            }
        }
    
        void TriggerDrawingEffects(Vector2 position)
        {
            if (flashOnDraw && currentDraggedLetter != null)
            {
                StartCoroutine(FlashEffect());
            }
        
            if (drawingParticles != null)
            {
                drawingParticles.transform.position = canvas.transform.TransformPoint(position);
                drawingParticles.Emit(1);
            }
        }
    
        void FinalizeDrawing()
        {
            if (currentLine != null && currentDrawPoints.Count > 0)
            {
                currentLine.positionCount = currentDrawPoints.Count;
                for (int i = 0; i < currentDrawPoints.Count; i++)
                {
                    currentLine.SetPosition(i, ConvertToWorldSpace(currentDrawPoints[i]));
                }
            }
        }
    
        public void ClearDrawing()
        {
            currentDrawPoints.Clear();
            
            // Clear all active lines
            foreach (var line in activeLines.Values)
            {
                if (line != null)
                {
                    Destroy(line.gameObject);
                }
            }
            activeLines.Clear();
            currentLine = null;
        }

        public void ClearCurrentLine()
        {
            // Clear only the current line being drawn
            if (currentLine != null)
            {
                Destroy(currentLine.gameObject);
                currentLine = null;
            }
            currentDrawPoints.Clear();
            
            // Clear all hit points
            ClearAllHitPoints();
        }

        public void ClearAllHitPoints()
        {
            foreach (var hitPoint in hitPoints)
            {
                if (hitPoint != null && hitPoint.gameObject != null)
                {
                    Destroy(hitPoint.gameObject);
                }
            }
            hitPoints.Clear();
        }
    
        IEnumerator FlashEffect()
        {
            if (currentDraggedLetter == null) yield break;
        
            var image = currentDraggedLetter.GetComponent<Image>();
            if (image == null) yield break;
        
            Color originalColor = image.color;
            image.color = flashColor;
        
            yield return new WaitForSeconds(0.1f);
        
            image.color = originalColor;
        }
    
        IEnumerator BeatFlashEffect()
        {
            if (currentDraggedLetter == null) yield break;
        
            var image = currentDraggedLetter.GetComponent<Image>();
            if (image == null) yield break;
        
            Color originalColor = image.color;
            image.color = flashColor;
        
            yield return new WaitForSeconds(0.05f);
        
            image.color = originalColor;
        }
    
        public bool IsDrawing() => isDrawing;
        public Vector2 GetCurrentDrawPosition() => currentDrawPosition;
        public List<Vector2> GetDrawPoints() => currentDrawPoints;
        public LetterMatchController GetCurrentDraggedLetter() => currentDraggedLetter;
        
        // Methods for music playback animation
        public void AnimateHitPointsAtTime(float musicTime)
        {
            foreach (var hitPoint in hitPoints)
            {
                if (hitPoint != null && !hitPoint.isAnimating)
                {
                    // Check if it's time to animate this hit point (within a small time window)
                    float timeDiff = Mathf.Abs(musicTime - hitPoint.spawnTime);
                    if (timeDiff < 0.05f) // 50ms window for more precise timing
                    {
                        AnimateHitPointPulse(hitPoint);
                    }
                }
            }
        }
        
        // Overloaded method to animate hit points at time for a specific channel
        public void AnimateHitPointsAtTime(float musicTime, string channelId)
        {
            foreach (var hitPoint in hitPoints)
            {
                if (hitPoint != null && !hitPoint.isAnimating && hitPoint.channelId == channelId)
                {
                    // Check if it's time to animate this hit point (within a small time window)
                    float timeDiff = Mathf.Abs(musicTime - hitPoint.spawnTime);
                    if (timeDiff < 0.05f) // 50ms window for more precise timing
                    {
                        AnimateHitPointPulse(hitPoint);
                    }
                }
            }
        }
        
        public List<HitPoint> GetHitPoints() => hitPoints;

        public void AnimateHitPointsForChannel(string channelId)
        {
            foreach (var hitPoint in hitPoints)
            {
                if (hitPoint != null && hitPoint.channelId == channelId)
                {
                    hitPoint.TriggerPulse();
                }
            }
        }
    }
} 