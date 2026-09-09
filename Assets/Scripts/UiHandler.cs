using UnityEngine;
using UnityEngine.UI;

public class UiHandler : MonoBehaviour
{
    [SerializeField] GameObject[] panels;
    [SerializeField] Button switchButton;

    private int currentPanelId;

    private void Start()
    {
        switchButton.onClick.AddListener(SwitchButton);

        FindActivePanel();
    }

    private void OnDisable()
    {
        switchButton.onClick.RemoveListener(SwitchButton);
    }

    private void FindActivePanel()
    {
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i].activeSelf)
            { 
                currentPanelId = i;
                break;
            }
        }
    }

    private void SwitchButton()
    {
        panels[currentPanelId].SetActive(false);

        currentPanelId++;
        if(currentPanelId >= panels.Length)
            currentPanelId = 0;

        panels[currentPanelId].SetActive(true);
    }
}
