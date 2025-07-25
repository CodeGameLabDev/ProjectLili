using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Generates letter target placeholders (only shadow visible) as direct children of <see cref="autoSlotParent"/>.
/// This lets designers simply drop a FindLetterLevel prefab in the scene, assign the references,
/// and see the shadow versions of the target word laid out in the UI.
/// </summary>
public class FindLetterLevel : MonoBehaviour, IGameLevel
{
    // --------------- IGameLevel implementation -----------------
    public event System.Action OnGameStart;
    public event System.Action OnGameComplete;

    private bool isLevelCompleted = false;
    public bool IsCompleted => isLevelCompleted;
    public string LevelName => targetLetters;

    /// <summary>
    /// Called by <see cref="GameManager"/> to begin the level.
    /// Determines the <see cref="targetLetters"/> string from the active ModuleData
    /// and then spawns the letter placeholders.
    /// </summary>
    public void StartGame()
    {
        // 1) Determine targetLetters from current module data
        SetupTargetLettersFromData();

        // 2) Spawn placeholders
        GenerateLetterTargets();

        isLevelCompleted = false;
        OnGameStart?.Invoke();
    }

    /// <summary>
    /// External callers should invoke this when the player has found all letters.
    /// </summary>
    public void CompleteGame()
    {
        if (isLevelCompleted) return;
        isLevelCompleted = true;
        OnGameComplete?.Invoke();
    }

    // ------------------------------------------------------------------------
    private bool isNumberMode = false;

    private void SetupTargetLettersFromData()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        AlfabeModuleData alfabe = gm.GetAlfabeModuleData();
        NumberModuleData number = gm.GetNumberModuleData();

        if (alfabe != null)
        {
            // Same rule set used in WordGameManager
            if (gm.currentIndex == 0)
            {
                targetLetters = alfabe.UpperCaseLetter.letter.ToString();
            }
            else if (gm.currentIndex == 1)
            {
                targetLetters = alfabe.LowerCaseLetter.letter.ToString();
            }
            else
            {
                targetLetters = alfabe.Word;
            }
            isNumberMode = false;
        }
        else if (number != null)
        {
            targetLetters = number.NumberData.letter.ToString();
            isNumberMode = true;
        }
        // Fallback: keep whatever was set in inspector

        // ---- If single letter (alphabetic) we need 6 placeholders: AaAaAa ----
        if (!string.IsNullOrEmpty(targetLetters) && targetLetters.Length == 1)
        {
            char baseChar = targetLetters[0];
            if (char.IsLetter(baseChar))
            {
                char upper = char.ToUpper(baseChar);
                char lower = char.ToLower(baseChar);
                targetLetters = "" + upper + lower + upper + lower + upper + lower; // "AaAaAa"
            }
        }
    }

    // ------------------------------------------------------------------------
    // PREVIOUS Awake() removed; generation now handled in StartGame
    private void Awake() {}

    [Header("Letter Target Settings")]
    [Tooltip("Target word / letters the player must find.")] public string targetLetters;

    [Tooltip("Prefab that contains SpriteLetter / ShadowLetter objects.")]
    public GameObject letterHolderPrefab;

    [Tooltip("Database used to resolve sprites & prefabs for each letter.")]
    public LetterPathDatabase letterDatabase;

    [Header("Slot Layout")]
    [Tooltip("Parent transform under which letter placeholders will be spawned.")]
    public Transform autoSlotParent;
    [Tooltip("Horizontal spacing (px) between consecutive letters.")]
    public float autoSlotSpacing = 120f;
    [Tooltip("Width & height (px) of each placeholder.")]
    public float autoSlotSize = 100f;

    /// <summary>
    /// Clears existing children of <see cref="autoSlotParent"/> and instantiates one
    /// <paramref name="letterHolderPrefab"/> per letter. Only the shadow graphics are shown.
    /// </summary>
    private void GenerateLetterTargets()
    {
        if (autoSlotParent == null || letterHolderPrefab == null || string.IsNullOrEmpty(targetLetters))
        {
            Debug.LogWarning("FindLetterLevel: Missing references or target letters – generation skipped.");
            return;
        }

        // 1. Clear old children (handy during iterative editing).
        foreach (Transform child in autoSlotParent)
        {
#if UNITY_EDITOR
            DestroyImmediate(child.gameObject);
#else
            Destroy(child.gameObject);
#endif
        }

        // 2. Lay out letters centred around parent.
        string trimmed = targetLetters.Replace(" ", "");
        float startX = -((trimmed.Length - 1) * autoSlotSpacing) / 2f;

        for (int i = 0; i < trimmed.Length; i++)
        {
            char c = trimmed[i];

            GameObject holder = Instantiate(letterHolderPrefab, autoSlotParent);
            holder.name = $"LetterTarget_{c}_{i}";

            // Position & size
            RectTransform holderRt = holder.GetComponent<RectTransform>();
            if (holderRt == null) holderRt = holder.AddComponent<RectTransform>();
            holderRt.anchorMin = holderRt.anchorMax = holderRt.pivot = new Vector2(0.5f, 0.5f);
            holderRt.sizeDelta = new Vector2(autoSlotSize, autoSlotSize);
            holderRt.anchoredPosition = new Vector2(startX + i * autoSlotSpacing, 0f);

            // Graphics (sprite hidden, shadow visible)
            SetupHolderGraphics(holder.transform, c, letterDatabase);
        }
    }

    #region Helper methods (copied from HiddenLetterAssetHolder)
    private void SetupHolderGraphics(Transform holder, char letterChar, LetterPathDatabase db)
    {
        if (db == null) return;
        string id = letterChar.ToString();
        var data = db.LoadLetterData(id);
        if (data == null) return;

        Transform spriteTf = holder.Find("SpriteLetter") ?? holder.Find("SpriteHolder");
        Transform shadowTf = holder.Find("ShadowLetter") ?? holder.Find("ShadowHolder");
        Transform spineTf  = holder.Find("SpineLetter")  ?? holder.Find("SpineHolder");

        SetupLetterComponent(spriteTf, data.letterSprite, "Sprite");
        SetupLetterComponent(shadowTf, data.letterShadowSprite, "Shadow");
        if (data.prefab != null) SetupSpineComponent(spineTf, data.prefab);

        // Keep only shadow visible initially
        if (shadowTf != null) shadowTf.gameObject.SetActive(true);
        if (spriteTf != null) spriteTf.gameObject.SetActive(false);
    }

    private void SetupLetterComponent(Transform tf, Sprite sprite, string type)
    {
        if (tf == null || sprite == null) return;
        var rect = tf.GetComponent<RectTransform>() ?? tf.gameObject.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        var img = tf.GetComponent<Image>() ?? tf.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.SetNativeSize();

        if (type == "Shadow")
        {
            img.color = new Color(1f, 1f, 1f, 0.3f);
        }
        else if (type == "Sprite")
        {
            Color c = img.color;
            img.color = new Color(c.r, c.g, c.b, 1f); // ensure full opacity
        }
    }

    private void SetupSpineComponent(Transform tf, GameObject prefab)
    {
        if (tf == null || prefab == null) return;
        var rect = tf.GetComponent<RectTransform>() ?? tf.gameObject.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        // Preserve prefab offset and localScale
        rect.anchoredPosition = prefab.GetComponent<RectTransform>().anchoredPosition;
        rect.localScale = Vector3.one;

        GameObject inst = Instantiate(prefab, tf);
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localRotation = Quaternion.identity;
        inst.transform.localScale = Vector3.one;
    }
    #endregion
}
