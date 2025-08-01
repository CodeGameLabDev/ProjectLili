using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardDatabase", menuName = "MemoryCard/Card Database")]
public class CardDatabase : ScriptableObject
{
    [Header("Number Cards")]
    [SerializeField] private int maxNumberCards = 10;
    [SerializeField] private Color[] numberCardColors = new Color[]
    {
        Color.red, Color.blue, Color.green, Color.yellow, 
        Color.magenta, Color.cyan, new Color(1f, 0.5f, 0f), new Color(0.5f, 0f, 1f),
        new Color(1f, 0.75f, 0.8f), new Color(0.6f, 0.4f, 0.2f)
    };
    
    [Header("Animal Cards")]
    [SerializeField] private List<ImageCardData> animalCards = new List<ImageCardData>();
    
    [Header("Object Cards")]
    [SerializeField] private List<ImageCardData> objectCards = new List<ImageCardData>();
    
    [System.Serializable]
    public class ImageCardData
    {
        public string cardName;
        public Sprite cardSprite;
        public AudioClip cardSound;
        public Color cardColor = Color.white;
    }
    
    public List<CardData> GetNumberCards(int count)
    {
        var cards = new List<CardData>();
        int actualCount = Mathf.Min(count, maxNumberCards);
        
        for (int i = 1; i <= actualCount; i++)
        {
            Color cardColor = numberCardColors[(i - 1) % numberCardColors.Length];
            cards.Add(CardData.CreateNumberCard(i, cardColor));
        }
        
        return cards;
    }
    
    public List<CardData> GetAnimalCards(int count)
    {
        var cards = new List<CardData>();
        int actualCount = Mathf.Min(count, animalCards.Count);
        
        for (int i = 0; i < actualCount; i++)
        {
            var imageData = animalCards[i];
            cards.Add(CardData.CreateImageCard(i + 1, imageData.cardName, imageData.cardSprite, imageData.cardColor));
        }
        
        return cards;
    }
    
    public List<CardData> GetObjectCards(int count)
    {
        var cards = new List<CardData>();
        int actualCount = Mathf.Min(count, objectCards.Count);
        
        for (int i = 0; i < actualCount; i++)
        {
            var imageData = objectCards[i];
            cards.Add(CardData.CreateImageCard(i + 1, imageData.cardName, imageData.cardSprite, imageData.cardColor));
        }
        
        return cards;
    }
    

    
    public int GetMaxNumberCards() => maxNumberCards;
    public int GetMaxAnimalCards() => animalCards.Count;
    public int GetMaxObjectCards() => objectCards.Count;
} 