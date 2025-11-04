using UnityEditor;
using UnityEngine;
using System.IO;

public class MarsAutoTerrainGenerator
{
    [MenuItem("Mars/Generate Mars Dataset")]
    public static void GenerateMarsDataset()
    {
        // 📸 CONFIGURATION -----------------------------------
        int numScenes = 1000;                // ← Number of samples to generate (up to 10000)
        bool enableTopView = false;          // ← Whether to also render top-down view
        string baseOutputDir = "Assets/TerrainLayerImages";
        // -----------------------------------------------------

        string imagesDir = Path.Combine(baseOutputDir, "Images");
        string labelsDir = Path.Combine(baseOutputDir, "Labels");
        string topviewsDir = Path.Combine(baseOutputDir, "Topviews");
        Directory.CreateDirectory(imagesDir);
        Directory.CreateDirectory(labelsDir);
        Directory.CreateDirectory(topviewsDir);

        for (int i = 0; i < numScenes; i++)
        {
            Debug.Log($"🧩 Generating terrain {i + 1}/{numScenes}...");
            Generate3DTerrain(i, enableTopView, imagesDir, labelsDir, topviewsDir);
        }

        Debug.Log($"✅ Finished generating {numScenes} Mars terrain samples!");
    }

    private static void Generate3DTerrain(int index, bool enableTopView, string imagesDir, string labelsDir, string topviewsDir)
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);

        // ⚙️ Terrain parameters
        float scale = Random.Range(0.01f, 0.03f);
        float peakIntensity = Random.Range(0.3f, 0.6f);
        float cliffSharpness = Random.Range(2f, 5f);
        float riverNoise = Random.Range(0.1f, 0.3f);

        TerrainData terrainData = new TerrainData
        {
            heightmapResolution = 256,
            size = new Vector3(500, 120, 500)
        };

        int width = terrainData.heightmapResolution;
        int height = terrainData.heightmapResolution;
        float[,] heights = new float[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = x * scale;
                float ny = y * scale;
                float baseHeight = Mathf.PerlinNoise(nx, ny);
                float peak = Mathf.Pow(baseHeight, cliffSharpness) * peakIntensity;
                float riverValley = Mathf.Sin((float)x / width * Mathf.PI * 3f + Random.value) * riverNoise;
                riverValley += Mathf.PerlinNoise(nx * 0.5f, ny * 0.5f) * 0.05f;
                heights[y, x] = Mathf.Clamp01(peak - riverValley);
            }
        }
        terrainData.SetHeights(0, 0, heights);

        GameObject terrainObj = Terrain.CreateTerrainGameObject(terrainData);
        terrainObj.name = "MarsTerrain";

        // 🎨 Textures
        string basePath = "Assets/Yoge/Textures/Stylized - Nature/";
        TerrainLayer soil1 = CreateTextureLayer(basePath, "Texture_2_Diffuse.png", "Texture_2_Normal.png", 10);
        TerrainLayer soil2 = CreateTextureLayer(basePath, "Texture_4_Diffuse.png", "Texture_4_Normal.png", 8);
        TerrainLayer bedrock = CreateTextureLayer(basePath, "Texture_5_Diffuse.png", "Texture_5_Normal.png", 6);
        TerrainLayer sand = CreateTextureLayer(basePath, "Texture_7_Diffuse.png", "Texture_7_Normal.png", 12);
        TerrainLayer bigRock = CreateTextureLayer(basePath, "Texture_9_Diffuse.png", "Texture_9_Normal.png", 5);
        terrainData.terrainLayers = new TerrainLayer[] { soil1, soil2, bedrock, sand, bigRock };

        int alphaRes = terrainData.alphamapResolution;
        float[,,] alphaMaps = new float[alphaRes, alphaRes, 5];
        for (int y = 0; y < alphaRes; y++)
        {
            for (int x = 0; x < alphaRes; x++)
            {
                float r = Random.value;
                if (r < 0.4f) alphaMaps[y, x, 0] = 1f; // soil
                else if (r < 0.65f) alphaMaps[y, x, 2] = 1f; // bedrock
                else if (r < 0.85f) alphaMaps[y, x, 3] = 1f; // sand
                else alphaMaps[y, x, 4] = 1f; // bigrock
            }
        }
        terrainData.SetAlphamaps(0, 0, alphaMaps);

        // ☀️ Sunlight
        GameObject lightObj = new GameObject("SunLight");
        Light sun = lightObj.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.3f;
        sun.color = new Color(1f, 0.85f, 0.65f);
        sun.transform.rotation = Quaternion.Euler(50f, 30f, 0f);

        // 🎥 Main camera
        GameObject camObj = new GameObject("CloseFramedCamera");
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 55f;
        cam.transform.position = new Vector3(terrainData.size.x / 2f, 85f, terrainData.size.z / 2f - 120f);
        cam.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 400f;

        // 📸 Render images
        float distanceThreshold = 120f;
        string padded = index.ToString("D5");  // e.g. 00003
        RenderAndSave(cam, imagesDir, $"MarsTerrain_{padded}");
        RenderAndSaveMask(cam, terrainData, labelsDir, $"MarsTerrain_MASK_{padded}", distanceThreshold);

        // 🛰️ Optional top view
        if (enableTopView)
        {
            GameObject topCamObj = new GameObject("TopDownCamera");
            Camera topCam = topCamObj.AddComponent<Camera>();
            topCam.orthographic = true;
            topCam.orthographicSize = terrainData.size.x / 2f;
            topCam.aspect = 1f;
            topCam.transform.position = new Vector3(terrainData.size.x / 2f, 600f, terrainData.size.z / 2f);
            topCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            topCam.clearFlags = CameraClearFlags.Skybox;
            topCam.farClipPlane = 1000f;
            RenderAndSave(topCam, topviewsDir, $"TopViewMarsTerrain_{padded}");
            Object.DestroyImmediate(topCamObj);
        }
    }

    private static TerrainLayer CreateTextureLayer(string basePath, string diffuseName, string normalName, float tileSize)
    {
        TerrainLayer layer = new TerrainLayer();
        layer.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + diffuseName);
        layer.normalMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + normalName);
        layer.tileSize = new Vector2(tileSize, tileSize);
        layer.name = Path.GetFileNameWithoutExtension(diffuseName);
        return layer;
    }

    private static void RenderAndSave(Camera cam, string dir, string name)
    {
        int res = 2048;
        RenderTexture rt = new RenderTexture(res, res, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, res, res), 0, 0);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(dir, $"{name}.png"), tex.EncodeToPNG());
        RenderTexture.active = null;
        cam.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
    }

    private static void RenderAndSaveMask(Camera cam, TerrainData terrainData, string dir, string name, float distanceThreshold)
    {
        int res = 1024;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGB24, false);
        Color soilColor = Color.red;
        Color bedrockColor = Color.green;
        Color sandColor = Color.yellow;
        Color bigrockColor = Color.gray;
        Color skyColor = Color.blue;

        float[,,] alphamaps = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);
        float terrainWidth = terrainData.size.x;
        float terrainLength = terrainData.size.z;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                Ray ray = cam.ScreenPointToRay(new Vector3(x / (float)res * cam.pixelWidth, y / (float)res * cam.pixelHeight, 0f));
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    float dist = hit.distance;
                    if (dist > distanceThreshold)
                    {
                        tex.SetPixel(x, y, skyColor);
                        continue;
                    }

                    Terrain terrain = hit.collider.GetComponent<Terrain>();
                    if (terrain != null)
                    {
                        Vector3 terrainPos = hit.point - terrain.transform.position;
                        float normX = terrainPos.x / terrainWidth;
                        float normZ = terrainPos.z / terrainLength;
                        int mapX = Mathf.Clamp((int)(normX * terrainData.alphamapWidth), 0, terrainData.alphamapWidth - 1);
                        int mapZ = Mathf.Clamp((int)(normZ * terrainData.alphamapHeight), 0, terrainData.alphamapHeight - 1);
                        int dominant = 0;
                        float maxVal = 0f;
                        for (int i = 0; i < 5; i++)
                        {
                            if (alphamaps[mapZ, mapX, i] > maxVal)
                            {
                                maxVal = alphamaps[mapZ, mapX, i];
                                dominant = i;
                            }
                        }

                        Color c = dominant switch
                        {
                            0 => soilColor,
                            1 => soilColor,
                            2 => bedrockColor,
                            3 => sandColor,
                            4 => bigrockColor,
                            _ => Color.magenta
                        };
                        tex.SetPixel(x, y, c);
                    }
                }
                else tex.SetPixel(x, y, skyColor);
            }
        }

        tex.Apply();
        File.WriteAllBytes(Path.Combine(dir, $"{name}.png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }
}