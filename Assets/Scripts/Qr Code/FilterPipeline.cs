using System.IO;
using UnityEditor;
using UnityEngine;
using ZXing;
using ZXing.QrCode;

public class FilterPipeline : EditorWindow
{
    // =========================================================================
    // GLOBAL SERIALIZED FIELDS
    // =========================================================================
    [Header("Preview Settings")]
    [SerializeField] private bool showGizmos = true;

    [Header("Dataset Output")]
    [SerializeField] private int totalImagesToGenerate = 3000;
    [SerializeField] private int highResSize = 480;
    [SerializeField] private int lowResSize = 240;
    [SerializeField] private string outputDirectory = "Assets/QRDataset";

    [Header("GPU Pipeline")]
    [SerializeField] private ComputeShader distortionComputeShader;

    [Header("Variant 1: Fisheye / Lens Barrel")]
    [SerializeField, Range(0f, 1f)] private float barrelChance = 0.3f;
    [SerializeField] private float minBarrelStrength = -0.2f;
    [SerializeField] private float maxBarrelStrength = 0.3f;

    [Header("Variant 2: Perspective Skew")]
    [SerializeField, Range(0f, 1f)] private float perspectiveChance = 0.5f;
    [SerializeField] private float minPerspectiveAngle = -0.25f;
    [SerializeField] private float maxPerspectiveAngle = 0.25f;

    [Header("Variant 3: Paper Wrinkles / Waves")]
    [SerializeField, Range(0f, 1f)] private float waveChance = 0.2f;
    [SerializeField] private float minWaveFrequency = 10f;
    [SerializeField] private float maxWaveFrequency = 30f;
    [SerializeField] private float minWaveAmplitude = 0.002f;
    [SerializeField] private float maxWaveAmplitude = 0.008f;

    [Header("Variant 4: Lens Vignette")]
    [SerializeField] private float minVignette = 0.8f;
    [SerializeField] private float maxVignette = 1.4f;

    [Header("Variant 5: Sensor Noise")]
    [SerializeField] private float minNoise = 0.01f;
    [SerializeField] private float maxNoise = 0.06f;

    [Header("Variant 6: Multi-Spot Lens Defocus Blur")]
    [SerializeField, Range(0f, 1f)] private float blurChance = 0.5f;
    [SerializeField, Range(1, 16)] private int maxBlurSpotCount = 4;
    [SerializeField] private float minBlurRadius = 0.05f;
    [SerializeField] private float maxBlurRadius = 0.3f;
    [SerializeField] private float minBlurStrength = 1.0f;
    [SerializeField] private float maxBlurStrength = 3.0f;

    [Header("Variant 7: Specular Glare / Bright Spot")]
    [SerializeField, Range(0f, 1f)] private float glareChance = 0.3f;
    [SerializeField] private float minGlareIntensity = 0.3f;
    [SerializeField] private float maxGlareIntensity = 0.8f;
    [SerializeField] private float minGlareRadius = 0.1f;
    [SerializeField] private float maxGlareRadius = 0.35f;

    // =========================================================================
    // EDITOR WINDOW PREVIEW STATE
    // =========================================================================
    private Vector2 scrollPosition;
    private Texture2D testSourceTexture;
    private RenderTexture previewRenderTexture;

    // PREVIEW FILTER OVERRIDES
    private float previewBarrel;
    private float previewPerspective;
    private float previewWaveFreq;
    private float previewWaveAmp;
    private float previewVignette = 1.0f;
    private float previewNoise = 0.02f;

    private Vector4[] previewBlurSpots = new Vector4[16];
    private int previewBlurSpotCount = 0;

    private float previewGlareIntensity;
    private float previewGlareRadius = 0.2f;
    private Vector2 previewGlareCenter = new Vector2(0.5f, 0.5f);

    [MenuItem("Tools/Filter Pipeline")]
    public static void ShowWindow()
    {
        GetWindow<FilterPipeline>("Filter Pipeline");
    }

    private void OnEnable()
    {
        GenerateTestPreviewTexture();
        RandomizePreviewFilterValues();
    }

