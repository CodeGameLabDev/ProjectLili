using UnityEditor;
using UnityEngine;

public class ClearData : EditorWindow
{
    public static void ShowWindow()
    {
        GetWindow<ClearData>("ClearData");
    }

    [MenuItem("Local/Clear All Saved Data")]
    public static void ClearAllData()
    {
        if (EditorUtility.DisplayDialog("Verileri Sil", "Tüm kayıtlı veriler silinsin mi?", "Evet", "Hayır"))
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("Tüm PlayerPrefs verisi silindi.");
        }
    }
}
