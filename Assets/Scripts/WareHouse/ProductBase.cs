using SmartWarehouse.Cloud;
using SmartWarehouse.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartWarehouse.Inventory
{     
    public class ProductBase
    {
        [Header("Identification")]
        public string productName;
        public List<string> labels = new List<string>();
        public string customCode;

        public string iconFileName;

        [Header("Warehouse Rules & Thresholds")]
        public int minDaysBeforeExpiryWarning = 3;
        public int minStockWarningThreshold = 5;
    }
}