    private void OnDisable()
    {
        if (testSourceTexture != null) DestroyImmediate(testSourceTexture);
        if (previewRenderTexture != null) RenderTexture.ReleaseTemporary(previewRenderTexture);
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // PREVIEW BOX DRAWING (RESPONSIVE FLEXIBLE SPACING)
        DrawPreviewSection();

        EditorGUILayout.Space(15);
        showGizmos = EditorGUILayout.Toggle("Show Handles Overlay", showGizmos);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Dataset Output Settings", EditorStyles.boldLabel);
        totalImagesToGenerate = EditorGUILayout.IntField("Total Images", totalImagesToGenerate);
        highResSize = EditorGUILayout.IntField("High-Res Size (px)", highResSize);
        lowResSize = EditorGUILayout.IntField("Low-Res Size (px)", lowResSize);
        outputDirectory = EditorGUILayout.TextField("Output Directory", outputDirectory);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("GPU Pipeline", EditorStyles.boldLabel);
        distortionComputeShader = (ComputeShader)EditorGUILayout.ObjectField("Compute Shader", distortionComputeShader, typeof(ComputeShader), false);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Variant 1: Fisheye / Lens Barrel", EditorStyles.boldLabel);
        barrelChance = EditorGUILayout.Slider("Barrel Chance", barrelChance, 0f, 1f);
        minBarrelStrength = EditorGUILayout.FloatField("Min Barrel Strength", minBarrelStrength);
        maxBarrelStrength = EditorGUILayout.FloatField("Max Barrel Strength", maxBarrelStrength);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Variant 2: Perspective Skew", EditorStyles.boldLabel);
        perspectiveChance = EditorGUILayout.Slider("Perspective Chance", perspectiveChance, 0f, 1f);
        minPerspectiveAngle = EditorGUILayout.FloatField("Min Perspective Angle", minPerspectiveAngle);
        maxPerspectiveAngle = EditorGUILayout.FloatField("Max Perspective Angle", maxPerspectiveAngle);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Variant 3: Paper Wrinkles / Waves", EditorStyles.boldLabel);
        waveChance = EditorGUILayout.Slider("Wave Chance", waveChance, 0f, 1f);
        minWaveFrequency = EditorGUILayout.FloatField("Min Wave Frequency", minWaveFrequency);
        maxWaveFrequency = EditorGUILayout.FloatField("Max Wave Frequency", maxWaveFrequency);
        minWaveAmplitude = EditorGUILayout.FloatField("Min Wave Amplitude", minWaveAmplitude);
        maxWaveAmplitude = EditorGUILayout.FloatField("Max Wave Amplitude", maxWaveAmplitude);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Variant 4: Lens Vignette", EditorStyles.boldLabel);
        minVignette = EditorGUILayout.FloatField("Min Vignette", minVignette);
        maxVignette = EditorGUILayout.FloatField("Max Vignette", maxVignette);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Variant 5: Sensor Noise", EditorStyles.boldLabel);
        minNoise = EditorGUILayout.FloatField("Min Noise", minNoise);
        maxNoise = EditorGUILayout.FloatField("Max Noise", maxNoise);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Variant 6: Multi-Spot Lens Defocus Blur", EditorStyles.boldLabel);
        blurChance = EditorGUILayout.Slider("Blur Chance", blurChance, 0f, 1f);
        maxBlurSpotCount = EditorGUILayout.IntSlider("Max Blur Spot Count", maxBlurSpotCount, 1, 16);
        minBlurRadius = EditorGUILayout.FloatField("Min Blur Radius", minBlurRadius);
        maxBlurRadius = EditorGUILayout.FloatField("Max Blur Radius", maxBlurRadius);
        minBlurStrength = EditorGUILayout.FloatField("Min Blur Strength", minBlurStrength);
        maxBlurStrength = EditorGUILayout.FloatField("Max Blur Strength", maxBlurStrength);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Variant 7: Specular Glare / Bright Spot", EditorStyles.boldLabel);
        glareChance = EditorGUILayout.Slider("Glare Chance", glareChance, 0f, 1f);
        minGlareIntensity = EditorGUILayout.FloatField("Min Glare Intensity", minGlareIntensity);
        maxGlareIntensity = EditorGUILayout.FloatField("Max Glare Intensity", maxGlareIntensity);
        minGlareRadius = EditorGUILayout.FloatField("Min Glare Radius", minGlareRadius);
        maxGlareRadius = EditorGUILayout.FloatField("Max Glare Radius", maxGlareRadius);

        EditorGUILayout.Space(15);

        if (GUILayout.Button("Generate Dataset", GUILayout.Height(35)))
        {
            if (distortionComputeShader == null)
            {
                Debug.LogError("Assign the Compute Shader before generating!");
                return;
            }
            GenerateDataset();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawPreviewSection()
    {
        EditorGUILayout.LabelField("Live GPU Distortion Preview", EditorStyles.boldLabel);

        if (testSourceTexture == null)
        {
            GenerateTestPreviewTexture();
        }

        // Dynamic Sizing based on Window Width
        float currentWindowWidth = position.width - 20f; // Padding margin
        float previewSize = Mathf.Clamp(currentWindowWidth * 0.38f, 140f, 260f);
        float spacing = Mathf.Max((currentWindowWidth - (previewSize * 2f)) / 3f, 10f);

        Rect rawRect = GUILayoutUtility.GetRect(position.width, previewSize + 22f);

        Rect originalRect = new Rect(spacing, rawRect.y + 20f, previewSize, previewSize);
        Rect distortedRect = new Rect(spacing * 2f + previewSize, rawRect.y + 20f, previewSize, previewSize);

        // Header Labels
        GUI.Label(new Rect(originalRect.x, rawRect.y, previewSize, 20f), "Original (Clean)", EditorStyles.centeredGreyMiniLabel);
        GUI.Label(new Rect(distortedRect.x, rawRect.y, previewSize, 20f), "Distorted (GPU)", EditorStyles.centeredGreyMiniLabel);

        // Render Original Clean Texture
        if (testSourceTexture != null)
        {
            GUI.DrawTexture(originalRect, testSourceTexture, ScaleMode.ScaleToFit);
        }

        // Render Distorted GPU Texture
        if (distortionComputeShader != null && testSourceTexture != null)
        {
            int kernel = distortionComputeShader.FindKernel("ApplyDistortions");

            if (previewRenderTexture != null) RenderTexture.ReleaseTemporary(previewRenderTexture);

            previewRenderTexture = ApplyDistortionsInternal(
                testSourceTexture,
                kernel,
                previewBarrel,
                previewPerspective,
                previewWaveFreq,
                previewWaveAmp,
                previewVignette,
                previewNoise,
                previewBlurSpotCount,
                previewBlurSpots,
                previewGlareIntensity,
                previewGlareCenter,
                previewGlareRadius
            );

            GUI.DrawTexture(distortedRect, previewRenderTexture, ScaleMode.ScaleToFit);

            // Interactive Drag Glare Target on Distorted Image
            Event e = Event.current;
            if (distortedRect.Contains(e.mousePosition) && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag))
            {
                float relativeX = (e.mousePosition.x - distortedRect.x) / distortedRect.width;
                float relativeY = 1.0f - ((e.mousePosition.y - distortedRect.y) / distortedRect.height);
                previewGlareCenter = new Vector2(Mathf.Clamp01(relativeX), Mathf.Clamp01(relativeY));
                Repaint();
            }

            // Draw Overlay Handles over the Distorted Image
            if (showGizmos)
            {
                if (previewGlareIntensity > 0.01f)
                {
                    Vector2 glareCrosshair = new Vector2(
                        distortedRect.x + (previewGlareCenter.x * distortedRect.width),
                        distortedRect.y + ((1.0f - previewGlareCenter.y) * distortedRect.height)
                    );
                    Handles.color = Color.yellow;
                    Handles.DrawWireDisc(glareCrosshair, Vector3.forward, 8f);
                }

                Handles.color = new Color(0.2f, 0.8f, 1.0f, 0.8f);
                for (int i = 0; i < previewBlurSpotCount; i++)
                {
                    Vector2 spotPos = new Vector2(
                        distortedRect.x + (previewBlurSpots[i].x * distortedRect.width),
                        distortedRect.y + ((1.0f - previewBlurSpots[i].y) * distortedRect.height)
                    );
                    float radiusInPixels = previewBlurSpots[i].z * distortedRect.width;
                    Handles.DrawWireDisc(spotPos, Vector3.forward, radiusInPixels);
                }
            }
        }
        else
        {
            EditorGUI.DrawRect(distortedRect, Color.black);
            GUI.Label(distortedRect, "Assign Compute Shader\nto view preview", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Randomize QR Code", GUILayout.Height(25)))
        {
            GenerateTestPreviewTexture();
            Repaint();
        }
        if (GUILayout.Button("Randomize Filters", GUILayout.Height(25)))
        {
            RandomizePreviewFilterValues();
            Repaint();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void RandomizePreviewFilterValues()
    {
        previewBarrel = (Random.value < barrelChance) ? Random.Range(minBarrelStrength, maxBarrelStrength) : 0f;
        previewPerspective = (Random.value < perspectiveChance) ? Random.Range(minPerspectiveAngle, maxPerspectiveAngle) : 0f;

        bool applyWave = Random.value < waveChance;
        previewWaveFreq = applyWave ? Random.Range(minWaveFrequency, maxWaveFrequency) : 0f;
        previewWaveAmp = applyWave ? Random.Range(minWaveAmplitude, maxWaveAmplitude) : 0f;

        previewVignette = Random.Range(minVignette, maxVignette);
        previewNoise = Random.Range(minNoise, maxNoise);

        bool applyBlur = (blurChance > 0f) && (Random.value < blurChance);
        previewBlurSpotCount = applyBlur ? Random.Range(1, maxBlurSpotCount + 1) : 0;

        for (int i = 0; i < previewBlurSpotCount; i++)
        {
            float px = Random.Range(0.15f, 0.85f);
            float py = Random.Range(0.15f, 0.85f);
            float radius = Random.Range(minBlurRadius, maxBlurRadius);
            float strength = Random.Range(minBlurStrength, maxBlurStrength);
            previewBlurSpots[i] = new Vector4(px, py, radius, strength);
        }

        bool applyGlare = (glareChance > 0f) && (Random.value < glareChance);
        previewGlareIntensity = applyGlare ? Random.Range(minGlareIntensity, maxGlareIntensity) : 0f;
        previewGlareRadius = applyGlare ? Random.Range(minGlareRadius, maxGlareRadius) : 0f;
        previewGlareCenter = new Vector2(Random.Range(0.2f, 0.8f), Random.Range(0.2f, 0.8f));
    }

    private void GenerateTestPreviewTexture()
    {
        if (testSourceTexture != null) DestroyImmediate(testSourceTexture);

        BarcodeWriter writer = new()
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions { Height = 256, Width = 256, Margin = 2 }
        };

        string randomPayload = System.Guid.NewGuid().ToString().Substring(0, Random.Range(8, 20));
        var bitMatrix = writer.Encode(randomPayload);
        int width = bitMatrix.Width;
        int height = bitMatrix.Height;

        testSourceTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isBlack = bitMatrix[x, height - 1 - y];
                pixels[y * width + x] = isBlack ? new Color32(0, 0, 0, 255) : new Color32(255, 255, 255, 255);
            }
        }

        testSourceTexture.SetPixels32(pixels);
        testSourceTexture.Apply();
    }

    private void GenerateDataset()
    {
        string hrPath = Path.Combine(outputDirectory, "HighRes");
        string lrPath = Path.Combine(outputDirectory, "LowRes");

        if (Directory.Exists(hrPath)) Directory.Delete(hrPath, true);
        if (Directory.Exists(lrPath)) Directory.Delete(lrPath, true);

        Directory.CreateDirectory(hrPath);
        Directory.CreateDirectory(lrPath);

        BarcodeWriter writer = new()
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions { Height = highResSize, Width = highResSize, Margin = 2 }
        };

        int kernel = distortionComputeShader.FindKernel("ApplyDistortions");

        for (int i = 0; i < totalImagesToGenerate; i++)
        {
            string payload = System.Guid.NewGuid().ToString().Substring(0, Random.Range(8, 24));

            var bitMatrix = writer.Encode(payload);
            int width = bitMatrix.Width;
            int height = bitMatrix.Height;

            Texture2D hrTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isBlack = bitMatrix[x, height - 1 - y];
                    pixels[y * width + x] = isBlack ? new Color32(0, 0, 0, 255) : new Color32(255, 255, 255, 255);
                }
            }

            hrTexture.SetPixels32(pixels);
            hrTexture.Apply();

            float barrel = (Random.value < barrelChance) ? Random.Range(minBarrelStrength, maxBarrelStrength) : 0f;
            float perspective = (Random.value < perspectiveChance) ? Random.Range(minPerspectiveAngle, maxPerspectiveAngle) : 0f;

            bool applyWave = Random.value < waveChance;
            float waveFreq = applyWave ? Random.Range(minWaveFrequency, maxWaveFrequency) : 0f;
            float waveAmp = applyWave ? Random.Range(minWaveAmplitude, maxWaveAmplitude) : 0f;

            float vignette = Random.Range(minVignette, maxVignette);
            float noise = Random.Range(minNoise, maxNoise);

            bool applyBlur = Random.value < blurChance;
            int spotCount = applyBlur ? Random.Range(1, maxBlurSpotCount + 1) : 0;
            Vector4[] blurSpots = new Vector4[16];
            for (int s = 0; s < spotCount; s++)
            {
                blurSpots[s] = new Vector4(
                    Random.Range(0.1f, 0.9f),
                    Random.Range(0.1f, 0.9f),
                    Random.Range(minBlurRadius, maxBlurRadius),
                    Random.Range(minBlurStrength, maxBlurStrength)
                );
            }

            bool applyGlare = Random.value < glareChance;
            float glareIntensity = applyGlare ? Random.Range(minGlareIntensity, maxGlareIntensity) : 0f;
            float glareRadius = applyGlare ? Random.Range(minGlareRadius, maxGlareRadius) : 0f;
            Vector2 glareCenter = new Vector2(Random.Range(0.2f, 0.8f), Random.Range(0.2f, 0.8f));

            RenderTexture distortedRT = ApplyDistortionsInternal(
                hrTexture, kernel, barrel, perspective, waveFreq, waveAmp, vignette, noise, spotCount, blurSpots, glareIntensity, glareCenter, glareRadius
            );

            Texture2D lrTexture = DownsampleToTexture2D(distortedRT, lowResSize);

            SaveTextureToPNG(hrTexture, Path.Combine(hrPath, $"qr_{i:D5}_HR.png"));
            SaveTextureToPNG(lrTexture, Path.Combine(lrPath, $"qr_{i:D5}_LR.png"));

            DestroyImmediate(hrTexture);
            DestroyImmediate(lrTexture);
            RenderTexture.ReleaseTemporary(distortedRT);
        }

