using System;
using Unity.InferenceEngine;

using UnityEngine;
using ZXing;

public class QrDatasetTester : MonoBehaviour
{
    [Header("Model Settings")]
    [SerializeField] private ModelAsset modelAsset;
    [SerializeField] private int inputRes = 240;
    [SerializeField] private int outputRes = 480;

    [Header("Test Input")]
    [Tooltip("Drag a low-res dataset image (e.g., from your _LR folder) here to test.")]
    [SerializeField] private Texture2D testLowResImage;

    [Header("UI / Output Preview")]
    [SerializeField] private UnityEngine.UI.RawImage outputPreviewReceiver;

    private Model runtimeModel;
    private Worker engine;
    private RenderTexture lrRT;
    private RenderTexture hrRT;
    private Texture2D readbackTex;
    private BarcodeReader barcodeReader;

    private void Start()
    {
        barcodeReader = new BarcodeReader();

        // Load model and create Sentis worker (using CPU or GPU backend matching your main scanner)
        runtimeModel = ModelLoader.Load(modelAsset);
        engine = new Worker(runtimeModel, BackendType.GPUCompute);

        // Initialize render textures matching your model specifications
        lrRT = new RenderTexture(inputRes, inputRes, 0, RenderTextureFormat.ARGB32);
        lrRT.filterMode = FilterMode.Bilinear;
        lrRT.Create();

        hrRT = new RenderTexture(outputRes, outputRes, 0, RenderTextureFormat.ARGB32);
        hrRT.filterMode = FilterMode.Bilinear;
        hrRT.Create();

        readbackTex = new Texture2D(outputRes, outputRes, TextureFormat.RGB24, false);
    }

    private void OnDestroy()
    {
        engine?.Dispose();

        if (lrRT != null) lrRT.Release();
        if (hrRT != null) hrRT.Release();
    }

    [ContextMenu("Run Test Inference")]
    public void RunTest()
    {
        if (testLowResImage == null)
        {
            Debug.LogError("Please assign a test low-res image in the inspector first!");
            return;
        }

        // 1. Blit the dropped training image into the low-res render texture
        Graphics.Blit(testLowResImage, lrRT);

        // 2. Convert to NCHW tensor and execute inference
        var inputTransform = new TextureTransform().SetTensorLayout(TensorLayout.NCHW);

        using (Tensor<float> inputTensor = new Tensor<float>(new TensorShape(1, 3, inputRes, inputRes)))
        {
            TextureConverter.ToTensor(lrRT, inputTensor, inputTransform);
            engine.Schedule(inputTensor);

            using (Tensor<float> outputTensor = engine.PeekOutput() as Tensor<float>)
            {
                if (outputTensor == null)
                {
                    Debug.LogError("Model output failed or returned non-float tensor.");
                    return;
                }

                var outputTransform = new TextureTransform().SetTensorLayout(TensorLayout.NCHW);
                TextureConverter.RenderToTexture(outputTensor, hrRT, outputTransform);
            }
        }

        // 3. Read back pixels to check the result and attempt decoding
        RenderTexture.active = hrRT;
        readbackTex.ReadPixels(new Rect(0, 0, outputRes, outputRes), 0, 0);
        readbackTex.Apply();
        RenderTexture.active = null;

        if (outputPreviewReceiver != null)
        {
            outputPreviewReceiver.texture = readbackTex;
        }

        // 4. Test ZXing decode on the reconstructed output
        Color32[] rawPixels = readbackTex.GetPixels32();

        // Add safety quiet zone padding for ZXing
        const int padding = 32;
        int paddedRes = outputRes + padding * 2;
        Color32[] paddedPixels = new Color32[paddedRes * paddedRes];
        Color32 white = new Color32(255, 255, 255, 255);

        for (int i = 0; i < paddedPixels.Length; i++) paddedPixels[i] = white;

        for (int y = 0; y < outputRes; y++)
        {
            int srcRow = (outputRes - 1 - y) * outputRes;
            int dstRow = (y + padding) * paddedRes;
            for (int x = 0; x < outputRes; x++)
            {
                paddedPixels[dstRow + x + padding] = rawPixels[srcRow + x];
            }
        }

        var result = barcodeReader.Decode(paddedPixels, paddedRes, paddedRes);
        if (result != null)
        {
            Debug.Log($"<color=green>SUCCESS! Decoded test image text:</color> {result.Text}");
        }
        else
        {
            Debug.LogWarning("Inference ran, but ZXing failed to decode the output image (it may have generated static or blurred lines).");
        }
    }
}