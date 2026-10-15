using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using Mono.Cecil;

public class WarehouseManager : MonoBehaviour
{
    public static WarehouseManager Instance { get; private set; }

    [Header("Warehouse UI Layout Container")]
    [SerializeField] private Transform warehouseScrollViewContent;
    [SerializeField] private ShelfRowUI shelfRowPrefab;
    [SerializeField] private Button addShelfButtonPrefab;
    [SerializeField] private RectTransform contentRectTransform;

    [Header("UI Config")]
    [SerializeField] float shelfRowSpacing = 10f;
    [SerializeField] Vector2 addShelfButtonDefault;
    [SerializeField] Vector2 shelfSpawnDefault;
    [SerializeField] Vector2 addShelfOffset;
    [SerializeField] Vector2 shelfOffset;

    [Header("Action Buttons")]
    [SerializeField] private Button loadLayoutButton;
    [SerializeField] private Button saveLayoutButtonButton;
    [SerializeField] private Button setActiveLayoutButton;

    [SerializeField] private WarehouseLayoutData currentLayout = new WarehouseLayoutData();

    private Dictionary<string, ShelfRowUI> activeShelfUIElements = new Dictionary<string, ShelfRowUI>();
    private Button spawnedAddButtonInstance;
    private string defaultSaveFilePath;
    private string activeLayoutId = "DefaultLayout"; // Track active session name/ID
    private float addedSpawnPositionY = 0f;

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
            setActiveLayoutButton.onClick.AddListener(() => SetActiveLayoutAndSyncToServer(activeLayoutId));
        }

        //LoadWarehouseLayoutLocally();
    }

    public void CreateDefaultShelf()
    {
        CreateCustomShelf(1, 1);
    }

    public void CreateCustomShelf(int rows, int cols)
    {
        CustomShelfData newShelf = new CustomShelfData
        {
            shelfId = System.Guid.NewGuid().ToString(),
            rowCount = rows,
            columnCount = cols
        };

        int totalSlots = rows * cols;
        for (int i = 0; i < totalSlots; i++)
        {
            newShelf.slots.Add(new StockSlot
            {
                slotIndex = i,
                productIds = new List<string>()
            });
        }

        currentLayout.customShelves.Add(newShelf);
        RebuildWarehouseUI();
        //SaveWarehouseLayoutLocally(); // Local auto-save on structural edit
    }

    public void DeleteShelf(string shelfId)
    {
        CustomShelfData targetShelf = currentLayout.customShelves.Find(s => s.shelfId == shelfId);
        if (targetShelf != null)
        {
            currentLayout.customShelves.Remove(targetShelf);
        }

        RebuildWarehouseUI();
        //SaveWarehouseLayoutLocally();
    }

    public void ResizeShelf(string shelfId, int newRows, int newCols)
    {
        CustomShelfData targetShelf = currentLayout.customShelves.Find(s => s.shelfId == shelfId);
        if (targetShelf == null) return;

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
                    productIds = new List<string>()
                });
            }
        }

        for (int i = 0; i < targetShelf.slots.Count; i++)
        {
            targetShelf.slots[i].slotIndex = i;
        }
        RebuildWarehouseUI();
        //SaveWarehouseLayoutLocally();
    }

    private void RebuildWarehouseUI()
    {
        foreach (var kvp in activeShelfUIElements)
        {
            if (kvp.Value != null) Destroy(kvp.Value.gameObject);
        }
        activeShelfUIElements.Clear();

        if (spawnedAddButtonInstance != null)
        {
            Destroy(spawnedAddButtonInstance.gameObject);
        }

        Vector2 lastSpawnPos = shelfSpawnDefault;
        foreach (var shelf in currentLayout.customShelves)
        {
            lastSpawnPos.y -= shelf.rowCount * shelfRowSpacing;
            if (shelf != currentLayout.customShelves[^1]) lastSpawnPos.y -= shelfOffset.y;
        }
        lastSpawnPos.y -= addShelfOffset.y;
        DynamicContainer(lastSpawnPos.y);


        lastSpawnPos = shelfSpawnDefault + new Vector2(0f, addedSpawnPositionY);
        foreach (var shelf in currentLayout.customShelves)
        {
            ShelfRowUI shelfUI = Instantiate(shelfRowPrefab, warehouseScrollViewContent);
            shelfUI.Initialize(shelf);
            activeShelfUIElements[shelf.shelfId] = shelfUI;

            shelfUI.transform.localPosition = lastSpawnPos;
            lastSpawnPos.y -= shelf.rowCount * shelfRowSpacing;
            if(shelf != currentLayout.customShelves[^1]) lastSpawnPos.y -= shelfOffset.y;
        }

        lastSpawnPos.y -= addShelfOffset.y; 
        SpawnAddShelfButton(lastSpawnPos);
    }

    private void SpawnAddShelfButton(Vector2 spawnPos)
    {
        spawnedAddButtonInstance = Instantiate(addShelfButtonPrefab, warehouseScrollViewContent);
        spawnedAddButtonInstance.transform.localPosition = spawnPos;
        spawnedAddButtonInstance.transform.SetAsLastSibling();


        spawnedAddButtonInstance.onClick.RemoveAllListeners();
        spawnedAddButtonInstance.onClick.AddListener(CreateDefaultShelf);
    

    }

    private void DynamicContainer(float lastSpawnY)
    {
        ChangeRectTransformSize(0f);

        if (lastSpawnY > -450f)
        {
            addedSpawnPositionY = 0f;
            return;
        } 
        
        float offset = lastSpawnY - 100f;
        addedSpawnPositionY = -offset/2f;
        ChangeRectTransformSize(offset);
    }

    private void ChangeRectTransformSize(float offset)
    {
        Vector2 currentOffsetMin = contentRectTransform.offsetMin;
        currentOffsetMin.y = offset;
        contentRectTransform.offsetMin = currentOffsetMin;
    }

    // --- AUTO-STACKING STOCK LOGIC ---
    public bool TryAddToWarehouse(string productId, int count = 1)
    {
        for (int c = 0; c < count; c++)
        {
            bool added = false;
            foreach (var shelf in currentLayout.customShelves)
            {
                foreach (var slot in shelf.slots)
                {
                    if (!slot.IsFull)
                    {
                        slot.productIds.Add(productId);
                        added = true;
                        break;
                    }
                }
                if (added) break;
            }

            if (!added)
            {
                RefreshAllUI();
                SaveWarehouseLayoutLocally();
                return false;
            }
        }

        RefreshAllUI();
        SaveWarehouseLayoutLocally();
        return true;
    }

    public bool RemoveFromWarehouse(string productId, int count = 1)
    {
        int removedCount = 0;
        foreach (var shelf in currentLayout.customShelves)
        {
            foreach (var slot in shelf.slots)
            {
                while (slot.productIds.Contains(productId) && removedCount < count)
                {
                    slot.productIds.Remove(productId);
                    removedCount++;
                }
                if (removedCount >= count) break;
            }
            if (removedCount >= count) break;
        }

        RefreshAllUI();
        SaveWarehouseLayoutLocally();
        return removedCount > 0;
    }

    private void RefreshAllUI()
    {
        foreach (var kvp in activeShelfUIElements)
        {
            if (kvp.Value != null) kvp.Value.RefreshUI();
        }
    }

    // ==========================================
    // 1. LOAD BUTTON ACTION: Load layout locally from file destination
    // ==========================================
    public void LoadWarehouseLayoutLocally()
    {
        string folderPath = Application.persistentDataPath;
        System.Diagnostics.Process.Start("explorer.exe", folderPath.Replace('/', '\\'));

        if (File.Exists(defaultSaveFilePath))
        {
            string json = File.ReadAllText(defaultSaveFilePath);
            currentLayout = JsonUtility.FromJson<WarehouseLayoutData>(json) ?? new WarehouseLayoutData();
            Debug.Log("Warehouse layout loaded locally.");
        }
        else
        {
            currentLayout = new WarehouseLayoutData();
            Debug.LogWarning("No local save file found. Initialized blank layout.");
        }

        RebuildWarehouseUI();
    }

    // ==========================================
    // 2. SAVE BUTTON ACTION: Save layout locally to disk
    // ==========================================
    public void SaveWarehouseLayoutLocally()
    {
        string json = JsonUtility.ToJson(currentLayout, true);
        File.WriteAllText(defaultSaveFilePath, json);
        Debug.Log($"Warehouse layout saved locally to: {defaultSaveFilePath}");
    }

    // ==========================================
    // 3. SET BUTTON ACTION: Set active layout AND sync to server
    // ==========================================
    public void SetActiveLayoutAndSyncToServer(string layoutId)
    {
        activeLayoutId = layoutId;
        string json = JsonUtility.ToJson(currentLayout, true);

        // Ensure local file is current before pushing to server
        File.WriteAllText(defaultSaveFilePath, json);

        // Trigger network sync request to backend server
        StartCoroutine(SyncLayoutToServer(json));
    }

    private IEnumerator SyncLayoutToServer(string jsonPayload)
    {
        string serverUrl = "https://your-backend-api.com/api/warehouse/save"; // Replace with your endpoint

        using (UnityWebRequest request = new UnityWebRequest(serverUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Layout '{activeLayoutId}' successfully set as active and synced to server!");
            }
            else
            {
                Debug.LogError($"Failed to sync active layout to server: {request.error}");
            }
        }
    }
}