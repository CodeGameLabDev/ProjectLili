using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;

[System.Serializable]
public enum MaskotType { Lili, Bear}

[System.Serializable]
public class MaskotData
{
    public MaskotType maskotType;
    public MaskotController controller;
    public GameObject maskotObject;
}

public class MaskotManager : MonoBehaviour
{
    [SerializeField] private List<MaskotData> maskots = new List<MaskotData>();
    [SerializeField, OnValueChanged("ChangeMaskot")] private MaskotType activeMaskotType = MaskotType.Lili;
    
    private MaskotController currentActiveMaskot;
    private MaskotData currentMaskotData;
    private Vector2 lastPosition = Vector2.zero;
    
    void Start()
    {
        foreach (var maskot in maskots)
            if (maskot.maskotObject != null) maskot.maskotObject.SetActive(false);
        ChangeMaskot();
        
        // Store initial position
        if (currentActiveMaskot?.RectTransform != null)
            lastPosition = currentActiveMaskot.RectTransform.anchoredPosition;
    }
    
    private void ChangeMaskot()
    {
        // Store current position before switching
        if (currentActiveMaskot?.RectTransform != null)
        {
            lastPosition = currentActiveMaskot.RectTransform.anchoredPosition;
            Debug.Log($"Storing position: {lastPosition} from {currentMaskotData?.maskotType}");
        }
        
        // Deactivate current
        if (currentMaskotData?.maskotObject != null)
            currentMaskotData.maskotObject.SetActive(false);
        
        // Activate new
        currentMaskotData = GetMaskotData(activeMaskotType);
        if (currentMaskotData != null)
        {
            if (currentMaskotData.maskotObject != null)
                currentMaskotData.maskotObject.SetActive(true);
            
            currentActiveMaskot = currentMaskotData.controller;
            
            // Use coroutine for reliable position sync
            if (currentActiveMaskot != null)
            {
                // Force position sync immediately
                if (currentActiveMaskot.RectTransform != null)
                {
                    currentActiveMaskot.RectTransform.anchoredPosition = lastPosition;
                    Debug.Log($"Applied position: {lastPosition} to {activeMaskotType}");
                }
                
                currentActiveMaskot.StartIdleMode();
            }
        }
        
        Debug.Log($"Switched to: {activeMaskotType}");
    }
    
    private MaskotData GetMaskotData(MaskotType type)
    {
        foreach (var maskot in maskots)
            if (maskot.maskotType == type) return maskot;
        return null;
    }
    
    // Animation API
    public float PlayHappyAnimation(float duration = 0f) => currentActiveMaskot?.PlayRandomHappyAnimation(duration) ?? 0f;
    public float PlaySadAnimation(float duration = 0f) => currentActiveMaskot?.PlayRandomSadAnimation(duration) ?? 0f;
    public void PlayIdleAnimation() => currentActiveMaskot?.PlayIdleAnimation();
    public float PlaySpecificAnimation(string animationName, float duration = 0f) => currentActiveMaskot?.PlaySpecificAnimation(animationName, duration) ?? 0f;
    public void StartIdleMode() => currentActiveMaskot?.StartIdleMode();
    public void StopIdleMode() => currentActiveMaskot?.StopIdleMode();
    
    // Position API
    public float EnterScreen(bool withAnimation = true, Vector2 onScreenPosition = default, float moveDuration = 1f, Ease moveEase = Ease.OutBounce)
        => currentActiveMaskot?.EnterScreen(withAnimation, onScreenPosition, moveDuration, moveEase) ?? 0f;
    
    public float ExitScreen(bool withAnimation = true, Vector2 offScreenPosition = default, float moveDuration = 1f, Ease moveEase = Ease.OutBounce)
        => currentActiveMaskot?.ExitScreen(withAnimation, offScreenPosition, moveDuration, moveEase) ?? 0f;
    
    public float MoveToPosition(Vector2 targetPosition, float duration = -1f, Ease moveEase = Ease.OutBounce, float moveDuration = 1f)
        => currentActiveMaskot?.MoveToPosition(targetPosition, duration, moveEase, moveDuration) ?? 0f;
    
    /// <summary>
    /// Aktif maskotun pozisyonunu direkt değiştirir (animasyon olmadan)
    /// </summary>
    public void SetPosition(Vector2 position)
    {
        if (currentActiveMaskot?.RectTransform != null)
        {
            currentActiveMaskot.RectTransform.anchoredPosition = position;
            lastPosition = position;
            Debug.Log($"Set position: {position} for {activeMaskotType}");
        }
    }
    
    /// <summary>
    /// Aktif maskotun mevcut pozisyonunu döndürür
    /// </summary>
    public Vector2 GetCurrentPosition() => currentActiveMaskot?.RectTransform?.anchoredPosition ?? Vector2.zero;
    
    // Maskot Management
    public void SetActiveMaskot(MaskotType newMaskotType)
    {
        if (activeMaskotType != newMaskotType)
        {
            activeMaskotType = newMaskotType;
            ChangeMaskot();
        }
    }
    
    public MaskotType GetActiveMaskotType() => activeMaskotType;
    public MaskotController GetActiveMaskotController() => currentActiveMaskot;
    
    // Test Buttons
    [Button("Happy", ButtonSizes.Large), GUIColor(0.3f, 1f, 0.3f)]
    private void TestHappy() => PlayHappyAnimation();
    
    [Button("Sad", ButtonSizes.Large), GUIColor(1f, 0.3f, 0.3f)]
    private void TestSad() => PlaySadAnimation();
    
    [Button("Timed Happy (1s)", ButtonSizes.Medium), GUIColor(0.3f, 0.8f, 1f)]
    private void TestTimedHappy() => PlayHappyAnimation(1f);
    
    [Button("Switch Maskot", ButtonSizes.Medium), GUIColor(1f, 1f, 0.3f)]
    private void TestSwitch()
    {
        var types = System.Enum.GetValues(typeof(MaskotType));
        var currentIndex = System.Array.IndexOf(types, activeMaskotType);
        var nextIndex = (currentIndex + 1) % types.Length;
        SetActiveMaskot((MaskotType)types.GetValue(nextIndex));
    }
    
    [Button("Enter Screen", ButtonSizes.Large), GUIColor(0.3f, 1f, 0.3f)]
    private void TestEnterScreen()
    {
        Vector2 onScreenPos = new Vector2(638.7f, -358.5f);
        EnterScreen(true, onScreenPos, 1f, Ease.InSine);
    }
    
    [Button("Exit Screen", ButtonSizes.Large), GUIColor(1f, 0.3f, 0.3f)]
    private void TestExitScreen()
    {
        Vector2 offScreenPos = new Vector2(1366f, -358.5f);
        ExitScreen(true, offScreenPos, 1f, Ease.InSine);
    }
    
    [Button("Set Position (200, 100)", ButtonSizes.Medium), GUIColor(1f, 1f, 0.3f)]
    private void TestSetPosition()
    {
        SetPosition(new Vector2(200f, 100f));
    }
    
    // Properties
    public bool IsPlayingTimedAnimation => currentActiveMaskot?.IsPlayingTimedAnimation ?? false;
    public bool IsInIdleMode => currentActiveMaskot?.IsInIdleMode ?? false;
    public MaskotType ActiveMaskotType => activeMaskotType;
    public MaskotController ActiveMaskotController => currentActiveMaskot;
} 