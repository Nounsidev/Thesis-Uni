using System;
using System.Collections.Generic;
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
    [SerializeField] private TMP_InputField codeInputField; // Should be locked/disabled when editing
    [SerializeField] private TMP_InputField labelsInputField;
    [SerializeField] private TMP_InputField expiryWarningField;
    [SerializeField] private TMP_InputField stockWarningField;

    [Header("Buttons")]
    [SerializeField] private Button submitButton;
    [SerializeField] private Button cancelButton;

    [Header("UI Previews")]
    [SerializeField] private Image iconPreviewImage;
    [SerializeField] private Sprite defaultPlaceholderSprite;

    private Texture2D uploadedIconTexture;
    private bool isEditingMode = false;
    private string editingCustomCode = "";

    private void Awake()
    {
        submitButton.onClick.AddListener(SubmitAndSaveNewProduct);
        cancelButton.onClick.AddListener(ClearFormFields);
    }

    private void OnDisable()
    {
        submitButton.onClick.RemoveListener(SubmitAndSaveNewProduct);
        cancelButton.onClick.RemoveListener(ClearFormFields);
    }

    /// <summary>
    /// Call this from the Catalog Row's EDIT button to populate the form with existing product data.
    /// </summary>
    public void LoadProductIntoForm(ProductBase product)
    {
        if (product == null) return;

        isEditingMode = true;
        editingCustomCode = product.customCode;

        if (nameInputField != null) nameInputField.text = product.productName;
        if (codeInputField != null)
        {
            codeInputField.text = product.customCode;
            codeInputField.interactable = false; // Prevent modifying primary key code during edit
        }

        if (labelsInputField != null)
        {
            labelsInputField.text = product.labels != null ? string.Join(", ", product.labels) : "";
        }

        if (expiryWarningField != null) expiryWarningField.text = product.minDaysBeforeExpiryWarning.ToString();
        if (stockWarningField != null) stockWarningField.text = product.minStockWarningThreshold.ToString();

        // Load existing icon if available locally
        string localIconPath = Path.Combine(Application.persistentDataPath, product.iconFileName);
        if (File.Exists(localIconPath))
        {
            byte[] fileData = File.ReadAllBytes(localIconPath);
            uploadedIconTexture = new Texture2D(2, 2);
            if (uploadedIconTexture.LoadImage(fileData))
            {
                iconPreviewImage.sprite = Sprite.Create(uploadedIconTexture, new Rect(0, 0, uploadedIconTexture.width, uploadedIconTexture.height), new Vector2(0.5f, 0.5f));
            }
        }
    }

    public void OnClickSelectIcon()
    {
#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.OpenFilePanel("Select Product Icon", "", "png,jpg");
        LoadIconFromFile(path);
#else
        NativeFilePicker.Permission permission = NativeFilePicker.PickFile((path) =>
        {
            if (!string.IsNullOrEmpty(path)) LoadIconFromFile(path);
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
            iconPreviewImage.sprite = Sprite.Create(uploadedIconTexture, new Rect(0, 0, uploadedIconTexture.width, uploadedIconTexture.height), new Vector2(0.5f, 0.5f));
        }
    }

    private async void SubmitAndSaveNewProduct()
    {
        string productName = nameInputField.text;
        string customCode = isEditingMode ? editingCustomCode : codeInputField.text.ToUpper();

        if (string.IsNullOrEmpty(productName) || string.IsNullOrEmpty(customCode))
        {
            Debug.LogError("Product Name and Custom Code are required!");
            return;
        }

        ProductBase product;

        if (isEditingMode && ProductRegistry.Instance != null)
        {
            product = ProductRegistry.Instance.GetProduct(customCode);
            if (product == null) product = new ProductBase { customCode = customCode };
        }
        else
        {
            product = new ProductBase { customCode = customCode };
        }

        product.productName = productName;
        product.iconFileName = $"icon_{customCode}.png";
        product.labels.Clear();

        if (labelsInputField != null && !string.IsNullOrEmpty(labelsInputField.text))
        {
            string[] rawLabels = labelsInputField.text.Split(',');
            foreach (var label in rawLabels)
            {
                string trimmed = label.Trim();
                if (!string.IsNullOrEmpty(trimmed)) product.labels.Add(trimmed);
            }
        }

        if (int.TryParse(expiryWarningField.text, out int expiryDays)) product.minDaysBeforeExpiryWarning = expiryDays;
        if (int.TryParse(stockWarningField.text, out int stockThreshold)) product.minStockWarningThreshold = stockThreshold;

        if (uploadedIconTexture != null)
        {
            byte[] iconBytes = uploadedIconTexture.EncodeToPNG();
            string localIconPath = Path.Combine(Application.persistentDataPath, product.iconFileName);
            File.WriteAllBytes(localIconPath, iconBytes);
            await CloudFileSaving.SaveBytesAsync(Key.ProductIcon, iconBytes);
        }

        if (ProductRegistry.Instance != null)
        {
            ProductRegistry.Instance.AddAndSyncNewProduct(product);
        }

        Debug.Log($"Product '{productName}' successfully {(isEditingMode ? "updated" : "created")} and synced!");
        ClearFormFields();
    }

    private void ClearFormFields()
    {
        isEditingMode = false;
        editingCustomCode = "";
        if (nameInputField != null) nameInputField.text = string.Empty;
        if (codeInputField != null)
        {
            codeInputField.text = string.Empty;
            codeInputField.interactable = true; // Re-enable for new entries
        }
        if (labelsInputField != null) labelsInputField.text = string.Empty;
        if (expiryWarningField != null) expiryWarningField.text = string.Empty;
        if (stockWarningField != null) stockWarningField.text = string.Empty;

        uploadedIconTexture = null;
        if (iconPreviewImage != null) iconPreviewImage.sprite = defaultPlaceholderSprite;
    }
}