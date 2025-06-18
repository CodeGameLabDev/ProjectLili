using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Spine.Unity;

public class WordSpawner : MonoBehaviour
{
    [TabGroup("Database")]
    public LetterPathDatabase letterDatabase;
    
    [TabGroup("Database")]
    public ColorPalette colorPalette;

    [TabGroup("Settings")]
    public CanvasScaler canvasScaler;

    [TabGroup("Settings")]
    public Transform spawnParent;

    [TabGroup("Settings")]
    [Range(-100f, 200f)]
    public float letterSpacing = 0f;

    [TabGroup("Settings")]
    [Range(0.5f, 3f)]
    public float maxSize = 1.5f;

    [TabGroup("Debug")]
    [ReadOnly]
    public List<Vector2> letterPositions = new List<Vector2>();

    [TabGroup("Debug")]
    [ReadOnly]
    public Vector3 letterScale = Vector3.one;

    [TabGroup("Generated Lists")]
    [ReadOnly]
    public List<ShadowComponent> shadows = new List<ShadowComponent>();

    [TabGroup("Generated Lists")]
    [ReadOnly]
    public List<LetterController> sprites = new List<LetterController>();

    [TabGroup("Generated Lists")]
    [ReadOnly]
    public List<GameObject> spines = new List<GameObject>();

    [TabGroup("Actions")]
    public string wordToSpawn = "MERHABA";

    [TabGroup("Actions")]
    [Button("Kelime Yarat", ButtonSizes.Large)]
    [GUIColor(0.4f, 0.8f, 1f)]
    public void SpawnWord()
    {
        if (!ValidateReferences()) return;

        ClearAll();
        CalculateLetterPositions();
        CreateLetterObjects();
    }

    [TabGroup("Actions")]
    [Button("Temizle", ButtonSizes.Medium)]
    [GUIColor(1f, 0.6f, 0.6f)]
    public void ClearAll()
    {
        for (int i = 0; i < shadows.Count; i++)
        {
            if (shadows[i] != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(shadows[i].gameObject);
#else
                Destroy(shadows[i].gameObject);
#endif
            }
        }
        
        shadows.Clear();
        sprites.Clear();
        spines.Clear();
    }

    private bool ValidateReferences()
    {
        if (letterDatabase == null || canvasScaler == null || spawnParent == null || string.IsNullOrEmpty(wordToSpawn))
        {
            Debug.LogError("Referanslar eksik!");
            return false;
        }
        return true;
    }

    private void CalculateLetterPositions()
    {
        letterPositions.Clear();
        
        float totalWidth = CalculateWordWidth();
        float res = canvasScaler.referenceResolution.x;
        float availableWidth = res - (res * 0.16f); // %8 padding her yandan
        
        float scaleFactor = Mathf.Min(availableWidth / totalWidth, maxSize);
        letterScale = Vector3.one * scaleFactor;

        float startX = -totalWidth * scaleFactor * 0.5f;
        float currentX = startX;

        for (int i = 0; i < wordToSpawn.Length; i++)
        {
            if (!char.IsLetter(wordToSpawn[i])) continue;
            
            var letterData = letterDatabase.LoadLetterData(wordToSpawn[i].ToString());
            if (letterData == null) continue;

            currentX += letterData.letterWidth * 0.5f * scaleFactor;
            letterPositions.Add(new Vector2(currentX, 0));
            currentX += letterData.letterWidth * 0.5f * scaleFactor;
            
            if (i < wordToSpawn.Length - 1)
                currentX += letterSpacing * scaleFactor;
        }
    }

    private float CalculateWordWidth()
    {
        float totalWidth = 0f;
        int letterCount = 0;
        
        for (int i = 0; i < wordToSpawn.Length; i++)
        {
            if (!char.IsLetter(wordToSpawn[i])) continue;
            
            var letterData = letterDatabase.LoadLetterData(wordToSpawn[i].ToString());
            if (letterData != null)
            {
                totalWidth += letterData.letterWidth;
                letterCount++;
            }
        }
        
        if (letterCount > 1)
            totalWidth += letterSpacing * (letterCount - 1);
        
        return totalWidth;
    }

    private void CreateLetterObjects()
    {
        int positionIndex = 0;

        for (int i = 0; i < wordToSpawn.Length; i++)
        {
            if (!char.IsLetter(wordToSpawn[i])) continue;
            if (positionIndex >= letterPositions.Count) break;

            string letterId = wordToSpawn[i].ToString();
            var letterData = letterDatabase.LoadLetterData(letterId);
            if (letterData == null) continue;

            CreateLetterHierarchy(letterData, letterPositions[positionIndex], letterId, i);
            positionIndex++;
        }

        Debug.Log($"Kelime '{wordToSpawn}' yaratıldı! {shadows.Count} shadow, {sprites.Count} sprite, {spines.Count} spine.");
    }

