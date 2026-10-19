using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class WarehouseLayoutData
{
    public List<CustomShelfData> customShelves = new List<CustomShelfData>();
}

[Serializable]
public class CustomShelfData
{
    public int shelfId;
    public int rowCount;
    public int columnCount;
    public List<StockSlot> slots = new List<StockSlot>();
    public List<string> prioritizedProducts = new List<string>();

    public int GetProductCount()
    {
        int totalProduct = 0;
        foreach(var slot in slots)
        {
            foreach(var product in slot.products)
                if(product != null)
                    totalProduct++;
        }

        return totalProduct;
    }
}

[Serializable]
public class StockSlot
{
    public int slotIndex;
    public List<StorageItem> products = new List<StorageItem>(MAX_CAPACITY);
    public const int MAX_CAPACITY = 2;

    public bool IsEmpty => products.Count == 0;
    public bool IsFull => products.Count >= MAX_CAPACITY;
}
