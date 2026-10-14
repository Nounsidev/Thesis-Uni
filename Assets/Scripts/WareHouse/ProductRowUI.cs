using UnityEngine;
using TMPro;
using UnityEngine.UI;
using SmartWarehouse.Inventory;
using System.IO;
using SmartWarehouse.UI;

public class ProductRowUI : MonoBehaviour
{
    [Header("Row UI Fields")]
    [SerializeField] private TMP_Text nameAndCodeText;    // Displays: "PRODUCT NAME - CODE"
    [SerializeField] private TMP_Text warningExpiryText;  // Displays: "WARNING EXPIRY: X"
    [SerializeField] private TMP_Text warningStockText;   // Displays: "WARNING STOCK: X"
    [SerializeField] private TMP_Text labelsText;         // Displays: "LABELS: Tag1, Tag2"
    [SerializeField] private Image iconImage;
    [SerializeField] private Sprite defaultPlaceholderSprite;

    [Header("Row Action Button")]
    [SerializeField] private Button editButton;

    private ProductBase assignedProduct;
    private ProductConfigUI configUIController;

    public void Initialize(ProductBase product, ProductConfigUI configUI)
    {
        assignedProduct = product;
        configUIController = configUI;

        // 1. Name and Code on the same line
        if (nameAndCodeText != null)
        {
            nameAndCodeText.text = $"{product.productName} - {product.customCode}";
        }

        // 2. Formatted Expiry Warning
        if (warningExpiryText != null)
        {
            warningExpiryText.text = $"WARNING EXPIRY: {product.minDaysBeforeExpiryWarning}";
        }

        // 3. Formatted Stock Warning
        if (warningStockText != null)
        {
            warningStockText.text = $"WARNING STOCK: {product.minStockWarningThreshold}";
        }

        // 4. Formatted Labels
        if (labelsText != null)
        {
            string labelsFormatted = (product.labels != null && product.labels.Count > 0)
                ? string.Join(", ", product.labels)
                : "None";
            labelsText.text = $"LABELS: {labelsFormatted}";
        }

        // 5. Load and display product icon from persistent path if available
        if (iconImage != null)
        {
            string localIconPath = Path.Combine(Application.persistentDataPath, product.iconFileName);
            if (File.Exists(localIconPath))
            {
                byte[] fileData = File.ReadAllBytes(localIconPath);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(fileData))
                {
                    iconImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }
            else
            {
                iconImage.sprite = defaultPlaceholderSprite;
            }
        }

        // 6. Bind the Edit button click listener automatically
        if (editButton != null)
        {
            editButton.onClick.RemoveAllListeners();
            editButton.onClick.AddListener(OnClickEditRow);
        }
    }

    private void OnClickEditRow()
    {
        if (configUIController != null && assignedProduct != null)
        {
            configUIController.LoadProductIntoForm(assignedProduct);
            UiHandler.Instance.SwitchPanel(PanelType.AddProduct);
        }
    }
}