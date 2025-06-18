using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using Sirenix.OdinInspector;

[System.Serializable]
public class SpineMaterialManager : MonoBehaviour
{
    [TabGroup("References")]
    [SerializeField] private SkeletonGraphic skeletonGraphic;
    
    [TabGroup("Materials")]
    [SerializeField] private List<Material> customMaterials = new List<Material>();
    
    [TabGroup("Settings")]
    [SerializeField] private bool autoDetectOnStart = true;
    
    [TabGroup("Debug")]
    [SerializeField, ReadOnly] private Material[] currentMaterials;

    void Start()
    {
        if (skeletonGraphic == null)
            skeletonGraphic = GetComponent<SkeletonGraphic>();
            
        if (autoDetectOnStart)
        {
            ApplyMultipleMaterials();
        }
    }

    [TabGroup("Materials")]
    [Button("Materyalleri Uygula")]
    public void ApplyMultipleMaterials()
    {
        if (skeletonGraphic == null)
        {
            Debug.LogError("SkeletonGraphic referansı bulunamadı!");
            return;
        }

        if (customMaterials.Count > 0)
        {
            // Manuel olarak atanmış materyalleri kullan
            ApplyCustomMaterials();
        }
        else
        {
            // Otomatik olarak atlas'tan tespit et
            AutoDetectMaterials();
        }
        
        UpdateCurrentMaterials();
    }

    [TabGroup("Materials")]
    [Button("Otomatik Tespit Et")]
    public void AutoDetectMaterials()
    {
        if (skeletonGraphic?.skeletonDataAsset?.atlasAssets == null)
        {
            Debug.LogError("SkeletonDataAsset veya AtlasAssets bulunamadı!");
            return;
        }

        var materials = new List<Material>();
        var atlasAssets = skeletonGraphic.skeletonDataAsset.atlasAssets;
        
        foreach (var atlas in atlasAssets)
        {
            if (atlas != null && atlas.Materials != null)
            {
                foreach (var material in atlas.Materials)
                {
                    if (material != null && !materials.Contains(material))
                    {
                        materials.Add(material);
                    }
                }
            }
        }
        
        if (materials.Count > 0)
        {
            // SkeletonGraphic sadece tek material destekliyor
            skeletonGraphic.material = materials[0];
            
            if (materials.Count > 1)
            {
                Debug.LogWarning($"{materials.Count} material tespit edildi ama SkeletonGraphic sadece 1 material destekliyor. İlk material uygulandı.");
                
                // Tespit edilen tüm materyalleri custom materials listesine ekle
                customMaterials.Clear();
                customMaterials.AddRange(materials);
            }
            
            Debug.Log($"Material uygulandı: {materials[0].name}");
        }
        else
        {
            Debug.LogWarning("Hiç material tespit edilemedi!");
        }
    }

    private void ApplyCustomMaterials()
    {
        if (customMaterials.Count > 0)
        {
            // SkeletonGraphic sadece tek material destekliyor
            skeletonGraphic.material = customMaterials[0];
            
            if (customMaterials.Count > 1)
            {
                Debug.LogWarning($"{customMaterials.Count} custom material mevcut ama SkeletonGraphic sadece 1 material destekliyor. İlk material uygulandı: {customMaterials[0].name}");
            }
            else
            {
                Debug.Log($"Custom material uygulandı: {customMaterials[0].name}");
            }
        }
    }

    [TabGroup("Materials")]
    [Button("Doğru Shader'ı Uygula")]
    public void ApplyCorrectShader()
    {
        if (skeletonGraphic?.material == null)
        {
            Debug.LogError("Material bulunamadı!");
            return;
        }

        // Doğru SkeletonGraphic shader'ını bul
        Shader correctShader = Shader.Find("Spine/SkeletonGraphicDefault");
        if (correctShader == null)
        {
            correctShader = Shader.Find("Spine/SkeletonGraphic");
        }

        if (correctShader != null)
        {
            Material newMaterial = new Material(correctShader);
            newMaterial.name = "SkeletonGraphic_Fixed";
            
            // Eski material'dan texture'ı kopyala
            if (skeletonGraphic.material.mainTexture != null)
            {
                newMaterial.mainTexture = skeletonGraphic.material.mainTexture;
            }
            
            skeletonGraphic.material = newMaterial;
            Debug.Log("Doğru SkeletonGraphic shader'ı uygulandı!");
        }
        else
        {
            Debug.LogError("SkeletonGraphic shader'ı bulunamadı!");
        }
        
        UpdateCurrentMaterials();
    }

