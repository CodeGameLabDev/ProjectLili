using UnityEngine;
using UnityEngine.UI;
using Spine.Unity;
using DG.Tweening;
using UnityEngine.EventSystems;
using System.Linq;

public class LetterController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public string letterId;
    public GameObject spineChild;

    private SkeletonGraphic skeletonGraphic;
    private Image imageComponent;
    private CanvasGroup spineCanvasGroup;
    private Canvas canvas;
    private RectTransform rectTransform;
    private bool isDragging, isLocked;

    void Awake()
    {
        imageComponent = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void SetId(string id) => letterId = id;
    public string GetId() => letterId;

    public void SetSpineChild(GameObject spine)
    {
        spineChild = spine;
        if (spine != null)
        {
            skeletonGraphic = spine.GetComponent<SkeletonGraphic>();
            
            spineCanvasGroup = spine.GetComponent<CanvasGroup>();
            if (spineCanvasGroup == null)
                spineCanvasGroup = spine.AddComponent<CanvasGroup>();
            
            // Başlangıç değerleri
            spineCanvasGroup.alpha = 1f;
            spineCanvasGroup.interactable = true;
            spineCanvasGroup.blocksRaycasts = true;
        }
    }

    public GameObject GetSpineChild() => spineChild;
    public void HideImage() => imageComponent.enabled = false;
    public void ShowImage() => imageComponent.enabled = true;
    
    public void HideSpine()
    {
        if (spineCanvasGroup != null) 
        {
            spineCanvasGroup.alpha = 0f;
            spineCanvasGroup.interactable = false;
            spineCanvasGroup.blocksRaycasts = false;
        }
    }
    
    public void ShowSpine()
    {
        if (spineCanvasGroup != null) 
        {
            spineCanvasGroup.alpha = 1f;
            spineCanvasGroup.interactable = true;
            spineCanvasGroup.blocksRaycasts = true;
        }
    }

    public void StartSpineBlink()
    {
        ShowSpine();
        skeletonGraphic?.AnimationState?.SetAnimation(0, "blink", true);
    }

    public void GoToNearestTarget()
    {
        if (WordGameManager.Instance?.TargetManager == null) return;

        // Spine'ı kapat, sprite'ı göster
        HideSpine();
        ShowImage();
        transform.parent = WordGameManager.Instance.transform;

        Vector2 currentPos = rectTransform.anchoredPosition;
        Vector2 targetPos = WordGameManager.Instance.TargetManager.GetNearestEmptyTarget(currentPos);
        
        float distance = Vector2.Distance(currentPos, targetPos);
        if (distance < 10f)
        {
            // Kısa mesafe için hafif dönüş + rastgele açı (başlangıçta)
            float spinRotation = (0.5f / 2f) * 360f; // Yarıya indirdik: 0.5 saniye = 90°
            float rotationDirection = Random.Range(0, 2) == 0 ? 1f : -1f;
            float finalRandomRotation = Random.Range(-30f, 30f);
            float totalRotation = (spinRotation * rotationDirection) + finalRandomRotation;
            
            rectTransform.DORotate(new Vector3(0, 0, totalRotation), 0.5f, RotateMode.LocalAxisAdd).SetEase(Ease.OutSine);
            return;
        }

        AnimateToTarget(targetPos, distance, true); // Başlangıçta takla atsın
    }
        Sequence moveSequence = DOTween.Sequence();

    void AnimateToTarget(Vector2 targetPos, float distance, bool shouldSpin = true)
    {
        // Duration hesapla
        float duration = Mathf.Clamp(distance / 200f, 0.8f, 2f);
        
        float totalRotation = 0f;
        
        if (shouldSpin)
        {
            // Takla miktarını yarıya indirdik (2 saniye = 1 takla = 360°)
            float spinRotation = (duration / 2f) * 360f;
            
            // Rastgele yön
            float rotationDirection = Random.Range(0, 2) == 0 ? 1f : -1f;
            spinRotation *= rotationDirection;
            
            // Final rastgele açı (-30° ile +30° arası)
            float finalRandomRotation = Random.Range(-30f, 30f);
            
            totalRotation = spinRotation + finalRandomRotation;
        }
        else
        {
            // Sadece final rastgele açı
            totalRotation = Random.Range(-30f, 30f);
        }

        
        // Pozisyon ve rotation beraber
        moveSequence.Append(rectTransform.DOAnchorPos(targetPos, duration).SetEase(Ease.OutCubic));
        moveSequence.Join(rectTransform.DORotate(new Vector3(0, 0, totalRotation), duration, RotateMode.LocalAxisAdd).SetEase(Ease.OutSine));
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isLocked) return;

        // Tüm DOTween animasyonlarını ve sequence'ları hemen durdur
        DOTween.Kill(rectTransform);
        DOTween.Kill(transform);
        DOTween.Kill(this);
        moveSequence.Kill();
        transform.rotation = Quaternion.identity;

        isDragging = true;
        transform.SetAsLastSibling();
        ReleaseCurrentTarget();

        ShowSpine();
        HideImage();
        skeletonGraphic?.AnimationState?.SetAnimation(0, "rage", true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || isLocked) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform, eventData.position, canvas.worldCamera, out var mousePos))
            transform.position = canvas.transform.TransformPoint(mousePos);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging || isLocked) return;
        
        isDragging = false;
        Vector2 currentPos = rectTransform.anchoredPosition;
        Vector2 correctShadowPos = FindNearestEmptySameIdShadow(currentPos);

        if (correctShadowPos != Vector2.zero)
            SnapToCorrectShadow(correctShadowPos);
        else
            GoToNearestEmptyTarget();
    }

    Vector2 FindNearestEmptySameIdShadow(Vector2 currentPos)
    {
        var wordSpawner = WordGameManager.Instance.wordSpawner;
        
        var nearestShadow = wordSpawner.shadows
            .Where(s => s.GetId() == letterId)
            .Select(s => new { Shadow = s, Pos = s.GetComponent<RectTransform>().anchoredPosition })
            .Where(s => Vector2.Distance(currentPos, s.Pos) < 100f && IsShadowEmpty(s.Pos))
            .OrderBy(s => Vector2.Distance(currentPos, s.Pos))
            .FirstOrDefault();

        return nearestShadow?.Pos ?? Vector2.zero;
    }

    bool IsShadowEmpty(Vector2 shadowPos)
    {
        var wordSpawner = WordGameManager.Instance.wordSpawner;
        
        return !wordSpawner.sprites
            .Where(s => s != this && s.isLocked)
            .Any(s => Vector2.Distance(shadowPos, s.GetComponent<RectTransform>().anchoredPosition) < 50f);
    }

    void SnapToCorrectShadow(Vector2 shadowPos)
    {
        isLocked = true;
        
        HideSpine();
        ShowImage();

        // Shadow'ı gizle
        var wordSpawner = WordGameManager.Instance.wordSpawner;
        var targetShadow = wordSpawner.shadows
            .Where(s => s.GetId() == letterId)
            .FirstOrDefault(s => Vector2.Distance(shadowPos, s.GetComponent<RectTransform>().anchoredPosition) < 10f);
        
        targetShadow?.HideImage();

        Sequence snapSequence = DOTween.Sequence();
        snapSequence.Append(rectTransform.DOAnchorPos(shadowPos, 0.3f).SetEase(Ease.OutBack));
        snapSequence.Join(transform.DORotate(Vector3.zero, 0.2f));
        snapSequence.OnComplete(CheckGameWin);
    }

    void ReleaseCurrentTarget()
    {
        Vector2 currentPos = rectTransform.anchoredPosition;
        var targetManager = WordGameManager.Instance?.TargetManager;
        
        if (targetManager != null)
        {
            var occupiedTarget = targetManager.targets
                .Where(t => t.isOccupied && Vector2.Distance(currentPos, t.position) < 50f)
                .FirstOrDefault();
            
            occupiedTarget?.SetOccupied(false);
        }
    }

        void GoToNearestEmptyTarget()
    {
        HideSpine();
        ShowImage();

        Vector2 nearestEmptyTarget = FindNearestEmptyTarget();
        float distance = Vector2.Distance(rectTransform.anchoredPosition, nearestEmptyTarget);
        
        // Drag & drop sonrası - takla atmasın
        AnimateToTarget(nearestEmptyTarget, distance, false);
    }

    Vector2 FindNearestEmptyTarget()
    {
        var targetManager = WordGameManager.Instance.TargetManager;
        Vector2 currentPos = rectTransform.anchoredPosition;

        var nearestTarget = targetManager.targets
            .Where(t => !t.isOccupied)
            .OrderBy(t => Vector2.Distance(currentPos, t.position))
            .FirstOrDefault();

        if (nearestTarget != null)
        {
            nearestTarget.SetOccupied(true);
            return nearestTarget.position;
        }

        return targetManager.targets.Count > 0 ? targetManager.targets[0].position : Vector2.zero;
    }

    public void PlayGloryAnimation()
    {
        ShowSpine();
        skeletonGraphic?.AnimationState?.SetAnimation(0, "glory", true);
        HideImage();
    }

    void CheckGameWin()
    {
        if (WordGameManager.Instance?.wordSpawner.sprites.All(s => s.isLocked) == true)
        {
            Debug.Log("KAZANDIN! Tüm harfler doğru yerde!");
            WordGameManager.Instance.OnGameWon();
        }
    }
} 