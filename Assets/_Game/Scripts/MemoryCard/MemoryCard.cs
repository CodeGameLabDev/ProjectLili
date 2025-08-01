using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using DG.Tweening;

public class MemoryCard : MonoBehaviour, IPointerClickHandler
{
    [Header("Card References")] 
    [SerializeField] private Image frontImage;     // Image component that shows the card sprite when revealed
    [SerializeField] private GameObject backSide;  // GameObject that represents the back side (question mark)
    [SerializeField] private TMP_Text labelText;   // Optional label under the image (can be left null)

    private MemoryCardManager manager;
    private CardData cardData;
    private bool isRevealed;
    private bool isMatched;

    // Animation settings (can be tweaked per prefab)
    [SerializeField] private float flipDuration = 0.35f;
    [SerializeField] private float vanishDuration = 0.4f;

    // Stores the final anchored position on the grid panel – used during intro animation
    internal Vector2 TargetAnchoredPos { get; set; }

    public int PairId => cardData?.pairId ?? 0;
    private string originalLabel;
    private Color pairColor;

    // Colours when card is closed
    private static readonly Color closedFrontColor = new Color32(0x60, 0xEF, 0xC3, 0xFF); // teal
    private static readonly Color closedBackColor = Color.white;
    private static readonly Color labelClosedColor = Color.white;
    private static readonly Color labelOpenColor = Color.black;

    #region Setup

    public void Configure(CardData data)
    {
        cardData = data;
        originalLabel = data.displayText;
        this.pairColor = data.pairColor;
        
        // Set sprite if available
        if (data.cardSprite != null)
        {
            frontImage.sprite = data.cardSprite;
        }
        
        // Closed visuals
        SetFrontColor(closedFrontColor);
        SetBackColor(closedBackColor);
        if (labelText != null)
        {
            labelText.text = "?";
            labelText.color = labelClosedColor;
            labelText.gameObject.SetActive(true);
        }
    }

    private void SetBackColor(Color c)
    {
        if (backSide != null)
        {
            Image img = backSide.GetComponent<Image>();
            if (img != null)
                img.color = c;
        }
    }

    private void SetFrontColor(Color c)
    {
        if (frontImage != null)
            frontImage.color = c;
    }

    #endregion

    #region Public API

    public void SetSprite(Sprite sprite)
    {
        if (cardData != null)
            cardData.cardSprite = sprite;
            
        if (frontImage != null)
            frontImage.sprite = sprite;

        if (labelText != null)
            labelText.text = sprite != null ? sprite.name : string.Empty;
    }

    public void SetManager(MemoryCardManager memoryCardManager)
    {
        manager = memoryCardManager;
    }

    public Sprite GetSprite()
    {
        return cardData?.cardSprite;
    }

    #endregion

    #region Interaction

    public void OnPointerClick(PointerEventData eventData)
    {
        if (manager == null || !manager.IsInteractionAllowed)
            return;
        if (isMatched || isRevealed)
            return;

        manager.OnCardClicked(this);
    }

    public Tween RevealAnimated()
    {
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DORotate(new Vector3(0, 90, 0), flipDuration / 2f).SetEase(Ease.InCubic));
        seq.AppendCallback(() => {
            SetFrontColor(Color.white);
            SetBackColor(pairColor);
            if(labelText!=null)
            {
                labelText.text = originalLabel;
                labelText.color = labelOpenColor;
                labelText.gameObject.SetActive(cardData?.showText ?? true);
            }
        });
        seq.Append(transform.DORotate(Vector3.zero, flipDuration / 2f).SetEase(Ease.OutCubic));
        seq.OnComplete(() => { isRevealed = true; });
        return seq;
    }

    public Tween HideAnimated()
    {
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DORotate(new Vector3(0, 90, 0), flipDuration / 2f).SetEase(Ease.InCubic));
        seq.AppendCallback(() =>
        {
            SetFrontColor(closedFrontColor);
            SetBackColor(closedBackColor);
            if(labelText!=null)
            {
                labelText.text = "?";
                labelText.color = labelClosedColor;
            }
        });
        seq.Append(transform.DORotate(Vector3.zero, flipDuration / 2f).SetEase(Ease.OutCubic));
        seq.OnComplete(() => { isRevealed = false; });
        return seq;
    }

    public Tween VanishAnimated()
    {
        isMatched = true;
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(0f, vanishDuration).SetEase(Ease.InBack));
        seq.OnComplete(() => gameObject.SetActive(false));
        return seq;
    }

    // Used at scene start to guarantee card starts hidden without altering colour or label
    public void ForceFaceDown()
    {
        isRevealed = false;
        SetFrontColor(closedFrontColor);
        SetBackColor(closedBackColor);
        if(labelText!=null)
        {
            labelText.text = "?";
            labelText.color = labelClosedColor;
        }
    }

    private void ToggleFace(bool dummy){}

    #endregion
} 