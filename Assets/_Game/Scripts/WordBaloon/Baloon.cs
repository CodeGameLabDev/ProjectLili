using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class Baloon : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector] public WordBaloon manager;
    [HideInInspector] public char letter;
    [HideInInspector] public bool isTarget;
    public float riseSpeed = 120f;

    [SerializeField] private GameObject letterPrefab;
    [SerializeField] private GameObject spinePrefab;

    [Header("Harf Sprite'ı için Parent")]
    public Transform letterImage; // LetterImage objesinin Transform'u (parent)
    public LetterPathDatabase letterDatabase; // Inspector'dan atanacak

    private GameObject currentSpriteInstance;
    private GameObject currentSpineInstance;
    private bool isPopped = false;
    
    // isPopped için public property
    public bool IsPopped => isPopped;

    public void SetLetter(char c, bool isTargetLetter, LetterData letterData)
    {
        letter = c;
        isTarget = isTargetLetter;
        isPopped = false;
        // Eski objeleri sil
        if (currentSpineInstance != null) { Destroy(currentSpineInstance); currentSpineInstance = null; }
        if (currentSpriteInstance != null) { Destroy(currentSpriteInstance); currentSpriteInstance = null; }

        // Sprite objesini letterPrefab'ın child'ı olarak oluştur
        if (letterPrefab != null && letterData != null && letterData.letterSprite != null)
        {
            GameObject spriteObj = new GameObject("Sprite_Letter", typeof(RectTransform));
            spriteObj.transform.SetParent(letterPrefab.transform, false);
            var image = spriteObj.AddComponent<Image>();
            image.sprite = letterData.letterSprite;
            image.SetNativeSize();
            var rect = spriteObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
            currentSpriteInstance = spriteObj;
        }
        // Spine objesini spinePrefab'ın child'ı olarak oluştur
        if (spinePrefab != null && letterData != null && letterData.prefab != null)
        {
            currentSpineInstance = Instantiate(letterData.prefab, spinePrefab.transform);
            currentSpineInstance.transform.localPosition = Vector3.zero;
            currentSpineInstance.transform.localRotation = Quaternion.identity;
            currentSpineInstance.transform.localScale = Vector3.one;
            // Spine başlangıçta kapalı olmalı
            currentSpineInstance.SetActive(false);
        }
    }

    public void PopBaloon()
    {
        if (isPopped) return;
        isPopped = true;

        Debug.Log($"Balon patlatılıyor! Harf: {letter}");

        // BalonIMAGE patlama efekti
        Transform baloonImage = transform.Find("BaloonIMAGE");
        if (baloonImage != null)
        {
            var baloonImageRect = baloonImage.GetComponent<RectTransform>();
            if (baloonImageRect != null)
            {
                // Balon görseli büyüyüp patla
                baloonImageRect.DOScale(1.4f, 0.12f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() => {
                        baloonImageRect.DOScale(0f, 0.25f)
                            .SetEase(Ease.InBack);
                    });
            }
        }

        // Balon patlama animasyonu - büyüyüp patla
        var baloonRect = GetComponent<RectTransform>();
        if (baloonRect != null)
        {
            // Balon büyüme ve patlama animasyonu
            baloonRect.DOScale(1.3f, 0.1f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => {
                    // Patlama efekti - küçül ve kaybol
                    baloonRect.DOScale(0f, 0.2f)
                        .SetEase(Ease.InBack)
                        .OnComplete(() => {
                            // Balon tamamen kayboldu, harfi LetterHolder'a gönder
                            ExtractLetterToHolder();
                        });
                });
        }
        else
        {
            // RectTransform yoksa direkt harfi gönder
            ExtractLetterToHolder();
        }

        // Harf sprite'ını kapat
        if (letterPrefab != null) letterPrefab.SetActive(false);
    }

    private void ExtractLetterToHolder()
    {
        if (letterPrefab != null && manager != null)
        {
            // LetterPrefab'i parent'tan çıkar (Canvas'ta bağımsız hareket edebilsin)
            letterPrefab.transform.SetParent(null);

            // Canvas ekle (eğer yoksa)
            Canvas canvas = letterPrefab.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = letterPrefab.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000;
            }

            // RectTransform ayarları
            var rect = letterPrefab.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.localScale = Vector3.one;
                // İsteğe bağlı: letterHolder boyutuna göre sizeDelta ayarla
                // rect.sizeDelta = new Vector2(manager.GetLetterHolderSize(), manager.GetLetterHolderSize());
            }

            letterPrefab.SetActive(true);

            // Manager'a LetterPrefab'i gönder
            manager.OnBaloonPopped(this, letterPrefab);
        }
        else if (currentSpineInstance != null && manager != null)
        {
            currentSpineInstance.SetActive(true);
            currentSpineInstance.transform.SetParent(null);
            manager.OnBaloonPopped(this, currentSpineInstance);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (manager != null && !isPopped)
        {
            Debug.Log($"Balon tıklandı! Harf: {letter}, Target: {isTarget}");
            manager.OnBaloonTapped(this);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // Ana objeye Image component'i ekle (eğer yoksa) - tıklama algılama için gerekli
        if (GetComponent<Image>() == null)
        {
            var image = gameObject.AddComponent<Image>();
            image.color = new Color(1, 1, 1, 0); // Şeffaf renk
            image.raycastTarget = true;
            Debug.Log("Baloon objesine Image component'i eklendi");
        }
        
        // BaloonIMAGE child objesine de tıklama algılama ekle (backup)
        Transform baloonImage = transform.Find("BaloonIMAGE");
        if (baloonImage != null)
        {
            var baloonImageComponent = baloonImage.GetComponent<Image>();
            if (baloonImageComponent != null)
            {
                baloonImageComponent.raycastTarget = true;
                Debug.Log("BaloonIMAGE raycastTarget açıldı");
                
                // BaloonIMAGE objesine de click handler ekle
                if (baloonImage.GetComponent<BaloonImageClickHandler>() == null)
                {
                    var clickHandler = baloonImage.gameObject.AddComponent<BaloonImageClickHandler>();
                    clickHandler.parentBaloon = this;
                    Debug.Log("BaloonIMAGE'e click handler eklendi");
                }
            }
        }
        
        // EventSystem kontrolü
        if (FindObjectOfType<EventSystem>() == null)
        {
            Debug.LogError("EventSystem bulunamadı! UI tıklama çalışmayabilir.");
        }
        else
        {
            Debug.Log("EventSystem bulundu, UI tıklama çalışmalı");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

// BaloonIMAGE child objesi için click handler
public class BaloonImageClickHandler : MonoBehaviour, IPointerClickHandler
{
    public Baloon parentBaloon;
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (parentBaloon != null && parentBaloon.manager != null && !parentBaloon.IsPopped)
        {
            Debug.Log($"BaloonIMAGE tıklandı! Harf: {parentBaloon.letter}, Target: {parentBaloon.isTarget}");
            parentBaloon.manager.OnBaloonTapped(parentBaloon);
        }
    }
}
