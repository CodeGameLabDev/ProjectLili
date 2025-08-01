using UnityEngine;

[System.Serializable]
public class CardData
{
    public enum CardType
    {
        Number,
        Image
    }

    [Header("Card Identity")]
    public int pairId;
    public CardType cardType;
    public string cardName;
    
    [Header("Visual Data")]
    public Sprite cardSprite;
    public Color pairColor = Color.white;
    
    [Header("Audio")]
    public AudioClip cardSound;
    
    [Header("Display Settings")]
    public string displayText;
    public bool showText = true;
    
    public CardData(int id, CardType type, string name, Sprite sprite = null, Color color = default)
    {
        pairId = id;
        cardType = type;
        cardName = name;
        cardSprite = sprite;
        pairColor = color == default ? Color.white : color;
        displayText = name;
    }
    
    public static CardData CreateNumberCard(int number, Color color)
    {
        return new CardData(number, CardType.Number, number.ToString(), null, color)
        {
            displayText = number.ToString(),
            showText = true
        };
    }
    
    public static CardData CreateImageCard(int id, string name, Sprite sprite, Color color)
    {
        return new CardData(id, CardType.Image, name, sprite, color)
        {
            displayText = name,
            showText = false
        };
    }
} 