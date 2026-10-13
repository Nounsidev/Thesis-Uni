using System.Threading.Tasks;
using UnityEngine;
using SmartWarehouse.Cloud;

public class CloudDataManager : MonoBehaviour
{
    public static CloudDataManager Instance { get; private set; }
    private ProductData productData;


    private readonly ICloudDataService cloudDataService = new CloudSaveDataService();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    /// <summary>
    /// Load Important Data when first start the application
    /// </summary>
    /// <returns></returns>
    public async Task LoadImportantData()
    {
        //General Player Data
        productData = await cloudDataService.LoadData<ProductData>(CloudSaveKey.GetPath(Key.ProductData));
    }

    #region Save Data
    public async Task SaveProductData(ProductData productData)
    {
        await cloudDataService.SaveData(CloudSaveKey.GetPath(Key.ProductData), productData);
        this.productData = productData;
    }

    #endregion

}
