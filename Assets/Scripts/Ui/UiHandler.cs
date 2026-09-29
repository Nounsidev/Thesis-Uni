using UnityEngine;
using UnityEngine.UI;

public class UiHandler : MonoBehaviour
{
    [SerializeField] GameObject[] panels;
    [SerializeField] Button[] switchButtons;

    private int currentPanelId;

    private void Start()
    {
        for (int i = 0; i < panels.Length; i++)
        {
            int iCpy = i;
            switchButtons[i].onClick.AddListener(()=>SwitchButton(iCpy));
        }

        ResetActivePanel();
    }

    private void OnDisable()
    {
        for (int i = 0; i < panels.Length; i++)
        {
            int iCpy = i;
            switchButtons[i].onClick.RemoveListener(() => SwitchButton(iCpy));
        }
    }

    private void ResetActivePanel()
    {
        for (int i = 1; i < panels.Length; i++)
        {
            panels[i].SetActive(false);
        }

        panels[0].SetActive(true);
    }

    private void SwitchButton(int buttonId)
    {
        panels[currentPanelId].SetActive(false);

        currentPanelId = buttonId;
        panels[currentPanelId].SetActive(true);
    }
}
