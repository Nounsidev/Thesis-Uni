using UnityEngine;

public class ActiveLayout : WarehouseLayoutBase
{
    [SerializeField] WarehouseManager warehouseEditor;
    public static ActiveLayout Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void SetActiveLayout(WarehouseLayoutData layout)
    {
        currentLayout = layout;
    }    

    public WarehouseLayoutData GetWarehouseData()
    {
        return currentLayout;
    }

    // --- AUTO-STACKING STOCK LOGIC ---
    public void AddToWarehouse(StorageItem product, int shelfId, int slotId)
    {
        StockSlot slot = currentLayout.customShelves[shelfId].slots[slotId];

        if(slot.IsEmpty)
            slot.products[0] = product;
        else slot.products[1] = product;

        SaveNewChange();
    }

    public void RemoveFromWarehouse(StorageItem product, int shelfId, int slotId)
    {
        StockSlot slot = currentLayout.customShelves[shelfId].slots[slotId];

        int id = slot.products.IndexOf(product);
        slot.products[id] = null;

        SaveNewChange();
    }

    private async void SaveNewChange()
    {
        LoadingIcon.Instance.Show();
        await CloudDataManager.SaveWarehouseLayoutData(currentLayout);
        LoadingIcon.Instance.Hide();
    }

    protected override Vector2 RebuildWarehouseUI()
    {
        base.RebuildWarehouseUI();
        RefreshAllUI();
        return Vector2.zero;
    }

    private void RefreshAllUI()
    {
        foreach (var kvp in activeShelfUIElements)
        {
            if (kvp.Value != null) kvp.Value.RefreshUI();
        }
    }

    public void LoadStorageData(WarehouseLayoutData layoutData)
    {
        currentLayout = layoutData;
        warehouseEditor.LoadLayoutData(layoutData);

        RebuildWarehouseUI();
    }
}
