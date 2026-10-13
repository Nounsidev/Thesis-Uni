using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;
using SmartWarehouse.Cloud;

// Uncomment once Unity Cloud Save package is installed via Package Manager:
// using Unity.Services.CloudSave;

namespace SmartWarehouse.Logging
{
    public enum LogCategory
    {
        PLC_Event,
        PLC_Error,
        QRScanner_Event,
        QRScanner_Error,
        AI_Event,
        AI_Error,
    }

    [Serializable]
    public class LogEntryData
    {
        public string timestamp;
        public string category;
        public string message;
    }

    [Serializable]
    public class CloudLogContainer
    {
        public List<LogEntryData> entries = new List<LogEntryData>();
    }

    public class LogManager : MonoBehaviour
    {
        public static LogManager Instance { get; private set; }

        [Header("UI References")]
        private LogUi logUi;


        [Header("Cloud Sync Settings")]
        [SerializeField] private int cloudSyncBatchSize = 20;
        private List<LogEntryData> pendingCloudLogs = new List<LogEntryData>();

        private string logFilePath;

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
            logUi = GetComponent<LogUi>();
            logFilePath = Path.Combine(Application.persistentDataPath, "system_events.log");
            //Debug.Log($"Log file path: {logFilePath}");

            // Initialize Unity Cloud Services
            try
            {
                await UnityServices.InitializeAsync();
                Debug.Log("Unity Cloud Services initialized successfully for logging.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize Unity Cloud Services: {ex.Message}");
            }
        }

        public async void LogEvent(LogCategory category, string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string formattedEntry = $"[{timestamp}] [{category}] {message}";

            // 1. Console Log
            Debug.Log(formattedEntry);

            // 2. UI Display
            logUi.UpdateText(formattedEntry + "\n");

            // 3. Local File Log (Append)
            try
            {
                File.AppendAllText(logFilePath, formattedEntry + "\n");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to write to log file: {ex.Message}");
            }

            // 4. Queue for Cloud Sync
            pendingCloudLogs.Add(new LogEntryData
            {
                timestamp = timestamp,
                category = category.ToString(),
                message = message
            });

            if (pendingCloudLogs.Count >= cloudSyncBatchSize)
            {
                await FlushLogsToCloudAsync();
            }
        }

        private async Task FlushLogsToCloudAsync()
        {
            if (pendingCloudLogs.Count == 0) return;

            if (!File.Exists(logFilePath)) return;

            try
            {
                // 1. Read the local log file as a byte array (just like the documentation example)
                byte[] fileBytes = File.ReadAllBytes(logFilePath);

                // 2. Define a unique file key for cloud storage (e.g., based on date or device)
                string cloudFileName = CloudSaveKey.GetPath(Key.SystemLog) + ".log";

                // 3. Upload using Unity Cloud Save Files API
                await CloudSaveService.Instance.Files.Player.SaveAsync(cloudFileName, fileBytes);

                Debug.Log($"Successfully uploaded log file ({cloudFileName}) to Unity Cloud.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to upload log file to Unity Cloud: {ex.Message}");
            }
        }

        private async Task LoadLogFileFromCloudAsync()
        {
            try
            {
                string cloudFileName = CloudSaveKey.GetPath(Key.SystemLog) + ".log";

                // 1. Download file bytes from Unity Cloud Save
                byte[] fileBytes = await CloudSaveService.Instance.Files.Player.LoadBytesAsync(cloudFileName);

                if (fileBytes != null && fileBytes.Length > 0)
                {
                    // 2. Write the downloaded bytes back into your local persistent log file path
                    File.WriteAllBytes(logFilePath, fileBytes);
                    Debug.Log($"Successfully loaded and restored log file from Unity Cloud to: {logFilePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"No existing log file found in cloud or failed to load: {ex.Message}");
            }
        }


        

        private async void OnApplicationQuit()
        {
            // Ensure remaining buffered logs are pushed before closing
            await FlushLogsToCloudAsync();
        }
    }
}