using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using Spine;
using Sirenix.OdinInspector;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System.Threading;

public class MaskotController : MonoBehaviour
{
    [SerializeField] private SkeletonGraphic skeletonGraphic;
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private List<string> happyAnimations = new List<string> { "happy1", "happy2", "celebration" };
    [SerializeField] private List<string> sadAnimations = new List<string> { "sad1", "sad2", "disappointed" };
    [SerializeField] private List<string> idleAnimations = new List<string> { "idle1", "idle2", "idle_blink" };
    [SerializeField] private List<string> enterAnimations = new List<string> { "enter", "appear" };
    [SerializeField] private List<string> exitAnimations = new List<string> { "exit", "disappear" };
    [SerializeField] private float defaultAnimationSpeed = 1f;
    
    private bool isPlayingTimedAnimation, isInIdleMode;
    private CancellationTokenSource animationCancellationTokenSource;

    void Start()
    {
        if (skeletonGraphic == null) skeletonGraphic = GetComponent<SkeletonGraphic>();
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (skeletonGraphic?.AnimationState != null) skeletonGraphic.AnimationState.Complete += OnAnimationComplete;
        StartIdleMode();
    }
    
    void OnDestroy()
    {
        StopTimedAnimation();
        if (skeletonGraphic?.AnimationState != null) skeletonGraphic.AnimationState.Complete -= OnAnimationComplete;
    }
    
    // Animation Control
    public void StartIdleMode() { isInIdleMode = true; PlayRandomIdleAnimationInternal(); }
    public void StopIdleMode() => isInIdleMode = false;
    
    private void OnAnimationComplete(Spine.TrackEntry trackEntry)
    {
        if (isInIdleMode && !isPlayingTimedAnimation) PlayRandomIdleAnimationInternal();
        else if (!isInIdleMode && !isPlayingTimedAnimation) StartIdleMode();
    }
    
    public float PlayRandomHappyAnimation(float duration = 0f)
    {
        StopIdleMode();
        if (happyAnimations.Count == 0) return 0f;
        return PlaySpecificAnimation(happyAnimations[Random.Range(0, happyAnimations.Count)], duration);
    }
    
    public float PlayRandomSadAnimation(float duration = 0f)
    {
        StopIdleMode();
        if (sadAnimations.Count == 0) return 0f;
        return PlaySpecificAnimation(sadAnimations[Random.Range(0, sadAnimations.Count)], duration);
    }
    
    public float PlayRandomIdleAnimation(float duration = 0f)
    {
        StopIdleMode();
        if (idleAnimations.Count == 0) return 0f;
        return PlaySpecificAnimation(idleAnimations[Random.Range(0, idleAnimations.Count)], duration);
    }
    
    private void PlayRandomIdleAnimationInternal()
    {
        if (idleAnimations.Count == 0) return;
        PlayAnimation(idleAnimations[Random.Range(0, idleAnimations.Count)], false);
    }
    
    private string GetRandomEnterAnimation() => enterAnimations.Count > 0 ? enterAnimations[Random.Range(0, enterAnimations.Count)] : "";
    private string GetRandomExitAnimation() => exitAnimations.Count > 0 ? exitAnimations[Random.Range(0, exitAnimations.Count)] : "";
    
    public float PlaySpecificAnimation(string animationName, float duration = 0f)
    {
        StopIdleMode();
        if (skeletonGraphic == null) return 0f;
        
        var animation = skeletonGraphic.Skeleton.Data.FindAnimation(animationName);
        if (animation == null) { Debug.LogWarning($"Animation '{animationName}' not found!"); return 0f; }
        
        float animationDuration = animation.Duration;
        if (duration > 0f) { PlayTimedAnimation(animationName, duration); return duration; }
        else { PlayAnimation(animationName, false); return animationDuration; }
    }
    
    public void PlayIdleAnimation() { StopTimedAnimation(); StartIdleMode(); }
    
    private void PlayAnimation(string animationName, bool loop)
    {
        if (skeletonGraphic == null) return;
        skeletonGraphic.AnimationState.SetAnimation(0, animationName, loop);
        skeletonGraphic.timeScale = defaultAnimationSpeed;
    }
    
    private void PlayTimedAnimation(string animationName, float duration)
    {
        StopTimedAnimation();
        animationCancellationTokenSource = new CancellationTokenSource();
        TimedAnimationAsync(animationName, duration, animationCancellationTokenSource.Token).Forget();
    }
    
    private void StopTimedAnimation()
    {
        if (animationCancellationTokenSource != null)
        {
            animationCancellationTokenSource.Cancel();
            animationCancellationTokenSource.Dispose();
            animationCancellationTokenSource = null;
        }
        isPlayingTimedAnimation = false;
    }
    
    private async UniTask TimedAnimationAsync(string animationName, float duration, CancellationToken cancellationToken)
    {
        try
        {
            isPlayingTimedAnimation = true;
            PlayAnimation(animationName, true);
            await UniTask.Delay((int)(duration * 1000), cancellationToken: cancellationToken);
            isPlayingTimedAnimation = false;
            StartIdleMode();
        }
        catch (System.OperationCanceledException) { isPlayingTimedAnimation = false; }
        finally
        {
            if (animationCancellationTokenSource != null)
            {
                animationCancellationTokenSource.Dispose();
                animationCancellationTokenSource = null;
            }
        }
    }
    
    // Position Control
    public float EnterScreen(bool withAnimation = true, Vector2 onScreenPosition = default, float moveDuration = 1f, Ease moveEase = Ease.OutBounce)
    {
        if (withAnimation)
        {
            string enterAnim = GetRandomEnterAnimation();
            if (!string.IsNullOrEmpty(enterAnim)) PlayAnimation(enterAnim, false);
        }
        if (rectTransform != null) rectTransform.DOAnchorPos(onScreenPosition, moveDuration).SetEase(moveEase);
        return moveDuration;
    }
    
    public float ExitScreen(bool withAnimation = true, Vector2 offScreenPosition = default, float moveDuration = 1f, Ease moveEase = Ease.OutBounce)
    {
        if (withAnimation)
        {
            string exitAnim = GetRandomExitAnimation();
            if (!string.IsNullOrEmpty(exitAnim)) PlayAnimation(exitAnim, false);
        }
        if (rectTransform != null) rectTransform.DOAnchorPos(offScreenPosition, moveDuration).SetEase(moveEase);
        return moveDuration;
    }
    
    public float MoveToPosition(Vector2 targetPosition, float duration = -1f, Ease moveEase = Ease.OutBounce, float moveDuration = 1f)
    {
        float moveDur = duration > 0 ? duration : moveDuration;
        if (rectTransform != null) rectTransform.DOAnchorPos(targetPosition, moveDur).SetEase(moveEase);
        return moveDur;
    }
    
    // Test Buttons
    [Button("Happy", ButtonSizes.Large), GUIColor(0.3f, 1f, 0.3f)]
    private void TestHappy() => PlayRandomHappyAnimation();
    
    [Button("Sad", ButtonSizes.Large), GUIColor(1f, 0.3f, 0.3f)]
    private void TestSad() => PlayRandomSadAnimation();
    
    [Button("Timed (1s)", ButtonSizes.Medium), GUIColor(0.3f, 0.8f, 1f)]
    private void TestTimed() => PlayRandomHappyAnimation(1f);
    
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
    
    // Properties
    public bool IsPlayingTimedAnimation => isPlayingTimedAnimation;
    public bool IsInIdleMode => isInIdleMode;
    public SkeletonGraphic SkeletonGraphic => skeletonGraphic;
    public RectTransform RectTransform => rectTransform;
}
