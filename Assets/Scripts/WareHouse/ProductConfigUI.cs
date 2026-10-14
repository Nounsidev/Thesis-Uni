using System;
using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using ZXing;
using ZXing.Common;
using SmartWarehouse.Core;
using SmartWarehouse.Cloud;
using SmartWarehouse.Inventory;

public class ProductConfigUI : MonoBehaviour
{
    [Header("Form Inputs")]
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private TMP_InputField codeInputField;
    [SerializeField] private TMP_Dropdown categoryDropdown;
    [SerializeField] private TMP_InputField expiryWarningField;
    [SerializeField] private TMP_InputField stockWarningField;

    [Header("UI Previews")]
    [SerializeField] private Image iconPreviewImage;
    private Texture2D uploadedIconTexture;

    /// <summary>
    /// Handles cross-platform image selection for both Editor testing and standalone runtime builds.
    /// </summary>
    public void OnClickSelectIcon()
    {
#if UNITY_EDITOR
        // Editor fallback for fast testing
        string path = UnityEditor.EditorUtility.OpenFilePanel("Select Product Icon", "", "png,jpg");
        LoadIconFromFile(path);
#else
        // Runtime File Picker for standalone builds (requires Native File Picker plugin or standard runtime open file implementation)
        NativeFilePicker.Permission permission = NativeFilePicker.PickFile((path) =>
        {
            if (!string.IsNullOrEmpty(path))
            {
                LoadIconFromFile(path);
            }
        }, "Select Product Image", new string[] { "image/png", "image/jpeg" });
#endif
    }

    private void LoadIconFromFile(string path)
    {
        if (string.IsNullOrEmpty(path)) return;

        byte[] fileData = File.ReadAllBytes(path);
        uploadedIconTexture = new Texture2D(2, 2);
        if (uploadedIconTexture.LoadImage(fileData))
        {
            iconPreviewImage.sprite = Sprite.Create(
                uploadedIconTexture,
                new Rect(0, 0, uploadedIconTexture.width, uploadedIconTexture.height),
                new Vector2(0.5f, 0.5f)
            );
            Debug.Log("Product icon loaded successfully from: " + path);
        }
    }

    public async void SubmitAndSaveNewProduct()
    {
        string productName = nameInputField.text;
        string customCode = codeInputField.text.ToUpper();
        string categoryStr = categoryDropdown.options[categoryDropdown.value].text;

        if (string.IsNullOrEmpty(productName) || string.IsNullOrEmpty(customCode))
        {
            Debug.LogError("Product Name and Custom Code are required!");
            return;
        }

        // Use ProductData (pure JSON class, not ScriptableObject)
        ProductBase newProduct = new ProductBase
        {
            productName = productName,
            category = categoryStr,
            customCode = customCode,
            iconFileName = $"icon_{customCode}.png"
        };

        if (int.TryParse(expiryWarningField.text, out int expiryDays))
            newProduct.minDaysBeforeExpiryWarning = expiryDays;

        if (int.TryParse(stockWarningField.text, out int stockThreshold))
            newProduct.minStockWarningThreshold = stockThreshold;

        // Save icon locally in persistent data path and upload bytes to cloud
        if (uploadedIconTexture != null)
        {
            byte[] iconBytes = uploadedIconTexture.EncodeToPNG();
            string localIconPath = Path.Combine(Application.persistentDataPath, newProduct.iconFileName);
            File.WriteAllBytes(localIconPath, iconBytes);

            await CloudFileSaving.SaveBytesAsync(Key.Catalog, iconBytes);
        }

        // Register product and sync catalog JSON container to cloud
        if (ProductRegistry.Instance != null)
        {
            ProductRegistry.Instance.AddAndSyncNewProduct(newProduct);
        }

        Debug.Log($"Product '{productName}' successfully created, configured, and synced!");
    }

    private Texture2D GenerateQRCodeTexture(string textToEncode)
    {
        BarcodeWriter writer = new BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new EncodingOptions { Height = 256, Width = 256, Margin = 1 }
        };

        Color32[] pixelMap = writer.Write(textToEncode);
        Texture2D qrTex = new Texture2D(256, 256);
        qrTex.SetPixels32(pixelMap);
        qrTex.Apply();
        return qrTex;
    }
}