    private void CreateLetterHierarchy(LetterData letterData, Vector2 position, string letterId, int index)
    {
        // Shadow (Parent)
        var shadowObj = CreateShadowObject(letterData, position, letterId, index);
        if (shadowObj == null) return;

        var shadowComp = shadowObj.AddComponent<ShadowComponent>();
        shadowComp.SetId(letterId);
        shadows.Add(shadowComp);

        // Sprite (Child of Shadow)
        var spriteObj = CreateSpriteObject(letterData, Vector2.zero, letterId, index, shadowObj.transform);
        if (spriteObj == null) return;

        var letterController = spriteObj.AddComponent<LetterController>();
        letterController.SetId(letterId);
        sprites.Add(letterController);

        // Spine (Child of Sprite)
        var spineObj = CreateSpineObject(letterData, GetSpinePosition(spriteObj), letterId, index, spriteObj.transform);
        if (spineObj != null)
        {
            letterController.SetSpineChild(spineObj);
            spines.Add(spineObj);
            
            // Renkleri ayarla
            ApplyColors(spriteObj, spineObj, index);
        }
    }

    private GameObject CreateShadowObject(LetterData letterData, Vector2 position, string letterId, int index)
    {
        if (letterData.letterShadowSprite == null) return null;

        var obj = new GameObject($"Shadow_{letterId}_{index}");
        obj.transform.SetParent(spawnParent);

        var image = obj.AddComponent<Image>();
        image.sprite = letterData.letterShadowSprite;
        image.SetNativeSize();

        var rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.localScale = letterScale;
        rect.localPosition = new Vector3(rect.localPosition.x, rect.localPosition.y, 1f); // Shadow arkada

        return obj;
    }

    private GameObject CreateSpriteObject(LetterData letterData, Vector2 position, string letterId, int index, Transform parent)
    {
        if (letterData.letterSprite == null) return null;

        var obj = new GameObject($"Sprite_{letterId}_{index}");
        obj.transform.SetParent(parent);

        var image = obj.AddComponent<Image>();
        image.sprite = letterData.letterSprite;
        image.SetNativeSize();

        var rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;

        return obj;
    }

    private GameObject CreateSpineObject(LetterData letterData, Vector2 position, string letterId, int index, Transform parent)
    {
        if (letterData.prefab == null) return null;

        var obj = Instantiate(letterData.prefab, parent);
        obj.name = $"Spine_{letterId}_{index}";
        obj.SetActive(true); // Spine'ı aktif yap

        var rect = obj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one;
        }
        else
        {
            obj.transform.localPosition = position;
            obj.transform.localScale = Vector3.one;
        }

        return obj;
    }

    private Vector2 GetSpinePosition(GameObject spriteObj)
    {
        var rect = spriteObj.GetComponent<RectTransform>();
        float height = rect != null ? rect.rect.height : 0f;
        return new Vector2(0, -height * 0.5f);
    }

    private void ApplyColors(GameObject spriteObj, GameObject spineObj, int index)
    {
        if (colorPalette == null || colorPalette.ColorCount == 0) return;

        Color currentColor = colorPalette.GetColor(index);

        // Sprite'ın rengini değiştir
        var spriteImage = spriteObj.GetComponent<Image>();
        if (spriteImage != null)
        {
            spriteImage.color = currentColor;
        }

        // Spine'ın rengini SkeletonGraphic CustomMaterialOverride ile değiştir
        var skeletonGraphic = spineObj.GetComponent<SkeletonGraphic>();
        
        if (skeletonGraphic != null)
        {
            // Her spine için ayrı material instance'ları oluştur
            var newOverrides = new Dictionary<Texture, Material>();
            
            foreach (var kvp in skeletonGraphic.CustomMaterialOverride)
            {
                Material originalMaterial = kvp.Value;
                if (originalMaterial != null)
                {
                    // Her spine için yeni material instance oluştur
                    Material materialInstance = new Material(originalMaterial);
                    materialInstance.name = $"{originalMaterial.name}_Spine_{index}";
                    
                    if (materialInstance.HasProperty("_FillColor"))
                    {
                        materialInstance.SetColor("_FillColor", currentColor);
                        Debug.Log($"Spine {index} material {materialInstance.name} _FillColor ayarlandı: {currentColor}");
                    }
                    
                    newOverrides[kvp.Key] = materialInstance;
                }
            }
            
            // Eski override'ları temizle ve yenilerini ekle
            skeletonGraphic.CustomMaterialOverride.Clear();
            foreach (var kvp in newOverrides)
            {
                skeletonGraphic.CustomMaterialOverride[kvp.Key] = kvp.Value;
            }
            
            // Eğer CustomMaterialOverride boşsa, ana material'ı değiştir
            if (newOverrides.Count == 0 && skeletonGraphic.material != null)
            {
                Material materialInstance = new Material(skeletonGraphic.material);
                materialInstance.name = $"{skeletonGraphic.material.name}_Spine_{index}";
                materialInstance.SetColor("_FillColor", currentColor);
                skeletonGraphic.material = materialInstance;
                Debug.Log($"Spine {index} ana material _FillColor ayarlandı: {currentColor}");
            }
        }
    }

    private void OnValidate()
    {
        if (spawnParent == null) spawnParent = transform;
        if (canvasScaler == null) canvasScaler = FindObjectOfType<CanvasScaler>();
    }
} 