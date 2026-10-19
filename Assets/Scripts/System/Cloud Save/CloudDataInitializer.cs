using UnityEngine;
using SmartWarehouse.Cloud;



public class CloudDataInitializer : MonoBehaviour
{
    public enum Mode { Debug, Production }
    public Mode mode;
    [SerializeField] private AuthenticationManager authenticationManager;
    private readonly ICloudDataService cloudDataService = new CloudSaveDataService();
    public static CloudDataInitializer Instance;

    [SerializeField] private bool isForceLoadData;

    private void Awake()
    {
        Instance = this;
    }


    public void CreateData()
    {
        CreateCloudSaveData();
    }

    #region Cloud Data Handler
    private async void CreateCloudSaveData()
    {
        bool isNewKeyCreated = false;
        await cloudDataService.LoadAllKeys();

        if (CheckKey(Key.ProductData))
        {
            //Create Character Data pack
            ProductData productData = new();

            await CloudDataManager.SaveProductData(productData);
            if (mode == Mode.Production) isNewKeyCreated = true;         
        }

        if (CheckKey(Key.Layout))
        {
            WarehouseLayoutData layoutData = new();

            await CloudDataManager.SaveWarehouseLayoutData(layoutData);
            if (mode == Mode.Production) isNewKeyCreated = true;
        }

        if (isForceLoadData || !isNewKeyCreated) await CloudDataManager.LoadImportantData();
    }

    private bool CheckKey(Key key)
    {
        return !cloudDataService.CheckKey(CloudSaveKey.GetPath(key));
    }

    #endregion
}
