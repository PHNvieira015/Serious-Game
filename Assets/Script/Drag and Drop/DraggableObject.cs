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

    private RectTransform rectTransform;
    private Vector2 originalAnchoredPosition;
    private bool isDragging;
    private bool isUsed;
    private Canvas parentCanvas;
    private Canvas ownCanvas;
    private Transform originalParent;
    private int originalSiblingIndex;

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
        isUsed = false;

        blockLayer = LayerMask.NameToLayer("Block Layer");
        Debug.Log($"Block Layer name: Block Layer | Layer number: {blockLayer}");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isUsed) return;

        if (!CanUseCheck())
        {
            Debug.Log($"Not enough {CheckType} remaining!");
            return;
        }

        isDragging = true;
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        originalAnchoredPosition = rectTransform.anchoredPosition;
        ownCanvas.sortingOrder = 9999;

        Debug.Log($"Picked Action: {CheckType}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || isUsed)
            return;

        rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging || isUsed)
        {
            ResetDragState();
            ReturnToOriginalPosition();
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

        Debug.Log($"Raycast hit {results.Count} objects");

        foreach (var result in results)
        {
            if (result.gameObject == gameObject || result.gameObject.transform.IsChildOf(transform))
            {
                Debug.Log($"Skipping: {result.gameObject.name} (self)");
                continue;
            }

            string layerName = LayerMask.LayerToName(result.gameObject.layer);
            Debug.Log($"Checking: {result.gameObject.name} | Layer: {layerName} ({result.gameObject.layer})");

            if (result.gameObject.layer == blockLayer)
            {
                Debug.Log($"Found Block Layer: {result.gameObject.name}");

                if (!CanUseCheck())
                {
                    Debug.Log($"Not enough {CheckType} remaining!");
                    ReturnToOriginalPosition();
                    return;
                }

                Hit();
                return;
            }
        }

        Debug.Log("No Block Layer found under cursor");
        ReturnToOriginalPosition();
    }

    private bool CanUseCheck()
    {
        if (GameManager.Instance == null)
            return true;

        int remaining = GameManager.Instance.GetRemainingChecks(CheckType);
        return remaining > 0;
    }

    private void Hit()
    {
        isUsed = true;
        Debug.Log($"CORRECT - Placed on Block Layer");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UseCheck(CheckType);
            GameManager.Instance.AddScore(10);
        }

        if (correctFeedback != null)
            correctFeedback.SetActive(true);

        Invoke(nameof(DisableObject), 0.5f);
    }

    private void DisableObject()
    {
        gameObject.SetActive(false);
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

    private void ResetDragState()
    {
        isDragging = false;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
        if (ownCanvas != null)
            ownCanvas.sortingOrder = 100;
    }

    public void ResetDraggable()
    {
        isUsed = false;
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
        if (!isUsed)
        {
            ReturnToOriginalPosition();
            if (ownCanvas != null)
                ownCanvas.sortingOrder = 100;
        }
    }
}