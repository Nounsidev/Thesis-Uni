using SmartWarehouse.Cloud;
using System;
using TMPro;
using UnityEngine;

public static class ItemReader
{
    public static StorageItem ParseItemCode(string rawCode)
    {
        // Example rawCode: "PK=CBF-261015"
        string[] parts = rawCode.Split('-');

        if (parts.Length < 2) return null;
         
        string productID = parts[0];   
        string expiryRaw = parts[1];    // "261015" (YYMMDD)
        string timestamp = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");


        return new StorageItem
        {
            itemId = productID, 
            expiredAt = expiryRaw, 
            arrivedAt = timestamp,
        };
    }
}
