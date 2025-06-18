using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CreatePrefab : MonoBehaviour
{
    [Title("Prefab Variant Oluşturucu")]
    [LabelText("SkeletonDataAsset Listesi")]
    [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "name")]
    public List<SkeletonDataAsset> skeletonDataAssets = new List<SkeletonDataAsset>();
    
    [LabelText("Ana Prefab")]
    [Required("Ana prefab seçilmelidir!")]
    [AssetsOnly]
    public GameObject basePrefab;
    
    [LabelText("Kayıt Klasörü")]
    [FolderPath]
    public string savePath = "Assets/_Game/Prefabs/LettersAndNumbers/";
    
    [Button("Prefab Variant'larını Oluştur", ButtonSizes.Large)]
    [GUIColor(0.4f, 0.8f, 1f)]
    public void CreatePrefabVariants()
    {
#if UNITY_EDITOR
        if (ValidateInputs())
        {
            CreateVariantsFromList();
        }
#else
        Debug.LogWarning("Bu fonksiyon sadece Unity Editor'da çalışır!");
#endif
    }
    
#if UNITY_EDITOR
    private bool ValidateInputs()
    {
        if (basePrefab == null)
        {
            Debug.LogError("Ana prefab seçilmemiş!");
            return false;
        }
        
        if (skeletonDataAssets == null || skeletonDataAssets.Count == 0)
        {
            Debug.LogError("SkeletonDataAsset listesi boş!");
            return false;
        }
        
        if (string.IsNullOrEmpty(savePath))
        {
            Debug.LogError("Kayıt klasörü belirlenmemiş!");
            return false;
        }
        
        // Klasör varsa oluştur
        if (!AssetDatabase.IsValidFolder(savePath))
        {
            System.IO.Directory.CreateDirectory(savePath);
            AssetDatabase.Refresh();
        }
        
        return true;
    }
    
    private void CreateVariantsFromList()
    {
        int successCount = 0;
        int errorCount = 0;
        
        foreach (var skeletonData in skeletonDataAssets)
        {
            if (skeletonData == null)
            {
                Debug.LogWarning("Liste içinde null SkeletonDataAsset bulundu, atlanıyor...");
                errorCount++;
                continue;
            }
            
            try
            {
                CreateSingleVariant(skeletonData);
                successCount++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Variant oluşturulurken hata: {skeletonData.name} - {e.Message}");
                errorCount++;
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"Prefab variant oluşturma tamamlandı! Başarılı: {successCount}, Hatalı: {errorCount}");
    }
    
    private void CreateSingleVariant(SkeletonDataAsset skeletonData)
    {
        // İsmi düzenle: "skeleton data" kısmını "prefab" ile değiştir
        string originalName = skeletonData.name;
        string newName = originalName.Replace("skeleton data", "prefab", System.StringComparison.OrdinalIgnoreCase);
        newName = newName.Replace("skeletondata", "prefab", System.StringComparison.OrdinalIgnoreCase);
        newName = newName.Replace("SkeletonData", "Prefab");
        newName = newName.Replace("Skeleton Data", "Prefab");
        
        // Eğer değişiklik olmadıysa sona "Prefab" ekle
        if (newName == originalName)
        {
            newName = originalName + "Prefab";
        }
        
        // Variant oluştur
        string variantPath = savePath + newName + ".prefab";
        
        // Eğer dosya zaten varsa üzerine yaz uyarısı
        if (AssetDatabase.LoadAssetAtPath<GameObject>(variantPath) != null)
        {
            Debug.LogWarning($"Prefab zaten mevcut, üzerine yazılıyor: {variantPath}");
        }
        
        // Prefab variant oluştur
        GameObject variantPrefab = PrefabUtility.InstantiatePrefab(basePrefab) as GameObject;
        
        if (variantPrefab == null)
        {
            Debug.LogError($"Prefab instantiate edilemedi: {basePrefab.name}");
            return;
        }
        
        // Ana prefab'daki SkeletonGraphic komponentini güncelle
        SkeletonGraphic skeletonGraphic = variantPrefab.GetComponent<SkeletonGraphic>();
        
        if (skeletonGraphic != null)
        {
            skeletonGraphic.skeletonDataAsset = skeletonData;
            Debug.Log($"SkeletonDataAsset güncellendi: {variantPrefab.name} -> {skeletonData.name}");
        }
        else
        {
            Debug.LogWarning($"SkeletonGraphic komponenti bulunamadı: {variantPrefab.name}");
        }
        
        // Variant'ı kaydet
        GameObject savedVariant = PrefabUtility.SaveAsPrefabAsset(variantPrefab, variantPath);
        
        if (savedVariant != null)
        {
            Debug.Log($"Prefab variant oluşturuldu: {variantPath}");
        }
        else
        {
            Debug.LogError($"Prefab variant kaydedilemedi: {variantPath}");
        }
        
        // Sahne nesnesini temizle
        DestroyImmediate(variantPrefab);
    }
    
    [Button("Kayıt Klasörünü Aç")]
    [EnableIf("@!string.IsNullOrEmpty(savePath)")]
    public void OpenSaveFolder()
    {
        if (!string.IsNullOrEmpty(savePath) && AssetDatabase.IsValidFolder(savePath))
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(savePath);
            EditorGUIUtility.PingObject(Selection.activeObject);
        }
        else
        {
            Debug.LogWarning("Geçerli bir klasör bulunamadı!");
        }
    }
    
    [Button("Listeyi Temizle")]
    [GUIColor(1f, 0.6f, 0.6f)]
    public void ClearList()
    {
        skeletonDataAssets.Clear();
        Debug.Log("SkeletonDataAsset listesi temizlendi.");
    }
    
    [InfoBox("Ana prefab'da SkeletonGraphic komponenti olmalıdır.", InfoMessageType.Info)]
    [Button("Prefab'ı Kontrol Et")]
    public void ValidatePrefab()
    {
        if (basePrefab == null)
        {
            Debug.LogWarning("Ana prefab seçilmemiş!");
            return;
        }
        
        SkeletonGraphic skeletonGraphic = basePrefab.GetComponent<SkeletonGraphic>();
        
        if (skeletonGraphic != null)
        {
            Debug.Log($"✓ Prefab geçerli! {basePrefab.name} üzerinde SkeletonGraphic bulundu.");
        }
        else
        {
            Debug.LogError($"✗ Ana prefab'da SkeletonGraphic komponenti bulunamadı: {basePrefab.name}");
        }
    }
#endif

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
