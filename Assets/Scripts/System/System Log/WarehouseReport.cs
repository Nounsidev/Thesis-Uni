using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public static class WarehouseReport
{
    public static void GenerateCombinedReport(List<WarehouseTransaction> allTransactions, DateTime targetDate, bool isWeekly)
    {
        // 1. Apply Date Filter (Daily or Weekly) using dd-MM-yyyy format
        List<WarehouseTransaction> timeFilteredList;
        string reportTitle;

        if (isWeekly)
        {
            DateTime startDate = targetDate.AddDays(-7);
            timeFilteredList = allTransactions.Where(t =>
            {
                if (DateTime.TryParseExact(t.item.arrivedAt, "dd-MM-yyyy HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out DateTime txDate) ||
                    DateTime.TryParse(t.item.arrivedAt, out txDate))
                {
                    return txDate >= startDate && txDate <= targetDate;
                }
                return false;
            }).ToList();
            reportTitle = $"Weekly_Report_{targetDate:dd-MM-yyyy}";
        }
        else
        {
            string dateString = targetDate.ToString("dd-MM-yyyy");
            timeFilteredList = allTransactions
                .Where(t => t.item.arrivedAt != null && t.item.arrivedAt.StartsWith(dateString))
                .ToList();
            reportTitle = $"Daily_Report_{targetDate:dd-MM-yyyy}";
        }

        // 2. Sort the list: "IN" comes first, then "OUT"
        var sortedList = timeFilteredList
            .OrderByDescending(t => t.actionType) // "IN" sorts before "OUT" alphabetically
            .ThenBy(t => t.item.arrivedAt)        // Secondary sort by arrival time
            .ToList();

        // 3. Export to a single CSV file
        ExportCombinedToFile(sortedList, reportTitle);
    }

    private static void ExportCombinedToFile(List<WarehouseTransaction> filteredList, string fileNamePrefix)
    {
        StringBuilder csv = new StringBuilder();
        csv.AppendLine("Action Type,Product Code,Product Type,Arrived At,Expires At");

        foreach (var tx in filteredList)
        {
            csv.AppendLine($"{tx.actionType},{tx.item.itemId},{tx.item.arrivedAt},{tx.item.expiredAt}");
        }

        string fileName = $"Warehouse_{fileNamePrefix}.csv";
        string desktopPath = Path.Combine(Application.persistentDataPath, fileName);

        File.WriteAllText(desktopPath, csv.ToString(), Encoding.UTF8);
        Debug.Log($"Generated combined report with IN items first at: {desktopPath}");
    }
}


