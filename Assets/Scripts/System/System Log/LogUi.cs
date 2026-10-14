using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LogUi : MonoBehaviour
{
    [SerializeField] GameObject logPanel;
    [SerializeField] private TextMeshProUGUI logTextDisplay;
    [SerializeField] private int maxLogLines = 20;
    [SerializeField] KeyCode toggleKey = KeyCode.L;
    bool isActive = false;


    private void Update()
    {
        if(Input.GetKeyDown(toggleKey))
        {
            ToggleLog();
        }
    }


    void ToggleLog()
    {
        isActive = !isActive;
        logPanel.SetActive(isActive);
    }

    public void UpdateText(string newLine)
    {
        logTextDisplay.text += newLine;
        TrimLogDisplay();
    }

    private void TrimLogDisplay()
    {
        string[] lines = logTextDisplay.text.Split('\n');
        if (lines.Length > maxLogLines)
        {
            logTextDisplay.text = string.Join("\n", lines, lines.Length - maxLogLines, maxLogLines);
        }
    }
}
