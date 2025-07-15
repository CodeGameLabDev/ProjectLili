using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Generates letter target placeholders (only shadow visible) as direct children of <see cref="autoSlotParent"/>.
/// This lets designers simply drop a FindLetterLevel prefab in the scene, assign the references,
/// and see the shadow versions of the target word laid out in the UI.
/// </summary>
public class FindLetterLevel : MonoBehaviour
{
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

    private void Awake()
    {
        GenerateLetterTargets();
    }

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

        if (type == "Shadow") img.color = new Color(1f, 1f, 1f, 0.3f);
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
