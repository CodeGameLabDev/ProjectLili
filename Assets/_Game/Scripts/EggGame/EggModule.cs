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
    
    [Header("Game Objects")]
    public GameObject colorGameObject;
    public GameObject patternGameObject;
    
    [Header("Egg")]
    public Image eggImage; // Yumurta image referansı
    public Image eggPatterns; // Yumurta desen image referansı
    
    [Header("Color Button System")]
    public Transform colorButtonParent; // Color butonları parent'ı
    
    [Button("Initialize Color Buttons")]
    public void InitializeColorButtons()
    {
        if (colorButtonParent == null) return;
        
        // Color parent altındaki tüm butonları al
        colorButtons = new List<Button>(colorButtonParent.GetComponentsInChildren<Button>());
        colorButtonImages = new Image[colorButtons.Count];
        
        // Image componentlerini al
        for (int i = 0; i < colorButtons.Count; i++)
        {
            colorButtonImages[i] = colorButtons[i].GetComponent<Image>();
        }
    }
    
    [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "name")]
    public List<Button> colorButtons = new List<Button>(); // Color butonları listesi
    private Image[] colorButtonImages; // Color butonların image componentleri
    private int currentSelectedColorIndex = -1; // Şu an seçili olan color buton indexi
    
    [Header("Pattern Button System")]
    public Transform patternButtonParent; // Pattern butonları parent'ı
    
    [Button("Initialize Pattern Buttons")]
    public void InitializePatternButtons()
    {
        if (patternButtonParent == null) return;
        
        // Pattern parent altındaki tüm butonları al
        patternButtons = new List<Button>(patternButtonParent.GetComponentsInChildren<Button>());
        patternButtonImages = new Image[patternButtons.Count];
        
        // Image componentlerini al
        for (int i = 0; i < patternButtons.Count; i++)
        {
            patternButtonImages[i] = patternButtons[i].GetComponent<Image>();
        }
    }
    
    [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "name")]
    public List<Button> patternButtons = new List<Button>(); // Pattern butonları listesi
    private Image[] patternButtonImages; // Pattern butonların image componentleri
    private int currentSelectedPatternIndex = -1; // Şu an seçili olan pattern buton indexi
    
    // Start is called before the first frame update
    void Start()
    {
        // Buton event'lerini bağla
        colorButton.onClick.AddListener(OnColorButtonClicked);
        patternButton.onClick.AddListener(OnPatternButtonClicked);
        
        // Child butonları setup et
        SetupColorButtons();
        SetupPatternButtons();
        
        // Başlangıçta Color sekmesini aktif yap
        ShowColorTab();
        eggPatterns.enabled = false;

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    // Color butonları setup et
    private void SetupColorButtons()
    {
        if (colorButtons == null || colorButtons.Count == 0) return;
        
        colorButtonImages = new Image[colorButtons.Count];
        
        // Her color buton için image component'ini al ve event listener ekle
        for (int i = 0; i < colorButtons.Count; i++)
        {
            if (colorButtons[i] == null) continue;
            
            colorButtonImages[i] = colorButtons[i].GetComponent<Image>();
            
            // Local variable kullanarak closure problemi çöz
            int index = i;
            colorButtons[i].onClick.AddListener(() => SelectColorButton(index));
            
            // Başlangıçta tüm butonların alpha'sını 0 yap
            if (colorButtonImages[i] != null)
            {
                Color color = colorButtonImages[i].color;
                color.a = 0f;
                colorButtonImages[i].color = color;
            }
        }
    }
    
    // Pattern butonları setup et
    private void SetupPatternButtons()
    {
        if (patternButtons == null || patternButtons.Count == 0) return;
        
        patternButtonImages = new Image[patternButtons.Count];
        
        // Her pattern buton için image component'ini al ve event listener ekle
        for (int i = 0; i < patternButtons.Count; i++)
        {
            if (patternButtons[i] == null) continue;
            
            patternButtonImages[i] = patternButtons[i].GetComponent<Image>();
            
            // Local variable kullanarak closure problemi çöz
            int index = i;
            patternButtons[i].onClick.AddListener(() => SelectPatternButton(index));
            
            // Başlangıçta tüm butonların alpha'sını 0 yap
            if (patternButtonImages[i] != null)
            {
                Color color = patternButtonImages[i].color;
                color.a = 0f;
                patternButtonImages[i].color = color;
            }
        }
    }
    
    // Color buton seç
    public void SelectColorButton(int index)
    {
        if (index < 0 || index >= colorButtons.Count) return;
        
        // Önceki seçili color butonun alpha'sını 0 yap
        if (currentSelectedColorIndex >= 0 && currentSelectedColorIndex < colorButtonImages.Length)
        {
            if (colorButtonImages[currentSelectedColorIndex] != null)
            {
                Color prevColor = colorButtonImages[currentSelectedColorIndex].color;
                prevColor.a = 0f;
                colorButtonImages[currentSelectedColorIndex].color = prevColor;
            }
        }
        
        // Yeni seçili color butonun alpha'sını 1 yap
        if (colorButtonImages[index] != null)
        {
            Color newColor = colorButtonImages[index].color;
            newColor.a = 1f;
            colorButtonImages[index].color = newColor;
        }
        
        // Şu anki seçili color index'i güncelle
        currentSelectedColorIndex = index;
        
        // Renk uygula
        ApplyColorToEgg(index);
    }
    
    // Pattern buton seç
    public void SelectPatternButton(int index)
    {
        if (index < 0 || index >= patternButtons.Count) return;
        
        // Önceki seçili pattern butonun alpha'sını 0 yap
        if (currentSelectedPatternIndex >= 0 && currentSelectedPatternIndex < patternButtonImages.Length)
        {
            if (patternButtonImages[currentSelectedPatternIndex] != null)
            {
                Color prevColor = patternButtonImages[currentSelectedPatternIndex].color;
                prevColor.a = 0f;
                patternButtonImages[currentSelectedPatternIndex].color = prevColor;
            }
        }
        
        // Yeni seçili pattern butonun alpha'sını 1 yap
        if (patternButtonImages[index] != null)
        {
            Color newColor = patternButtonImages[index].color;
            newColor.a = 1f;
            patternButtonImages[index].color = newColor;
        }
        
        // Şu anki seçili pattern index'i güncelle
        currentSelectedPatternIndex = index;
        
        // Desen uygula
        ApplyPatternToEgg(index);
    }
    
    // Seçilen color butonun rengini yumurtaya uygula
    private void ApplyColorToEgg(int buttonIndex)
    {
        if (eggImage == null || buttonIndex < 0 || buttonIndex >= colorButtons.Count) return;
        if (colorButtons[buttonIndex] == null) return;
        
        // Seçilen color butonun ilk child'ındaki Image component'ini al
        Transform buttonTransform = colorButtons[buttonIndex].transform;
        if (buttonTransform.childCount > 0)
        {
            Image childImage = buttonTransform.GetChild(0).GetComponent<Image>();
            if (childImage != null)
            {
                // Child image'in rengini yumurtaya uygula
                Color eggColor = childImage.color;
                eggColor.a = eggImage.color.a; // Yumurtanın alpha'sını koru
                eggImage.color = eggColor;
            }
        }
    }
    
    // Seçilen pattern butonun desenini yumurtaya uygula
    private void ApplyPatternToEgg(int buttonIndex)
    {
        if (eggPatterns == null || buttonIndex < 0 || buttonIndex >= patternButtons.Count) return;
        if (patternButtons[buttonIndex] == null) return;
        
        // Seçilen pattern butonun child(0).child(0)'ındaki Image component'ini al
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
                    // Pattern image'in sprite'ını ve rengini yumurta desenine uygula
                    eggPatterns.sprite = patternImage.sprite;
                    Color patternColor = patternImage.color;
                    patternColor.a = eggPatterns.color.a; // Yumurta deseninin alpha'sını koru
                    eggPatterns.color = patternColor;
                }
            }
        }
    }
    
    // Color butonuna tıklandığında çağrılır
    public void OnColorButtonClicked()
    {
        ShowColorTab();
    }
    
    // Pattern butonuna tıklandığında çağrılır
    public void OnPatternButtonClicked()
    {
        ShowPatternTab();
    }
    
    // Color sekmesini göster
    private void ShowColorTab()
    {
        colorGameObject.SetActive(true);
        patternGameObject.SetActive(false);
    }
    
    // Pattern sekmesini göster
    private void ShowPatternTab()
    {
        colorGameObject.SetActive(false);
        patternGameObject.SetActive(true);
    }
}
