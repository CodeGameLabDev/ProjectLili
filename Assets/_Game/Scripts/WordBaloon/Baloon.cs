using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using Spine.Unity;

public class Baloon : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector] public WordBaloon manager;
    [HideInInspector] public char letter;
    [HideInInspector] public bool isTarget;
    [HideInInspector] public Color letterColor;
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

    public void SetLetter(char c, bool isTargetLetter, LetterData letterData, Color letterColor)
    {
        letter = c;
        isTarget = isTargetLetter;
        this.letterColor = letterColor;
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
            image.color = letterColor;
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

            // Spine renk uygula
            ApplyColorToSpine(currentSpineInstance, letterColor);
        }

        // LetterPrefab'in kendi Image rengini ayarla (balon içindeki harf görseli)
        var lpImage = letterPrefab.GetComponent<Image>();
        if (lpImage != null)
        {
            lpImage.color = letterColor;
        }
    }

    private void ApplyColorToSpine(GameObject spineObj, Color color)
    {
        if (spineObj == null) return;
        var skeletonGraphic = spineObj.GetComponent<SkeletonGraphic>();
        if (skeletonGraphic != null)
        {
            if (skeletonGraphic.CustomMaterialOverride.Count > 0)
            {
                var newOverrides = new System.Collections.Generic.Dictionary<UnityEngine.Texture, UnityEngine.Material>();
                foreach (var kvp in skeletonGraphic.CustomMaterialOverride)
                {
                    if (kvp.Value != null)
                    {
                        var mat = new UnityEngine.Material(kvp.Value);
                        if (mat.HasProperty("_FillColor"))
                        {
                            mat.SetColor("_FillColor", color);
                        }
                        newOverrides[kvp.Key] = mat;
                    }
                }
                skeletonGraphic.CustomMaterialOverride.Clear();
                foreach (var kvp in newOverrides)
                    skeletonGraphic.CustomMaterialOverride[kvp.Key] = kvp.Value;
            }
            else if (skeletonGraphic.material != null)
            {
                var mat = new UnityEngine.Material(skeletonGraphic.material);
                if (mat.HasProperty("_FillColor"))
                    mat.SetColor("_FillColor", color);
                skeletonGraphic.material = mat;
            }
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
        if (manager == null) return;

        // LetterPrefab'i gizle
        if (letterPrefab != null)
            letterPrefab.SetActive(false);

        // Spine container objesi (spinePrefab) hareket edecek
        GameObject movingObj = spinePrefab != null ? spinePrefab : letterPrefab;

        // SpinePrefab ve child spine'i aktif et
        movingObj.SetActive(true);
        if (currentSpineInstance != null)
            currentSpineInstance.SetActive(true);

        // Rage animasyonu oynat
        PlaySpineAnimation(currentSpineInstance, "rage");

        // Overlay canvas'a taşı
        Canvas overlay = manager.GetOverlayCanvas();
        if (overlay == null)
        {
            Debug.LogError("Overlay canvas bulunamadı!");
            return;
        }

        movingObj.transform.SetParent(overlay.transform, false);

        // Başlangıç pozisyonu: balonun ekran pozisyonu
        var rect = movingObj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;

            Vector2 startScreenPos = RectTransformUtility.WorldToScreenPoint(null, transform.position);
            rect.position = startScreenPos;
        }

        // Manager'a hareket edecek objeyi gönder
        manager.OnBaloonPopped(this, movingObj);
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

    private void PlaySpineAnimation(GameObject spineObj, string animName)
    {
        if (spineObj == null) return;
        var graphic = spineObj.GetComponent<SkeletonGraphic>();
        if (graphic != null && graphic.AnimationState != null)
        {
            graphic.AnimationState.SetAnimation(0, animName, true);
            return;
        }
        var skeletonAnim = spineObj.GetComponent<SkeletonAnimation>();
        if (skeletonAnim != null && skeletonAnim.AnimationState != null)
        {
            skeletonAnim.AnimationState.SetAnimation(0, animName, true);
        }
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
