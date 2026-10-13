using SmartWarehouse.Cloud;
using SmartWarehouse.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SmartWarehouse.Logging
{
    public enum TransactionActionType
    {
        IN,
        OUT
    }

    public class TransactionManager : MonoBehaviour
    {
        public static TransactionManager Instance { get; private set; }

        private TransactionHistoryContainer transactionContainer = new();

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
        }

        /// <summary>
        /// Records a new transaction (IN or OUT), saves locally, and syncs to cloud.
        /// </summary>
        public async void RecordTransaction(StorageItem item, TransactionActionType actionType)
        {
            WarehouseTransaction newTx = new WarehouseTransaction
            {
                item = item,
                actionType = actionType 
            };

            transactionContainer.transactions.Add(newTx);
            Debug.Log($"Recorded Transaction: [{newTx.actionType}] for SKU {item.itemId}");

            // Sync updated transaction container using our generic CloudFileSaving helper
            await CloudFileSaving.SaveDataAsync(Key.Transaction, transactionContainer);
        }

        /// <summary>
        /// Pulls the latest transaction history from Unity Cloud Save.
        /// </summary>
        public async Task LoadTransactionsAsync()
        {
            var loadedData = await CloudFileSaving.LoadDataAsync<TransactionHistoryContainer>(Key.Transaction);
            if (loadedData != null)
            {
                transactionContainer = loadedData;
                Debug.Log($"Loaded {transactionContainer.transactions.Count} transactions into memory.");
            }
        }

        public void ExportTransaction()
        {
            WarehouseReport.GenerateCombinedReport(transactionContainer.transactions, DateTime.Now, isWeekly: false);
        }
    }

    [Serializable]
    public class WarehouseTransaction
    {
        public StorageItem item;
        public TransactionActionType actionType;
    }

    [Serializable]
    public class TransactionHistoryContainer
    {
        public List<WarehouseTransaction> transactions = new List<WarehouseTransaction>();
        public void AddTransaction(StorageItem item, TransactionActionType actionType)
        {
            transactions.Add(new WarehouseTransaction { item = item, actionType = actionType });
        }
    }
}
