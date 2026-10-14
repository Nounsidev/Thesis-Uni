using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using SmartWarehouse.Core;
using SmartWarehouse.Cloud;

namespace SmartWarehouse.Inventory
{
    [System.Serializable]
    public class ProductCatalogContainer
    {
        public List<ProductBase> products = new List<ProductBase>();
    }

    public class ProductRegistry : MonoBehaviour
    {
        public static ProductRegistry Instance { get; private set; }

        private Dictionary<string, ProductBase> productDatabase = new Dictionary<string, ProductBase>();
        private ProductCatalogContainer catalogContainer = new ProductCatalogContainer();
        private string localCatalogPath;

        private async void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            localCatalogPath = Path.Combine(Application.persistentDataPath, "product_catalog.json");
            await InitializeRegistryAsync();
        }

        /// <summary>
        /// Cloud-First initialization: Pulls from Cloud Save first, falls back to local cache if offline.
        /// </summary>
        public async Task InitializeRegistryAsync()
        {
            bool cloudSyncSuccessful = false;

            // 1. Try fetching latest catalog from Cloud Save first
            try
            {
                var cloudContainer = await CloudFileSaving.LoadDataAsync<ProductCatalogContainer>(Key.Catalog);
                if (cloudContainer != null && cloudContainer.products != null)
                {
                    catalogContainer = cloudContainer;
                    RebuildDictionary();
                    SaveCatalogLocally(); // Cache the fresh cloud data locally
                    cloudSyncSuccessful = true;
                    Debug.Log($"[ProductRegistry] Successfully synced and loaded {productDatabase.Count} products from Cloud Save.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ProductRegistry] Cloud sync failed or offline ({ex.Message}). Falling back to local cache.");
            }

            // 2. Fall back to local cache if offline or cloud fetch failed
            if (!cloudSyncSuccessful)
            {
                LoadCatalogLocally();
                Debug.Log($"[ProductRegistry] Initialized using local cache. Total products: {productDatabase.Count}");
            }
        }

        private void LoadCatalogLocally()
        {
            if (File.Exists(localCatalogPath))
            {
                string json = File.ReadAllText(localCatalogPath);
                var localContainer = JsonUtility.FromJson<ProductCatalogContainer>(json);
                if (localContainer != null && localContainer.products != null)
                {
                    catalogContainer = localContainer;
                    RebuildDictionary();
                }
            }
        }

        private void SaveCatalogLocally()
        {
            string json = JsonUtility.ToJson(catalogContainer, true);
            File.WriteAllText(localCatalogPath, json);
        }

        /// <summary>
        /// Adds or updates a product locally, caches it, and pushes the updated container to Cloud Save.
        /// </summary>
        public async void AddAndSyncNewProduct(ProductBase newProduct)
        {
            if (newProduct == null || string.IsNullOrEmpty(newProduct.customCode)) return;

            string key = newProduct.customCode.ToUpper();

            int existingIndex = catalogContainer.products.FindIndex(p => p.customCode.ToUpper() == key);
            if (existingIndex >= 0)
            {
                catalogContainer.products[existingIndex] = newProduct;
            }
            else
            {
                catalogContainer.products.Add(newProduct);
            }

            RebuildDictionary();
            SaveCatalogLocally();

            // Push updated container JSON up to the server
            try
            {
                await CloudFileSaving.SaveDataAsync(Key.Catalog, catalogContainer);
                Debug.Log($"[ProductRegistry] Product '{newProduct.customCode}' saved locally and synced to cloud.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ProductRegistry] Failed to sync product catalog container to cloud: {ex.Message}");
            }
        }

        private void RebuildDictionary()
        {
            productDatabase.Clear();
            foreach (var product in catalogContainer.products)
            {
                if (!string.IsNullOrEmpty(product.customCode))
                {
                    productDatabase[product.customCode.ToUpper()] = product;
                }
            }
        }

        public ProductBase GetProduct(string customCode)
        {
            if (string.IsNullOrEmpty(customCode)) return null;
            productDatabase.TryGetValue(customCode.ToUpper(), out ProductBase product);
            return product;
        }

        public List<ProductBase> GetAllProducts()
        {
            return catalogContainer.products;
        }
    }
}