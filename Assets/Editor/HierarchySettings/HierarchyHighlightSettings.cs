using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class HighlightRule
{
    public string nameMatch = "Manager";
    public bool matchBySuffix = true;
    public Color backgroundColor = Color.yellow;
    public Color textColor = Color.white;
    public int selectedIconIndex = 0;
    public string iconName = "d_UnityEditor.InspectorWindow";
    public Texture2D customIcon = null;
}

public class HierarchyHighlightSettings : ScriptableObject
{
    public List<HighlightRule> rules = new List<HighlightRule>();
}
