using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GridPopulator : MonoBehaviour
{
    [Header("Grid Settings")]
    public GridLayoutGroup gridLayout;
    public GameObject cellPrefab;

    [Header("Data")]
    public List<GridData> gridData = new List<GridData>();

    [System.Serializable]
    public class GridData
    {
        public string month;
        public string content;
    }

    private void Start()
    {
        PopulateGrid();
    }

    public void PopulateGrid()
    {
        if (gridLayout == null)
        {
            gridLayout = GetComponent<GridLayoutGroup>();
        }

        if (cellPrefab == null)
        {
            Debug.LogError("Cell Prefab not assigned!");
            return;
        }

        // Clear existing cells
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Create cells from data
        foreach (GridData data in gridData)
        {
            GameObject cell = Instantiate(cellPrefab, transform);
            cell.SetActive(true);

            // Set text
            TMP_Text[] texts = cell.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 2)
            {
                texts[0].text = data.month;
                texts[1].text = data.content;
            }
        }
    }

    public void SetData(List<GridData> data)
    {
        gridData = data;
        PopulateGrid();
    }
}