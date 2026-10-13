using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using SmartWarehouse.Cloud;
using SmartWarehouse.Core;

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

    public class LogManager : MonoBehaviour
    {
        public static LogManager Instance { get; private set; }

        [Header("UI References")]
        private LogUi logUi;

        [Header("Cloud Sync Settings")]
        [SerializeField] private int logCountBeforeCloudSync = 20;
        private int logsWrittenSinceLastSync = 0;

        private string logFilePath;

        private void Awake()
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
        }

        public async void LogEvent(LogCategory category, string message)
        {
            string timestamp = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
            string formattedEntry = $"[{timestamp}] [{category}] {message}";

            // 1. Console Log
            Debug.Log(formattedEntry);

            // 2. UI Display
            if (logUi != null)
            {
                logUi.UpdateText(formattedEntry + "\n");
            }

            // 3. Local File Log (Direct Append - already working!)
            try
            {
                File.AppendAllText(logFilePath, formattedEntry + "\n");
                logsWrittenSinceLastSync++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to write to log file: {ex.Message}");
            }

            // 4. Trigger cloud sync every X log entries
            if (logsWrittenSinceLastSync >= logCountBeforeCloudSync)
            {
                logsWrittenSinceLastSync = 0;
                await FlushLogsToCloudAsync();
            }
        }

        private async Task FlushLogsToCloudAsync()
        {
            if (!File.Exists(logFilePath)) return;

            try
            {
                // Read the actual local log file as raw bytes and sync via cloud helper
                byte[] fileBytes = File.ReadAllBytes(logFilePath);
                await CloudFileSaving.SaveBytesAsync(Key.SystemLog, fileBytes);
                Debug.Log("System log file successfully synced to cloud.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to flush log file to cloud: {ex.Message}");
            }
        }

        public async Task LoadSystemLogAsync()
        {
            try
            {
                byte[] fileBytes = await CloudFileSaving.LoadBytesAsync(Key.SystemLog);
                if (fileBytes != null && fileBytes.Length > 0)
                {
                    File.WriteAllBytes(logFilePath, fileBytes);
                    Debug.Log("Successfully restored local log file from cloud.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Could not load log file from cloud (using local cache): {ex.Message}");
            }
        }

        private async void OnApplicationQuit()
        {
            await FlushLogsToCloudAsync();
        }
    }
}