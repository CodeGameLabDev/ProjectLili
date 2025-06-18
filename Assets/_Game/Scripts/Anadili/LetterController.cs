using UnityEngine;
using UnityEngine.UI;
using Spine.Unity;
using DG.Tweening;
using UnityEngine.EventSystems;

public class LetterController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public string letterId;
    public GameObject spineChild;

    private SkeletonGraphic skeletonGraphic;
    private Image imageComponent;
    private CanvasGroup spineCanvasGroup;
    
    // Drag & Drop değişkenleri
    private Canvas canvas;
    private bool isDragging = false;
    private bool isLocked = false;
    private Vector3 originalScale;
    private Vector2 originalPosition;
    
    private void Awake()
    {
        imageComponent = GetComponent<Image>();
        canvas = GetComponentInParent<Canvas>();
        originalScale = transform.localScale;
        originalPosition = GetComponent<RectTransform>().anchoredPosition;
    }
    
    public void SetId(string id)
    {
        letterId = id;
    }
    
    public string GetId()
    {
        return letterId;
    }
    
    public void SetSpineChild(GameObject spine)
    {
        spineChild = spine;
        // Spine set edildiğinde SkeletonGraphic'i cache et
        if (spineChild != null)
        {
            skeletonGraphic = spineChild.GetComponent<SkeletonGraphic>();
            
            // CanvasGroup ekle (performans için)
            spineCanvasGroup = spineChild.GetComponent<CanvasGroup>();
            if (spineCanvasGroup == null)
            {
                spineCanvasGroup = spineChild.AddComponent<CanvasGroup>();
            }
        }
    }
    
    public GameObject GetSpineChild()
    {
        return spineChild;
    }
    
    public void HideImage()
    {
        if (imageComponent != null)
            imageComponent.enabled = false;
    }
    
    public void ShowImage()
    {
        if (imageComponent != null)
            imageComponent.enabled = true;
    }
    
    public void StartSpineBlink()
    {
        // Spine'ı görünür yap
        if (spineCanvasGroup != null)
        {
            spineCanvasGroup.alpha = 1f;
        }
        
        skeletonGraphic.AnimationState?.SetAnimation(0, "blink", true);    
    }

    public void GoToNearestTarget()
    {
        if (WordGameManager.Instance == null || WordGameManager.Instance.TargetManager == null) return;

        imageComponent.enabled = true;
        spineCanvasGroup.alpha = 0f;

        transform.parent = WordGameManager.Instance.transform;
        
        // RectTransform pozisyonunu kullan (UI için)
        RectTransform rectTransform = GetComponent<RectTransform>();
        Vector2 currentPos = rectTransform.anchoredPosition;
        
        // En yakın boş target'ı bul ve işgal et
        Vector2 targetPos = WordGameManager.Instance.TargetManager.GetNearestEmptyTarget(currentPos);
        
        // Bu target'ı işgal edilmiş olarak işaretle (GetNearestEmptyTarget zaten yapıyor ama emin olmak için)
        MarkTargetAsOccupied(targetPos);
        
        // Mesafeyi hesapla
        float distance = Vector2.Distance(currentPos, targetPos);
        
        Debug.Log($"Harf '{letterId}' - Başlangıç: {currentPos}, Hedef: {targetPos}, Mesafe: {distance:F1}");
        
        // Eğer mesafe çok küçükse (zaten hedefteyse), sadece dön
        if (distance < 10f)
        {
            float randomRotation = Random.Range(-30f, 30f);
            rectTransform.DORotate(new Vector3(0, 0, randomRotation), 0.5f, RotateMode.LocalAxisAdd);
            Debug.Log($"Harf '{letterId}' zaten hedefe yakın, sadece döndürülüyor");
            return;
        }
        
        // Son durma açısı -30 ile +30 arasında olsun
        float finalRotation = Random.Range(-30f, 30f);
        
        // Dönüş animasyonu için ara açı hesapla
        float rotations = distance / 400f;
        float rotationDirection = Random.Range(0, 2) == 0 ? 1f : -1f;
        float spinRotation = rotations * 360f * rotationDirection;
        
        // Animasyon süresi (mesafeye göre)
        float duration = Mathf.Clamp(distance / 200f, 0.8f, 2.5f);
        
        // Dönerek target'a git
        Sequence moveSequence = DOTween.Sequence();
        
        // Pozisyon animasyonu
        moveSequence.Append(rectTransform.DOAnchorPos(targetPos, duration).SetEase(Ease.OutQuad));
        
        // Dönüş animasyonu (paralel) - önce dön sonra son açıya git
        moveSequence.Join(rectTransform.DORotate(new Vector3(0, 0, spinRotation), duration * 0.8f, RotateMode.LocalAxisAdd).SetEase(Ease.OutQuad));
        
        // Son açıya düzelt
        moveSequence.Append(rectTransform.DORotate(new Vector3(0, 0, finalRotation), duration * 0.2f, RotateMode.LocalAxisAdd).SetEase(Ease.OutBack));
        
        // Animasyon bittiğinde kontrol et - eğer ters durduysa düzelt
        moveSequence.OnComplete(() => {
            float currentZ = rectTransform.eulerAngles.z;
            // Açıyı -180 ile +180 arasına normalize et
            if (currentZ > 180f) currentZ -= 360f;
            
            // Eğer baş aşağı duruyorsa (90-270 derece arası) yumuşak düzelt
            if (Mathf.Abs(currentZ) > 90f)
            {
                float correctAngle = Random.Range(-30f, 30f);
                
                // Açı farkına göre süre hesapla - ana animasyonla aynı hızda
                float angleDifference = Mathf.Abs(currentZ - correctAngle);
                float correctionDuration = (angleDifference / 180f) * (duration * 0.2f); // Ana animasyonun son kısmı ile aynı oran
                correctionDuration = Mathf.Clamp(correctionDuration, 0.3f, 1.5f); // Min-max sınırlar
                
                rectTransform.DORotate(new Vector3(0, 0, correctAngle), correctionDuration).SetEase(Ease.InOutSine);
                Debug.Log($"Harf '{letterId}' ters durdu, düzeltiliyor: {currentZ:F1}° -> {correctAngle:F1}° ({correctionDuration:F1}s)");
            }
            
            Debug.Log($"Harf '{letterId}' target'a ulaştı! Mesafe: {distance:F1}, Son açı: {currentZ:F1}°");
        });
    }

    // Drag & Drop Interface Metodları
    public void OnPointerDown(PointerEventData eventData)
    {
        if (isLocked) return;

        isDragging = true;
        transform.SetAsLastSibling();
        
        // Bu harfin target'ını boşalt (eğer bir target'taysa)
        ReleaseCurrentTarget();
        
        // Spine'ı göster, sprite'ı gizle (performanslı yöntem)
        if (spineCanvasGroup != null)
        {
            spineCanvasGroup.alpha = 1f;
        }
        imageComponent.enabled = false;
        
        // Rotasyonu sıfırla
        transform.DORotate(Vector3.zero, 0.2f);
        
        // Dance animasyonu başlat
        if (skeletonGraphic != null)
        {
            skeletonGraphic.AnimationState?.SetAnimation(0, "rage", true);
        }
        
        Debug.Log($"Harf '{letterId}' drag başladı");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || isLocked) return;

        Vector2 mousePos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            canvas.worldCamera,
            out mousePos))
        {
            transform.position = canvas.transform.TransformPoint(mousePos);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging || isLocked) return;
        
        isDragging = false;
        
        // Aynı ID'ye sahip boş shadow'a yakın mı kontrol et
        Vector2 currentPos = GetComponent<RectTransform>().anchoredPosition;
        Vector2 correctShadowPos = FindNearestEmptySameIdShadow(currentPos);
        
        // Boş aynı ID shadow bulunduysa doğru yerleştir
        if (correctShadowPos != Vector2.zero)
        {
            SnapToCorrectShadow(correctShadowPos);
        }
        else
        {
            // Yanlış - en yakın boş target'a git
            GoToNearestEmptyTarget();
        }
    }
    
    private Vector2 FindNearestEmptySameIdShadow(Vector2 currentPos)
    {
        var wordSpawner = WordGameManager.Instance.wordSpawner;
        Vector2 nearestShadowPos = Vector2.zero;
        float nearestDistance = float.MaxValue;
        
        // Aynı ID'ye sahip tüm shadow'ları kontrol et
        for (int i = 0; i < wordSpawner.shadows.Count; i++)
        {
            if (wordSpawner.shadows[i].GetId() == letterId)
            {
                Vector2 shadowPos = wordSpawner.shadows[i].GetComponent<RectTransform>().anchoredPosition;
                float distance = Vector2.Distance(currentPos, shadowPos);
                
                // 100f mesafe içinde ve shadow boşsa
                if (distance < 100f && IsShadowEmpty(shadowPos))
                {
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestShadowPos = shadowPos;
                    }
                }
            }
        }
        
        if (nearestShadowPos != Vector2.zero)
        {
            Debug.Log($"Harf '{letterId}' için boş shadow bulundu: {nearestShadowPos}, mesafe: {nearestDistance:F1}");
        }
        else
        {
            Debug.Log($"Harf '{letterId}' için yakında boş shadow bulunamadı!");
        }
        
        return nearestShadowPos;
    }
    
    private bool IsShadowEmpty(Vector2 shadowPos)
    {
        // Tüm sprite'ları kontrol et, bu shadow pozisyonunda harf var mı?
        var wordSpawner = WordGameManager.Instance.wordSpawner;
        
        for (int i = 0; i < wordSpawner.sprites.Count; i++)
        {
            if (wordSpawner.sprites[i] == this) continue; // Kendisini atla
            
            Vector2 spritePos = wordSpawner.sprites[i].GetComponent<RectTransform>().anchoredPosition;
            float distance = Vector2.Distance(shadowPos, spritePos);
            
            // Eğer başka bir harf bu shadow'a çok yakınsa, shadow dolu demektir
            if (distance < 50f && wordSpawner.sprites[i].isLocked)
            {
                Debug.Log($"Shadow {shadowPos} dolu - Harf '{wordSpawner.sprites[i].letterId}' burada");
                return false;
            }
        }
        
        return true; // Shadow boş
    }
    
    private void SnapToCorrectShadow(Vector2 shadowPos)
    {
        isLocked = true;
        
        // Sprite'ı göster, spine'ı gizle (performanslı yöntem)
        if (spineCanvasGroup != null)
        {
            spineCanvasGroup.alpha = 0f;
        }
        imageComponent.enabled = true;
        
        // Shadow'a snap et - sadece pozisyon ve rotasyon
        Sequence snapSequence = DOTween.Sequence();
        snapSequence.Append(GetComponent<RectTransform>().DOAnchorPos(shadowPos, 0.3f).SetEase(Ease.OutBack));
        snapSequence.Join(transform.DORotate(Vector3.zero, 0.2f));
        
        snapSequence.OnComplete(() => {
            Debug.Log($"Harf '{letterId}' doğru shadow'una yerleştirildi!");
            CheckGameWin();
        });
    }
    
    private void ReleaseCurrentTarget()
    {
        // Bu harfin şu anda bulunduğu target'ı boşalt
        Vector2 currentPos = GetComponent<RectTransform>().anchoredPosition;
        if (WordGameManager.Instance?.TargetManager != null)
        {
            // Mevcut pozisyona yakın target'ı bul ve boşalt
            var targetManager = WordGameManager.Instance.TargetManager;
            for (int i = 0; i < targetManager.targets.Count; i++)
            {
                float distance = Vector2.Distance(currentPos, targetManager.targets[i].position);
                if (distance < 50f && targetManager.targets[i].isOccupied)
                {
                    targetManager.targets[i].SetOccupied(false);
                    Debug.Log($"Target {i} boşaltıldı");
                    break;
                }
            }
        }
    }
    
    private void GoToNearestEmptyTarget()
    {
        // Sprite'ı göster, spine'ı gizle (performanslı yöntem)
        if (spineCanvasGroup != null)
        {
            spineCanvasGroup.alpha = 0f;
        }
        imageComponent.enabled = true;
        
        // En yakın boş target'ı bul
        Vector2 nearestEmptyTarget = FindNearestEmptyTarget();
        
        Sequence wrongSequence = DOTween.Sequence();
        wrongSequence.Append(GetComponent<RectTransform>().DOAnchorPos(nearestEmptyTarget, 0.5f).SetEase(Ease.OutQuad));
        wrongSequence.Join(transform.DORotate(new Vector3(0, 0, Random.Range(-30f, 30f)), 0.5f));
        
        wrongSequence.OnComplete(() => {
            Debug.Log($"Harf '{letterId}' en yakın boş target'a gitti");
        });
    }
    
    private void MarkTargetAsOccupied(Vector2 targetPos)
    {
        var targetManager = WordGameManager.Instance.TargetManager;
        
        // Bu pozisyona yakın target'ı bul ve işgal et
        for (int i = 0; i < targetManager.targets.Count; i++)
        {
            float distance = Vector2.Distance(targetPos, targetManager.targets[i].position);
            if (distance < 10f) // Çok yakın pozisyonsa aynı target
            {
                targetManager.targets[i].SetOccupied(true);
                Debug.Log($"Target {i} işgal edildi (GoToNearestTarget)");
                break;
            }
        }
    }
    
    private Vector2 FindNearestEmptyTarget()
    {
        var targetManager = WordGameManager.Instance.TargetManager;
        Vector2 currentPos = GetComponent<RectTransform>().anchoredPosition;
        
        Vector2 nearestTargetPos = Vector2.zero;
        float nearestDistance = float.MaxValue;
        int nearestIndex = -1;
        
        Debug.Log("=== En Yakın Boş Target Aranıyor ===");
        
        // En yakın boş target'ı bul
        for (int i = 0; i < targetManager.targets.Count; i++)
        {
            if (!targetManager.targets[i].isOccupied)
            {
                float distance = Vector2.Distance(currentPos, targetManager.targets[i].position);
                Debug.Log($"Target {i}: Pos={targetManager.targets[i].position}, Mesafe={distance:F1}");
                
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTargetPos = targetManager.targets[i].position;
                    nearestIndex = i;
                }
            }
        }
        
        // En yakın target'ı işgal et
        if (nearestIndex != -1)
        {
            targetManager.targets[nearestIndex].SetOccupied(true);
            Debug.Log($"✅ En yakın boş target {nearestIndex} bulundu ve işgal edildi (mesafe: {nearestDistance:F1})");
            return nearestTargetPos;
        }
        
        // Hiç boş target yoksa ilk target'ı döndür
        Debug.Log("❌ Hiç boş target yok! İlk target'a gidiyor");
        return targetManager.targets.Count > 0 ? targetManager.targets[0].position : Vector2.zero;
    }
    
    private void CheckGameWin()
    {
        // Tüm harflerin doğru yere yerleştirilip yerleştirilmediğini kontrol et
        if (WordGameManager.Instance != null)
        {
            bool allLettersPlaced = true;
            foreach (var sprite in WordGameManager.Instance.wordSpawner.sprites)
            {
                if (!sprite.isLocked)
                {
                    allLettersPlaced = false;
                    break;
                }
            }
            
            if (allLettersPlaced)
            {
                Debug.Log("KAZANDIN! Tüm harfler doğru yerde!");
                // Win efekti burada eklenebilir
            }
        }
    }
} 