using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// Simple component that notifies its parent <see cref="FeedAnimalLevel"/> when clicked.
/// </summary>
public class FoodItem : MonoBehaviour, IPointerClickHandler
{
    private FeedAnimalLevel parentLevel;
    private Transform targetPosition;
    private float moveDuration = 0.5f;

    private bool hasMoved = false;

    private void Awake()
    {
        if (parentLevel == null)
        {
            parentLevel = GetComponentInParent<FeedAnimalLevel>();
        }
    }

    /// <summary>
    /// Called by the parent level to set the target position and other parameters.
    /// </summary>
    public void Initialize(FeedAnimalLevel level, Transform target, float duration)
    {
        parentLevel = level;
        targetPosition = target;
        moveDuration = duration;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (hasMoved) return; // prevent double clicks
        hasMoved = true;
        // Disable further clicks
        var col2d = GetComponent<Collider2D>();
        if (col2d != null) col2d.enabled = false;
        // Animate to target
        if (targetPosition != null)
        {
            transform.DOMove(targetPosition.position, moveDuration).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                parentLevel?.FoodMoved(this);
            });
        }
        else
        {
            // If target not set, immediately notify
            parentLevel?.FoodMoved(this);
        }
    }
} 