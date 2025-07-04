using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using static VHierarchy.Libs.VUtils;

[CreateAssetMenu(fileName = "Letter Path Database", menuName = "Game/Letter Path Database")]
public class LetterPathDatabase : ScriptableObject
{
    [TabGroup("Database")]
    [Tooltip("Harf ID'si ve Resources yolu eşleşmeleri")]
    public SerializableDictionary<string, string> letterPaths = new SerializableDictionary<string, string>();

    [TabGroup("Settings")]
    [Tooltip("Otomatik doldurma için kullanılacak Resources klasör yolu")]
    public string resourcesFolderPath = "LetterAll";

    /// <summary>
    /// Verilen harf ID'si için Resources yolunu döndürür
    /// </summary>
    public string GetLetterPath(string letterId)
    {
        if (letterPaths.ContainsKey(letterId))
            return letterPaths[letterId];
        
        Debug.LogWarning($"Harf ID '{letterId}' için yol bulunamadı!");
        return null;
    }

    /// <summary>
    /// Harf ID'sini kullanarak LetterData'yı yükler
    /// </summary>
    public LetterData LoadLetterData(string letterId)
    {
        string path = GetLetterPath(letterId);
        return string.IsNullOrEmpty(path) ? null : Resources.Load<LetterData>(path);
    }

    /// <summary>
    /// Tüm harf ID'lerini döndürür
    /// </summary>
    public List<string> GetAllLetterIds() => new List<string>(letterPaths.Keys);
} 