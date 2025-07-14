using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Spine.Unity;

public class WordSpawner : MonoBehaviour
{
    [TabGroup("Database")]
    public LetterPathDatabase letterDatabase;
    public ColorPalette colorPalette;

    [TabGroup("Settings")]
    public CanvasScaler canvasScaler;
    public Transform spawnParent;
    [Range(-100f, 200f)] public float letterSpacing = 0f;
    [Range(0.5f, 3f)] public float maxSize = 1.5f;

    [TabGroup("Debug"), ReadOnly]
    public List<Vector2> letterPositions = new List<Vector2>();
    public Vector3 letterScale = Vector3.one;

    [TabGroup("Generated Lists"), ReadOnly]
    public List<ShadowComponent> shadows = new List<ShadowComponent>();
    public List<LetterController> sprites = new List<LetterController>();
    public List<GameObject> spines = new List<GameObject>();

    [TabGroup("Actions")]
    public string wordToSpawn = "MERHABA";

    [TabGroup("Actions")]
    [Button("Kelime Yarat", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
    public void SpawnWord()
    {
        if (!ValidateReferences()) return;
        ClearAll();
        CalculateLetterPositions();
        CreateLetterObjects();
    }



    [TabGroup("Actions")]
    [Button("Kelime Yarat", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
    public void SpawnWord(string word)
    {
        wordToSpawn = word;
        if (!ValidateReferences()) return;
        ClearAll();
        CalculateLetterPositions();
        CreateLetterObjects();
    }


    [TabGroup("Actions")]
    [Button("Temizle", ButtonSizes.Medium), GUIColor(1f, 0.6f, 0.6f)]
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

    bool ValidateReferences()
    {
        if (letterDatabase == null || canvasScaler == null || spawnParent == null || string.IsNullOrEmpty(wordToSpawn))
        {
            Debug.LogError("Referanslar eksik!");
            return false;
        }
        return true;
    }

    void CalculateLetterPositions()
    {
        letterPositions.Clear();
        
        float totalWidth = CalculateWordWidth();
        float res = canvasScaler.referenceResolution.x;
        float availableWidth = res - (res * 0.16f);
        
        float scaleFactor = Mathf.Min(availableWidth / totalWidth, maxSize);
        letterScale = Vector3.one * scaleFactor;

        float startX = -totalWidth * scaleFactor * 0.5f;
        float currentX = startX;

        for (int i = 0; i < wordToSpawn.Length; i++)
        {
            if (!char.IsLetterOrDigit(wordToSpawn[i])) continue;
            
            var letterData = letterDatabase.LoadLetterData(wordToSpawn[i].ToString());
            if (letterData == null) continue;

            currentX += letterData.letterWidth * 0.5f * scaleFactor;
            letterPositions.Add(new Vector2(currentX, 0));
            currentX += letterData.letterWidth * 0.5f * scaleFactor;
            
            if (i < wordToSpawn.Length - 1)
                currentX += letterSpacing * scaleFactor;
        }
    }

    float CalculateWordWidth()
    {
        float totalWidth = 0f;
        int letterCount = 0;
        
        for (int i = 0; i < wordToSpawn.Length; i++)
        {
            if (!char.IsLetterOrDigit(wordToSpawn[i])) continue;
            
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

    void CreateLetterObjects()
    {
        int positionIndex = 0;

        for (int i = 0; i < wordToSpawn.Length; i++)
        {
            if (!char.IsLetterOrDigit(wordToSpawn[i])) continue;
            if (positionIndex >= letterPositions.Count) break;

            string letterId = wordToSpawn[i].ToString();
            var letterData = letterDatabase.LoadLetterData(letterId);
            if (letterData == null) continue;

            CreateLetterHierarchy(letterData, letterPositions[positionIndex], letterId, i);
            positionIndex++;
        }
    }

    void CreateLetterHierarchy(LetterData letterData, Vector2 position, string letterId, int index)
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
            ApplyColors(spriteObj, spineObj, index);
        }
    }

    GameObject CreateShadowObject(LetterData letterData, Vector2 position, string letterId, int index)
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
        rect.localPosition = new Vector3(rect.localPosition.x, rect.localPosition.y, 1f);

        return obj;
    }

    GameObject CreateSpriteObject(LetterData letterData, Vector2 position, string letterId, int index, Transform parent)
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

    GameObject CreateSpineObject(LetterData letterData, Vector2 position, string letterId, int index, Transform parent)
    {
        if (letterData.prefab == null) return null;

        var obj = Instantiate(letterData.prefab, parent);
        obj.name = $"Spine_{letterId}_{index}";
        obj.SetActive(true);

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

    Vector2 GetSpinePosition(GameObject spriteObj)
    {
        var rect = spriteObj.GetComponent<RectTransform>();
        float height = rect != null ? rect.rect.height : 0f;
        return new Vector2(0, -height * 0.5f);
    }

    void ApplyColors(GameObject spriteObj, GameObject spineObj, int index)
    {
        if (colorPalette == null || colorPalette.ColorCount == 0) return;

        Color currentColor = colorPalette.GetColor(index);

        // Sprite rengi
        var spriteImage = spriteObj.GetComponent<Image>();
        if (spriteImage != null)
            spriteImage.color = currentColor;

        // Spine rengi - Düzeltilmiş
        var skeletonGraphic = spineObj.GetComponent<SkeletonGraphic>();
        if (skeletonGraphic != null)
        {
            // Önce CustomMaterialOverride'ları kontrol et
            if (skeletonGraphic.CustomMaterialOverride.Count > 0)
            {
                var newOverrides = new Dictionary<Texture, Material>();
                
                foreach (var kvp in skeletonGraphic.CustomMaterialOverride)
                {
                    if (kvp.Value != null)
                    {
                        var materialInstance = new Material(kvp.Value);
                        materialInstance.name = $"{kvp.Value.name}_Spine_{index}";
                        
                        if (materialInstance.HasProperty("_FillColor"))
                            materialInstance.SetColor("_FillColor", currentColor);
                        
                        newOverrides[kvp.Key] = materialInstance;
                    }
                }
                
                skeletonGraphic.CustomMaterialOverride.Clear();
                foreach (var kvp in newOverrides)
                    skeletonGraphic.CustomMaterialOverride[kvp.Key] = kvp.Value;
            }
            // Eğer CustomMaterialOverride boşsa ana material'ı değiştir
            else if (skeletonGraphic.material != null)
            {
                var materialInstance = new Material(skeletonGraphic.material);
                materialInstance.name = $"{skeletonGraphic.material.name}_Spine_{index}";
                
                if (materialInstance.HasProperty("_FillColor"))
                    materialInstance.SetColor("_FillColor", currentColor);
                
                skeletonGraphic.material = materialInstance;
            }
        }
    }

    void OnValidate()
    {
        if (spawnParent == null) spawnParent = transform;
        if (canvasScaler == null) canvasScaler = FindObjectOfType<CanvasScaler>();
    }
} 