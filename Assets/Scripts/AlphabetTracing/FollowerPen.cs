using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Eğer UI Image ise

public class FollowerPen : MonoBehaviour
{
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private bool isMouseCurrentlyBeingFollowed = false; // Kalem aktif olarak fareyi mi takip ediyor?

    public bool canFollowMouse = true;

    void Awake() // Start yerine Awake, diğer scriptlerin Start'ından önce çalışması için
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("FollowerPen scripti bir RectTransform gerektirir. Obje devre dışı bırakılıyor.");
            this.enabled = false;
            return;
        }

        // En üstteki Canvas'ı bulmaya çalış (nested canvas'lar için)
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null) {
            parentCanvas = canvas.rootCanvas;
        }
        
        if (parentCanvas == null)
        {
            Debug.LogError("FollowerPen bir root Canvas içinde olmalıdır. Obje devre dışı bırakılıyor.");
            this.enabled = false;
        }
        // Başlangıçta en önde olması için (isteğe bağlı, Update'te de yapılabilir)
        // rectTransform.SetAsLastSibling(); 
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))StartFollowingMouse();
        if (Input.GetMouseButtonUp(0))StopFollowingMouse();
        if (isMouseCurrentlyBeingFollowed && Input.GetMouseButton(0) && canFollowMouse)
            UpdatePositionToScreenPoint(Input.mousePosition);
    }

    /// <summary>
    /// Kalemin fareyi takip etmeye başlamasını sağlar ve kalemi en öne getirir.
    /// </summary>
    public void StartFollowingMouse()
    {
        canFollowMouse = true;
        isMouseCurrentlyBeingFollowed = true;
        BringToFront();
        UpdatePositionToScreenPoint(Input.mousePosition); 
        Debug.Log("FollowerPen: Fare takibi BAŞLADI.");
    }

    /// <summary>
    /// Kalemin fareyi takip etmesini durdurur.
    /// </summary>
    public void StopFollowingMouse()
    {
        isMouseCurrentlyBeingFollowed = false;
        Debug.Log("FollowerPen: Fare takibi DURDU.");
    }

    /// <summary>
    /// Kalemin pozisyonunu verilen ekran koordinatına (screen point) günceller.
    /// </summary>
    /// <param name="screenPosition">Hedef ekran pozisyonu (Input.mousePosition gibi).</param>
    public void UpdatePositionToScreenPoint(Vector2 screenPosition)
    {
        if (!rectTransform || !parentCanvas) return;

        Vector2 localPointerPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform, 
            screenPosition, // Fare pozisyonu yerine verilen parametreyi kullan
            GetEventCamera(),
            out localPointerPosition))
        {
            rectTransform.localPosition = localPointerPosition;
        }
    }

    /// <summary>
    /// Kalemi hiyerarşide en öne (en üste çizilecek şekilde) taşır.
    /// </summary>
    public void BringToFront()
    {
        if (rectTransform != null)
        {
            rectTransform.SetAsLastSibling();
        }
    }

    /// <summary>
    /// Kalemin fareyi takip etmeye başlamasını sağlar ve kalemi en öne getirir.
    /// </summary>
    public void TeleportToPositionAndHold(Vector2 screenPosition)
    {
        if (!rectTransform || !parentCanvas) return;
        Debug.Log("TeleportToPositionAndHold: Fare takibi BAŞLADI.");
        canFollowMouse = false;
        UpdatePositionToScreenPoint(screenPosition);
        BringToFront();
    }
    
    // Canvas'ın render moduna göre kullanılacak kamerayı döndürür.
    private Camera GetEventCamera()
    {
        if (parentCanvas == null) return null;
        return (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : parentCanvas.worldCamera;
    }

    // Kalemin şu anda fareyi takip edip etmediğini döndürür.
    public bool IsCurrentlyFollowingMouse()
    {
        return isMouseCurrentlyBeingFollowed;
    }
}

