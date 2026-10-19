using SmartWarehouse.Cloud;
using SmartWarehouse.Core;
using SmartWarehouse.Inventory;
using SmartWarehouse.Logging;
using System.Threading.Tasks;

public static class CloudDataManager
{
    private static ProductData productData;
    private static WarehouseLayoutData layoutData;
    private static readonly ICloudDataService cloudDataService = new CloudSaveDataService();

    /// <summary>
    /// Load Important Data when first start the application
    /// </summary>
    /// <returns></returns>
    public static async Task LoadImportantData()
    {
        //General Player Data
        LoadingIcon.Instance.Show();

        productData = await cloudDataService.LoadData<ProductData>(CloudSaveKey.GetPath(Key.ProductData));
        layoutData = await cloudDataService.LoadData<WarehouseLayoutData>(CloudSaveKey.GetPath(Key.Layout));
        await TransactionManager.Instance.LoadTransactionsAsync();
        await LogManager.Instance.LoadSystemLogAsync();
        await ProductRegistry.Instance.InitializeRegistryAsync();


        WarehouseManager.Instance.LoadLayoutData(layoutData);
        LoadingIcon.Instance.Hide();
    }

    #region Save Data
    public static async Task SaveProductData(ProductData productData)
    {
        await cloudDataService.SaveData(CloudSaveKey.GetPath(Key.ProductData), productData);
        CloudDataManager.productData = productData;     
    }

    public static async Task SaveWarehouseLayoutData(WarehouseLayoutData layoutData)
    {
        await cloudDataService.SaveData(CloudSaveKey.GetPath(Key.Layout), layoutData);
        CloudDataManager.layoutData = layoutData;
    }

    public static ProductData GetProductData()
    {
        return productData;
    }

    public static WarehouseLayoutData GetWarehouseLayoutData()
    {
        return layoutData;
    }

    #endregion

}
