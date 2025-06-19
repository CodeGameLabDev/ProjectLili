using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LetterData))]
public class LetterDataEditor : Editor
{

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
        if (letterData.letterSprite == null)
        {
            Debug.LogWarning($"Harf {letterData.letter} için letterSprite atanmamış!");
            return;
        }

        // Sprite'ın native size'ından genişliği al
        float letterWidth = letterData.letterSprite.rect.width;
        letterData.letterWidth = letterWidth;
        
        Debug.Log($"Harf '{letterData.letter}' genişliği (native size): {letterWidth}");
        
        EditorUtility.SetDirty(letterData);
        AssetDatabase.SaveAssets();
    }


} 