using SmartWarehouse.Inventory;
using System;
using System.Collections.Generic;
using UnityEngine;

public class LiveProductCatalog : ProductCatalogListManager
{
    [SerializeField] private SpecificProductUI specificProductPrefab;
    public static LiveProductCatalog Instance { get; private set; }
    ActiveLayout activeLayout;
    Dictionary<string, List<PlacedItemInfo>> storageItems = new Dictionary<string, List<PlacedItemInfo>>();
    private List<SpecificProductUI> productUIs = new List<SpecificProductUI>();
    private List<string> expandUiList = new();

    protected override void Awake()
    {
        base.Awake();
        activeLayout = GetComponent<ActiveLayout>();
        Instance = this;
    }

    private void LoadProduct()
    {
        var warehouseData = activeLayout.GetWarehouseData();
      
        RebuildProductCatalog(warehouseData);
        RefreshCatalogDisplay();
    }

    private void RebuildProductCatalog(WarehouseLayoutData warehouseData)
    {
        storageItems.Clear();
        var shelfs = warehouseData.customShelves;
        for (int shelfId = 0; shelfId < shelfs.Count; shelfId++)
        {
            var shelf = shelfs[shelfId];
            for(int slotId = 0; slotId < shelf.slots.Count; slotId++)
            {
                var slot = shelf.slots[slotId];
                for(int productId = 0; productId < slot.products.Count; productId++)
                {
                    StorageItem product = slot.products[productId];

                    if (product == null) continue;

                    int row = slotId / shelf.columnCount;
                    int col = slotId % shelf.columnCount;

                    if (!storageItems.ContainsKey(product.itemId))
                    {
                        storageItems.Add(product.itemId, new List<PlacedItemInfo>());
                    }

                    storageItems[product.itemId].Add(new PlacedItemInfo
                    {
                        item = product,
                        shelfId = shelfId,
                        slotId = slotId,
                        row = row,
                        column = col
                    });                 
                }

            }
        }
    }


    public override void RefreshCatalogDisplay()
    {
        // 1. Clear old UI elements
        foreach (var row in activeRows)
        {
            if (row != null) Destroy(row.gameObject);
        }
        activeRows.Clear();

        foreach (var product in productUIs)
        {
            if (product != null) Destroy(product.gameObject);
        }
        productUIs.Clear();

        if (ProductRegistry.Instance == null) return;

        // Update total count display based on actual items in warehouse data
        totalProductDisplay.text = activeLayout.GetWarehouseData().GetTotalProducts().ToString();

        List<ProductBase> allProducts = ProductRegistry.Instance.GetAllProducts();
        if (allProducts == null) return;

        // 2. Filtering Logic: Only keep products that exist in our warehouse dictionary AND match the search query
        string searchQuery = searchInputField != null ? searchInputField.text.Trim().ToLower() : "";
        List<ProductBase> filteredProducts = new List<ProductBase>();

        foreach (var p in allProducts)
        {
            // Check if this product actually exists in our stored items dictionary
            if (!storageItems.ContainsKey(p.customCode)) continue;

            bool matchesSearch = string.IsNullOrEmpty(searchQuery) ||
                (p.productName != null && p.productName.ToLower().Contains(searchQuery)) ||
                (p.customCode != null && p.customCode.ToLower().Contains(searchQuery)) ||
                (p.labels != null && p.labels.Exists(l => l.ToLower().Contains(searchQuery)));

            if (matchesSearch)
            {
                filteredProducts.Add(p);
            }
        }

        // 3. Sorting Logic (A-Z / Z-A by Name or Code)
        int sortMode = sortDropdown != null ? sortDropdown.value : 0;
        switch (sortMode)
        {
            case 0: // Product Name (Ascending: A - Z)
                filteredProducts.Sort((a, b) => string.Compare(a.productName, b.productName, System.StringComparison.OrdinalIgnoreCase));
                break;
            case 1: // Product Name (Descending: Z - A)
                filteredProducts.Sort((a, b) => string.Compare(b.productName, a.productName, System.StringComparison.OrdinalIgnoreCase));
                break;
            case 2: // Custom Code (Ascending: A - Z)
                filteredProducts.Sort((a, b) => string.Compare(a.customCode, b.customCode, System.StringComparison.OrdinalIgnoreCase));
                break;
            case 3: // Custom Code (Descending: Z - A)
                filteredProducts.Sort((a, b) => string.Compare(b.customCode, a.customCode, System.StringComparison.OrdinalIgnoreCase));
                break;
        }

        // 4. Instantiate and pass the scattered location list (`storageItems[product.productId]`) to your UI row
        foreach (var product in filteredProducts)
        {
            // Make sure we have placement info for this product
            ProductRowUI rowUI = Instantiate(rowPrefab, contentContainer);
            if (rowUI != null)
            {
                rowUI.Initialize(product, productConfigUI, true, storageItems[product.customCode].Count);
                activeRows.Add(rowUI);
            }


            if(expandUiList.Contains(product.customCode))
            {
                foreach(var item in storageItems[product.customCode])
                {
                    var subItemUi = Instantiate(specificProductPrefab, contentContainer);
                    subItemUi.SetInfo(item, ProductDateWarningInfo(product, item));
                }
            }         
        }

        DynamicContainer(contentContainer.childCount);
    }

    public void ToggleUiDisplay(string productId)
    {
        if(expandUiList.Contains(productId))
            expandUiList.Remove(productId);
        else expandUiList.Add(productId);

        RefreshCatalogDisplay();
    }

    public bool ProductDateWarningInfo(ProductBase productBase, PlacedItemInfo placedItem)
    {
        if(DateTime.TryParse(placedItem.item.expiredAt, out DateTime expiryDate))
        {
            double daysUntilExpiry = (expiryDate - DateTime.Now).TotalDays;

            return daysUntilExpiry < productBase.minDaysBeforeExpiryWarning;
        }
        return false;
    }
}
