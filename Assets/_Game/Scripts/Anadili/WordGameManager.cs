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
        string word = "";

        AlfabeModuleData alfabeModuleData = GameManager.Instance.GetAlfabeModuleData();

        if(alfabeModuleData != null){
            if(GameManager.Instance.currentIndex == 0)
            {
                word = alfabeModuleData.UpperCaseLetter.letter.ToString();
                GameManager.Instance.MaskotManager.SetPosition(new Vector2(1500, -350));
                GameManager.Instance.MaskotManager.EnterScreen(false, new Vector2(650, -350), 0.5f, DG.Tweening.Ease.Linear);
            }
            else if(GameManager.Instance.currentIndex == 1)
            {
                word = alfabeModuleData.LowerCaseLetter.letter.ToString();
            }
            else
            {
                word = alfabeModuleData.Word;
            }
        }
        else{
            NumberModuleData numberModuleData = GameManager.Instance.GetNumberModuleData();
            if(GameManager.Instance.currentIndex == 0)
            {
                word = numberModuleData.NumberData.letter.ToString();
            }
        }

        CreateWord(word);
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
        //CreateWord(wordSpawner.wordToSpawn);
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

        // Tüm harflere glory animasyonu oynat
        for (int i = 0; i < wordSpawner.sprites.Count; i++)
            wordSpawner.sprites[i]?.PlayGloryAnimation();
            
        GameManager.Instance.MaskotManager.PlayHappyAnimation(5f);
        Invoke(nameof(CompleteGame), 5f);
    }
} 