    [TabGroup("Materials")]
    [Button("Mevcut Materyalleri Temizle")]
    public void ClearMaterials()
    {
        customMaterials.Clear();
        if (skeletonGraphic != null)
        {
            skeletonGraphic.material = null;
        }
        UpdateCurrentMaterials();
    }

    [TabGroup("Materials")]
    [Button("Orijinal Renkleri Koru")]
    public void PreserveOriginalColors()
    {
        if (skeletonGraphic == null)
        {
            Debug.LogError("SkeletonGraphic bulunamadı!");
            return;
        }

        // SkeletonGraphic color'ını beyaz yap (vertex color etkisini kaldır)
        skeletonGraphic.color = Color.white;
        
        // Eğer material varsa ve alpha değeri düşükse düzelt
        if (skeletonGraphic.material != null)
        {
            Material mat = skeletonGraphic.material;
            
            // Material'in alpha değerini kontrol et
            if (mat.HasProperty("_Color"))
            {
                Color matColor = mat.color;
                if (matColor.a < 1f)
                {
                    matColor.a = 1f;
                    mat.color = matColor;
                }
            }
        }

        Debug.Log("Orijinal renkler korunmak için SkeletonGraphic color beyaz yapıldı!");
    }

    [TabGroup("Debug")]
    [Button("Renk Durumu Kontrol")]
    public void CheckColorStatus()
    {
        if (skeletonGraphic == null)
        {
            Debug.LogError("SkeletonGraphic bulunamadı!");
            return;
        }

        Color skeletonColor = skeletonGraphic.color;
        
        if (skeletonColor == Color.white)
        {
            Debug.Log("✅ SkeletonGraphic color beyaz - Orijinal material renkleri korunuyor");
        }
        else
        {
            Debug.LogWarning($"❌ SkeletonGraphic color: {skeletonColor} - Bu material renklerini override ediyor!\n'Orijinal Renkleri Koru' butonunu kullanın.");
        }

        if (skeletonGraphic.material != null && skeletonGraphic.material.HasProperty("_Color"))
        {
            Color matColor = skeletonGraphic.material.color;
            Debug.Log($"Material color: {matColor}");
        }
    }

    [TabGroup("Debug")]
    [Button("Shader Kontrolü")]
    public void CheckShaderCompatibility()
    {
        if (skeletonGraphic?.material?.shader == null)
        {
            Debug.LogWarning("Material veya shader bulunamadı!");
            return;
        }

        string shaderName = skeletonGraphic.material.shader.name;
        bool isCompatible = shaderName.Contains("Spine/SkeletonGraphic") || 
                           shaderName.Contains("SkeletonGraphic");

        if (isCompatible)
        {
            Debug.Log($"✅ Shader uyumlu: {shaderName}");
        }
        else
        {
            Debug.LogWarning($"❌ Shader uyumsuz: {shaderName}\nDoğru shader için 'Doğru Shader'ı Uygula' butonunu kullanın!");
        }
    }

    [TabGroup("Debug")]
    [Button("Materyalleri Yenile")]
    private void UpdateCurrentMaterials()
    {
        if (skeletonGraphic?.material != null)
        {
            // SkeletonGraphic sadece tek material destekliyor
            currentMaterials = new Material[] { skeletonGraphic.material };
        }
        else
        {
            currentMaterials = new Material[0];
        }
    }

    // Inspector'da SkeletonGraphic referansını otomatik bul
    [TabGroup("References")]
    [Button("SkeletonGraphic Bul")]
    private void FindSkeletonGraphic()
    {
        if (skeletonGraphic == null)
        {
            skeletonGraphic = GetComponent<SkeletonGraphic>();
            if (skeletonGraphic == null)
            {
                skeletonGraphic = GetComponentInChildren<SkeletonGraphic>();
            }
        }
        
        if (skeletonGraphic != null)
        {
            Debug.Log("SkeletonGraphic bulundu!");
        }
        else
        {
            Debug.LogWarning("SkeletonGraphic bulunamadı!");
        }
    }
} 