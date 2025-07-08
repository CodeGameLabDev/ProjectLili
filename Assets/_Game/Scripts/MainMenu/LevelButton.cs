using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelButton : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI levelNameText;
    [SerializeField] private Button button;
    
    private IGameData gameData;
    private string levelName;
    
    private void Awake()
    {
   
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }
    }
    
    public void SetLevelData(IGameData data, string name)
    {
        gameData = data;
        levelName = data.LevelName; // IGameData'dan level name'i al
        
        // TextMeshPro'ya level name'ini yaz
        if (levelNameText != null)
        {
            levelNameText.text = levelName;
        }
    }
    
    private void OnButtonClick()
    {
        if (gameData == null)
        {
            Debug.LogError("Level data is not assigned!");
            return;
        }
        
        // GameManager'a level'i ver ve başlat
        if (GameManager.Instance != null)
        {
            // Modules ve Levels objeleri kapat
            if (MainMenu.Instance != null)
            {
                MainMenu.Instance.CloseModulesAndLevels();
                Debug.Log("Modules ve Levels objeleri kapatıldı!");
            }
            
            GameManager.Instance.StartLevel(gameData, MainMenu.Instance.LevelsObject);
        }
        else
        {
            Debug.LogError("GameManager instance not found!");
        }
    }
    
    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }
} 