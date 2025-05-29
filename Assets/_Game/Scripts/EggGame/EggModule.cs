using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public class EggModule : MonoBehaviour
{
    [Header("Tab Buttons")]
    public Button colorButton, patternButton;
    
    [Header("Game Objects")]
    public GameObject colorGameObject, patternGameObject;
    public Button doneButton; 
    
    [Header("Egg Display")]
    public Image eggImage, eggPatterns; 
    Color initialEggColor;
    Sprite initialEggPatternSprite;
    Color initialEggPatternColor;

    [Header("Completed Egg Targets")]
    public Transform[] eggTargets = new Transform[3]; 
    public float animDuration = 0.8f;
    
    [Header("Color Buttons")]
    public Transform colorButtonParent;
    [ListDrawerSettings(ShowIndexLabels = true)] public Button[] colorButtons;
    Image[] colorButtonImages;
    int colorIndex = -1;
    bool colorSelected; 
    
    [Header("Pattern Buttons")]
    public Transform patternButtonParent;
    [ListDrawerSettings(ShowIndexLabels = true)] public Button[] patternButtons;
    Image[] patternButtonImages;
    int patternIndex = -1;
    bool patternSelected; 
    
    void Start()
    {
        colorButton.onClick.AddListener(() => ShowTab(true));
        patternButton.onClick.AddListener(() => ShowTab(false));
        if(doneButton) doneButton.onClick.AddListener(OnDone);
        
        SetupButtons(colorButtons, ref colorButtonImages, true);
        SetupButtons(patternButtons, ref patternButtonImages, false);
        
        if (eggImage) initialEggColor = eggImage.color;
        if (eggPatterns)
        {
            initialEggPatternSprite = eggPatterns.sprite;
            initialEggPatternColor = eggPatterns.color;
            eggPatterns.enabled = false;
        }
        
        ShowTab(true);
        if(doneButton) doneButton.gameObject.SetActive(false); 
    }

    void SetupButtons(Button[] buttons, ref Image[] images, bool isColor)
    {
        if (buttons == null) return;
        images = new Image[buttons.Length];
        
        for (int i = 0; i < buttons.Length; i++)
        {
            if (!buttons[i]) continue;
            images[i] = buttons[i].GetComponent<Image>();
            int index = i;
            buttons[i].onClick.AddListener(() => SelectButton(index, isColor));
            if (images[i]) SetAlpha(images[i], 0);
        }
    }

    void SelectButton(int index, bool isColor)
    {
        if (isColor)
        {
            if (index < 0 || index >= colorButtons.Length || !colorButtons[index]) return;
            if (colorIndex >= 0 && colorButtonImages[colorIndex]) SetAlpha(colorButtonImages[colorIndex], 0);
            if (colorButtonImages[index]) SetAlpha(colorButtonImages[index], 1);
            colorIndex = index;
            colorSelected = true;
            ApplyColor(index);
        }
        else
        {
            if (index < 0 || index >= patternButtons.Length || !patternButtons[index]) return;
            if (patternIndex >= 0 && patternButtonImages[patternIndex]) SetAlpha(patternButtonImages[patternIndex], 0);
            if (patternButtonImages[index]) SetAlpha(patternButtonImages[index], 1);
            patternIndex = index;
            patternSelected = true;
            ApplyPattern(index);
        }
        
        if (doneButton) doneButton.gameObject.SetActive(colorSelected && patternSelected);
    }

    void ApplyColor(int index)
    {
        if (!eggImage || !colorButtons[index]) return;
        var child = colorButtons[index].transform.GetChild(0).GetComponent<Image>();
        if (child)
        {
            var c = child.color;
            c.a = eggImage.color.a;
            eggImage.color = c;
        }
    }
    
    void ApplyPattern(int index)
    {
        if (!eggPatterns || !patternButtons[index]) return;
        var t = patternButtons[index].transform;
        if (t.childCount > 0 && t.GetChild(0).childCount > 0)
        {
            var img = t.GetChild(0).GetChild(0).GetComponent<Image>();
            if (img)
            {
                eggPatterns.enabled = true;
                eggPatterns.sprite = img.sprite;
                var c = img.color;
                c.a = eggPatterns.color.a;
                eggPatterns.color = c;
            }
        }
    }

    void SetAlpha(Image img, float alpha)
    {
        var c = img.color;
        c.a = alpha;
        img.color = c;
    }

    Transform FindEmptySlot()
    {
        if (eggTargets == null) return null;
        foreach (var t in eggTargets)
            if (t && t.childCount == 0) return t;
        return null; 
    }

    void CheckGameEnd()
    {
        if (FindEmptySlot() == null)
        {
            Debug.Log("OYUN BİTTİ!");
        }
    }

    void OnDone()
    {
        var slot = FindEmptySlot();
        if (slot && eggImage)
        {
            var egg = new GameObject("Egg_" + Time.frameCount);
            egg.transform.SetParent(eggImage.transform.parent, false);
            var rect = egg.AddComponent<RectTransform>();
            
            CopyEggTransform(rect);
            CopyVisual(eggImage, egg.transform);
            StartCoroutine(AnimateToSlot(egg, slot));
        }

        ResetAll();
    }

    void CopyEggTransform(RectTransform rect)
    {
        rect.position = eggImage.transform.position;
        rect.localScale = eggImage.transform.localScale; 
        rect.rotation = eggImage.transform.rotation;
        rect.sizeDelta = eggImage.rectTransform.sizeDelta; 
        rect.anchoredPosition = eggImage.rectTransform.anchoredPosition;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    void ResetAll()
    {
        ResetEgg();
        if(doneButton) doneButton.gameObject.SetActive(false);
        colorSelected = patternSelected = false;

        if (colorIndex >= 0 && colorButtonImages[colorIndex]) 
            SetAlpha(colorButtonImages[colorIndex], 0);
        if (patternIndex >= 0 && patternButtonImages[patternIndex]) 
            SetAlpha(patternButtonImages[patternIndex], 0);
        
        colorIndex = patternIndex = -1;
        ShowTab(true); 
    }

    void CopyVisual(Image source, Transform parent)
    {
        foreach (Transform child in parent)
            if (child.name == source.name) return;
        
        var copy = Instantiate(source, parent);
        var copyRect = copy.GetComponent<RectTransform>();
        
        copy.transform.localPosition = Vector3.zero;
        copy.transform.localRotation = Quaternion.identity;
        copy.transform.localScale = Vector3.one;
        copy.raycastTarget = false;
        
        if (copyRect)
        {
            copyRect.anchorMin = Vector2.zero;
            copyRect.anchorMax = Vector2.one;
            copyRect.pivot = new Vector2(0.5f, 0.5f);
            copyRect.sizeDelta = Vector2.zero;
            copyRect.offsetMin = Vector2.zero;
            copyRect.offsetMax = Vector2.zero;
        }
    }

    void SetChildStretch(Transform parent)
    {
        foreach (Transform child in parent)
        {
            var childRect = child.GetComponent<RectTransform>();
            if (childRect)
            {
                childRect.anchorMin = Vector2.zero;
                childRect.anchorMax = Vector2.one;
                childRect.pivot = new Vector2(0.5f, 0.5f);
                childRect.sizeDelta = Vector2.zero;
                childRect.offsetMin = Vector2.zero;
                childRect.offsetMax = Vector2.zero;
            }
        }
    }

    IEnumerator AnimateToSlot(GameObject egg, Transform slot)
    {
        var rect = egg.GetComponent<RectTransform>();
        if (!rect) yield break;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        var start = new { pos = rect.position, rot = rect.rotation, size = rect.sizeDelta };
        var target = new { pos = slot.position, rot = slot.rotation, size = new Vector2(111f, 148f) };

        float t = 0;
        while (t < animDuration)
        {
            t += Time.deltaTime;
            float p = t / animDuration;
            
            rect.position = Vector3.Lerp(start.pos, target.pos, p);
            rect.rotation = Quaternion.Slerp(start.rot, target.rot, p);
            rect.sizeDelta = Vector2.Lerp(start.size, target.size, p);
            
            yield return null;
        }

        rect.position = target.pos;
        rect.rotation = target.rot;
        
        egg.transform.SetParent(slot, false);
        
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        
        rect.position = slot.position;
        rect.sizeDelta = new Vector2(111f, 148f);
        
        SetChildStretch(egg.transform);
        
        for(int i = 0; i < 3; i++)
        {
            yield return null;
            rect.sizeDelta = new Vector2(111f, 148f);
            SetChildStretch(egg.transform);
        }
        
        var targetImage = slot.GetComponent<Image>();
        if (targetImage) targetImage.enabled = false;
        
        CheckGameEnd();
    }

    void ResetEgg()
    {
        if (eggImage) eggImage.color = initialEggColor;
        if (eggPatterns)
        {
            eggPatterns.sprite = initialEggPatternSprite;
            eggPatterns.color = initialEggPatternColor;
            eggPatterns.enabled = false;
        }
    }
    
    void ShowTab(bool showColor)
    {
        if(colorGameObject) colorGameObject.SetActive(showColor);
        if(patternGameObject) patternGameObject.SetActive(!showColor);
    }
}
