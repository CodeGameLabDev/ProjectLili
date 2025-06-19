using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

[System.Serializable]
public class ShapeGroup
{
    public string groupName = "Grup";
    public List<Transform> draggableShapes = new List<Transform>(); // Sürüklenecekler
    public List<Transform> targetShapes = new List<Transform>();    // Yerleşecekler
}

public class ShapeGameManager : MonoBehaviour
{
    [Header("Şekil Grupları")]
    public List<ShapeGroup> shapeGroups = new List<ShapeGroup>();
    
    [Header("Ayarlar")]
    public float proximityDistance = 100f;
    
    [Header("Animasyon Ayarları")]
    public float moveDuration = 0.5f;
    public float disappearDuration = 0.3f;
    public Ease moveEase = Ease.OutBack;
    public Ease disappearEase = Ease.InBack;
    
    private Transform draggedShape;
    private Vector3 startPosition;
    private Transform startParent;
    private ShapeGroup activeGroup;
    private Canvas canvas;
    
    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindObjectOfType<Canvas>();
            
        // Tüm gruplardaki sürüklenebilir şekillere listener ekle
        foreach (ShapeGroup group in shapeGroups)
        {
            foreach (Transform shape in group.draggableShapes)
            {
                SetupShapeDrag(shape, group);
            }
        }
    }
    
    void SetupShapeDrag(Transform shape, ShapeGroup group)
    {
        // EventTrigger ekle
        EventTrigger trigger = shape.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = shape.gameObject.AddComponent<EventTrigger>();
            
        // Drag başlangıcı
        EventTrigger.Entry dragStart = new EventTrigger.Entry();
        dragStart.eventID = EventTriggerType.BeginDrag;
        dragStart.callback.AddListener((data) => { StartDragging(shape, group, (PointerEventData)data); });
        trigger.triggers.Add(dragStart);
        
        // Sürükleme
        EventTrigger.Entry drag = new EventTrigger.Entry();
        drag.eventID = EventTriggerType.Drag;
        drag.callback.AddListener((data) => { Drag(shape, (PointerEventData)data); });
        trigger.triggers.Add(drag);
        
        // Drag bitişi
        EventTrigger.Entry dragEnd = new EventTrigger.Entry();
        dragEnd.eventID = EventTriggerType.EndDrag;
        dragEnd.callback.AddListener((data) => { EndDragging(shape, (PointerEventData)data); });
        trigger.triggers.Add(dragEnd);
    }
    
    void StartDragging(Transform shape, ShapeGroup group, PointerEventData eventData)
    {
        draggedShape = shape;
        activeGroup = group;
        startPosition = shape.position;
        startParent = shape.parent;
        
        // En üste getir
        shape.SetParent(canvas.transform, true);
        shape.SetAsLastSibling();
        
        // CanvasGroup ayarla
        CanvasGroup canvasGroup = shape.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = shape.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
    }
    
    void Drag(Transform shape, PointerEventData eventData)
    {
        shape.position = eventData.position;
    }
    
    void EndDragging(Transform shape, PointerEventData eventData)
    {
        // CanvasGroup'u geri al
        CanvasGroup canvasGroup = shape.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;
            
        // Aktif gruptaki en yakın hedefi bul
        Transform closestTarget = FindClosestTarget(shape.position, activeGroup);
        
        if (closestTarget != null)
        {
            float distance = Vector3.Distance(shape.position, closestTarget.position);
            
            if (distance < proximityDistance)
            {
                // Doğru yere yerleştir
                PlaceShape(shape, closestTarget);
                return;
            }
        }
        
        // Yanlış yer - geri döndür
        ReturnShape(shape);
    }
    
    Transform FindClosestTarget(Vector3 position, ShapeGroup group)
    {
        Transform closest = null;
        float smallestDistance = float.MaxValue;
        
        foreach (Transform target in group.targetShapes)
        {
            // Sadece aktif hedefler
            if (target.gameObject.activeInHierarchy)
            {
                float distance = Vector3.Distance(position, target.position);
                if (distance < smallestDistance)
                {
                    smallestDistance = distance;
                    closest = target;
                }
            }
        }
        
        return closest;
    }
    
    void PlaceShape(Transform shape, Transform target)
    {
        // Önce şekli kilitle (sürükleme durdur)
        EventTrigger trigger = shape.GetComponent<EventTrigger>();
        if (trigger != null)
            trigger.enabled = false;
            
        Debug.Log($"Şekil animasyonla yerleştiriliyor... ({activeGroup.groupName})");
        
        // Animasyonlu yerleştirme
        AnimatedPlacement(shape, target);
    }
    
    void AnimatedPlacement(Transform shape, Transform target)
    {
        // Hedef boyutuna göre ayarla
        RectTransform shapeRect = shape.GetComponent<RectTransform>();
        RectTransform targetRect = target.GetComponent<RectTransform>();
        
        Vector2 targetSize = targetRect != null ? targetRect.sizeDelta : shapeRect.sizeDelta;
        
        // Sequence oluştur
        Sequence animationSequence = DOTween.Sequence();
        
        // 1. Adım: Şekli hedef pozisyonuna taşı, boyutunu ve rotasyonunu ayarla
        animationSequence.Append(shape.DOMove(target.position, moveDuration).SetEase(moveEase));
        animationSequence.Join(shapeRect.DOSizeDelta(targetSize, moveDuration).SetEase(moveEase));
        animationSequence.Join(shape.DORotateQuaternion(target.rotation, moveDuration).SetEase(moveEase));
        
        // 2. Adım: Küçük bir bekleme
        animationSequence.AppendInterval(0.1f);
        
        // 3. Adım: İkisini de yok et
        animationSequence.Append(DestroyAnimation(shape, target));
        
        // Animasyon bittiğinde
        animationSequence.OnComplete(() => {
            AnimationCompleted(shape, target);
        });
    }
    
    Tween DestroyAnimation(Transform shape, Transform target)
    {
        // Paralel olarak ikisini de küçült ve saydamlaştır
        Sequence destruction = DOTween.Sequence();
        
        // Scale animasyonu
        destruction.Append(shape.DOScale(0f, disappearDuration).SetEase(disappearEase));
        destruction.Join(target.DOScale(0f, disappearDuration).SetEase(disappearEase));
        
        // Saydamlık animasyonu (eğer Image varsa)
        Image shapeImage = shape.GetComponent<Image>();
        Image targetImage = target.GetComponent<Image>();
        
        if (shapeImage != null)
            destruction.Join(shapeImage.DOFade(0f, disappearDuration));
        if (targetImage != null)
            destruction.Join(targetImage.DOFade(0f, disappearDuration));
            
        return destruction;
    }
    
    void AnimationCompleted(Transform shape, Transform target)
    {
        // Objeleri gizle
        shape.gameObject.SetActive(false);
        target.gameObject.SetActive(false);
        
        Debug.Log($"Şekil başarıyla yerleştirildi ve yok edildi! ({activeGroup.groupName})");
        
        // Oyun tamamlandı mı kontrol et
        CheckGameCompleted();
    }
    
    void ReturnShape(Transform shape)
    {
        // Eski yerine döndür
        shape.SetParent(startParent, false);
        shape.position = startPosition;
        draggedShape = null;
    }
    
    void CheckGameCompleted()
    {
        bool allTargetsCompleted = true;
        
        foreach (ShapeGroup group in shapeGroups)
        {
            foreach (Transform target in group.targetShapes)
            {
                if (target.gameObject.activeInHierarchy)
                {
                    allTargetsCompleted = false;
                    break;
                }
            }
            if (!allTargetsCompleted) break;
        }
        
        if (allTargetsCompleted)
        {
            Debug.Log("Tebrikler! Oyun tamamlandı!");
        }
    }
} 