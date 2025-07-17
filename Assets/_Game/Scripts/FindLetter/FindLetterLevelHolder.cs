using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using UnityEngine.EventSystems;

public class FindLetterLevelHolder : MonoBehaviour, IPointerClickHandler
{

    // Start is called before the first frame update
     public GameObject spineObject;
    public GameObject spriteObject;
    public GameObject shadowObject;

    [Tooltip("Spine animation name to play on click (leave empty to keep default)")] public string clickAnimationName = "";

    [Header("Color Settings")]
    [Tooltip("Optional: Assign a ColorPalette asset to randomize the sprite's color.")]
    public ColorPalette colorPalette;

    [Tooltip("If true, pick a random color from the palette on Awake")] public bool randomizeColor = true;

    // NEW: Size customization
    [Header("Size Settings")]
    [Tooltip("Scale multiplier for this letter object. Use values < 1 to shrink, > 1 to enlarge.")]
    public float scaleMultiplier = 1f;

    private Vector3 _initialScale;

    void Awake()
    {
        AutoAssignChildren();
        // Ensure initial state early (may get overridden by generator later)
        SetInitialVisibility();

        // Capture the original scale and apply custom multiplier if needed
        _initialScale = transform.localScale;
        ApplyScale();

        // Apply random color if palette is provided
        ApplyRandomColor();
        // If there isn't already a ClickableUIObject or Button, make sure clicks are forwarded
        if (GetComponent<ClickableUIObject>() == null && GetComponent<UnityEngine.UI.Button>() == null)
        {
            gameObject.AddComponent<ClickableUIObject>().OnClick = () => HandleClick();
        }
    }

    // After other scripts (e.g., SetupHolderGraphics) may have changed active states, enforce our rule again.
    void Start()
    {
        SetInitialVisibility();
    }

    // Attempt to auto-link children by common names if inspector references are empty.
    void AutoAssignChildren()
    {
        if (spineObject == null)
        {
            var tf = transform.Find("SpineLetter") ?? transform.Find("SpineHolder");
            if (tf != null) spineObject = tf.gameObject;
        }
        if (spriteObject == null)
        {
            var tf = transform.Find("SpriteLetter") ?? transform.Find("SpriteHolder");
            if (tf != null) spriteObject = tf.gameObject;
        }
        if (shadowObject == null)
        {
            var tf = transform.Find("ShadowLetter") ?? transform.Find("ShadowHolder");
            if (tf != null) shadowObject = tf.gameObject;
        }
    }

    void SetInitialVisibility()
    {
        // At game start only the sprite should be visible. Hide spine and shadow.
        if (spineObject != null) spineObject.SetActive(false);
        if (spriteObject != null) spriteObject.SetActive(true);
        if (shadowObject != null) shadowObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        HandleClick();
    }

    void HandleClick()
    {
        // Toggle visibility: hide sprite & shadow, show spine
        if (spriteObject != null && spriteObject.activeSelf)
            spriteObject.SetActive(false);
        if (shadowObject != null && shadowObject.activeSelf)
            shadowObject.SetActive(false);
        if (spineObject != null && !spineObject.activeSelf)
            spineObject.SetActive(true);

        // Try playing Spine animation
        if (spineObject != null)
        {
            // Try both SkeletonAnimation (runtime) and SkeletonGraphic (UI). Search in children as well.
            Spine.Unity.IAnimationStateComponent stateComponent = spineObject.GetComponentInChildren<Spine.Unity.IAnimationStateComponent>(true);

            if (stateComponent != null && stateComponent.AnimationState != null)
            {
                var state = stateComponent.AnimationState;
                if (!string.IsNullOrEmpty(clickAnimationName))
                {
                    state.SetAnimation(0, clickAnimationName, false);
                }
                else
                {
                    var firstAnim = state.Data?.SkeletonData?.Animations?.Items?[0];
                    if (firstAnim != null)
                        state.SetAnimation(0, firstAnim.Name, false);
                }
            }
            else
            {
                // Fallback to Animator on self or children
                var animator = spineObject.GetComponentInChildren<Animator>(true);
                if (animator != null)
                {
                    animator.SetTrigger("Play");
                }
            }
        }
    }

    void ApplyRandomColor()
    {
        if (!randomizeColor || spriteObject == null) return;

        // Try to fetch palette via inspector; fallback to Resources
        if (colorPalette == null)
        {
            colorPalette = Resources.Load<ColorPalette>("ColorPalette");
        }

        if (colorPalette == null || colorPalette.ColorCount == 0) return;

        Color chosen = colorPalette.GetColor(Random.Range(0, colorPalette.ColorCount));

        // Support UI.Image and SpriteRenderer
        var img = spriteObject.GetComponent<UnityEngine.UI.Image>();
        if (img != null)
        {
            img.color = chosen;
        }
        else
        {
            var sr = spriteObject.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = chosen;
            }
        }
    }

    // NEW: Apply the configured scale multiplier to the root transform
    void ApplyScale()
    {
        // Guard against non-positive multipliers
        float safeMultiplier = scaleMultiplier <= 0 ? 1f : scaleMultiplier;
        transform.localScale = _initialScale * safeMultiplier;
    }
}
