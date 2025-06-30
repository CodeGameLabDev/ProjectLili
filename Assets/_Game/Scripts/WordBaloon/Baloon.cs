using UnityEngine;
using UnityEngine.UI;

public class Baloon : MonoBehaviour
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

    public void SetLetter(char c, bool isTargetLetter, LetterData letterData)
    {
        letter = c;
        isTarget = isTargetLetter;
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
        }
    }

    void OnMouseDown()
    {
        if (manager != null)
            manager.OnBaloonTapped(this);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
