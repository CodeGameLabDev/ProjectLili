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
    [LabelText("Harf Spine")]
    public GameObject prefab; 

    [TabGroup("Prefabs")]
    [LabelText("Harf Sprite")]
    public Sprite letterSprite;

    [TabGroup("Prefabs")]
    [LabelText("Harf Shadow Sprite")]
    public Sprite letterShadowSprite;


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


} 