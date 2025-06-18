using UnityEngine;
using System;
using Sirenix.OdinInspector;
using static VHierarchy.Libs.VUtils;

[Serializable]
public class SoundReference
{
    public string soundId;

}

[CreateAssetMenu(fileName = "Letter_", menuName = "Game/Letter Data")]
public class LetterData : ScriptableObject
{
    [TabGroup("Letter Info")]
    [LabelText("Harf")]
     public char letter;
    
    [TabGroup("Letter Info")]
    [LabelText("Harf ID")]
    [ReadOnly] public string letterId;
    
    [TabGroup("Prefabs")]
    [LabelText("Harf Prefabı")]
    public GameObject prefab; 
    

    [TabGroup("Letter Info")]
    [LabelText("Harf Genişliği")]
    [Tooltip("Harfin genişliği")]
    public float letterWidth;

    [TabGroup("Sounds")]    
    public SerializableDictionary<string, SoundReference> _soundDict;

    void OnEnable()
    {
        InitializeSoundDictionary();
    }

    public void InitializeSoundDictionary()
    {
        _soundDict = new SerializableDictionary<string, SoundReference>();
        
    }

    public GameObject GetPrefab()
    {
        return prefab;
    }


    [Button("Harf ID'sini Güncelle")]
    private void UpdateLetterId()
    {
        letterId = letter.ToString();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
} 