using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using SmartWarehouse.Cloud;

namespace SmartWarehouse.Core
{
    public static class CloudFileSaving
    {
        /// <summary>
        /// Generic Save: Takes ANY serializable data class, saves it locally as a JSON cache, 
        /// and uploads it as a byte file to Unity Cloud Save.
        /// </summary>
        public static async Task SaveDataAsync<T>(Key cloudKey, T dataObject) where T : class
        {
            try
            {
                string json = JsonUtility.ToJson(dataObject, true);
                string fileName = CloudSaveKey.GetPath(cloudKey) + ".json";
                string localPath = Path.Combine(Application.persistentDataPath, fileName);

                // 1. Save locally as cache
                File.WriteAllText(localPath, json, Encoding.UTF8);

                // 2. Convert to byte array
                byte[] fileBytes = Encoding.UTF8.GetBytes(json);

                // 3. Upload to Cloud
                await CloudSaveService.Instance.Files.Player.SaveAsync(fileName, fileBytes);
                Debug.Log($"[CloudSync] Successfully saved '{typeof(T).Name}' to cloud key: {fileName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CloudSync] Failed to save '{typeof(T).Name}': {ex.Message}");
            }
        }

        /// <summary>
        /// Generic Load: Downloads file bytes from Unity Cloud, updates the local cache, 
        /// and populates your data object. Falls back to local cache if offline.
        /// </summary>
        public static async Task<T> LoadDataAsync<T>(Key cloudKey) where T : class, new()
        {
            string fileName = CloudSaveKey.GetPath(cloudKey) + ".json";
            string localPath = Path.Combine(Application.persistentDataPath, fileName);
            T dataObject = new T();

            try
            {
                // 1. Attempt download from cloud
                byte[] fileBytes = await CloudSaveService.Instance.Files.Player.LoadBytesAsync(fileName);

                if (fileBytes != null && fileBytes.Length > 0)
                {
                    string json = Encoding.UTF8.GetString(fileBytes);
                    File.WriteAllText(localPath, json, Encoding.UTF8);
                    JsonUtility.FromJsonOverwrite(json, dataObject);
                    Debug.Log($"[CloudSync] Successfully loaded '{typeof(T).Name}' from cloud.");
                    return dataObject;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CloudSync] Cloud load failed for '{typeof(T).Name}' (using local cache): {ex.Message}");
            }

            // 2. Fallback to local file if cloud load failed or wasn't found
            if (File.Exists(localPath))
            {
                string json = File.ReadAllText(localPath);
                JsonUtility.FromJsonOverwrite(json, dataObject);
                Debug.Log($"[CloudSync] Loaded '{typeof(T).Name}' from local cache fallback.");
            }

            return dataObject;
        }

        // ==========================================
        // RAW BYTE METHODS (For physical files like .log)
        // ==========================================

        /// <summary>
        /// Saves a raw byte array directly to Unity Cloud Save (ideal for physical log files).
        /// </summary>
        public static async Task SaveBytesAsync(Key cloudKey, byte[] fileBytes)
        {
            try
            {
                string fileName = CloudSaveKey.GetPath(cloudKey) + ".log";
                await CloudSaveService.Instance.Files.Player.SaveAsync(fileName, fileBytes);
                Debug.Log($"[CloudSync] Successfully saved raw bytes to cloud key: {fileName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CloudSync] Failed to save raw bytes: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads raw bytes directly from Unity Cloud Save.
        /// </summary>
        public static async Task<byte[]> LoadBytesAsync(Key cloudKey)
        {
            try
            {
                string fileName = CloudSaveKey.GetPath(cloudKey) + ".log";
                return await CloudSaveService.Instance.Files.Player.LoadBytesAsync(fileName);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CloudSync] Failed to load raw bytes from cloud: {ex.Message}");
                return null;
            }
        }
    }
}