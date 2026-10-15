using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShelfRowUI : MonoBehaviour
{
    [SerializeField] private Transform slotContainerParent;
    [SerializeField] private StockSlotUIElement slotPrefab;

    // Direct UI controls on the shelf prefab
    [SerializeField] private TMP_InputField rowInputField;
    [SerializeField] private TMP_InputField columnInputField;
    [SerializeField] private Button deleteShelfButton;

    private CustomShelfData shelfData;
    private List<StockSlotUIElement> spawnedSlotElements = new List<StockSlotUIElement>();
    private bool isInitializing = false;

    public void Initialize(CustomShelfData data)
    {
        isInitializing = true;
        shelfData = data;

        // Set input fields to current dimensions without triggering infinite callbacks
        if (rowInputField != null)
        {
            rowInputField.text = shelfData.rowCount.ToString();
            rowInputField.onEndEdit.RemoveAllListeners();
            rowInputField.onEndEdit.AddListener(OnDimensionsChanged);
        }

        if (columnInputField != null)
        {
            columnInputField.text = shelfData.columnCount.ToString();
            columnInputField.onEndEdit.RemoveAllListeners();
            columnInputField.onEndEdit.AddListener(OnDimensionsChanged);
        }

        if (deleteShelfButton != null)
        {
            deleteShelfButton.onClick.RemoveAllListeners();
            deleteShelfButton.onClick.AddListener(() =>
            {
                WarehouseManager.Instance.DeleteShelf(shelfData.shelfId);
            });
        }

        // Apply grid column constraint
        GridLayoutGroup gridLayout = slotContainerParent.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = shelfData.columnCount;
        }

        // Clear existing slots
        foreach (Transform child in slotContainerParent)
        {
            Destroy(child.gameObject);
        }
        spawnedSlotElements.Clear();

        // Instantiate slots
        for (int i = 0; i < shelfData.slots.Count; i++)
        {
            StockSlotUIElement slotUI = Instantiate(slotPrefab, slotContainerParent);
            
            slotUI.Render(shelfData.slots[i]);
            spawnedSlotElements.Add(slotUI);           
        }

        isInitializing = false;
    }

    private void OnDimensionsChanged(string value)
    {
        if (isInitializing) return;

        // Parse input values safely, fallback to minimum 1 if empty or invalid
        int newRows = 1;
        int newCols = 1;

        if (rowInputField != null && int.TryParse(rowInputField.text, out int r))
        {
            newRows = Mathf.Max(1, r);
        }

        if (columnInputField != null && int.TryParse(columnInputField.text, out int c))
        {
            newCols = Mathf.Max(1, c);

        }

        // Request manager to resize this specific shelf
        WarehouseManager.Instance.ResizeShelf(shelfData.shelfId, newRows, newCols);
    }

    public void RefreshUI()
    {
        for (int i = 0; i < spawnedSlotElements.Count && i < shelfData.slots.Count; i++)
        {
            spawnedSlotElements[i].Render(shelfData.slots[i]);
        }
    }
}