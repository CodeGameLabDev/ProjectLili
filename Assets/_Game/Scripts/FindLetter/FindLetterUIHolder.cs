using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FindLetterUIHolder : MonoBehaviour
{
    public GameObject spineObject;
    public GameObject spriteObject;
    public GameObject shadowObject;

    void Awake()
    {
        AutoAssignChildren();
    }

    void AutoAssignChildren()
    {
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
        if (spineObject == null)
        {
            var tf = transform.Find("SpineLetter") ?? transform.Find("SpineHolder");
            if (tf != null) spineObject = tf.gameObject;
        }
    }

    public void ShowSpriteHideShadow()
    {
        if (shadowObject != null) shadowObject.SetActive(false);
        if (spriteObject != null) spriteObject.SetActive(true);
        if (spineObject != null) spineObject.SetActive(false);

        // Force full opacity on sprite image or renderer
        if (spriteObject != null)
        {
            var img = spriteObject.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                Color c = img.color;
                if (c.a < 1f)
                    img.color = new Color(c.r, c.g, c.b, 1f);
            }
            else
            {
                var sr = spriteObject.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Color c = sr.color;
                    if (c.a < 1f)
                        sr.color = new Color(c.r, c.g, c.b, 1f);
                }
            }
        }
    }

    [Header("Color Settings")]
    [Tooltip("Optional: Assign a ColorPalette asset to randomize the sprite's color.")]
    public ColorPalette colorPalette;
    [Tooltip("If true, pick a random color from the palette on Start")] public bool randomizeColor = true;

    void Start()
    {
        ApplyRandomColor();
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
        var img = spriteObject.GetComponent<Image>();
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
    // Start is called before the first frame update
    
}
