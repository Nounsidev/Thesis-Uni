using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using SmartWarehouse.Core;
using SmartWarehouse.Cloud;
using SmartWarehouse.Inventory;
using SmartWarehouse.Logging;

public class ProductConfigUI : MonoBehaviour
{
    [Header("Form Inputs")]
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private TMP_InputField codeInputField;
    [SerializeField] private TMP_InputField labelsInputField; // Comma-separated labels
    [SerializeField] private TMP_InputField expiryWarningField;
    [SerializeField] private TMP_InputField stockWarningField;

    [Header("UI Previews")]
    [SerializeField] private Image iconPreviewImage;
    [SerializeField] private Sprite defaultPlaceholderSprite;

    [Header("Buttons")]
    [SerializeField] private Button selectIconButton;
    [SerializeField] private Button addItemButton;
    [SerializeField] private Button resetDataButton;    

    private Texture2D uploadedIconTexture;

    private void Awake()
    {
        
        selectIconButton.onClick.AddListener(SelectIconButton);      
        addItemButton.onClick.AddListener(SubmitAndSaveNewProduct);    
        resetDataButton.onClick.AddListener(ClearFormFields);
    }

    private void OnDestroy()
    {
        selectIconButton.onClick.RemoveListener(SelectIconButton);
        addItemButton.onClick.RemoveListener(SubmitAndSaveNewProduct);
        resetDataButton.onClick.RemoveListener(ClearFormFields);
    }

    private void SelectIconButton()
    {
#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.OpenFilePanel("Select Product Icon", "", "png,jpg");
        LoadIconFromFile(path);
#else
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
        }
    }

    private async void SubmitAndSaveNewProduct()
    {
        string productName = nameInputField.text;
        string customCode = codeInputField.text.ToUpper();

        if (string.IsNullOrEmpty(productName) || string.IsNullOrEmpty(customCode))
        {
            Debug.LogError("Product Name and Custom Code are required!");
            return;
        }

        ProductBase newProduct = new ProductBase
        {
            productName = productName,
            customCode = customCode,
            iconFileName = $"icon_{customCode}.png"
        };

        // Parse comma-separated labels (e.g., "Perishable, Frozen, Meat")
        if (labelsInputField != null && !string.IsNullOrEmpty(labelsInputField.text))
        {
            string[] rawLabels = labelsInputField.text.Split(',');
            foreach (var label in rawLabels)
            {
                string trimmed = label.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    newProduct.labels.Add(trimmed);
                }
            }
        }

        if (int.TryParse(expiryWarningField.text, out int expiryDays))
            newProduct.minDaysBeforeExpiryWarning = expiryDays;

        if (int.TryParse(stockWarningField.text, out int stockThreshold))
            newProduct.minStockWarningThreshold = stockThreshold;

        // Save icon locally and sync bytes to cloud
        if (uploadedIconTexture != null)
        {
            byte[] iconBytes = uploadedIconTexture.EncodeToPNG();
            string localIconPath = Path.Combine(Application.persistentDataPath, newProduct.iconFileName);
            File.WriteAllBytes(localIconPath, iconBytes);

            await CloudFileSaving.SaveBytesAsync(Key.ProductIcon, iconBytes);
        }
       
        // Register and sync catalog container
        if (ProductRegistry.Instance != null)
        {
            ProductRegistry.Instance.AddAndSyncNewProduct(newProduct);
        }

        Debug.Log($"Product '{productName}' successfully created, saved, and synced!");
        LogManager.Instance.LogEvent(LogCategory.SYSTEM_Event, $"New product created: {productName} (Code: {customCode})");

        ClearFormFields();
    }

    /// <summary>
    /// Clears all input fields, uploaded textures, and image preview elements.
    /// Hook this to your "RESET DATA" button.
    /// </summary>
    private void ClearFormFields()
    {
        if (nameInputField != null) nameInputField.text = string.Empty;
        if (codeInputField != null) codeInputField.text = string.Empty;
        if (labelsInputField != null) labelsInputField.text = string.Empty;
        if (expiryWarningField != null) expiryWarningField.text = string.Empty;
        if (stockWarningField != null) stockWarningField.text = string.Empty;

        uploadedIconTexture = null;

        if (iconPreviewImage != null)
        {
            iconPreviewImage.sprite = defaultPlaceholderSprite;
        }

        Debug.Log("[ProductConfigUI] All input fields and UI previews cleared.");
    }
}