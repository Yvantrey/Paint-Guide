using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Renders RUGA's main screens to PNG files without pressing Play, to check the layout and theme.
// Menu: RUGA > Preview Screens. From the command line, pass -previewOut <folder>.
public static class RugaPreview
{
    const int Width = 1080;
    const int Height = 2337; // a typical 19.5:9 phone

    [MenuItem("RUGA/Preview Screens")]
    public static void RenderScreens()
    {
        var outDir = ArgValue("-previewOut") ?? Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Preview");
        Directory.CreateDirectory(outDir);

        // The preview uses a temporary scene; offer to save the open one, and reopen it afterwards.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        var previousScene = EditorSceneManager.GetActiveScene().path;

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Preview Camera").AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        var target = new RenderTexture(Width, Height, 24);
        camera.targetTexture = target;

        var ui = new GameObject("UI").AddComponent<UIManager>();
        var so = new SerializedObject(ui);
        so.FindProperty("titleFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Fonts/CopperplateCC-Bold SDF.asset");
        so.FindProperty("bodyFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Fonts/Alegreya SDF.asset");
        so.FindProperty("iconFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Icons/Phosphor SDF.asset");
        so.FindProperty("logo").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/AppIcon/ruga_icon.png");
        so.ApplyModifiedPropertiesWithoutUndo();
        ui.Build();

        var canvas = ui.GetComponentInChildren<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1f;

        var paintings = PaintingFactory.LoadAll();
        var mountain = paintings.First(p => p.Id == "mountain_sunrise");
        var record = FakeRecord(mountain);

        ui.ShowHome(new[] { record }, mountain);
        Capture(camera, canvas, Path.Combine(outDir, "1_home.png"));

        ui.ShowCatalog(paintings, mountain);
        Capture(camera, canvas, Path.Combine(outDir, "2_catalog.png"));

        ui.ShowReference(mountain);
        ui.ShowReferenceStage(mountain, 2, false);
        Capture(camera, canvas, Path.Combine(outDir, "3_reference_guide.png"));
        ui.ShowReferenceStage(mountain, 2, true);
        Capture(camera, canvas, Path.Combine(outDir, "4_reference_finished.png"));

        // AR paint panel over a grey stand-in for the camera image.
        ui.ShowWorkspace(mountain.Name);
        ui.SetModeTag("Guide locked");
        ui.ShowPaintPanel();
        ui.ShowStage(mountain, 1);
        ui.SetGuideControls(0.5f, GuideColour.Cyan, true, false, false);
        Capture(camera, canvas, Path.Combine(outDir, "5_ar_paint.png"), new Color(0.45f, 0.42f, 0.38f));

        ui.ShowSummary(mountain, record, true);
        Capture(camera, canvas, Path.Combine(outDir, "6_summary.png"));

        ui.ShowSettings(new Preferences());
        Capture(camera, canvas, Path.Combine(outDir, "7_settings.png"));

        Debug.Log($"RUGA: Preview screens saved to {outDir}");
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(0);
            return;
        }

        target.Release();
        if (!string.IsNullOrEmpty(previousScene))
            EditorSceneManager.OpenScene(previousScene);
        EditorUtility.RevealInFinder(outDir);
    }

    static SessionRecord FakeRecord(Painting painting)
    {
        var record = new SessionRecord
        {
            sessionId = "preview",
            paintingId = painting.Id,
            paintingName = painting.Name,
            stageCount = painting.Stages.Count,
            startTimeUtc = DateTime.UtcNow.AddHours(-1).ToString("o"),
            completed = true
        };
        float total = 0f;
        for (int i = 0; i < painting.Stages.Count; i++)
        {
            float seconds = 420f + 180f * i;
            total += seconds;
            record.stages.Add(new StageRecord { stageIndex = i, completedTimeUtc = DateTime.UtcNow.ToString("o"), durationSeconds = seconds });
        }
        record.totalSeconds = total;
        return record;
    }

    static void Capture(Camera camera, Canvas canvas, string path, Color? background = null)
    {
        camera.backgroundColor = background ?? Color.black;

        // Settle layout twice (nested layout groups and text sizes depend on each other), then fit pictures.
        for (int i = 0; i < 2; i++)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvas.transform);
        }
        foreach (var cover in canvas.GetComponentsInChildren<CoverImage>())
            cover.Fit();
        Canvas.ForceUpdateCanvases();

        camera.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = camera.targetTexture;
        var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        image.Apply();
        RenderTexture.active = previous;
        File.WriteAllBytes(path, image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image);
    }

    static string ArgValue(string name)
    {
        var args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
