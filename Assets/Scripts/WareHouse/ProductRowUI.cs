using UnityEngine;
using TMPro;
using UnityEngine.UI;
using SmartWarehouse.Inventory;
using System.IO;
using System;
using SmartWarehouse.UI;

public class ProductRowUI : MonoBehaviour
{
    [Header("Row UI Fields")]
    [SerializeField] private TMP_Text nameAndCodeText;
    [SerializeField] private TMP_Text warningExpiryText;
    [SerializeField] private TMP_Text warningStockText;
    [SerializeField] private TMP_Text labelsText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Sprite defaultPlaceholderSprite;

    [Header("Row Action Buttons")]
    [SerializeField] private Button editButton;
    [SerializeField] private Button deleteButton; // Drag your row's Delete button here

    private ProductBase assignedProduct;
    private ProductConfigUI configUIController;

    public void OnDestroy()
    {       
        editButton.onClick.RemoveListener(OnClickEditRow);    
        deleteButton.onClick.RemoveListener(OnClickDeleteRow);       
    }


    public void Initialize(ProductBase product, ProductConfigUI configUI)
    {
        assignedProduct = product;
        configUIController = configUI;

        // Populate text fields
        if (nameAndCodeText != null) nameAndCodeText.text = $"{product.productName} - {product.customCode}";
        if (warningExpiryText != null) warningExpiryText.text = $"WARNING EXPIRY: {product.minDaysBeforeExpiryWarning}";
        if (warningStockText != null) warningStockText.text = $"WARNING STOCK: {product.minStockWarningThreshold}";
        if (labelsText != null)
        {
            string labelsFormatted = (product.labels != null && product.labels.Count > 0) ? string.Join(", ", product.labels) : "None";
            labelsText.text = $"LABELS: {labelsFormatted}";
        }

        // Load icon
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

        // Bind Edit button
        if (editButton != null)
        {
            editButton.onClick.AddListener(OnClickEditRow);
        }

        // Bind Delete button
        if (deleteButton != null)
        {
            
            deleteButton.onClick.AddListener(OnClickDeleteRow);
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

    private void OnClickDeleteRow()
    {
        if (assignedProduct != null && ProductRegistry.Instance != null)
        {
            ProductRegistry.Instance.DeleteProduct(assignedProduct.customCode);
        }
    }
}