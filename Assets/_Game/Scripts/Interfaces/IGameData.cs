using UnityEngine;
using System.Collections.Generic;


public enum CanvasType
{
    Width,
    Height,
}

[System.Serializable]
public class GameClass{
    public CanvasType canvasType;
    public GameObject gameObject;
}

public interface IGameData
{
    public string LevelName { get; }
    public List<GameClass> GameLevels { get; }

    public MaskotType MaskotType { get; }
    
    // Her oyun türü kendi özel verilerini implement edecek
    void InitializeGameData();
} 