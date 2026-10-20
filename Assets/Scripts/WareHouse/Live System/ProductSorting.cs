using System;
using System.Linq;
using UnityEngine;



namespace SmartWarehouse.Inventory
{
    public enum ExpiryStage {Close, Far }

    public class ProductSorting : MonoBehaviour
    {
        [SerializeField] int expiryThreshold;
        ActiveLayout activeLayout;

        private void Awake()
        {
            activeLayout = GetComponent<ActiveLayout>();
        }

        public void SortItem(StorageItem storageItem)
        {
            ExpiryStage stage = EvaluateExpiryDate(storageItem.expiredAt);
            CustomShelfData shelfData = FindBestShelf(storageItem);

            StockSlot stockSlot = FindBestSlotForProduct(storageItem, shelfData, stage);

            activeLayout.AddToWarehouse(storageItem, shelfData.shelfId, stockSlot.slotIndex);
        }

        private CustomShelfData FindBestShelf(StorageItem storageItem)
        {
            var shelves = activeLayout.GetWarehouseData().customShelves;

            var assignShelves = shelves.FindAll(s =>
                (s.prioritizedProducts != null && s.prioritizedProducts.Contains(storageItem.itemId))
                || (s.prioritizedProducts == null)
                || (s.prioritizedProducts.Count == 0)).OrderByDescending(s => s.GetProductCount()).ToList();

            return shelves[0];
        }

        private StockSlot FindBestSlotForProduct(StorageItem storageItem, CustomShelfData targetShelf, ExpiryStage stage)
        {
            StockSlot targetSlot = null;

            if (stage == ExpiryStage.Close)
            {
                // Close expiry items go straight to the bottom (lowest slot index available)
                targetSlot = targetShelf.slots
                    .Where(slot => !slot.IsFull)
                    .OrderBy(slot => slot.slotIndex) // Index 0 is bottom
                    .FirstOrDefault();
            }
            else
            {
                // Far or Mid expiry items go to upper buffer slots (highest available slot index)
                targetSlot = targetShelf.slots
                    .Where(slot => !slot.IsFull)
                    .OrderByDescending(slot => slot.slotIndex) // Top slots first for buffer storage
                    .FirstOrDefault();
            }

            return targetSlot;
        }

        private ExpiryStage EvaluateExpiryDate(string expiryStr)
        {
            if(DateTime.TryParse(expiryStr, out DateTime expiry))
            {
                int dayRemaining = (expiry - DateTime.Now).Days;

                if (dayRemaining > expiryThreshold) return ExpiryStage.Far;
                else return ExpiryStage.Close;
            }
            return ExpiryStage.Close;
        }
    }
}
