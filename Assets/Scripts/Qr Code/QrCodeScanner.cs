using SmartWarehouse.Inventory;
using SmartWarehouse.Logging;
using System;
using TMPro;
using Unity.InferenceEngine;

using UnityEngine;
using UnityEngine.UI;
using ZXing;

public class QrCodeScanner : MonoBehaviour
{
    public enum ScanMode
    {
        RawCamera,
        Model1_ESPCN,
        Model2_EDSR,
        Both_Fallback // Tries Model 1 first; falls back to Model 2 if Model 1 fails to decode
    }

    [Header("Components")]
    [SerializeField] private RawImage cameraPreview;
    [SerializeField] private AspectRatioFitter aspectFitter;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private ProductSorting productSorting;

    [Header("Scan Pipeline Mode")]
    [SerializeField] private ScanMode scanMode = ScanMode.Model1_ESPCN;

    [Header("Model 1 Settings (ESPCN)")]
    [SerializeField] private ModelAsset model1Asset;
    [SerializeField] private RawImage model1Preview;
    [SerializeField] private int model1InputRes = 240;
    [SerializeField] private int model1OutputRes = 480;

    [Header("Model 2 Settings (EDSR)")]
    [SerializeField] private ModelAsset model2Asset;
    [SerializeField] private RawImage model2Preview;
    [SerializeField] private int model2InputRes = 240;
    [SerializeField] private int model2OutputRes = 480;

    [Header("Scan Settings")]
    [SerializeField] private bool isAutoScan = false;
    [SerializeField] private float scanInterval = 0.25f;
    [SerializeField] private Button scanButton;
    [SerializeField] private Button initButton;
    private float scanTimer;

    private bool isCamAvailable;
    private bool isAspectSet;
    private WebCamTexture cameraTexture;
    private IBarcodeReader barcodeReader;

    // SENTIS MODEL 1 FIELDS
    private Worker worker1;
    private RenderTexture lrRT1;
    private RenderTexture hrRT1;
    private Texture2D readbackTex1;

    // SENTIS MODEL 2 FIELDS
    private Worker worker2;
    private RenderTexture lrRT2;
    private RenderTexture hrRT2;
    private Texture2D readbackTex2;

    private void Start()
    {
        scanButton.onClick.AddListener(ScanButton);
        initButton.onClick.AddListener(InitButton);

        InitializeSentisModels();
    }

    private void OnDisable()
    {
        if (isCamAvailable && cameraTexture != null && cameraTexture.isPlaying)
            cameraTexture.Stop();

        scanButton.onClick.RemoveListener(ScanButton);
        initButton.onClick.RemoveListener(InitButton);

        CleanupSentis();
    }

