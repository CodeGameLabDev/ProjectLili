using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LetterPathDatabase))]
public class LetterPathDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var database = (LetterPathDatabase)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Editor Araçları", EditorStyles.boldLabel);

        if (GUILayout.Button("Otomatik Doldur"))
        {
            AutoPopulateDatabase(database);
        }

        if (GUILayout.Button("Veritabanını Temizle"))
        {
            ClearDatabase(database);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Toplam Harf Sayısı: {database.letterPaths.Count}", EditorStyles.helpBox);
    }

    private void AutoPopulateDatabase(LetterPathDatabase database)
    {
        if (string.IsNullOrEmpty(database.resourcesFolderPath))
        {
            Debug.LogWarning("Resources klasör yolu boş! Lütfen önce klasör yolunu belirtin.");
            return;
        }

        // Veritabanını temizle
        database.letterPaths.Clear();

        // Resources klasöründen tüm LetterData asset'lerini yükle
        LetterData[] letterDataArray = Resources.LoadAll<LetterData>(database.resourcesFolderPath);

        int addedCount = 0;
        foreach (LetterData letterData in letterDataArray)
        {
            if (!string.IsNullOrEmpty(letterData.letterId))
            {
                // Asset'in yolunu hesapla (Resources klasöründen sonraki kısmı)
                string assetPath = $"{database.resourcesFolderPath}/{letterData.name}";
                
                // Sözlüğe ekle
                if (!database.letterPaths.ContainsKey(letterData.letterId))
                {
                    database.letterPaths.Add(letterData.letterId, assetPath);
                    addedCount++;
                }
                else
                {
                    Debug.LogWarning($"Duplike harf ID bulundu: {letterData.letterId} ({letterData.name})");
                }
            }
            else
            {
                Debug.LogWarning($"LetterData '{letterData.name}' için letterId boş!");
            }
        }

        Debug.Log($"Otomatik doldurma tamamlandı! {addedCount} harf eklendi.");
        
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
    }

    private void ClearDatabase(LetterPathDatabase database)
    {
        if (EditorUtility.DisplayDialog("Veritabanını Temizle", 
            "Bu işlem tüm harf yollarını silecek. Emin misiniz?", 
            "Evet", "Hayır"))
        {
            int count = database.letterPaths.Count;
            database.letterPaths.Clear();
            
            Debug.Log($"Veritabanı temizlendi! {count} kayıt silindi.");
            
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }
    }
} 