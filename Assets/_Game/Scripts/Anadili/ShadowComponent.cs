using UnityEngine;
using UnityEngine.UI;

public class ShadowComponent : MonoBehaviour
{
    public string letterId;
    private Image imageComponent;
    
    void Awake() => imageComponent = GetComponent<Image>();
    
    public void SetId(string id) => letterId = id;
    public string GetId() => letterId;
    
    public void HideImage() => imageComponent.enabled = false;
    public void ShowImage() => imageComponent.enabled = true;
} 