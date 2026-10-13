using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using SmartWarehouse.Cloud;


public class CloudDataInitializer : MonoBehaviour
{
    public enum Mode { Debug, Production }
    public Mode mode;
    [SerializeField] private AuthenticationManager authenticationManager;
    [SerializeField] private Animator anim;
    private readonly ICloudDataService cloudDataService = new CloudSaveDataService();
    public static CloudDataInitializer Instance;
    [SerializeField] private Button playButton;

    [SerializeField] private bool isForceLoadData;
    [SerializeField] private bool isForceLoadTutorialScene;

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
        if (!cloudDataService.CheckKey(CloudSaveKey.GetPath(Key.ProductData)))
        {
            //Create Character Data pack
            ProductData productData = new();

            await CloudDataManager.Instance.SaveProductData(productData);
            if (mode == Mode.Production) isNewKeyCreated = true;
            //isFirstTimePlayer = true;
        }


        if (isForceLoadData || !isNewKeyCreated) await CloudDataManager.Instance.LoadImportantData();
        StartCoroutine(PlayButtonCoroutine());
    }

    private IEnumerator PlayButtonCoroutine()
    {
        yield return new WaitForSeconds(2.8f);
        playButton.gameObject.SetActive(true);
        anim.Play("Play");
    }
    #endregion
}