        AssetDatabase.Refresh();
        Debug.Log("Dataset generation complete!");
    }

    private RenderTexture ApplyDistortionsInternal(
        Texture sourceTexture, int kernel,
        float barrel, float perspective, float waveFreq, float waveAmp,
        float vignette, float noise, int blurSpotCount, Vector4[] blurSpots,
        float glareIntensity, Vector2 glareCenter, float glareRadius)
    {
        int w = sourceTexture.width;
        int h = sourceTexture.height;

        RenderTexture result = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
        result.enableRandomWrite = true;
        result.Create();

        distortionComputeShader.SetTexture(kernel, "InputTexture", sourceTexture);
        distortionComputeShader.SetTexture(kernel, "OutputTexture", result);
        distortionComputeShader.SetVector("Resolution", new Vector2(w, h));

        distortionComputeShader.SetFloat("BarrelStrength", barrel);
        distortionComputeShader.SetFloat("PerspectiveAngle", perspective);
        distortionComputeShader.SetFloat("WaveFrequency", waveFreq);
        distortionComputeShader.SetFloat("WaveAmplitude", waveAmp);
        distortionComputeShader.SetFloat("VignetteStrength", vignette);
        distortionComputeShader.SetFloat("NoiseIntensity", noise);

        distortionComputeShader.SetInt("BlurSpotCount", blurSpotCount);
        if (blurSpots != null && blurSpots.Length > 0)
        {
            distortionComputeShader.SetVectorArray("BlurSpots", blurSpots);
        }

        distortionComputeShader.SetFloat("GlareIntensity", glareIntensity);
        distortionComputeShader.SetVector("GlareCenter", glareCenter);
        distortionComputeShader.SetFloat("GlareRadius", glareRadius);

        int threadGroupsX = Mathf.CeilToInt(w / 8f);
        int threadGroupsY = Mathf.CeilToInt(h / 8f);
        distortionComputeShader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);

        return result;
    }

    private Texture2D DownsampleToTexture2D(RenderTexture source, int targetSize)
    {
        RenderTexture temp = RenderTexture.GetTemporary(targetSize, targetSize, 0, RenderTextureFormat.ARGB32);
        temp.filterMode = FilterMode.Bilinear;

        Graphics.Blit(source, temp);

        RenderTexture.active = temp;
        Texture2D result = new Texture2D(targetSize, targetSize, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, targetSize, targetSize), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(temp);

        return result;
    }

    private void SaveTextureToPNG(Texture2D texture, string path)
    {
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
    }
}