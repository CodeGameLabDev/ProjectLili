using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections;
using System;

public class WordGameManager : Singleton<WordGameManager>, IGameLevel
{
    [TabGroup("References")]
    public WordSpawner wordSpawner;
    
    private TargetManager targetManager;
    public TargetManager TargetManager => targetManager;
    
    // IGameLevel implementation
    public event Action OnGameStart;
    public event Action OnGameComplete;
    public bool IsCompleted { get; private set; }
    public string LevelName => "Word Game";



    
    
    public void StartGame()
    {
        targetManager = GetComponent<TargetManager>();
        IsCompleted = false;
        CreateWord(wordSpawner.wordToSpawn);
        OnGameStart?.Invoke();
    }
    
    public void CompleteGame()
    {
        if (!IsCompleted)
        {
            IsCompleted = true;
            OnGameComplete?.Invoke();
        }
    }

    void Start()
    {
        StartGame();
    }

    [Button("Kelime Yarat", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
    public void CreateWord(string word)
    {
        targetManager.SetupTargets(word.Length);
        wordSpawner.SpawnWord(word);
        SetupInitialView();
        StartCoroutine(WindEffect());
    }

    public void SetupInitialView()
    {
        if (wordSpawner == null) return;

        // Shadow'ları gizle
        for (int i = 0; i < wordSpawner.shadows.Count; i++)
            wordSpawner.shadows[i]?.HideImage();

        // Sprite'ları gizle, spine'ları göster ve blink başlat
        for (int i = 0; i < wordSpawner.sprites.Count; i++)
        {
            var sprite = wordSpawner.sprites[i];
            if (sprite != null)
            {
                sprite.HideImage();
                sprite.StartSpineBlink();
            }
        }
    }

    public IEnumerator WindEffect()
    {
        // Harfleri ters sırada target'lara gönder
        for (int i = wordSpawner.sprites.Count - 1; i >= 0; i--)
        {
            wordSpawner.shadows[i].ShowImage();
            wordSpawner.sprites[i].GoToNearestTarget();
            yield return new WaitForSeconds(0.12f);
        }
    }

    public void OnGameWon()
    {

        Invoke(nameof(GloryAnimation), 1f);

        Invoke(nameof(CompleteGame), 5f);
    }


    public void GloryAnimation()
    {
        for (int i = 0; i < wordSpawner.sprites.Count; i++)
            wordSpawner.sprites[i]?.PlayGloryAnimation();
    }
} 