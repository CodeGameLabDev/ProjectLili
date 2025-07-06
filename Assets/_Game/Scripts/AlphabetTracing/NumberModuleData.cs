using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NumberModuleData", menuName = "Game/Number Module Data")]
public class NumberModuleData : ScriptableObject, IGameData
{
    [TabGroup("Level Settings")]
    [SerializeField] private List<GameClass> gameLevels = new List<GameClass>();
    
    [TabGroup("Number Data")]
    [SerializeField] private LetterData numberData;
    
    // IGameData interface implementation
    public List<GameClass> GameLevels => gameLevels;
    
    // Number modülüne özel özellikler
    public LetterData NumberData => numberData;
    
    public void InitializeGameData()
    {
        Debug.Log($"Number Module Data initialized");
    }
} 