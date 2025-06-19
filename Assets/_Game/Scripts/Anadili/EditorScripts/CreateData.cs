using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CreateData : MonoBehaviour
{
    [Title("LetterData Oluşturucu")]
    [LabelText("Prefab Listesi")]
    [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "name")]
    public List<GameObject> prefabList = new List<GameObject>();
    
    [LabelText("Kayıt Klasörü")]
    [FolderPath]
    public string savePath = "Assets/_Game/Scripts/Anadili/Data/";
    
    [Button("LetterData'ları Oluştur", ButtonSizes.Large)]
    [GUIColor(0.4f, 0.8f, 1f)]
    public void CreateLetterDatas()
    {
#if UNITY_EDITOR
        if (ValidateInputs())
        {
            CreateDataFromList();
        }
#else
        Debug.LogWarning("Bu fonksiyon sadece Unity Editor'da çalışır!");
#endif
    }
    
#if UNITY_EDITOR
    private bool ValidateInputs()
    {
        if (prefabList == null || prefabList.Count == 0)
        {
            Debug.LogError("Prefab listesi boş!");
            return false;
        }
        
        if (string.IsNullOrEmpty(savePath))
        {
            Debug.LogError("Kayıt klasörü belirlenmemiş!");
            return false;
        }
        
        // Klasör yoksa oluştur
        if (!AssetDatabase.IsValidFolder(savePath))
        {
            System.IO.Directory.CreateDirectory(savePath);
            AssetDatabase.Refresh();
        }
        
        return true;
    }
    
    private void CreateDataFromList()
    {
        int successCount = 0;
        int errorCount = 0;
        
        foreach (var prefab in prefabList)
        {
            if (prefab == null)
            {
                Debug.LogWarning("Liste içinde null prefab bulundu, atlanıyor...");
                errorCount++;
                continue;
            }
            
            try
            {
                CreateSingleLetterData(prefab);
                successCount++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"LetterData oluşturulurken hata: {prefab.name} - {e.Message}");
                errorCount++;
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"LetterData oluşturma tamamlandı! Başarılı: {successCount}, Hatalı: {errorCount}");
    }
    
    private void CreateSingleLetterData(GameObject prefab)
    {
        string prefabName = prefab.name;
        char letter;
        string letterId;
        string dataName;
        
        // Orijinal prefab isminin ilk _ den önceki kısmını al
        string baseName = prefabName.Split('_')[0];
        
        // İsimden harf/sayı çıkar
        if (IsNumberPrefab(prefabName))
        {
            // Number prefab'ı (örn: "Number_1", "1" vs.)
            char number = ExtractNumber(prefabName);
            letter = number;
            letterId = number.ToString();
            dataName = "number_" + baseName; // number_1_prefab.asset
        }
        else
        {
            // Letter prefab'ı
            string extractedLetter = ExtractLetter(prefabName);
            bool isLowerCase = IsLowerCasePrefab(prefabName);
            
            if (isLowerCase)
            {
                // Küçük harf için hem letter hem letterId küçük olacak
                letter = extractedLetter.ToLower()[0]; // İlk karakter letter field'ı için
                letterId = extractedLetter.ToLower(); // Tam string letterId için
                dataName = "low_" + baseName; // low_a_low_prefab.asset
            }
            else
            {
                // Büyük harf için hem letter hem letterId büyük olacak
                letter = extractedLetter.ToUpper()[0]; // İlk karakter letter field'ı için
                letterId = extractedLetter.ToUpper(); // Tam string letterId için
                dataName = "up_" + baseName; // up_A_prefab.asset
            }
        }
        
        // LetterData oluştur
        LetterData letterData = ScriptableObject.CreateInstance<LetterData>();
        letterData.letter = letter;
        letterData.letterId = letterId;
        letterData.prefab = prefab;
        letterData.letterWidth = 1.0f; // Varsayılan değer
        
        // Dosya yolu oluştur
        string assetPath = savePath + dataName + ".asset";
        
        // Eğer dosya zaten varsa üzerine yaz uyarısı
        if (AssetDatabase.LoadAssetAtPath<LetterData>(assetPath) != null)
        {
            Debug.LogWarning($"LetterData zaten mevcut, üzerine yazılıyor: {assetPath}");
        }
        
        // Asset'i kaydet
        AssetDatabase.CreateAsset(letterData, assetPath);
        Debug.Log($"LetterData oluşturuldu: {dataName} -> '{letter}' ({letterId})");
    }
    
    private bool IsNumberPrefab(string prefabName)
    {
        string lowerName = prefabName.ToLower();
        
        // Açıkça "number" veya "digit" içeriyorsa sayıdır
        if (lowerName.Contains("number") || lowerName.Contains("digit"))
            return true;
            
        // Sadece rakam ve _ içeriyorsa sayıdır (örn: "1_prefab", "0_prefab")
        string nameWithoutUnderscore = prefabName.Replace("_", "");
        if (nameWithoutUnderscore.All(char.IsDigit) && nameWithoutUnderscore.Length > 0)
            return true;
            
        return false;
    }
    
    private bool IsLowerCasePrefab(string prefabName)
    {
        string lowerName = prefabName.ToLower();
        return lowerName.Contains("low") || lowerName.Contains("small") || lowerName.Contains("küçük");
    }
    
    private string ExtractLetter(string prefabName)
    {
        // Önce harf+sayı kombinasyonunu ara (A1, B2, a1, b2 gibi)
        var letterNumberMatch = System.Text.RegularExpressions.Regex.Match(prefabName, @"[A-Za-z]\d+");
        if (letterNumberMatch.Success)
        {
            return letterNumberMatch.Value; // A1, B2, a1, b2 gibi
        }
        
        // Harf+sayı yoksa, tek harf ara
        var singleLetterMatch = System.Text.RegularExpressions.Regex.Match(prefabName, @"[A-Za-z]");
        if (singleLetterMatch.Success)
        {
            return singleLetterMatch.Value; // A, B, a, b gibi
        }
        
        // Hiçbir şey bulamazsa A döndür
        Debug.LogWarning($"Prefab isminden harf çıkarılamadı: {prefabName}, 'A' kullanılıyor");
        return "A";
    }
    
    private char ExtractNumber(string prefabName)
    {
        // Prefab isminden sayıyı çıkar (örn: "Number_1" -> '1', "5" -> '5')
        var match = System.Text.RegularExpressions.Regex.Match(prefabName, @"\d");
        if (match.Success)
        {
            return match.Value[0];
        }
        
        // Hiçbir şey bulamazsa 0 döndür
        Debug.LogWarning($"Prefab isminden sayı çıkarılamadı: {prefabName}, '0' kullanılıyor");
        return '0';
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
        prefabList.Clear();
        Debug.Log("Prefab listesi temizlendi.");
    }
    
    [InfoBox("Prefab isimleri şu formatları destekler:\n• Letter_A (Büyük A) → up_A.asset\n• a1_low_prefab (Küçük a1) → low_a1.asset\n• Number_1 (Sayı 1) → number_1.asset", InfoMessageType.Info)]
    [Button("Prefab İsimlerini Kontrol Et")]
    public void ValidatePrefabNames()
    {
        if (prefabList == null || prefabList.Count == 0)
        {
            Debug.LogWarning("Prefab listesi boş!");
            return;
        }
        
        int validCount = 0;
        int invalidCount = 0;
        
        foreach (var prefab in prefabList)
        {
            if (prefab == null) continue;
            
            string prefabName = prefab.name;
            bool isValid = false;
            string prediction = "";
            
            string dataFileName;
            
            if (IsNumberPrefab(prefabName))
            {
                char number = ExtractNumber(prefabName);
                dataFileName = "number_" + number.ToString() + ".asset";
                prediction = $"Sayı: '{number}' → {dataFileName}";
                isValid = true;
            }
            else
            {
                string extractedLetter = ExtractLetter(prefabName);
                bool isLower = IsLowerCasePrefab(prefabName);
                char letterField = isLower ? extractedLetter.ToLower()[0] : extractedLetter.ToUpper()[0];
                string letterIdField = isLower ? extractedLetter.ToLower() : extractedLetter.ToUpper();
                
                if (isLower)
                {
                    dataFileName = "low_" + extractedLetter.ToLower() + ".asset";
                }
                else
                {
                    dataFileName = "up_" + extractedLetter.ToUpper() + ".asset";
                }
                
                prediction = $"Letter: '{letterField}', ID: '{letterIdField}' ({(isLower ? "küçük" : "büyük")}) → {dataFileName}";
                isValid = true;
            }
            
            if (isValid)
            {
                Debug.Log($"✓ {prefabName} -> {prediction}");
                validCount++;
            }
            else
            {
                Debug.LogWarning($"✗ {prefabName} -> Geçersiz format");
                invalidCount++;
            }
        }
        
        Debug.Log($"Kontrol tamamlandı! Geçerli: {validCount}, Geçersiz: {invalidCount}");
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
