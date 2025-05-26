using UnityEngine;
using UnityEngine.EventSystems;

// UI objeleri için tıklama scripti
public class ClickableUIObject : MonoBehaviour, IPointerClickHandler
{
    public System.Action OnClick;
    
    public void OnPointerClick(PointerEventData eventData)
    {
        OnClick?.Invoke();
    }
} 