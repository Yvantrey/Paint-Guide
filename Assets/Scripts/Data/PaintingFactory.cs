using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Factory: turns the bundled JSON files and images into Painting and Stage objects.
// Files live in Assets/Resources/Paintings: <id>.json, plus a folder <id>/ holding the PNGs.
// A missing image is replaced by a test pattern, so the app keeps working while artwork is in progress.
public static class PaintingFactory
{
    const string Folder = "Paintings";

    // Warm-ups first, then paintings, each sorted by the "order" field.
    public static List<Painting> LoadAll()
    {
        var loaded = new List<(PaintingJson json, Painting painting)>();
        foreach (var file in Resources.LoadAll<TextAsset>(Folder))
        {
            var json = Parse(file);
            var painting = json != null ? Create(json) : null;
            if (painting != null)
                loaded.Add((json, painting));
        }

        return loaded
            .OrderBy(p => p.painting.Category)
            .ThenBy(p => p.json.order)
            .Select(p => p.painting)
            .ToList();
    }

    public static Painting Create(PaintingJson json)
    {
        var stages = new List<Stage>(json.stages.Length);
        for (int i = 0; i < json.stages.Length; i++)
        {
            var s = json.stages[i];
            var image = LoadImage(json.id, s.guideImage) ?? GuideTextures.Placeholder(i, json.stages.Length);
            stages.Add(new Stage(i, s.title, s.instruction, image));
        }

        var category = json.category == "warmup" ? PaintingCategory.Warmup : PaintingCategory.Painting;
        var finalImage = LoadImage(json.id, json.finalImage) ?? GuideTextures.FinalPlaceholder();
        return new Painting(json.id, json.name, json.label, category, finalImage, stages);
    }

    static PaintingJson Parse(TextAsset file)
    {
        PaintingJson json;
        try
        {
            json = JsonUtility.FromJson<PaintingJson>(file.text);
        }
        catch (System.ArgumentException e)
        {
            Debug.LogError($"RUGA: {file.name}.json is not valid JSON: {e.Message}");
            return null;
        }

        if (json == null || string.IsNullOrEmpty(json.id) || json.stages == null || json.stages.Length == 0)
        {
            Debug.LogError($"RUGA: {file.name}.json needs an id and at least one stage.");
            return null;
        }

        return json;
    }

    static Texture2D LoadImage(string paintingId, string imageName)
    {
        if (string.IsNullOrEmpty(imageName))
            return null;
        return Resources.Load<Texture2D>($"{Folder}/{paintingId}/{imageName}");
    }
}
