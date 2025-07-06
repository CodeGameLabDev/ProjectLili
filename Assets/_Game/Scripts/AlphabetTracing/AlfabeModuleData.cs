using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AlfabeModuleData", menuName = "Game/Alfabe Module Data")]
public class AlfabeModuleData : ScriptableObject, IGameData
{
    
    
    [TabGroup("Level Settings")]
    [SerializeField] private List<GameClass> gameLevels = new List<GameClass>();
    
    [TabGroup("Letter Data")]
    [SerializeField] private LetterData upperCaseLetter;
    
    [TabGroup("Letter Data")]
    [SerializeField] private LetterData lowerCaseLetter;
    
    [TabGroup("Word Settings")]
    [SerializeField] private string word = "";
    
    [TabGroup("Word Settings")]
    [SerializeField] private string baloonWord = "";

    
    // IGameData interface implementation
    public List<GameClass> GameLevels => gameLevels;
    
    // Alfabe modülüne özel özellikler
    public LetterData UpperCaseLetter => upperCaseLetter;
    public LetterData LowerCaseLetter => lowerCaseLetter;
    public string Word => word;
    public string BaloonWord => baloonWord;
    public void InitializeGameData()
    {
        Debug.Log($"Alfabe Module Data initialized");
    }
} 