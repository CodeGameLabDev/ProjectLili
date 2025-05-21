using UnityEditor;
using UnityEngine;

public class HierarchyHighlighter : EditorWindow
{
    private HierarchyHighlightSettings settings;
    private Vector2 scroll;

    private static GUIContent[] _iconOptions;
    private static GUIContent[] iconOptions
    {
        get
        {
            if (_iconOptions == null)
            {
                _iconOptions = new GUIContent[]
                {
                    new GUIContent(EditorGUIUtility.IconContent("d_UnityEditor.InspectorWindow").image, "InspectorWindow"),
                    new GUIContent(EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow").image, "ConsoleWindow"),
                    new GUIContent(EditorGUIUtility.IconContent("console.erroricon").image, "Error"),
                    new GUIContent(EditorGUIUtility.IconContent("console.warnicon").image, "Warning"),
                    new GUIContent(EditorGUIUtility.IconContent("d_Prefab Icon").image, "Prefab"),
                    new GUIContent(EditorGUIUtility.IconContent("Camera Icon").image, "Camera"),
                    new GUIContent(EditorGUIUtility.IconContent("d_UnityEditor.AnimationWindow").image, "Animation"),
                    new GUIContent(EditorGUIUtility.IconContent("d_winbtn_mac_close").image, "Close"),
                    new GUIContent(EditorGUIUtility.IconContent("d_winbtn_mac_max").image, "Maximize"),
                };
            }
            return _iconOptions;
        }
    }

    [MenuItem("Local/Hierarchy Highlighter")]
    public static void ShowWindow()
    {
        GetWindow<HierarchyHighlighter>("Hierarchy Highlighter");
    }

    private void OnEnable()
    {
        LoadSettings();
    }

    private void LoadSettings()
    {
        settings = AssetDatabase.LoadAssetAtPath<HierarchyHighlightSettings>("Assets/Editor/HierarchyHighlightSettings.asset");
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<HierarchyHighlightSettings>();
            AssetDatabase.CreateAsset(settings, "Assets/Editor/HierarchyHighlightSettings.asset");
            AssetDatabase.SaveAssets();
        }
    }

    private void OnGUI()
    {
        if (settings == null)
        {
            LoadSettings();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Highlight Rules", EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        int ruleToRemove = -1;

        for (int i = 0; i < settings.rules.Count; i++)
        {
            var rule = settings.rules[i];
            EditorGUILayout.BeginVertical("box");

            rule.nameMatch = EditorGUILayout.TextField("Match Text", rule.nameMatch);
            rule.matchBySuffix = EditorGUILayout.Toggle("Match by Suffix", rule.matchBySuffix);
            rule.backgroundColor = EditorGUILayout.ColorField("Background Color", rule.backgroundColor);
            rule.textColor = EditorGUILayout.ColorField("Text Color", rule.textColor);

            EditorGUILayout.Space(5);
            //EditorGUILayout.LabelField("Built-in Icon");
            //rule.selectedIconIndex = EditorGUILayout.Popup(rule.selectedIconIndex, iconOptions);
            //rule.iconName = iconOptions[rule.selectedIconIndex].tooltip;

            EditorGUILayout.Space(2);
            rule.customIcon = (Texture2D)EditorGUILayout.ObjectField("Custom Icon", rule.customIcon, typeof(Texture2D), false);

            if (GUILayout.Button("Remove Rule"))
            {
                ruleToRemove = i;
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();

        if (ruleToRemove >= 0)
        {
            settings.rules.RemoveAt(ruleToRemove);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        if (GUILayout.Button("Add New Rule"))
        {
            settings.rules.Add(new HighlightRule());
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        if (GUI.changed)
        {
            EditorApplication.RepaintHierarchyWindow();
            EditorUtility.SetDirty(settings);
        }
    }

    [InitializeOnLoadMethod]
    static void Init()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
    {
        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj == null) return;

        var settings = AssetDatabase.LoadAssetAtPath<HierarchyHighlightSettings>("Assets/Editor/HierarchyHighlightSettings.asset");
        if (settings == null) return;

        foreach (var rule in settings.rules)
        {
            bool match = rule.matchBySuffix ? obj.name.EndsWith(rule.nameMatch) : obj.name == rule.nameMatch;

            if (match)
            {
                EditorGUI.DrawRect(selectionRect, rule.backgroundColor);

                GUIStyle style = new GUIStyle(EditorStyles.label);
                style.normal.textColor = rule.textColor;
                style.fontStyle = FontStyle.Bold;

                Rect labelRect = new Rect(selectionRect.x, selectionRect.y, selectionRect.width - 20f, selectionRect.height);
                EditorGUI.LabelField(labelRect, obj.name, style);

                Texture2D icon = rule.customIcon != null
                    ? rule.customIcon
                    : EditorGUIUtility.IconContent(rule.iconName).image as Texture2D;

                if (icon != null)
                {
                    Rect iconRect = new Rect(selectionRect.xMax - 16f, selectionRect.y + 1f, 16f, 16f);
                    GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
                }

                break;
            }
        }
    }
}
