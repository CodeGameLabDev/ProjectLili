using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

[System.Serializable]
public class AnadiliTarget
{
    public Vector2 position;
    public bool isOccupied;
    
    public AnadiliTarget(Vector2 pos)
    {
        position = pos;
        isOccupied = false;
    }
    
    public void SetOccupied(bool occupied)
    {
        isOccupied = occupied;
    }
    
    public bool IsEmpty()
    {
        return !isOccupied;
    }
}

public class TargetManager : MonoBehaviour
{
    [TabGroup("Settings")]
    [LabelText("Parent RectTransform")]
    public RectTransform parentRectTransform;

    [TabGroup("Settings")]
    [Range(0.3f, 0.9f)]
    public float widthRatio = 0.6f;

    [TabGroup("Settings")]
    [Range(0.1f, 0.5f)]
    public float topRowRatio = 0.3f;

    [TabGroup("Settings")]
    [Range(0.1f, 0.5f)]
    public float bottomRowRatio = 0.3f;

    [TabGroup("Settings")]
    [Range(50f, 300f)]
    public float snapDistance = 140f;

    [TabGroup("Debug"), ReadOnly]
    public List<AnadiliTarget> targets = new List<AnadiliTarget>();

    private static UnityEngine.Sprite circleSprite; // Cache edilmiş sprite

    private void Awake()
    {
        if (parentRectTransform == null)
            parentRectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        LetterGameEvents.onTargetsSpawned += SetupTargets;
    }

    private void OnDisable()
    {
        LetterGameEvents.onTargetsSpawned -= SetupTargets;
    }

    [TabGroup("Actions")]
    [Button("Test", ButtonSizes.Medium)]
    [GUIColor(0.4f, 0.8f, 1f)]
    public void TestTargetPositions()
    {
        SetupTargets(3); // Test için 3 harf
    }

    [TabGroup("Actions")]
    [Button("Göster", ButtonSizes.Medium), GUIColor(0.4f, 1f, 0.4f)]
    public void ShowTargets()
    {
        HideTargets();
        if (circleSprite == null) circleSprite = CreateCircleSprite();
        
        for (int i = 0; i < targets.Count; i++)
        {
            var obj = new GameObject($"T{i}");
            obj.transform.SetParent(parentRectTransform);
            
            var rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = targets[i].position;
            rt.sizeDelta = Vector2.one * 50;
            
            var img = obj.AddComponent<UnityEngine.UI.Image>();
            img.sprite = circleSprite;
            img.color = targets[i].isOccupied ? Color.red : Color.green;
        }
    }

    [TabGroup("Actions")]
    [Button("Gizle", ButtonSizes.Medium), GUIColor(1f, 0.4f, 0.4f)]
    public void HideTargets()
    {
        for (int i = parentRectTransform.childCount - 1; i >= 0; i--)
        {
            var child = parentRectTransform.GetChild(i);
            if (child.name[0] == 'T' && char.IsDigit(child.name[1]))
            {
#if UNITY_EDITOR
                DestroyImmediate(child.gameObject);
#else
                Destroy(child.gameObject);
#endif
            }
        }
    }

    public void SetupTargets(int letterCount)
    {
        targets.Clear();
        if (!parentRectTransform) return;
        
        // En az 4, herzaman harf sayısından 2 fazla
        int targetCount = Mathf.Max(4, letterCount + 2);
        int rowCount = targetCount / 2;
        
        var rect = parentRectTransform.rect;
        float totalWidth = rect.width * widthRatio;
        float startX = -totalWidth * 0.5f;
        float spacingX = rowCount > 1 ? totalWidth / (rowCount - 1) : 0f;
        
        float[] rowY = { rect.height * topRowRatio, -rect.height * bottomRowRatio };
        
        for (int row = 0; row < 2; row++)
        {
            for (int i = 0; i < rowCount; i++)
            {
                Vector2 pos = new Vector2(startX + i * spacingX, rowY[row]);
                targets.Add(new AnadiliTarget(pos));
            }
        }
        
        Debug.Log($"Target'lar oluşturuldu: {targets.Count} adet ({letterCount} harf için)");
    }

    public Vector2 GetNearestEmptyTarget(Vector2 pos)
    {
        int nearest = -1;
        float minDist = float.MaxValue;
        
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i].IsEmpty())
            {
                float dist = Vector2.SqrMagnitude(pos - targets[i].position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = i;
                }
            }
        }

        if (nearest == -1)
            return targets.Count > 0 ? targets[0].position : Vector2.zero;

        // Target'ı işgal et ve pozisyonunu döndür
        targets[nearest].SetOccupied(true);
        return targets[nearest].position;
    }

    private static UnityEngine.Sprite CreateCircleSprite()
    {
        int size = 32;
        var tex = new Texture2D(size, size);
        var pixels = new Color[size * size];
        
        float center = size * 0.5f;
        float radius = size * 0.4f;
        float radiusSqr = radius * radius;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                pixels[y * size + x] = (dx * dx + dy * dy <= radiusSqr) ? Color.white : Color.clear;
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        return UnityEngine.Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f);
    }

    private void OnValidate()
    {
        if (parentRectTransform == null)
            parentRectTransform = GetComponent<RectTransform>();
    }
} 