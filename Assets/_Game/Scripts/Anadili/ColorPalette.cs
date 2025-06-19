using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "ColorPalette", menuName = "Anadili/Color Palette")]
public class ColorPalette : ScriptableObject
{
    [TabGroup("Colors")]
    [LabelText("Renk Paleti")]
    public List<Color> colors = new List<Color>();
    
    public Color GetColor(int index)
    {
        if (colors.Count == 0) return Color.white;
        return colors[index % colors.Count];
    }
    
    public int ColorCount => colors.Count;
} 