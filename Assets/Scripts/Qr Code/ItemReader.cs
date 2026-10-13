using SmartWarehouse.Cloud;
using System;
using TMPro;
using UnityEngine;

public static class ItemReader
{
    public static StorageItem ParseItemCode(string rawCode)
    {
        // Example rawCode: "PRH-CHK01-261015"
        string[] parts = rawCode.Split('-');

        if (parts.Length < 2) return null;
         
        string productID = parts[0];    // "CHK01"
        string expiryRaw = parts[1];    // "261015" (YYMMDD)
        string timestamp = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");


        return new StorageItem
        {
            itemId = GetItemIdFromProductID(productID), 
            expiredAt = expiryRaw, 
            arrivedAt = timestamp,
        };
    }

    private static int GetItemIdFromProductID(string productID)
    {
        try
        {
            // 1. Convert the string to the Enum type (ignoring case just in case)
            if (Enum.TryParse(productID, true, out ProductType matchedCategory))
            {
                // 2. Cast the enum to an integer to get its index value
                int index = (int)matchedCategory;
                Debug.Log($"Scanned '{productID}' matched enum '{matchedCategory}' at index {index}");
                return index;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to parse scanned code '{productID}' to enum: {ex.Message}");
        }

        return -1; // Not found
    }
}
