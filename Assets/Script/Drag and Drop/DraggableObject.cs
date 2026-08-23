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

    [Header("Spawn Settings")]
    public DraggableObject spawnPrefab;
    public bool canSpawnMultiple = true;

    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private Canvas ownCanvas;

    private bool isDragging;
    private bool isPlaced;
    private bool isSpawnedInstance;

    private int blockLayer;

    private DraggableObject spawnedObject;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

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

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        blockLayer = LayerMask.NameToLayer("Block Layer");

        isPlaced = false;
        isDragging = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isPlaced)
        {
            return;
        }

        if (!CanUseCheck())
        {
            Debug.Log($"Not enough {CheckType} remaining!");
            return;
        }

        if (!isSpawnedInstance && canSpawnMultiple)
        {
            SpawnDraggableObject(eventData);
            return;
        }

        StartDragging();
    }

    private void SpawnDraggableObject(PointerEventData eventData)
    {
        DraggableObject prefabToSpawn = spawnPrefab;

        if (prefabToSpawn == null)
        {
            prefabToSpawn = this;
        }

        spawnedObject = Instantiate(prefabToSpawn, parentCanvas.transform);

        spawnedObject.isSpawnedInstance = true;
        spawnedObject.isPlaced = false;
        spawnedObject.isDragging = true;

        RectTransform spawnedRect = spawnedObject.GetComponent<RectTransform>();

        spawnedRect.position = rectTransform.position;

        spawnedObject.BeginSpawnedDrag();

        Debug.Log($"Spawned copy of {gameObject.name}");
    }

    private void BeginSpawnedDrag()
    {
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        ownCanvas.sortingOrder = 9999;
    }

    private void StartDragging()
    {
        isDragging = true;

        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        ownCanvas.sortingOrder = 9999;

        Debug.Log($"Picked Action: {CheckType}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isSpawnedInstance)
        {
            if (!isDragging)
            {
                return;
            }

            MoveWithPointer(eventData);
            return;
        }

        if (spawnedObject != null)
        {
            if (!spawnedObject.gameObject.activeInHierarchy)
            {
                spawnedObject = null;
                return;
            }

            spawnedObject.MoveWithPointer(eventData);
            return;
        }

        if (!isDragging)
        {
            return;
        }

        MoveWithPointer(eventData);
    }

    private void MoveWithPointer(PointerEventData eventData)
    {
        if (parentCanvas == null)
        {
            return;
        }

        rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isSpawnedInstance)
        {
            if (!isDragging)
            {
                return;
            }

            isDragging = false;

            FinishDrag(eventData);
            return;
        }

        if (spawnedObject != null)
        {
            if (!spawnedObject.gameObject.activeInHierarchy)
            {
                spawnedObject = null;
                return;
            }

            spawnedObject.isDragging = false;
            spawnedObject.FinishDrag(eventData);

            spawnedObject = null;

            return;
        }

        if (!isDragging)
        {
            return;
        }

        isDragging = false;

        FinishDrag(eventData);
    }

    private void FinishDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        ownCanvas.sortingOrder = 100;

        TryPlace(eventData);
    }

    private void TryPlace(PointerEventData eventData)
    {
        var results = new System.Collections.Generic.List<RaycastResult>();

        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject == gameObject)
            {
                continue;
            }

            if (result.gameObject.transform.IsChildOf(transform))
            {
                continue;
            }

            if (result.gameObject.layer == blockLayer)
            {
                if (!CanUseCheck())
                {
                    Debug.Log($"Not enough {CheckType} remaining!");

                    DestroySpawnedObject();
                    return;
                }

                Hit(result);
                return;
            }
        }

        Miss();

        DestroySpawnedObject();
    }

    private bool CanUseCheck()
    {
        if (GameManager.Instance == null)
        {
            return true;
        }

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
            {
                correctFeedback.SetActive(true);
            }
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

        if (blockRect == null)
        {
            Debug.LogWarning("Could not find RectTransform on block.");
            return;
        }

        DraggableObject existingObject = blockRect.GetComponentInChildren<DraggableObject>();

        if (existingObject != null && existingObject != this)
        {
            Debug.Log($"Replacing {existingObject.gameObject.name} with {gameObject.name}");

            Destroy(existingObject.gameObject);
        }

        transform.SetParent(blockRect);

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        rectTransform.anchoredPosition = Vector2.zero;

        ownCanvas.sortingOrder = 200;

        canvasGroup.blocksRaycasts = true;

        Debug.Log($"Placed {gameObject.name} on top of {blockRect.name}");
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
        {
            wrongFeedback.SetActive(false);
        }
    }

    private void DestroySpawnedObject()
    {
        if (isSpawnedInstance)
        {
            Destroy(gameObject);
        }
        else
        {
            ResetDragState();
        }
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
            ownCanvas.sortingOrder = 100;
        }
    }

    public void ResetDraggable()
    {
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
    }

    private void OnEnable()
    {
        if (!isPlaced)
        {
            if (ownCanvas != null)
            {
                ownCanvas.sortingOrder = 100;
            }
        }
    }
}