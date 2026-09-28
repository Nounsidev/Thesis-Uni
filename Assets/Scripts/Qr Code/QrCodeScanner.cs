using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZXing;

public class QrCodeScanner : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private RawImage cameraPreview;
    [SerializeField] private AspectRatioFitter aspectFitter;
    [SerializeField] private TextMeshProUGUI resultText;

    [Header("Scan Setting")]
    [SerializeField] bool isAutoScan = false;
    [SerializeField] private float scanInterval = 0.25f;
    [SerializeField] Button scanButton;
    private float scanTimer;

    private bool isCamAvailable;
    private bool isAspectSet;
    private WebCamTexture cameraTexture;
    private IBarcodeReader barcodeReader;

    private void Start()
    {
        SetUpCamera();
        scanButton.onClick.AddListener(ScanButton);
    }

    private void OnDisable()
    {
        if (isCamAvailable && cameraTexture != null && cameraTexture.isPlaying)
            cameraTexture.Stop();
        scanButton.onClick.RemoveListener(ScanButton);
    }

    private void Update()
    {
       
        if (!isCamAvailable || cameraTexture == null || !cameraTexture.isPlaying) return;
        //Debug.Log("Scanning");
        // Set aspect ratio ONCE as soon as hardware initializes real dimensions
        if (!isAspectSet && cameraTexture.width > 16)
        {
            AdjustAspectRatio();
            isAspectSet = true;
        }

        if (!isAutoScan) return;
        scanTimer += Time.deltaTime;
        if (scanTimer < scanInterval) return;

        scanTimer = 0;
        DecodeFrame();
    }

    private void ScanButton()
    {
        DecodeFrame();
    }

    private void SetUpCamera()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            isCamAvailable = false;
            Debug.LogWarning("No Camera Available");
            return;
        }

        cameraTexture = new WebCamTexture(devices[0].name, 1280, 720);
        cameraPreview.texture = cameraTexture;
        cameraTexture.Play();

        isCamAvailable = true;
        BarcodeReaderInit();
    }

    private void AdjustAspectRatio()
    {
        if (aspectFitter == null) return;

        // Calculate native ratio
        float videoRatio = (float)cameraTexture.width / (float)cameraTexture.height;
        aspectFitter.aspectRatio = videoRatio;

        // Fix potential vertical flipping issue on certain PC camera drivers
        if (cameraTexture.videoVerticallyMirrored)
        {
            cameraPreview.rectTransform.localScale = new Vector3(1, -1, 1);
        }
        else
        {
            cameraPreview.rectTransform.localScale = Vector3.one;
        }
    }

    private void BarcodeReaderInit()
    {
        barcodeReader = new BarcodeReader
        {
            AutoRotate = false,
            Options = new ZXing.Common.DecodingOptions
            {
                TryHarder = true // Improves detection on lower-quality webcams
            }
        };
    }

    private void DecodeFrame()
    {
        try
        {
            Color32[] rawPixels = cameraTexture.GetPixels32();
            int width = cameraTexture.width;
            int height = cameraTexture.height;

            if (rawPixels == null || rawPixels.Length == 0) return;

            Result result = barcodeReader.Decode(rawPixels, width, height);

            if (result != null)
            {
                Debug.Log($"Scanned QR Code: {result.Text}");
                if (resultText != null)
                {
                    resultText.text = result.Text;
                }
            }
            //else Debug.Log("Not getting any results");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"QR Decoding Error: {e.Message}");
        }
    }
}