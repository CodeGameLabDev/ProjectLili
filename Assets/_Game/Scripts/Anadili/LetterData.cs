using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "Letter_", menuName = "Game/Letter Data")]
public class LetterData : ScriptableObject
{
    [TabGroup("Letter Info")]
    public char letter;

    public AudioClip letterNameSound;

    public AudioClip letterSongSound;
    
    [TabGroup("Letter Info")]
    [ReadOnly] public string letterId;
    
    [TabGroup("Prefabs")]
    public GameObject prefab; 

    [TabGroup("Prefabs")]
    public Sprite letterSprite;

    [TabGroup("Prefabs")]
    public Sprite letterShadowSprite;

    [TabGroup("Letter Info")]
    [Tooltip("Harfin genişliği")]
    public float letterWidth = 1f;

    public GameObject letterSpinePrefab;

    public GameObject GetPrefab() => prefab;
} 