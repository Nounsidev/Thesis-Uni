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
}

[Serializable]
public class StockSlot
{
    public int slotIndex;
    public List<string> productIds = new List<string>();
    public const int MAX_CAPACITY = 2;

    public bool IsEmpty => productIds.Count == 0;
    public bool IsFull => productIds.Count >= MAX_CAPACITY;
}
