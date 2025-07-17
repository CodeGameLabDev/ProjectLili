using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FindLetterUIHolder : MonoBehaviour
{
    public GameObject spineObject;
    public GameObject spriteObject;
    public GameObject shadowObject;

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
