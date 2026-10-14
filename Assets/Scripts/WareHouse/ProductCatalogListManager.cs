using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using SmartWarehouse.Inventory;
using UnityEngine.UI;

public class ProductCatalogListManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform contentContainer; // Content GameObject inside your ScrollView
    [SerializeField] private ProductRowUI rowPrefab;       // Your modular row prefab with individual fields & Edit button
    private ProductConfigUI productConfigUI; // Reference to your Add/Edit form panel
    [SerializeField] private Button exportFileButton;
    [SerializeField] private Button refreshButton;

    private List<ProductRowUI> activeRows = new List<ProductRowUI>();

    private void Awake()
    {
        exportFileButton.onClick.AddListener(ExportCatalogToExcel);
        refreshButton.onClick.AddListener(RefreshCatalogDisplay);
        productConfigUI = GetComponent<ProductConfigUI>();
    }

    private void OnDisable()
    {
        exportFileButton.onClick.RemoveListener(ExportCatalogToExcel);
        refreshButton.onClick.RemoveListener(RefreshCatalogDisplay);
    }

    private void OnEnable()
    {
        RefreshCatalogDisplay();
    }

    private void RefreshCatalogDisplay()
    {
        // Clear out old instantiated rows
        foreach (var row in activeRows)
        {
            if (row != null) Destroy(row);
        }
        activeRows.Clear();

        if (ProductRegistry.Instance == null) return;

        List<ProductBase> products = ProductRegistry.Instance.GetAllProducts();
        if (products == null) return;

        // Instantiate a row prefab for each product and initialize its individual fields
        foreach (var product in products)
        {
            ProductRowUI rowUI = Instantiate(rowPrefab, contentContainer);

            if (rowUI != null)
            {
                rowUI.Initialize(product, productConfigUI);
            }
            activeRows.Add(rowUI);
        }
    }

    /// <summary>
    /// Hook this to your EXPORT FILE button.
    /// </summary>
    private void ExportCatalogToExcel()
    {
        if (ProductRegistry.Instance == null) return;
        List<ProductBase> allProducts = ProductRegistry.Instance.GetAllProducts();
        if (allProducts == null || allProducts.Count == 0) return;

        StringBuilder csvBuilder = new StringBuilder();
        csvBuilder.AppendLine("Product Name,Custom Code,Labels,Expiry Warning (Days),Stock Warning Threshold");

        foreach (var p in allProducts)
        {
            string labelsFormatted = (p.labels != null) ? string.Join(";", p.labels) : "";
            csvBuilder.AppendLine($"\"{p.productName}\",\"{p.customCode}\",\"{labelsFormatted}\",{p.minDaysBeforeExpiryWarning},{p.minStockWarningThreshold}");
        }

        string fileName = $"Product_Catalog_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllText(filePath, csvBuilder.ToString(), Encoding.UTF8);

        Debug.Log($"[Catalog] Exported to CSV at: {filePath}");
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
#endif
    }
}