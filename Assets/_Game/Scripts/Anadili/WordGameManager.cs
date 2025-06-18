using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections;

public class WordGameManager : Singleton<WordGameManager>
{
    [TabGroup("References")]
    public WordSpawner wordSpawner;
    
    private TargetManager targetManager;
    public TargetManager TargetManager => targetManager;

    void Start()
    {
        targetManager = GetComponent<TargetManager>();
        CreateWord();
    }

    [Button("Kelime Yarat", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
    public void CreateWord()
    {
        targetManager.SetupTargets(wordSpawner.wordToSpawn.Length);
        wordSpawner.SpawnWord();
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
    }
} 