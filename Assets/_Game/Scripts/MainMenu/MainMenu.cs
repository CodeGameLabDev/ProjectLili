using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenu : Singleton<MainMenu>
{
    [Header("UI References")]
    [SerializeField] private Transform levelButtonParent; // Level butonlarının oluşturulacağı parent
    [SerializeField] private GameObject levelButtonPrefab; // Level butonu prefab'ı
    [SerializeField] private GameObject modulesObject; // Modules objesi - açılıp kapanacak
    [SerializeField] private GameObject levelsObject; // "Levels" objesi - açılacak

    [SerializeField] private List<GameObject> menuObjects = new List<GameObject>();
    
    public GameObject LevelsObject => levelsObject;
    
    [Header("Main Category Buttons")]
    [SerializeField] private Button alfabeButton;
    [SerializeField] private Button numaraButton;

    [SerializeField] private Button backButton;
    
    private void Start()
    {
        SetupCategoryButtons();
    }
    
    private void SetupCategoryButtons()
    {
        if (alfabeButton != null)
        {
            alfabeButton.onClick.AddListener(() => LoadCategoryLevels("Alfabe"));
        }
        
        if (numaraButton != null)
        {
            numaraButton.onClick.AddListener(() => LoadCategoryLevels("Rakamlar"));
        }
        if (backButton != null)
        {
            backButton.onClick.AddListener(() => BackButton());
        }
    }
    
    private void LoadCategoryLevels(string categoryPath)
    {
        // ✅ Modules objesini aç
        if (modulesObject != null)
        {
            modulesObject.SetActive(false);
        }
        
        // Önce mevcut level butonlarını temizle
        ClearLevelButtons();
        
        // İlgili kategorideki tüm asset'leri yükle
        Object[] assets = Resources.LoadAll("Game/" + categoryPath, typeof(ScriptableObject));
        
        foreach (Object asset in assets)
        {
            IGameData gameData = asset as IGameData;
            if (gameData != null)
            {
                CreateLevelButton(gameData, asset.name);
            }
        }
        
        // ✅ Levels objesini aç  
        if (levelsObject != null)
        {
            Debug.Log("Levels objesi açıldı");
            levelsObject.SetActive(true);
        }
    }
    
    private void CreateLevelButton(IGameData gameData, string levelName)
    {
        if (levelButtonPrefab != null && levelButtonParent != null)
        {
            GameObject buttonInstance = Instantiate(levelButtonPrefab, levelButtonParent);
            
            // Level button script'ini al ve veriyi set et
            LevelButton levelButtonScript = buttonInstance.GetComponent<LevelButton>();
            if (levelButtonScript != null)
            {
                levelButtonScript.SetLevelData(gameData, levelName);

            }
        }
    }

    public void MenuSetActive(bool menuActive)
    {
        for (int i = 0; i < menuObjects.Count; i++)
        {
            menuObjects[i].SetActive(menuActive);
        }
    }


    public void ModuleMenuSetActive(bool menuActive)
    {
        MenuSetActive(menuActive);
        modulesObject.SetActive(false);
        levelsObject.SetActive(menuActive);
    }

    public void BackButton()
    {
        modulesObject.SetActive(true);
        levelsObject.SetActive(false);
    }
    
    private void ClearLevelButtons()
    {
        if (levelButtonParent != null)
        {
            for (int i = levelButtonParent.childCount - 1; i >= 0; i--)
            {
                Destroy(levelButtonParent.GetChild(i).gameObject);
            }
        }
    }
    
    public void CloseModulesAndLevels()
    {
        MenuSetActive(false);
    }
    
    private void OnDestroy()
    {
        if (alfabeButton != null)
            alfabeButton.onClick.RemoveAllListeners();
        if (numaraButton != null)
            numaraButton.onClick.RemoveAllListeners();
    }
}
