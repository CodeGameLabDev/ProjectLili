using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NumberModuleData", menuName = "Game/Number Module Data")]
public class NumberModuleData : ScriptableObject, IGameData
{
    [TabGroup("Level Settings")]
    [SerializeField] private MaskotType maskotType;

    [TabGroup("Level Settings")]
    [SerializeField] private List<GameClass> gameLevels = new List<GameClass>();
    
    [TabGroup("Number Data")]
    [SerializeField] private LetterData numberData;
    
    [TabGroup("Level Settings")]
    [SerializeField] private string levelName = "";
    
    // IGameData interface implementation
    public string LevelName => levelName;
    public List<GameClass> GameLevels => gameLevels;
    public MaskotType MaskotType => maskotType;

    // Number modülüne özel özellikler
    public LetterData NumberData => numberData;
    
    public void InitializeGameData()
    {
        Debug.Log($"Number Module Data initialized");
    }
} 