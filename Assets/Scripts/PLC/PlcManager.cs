using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Interop.ActUtlTypeLib; // Correct wrapper namespace

public class PlcManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button connectButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("PLC Settings")]
    [Tooltip("Logical Station Number configured in MX Component Communication Setup Utility")]
    [SerializeField] private int logicalStationNumber = 1;

    private ActUtlType plc;
    private bool isConnected = false;

    private void Start()
    {
        //ActUtlType plc = new ActUtlType();

        if (connectButton != null) connectButton.onClick.AddListener(ConnectToPlc);
        if (disconnectButton != null) disconnectButton.onClick.AddListener(DisconnectFromPlc);

        UpdateStatus("Disconnected");
    }

    public void ConnectToPlc()
    {
        plc.ActLogicalStationNumber = logicalStationNumber;

        // Open connection to MX Component / GX Simulator
        int resultCode = plc.Open();

        if (resultCode == 0)
        {
            isConnected = true;
            Debug.Log("Successfully connected to Mitsubishi PLC / GX Simulator!");
            UpdateStatus("Connected");
        }
        else
        {
            isConnected = false;
            Debug.LogError($"PLC Connection failed! Error code: 0x{resultCode:X}");
            UpdateStatus($"Error: 0x{resultCode:X}");
        }
    }

    // Read a 16-bit word register (e.g., "D100")
    public int ReadDeviceRegister(string deviceCode)
    {
        if (!isConnected) return 0;

        int resultCode = plc.GetDevice(deviceCode, out int value);
        if (resultCode == 0)
        {
            return value;
        }

        Debug.LogError($"Failed to read device {deviceCode}. Code: 0x{resultCode:X}");
        return 0;
    }

    // Write a value to a register or bit (e.g., "Y0", "D100")
    public void WriteDeviceRegister(string deviceCode, int value)
    {
        if (!isConnected) return;

        int resultCode = plc.SetDevice(deviceCode, value);
        if (resultCode == 0)
        {
            Debug.Log($"Set {deviceCode} = {value}");
        }
        else
        {
            Debug.LogError($"Failed to write device {deviceCode}. Code: 0x{resultCode:X}");
        }
    }

    public void DisconnectFromPlc()
    {
        if (plc != null && isConnected)
        {
            plc.Close();
            isConnected = false;
            UpdateStatus("Disconnected");
            Debug.Log("Disconnected from PLC.");
        }
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void OnDisable()
    {
        DisconnectFromPlc();
    }
}