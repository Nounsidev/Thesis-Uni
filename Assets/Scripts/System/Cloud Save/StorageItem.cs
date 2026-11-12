using System;

[Serializable]
public class StorageItem
{
    public string itemId;
    public string expiredAt;
    public string arrivedAt;
}

[Serializable]
public class PlacedItemInfo
{
    public StorageItem item;
    public int shelfId;
    public int slotId;
    public int row;
    public int column;
}
