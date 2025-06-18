using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LetterData))]
public class LetterDataEditor : Editor
{
    private GameObject tempLetterObj;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var letterData = (LetterData)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Editor Araçları", EditorStyles.boldLabel);

        if (GUILayout.Button("Harf Genişliğini Hesapla"))
        {
            CalculateLetterWidth(letterData);
        }

        EditorGUILayout.Space();
    }

    private void CalculateLetterWidth(LetterData letterData)
    {
        if (letterData.prefab == null)
        {
            Debug.LogWarning($"Harf {letterData.letter} için prefab atanmamış!");
            return;
        }

        // Prefabdan geçici obje oluştur
        tempLetterObj = Instantiate(letterData.prefab);
        var rectTransform = tempLetterObj.GetComponent<RectTransform>();
        
        if (rectTransform != null)
        {
            float letterWidth = rectTransform.rect.width;
            letterData.letterWidth = letterWidth;
            
            Debug.Log($"Harf '{letterData.letter}' genişliği: {letterWidth}");
            
            EditorUtility.SetDirty(letterData);
            AssetDatabase.SaveAssets();
        }
        else
        {
            Debug.LogWarning($"Harf {letterData.letter} prefabında RectTransform bulunamadı!");
        }

        // Geçici objeyi temizle
        if (tempLetterObj != null)
        {
            DestroyImmediate(tempLetterObj);
            tempLetterObj = null;
        }
    }

    private void OnDisable()
    {
        if (tempLetterObj != null)
        {
            DestroyImmediate(tempLetterObj);
            tempLetterObj = null;
        }
    }
} 