using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZXing;
using ZXing.QrCode;
using ZXing.QrCode.Internal;

public class QrCodeGenerator : MonoBehaviour
{
    [SerializeField] int qrSize = 512;
    [SerializeField] RawImage rawImageReceiver;
    [SerializeField] TMP_InputField inputField;
    [SerializeField] Button convertButton;

    Texture2D storedEncodedTexture;

    private void Start()
    {
        CreateQrTexture();
        convertButton.onClick.AddListener(ConvertButton);
    }

    private void OnDisable()
    {
        convertButton.onClick.RemoveListener(ConvertButton);
    }

    private void CreateQrTexture()
    {
        storedEncodedTexture = new Texture2D(256, 256);
    }

    private Color32[] Encode(string textForEncoding, int size)
    {
        var options = new QrCodeEncodingOptions
        {
            Height = qrSize,
            Width = qrSize,
            Margin = 1,
        };

        options.Hints.Add(EncodeHintType.ERROR_CORRECTION, ErrorCorrectionLevel.H);

        BarcodeWriter writer = new BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = options,
        };

        return writer.Write(textForEncoding);
    }

    private void ConvertButton()
    {
        Debug.Log("Encoding...");
        EncodeTextToQrCode();
    }

    private void EncodeTextToQrCode()
    { 
        string textWrite = string.IsNullOrEmpty(inputField.text) ? "You should write something" : inputField.text;

        Color32[] convertPixelToTexture = Encode(textWrite, qrSize);
        storedEncodedTexture.SetPixels32(convertPixelToTexture);

        storedEncodedTexture.Apply();

        rawImageReceiver.texture = storedEncodedTexture;
    }


}
