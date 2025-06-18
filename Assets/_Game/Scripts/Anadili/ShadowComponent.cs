using UnityEngine;
using UnityEngine.UI;

public class ShadowComponent : MonoBehaviour
{
    public string letterId;
    private Image imageComponent;
    
    private void Awake()
    {
        imageComponent = GetComponent<Image>();
    }
    
    public void SetId(string id)
    {
        letterId = id;
    }
    
    public string GetId()
    {
        return letterId;
    }
    
    public void HideImage()
    {
        if (imageComponent != null)
            imageComponent.enabled = false;
    }
    
    public void ShowImage()
    {
        if (imageComponent != null)
            imageComponent.enabled = true;
    }
} 