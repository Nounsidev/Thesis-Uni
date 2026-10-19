using SmartWarehouse.Inventory;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StockSlotUIElement : MonoBehaviour
{
    [SerializeField] private Image productIcon;
    [SerializeField] private TextMeshProUGUI quantityText;

    private StockSlot currentSlotData;

    public void Render(StockSlot slot)
    {
        currentSlotData = slot;

        if (slot.IsEmpty)
        {
            if (productIcon != null) productIcon.gameObject.SetActive(false);
            quantityText.text = "EMPTY";
        }
        else
        {
            // For multi-product slots, we can display the icon of the first item (or handle dual icons)
            string firstProductId = slot.products[0].itemId;
            ProductBase product = new();
            //ProductBase product = ProductCatalogContainer.Instance.GetProductById(firstProductId);

            if (product != null && productIcon != null)
            {
                //productIcon.sprite = product.;
                productIcon.gameObject.SetActive(true);
            }

            if (quantityText != null)
            {
                // Shows total items in this slot (up to 2)
                quantityText.text = $"x{slot.products.Count}";
            }

        }
    }

    /*public void OnSlotClicked()
    {
        if (currentSlotData != null && !currentSlotData.IsEmpty)
        {
            // Clicking slot removes the last added item from this slot
            string targetId = currentSlotData.products[currentSlotData.products.Count - 1].itemId;
            WarehouseManager.Instance.RemoveFromWarehouse(targetId, 1);
        }
    }*/
}