using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;

// Editor-only helper. Run it from the menu: RUGA > Setup > Milestone N.
// It repeats the manual setup clicks, and is safe to run more than once.
public static class RugaProjectSetup
{
    const string MobileRendererPath = "Assets/Settings/Mobile_Renderer.asset";
    const string ARScenePath = "Assets/Scenes/ARWorkspace.unity";
    const string MaterialsFolder = "Assets/Materials";
    const string PrefabsFolder = "Assets/Prefabs";
    const string TitleFontPath = "Assets/UI/Fonts/CopperplateCC-Bold.ttf";
    const string IconFontPath = "Assets/UI/Icons/Phosphor.ttf";
    const string BodyFontPath = "Assets/UI/Fonts/Alegreya.ttf";
    const string AppIconPath = "Assets/UI/AppIcon/ruga_icon.png";
    const string AppIconBackgroundPath = "Assets/UI/AppIcon/ruga_icon_background.png";
    const string SplashLogoPath = "Assets/UI/AppIcon/ruga_splash.png";

    // RUGA accent, used for detected surfaces in the AR view.
    static readonly Color Accent = new Color32(0xc4, 0x64, 0x3f, 0xff); // terracotta

    [MenuItem("RUGA/Setup/Milestone 1")]
    static void SetupMilestone1()
    {
        SetPortrait();
        AddARBackgroundFeature();
        CreateARScene();
        AssetDatabase.SaveAssets();
        Debug.Log("RUGA: Milestone 1 setup finished.");
    }

    static void SetPortrait()
    {
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        Debug.Log("RUGA: Default orientation set to Portrait.");
    }

    // Same steps as clicking "Add Renderer Feature" in the URP renderer Inspector.
    static void AddARBackgroundFeature()
    {
        var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(MobileRendererPath);
        if (rendererData == null)
        {
            Debug.LogError($"RUGA: Could not find {MobileRendererPath}.");
            return;
        }

        foreach (var existing in rendererData.rendererFeatures)
        {
            if (existing is ARBackgroundRendererFeature)
            {
                Debug.Log("RUGA: AR Background Renderer Feature already on Mobile_Renderer.");
                return;
            }
        }

        var feature = ScriptableObject.CreateInstance<ARBackgroundRendererFeature>();
        feature.name = nameof(ARBackgroundRendererFeature);
        AssetDatabase.AddObjectToAsset(feature, rendererData);
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);

