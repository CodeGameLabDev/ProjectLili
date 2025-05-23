using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PuzzlePiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    public Vector3 StartPosition;
    
    [Header("Kilit Ayarları")]
    [SerializeField] private bool isLocked = true; // Başlangıçta kilitli
    
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Camera canvasCamera;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        // Eğer CanvasGroup yoksa ekle
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Canvas bileşenini bul
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("PuzzlePiece bir Canvas'ın içinde olmalıdır!");
        }
        
        // Canvas'ın kamerasını belirle
        canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        

        // Puzzle parçasının başlangıç pozisyonunu al
        StartPosition = rectTransform.localPosition;
        // Kilitli durumdaysa başlangıç görünümünü ayarla
        UpdateLockState();
    }
    
    // Tıklandığında en öne getir
    public void OnPointerDown(PointerEventData eventData)
    {
        // Kilitliyse işlem yapma
        if (isLocked) return;
        
        // Parçayı en öne getir
        BringToFront();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Kilitliyse sürüklemeyi engelle
        if (isLocked) return;
        
        canvasGroup.blocksRaycasts = false;    
        // Parçayı en öne getir
        BringToFront();
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Kilitliyse sürüklemeyi engelle
        if (isLocked) return;
        
        // RectTransform pozisyonunu güncelle
        rectTransform.position = GetInputPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Kilitliyse sürüklemeyi engelle
        if (isLocked) return;
        
        canvasGroup.blocksRaycasts = true;
        
        // İsteğe bağlı: Hedef pozisyonuna yakınsa, yerleştir
        CheckPlacement();
    }
    
    // Parçayı en öne getir
    private void BringToFront()
    {
        // Parçayı hiyerarşide en sona taşı (böylece en üstte render edilir)
        transform.SetAsLastSibling();
    }
    
    private Vector3 GetInputPosition(PointerEventData eventData)
    {
        // Canvas render moduna göre pozisyon hesapla
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return eventData.position;
        }
        else
        {
            // World Space veya Camera Space için
            Vector3 worldPosition;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                canvas.transform as RectTransform, 
                eventData.position, 
                canvasCamera, 
                out worldPosition
            );
            return worldPosition;
        }
    }
    
    private void CheckPlacement()
    {
        // Eğer hedef pozisyona yeterince yakınsa, tam olarak oraya yerleştir
        if (Vector3.Distance(rectTransform.localPosition, StartPosition) < 100f) // Yakınlık eşiğini ayarlayın
        {
            SetLocked(true);
            rectTransform.localPosition = StartPosition;
            // İsteğe bağlı: Yerleştirme olayını tetikle
             PuzzleModule.OnPiecePlaced?.Invoke(this);
        }
    }
    
    // Kilit durumunu değiştirmek için public metod
    public void SetLocked(bool locked)
    {
        isLocked = locked;
        UpdateLockState();
    }
    
    // Kilit durumunu kontrol et ve görünümü güncelle
    private void UpdateLockState()
    {
        if (isLocked)
        {
            // Kilitli durumda - interaktif değil ve hafifçe karartılmış
            canvasGroup.interactable = false;
        }
        else
        {
            // Kilit açık - normal interaktif durumda
            canvasGroup.interactable = true;
        }
    }
    
    // Kolay erişim için
    public bool IsLocked()
    {
        return isLocked;
    }
}

