using System;
using UnityEngine;
using SmartWarehouse.Core;
using SmartWarehouse.Cloud;

namespace SmartWarehouse.Inventory
{     
    public class ProductBase
    {
        [Header("Identification")]
        public string productName;
        public string category;
        public string customCode;

        public string iconFileName;

        [Header("Warehouse Rules & Thresholds")]
        public int minDaysBeforeExpiryWarning = 3;
        public int minStockWarningThreshold = 5;
    }
}