        var so = new SerializedObject(rendererData);
        var features = so.FindProperty("m_RendererFeatures");
        var featureMap = so.FindProperty("m_RendererFeatureMap");
        features.arraySize++;
        features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
        featureMap.arraySize++;
        featureMap.GetArrayElementAtIndex(featureMap.arraySize - 1).longValue = localId;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(rendererData);
        Debug.Log("RUGA: Added AR Background Renderer Feature to Mobile_Renderer.");
    }

    static void CreateARScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ARScenePath) != null &&
            !EditorUtility.DisplayDialog("RUGA setup",
                $"{ARScenePath} already exists. Replace it with a fresh AR scene?", "Replace", "Keep it"))
        {
            SetBuildScenes();
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var light = new GameObject("Directional Light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Use AR Foundation's own menu items so the objects match a manual setup.
        CreateFromMenu("GameObject/XR/AR Session");
        CreateFromMenu("GameObject/XR/XR Origin (Mobile AR)");

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ARScenePath);
        SetBuildScenes();
        Debug.Log($"RUGA: Created {ARScenePath} with AR Session and XR Origin.");
    }

    static void CreateFromMenu(string menuPath)
    {
        Selection.activeGameObject = null;
        if (!EditorApplication.ExecuteMenuItem(menuPath))
        {
            Debug.LogError($"RUGA: Menu item '{menuPath}' not found. Is AR Foundation installed?");
            return;
        }

        var created = Selection.activeGameObject;
        if (created != null)
        {
            created.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }

    static void SetBuildScenes()
    {
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ARScenePath, true) };
        Debug.Log("RUGA: Build scene list set to ARWorkspace only.");
    }

    // ---------- Full app (Milestones 2-7): AR managers, guide, data, sessions and UI ----------

    [MenuItem("RUGA/Setup/Full App")]
    internal static void SetupFullApp()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ARScenePath) == null)
        {
            Debug.LogError("RUGA: ARWorkspace scene not found. Run RUGA > Setup > Milestone 1 first.");
            return;
        }

        // The UI uses TextMeshPro's default font until custom fonts are assigned on the UIManager.
        if (!AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
        {
            EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
            Debug.Log("RUGA: Importing TextMeshPro Essential Resources. Click Import if a window asks.");
        }

        EnsureFolder(PrefabsFolder);
        var planeFill = CreateUnlitMaterial($"{MaterialsFolder}/PlaneFill.mat", new Color(Accent.r, Accent.g, Accent.b, 0.15f), true);
        var planeEdge = CreateUnlitMaterial($"{MaterialsFolder}/PlaneEdge.mat", Accent, false);
        // Guide materials are white; the app tints them and sets their opacity.
        var guideArt = CreateUnlitMaterial($"{MaterialsFolder}/GuideArt.mat", new Color(1f, 1f, 1f, 0.5f), true);
        var guideGrid = CreateUnlitMaterial($"{MaterialsFolder}/GuideGrid.mat", new Color(1f, 1f, 1f, 0.6f), true);
        guideGrid.renderQueue = (int)RenderQueue.Transparent + 1; // grid draws over the artwork
        EditorUtility.SetDirty(guideGrid);

        var planePrefab = CreatePlanePrefab(planeFill, planeEdge);
        var guidePrefab = CreateGuidePrefab(guideArt, guideGrid);
        DeleteIfExists($"{PrefabsFolder}/GuideTest.prefab");
        DeleteIfExists($"{MaterialsFolder}/GuideTest.mat");

        var scene = EditorSceneManager.OpenScene(ARScenePath, OpenSceneMode.Single);
        var origin = Object.FindAnyObjectByType<XROrigin>();
        var session = Object.FindAnyObjectByType<ARSession>();
        if (origin == null || session == null)
        {
            Debug.LogError("RUGA: ARWorkspace needs an XR Origin and an AR Session. Run RUGA > Setup > Milestone 1 first.");
            return;
        }

        // The app turns these on only after checking ARCore and the camera permission.
        session.enabled = false;
        var planeManager = GetOrAdd<ARPlaneManager>(origin.gameObject);
        planeManager.planePrefab = planePrefab;
        planeManager.requestedDetectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Horizontal;
        planeManager.enabled = false;
        var raycastManager = GetOrAdd<ARRaycastManager>(origin.gameObject);
        var anchorManager = GetOrAdd<ARAnchorManager>(origin.gameObject);

        var managers = GameObject.Find("Managers");
        if (managers == null)
            managers = new GameObject("Managers");
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(managers); // the old Milestone 2 script

        var tracking = GetOrAdd<ARTrackingManager>(managers);
        SetReferences(tracking, ("session", session), ("planeManager", planeManager),
            ("raycastManager", raycastManager), ("anchorManager", anchorManager));
        var guide = GetOrAdd<GuideManager>(managers);
        SetReferences(guide, ("guidePrefab", guidePrefab));
        var gestures = GetOrAdd<GuideGestures>(managers);
        var sessionManager = GetOrAdd<SessionManager>(managers);
        var data = GetOrAdd<DataManager>(managers);
        var ui = GetOrAdd<UIManager>(managers);
        var titleFont = GetOrCreateFontAsset(TitleFontPath);
        var iconFont = GetOrCreateFontAsset(IconFontPath);
        var bodyFont = GetOrCreateFontAsset(BodyFontPath);
        SetReferences(ui, ("titleFont", titleFont), ("iconFont", iconFont), ("bodyFont", bodyFont),
            ("logo", AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath)));
        var app = GetOrAdd<AppController>(managers);
        SetReferences(app, ("ui", ui), ("ar", tracking), ("guide", guide), ("gestures", gestures),
            ("session", sessionManager), ("data", data), ("arCamera", origin.Camera));

        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        SetAppIcon();
        SetSplashScreen();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("RUGA: Full app setup finished. Press Play to try it.");
    }

    // Semi-transparent pale green fill plus a thin outline, so detected surfaces are visible.
    static GameObject CreatePlanePrefab(Material fill, Material edge)
    {
        var go = new GameObject("ARPlaneVisual", typeof(ARPlane), typeof(ARPlaneMeshVisualizer),
            typeof(MeshFilter), typeof(MeshRenderer), typeof(LineRenderer));

        var meshRenderer = go.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = fill;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        // ARPlaneMeshVisualizer writes the boundary points in local space.
        var line = go.GetComponent<LineRenderer>();
        line.sharedMaterial = edge;
        line.useWorldSpace = false;
        line.loop = true;
        line.widthMultiplier = 0.005f;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;

        return SaveAsPrefab(go, $"{PrefabsFolder}/ARPlaneVisual.prefab");
    }

    // The guide: two flat quads lying on the surface. "Art" shows the stage image, "Grid" the alignment grid.
    // GuideManager sets their size in metres from the typed centimetres.
    static GameObject CreateGuidePrefab(Material art, Material grid)
    {
        var root = new GameObject("Guide");
        AddFlatQuad(root.transform, "Art", art, 0.002f); // just above the surface outline
        AddFlatQuad(root.transform, "Grid", grid, 0.003f);
        return SaveAsPrefab(root, $"{PrefabsFolder}/Guide.prefab");
    }

    static void AddFlatQuad(Transform parent, string name, Material material, float height)
    {
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        quad.transform.SetParent(parent, false);
        quad.transform.localPosition = new Vector3(0f, height, 0f);
        quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // lie flat, facing up, image top pointing away
        quad.transform.localScale = new Vector3(0.30f, 0.40f, 1f);

        var renderer = quad.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    // Makes a TextMeshPro SDF font asset next to the font file, the same way as
    // right-click > Create > TextMeshPro > Font Asset > SDF. Dynamic, so glyphs are added as needed.
    static TMP_FontAsset GetOrCreateFontAsset(string fontPath)
    {
        var assetPath = System.IO.Path.ChangeExtension(fontPath, null) + " SDF.asset";
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null)
            return existing;

        var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        if (font == null)
        {
            Debug.LogWarning($"RUGA: Font file {fontPath} not found; the UI will use the default font for it.");
            return null;
        }
        if (TMP_Settings.instance == null)
        {
            Debug.LogWarning("RUGA: TextMeshPro resources aren't imported yet. Run RUGA > Setup > Full App again after the import.");
            return null;
        }

        Selection.activeObject = font;
        EditorApplication.ExecuteMenuItem("Assets/Create/TextMeshPro/Font Asset/SDF");
        var created = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (created == null)
            Debug.LogWarning($"RUGA: Could not create a font asset for {fontPath}.");
        return created;
    }

    // Android adaptive app icon: a background and a foreground layer, cut to each launcher's shape.
    static void SetAppIcon()
    {
        MakeUncompressed(AppIconPath);
        MakeUncompressed(AppIconBackgroundPath);
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
        var background = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconBackgroundPath);
        if (icon == null || background == null)
        {
            Debug.LogWarning($"RUGA: App icon files not found in {System.IO.Path.GetDirectoryName(AppIconPath)}; keeping the current icon.");
            return;
        }

        var android = NamedBuildTarget.Android;
        var icons = PlayerSettings.GetPlatformIcons(android, AndroidPlatformIconKind.Adaptive);
        foreach (var platformIcon in icons)
            platformIcon.SetTextures(background, icon);
        PlayerSettings.SetPlatformIcons(android, AndroidPlatformIconKind.Adaptive, icons);
        Debug.Log($"RUGA: App icon set ({icons.Length} sizes).");
    }

    // Startup screen: the RUGA logo on the app's dark background, without the Unity logo
    // (optional for Unity Personal from Unity 6 on).
    static void SetSplashScreen()
    {
        if (!System.IO.File.Exists(SplashLogoPath))
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath) == null)
                return;
            AssetDatabase.CopyAsset(AppIconPath, SplashLogoPath);
        }

        // The splash screen needs the logo as a Sprite.
        if (AssetImporter.GetAtPath(SplashLogoPath) is TextureImporter importer &&
            (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        var logo = AssetDatabase.LoadAssetAtPath<Sprite>(SplashLogoPath);
        if (logo == null)
        {
            Debug.LogWarning("RUGA: Splash logo could not be loaded; keeping the current splash screen.");
            return;
        }

        PlayerSettings.SplashScreen.show = true;
        PlayerSettings.SplashScreen.showUnityLogo = false;
        PlayerSettings.SplashScreen.backgroundColor = new Color32(0xf3, 0xec, 0xe0, 0xff); // RUGA parchment
        PlayerSettings.SplashScreen.overlayOpacity = 0f;
        PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(2f, logo) };
        Debug.Log("RUGA: Splash screen set to the RUGA logo.");
    }

    // Icons look best uncompressed; Android resizes them for each screen density itself.
    static void MakeUncompressed(string path)
    {
        if (AssetImporter.GetAtPath(path) is TextureImporter importer &&
            (importer.textureCompression != TextureImporterCompression.Uncompressed || importer.mipmapEnabled))
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }

    static void DeleteIfExists(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            AssetDatabase.DeleteAsset(path);
    }

    static Material CreateUnlitMaterial(string path, Color color, bool transparent)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            // Keep the colour in step with the theme.
            if (existing.GetColor("_BaseColor") != color)
            {
                existing.SetColor("_BaseColor", color);
                EditorUtility.SetDirty(existing);
            }
            return existing;
        }

        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Cull", (float)CullMode.Off);
        if (transparent)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static GameObject SaveAsPrefab(GameObject instance, string path)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);
        return prefab;
    }

    static T GetOrAdd<T>(GameObject go) where T : Component =>
        go.TryGetComponent<T>(out var existing) ? existing : go.AddComponent<T>();

    static void SetReferences(Object target, params (string field, Object value)[] references)
    {
        var so = new SerializedObject(target);
        foreach (var (field, value) in references)
            so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder("Assets", path.Substring("Assets/".Length));
    }
}
