using SmartWarehouse.Cloud;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ProductData
{
    public List<StorageSlot> storageSlots = new List<StorageSlot>(9);

    public int FindNumberOfItem(ProductType productType)
    {
        int count = 0;
        int itemId = (int)productType;
        foreach (var slot in storageSlots)
        {
            foreach (var item in slot.items)
            {
                if (item.itemId == itemId)
                {
                    count++;
                }
            }
        }
        return count;
    }

    public List<StorageItem> GetItemsByType(ProductType productType)
    {
        List<StorageItem> itemsOfType = new List<StorageItem>();
        int itemId = (int)productType;
        foreach (var slot in storageSlots)
        {
            foreach (var item in slot.items)
            {
                if (item.itemId == itemId)
                {
                    itemsOfType.Add(item);
                }
            }
        }
        return itemsOfType;
    }
}

[Serializable]
public class StorageSlot
{
    public List<StorageItem> items = new List<StorageItem>();

    public void AddItem(StorageItem item)
    {
        items.Add(item);
    }

    public void RemoveItem(StorageItem item)
    {
        items.Remove(item);
    }
}

[Serializable]
public class StorageItem
{
    public int itemId;
    public string expiredAt;
    public string arrivedAt;
}
