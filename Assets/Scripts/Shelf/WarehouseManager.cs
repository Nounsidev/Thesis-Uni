using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using SmartWarehouse.Logging;

public class WarehouseManager : WarehouseLayoutBase
{
    public static WarehouseManager Instance { get; private set; }

    [Header("Warehouse UI Layout Container")]
    [SerializeField] private Button addShelfButtonPrefab;
    [SerializeField] private ActiveLayout activeLayout;

    [Header("Action Buttons")]
    [SerializeField] private Button loadLayoutButton;
    [SerializeField] private Button saveLayoutButtonButton;
    [SerializeField] private Button setActiveLayoutButton;


    private Button spawnedAddButtonInstance;
    private string defaultSaveFilePath;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        defaultSaveFilePath = Path.Combine(Application.persistentDataPath, "warehouse_layout.json");

        SpawnAddShelfButton(addShelfButtonDefault);
    }

    private void Start()
    {
        // Programmatically hook up button listeners via AddListener
        if (loadLayoutButton != null)
        {
            loadLayoutButton.onClick.AddListener(LoadWarehouseLayoutLocally);
        }

        if (saveLayoutButtonButton != null)
        {
            saveLayoutButtonButton.onClick.AddListener(SaveWarehouseLayoutLocally);
        }

        if (setActiveLayoutButton != null)
        {
            setActiveLayoutButton.onClick.AddListener(SetActiveLayoutAndSyncToServer);
        }

        //LoadWarehouseLayoutLocally();
    }

    private void OnDestroy()
    {
        loadLayoutButton.onClick.RemoveListener(LoadWarehouseLayoutLocally);
        saveLayoutButtonButton.onClick.RemoveListener(SaveWarehouseLayoutLocally);
        setActiveLayoutButton.onClick.RemoveListener(SetActiveLayoutAndSyncToServer);

    }

    public void CreateDefaultShelf()
    {
        CreateCustomShelf(1, 1);
    }

    public void CreateCustomShelf(int rows, int cols)
    {
        CustomShelfData newShelf = new CustomShelfData
        {
            shelfId = currentWarehouseData.customShelves.Count,
            rowCount = rows,
            columnCount = cols
        };

        int totalSlots = rows * cols;
        for (int i = 0; i < totalSlots; i++)
        {
            newShelf.slots.Add(new StockSlot
            {
                slotIndex = i,
                products = new List<StorageItem>()
            });
        }

        currentWarehouseData.customShelves.Add(newShelf);
        RebuildWarehouseUI();
        //SaveWarehouseLayoutLocally(); // Local auto-save on structural edit
    }

    public void DeleteShelf(int shelfId)
    {
        CustomShelfData targetShelf = currentWarehouseData.customShelves[shelfId];
        if (targetShelf != null)
        {
            currentWarehouseData.customShelves.Remove(targetShelf);
        }
        RefreshShelfId();
        RebuildWarehouseUI();
        //SaveWarehouseLayoutLocally();
    }

    public void ResizeShelf(int shelfId, int newRows, int newCols)
    {
        CustomShelfData targetShelf = currentWarehouseData.customShelves[shelfId];
        if (targetShelf == null)
        {
            Debug.Log("Null Target shelf");
            return;
        }

        if (targetShelf.rowCount == newRows && targetShelf.columnCount == newCols) return;

        targetShelf.rowCount = newRows;
        targetShelf.columnCount = newCols;

        int targetTotalSlots = newRows * newCols;

        if (targetShelf.slots.Count > targetTotalSlots)
        {
            targetShelf.slots.RemoveRange(targetTotalSlots, targetShelf.slots.Count - targetTotalSlots);
        }
        else if (targetShelf.slots.Count < targetTotalSlots)
        {
            int currentCount = targetShelf.slots.Count;
            for (int i = currentCount; i < targetTotalSlots; i++)
            {
                targetShelf.slots.Add(new StockSlot
                {
                    slotIndex = i,
                    products = new List<StorageItem>()
                });
            }
        }

        for (int i = 0; i < targetShelf.slots.Count; i++)
        {
            targetShelf.slots[i].slotIndex = i;
        }
        RebuildWarehouseUI(true);
        //SaveWarehouseLayoutLocally();
    }

    public void PriorityChange(int shelfId, List<string> newPrioritizedProducts)
    {
        Debug.Log("Shelf ID: " + shelfId);
        CustomShelfData targetShelf = currentWarehouseData.customShelves[shelfId];
        if (targetShelf == null)
        {
            Debug.Log("Null Target shelf");
            return; 
        }

        targetShelf.prioritizedProducts = newPrioritizedProducts;

        //activeShelfUIElements[shelfId].RefreshPriorityUI();
    }

    protected override Vector2 RebuildWarehouseUI(bool isEdit = true)
    {
        if (spawnedAddButtonInstance != null)
        {
            Destroy(spawnedAddButtonInstance.gameObject);
        }
        var lastSpawnPos = base.RebuildWarehouseUI(isEdit);

        SpawnAddShelfButton(lastSpawnPos);
        return lastSpawnPos;
    }

    private void SpawnAddShelfButton(Vector2 spawnPos)
    {
        spawnedAddButtonInstance = Instantiate(addShelfButtonPrefab, warehouseScrollViewContent);
        spawnedAddButtonInstance.transform.localPosition = spawnPos;
        spawnedAddButtonInstance.transform.SetAsLastSibling();


        spawnedAddButtonInstance.onClick.RemoveAllListeners();
        spawnedAddButtonInstance.onClick.AddListener(CreateDefaultShelf);

    }

    // ==========================================
    // 1. LOAD BUTTON ACTION: Load layout locally from file destination
    // ==========================================
    public void LoadWarehouseLayoutLocally()
    {
        if (File.Exists(defaultSaveFilePath))
        {
            string json = File.ReadAllText(defaultSaveFilePath);
            currentWarehouseData = JsonUtility.FromJson<WarehouseLayoutData>(json) ?? new WarehouseLayoutData();
            LogManager.Instance.LogEvent(LogCategory.SYSTEM_Event, $"Warehouse layout loaded locally");
        }
        else
        {
            currentWarehouseData = new WarehouseLayoutData();
            LogManager.Instance.LogEvent(LogCategory.SYSTEM_Error, $"No warehouse layout found locally");
        }
        
        RebuildWarehouseUI();
    }

    private void RefreshShelfId()
    { 
        foreach(var shelf in currentWarehouseData.customShelves)
        {
            shelf.shelfId = currentWarehouseData.customShelves.IndexOf(shelf);
        }
    }

    // ==========================================
    // 2. SAVE BUTTON ACTION: Save layout locally to disk
    // ==========================================
    public void SaveWarehouseLayoutLocally()
    {
        string json = JsonUtility.ToJson(currentWarehouseData, true);
        File.WriteAllText(defaultSaveFilePath, json);
        //Debug.Log($"Warehouse layout saved locally to: {defaultSaveFilePath}");
        LogManager.Instance.LogEvent(LogCategory.SYSTEM_Event, $"Warehouse layout saved locally");

        // Open the folder in Windows Explorer so you can see the saved file
        //string folderPath = Application.persistentDataPath;
        //System.Diagnostics.Process.Start("explorer.exe", folderPath.Replace('/', '\\'));
    }

    // ==========================================
    // 3. SET BUTTON ACTION: Set active layout AND sync to server
    // ==========================================
    public async void SetActiveLayoutAndSyncToServer()
    {
        LoadingIcon.Instance.Show();
        activeLayout.SetActiveLayout(currentWarehouseData);
        await CloudDataManager.SaveWarehouseLayoutData(currentWarehouseData);
        LoadingIcon.Instance.Hide();
    }

    public void LoadLayoutData(WarehouseLayoutData layoutData)
    { 
        currentWarehouseData = layoutData;
        RebuildWarehouseUI();
    }
}