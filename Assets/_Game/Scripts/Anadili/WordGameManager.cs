using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using Spine.Unity;
using System.Collections;

public class WordGameManager : Singleton<WordGameManager>
{
    [TabGroup("References")]
    public WordSpawner wordSpawner;
    
    [TabGroup("References")]
    private TargetManager targetManager;


    public TargetManager TargetManager => targetManager;



    private void Start()
    {
        targetManager = GetComponent<TargetManager>();
        CreateWord();
    }
    

     public void CreateWord()
    {
        
        CreateTargets();
        wordSpawner.SpawnWord();
        SetupInitialView();
        StartCoroutine(WindEffect());

    }
    private void CreateTargets()
    {
   
   Debug.Log("Targetlar oluşturuluyor: " + wordSpawner.wordToSpawn.Length);
        targetManager.SetupTargets(wordSpawner.wordToSpawn.Length);
           
    }

    [Button("Kelime Yarat", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]


    public void SetupInitialView()
    {
        if (wordSpawner == null) return;

        // Shadow'ları gizle
        for (int i = 0; i < wordSpawner.shadows.Count; i++)
        {
            wordSpawner.shadows[i]?.HideImage();
        }

        // Sprite'ları gizle ve spine blink başlat
        for (int i = 0; i < wordSpawner.sprites.Count; i++)
        {
            var sprite = wordSpawner.sprites[i];
            if (sprite != null)
            {
                sprite.HideImage();
                sprite.StartSpineBlink();
            }
        }

        Debug.Log("Başlangıç görünümü ayarlandı - Sadece spine'lar görünür");
    }

    public IEnumerator WindEffect()
    {
 
        
        // WordSpawner'daki spine'ların hepsini blink yap
        for (int i = wordSpawner.sprites.Count - 1; i >= 0; i--)
        {
            wordSpawner.shadows[i].ShowImage();
            wordSpawner.sprites[i].GoToNearestTarget();
            yield return new WaitForSeconds(0.5f);
        }
    }
} 