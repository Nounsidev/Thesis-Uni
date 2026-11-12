using System.Collections.Generic;
using UnityEngine;

public class WarehouseLayoutBase : MonoBehaviour
{
    [Header("Warehouse UI Layout Container")]
    [SerializeField] protected Transform warehouseScrollViewContent;
    [SerializeField] protected ShelfRowUI shelfRowPrefab;
    [SerializeField] protected RectTransform contentRectTransform;

    [Header("UI Config")]
    [SerializeField] float shelfRowSpacing = 10f;
    [SerializeField] protected Vector2 addShelfButtonDefault;
    [SerializeField] Vector2 shelfSpawnDefault;
    [SerializeField] Vector2 addShelfOffset;
    [SerializeField] Vector2 shelfOffset;

    private float addedSpawnPositionY = 0f;
    [SerializeField] protected WarehouseLayoutData currentWarehouseData = new WarehouseLayoutData();
    protected Dictionary<int, ShelfRowUI> activeShelfUIElements = new Dictionary<int, ShelfRowUI>();


    protected virtual Vector2 RebuildWarehouseUI(bool isEdit)
    {
        foreach (var kvp in activeShelfUIElements)
        {
            if (kvp.Value != null) Destroy(kvp.Value.gameObject);
        }
        activeShelfUIElements.Clear();

        Vector2 lastSpawnPos = shelfSpawnDefault;
        foreach (var shelf in currentWarehouseData.customShelves)
        {
            lastSpawnPos.y -= shelf.rowCount * shelfRowSpacing;
            if (shelf != currentWarehouseData.customShelves[^1]) lastSpawnPos.y -= shelfOffset.y;
        }
        lastSpawnPos.y -= addShelfOffset.y;
        DynamicContainer(lastSpawnPos.y);


        lastSpawnPos = shelfSpawnDefault + new Vector2(0f, addedSpawnPositionY);
        foreach (var shelf in currentWarehouseData.customShelves)
        {
            ShelfRowUI shelfUI = Instantiate(shelfRowPrefab, warehouseScrollViewContent);
            shelfUI.Initialize(shelf, isEdit);
            activeShelfUIElements[shelf.shelfId] = shelfUI;

            shelfUI.transform.localPosition = lastSpawnPos;
            lastSpawnPos.y -= shelf.rowCount * shelfRowSpacing;
            if (shelf != currentWarehouseData.customShelves[^1]) lastSpawnPos.y -= shelfOffset.y;
        }

        lastSpawnPos.y -= addShelfOffset.y;
        return lastSpawnPos;
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
        addedSpawnPositionY = -offset / 2f;
        ChangeRectTransformSize(offset);
    }

    private void ChangeRectTransformSize(float offset)
    {
        Vector2 currentOffsetMin = contentRectTransform.offsetMin;
        currentOffsetMin.y = offset;
        contentRectTransform.offsetMin = currentOffsetMin;
    }
}
