using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public class AlfabeLevelSelectButton : MonoBehaviour
{
    [Header("Level Data")]
    [SerializeField] private AlfabeModuleData gameData;
    
    
    private Button button;
    
    void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }
    }
    
    [Button("Start This Level")]
    public void OnButtonClick()
    {
        if (gameData == null)
        {
            Debug.LogError("Alfabe Module Data is not assigned!");
            return;
        }
        
        // Canvas'ı gizle
        if (GameManager.Instance.levelSelectCanvas != null)
        {
            GameManager.Instance.levelSelectCanvas.gameObject.SetActive(false);
        }
        
        // GameManager'a level'i ver ve başlat
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartLevel(gameData, GameManager.Instance.levelSelectCanvas.gameObject);
        }
        else
        {
            Debug.LogError("GameManager instance not found!");
        }
    }
    
    public void SetLevelData(AlfabeModuleData data)
    {
        gameData = data;
    }
    
    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }
} 