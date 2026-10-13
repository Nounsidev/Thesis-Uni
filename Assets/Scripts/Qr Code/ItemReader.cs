using UnityEngine;

public class ItemReader : MonoBehaviour
{
    public void ParseItemCode(string rawCode)
    {
        // Example rawCode: "PRH-CHK01-261015"
        string[] parts = rawCode.Split('-');

        if (parts.Length >= 3)
        {
            string category = parts[0];     // "PRH"
            string productID = parts[1];    // "CHK01"
            string expiryRaw = parts[2];    // "261015" (YYMMDD)

            Debug.Log($"Category: {category}, Product: {productID}, Expiry Code: {expiryRaw}");
        }
    }
}
