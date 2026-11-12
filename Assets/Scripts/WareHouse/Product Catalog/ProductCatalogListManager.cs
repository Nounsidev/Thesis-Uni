using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using SmartWarehouse.Inventory;
using UnityEngine.UI;
using TMPro;

public class ProductCatalogListManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] protected Transform contentContainer; // Content GameObject inside your ScrollView
    [SerializeField] protected ProductRowUI rowPrefab;       // Your modular row prefab with individual fields & Edit button
    [SerializeField] protected ProductConfigUI productConfigUI; // Reference to your Add/Edit form panel
    [SerializeField] protected Button exportFileButton;
    [SerializeField] protected Button refreshButton;

    [Header("Filtering & Sorting UI")]
    [SerializeField] protected TMP_InputField searchInputField; // Input field for searching name, code, or labels
    [SerializeField] protected TMP_Dropdown sortDropdown;       // Dropdown for sorting options
    [SerializeField] protected TextMeshProUGUI totalProductDisplay;

    protected List<ProductRowUI> activeRows = new List<ProductRowUI>();
    [SerializeField] protected RectTransform contentRectTransform;

    protected virtual void Awake()
    {
        exportFileButton.onClick.AddListener(ExportCatalogToExcel);
        refreshButton.onClick.AddListener(RefreshCatalogDisplay);

        // Listen for live search updates and sorting changes
        if (searchInputField != null) searchInputField.onValueChanged.AddListener(OnSearchValueChanged);
        if (sortDropdown != null) sortDropdown.onValueChanged.AddListener(OnSortValueChanged);
    }

    private void OnDestroy()
    {
        exportFileButton.onClick.RemoveListener(ExportCatalogToExcel);
        refreshButton.onClick.RemoveListener(RefreshCatalogDisplay);

        if (searchInputField != null) searchInputField.onValueChanged.RemoveListener(OnSearchValueChanged);
        if (sortDropdown != null) sortDropdown.onValueChanged.RemoveListener(OnSortValueChanged);
    }

    private void OnEnable()
    {
        RefreshCatalogDisplay();
    }

    public virtual void RefreshCatalogDisplay()
    {
        // Clear out old instantiated rows
        foreach (var row in activeRows)
        {
            if (row != null) Destroy(row.gameObject);
        }
        activeRows.Clear();

        if (ProductRegistry.Instance == null) return;
        List<ProductBase> products = ProductRegistry.Instance.GetAllProducts();
        totalProductDisplay.text = products.Count.ToString();

        if (products == null) return;

        // 1. Filtering Logic (Search by Name, Code, or Labels)
        string searchQuery = searchInputField != null ? searchInputField.text.Trim().ToLower() : "";
        List<ProductBase> filteredProducts = new List<ProductBase>();

        foreach (var p in products)
        {
            bool matchesSearch = string.IsNullOrEmpty(searchQuery) ||
                (p.productName != null && p.productName.ToLower().Contains(searchQuery)) ||
                (p.customCode != null && p.customCode.ToLower().Contains(searchQuery)) ||
                (p.labels != null && p.labels.Exists(l => l.ToLower().Contains(searchQuery)));

            if (matchesSearch)
            {
                filteredProducts.Add(p);
            }
        }

        // 2. Sorting Logic (A-Z / Z-A by Name or Code)
        int sortMode = sortDropdown != null ? sortDropdown.value : 0;
        switch (sortMode)
        {
            case 0: // Product Name (Ascending: A - Z)
                filteredProducts.Sort((a, b) => string.Compare(a.productName, b.productName, System.StringComparison.OrdinalIgnoreCase));
                break;
            case 1: // Product Name (Descending: Z - A)
                filteredProducts.Sort((a, b) => string.Compare(b.productName, a.productName, System.StringComparison.OrdinalIgnoreCase));
                break;
            case 2: // Custom Code (Ascending: A - Z)
                filteredProducts.Sort((a, b) => string.Compare(a.customCode, b.customCode, System.StringComparison.OrdinalIgnoreCase));
                break;
            case 3: // Custom Code (Descending: Z - A)
                filteredProducts.Sort((a, b) => string.Compare(b.customCode, a.customCode, System.StringComparison.OrdinalIgnoreCase));
                break;
        }

        // 3. Instantiate row prefabs for filtered and sorted products
        foreach (var product in filteredProducts)
        {
            ProductRowUI rowUI = Instantiate(rowPrefab, contentContainer);
            if (rowUI != null)
            {
                rowUI.Initialize(product, productConfigUI, false, 0);
                activeRows.Add(rowUI);
            }
        }

        DynamicContainer(activeRows.Count);
    }

    private void OnSearchValueChanged(string query)
    {
        RefreshCatalogDisplay();
    }

    private void OnSortValueChanged(int index)
    {
        RefreshCatalogDisplay();
    }

    protected void DynamicContainer(int numberOfItem)
    {
        ChangeRectTransformSize(0f);

        if (numberOfItem < 3) return;

        int expandIdex = numberOfItem - 3;
        float offset = -(expandIdex * 200f);
        ChangeRectTransformSize(offset);
    }

    private void ChangeRectTransformSize(float offset)
    {
        Vector2 currentOffsetMin = contentRectTransform.offsetMin;
        currentOffsetMin.y = offset;
        contentRectTransform.offsetMin = currentOffsetMin;
    }

    /// <summary>
    /// Hook this to your EXPORT FILE button.
    /// </summary>
    protected virtual void ExportCatalogToExcel()
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
        string filePath = Path.Combine(ProjectPath.GetPath(), fileName);
        File.WriteAllText(filePath, csvBuilder.ToString(), Encoding.UTF8);

        Debug.Log($"[Catalog] Exported to CSV at: {filePath}");
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
#endif
    }
}