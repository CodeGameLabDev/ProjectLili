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
    private Sprite assignedSprite;
    private bool isRevealed;
    private bool isMatched;

    // Animation settings (can be tweaked per prefab)
    [SerializeField] private float flipDuration = 0.35f;
    [SerializeField] private float vanishDuration = 0.4f;

    // Stores the final anchored position on the grid panel – used during intro animation
    internal Vector2 TargetAnchoredPos { get; set; }

    public int PairId { get; private set; }
    private string originalLabel;
    private Color pairColor;

    #region Setup

    public void Configure(int pairId, Color pairColor, string label)
    {
        PairId = pairId;
        originalLabel = label;
        this.pairColor = pairColor;
        // Ensure starting colour is white; actual colour will be applied on reveal
        SetBackColor(Color.white);
        if (labelText != null)
            labelText.text = label;
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

    #endregion

    #region Public API

    public void SetSprite(Sprite sprite)
    {
        assignedSprite = sprite;
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
        return assignedSprite;
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
        seq.AppendCallback(() => { ToggleFace(true); SetBackColor(pairColor); if(labelText!=null) labelText.text = originalLabel; });
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
            ToggleFace(false);
            SetBackColor(Color.white);
            if(labelText!=null) labelText.text = "?";
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
        ToggleFace(false);
    }

    private void ToggleFace(bool showFront)
    {
        if (frontImage != null)
            frontImage.gameObject.SetActive(showFront);
        if (labelText != null)
            labelText.gameObject.SetActive(showFront);
        // Keep backSide active in both states to act as coloured border
    }

    #endregion
} 