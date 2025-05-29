using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public class EggModule : MonoBehaviour
{
    [Header("Tab Buttons")]
    public Button colorButton;
    public Button patternButton;
    public Button doneButton;
    
    [Header("Game Objects")]
    public GameObject colorGameObject;
    public GameObject patternGameObject;
    
    [Header("Egg")]
    public Image eggImage;
    public Image eggPatterns;
    private Color initialEggColor;
    private Sprite initialEggPatternSprite;
    private Color initialEggPatternColor;
    
    [Header("Color Button System")]
    public Transform colorButtonParent;
    
    [Button("Initialize Color Buttons")]
    public void InitializeColorButtons()
    {
        if (colorButtonParent == null) return;
        colorButtons = new List<Button>(colorButtonParent.GetComponentsInChildren<Button>());
        colorButtonImages = new Image[colorButtons.Count];
        for (int i = 0; i < colorButtons.Count; i++)
            colorButtonImages[i] = colorButtons[i].GetComponent<Image>();
    }
    
    [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "name")]
    public List<Button> colorButtons = new List<Button>();
    private Image[] colorButtonImages;
    private int currentSelectedColorIndex = -1;
    private bool colorSelected = false;
    
    [Header("Pattern Button System")]
    public Transform patternButtonParent;
    
    [Button("Initialize Pattern Buttons")]
    public void InitializePatternButtons()
    {
        if (patternButtonParent == null) return;
        patternButtons = new List<Button>(patternButtonParent.GetComponentsInChildren<Button>());
        patternButtonImages = new Image[patternButtons.Count];
        for (int i = 0; i < patternButtons.Count; i++)
            patternButtonImages[i] = patternButtons[i].GetComponent<Image>();
    }
    
    [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "name")]
    public List<Button> patternButtons = new List<Button>();
    private Image[] patternButtonImages;
    private int currentSelectedPatternIndex = -1;
    private bool patternSelected = false;
    
    void Start()
    {
        colorButton.onClick.AddListener(ShowColorTab);
        patternButton.onClick.AddListener(ShowPatternTab);
        doneButton.onClick.AddListener(OnDoneButtonClicked);
        
        SetupColorButtons();
        SetupPatternButtons();
        
        StoreInitialEggState();
        
        ShowColorTab();
        eggPatterns.enabled = false;
        doneButton.gameObject.SetActive(false);
    }

    private void StoreInitialEggState()
    {
        if (eggImage != null) initialEggColor = eggImage.color;
        if (eggPatterns != null)
        {
            initialEggPatternSprite = eggPatterns.sprite;
            initialEggPatternColor = eggPatterns.color;
        }
    }

    private void CheckDoneButtonState()
    {
        if (doneButton != null)
        {
            doneButton.gameObject.SetActive(colorSelected && patternSelected);
        }
    }
    
    private void OnDoneButtonClicked()
    {
        ResetEgg();
        doneButton.gameObject.SetActive(false);
        colorSelected = false;
        patternSelected = false;

        if (currentSelectedColorIndex != -1 && colorButtonImages[currentSelectedColorIndex] != null)
        {
            Color prevColor = colorButtonImages[currentSelectedColorIndex].color;
            prevColor.a = 0f;
            colorButtonImages[currentSelectedColorIndex].color = prevColor;
        }
        if (currentSelectedPatternIndex != -1 && patternButtonImages[currentSelectedPatternIndex] != null)
        {
            Color prevPattern = patternButtonImages[currentSelectedPatternIndex].color;
            prevPattern.a = 0f;
            patternButtonImages[currentSelectedPatternIndex].color = prevPattern;
        }
        currentSelectedColorIndex = -1;
        currentSelectedPatternIndex = -1;
        
        ShowColorTab();
    }

    private void ResetEgg()
    {
        if (eggImage != null) eggImage.color = initialEggColor;
        if (eggPatterns != null)
        {
            eggPatterns.sprite = initialEggPatternSprite;
            eggPatterns.color = initialEggPatternColor;
            eggPatterns.enabled = false;
        }
    }
    
    private void SetupColorButtons()
    {
        if (colorButtons == null || colorButtons.Count == 0) return;
        colorButtonImages = new Image[colorButtons.Count];
        
        for (int i = 0; i < colorButtons.Count; i++)
        {
            if (colorButtons[i] == null) continue;
            colorButtonImages[i] = colorButtons[i].GetComponent<Image>();
            int index = i;
            colorButtons[i].onClick.AddListener(() => SelectColorButton(index));
            if (colorButtonImages[i] != null)
            {
                Color color = colorButtonImages[i].color;
                color.a = 0f;
                colorButtonImages[i].color = color;
            }
        }
    }
    
    private void SetupPatternButtons()
    {
        if (patternButtons == null || patternButtons.Count == 0) return;
        patternButtonImages = new Image[patternButtons.Count];
        
        for (int i = 0; i < patternButtons.Count; i++)
        {
            if (patternButtons[i] == null) continue;
            patternButtonImages[i] = patternButtons[i].GetComponent<Image>();
            int index = i;
            patternButtons[i].onClick.AddListener(() => SelectPatternButton(index));
            if (patternButtonImages[i] != null)
            {
                Color color = patternButtonImages[i].color;
                color.a = 0f;
                patternButtonImages[i].color = color;
            }
        }
    }
    
    public void SelectColorButton(int index)
    {
        if (index < 0 || index >= colorButtons.Count) return;
        
        if (currentSelectedColorIndex != -1 && colorButtonImages[currentSelectedColorIndex] != null)
        {
            Color prevColor = colorButtonImages[currentSelectedColorIndex].color;
            prevColor.a = 0f;
            colorButtonImages[currentSelectedColorIndex].color = prevColor;
        }
        
        if (colorButtonImages[index] != null)
        {
            Color newColor = colorButtonImages[index].color;
            newColor.a = 1f;
            colorButtonImages[index].color = newColor;
        }
        
        currentSelectedColorIndex = index;
        colorSelected = true;
        ApplyColorToEgg(index);
        CheckDoneButtonState();
    }
    
    public void SelectPatternButton(int index)
    {
        if (index < 0 || index >= patternButtons.Count) return;
        
        if (currentSelectedPatternIndex != -1 && patternButtonImages[currentSelectedPatternIndex] != null)
        {
            Color prevColor = patternButtonImages[currentSelectedPatternIndex].color;
            prevColor.a = 0f;
            patternButtonImages[currentSelectedPatternIndex].color = prevColor;
        }
        
        if (patternButtonImages[index] != null)
        {
            Color newColor = patternButtonImages[index].color;
            newColor.a = 1f;
            patternButtonImages[index].color = newColor;
        }
        
        currentSelectedPatternIndex = index;
        patternSelected = true;
        ApplyPatternToEgg(index);
        CheckDoneButtonState();
    }
    
    private void ApplyColorToEgg(int buttonIndex)
    {
        if (eggImage == null || buttonIndex < 0 || buttonIndex >= colorButtons.Count || colorButtons[buttonIndex] == null) return;
        
        Transform buttonTransform = colorButtons[buttonIndex].transform;
        if (buttonTransform.childCount > 0)
        {
            Image childImage = buttonTransform.GetChild(0).GetComponent<Image>();
            if (childImage != null)
            {
                Color eggColor = childImage.color;
                eggColor.a = eggImage.color.a;
                eggImage.color = eggColor;
            }
        }
    }
    
    private void ApplyPatternToEgg(int buttonIndex)
    {
        if (eggPatterns == null || buttonIndex < 0 || buttonIndex >= patternButtons.Count || patternButtons[buttonIndex] == null) return;
        
        Transform buttonTransform = patternButtons[buttonIndex].transform;
        if (buttonTransform.childCount > 0)
        {
            Transform firstChild = buttonTransform.GetChild(0);
            if (firstChild.childCount > 0)
            {
                Image patternImage = firstChild.GetChild(0).GetComponent<Image>();
                if (patternImage != null)
                {
                    eggPatterns.enabled = true;
                    eggPatterns.sprite = patternImage.sprite;
                    Color patternColor = patternImage.color;
                    patternColor.a = eggPatterns.color.a;
                    eggPatterns.color = patternColor;
                }
            }
        }
    }
    
    private void ShowColorTab()
    {
        colorGameObject.SetActive(true);
        patternGameObject.SetActive(false);
    }
    
    private void ShowPatternTab()
    {
        colorGameObject.SetActive(false);
        patternGameObject.SetActive(true);
    }
}
