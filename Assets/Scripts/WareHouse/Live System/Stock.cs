using UnityEngine;
using UnityEngine.UI;

public class Stock : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] Button exportButton;
    [SerializeField] Button switchLayoutButton;
    [SerializeField] GameObject[] layouts;
    int currentLayoutId;

    private void Awake()
    {
        ResetLayout();

        switchLayoutButton.onClick.AddListener(SwitchLayoutButton);
        exportButton.onClick.AddListener(ExportProductButton);
    }

    private void OnDestroy()
    {
        switchLayoutButton.onClick.RemoveListener(SwitchLayoutButton);
        exportButton.onClick.RemoveListener(ExportProductButton);
    }


    private void ResetLayout()
    {
        foreach (var layout in layouts)
            layout.SetActive(false);
        layouts[0].SetActive(true);
    }

    private void SwitchLayoutButton()
    {
        layouts[currentLayoutId].SetActive(false);

        currentLayoutId++;
        if (currentLayoutId >= layouts.Length) currentLayoutId = 0;

        layouts[currentLayoutId].SetActive(true);
    }

    private void ExportProductButton()
    {

    }
}
