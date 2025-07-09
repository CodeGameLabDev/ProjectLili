using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class MemoryCardManager : MonoBehaviour
{
    [Header("Setup References")]
    [SerializeField] private Transform cardPanel;          // Parent transform with GridLayoutGroup
    [SerializeField] private MemoryCard cardPrefab;        // Card prefab to spawn

    [Header("Gameplay Settings")]
    [SerializeField, Range(0.1f, 5f)] private float hideDelay = 1f; // Delay before flipping back non-matching cards

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

    private MemoryCard firstRevealed;
    private MemoryCard secondRevealed;

    private bool interactionAllowed = false;
    public bool IsInteractionAllowed => interactionAllowed;
    private bool resolvingPairs = false;

    private List<MemoryCard> spawnedCards = new List<MemoryCard>();

    private void Start()
    {
        SpawnAndShuffleCards();
        StartCoroutine(IntroAnimation());
    }

    #region Card Generation

    private void SpawnAndShuffleCards()
    {
        if (cardPrefab == null || cardPanel == null)
        {
            Debug.LogError("MemoryCardManager:: Missing references to prefab or panel");
            return;
        }

        // Build numbered pairs 1-4, duplicate each, assign random colour per pair
        var cardInfoList = new List<(int number, Color color)>();
        for (int n = 1; n <= 4; n++)
        {
            Color pairColor = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.7f, 1f);
            cardInfoList.Add((n, pairColor));
            cardInfoList.Add((n, pairColor));
        }

        Shuffle(cardInfoList);

        for (int i = 0; i < cardInfoList.Count; i++)
        {
            MemoryCard card = Instantiate(cardPrefab, cardPanel, false);
            card.SetManager(this);
            card.Configure(cardInfoList[i].number, cardInfoList[i].color, cardInfoList[i].number.ToString());
            card.ForceFaceDown();

            spawnedCards.Add(card);
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

        interactionAllowed = true;
    }

    private void LaunchCard(MemoryCard card)
    {
        RectTransform rt = card.GetComponent<RectTransform>();
        Vector2 target = card.TargetAnchoredPos;

        Sequence seq = DOTween.Sequence();
        seq.Append(rt.DOJumpAnchorPos(target, cardJumpPower, 1, cardLaunchDuration).SetEase(Ease.OutQuad));
        seq.Join(rt.DORotate(new Vector3(0, 360, 0), cardLaunchDuration, RotateMode.FastBeyond360));
    }
}
