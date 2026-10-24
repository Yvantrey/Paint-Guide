using System;
using System.Collections.Generic;
using UnityEngine;

public enum PaintingCategory
{
    Warmup,
    Painting
}

// A built-in painting or warm-up, ready to use. Built by PaintingFactory from JSON and images.
public class Painting
{
    public string Id { get; }
    public string Name { get; }
    public string Label { get; }
    public PaintingCategory Category { get; }
    public Texture2D FinalImage { get; }
    public IReadOnlyList<Stage> Stages { get; }

    public Painting(string id, string name, string label, PaintingCategory category, Texture2D finalImage, IReadOnlyList<Stage> stages)
    {
        Id = id;
        Name = name;
        Label = label;
        Category = category;
        FinalImage = finalImage;
        Stages = stages;
    }
}

public class Stage
{
    public int Index { get; }
    public string Title { get; }
    public string Instruction { get; }
    public Texture2D GuideImage { get; }

    public Stage(int index, string title, string instruction, Texture2D guideImage)
    {
        Index = index;
        Title = title;
        Instruction = instruction;
        GuideImage = guideImage;
    }
}

// The shape of a painting JSON file in Assets/Resources/Paintings. Field names match the JSON keys.
[Serializable]
public class PaintingJson
{
    public string id;
    public string name;
    public string label;
    public string category; // "warmup" or "painting"
    public int order;
    public string finalImage; // image name inside the painting's folder, without .png
    public StageJson[] stages;
}

[Serializable]
public class StageJson
{
    public string title;
    public string instruction;
    public string guideImage;
}
