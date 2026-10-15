using UnityEngine;
using UnityEngine.UI;
using SmartWarehouse.Logging;

namespace SmartWarehouse.UI
{
    public enum PanelType
    {
        AddProduct,
        Catalog,
        Warehouse,
        Stock,
        QrGenerator,
        QrScanner,
        Plc,
    }

    public class UiHandler : MonoBehaviour
    {
        [SerializeField] GameObject[] panels;
        [SerializeField] Button[] switchButtons;

        private int currentPanelId;
        public static UiHandler Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {

            for (int i = 0; i < panels.Length; i++)
            {
                int iCpy = i;
                switchButtons[i].onClick.AddListener(() => SwitchButton(iCpy));
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

        public void SwitchPanel(PanelType panelType)
        { 
            int id = (int)panelType;

            panels[currentPanelId].SetActive(false);

            currentPanelId = id;
            panels[currentPanelId].SetActive(true);
        }
    }
}