    private void Update()
    {
        if (!isCamAvailable || cameraTexture == null || !cameraTexture.isPlaying) return;

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

    private void InitButton()
    {
        SetUpCamera();
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

        LogManager.Instance.LogEvent(LogCategory.QRScanner_Event, "Camera initialized successfully.");
        BarcodeReaderInit();
    }

    private void InitializeSentisModels()
    {
        // 1. Initialize Model 1
        if (model1Asset != null)
        {
            Model model1 = ModelLoader.Load(model1Asset);
            worker1 = new Worker(model1, BackendType.GPUCompute);

            lrRT1 = RenderTexture.GetTemporary(model1InputRes, model1InputRes, 0, RenderTextureFormat.ARGB32);
            hrRT1 = RenderTexture.GetTemporary(model1OutputRes, model1OutputRes, 0, RenderTextureFormat.ARGB32);
            readbackTex1 = new Texture2D(model1OutputRes, model1OutputRes, TextureFormat.RGB24, false);

            if (model1Preview != null) model1Preview.texture = hrRT1;
        }

        // 2. Initialize Model 2
        if (model2Asset != null)
        {
            Model model2 = ModelLoader.Load(model2Asset);
            worker2 = new Worker(model2, BackendType.GPUCompute);

            lrRT2 = RenderTexture.GetTemporary(model2InputRes, model2InputRes, 0, RenderTextureFormat.ARGB32);
            hrRT2 = RenderTexture.GetTemporary(model2OutputRes, model2OutputRes, 0, RenderTextureFormat.ARGB32);
            readbackTex2 = new Texture2D(model2OutputRes, model2OutputRes, TextureFormat.RGB24, false);

            if (model2Preview != null) model2Preview.texture = hrRT2;
        }
    }

    private void AdjustAspectRatio()
    {
        if (aspectFitter != null && cameraTexture != null)
        {
            float videoRatio = (float)cameraTexture.width / (float)cameraTexture.height;
            aspectFitter.aspectRatio = videoRatio;

            if (cameraTexture.videoVerticallyMirrored)
            {
                cameraPreview.rectTransform.localScale = new Vector3(1, -1, 1);
            }
            else
            {
                cameraPreview.rectTransform.localScale = Vector3.one;
            }
        }

        if (model1Preview != null) model1Preview.rectTransform.localScale = Vector3.one;
        if (model2Preview != null) model2Preview.rectTransform.localScale = Vector3.one;
    }

    private void BarcodeReaderInit()
    {
        barcodeReader = new BarcodeReader
        {
            AutoRotate = false,
            Options = new ZXing.Common.DecodingOptions
            {
                TryHarder = true
            }
        };
    }

    private void DecodeFrame()
    {
        try
        {
            switch (scanMode)
            {
                case ScanMode.RawCamera:
                    DecodeNativeFrame();
                    break;

                case ScanMode.Model1_ESPCN:
                    if (worker1 != null) DecodeWithModel1();
                    else DecodeNativeFrame();
                    break;

                case ScanMode.Model2_EDSR:
                    if (worker2 != null) DecodeWithModel2();
                    else DecodeNativeFrame();
                    break;

                case ScanMode.Both_Fallback:
                    DecodeWithFallback();
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"QR Decoding Error: {e.Message}");
        }
    }

    private Result ProcessModelInference(Worker worker, RenderTexture lrRT, RenderTexture hrRT, Texture2D readbackTex, int inputRes, int outputRes)
    {
        // 1. Center-crop camera feed to 1:1 aspect ratio
        float aspect = (float)cameraTexture.width / cameraTexture.height;
        Vector2 scale = aspect > 1.0f ? new Vector2(1.0f / aspect, 1.0f) : new Vector2(1.0f, aspect);
        Vector2 offset = aspect > 1.0f ? new Vector2((1.0f - scale.x) * 0.5f, 0.0f) : new Vector2(0.0f, (1.0f - scale.y) * 0.5f);

        Graphics.Blit(cameraTexture, lrRT, scale, offset);

        // 2. Sentis Tensor Conversion & Execution
        var transform = new TextureTransform().SetTensorLayout(TensorLayout.NCHW);

        using (Tensor<float> inputTensor = new Tensor<float>(new TensorShape(1, 3, inputRes, inputRes)))
        {
            TextureConverter.ToTensor(lrRT, inputTensor, transform);
            worker.Schedule(inputTensor);

            using (Tensor<float> outputTensor = worker.PeekOutput() as Tensor<float>)
            {
                TextureConverter.RenderToTexture(outputTensor, hrRT);
            }
        }

        // 3. Read back reconstructed high-res texture
        RenderTexture.active = hrRT;
        readbackTex.ReadPixels(new Rect(0, 0, outputRes, outputRes), 0, 0);
        readbackTex.Apply();
        RenderTexture.active = null;

        Color32[] rawPixels = readbackTex.GetPixels32();

        // 4. Allocate padded canvas with white background (Quiet Zone)
        int padding = 32;
        int paddedRes = outputRes + (padding * 2);
        Color32[] paddedPixels = new Color32[paddedRes * paddedRes];
        Color32 white = new Color32(255, 255, 255, 255);

        for (int i = 0; i < paddedPixels.Length; i++) paddedPixels[i] = white;

        // 5. FLIP MOVEMENT (Y-AXIS INVERSION):
        // Maps bottom rows to top rows during pixel transfer without modifying UI Transforms
        for (int y = 0; y < outputRes; y++)
        {
            // Source row scanned normally from top to bottom
            int srcRow = y * outputRes;

            // Destination row mapped in reverse order (bottom-up)
            int dstY = (outputRes - 1 - y) + padding;
            int dstRow = dstY * paddedRes;

            for (int x = 0; x < outputRes; x++)
            {
                paddedPixels[dstRow + (x + padding)] = rawPixels[srcRow + x];
            }
        }

        // 6. Decode vertically re-ordered pixels with ZXing
        return barcodeReader.Decode(paddedPixels, paddedRes, paddedRes);
    }

    private void DecodeWithModel1()
    {
        Result result = ProcessModelInference(worker1, lrRT1, hrRT1, readbackTex1, model1InputRes, model1OutputRes);
        ProcessScanResult(result);
    }

    private void DecodeWithModel2()
    {
        Result result = ProcessModelInference(worker2, lrRT2, hrRT2, readbackTex2, model2InputRes, model2OutputRes);
        ProcessScanResult(result);
    }

    private void DecodeWithFallback()
    {
        // Try Model 1 First
        Result result = null;
        if (worker1 != null)
        {
            result = ProcessModelInference(worker1, lrRT1, hrRT1, readbackTex1, model1InputRes, model1OutputRes);
        }

        // If Model 1 succeeds, return result immediately
        if (result != null)
        {
            ProcessScanResult(result);
            return;
        }

        // Fallback to Model 2 if Model 1 failed
        if (worker2 != null)
        {
            result = ProcessModelInference(worker2, lrRT2, hrRT2, readbackTex2, model2InputRes, model2OutputRes);
        }

        ProcessScanResult(result);
    }

    private void DecodeNativeFrame()
    {
        Color32[] rawPixels = cameraTexture.GetPixels32();
        int width = cameraTexture.width;
        int height = cameraTexture.height;

        if (rawPixels == null || rawPixels.Length == 0) return;

        Result result = barcodeReader.Decode(rawPixels, width, height);
        ProcessScanResult(result);
    }

    private void ProcessScanResult(Result result)
    {
        if (result != null)
        {
            Debug.Log($"Scanned QR Code ({scanMode}): {result.Text}");
            if (result.Text != resultText.text)
            {
                resultText.text = result.Text;
                StorageItem scannedItem = ItemReader.ParseItemCode(result.Text);
                TransactionManager.Instance.RecordTransaction(scannedItem, TransactionActionType.IN);
                LogManager.Instance.LogEvent(LogCategory.QRScanner_Event, $"Scanned QR Code: {result.Text}");


                productSorting.SortItem(scannedItem);
            }
        }
        else
        {
            Debug.Log($"Not getting any results using mode: {scanMode}");
            LogManager.Instance.LogEvent(LogCategory.QRScanner_Error, $"No QR Code detected using mode: {scanMode}");
        }
    }

    public Sprite ConvertTextureToSprite(Texture2D texture)
    {
        return Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );
    }

    private void CleanupSentis()
    {
        worker1?.Dispose();
        worker2?.Dispose();

        if (lrRT1 != null) RenderTexture.ReleaseTemporary(lrRT1);
        if (hrRT1 != null) RenderTexture.ReleaseTemporary(hrRT1);
        if (readbackTex1 != null) Destroy(readbackTex1);

        if (lrRT2 != null) RenderTexture.ReleaseTemporary(lrRT2);
        if (hrRT2 != null) RenderTexture.ReleaseTemporary(hrRT2);
        if (readbackTex2 != null) Destroy(readbackTex2);
    }
}