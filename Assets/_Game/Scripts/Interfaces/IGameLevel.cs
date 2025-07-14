using UnityEngine;
using System;

public interface IGameLevel
{
    event Action OnGameStart;
    event Action OnGameComplete;
    
    void StartGame();
    void CompleteGame();
    bool IsCompleted { get; }
    string LevelName { get; }
} 