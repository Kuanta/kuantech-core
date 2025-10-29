#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class PrefabSnapshotStudioWindow : EditorWindow
{
    // ----- UI State -----
    GameObject prefab;
    int width = 1024, height = 1024;
    bool transparentBG = true;
    Color backgroundColor = new Color(0,0,0,0); // alfa 0

    // Camera
    bool useOrthographic = false;
    float fieldOfView = 30f;
    float orthoSize = 1.0f;
    Vector2 orbitAngles = new Vector2(20f, 0f); // pitch (x), yaw (y)
    float distancePadding = 1.15f; // kadraj tampon / zoom-out
    private float radiusScalar = 1.0f;
    private Vector3 cameraOffset;
    private Vector3 cameraLookOffset;

    // Lighting
    bool addKeyLight = true;
    Color keyLightColor = Color.white;
    float keyLightIntensity = 1.2f;
    Vector3 keyLightEuler = new Vector3(50f, -30f, 0f);

    bool addFillLight = true;
    Color fillLightColor = new Color(0.85f,0.9f,1f,1f);
    float fillLightIntensity = 0.6f;
    Vector3 fillLightEuler = new Vector3(20f, 160f, 0f);

    // Output
    // Output
    string defaultFileName = "snapshot.png";

// Persisted last save
    const string PREF_LAST_DIR  = "KT_PrefabShot_LastDir";
    const string PREF_LAST_FILE = "KT_PrefabShot_LastFile";

    string lastDir;   // EditorPrefs’ten okunacak
    string lastFile;  // EditorPrefs’ten okunacak

    bool enableQuickSave = true; // UI’dan aç/kapa

    [MenuItem("Kuantech/Prefab Snapshot Studio")]
    static void Open() => GetWindow<PrefabSnapshotStudioWindow>("Prefab Snapshot Studio");

    void OnEnable()
    {
        lastDir  = EditorPrefs.GetString(PREF_LAST_DIR, Application.dataPath);
        lastFile = EditorPrefs.GetString(PREF_LAST_FILE, defaultFileName);
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("Source Prefab", EditorStyles.boldLabel);
        prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            width = EditorGUILayout.IntField("Width", width);
            height = EditorGUILayout.IntField("Height", height);
        }
        width = Mathf.Max(8, width);
        height = Mathf.Max(8, height);

        transparentBG = EditorGUILayout.Toggle("Transparent Background", transparentBG);
        EditorGUI.BeginDisabledGroup(transparentBG);
        backgroundColor = EditorGUILayout.ColorField("Background Color", backgroundColor);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Camera", EditorStyles.boldLabel);
        useOrthographic = EditorGUILayout.Toggle("Orthographic", useOrthographic);
        if (useOrthographic)
            orthoSize = EditorGUILayout.Slider("Ortho Size", orthoSize, 0.01f, 10f);
        else
            fieldOfView = EditorGUILayout.Slider("Field of View", fieldOfView, 5f, 80f);

        orbitAngles.x = EditorGUILayout.Slider("Pitch (X)", orbitAngles.x, -80f, 80f);
        orbitAngles.y = EditorGUILayout.Slider("Yaw (Y)", orbitAngles.y, -180f, 180f);
        cameraOffset = EditorGUILayout.Vector3Field("Camera Offset", cameraOffset);
        cameraLookOffset = EditorGUILayout.Vector3Field("Camera Look Offset", cameraLookOffset);
        radiusScalar = EditorGUILayout.FloatField("RadiusScalar", radiusScalar);
        distancePadding = EditorGUILayout.Slider("Frame Padding", distancePadding, 1.0f, 2.0f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Lighting", EditorStyles.boldLabel);
        addKeyLight = EditorGUILayout.Toggle("Add Key Light", addKeyLight);
        if (addKeyLight)
        {
            keyLightColor = EditorGUILayout.ColorField("Key Color", keyLightColor);
            keyLightIntensity = EditorGUILayout.Slider("Key Intensity", keyLightIntensity, 0f, 5f);
            keyLightEuler = EditorGUILayout.Vector3Field("Key Rotation", keyLightEuler);
        }
        addFillLight = EditorGUILayout.Toggle("Add Fill Light", addFillLight);
        if (addFillLight)
        {
            fillLightColor = EditorGUILayout.ColorField("Fill Color", fillLightColor);
            fillLightIntensity = EditorGUILayout.Slider("Fill Intensity", fillLightIntensity, 0f, 5f);
            fillLightEuler = EditorGUILayout.Vector3Field("Fill Rotation", fillLightEuler);
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Capture Snapshot..."))
            Capture();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Save Options", EditorStyles.boldLabel);
        enableQuickSave = EditorGUILayout.Toggle("Enable Quick Save", enableQuickSave);
        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(lastDir) || string.IsNullOrEmpty(lastFile));
        if (GUILayout.Button($"Quick Capture (overwrite): {lastFile}"))
        {
            Capture(quickSave:true);
        }
        EditorGUI.EndDisabledGroup();
    }

    void Capture(bool quickSave = false)
    {
        if (!prefab)
        {
            EditorUtility.DisplayDialog("Prefab Snapshot", "Lütfen Project penceresinden bir prefab seçin.", "OK");
            return;
        }

        // Instantiate temp objects in a hidden preview stage
        var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        root.hideFlags = HideFlags.HideAndDontSave;

        // Calculate bounds
        var b = GetHierarchyBounds(root);
        var center = b.center;
        var radius = b.extents.magnitude;
        if (radius < 0.001f) radius = 0.1f;

        // Lights
        Light key = null, fill = null;
        if (addKeyLight)
        {
            key = new GameObject("~KeyLight").AddComponent<Light>();
            key.hideFlags = HideFlags.HideAndDontSave;
            key.type = LightType.Directional;
            key.color = keyLightColor;
            key.intensity = keyLightIntensity;
            key.transform.rotation = Quaternion.Euler(keyLightEuler);
        }
        if (addFillLight)
        {
            fill = new GameObject("~FillLight").AddComponent<Light>();
            fill.hideFlags = HideFlags.HideAndDontSave;
            fill.type = LightType.Directional;
            fill.color = fillLightColor;
            fill.intensity = fillLightIntensity;
            fill.transform.rotation = Quaternion.Euler(fillLightEuler);
        }

        // Camera
        var camGO = new GameObject("~SnapshotCam");
        camGO.hideFlags = HideFlags.HideAndDontSave;
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = transparentBG ? new Color(0,0,0,0) : backgroundColor;
        cam.orthographic = useOrthographic;
        if (useOrthographic) cam.orthographicSize = orthoSize;
        else cam.fieldOfView = fieldOfView;
        cam.nearClipPlane = 0.001f;
        cam.farClipPlane = 1000f;
        cam.allowHDR = false; // alfa için güvenli tarafta kal

        // Position camera with orbit + distance that frames bounds
        // Calculate distance to fit bounds vertically (perspective) or via ortho size
        float dist;
        if (useOrthographic)
        {
            // Ortho: sadece konum önemli; default orthoSize kadrajı belirler
            dist = radius * 3f * radiusScalar;
        }
        else
        {
            float fovRad = cam.fieldOfView * Mathf.Deg2Rad;
            dist = (radius * radiusScalar / Mathf.Sin(Mathf.Max(0.001f, fovRad * 0.5f))) * distancePadding;
        }

        // Orbit rotation
        var rot = Quaternion.Euler(orbitAngles.x, orbitAngles.y, 0f);
        cam.transform.position = center + rot * (Vector3.forward * dist) + cameraOffset;
        cam.transform.LookAt(center+cameraLookOffset);

        // RenderTexture (alfa destekli, MSAA kapalı)
        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 1,
            useMipMap = false,
            autoGenerateMips = false,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var prevActive = RenderTexture.active;
        var prevTarget = cam.targetTexture;

        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;

        // Readback with alpha
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply(false, false);

        // Save
        var prefabName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(prefab));
        var suggestedName = string.IsNullOrEmpty(prefabName) ? defaultFileName : (prefabName + "_shot.png");

// Hedef yol
        string path = null;

        if (quickSave && !string.IsNullOrEmpty(lastDir) && !string.IsNullOrEmpty(lastFile))
        {
            // Diyalog açmadan son dosyanın üstüne yaz
            path = Path.Combine(lastDir, lastFile);
        }
        else
        {
            // Dialog’u en son klasör ve dosya adıyla aç
            var initialDir = string.IsNullOrEmpty(lastDir) ? Application.dataPath : lastDir;
            var initialName = string.IsNullOrEmpty(lastFile) ? suggestedName : lastFile;

            path = EditorUtility.SaveFilePanel("Save PNG", initialDir, initialName, "png");
        }

// Kaydet
        if (!string.IsNullOrEmpty(path))
        {
            var png = tex.EncodeToPNG();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, png);
            Debug.Log("Saved: " + path);

            // Son klasör ve dosya adını güncelle + kalıcı yaz
            lastDir = Path.GetDirectoryName(path);
            lastFile = Path.GetFileName(path);
            EditorPrefs.SetString(PREF_LAST_DIR, lastDir);
            EditorPrefs.SetString(PREF_LAST_FILE, lastFile);

            // Eğer Assets altına kaydedildiyse, Project’i yenile
            if (path.Replace('\\','/').StartsWith(Application.dataPath.Replace('\\','/')))
                AssetDatabase.Refresh();
        }

        // Cleanup
        cam.targetTexture = prevTarget;
        RenderTexture.active = prevActive;
        rt.Release();
        DestroyImmediate(rt);
        DestroyImmediate(tex);
        if (key) DestroyImmediate(key.gameObject);
        if (fill) DestroyImmediate(fill.gameObject);
        DestroyImmediate(camGO);
        DestroyImmediate(root);
        AssetDatabase.Refresh();
    }

    static Bounds GetHierarchyBounds(GameObject root)
    {
        var rends = root.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return new Bounds(root.transform.position, Vector3.one * 0.1f);
        var b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        return b;
    }
}
#endif
