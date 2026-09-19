using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableObject :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
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

    [Header("Spawn Settings")]
    public DraggableObject spawnPrefab;
    public bool canSpawnMultiple = true;

    public bool IsPlaced => isPlaced;
    public bool IsSpawnedInstance => isSpawnedInstance;
    public Block CurrentBlock => currentBlock;

    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private Canvas ownCanvas;

    private bool isDragging;
    private bool isPlaced;
    private bool isSpawnedInstance;

    private int blockLayer;

    private DraggableObject spawnedObject;
    private Block currentBlock;

    private Vector3 dragStartPosition;
    private bool hasDragStartPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        Canvas containingCanvas = GetComponentInParent<Canvas>();

        if (containingCanvas != null)
        {
            parentCanvas = containingCanvas.rootCanvas;
        }

        ownCanvas = GetComponent<Canvas>();

        if (ownCanvas == null)
        {
            ownCanvas = gameObject.AddComponent<Canvas>();
        }

        ownCanvas.overrideSorting = true;
        ownCanvas.sortingOrder = 100;

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        blockLayer = LayerMask.NameToLayer("Block Layer");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isPlaced || !CanUseCheck(CheckType))
        {
            return;
        }

        if (!isSpawnedInstance && canSpawnMultiple)
        {
            if (parentCanvas == null)
            {
                Debug.LogWarning("[DRAG] Parent Canvas is missing.");
                return;
            }

            spawnedObject = CreateCopy(
                parentCanvas.transform,
                CheckType
            );

            if (spawnedObject == null)
            {
                return;
            }

            spawnedObject.rectTransform.position =
                rectTransform.position;

            spawnedObject.StartDragging();
            return;
        }

        StartDragging();
    }

    private DraggableObject CreateCopy(
        Transform parent,
        CheckType type)
    {
        DraggableObject template =
            spawnPrefab != null ? spawnPrefab : this;

        if (template.GetComponent<RectTransform>() == null)
        {
            Debug.LogWarning(
                "[PLACEMENT] Icon prefab needs a RectTransform."
            );

            return null;
        }

        DraggableObject copy = Instantiate(
            template,
            parent,
            false
        );

        // The requested type is authoritative for the new copy.
        copy.CheckType = type;
        copy.isSpawnedInstance = true;
        copy.isPlaced = false;
        copy.isDragging = false;
        copy.canSpawnMultiple = false;
        copy.currentBlock = null;
        copy.spawnedObject = null;

        if (!copy.gameObject.activeSelf)
        {
            copy.gameObject.SetActive(true);
        }

        return copy;
    }

    private void StartDragging()
    {
        isDragging = true;

        dragStartPosition = rectTransform.position;
        hasDragStartPosition = true;

        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        ownCanvas.sortingOrder = 9999;

        Debug.Log("[DRAG] Picked " + CheckType);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (spawnedObject != null)
        {
            if (spawnedObject.isDragging)
            {
                spawnedObject.MoveWithPointer(eventData);
            }

            return;
        }

        if (isDragging)
        {
            MoveWithPointer(eventData);
        }
    }

    private void MoveWithPointer(PointerEventData eventData)
    {
        if (rectTransform == null)
        {
            return;
        }

        RectTransform parentRect =
            rectTransform.parent as RectTransform;

        if (parentRect == null)
        {
            return;
        }

        Vector3 pointerPosition;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out pointerPosition))
        {
            rectTransform.position = pointerPosition;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (spawnedObject != null)
        {
            DraggableObject copy = spawnedObject;
            spawnedObject = null;

            copy.FinishDrag(eventData);
            return;
        }

        if (isDragging)
        {
            FinishDrag(eventData);
        }
    }

    private void FinishDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        isDragging = false;

        // Keep the icon out of raycasts while finding the block.
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 1f;

        Block target = FindDropBlock(eventData);

        if (target == null)
        {
            Miss();
            DestroySpawnedObject();
            return;
        }

        // Both click and drag finish through this method.
        if (!TryPlaceOnBlock(target))
        {
            DestroySpawnedObject();
        }
    }

    private Block FindDropBlock(PointerEventData eventData)
    {
        if (EventSystem.current == null)
        {
            return null;
        }

        List<RaycastResult> hits = new List<RaycastResult>();

        EventSystem.current.RaycastAll(eventData, hits);

        foreach (RaycastResult hit in hits)
        {
            if (hit.gameObject == gameObject ||
                hit.gameObject.transform.IsChildOf(transform))
            {
                continue;
            }

            Block target = hit.gameObject.GetComponentInParent<Block>();

            if (target == null)
            {
                ArticleBlockView view =
                    hit.gameObject.GetComponentInParent<ArticleBlockView>();

                if (view != null)
                {
                    target = view.blockComponent;
                }
            }

            if (target == null)
            {
                continue;
            }

            if (hit.gameObject.layer == blockLayer ||
                target.gameObject.layer == blockLayer)
            {
                return target;
            }
        }

        return null;
    }

    private bool CanUseCheck(CheckType type)
    {
        if (GameManager.Instance == null)
        {
            return true;
        }

        if (GameManager.Instance.GetRemainingChecks(type) > 0)
        {
            return true;
        }

        Debug.LogWarning(
            "[PLACEMENT] Not enough " + type + " remaining."
        );

        return false;
    }

    private ArticleBlockView FindBlockView(Block target)
    {
        ArticleBlockView view =
            target.GetComponent<ArticleBlockView>();

        if (view != null)
        {
            return view;
        }

        view = target.GetComponentInParent<ArticleBlockView>();

        if (view != null &&
            (view.blockComponent == null ||
             view.blockComponent == target))
        {
            return view;
        }

        // Supports a view that references a Block on another object.
        ArticleBlockView[] views =
            target.transform.root.GetComponentsInChildren<ArticleBlockView>(
                true
            );

        foreach (ArticleBlockView candidate in views)
        {
            if (candidate.blockComponent == target)
            {
                return candidate;
            }
        }

        return null;
    }

    private bool TryPlaceOnBlock(Block target)
    {
        if (target == null || rectTransform == null)
        {
            Debug.LogWarning(
                "[PLACEMENT] Missing Block or icon RectTransform."
            );

            return false;
        }

        if (isPlaced)
        {
            return currentBlock == target;
        }

        if (!CanUseCheck(CheckType))
        {
            return false;
        }

        ArticleBlockView view = FindBlockView(target);

        if (view == null)
        {
            Debug.LogWarning(
                "[PLACEMENT] No ArticleBlockView found for " +
                target.name
            );

            return false;
        }

        if (view.ChecklistContainer == null)
        {
            Debug.LogWarning(
                "[PLACEMENT] Assign Checklist Container on " +
                view.name
            );

            return false;
        }

        // Verify the new icon can be positioned before removing the old one.
        if (!view.PlaceIconInChecklist(this))
        {
            return false;
        }

        DraggableObject previous = target.CurrentDraggable;

        if (previous != null && previous != this)
        {
            previous.currentBlock = null;

            target.RemoveDraggable(previous);

            previous.gameObject.SetActive(false);

            Destroy(previous.gameObject);
        }

        currentBlock = target;
        isPlaced = true;
        isDragging = false;

        target.SetDraggable(this);

        // Updates the player mark, mark label, and text color.
        view.SetMarkColorFromDraggable(CheckType);

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;

        // Once placed, render as part of the article UI.
        ownCanvas.overrideSorting = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UseCheck(CheckType);
            GameManager.Instance.AddScore(10);
        }

        if (correctFeedback != null)
        {
            correctFeedback.SetActive(true);
        }

        Debug.Log(
            "[PLACEMENT] Block: " +
            target.name +
            " | Mark: " +
            CheckType +
            " | Container: " +
            view.ChecklistContainer.name
        );

        return true;
    }

    // Preserve the original method for existing callers.
    public void PlaceCopyOnBlock(Block block)
    {
        TryPlaceCopyOnBlock(block, CheckType);
    }

    public bool TryPlaceCopyOnBlock(
        Block block,
        CheckType selectedType)
    {
        if (block == null || !CanUseCheck(selectedType))
        {
            return false;
        }

        ArticleBlockView view = FindBlockView(block);

        if (view == null || view.ChecklistContainer == null)
        {
            Debug.LogWarning(
                "[PLACEMENT] Assign ArticleBlockView and its " +
                "Checklist Container for " +
                block.name
            );

            return false;
        }

        DraggableObject copy = CreateCopy(
            view.ChecklistContainer,
            selectedType
        );

        if (copy == null)
        {
            return false;
        }

        // Identical mark and icon placement logic to drag-and-drop.
        bool success = copy.TryPlaceOnBlock(block);

        if (!success)
        {
            Destroy(copy.gameObject);
        }

        return success;
    }

    private void Miss()
    {
        Debug.Log("[DRAG] No valid block under the pointer.");

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
        {
            wrongFeedback.SetActive(false);
        }
    }

    private void DestroySpawnedObject()
    {
        if (isSpawnedInstance)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        if (hasDragStartPosition && rectTransform != null)
        {
            rectTransform.position = dragStartPosition;
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
        {
            ownCanvas.overrideSorting = true;
            ownCanvas.sortingOrder = 100;
        }
    }

    public void ResetDraggable()
    {
        Block previousBlock = currentBlock;
        currentBlock = null;

        if (previousBlock != null &&
            previousBlock.CurrentDraggable == this)
        {
            previousBlock.RemoveDraggable(this);

            ArticleBlockView view = FindBlockView(previousBlock);

            if (view != null)
            {
                view.ClearPlayerSelection();
            }
        }

        isPlaced = false;
        isDragging = false;

        if (correctFeedback != null)
        {
            correctFeedback.SetActive(false);
        }

        if (wrongFeedback != null)
        {
            wrongFeedback.SetActive(false);
        }

        ResetDragState();

        // A reset copy should not remain as a stale checklist icon.
        if (isSpawnedInstance)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        if (!isPlaced && ownCanvas != null)
        {
            ownCanvas.overrideSorting = true;
            ownCanvas.sortingOrder = 100;
        }
    }

    private void OnDestroy()
    {
        if (currentBlock != null &&
            currentBlock.CurrentDraggable == this)
        {
            currentBlock.RemoveDraggable(this);
        }
    }
}