using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableObject : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Verification")]
    public CheckType CheckType;

    [Header("Visual Feedback")]
    public GameObject correctFeedback;
    public GameObject wrongFeedback;

    [Header("UI Settings")]
    public CanvasGroup canvasGroup;

    [Header("Placement Settings")]
    public bool stayOnBlock = true;

    private RectTransform rectTransform;
    private Vector2 originalAnchoredPosition;
    private bool isDragging;
    private bool isPlaced;
    private Canvas parentCanvas;
    private Canvas ownCanvas;
    private Transform originalParent;
    private int originalSiblingIndex;
    private Vector2 placedAnchoredPosition;
    private Transform placedParent;

    private int blockLayer;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        ownCanvas = GetComponent<Canvas>();
        if (ownCanvas == null)
        {
            ownCanvas = gameObject.AddComponent<Canvas>();
            ownCanvas.overrideSorting = true;
            ownCanvas.sortingOrder = 100;
        }
        else
        {
            ownCanvas.overrideSorting = true;
            ownCanvas.sortingOrder = 100;
        }

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        originalAnchoredPosition = rectTransform.anchoredPosition;
        isPlaced = false;

        blockLayer = LayerMask.NameToLayer("Block Layer");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanUseCheck())
        {
            Debug.Log($"Not enough {CheckType} remaining!");
            return;
        }

        isDragging = true;
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        if (isPlaced)
        {
            placedParent = transform.parent;
            placedAnchoredPosition = rectTransform.anchoredPosition;

            transform.SetParent(parentCanvas.transform);
            transform.SetAsLastSibling();
        }
        else
        {
            originalAnchoredPosition = rectTransform.anchoredPosition;
        }

        ownCanvas.sortingOrder = 9999;

        Debug.Log($"Picked Action: {CheckType}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            ResetDragState();
            return;
        }

        isDragging = false;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        ownCanvas.sortingOrder = 100;

        TryPlace(eventData);
    }

    private void TryPlace(PointerEventData eventData)
    {
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            if (result.gameObject == gameObject || result.gameObject.transform.IsChildOf(transform))
            {
                continue;
            }

            if (result.gameObject.layer == blockLayer)
            {
                if (!CanUseCheck())
                {
                    Debug.Log($"Not enough {CheckType} remaining!");
                    ReturnToOriginalPosition();
                    return;
                }

                Hit(result);
                return;
            }
        }

        if (isPlaced)
        {
            ReturnToPlacedPosition();
        }
        else
        {
            ReturnToOriginalPosition();
        }
    }

    private bool CanUseCheck()
    {
        if (GameManager.Instance == null)
            return true;

        int remaining = GameManager.Instance.GetRemainingChecks(CheckType);
        return remaining > 0;
    }

    private void Hit(RaycastResult result)
    {
        if (!isPlaced)
        {
            isPlaced = true;
            Debug.Log($"CORRECT - Placed on Block Layer: {result.gameObject.name}");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.UseCheck(CheckType);
                GameManager.Instance.AddScore(10);
            }

            if (correctFeedback != null)
                correctFeedback.SetActive(true);
        }
        else
        {
            Debug.Log($"MOVED - Placed on Block Layer: {result.gameObject.name}");
        }

        StayOnBlock(result);
    }

    private void StayOnBlock(RaycastResult result)
    {
        RectTransform blockRect = result.gameObject.GetComponent<RectTransform>();
        if (blockRect == null && result.gameObject.transform.parent != null)
        {
            blockRect = result.gameObject.transform.parent.GetComponent<RectTransform>();
        }

        if (blockRect != null)
        {
            transform.SetParent(blockRect);

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;

            ownCanvas.sortingOrder = 200;

            Debug.Log($"Placed {gameObject.name} on top of {blockRect.name}");
        }
        else
        {
            Debug.LogWarning("Could not find RectTransform on block, staying at current position");
        }

        canvasGroup.blocksRaycasts = true;
    }

    private void Miss()
    {
        Debug.Log($"WRONG - Not on Block Layer");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddMistake();
        }

        if (wrongFeedback != null)
        {
            wrongFeedback.SetActive(true);
            Invoke(nameof(HideWrongFeedback), 0.5f);
        }
    }

    private void HideWrongFeedback()
    {
        if (wrongFeedback != null)
            wrongFeedback.SetActive(false);
    }

    private void ReturnToOriginalPosition()
    {
        transform.SetParent(originalParent);
        transform.SetSiblingIndex(originalSiblingIndex);
        rectTransform.anchoredPosition = originalAnchoredPosition;
        ResetDragState();
    }

    private void ReturnToPlacedPosition()
    {
        if (placedParent != null)
        {
            transform.SetParent(placedParent);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = placedAnchoredPosition;
            ownCanvas.sortingOrder = 200;
        }
        else
        {
            ReturnToOriginalPosition();
        }
        ResetDragState();
    }

    private void ResetDragState()
    {
        isDragging = false;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
        if (ownCanvas != null && !isPlaced)
            ownCanvas.sortingOrder = 100;
    }

    public void ResetDraggable()
    {
        isPlaced = false;
        isDragging = false;
        gameObject.SetActive(true);
        ReturnToOriginalPosition();

        if (correctFeedback != null)
            correctFeedback.SetActive(false);

        if (wrongFeedback != null)
            wrongFeedback.SetActive(false);

        ResetDragState();
    }

    private void OnEnable()
    {
        if (!isPlaced)
        {
            ReturnToOriginalPosition();
            if (ownCanvas != null)
                ownCanvas.sortingOrder = 100;
        }
    }
}