using UnityEngine;
using UnityEngine.Events;

public static class LetterGameEvents
{
    // Oyun durumu event'leri
    public static UnityAction onGameStarted = delegate { };
    public static UnityAction onGameEnded = delegate { };
    public static UnityAction onGamePaused = delegate { };
    public static UnityAction onGameResumed = delegate { };

    // Kelime event'leri
    public static UnityAction<string> onWordSpawned = delegate { };
    public static UnityAction<string> onWordCompleted = delegate { };
    public static UnityAction<int> onTargetsSpawned = delegate { };

    // Harf event'leri
    public static UnityAction<char> onLetterDragStarted = delegate { };
    public static UnityAction<char> onLetterDragEnded = delegate { };
    public static UnityAction<char> onLetterPlaced = delegate { };
    public static UnityAction<char> onLetterReturned = delegate { };

    // Puan event'leri
    public static UnityAction<int> onScoreChanged = delegate { };
    public static UnityAction<int> onLevelChanged = delegate { };

    // UI event'leri
    public static UnityAction onUIUpdateRequested = delegate { };

    // Debug metodları
    public static void TriggerGameStarted()
    {
        Debug.Log("Event: Game Started");
        onGameStarted?.Invoke();
    }

    public static void TriggerGameEnded()
    {
        Debug.Log("Event: Game Ended");
        onGameEnded?.Invoke();
    }

    public static void TriggerWordSpawned(string word)
    {
        Debug.Log($"Event: Word Spawned - {word}");
        onWordSpawned?.Invoke(word);
    }

    public static void TriggerWordCompleted(string word)
    {
        Debug.Log($"Event: Word Completed - {word}");
        onWordCompleted?.Invoke(word);
    }

    public static void TriggerLetterPlaced(char letter)
    {
        Debug.Log($"Event: Letter Placed - {letter}");
        onLetterPlaced?.Invoke(letter);
    }

    public static void TriggerScoreChanged(int newScore)
    {
        Debug.Log($"Event: Score Changed - {newScore}");
        onScoreChanged?.Invoke(newScore);
    }

    public static void TriggerLevelChanged(int newLevel)
    {
        Debug.Log($"Event: Level Changed - {newLevel}");
        onLevelChanged?.Invoke(newLevel);
    }

    // Tüm event'leri temizle (scene değişimi vs. için)
    public static void ClearAllEvents()
    {
        onGameStarted = delegate { };
        onGameEnded = delegate { };
        onGamePaused = delegate { };
        onGameResumed = delegate { };
        onWordSpawned = delegate { };
        onWordCompleted = delegate { };
        onTargetsSpawned = delegate { };
        onLetterDragStarted = delegate { };
        onLetterDragEnded = delegate { };
        onLetterPlaced = delegate { };
        onLetterReturned = delegate { };
        onScoreChanged = delegate { };
        onLevelChanged = delegate { };
        onUIUpdateRequested = delegate { };
        
        Debug.Log("All game events cleared");
    }
} 