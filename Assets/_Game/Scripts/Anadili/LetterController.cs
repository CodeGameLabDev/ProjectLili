using UnityEngine;
using UnityEngine.UI;
using Spine.Unity;
using DG.Tweening;

public class LetterController : MonoBehaviour
{
    public string letterId;
    public GameObject spineChild;

    private SkeletonGraphic skeletonGraphic;
    private Image imageComponent;
    
    private void Awake()
    {
        imageComponent = GetComponent<Image>();
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
       skeletonGraphic.AnimationState?.SetAnimation(0, "blink", true);    
    }

    public void GoToNearestTarget()
    {
        if (WordGameManager.Instance == null || WordGameManager.Instance.TargetManager == null) return;

        spineChild.SetActive(false);
        imageComponent.enabled = true;

        transform.parent = WordGameManager.Instance.transform;
        
        // RectTransform pozisyonunu kullan (UI için)
        RectTransform rectTransform = GetComponent<RectTransform>();
        Vector2 currentPos = rectTransform.anchoredPosition;
        
        // En yakın boş target'ı bul ve işgal et
        Vector2 targetPos = WordGameManager.Instance.TargetManager.GetNearestEmptyTarget(currentPos);
        
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
} 