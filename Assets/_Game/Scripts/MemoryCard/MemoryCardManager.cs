using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class MemoryCardManager : MonoBehaviour, IGameLevel
{
    [Header("Setup References")]
    [SerializeField] private Transform cardPanel;          // Parent transform with GridLayoutGroup
    [SerializeField] private MemoryCard cardPrefab;        // Card prefab to spawn
    [SerializeField] private CardDatabase cardDatabase;    // Database for card data

    [Header("Gameplay Settings")]
    [SerializeField, Range(0.1f, 5f)] private float hideDelay = 1f; // Delay before flipping back non-matching cards
    
    [Header("Card Type Settings")]
    [SerializeField] public GameMode gameMode = GameMode.Numbers;
    [SerializeField, Range(1, 10)] public int numberOfPairs = 4;
    
    public enum GameMode
    {
        Numbers,    // Only number cards (text)
        Animals,    // Only animal photos
        Objects     // Only object photos
    }

    [Header("Intro References")]
    [SerializeField] private RectTransform liliTransform;   // Placeholder image for Lili
    [SerializeField] private RectTransform liliStartPos;    // Off-screen left anchor
    [SerializeField] private RectTransform liliEndPos;      // Off-screen right anchor
    [SerializeField] private RectTransform cartAnchor;      // Point from which cards are launched

    [Header("Intro Settings")]
    [SerializeField] private float liliMoveDuration = 3f;
    [SerializeField] private float cardLaunchInterval = 0.25f;
    [SerializeField] private float cardLaunchDuration = 1f;
    [SerializeField] private float cardJumpPower = 200f;
    [SerializeField] private float cardStartScale = 0f;

    private MemoryCard firstRevealed;
    private MemoryCard secondRevealed;

    private bool interactionAllowed = false;
    public bool IsInteractionAllowed => interactionAllowed;
    private bool resolvingPairs = false;

    // -------- IGameLevel Implementation Fields --------
    public event System.Action OnGameStart;
    public event System.Action OnGameComplete;
    private bool isCompleted = false;
    [SerializeField] private string levelName = "MemoryCard";
    public bool IsCompleted => isCompleted;
    public string LevelName => levelName;

    private int remainingPairs = 0;

    private List<MemoryCard> spawnedCards = new List<MemoryCard>();
    private GridLayoutGroup gridGroup;

    private void Start()
    {
        // Only cache GridLayoutGroup; actual gameplay setup will occur in StartGame()
        if (cardPanel != null)
            gridGroup = cardPanel.GetComponent<GridLayoutGroup>();
    }

    // ---------------- IGameLevel Methods ----------------
    public void StartGame()
    {
        isCompleted = false;
        interactionAllowed = false;

        // Clean any previously spawned cards (e.g., on replay)
        foreach (var c in spawnedCards)
        {
            if (c != null && c.gameObject.scene.IsValid()) Destroy(c.gameObject);
        }
        spawnedCards.Clear();

        SpawnAndShuffleCards();
        StartCoroutine(IntroAnimation());

        OnGameStart?.Invoke();
    }

    public void CompleteGame()
    {
        if (isCompleted) return;
        isCompleted = true;
        interactionAllowed = false;
        OnGameComplete?.Invoke();
    }

    #region Card Generation

    private void SpawnAndShuffleCards()
    {
        if (cardPrefab == null || cardPanel == null)
        {
            Debug.LogError("MemoryCardManager:: Missing references to prefab or panel");
            return;
        }

        if (cardDatabase == null)
        {
            Debug.LogError("MemoryCardManager:: Missing CardDatabase reference");
            return;
        }

        // Get card data based on game mode
        List<CardData> cardDataList = GetCardDataList();
        
        // Duplicate each card to create pairs
        var cardPairs = new List<CardData>();
        foreach (var cardData in cardDataList)
        {
            cardPairs.Add(cardData);
            cardPairs.Add(cardData); // Duplicate for pair
        }

        Shuffle(cardPairs);

        for (int i = 0; i < cardPairs.Count; i++)
        {
            MemoryCard card = Instantiate(cardPrefab, cardPanel, false);
            card.SetManager(this);
            card.Configure(cardPairs[i]);
            card.ForceFaceDown();

            spawnedCards.Add(card);
        }

        // Each card appears exactly twice => total pairs = cardPairs.Count / 2
        remainingPairs = cardPairs.Count / 2;
    }
    
    private List<CardData> GetCardDataList()
    {
        switch (gameMode)
        {
            case GameMode.Numbers:
                return cardDatabase.GetNumberCards(numberOfPairs);
                
            case GameMode.Animals:
                return cardDatabase.GetAnimalCards(numberOfPairs);
                
            case GameMode.Objects:
                return cardDatabase.GetObjectCards(numberOfPairs);
                
            default:
                return cardDatabase.GetNumberCards(numberOfPairs);
        }
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rnd = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }

    #endregion

    // (Old Reveal/Match logic removed; replaced by Gameplay Matching region below)

    #region Gameplay Matching

    public void OnCardClicked(MemoryCard card)
    {
        if (!interactionAllowed || resolvingPairs)
            return;

        if (firstRevealed == null)
        {
            firstRevealed = card;
            card.RevealAnimated();
            return;
        }

        if (secondRevealed == null && card != firstRevealed)
        {
            secondRevealed = card;
            card.RevealAnimated();
            StartCoroutine(ResolvePairCoroutine());
        }
    }

    private IEnumerator ResolvePairCoroutine()
    {
        resolvingPairs = true;

        // Wait for flip animation to finish
        yield return new WaitForSeconds(0.4f);

        bool isMatch = firstRevealed.PairId == secondRevealed.PairId;
        if (isMatch)
        {
            firstRevealed.VanishAnimated();
            secondRevealed.VanishAnimated();

            remainingPairs--;
            if (remainingPairs <= 0)
            {
                CompleteGame();
            }
        }
        else
        {
            firstRevealed.HideAnimated();
            secondRevealed.HideAnimated();
        }

        // Wait a bit for hide/vanish animations
        yield return new WaitForSeconds(0.5f);

        firstRevealed = null;
        secondRevealed = null;
        resolvingPairs = false;
    }

    #endregion

    private IEnumerator IntroAnimation()
    {
        // Wait one frame so GridLayoutGroup lays out the cards
        yield return new WaitForEndOfFrame();

        // Record grid positions and move cards into cart
        if (cartAnchor != null)
        {
            foreach (var card in spawnedCards)
            {
                RectTransform rt = card.GetComponent<RectTransform>();
                card.TargetAnchoredPos = rt.anchoredPosition; // grid slot
                rt.anchoredPosition = cartAnchor.anchoredPosition; // gather into cart
                rt.localScale = Vector3.one * cardStartScale;
            }
        }

        // Place Lili at start
        if (liliTransform != null && liliStartPos != null)
            liliTransform.anchoredPosition = liliStartPos.anchoredPosition;

        // TODO: Trigger Spine animation -> liliSkeleton.SetAnimation(0, "walk", true);

        // Move Lili across screen
        if (liliTransform != null && liliEndPos != null)
            liliTransform.DOAnchorPos(liliEndPos.anchoredPosition, liliMoveDuration).SetEase(Ease.Linear);

        // Sequentially launch cards
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            yield return new WaitForSeconds(cardLaunchInterval);
            LaunchCard(spawnedCards[i]);
        }

        // Wait until Lili finishes walking
        yield return new WaitForSeconds(liliMoveDuration);

        // TODO: Trigger Spine animation -> liliSkeleton.SetAnimation(0, "idle", true);

        if (gridGroup != null)
            gridGroup.enabled = false; // stop further layout calculations

        interactionAllowed = true;
    }

    private void LaunchCard(MemoryCard card)
    {
        RectTransform rt = card.GetComponent<RectTransform>();

        // Ensure card starts visually at Lili's current world position but in card panel's local space
        if (liliTransform != null)
        {
            Vector3 worldStart = liliTransform.position;
            RectTransform panelRect = cardPanel as RectTransform;
            if (panelRect != null)
            {
                Vector3 localPos = panelRect.InverseTransformPoint(worldStart);
                rt.anchoredPosition = (Vector2)localPos;
            }
            else
            {
                rt.position = worldStart; // fallback
            }
        }

        Vector2 target = card.TargetAnchoredPos;

        Sequence seq = DOTween.Sequence();
        seq.Append(rt.DOJumpAnchorPos(target, cardJumpPower, 1, cardLaunchDuration).SetEase(Ease.OutQuad));
        seq.Join(rt.DORotate(new Vector3(0, 360, 0), cardLaunchDuration, RotateMode.FastBeyond360));
        seq.Join(rt.DOScale(1f, cardLaunchDuration).SetEase(Ease.OutBack));
    }
}
