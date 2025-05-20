using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events; // UnityEvent için

[System.Serializable]
public class TracingObject
{
    public RectTransform startPointRect; // Başlangıç noktasının RectTransform'u
    public RectTransform endPointRect;   // Bitiş noktasının RectTransform'u
    public GameObject arrow;
    public GameObject brush;
    [HideInInspector] public bool isCompleted = false;


}

public class TracingController : MonoBehaviour
{
    public List<TracingObject> tracingObjects;
    public UnityEvent onAllSegmentsCompleted; // Tüm segmentler tamamlandığında tetiklenecek olay
    [Tooltip("Fare ile UI noktaları arasındaki algılama mesafesi (UI birimi/piksel).")]
    public float detectionRadius = 50f; // Inspector'dan ayarla, örn: 50 piksel
    private int currentIndex = 0;
    private bool isDragging = false;
    private Camera mainCam; // Screen Space Overlay için gerekmeyebilir ama dursun
    private Canvas rootCanvas; // Ana Canvas'ı bulmak için

    public GameObject LetterPieces, Letter;

    public FollowerPen followerPen;



    void Start()
    {
        mainCam = Camera.main; // Gerekirse diye
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) {
            // Eğer manager bir Canvas'ın altında değilse, sahnedeki ilk root Canvas'ı bulmayı dene
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach(Canvas c in canvases) {
                if (c.isRootCanvas) {
                    rootCanvas = c;
                    break;
                }
            }
        }

        if (rootCanvas == null) {
             Debug.LogError("Kök Canvas bulunamadı! TracingManager'ın bir Canvas altında olması veya sahnede bir Kök Canvas olması gerekir.");
             this.enabled = false;
             return;
        }

        if (tracingObjects == null || tracingObjects.Count == 0) { this.enabled = false; return; }
        for (int i = 0; i < tracingObjects.Count; i++) {
            TracingObject obj = tracingObjects[i];
            if (obj == null || obj.startPointRect == null || obj.endPointRect == null) {
                Debug.LogError($"TracingObject {i} eksik atanmış!"); this.enabled = false; return;
            }
            if (obj.brush != null) obj.brush.SetActive(false);
            if (obj.arrow != null) obj.arrow.SetActive(i == 0);
            obj.isCompleted = false;
        }
        Debug.Log($"TracingManager Başlatıldı. Canvas: {rootCanvas.name}, Algılama Yarıçapı (UI): {detectionRadius}");
        followerPen.gameObject.SetActive(true);
        followerPen.TeleportToPositionAndHold(GetScreenPos(tracingObjects[0].startPointRect));
    }

    void UpdateVisuals()
    {
        for (int i = 0; i < tracingObjects.Count; i++)
        {
            TracingObject obj = tracingObjects[i];
            if (obj != null)
            {
                bool isActiveSegment = (i == currentIndex);
                if (obj.arrow != null) obj.arrow.SetActive(isActiveSegment && !isDragging && !obj.isCompleted);
                if (obj.brush != null) obj.brush.SetActive(obj.isCompleted);
            }
        }
    }
    
    void Update()
    {
        if (currentIndex >= tracingObjects.Count) return; // Hepsi tamamlandı

        TracingObject current = tracingObjects[currentIndex];
        Vector2 mousePos = Input.mousePosition;
        Vector2 startPos = GetScreenPos(current.startPointRect);

        if (!isDragging)
        {
            if (Vector2.Distance(mousePos, startPos) <= detectionRadius && Input.GetMouseButtonDown(0)) {
                isDragging = true;
                Debug.Log($"Segment {currentIndex} BAŞLADI.");
            }
        }
        else // isDragging true ise
        {
            Vector2 endPos = GetScreenPos(current.endPointRect);

            if (Input.GetMouseButton(0)) { // Fare basılı tutuluyorsa
                if (Vector2.Distance(mousePos, endPos) <= detectionRadius) {
                    Debug.Log($"Segment {currentIndex} TAMAMLANDI.");
                    if (current.arrow != null) current.arrow.SetActive(false);
                    if (current.brush != null) current.brush.SetActive(true);
                    current.isCompleted = true;
                    isDragging = false;
                    currentIndex++;
                    if (currentIndex >= tracingObjects.Count) {
                        Debug.Log("TÜMÜ TAMAMLANDI!");
                        onAllSegmentsCompleted.Invoke();
                        LetterPieces.SetActive(false);
                        Letter.SetActive(true);
                        this.enabled = false; 
                        followerPen.gameObject.SetActive(false);
                    } else {
                        UpdateVisuals();
                        followerPen.TeleportToPositionAndHold(GetScreenPos(tracingObjects[currentIndex].startPointRect));
                    }
                }
            }
            else if (Input.GetMouseButtonUp(0)) { // Fare bırakıldıysa (ve bitişe ulaşılmadıysa)
                Debug.Log($"Segment {currentIndex} YARIDA BIRAKILDI.");
                isDragging = false; 
                UpdateVisuals(); // Oku tekrar göster
            }
        }
    }

    // Bir RectTransform'un merkezinin ekran koordinatlarını (sol alt köşe (0,0)) döndürür
    // Bu, Screen Space - Overlay için iyi çalışır.
    private Vector2 GetScreenPos(RectTransform rt)
    {
        if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) {
            // Pivot merkezde (0.5, 0.5) ise rectTransform.position doğrudan kullanılabilir.
            // Değilse, pivotu hesaba kat:
            return (Vector2)rt.position + 
                   new Vector2(rt.rect.width * (0.5f - rt.pivot.x), 
                               rt.rect.height * (0.5f - rt.pivot.y));
        } 
        else { // Screen Space - Camera veya World Space
             // Bu modlar için merkez noktasının dünya koordinatını alıp ekrana yansıtmak daha doğru
            Vector3[] worldCorners = new Vector3[4];
            rt.GetWorldCorners(worldCorners);
            Vector3 centerWorld = (worldCorners[0] + worldCorners[2]) / 2f;
            return mainCam.WorldToScreenPoint(centerWorld);
        }
    }
}

