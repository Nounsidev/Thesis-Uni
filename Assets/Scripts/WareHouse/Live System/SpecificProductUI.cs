using TMPro;
using UnityEngine;

public class SpecificProductUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI posText;

    [SerializeField] TextMeshProUGUI expiredDate;
    [SerializeField] TextMeshProUGUI arrivalDate;

    [SerializeField] TextMeshProUGUI warningText;

    public void SetInfo(PlacedItemInfo info, bool isExpiryWarning)
    {
        posText.text = $"Shelf: {info.shelfId}  Row: {info.row}  Col: {info.column}";
        expiredDate.text = $"Expires: {info.item.expiredAt}";
        arrivalDate.text = $"Arrived: {info.item.arrivedAt}";

       
        warningText.gameObject.SetActive(isExpiryWarning);

    }
}

