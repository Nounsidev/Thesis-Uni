using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SmartWarehouse.Inventory;

public class ShelfRowUI : MonoBehaviour
{
    [Header("Row UI handler")]
    [SerializeField] private Transform slotContainerParent;
    [SerializeField] private StockSlotUIElement slotPrefab;

    // Direct UI controls on the shelf prefab
    [Header("Shelf Customization")]
    [SerializeField] private TMP_InputField rowInputField;
    [SerializeField] private TMP_InputField columnInputField;
    [SerializeField] private Button deleteShelfButton;
    [SerializeField] private TMP_InputField priorityInputField;



    private CustomShelfData shelfData;
    private List<StockSlotUIElement> spawnedSlotElements = new List<StockSlotUIElement>();
    private bool isInitializing = false;

    public void Initialize(CustomShelfData data)
    {
        isInitializing = false;
        shelfData = data;
        Debug.Log("Shelf Init");
        // Set input fields to current dimensions without triggering infinite callbacks
        if (rowInputField != null)
        {
            rowInputField.text = shelfData.rowCount.ToString();
            rowInputField.onEndEdit.RemoveListener(OnDimensionsChanged);
            rowInputField.onEndEdit.AddListener(OnDimensionsChanged);
        }

        if (columnInputField != null)
        {
            columnInputField.text = shelfData.columnCount.ToString();
            columnInputField.onEndEdit.RemoveListener(OnDimensionsChanged);
            columnInputField.onEndEdit.AddListener(OnDimensionsChanged);
        }

        if (priorityInputField != null)
        { 
            RefreshPriorityUI();
            priorityInputField.onEndEdit.RemoveListener(OnPriorityChange);
            priorityInputField.onEndEdit.AddListener(OnPriorityChange);
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

        isInitializing = true;
    }

    private void OnDimensionsChanged(string value)
    {
        if (!isInitializing) return;

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
        Debug.Log($"Requesting resize for Shelf ID {shelfData.shelfId} to {newRows} rows and {newCols} columns.");
        WarehouseManager.Instance.ResizeShelf(shelfData.shelfId, newRows, newCols);
    }

    private void OnPriorityChange(string value)
    {
        if (!isInitializing)
        {
            //Debug.Log("Not Init");
            return;
        }
        
        value = value.ToUpper();
        string[] codes = value.Split(',');
        //Debug.Log("Processing priority input: " + value);
        shelfData.prioritizedProducts.Clear();
        bool isAllValid = true;
        foreach (var code in codes)
        {
            string trimmedCode = code.Trim();
            if (string.IsNullOrEmpty(trimmedCode)) continue;

            bool isValidProduct = ProductRegistry.Instance.CheckProductExist(trimmedCode);
            if (isValidProduct && !shelfData.prioritizedProducts.Contains(trimmedCode))
            {
                shelfData.prioritizedProducts.Add(trimmedCode);
                Debug.Log("Valid product added: " + trimmedCode);
            }
            else             {
                Debug.Log("Invalid or duplicate product code: " + trimmedCode);
                isAllValid = false;
            }
        }

        if (!isAllValid) return;
        WarehouseManager.Instance.PriorityChange(shelfData.shelfId, shelfData.prioritizedProducts);
    }

    public void RefreshPriorityUI()
    {
        priorityInputField.text = string.Join(", ", shelfData.prioritizedProducts);
    }

    public void RefreshUI()
    {
        for (int i = 0; i < spawnedSlotElements.Count && i < shelfData.slots.Count; i++)
        {
            spawnedSlotElements[i].Render(shelfData.slots[i]);
        }
    }
}