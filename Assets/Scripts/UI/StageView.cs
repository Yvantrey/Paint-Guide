using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The stage display shared by the AR paint panel and Reference mode:
// leaf progress, stage title and instruction, paint swatches, and Previous / Next buttons.
public class StageView
{
    RectTransform progressRow;
    TextMeshProUGUI stepLabel, title, instruction;
    GameObject swatchBlock;
    RectTransform swatchRow;
    Button previous;
    TextMeshProUGUI nextLabel, nextIcon;

    public static StageView BuildHeader(Transform parent, float titleSize = 19f)
    {
        var view = new StageView();

        // Stage progress: leaves spaced along a thin stem.
        var progress = UIFactory.Rect("Progress", parent);
        UIFactory.Size(progress, -1f, 26f);
        var stem = UIFactory.Panel("Stem", progress, UITheme.Line, false).rectTransform;
        stem.anchorMin = new Vector2(0f, 0.5f);
        stem.anchorMax = new Vector2(1f, 0.5f);
        stem.sizeDelta = new Vector2(0f, 2f);
        view.progressRow = UIFactory.Stretch(UIFactory.Rect("Leaves", progress));
        UIFactory.Horizontal(view.progressRow, 0f, TextAnchor.MiddleCenter).childForceExpandWidth = true;

        view.stepLabel = UIFactory.Micro(parent, "");
        view.title = UIFactory.Text(parent, "", titleSize, UITheme.Ink, true);
        view.instruction = UIFactory.Text(parent, "", 13f, UITheme.Muted);
        return view;
    }

    // "Paint swatches": the suggested colours for the stage, like little dabs of paint.
    public void BuildSwatches(Transform parent)
    {
        var block = UIFactory.Column(parent, 6, "Swatches");
        swatchBlock = block.gameObject;
        UIFactory.Micro(block, "Paint swatches");
        swatchRow = UIFactory.Row(block, 10);
    }

    public void BuildNav(Transform parent, Action<UIAction> raise)
    {
        var nav = UIFactory.Row(parent, 8);
        previous = UIFactory.Button(nav, "Previous", ButtonStyle.Secondary, () => raise(UIAction.PreviousStage), UITheme.SmallButtonHeight, Icons.ArrowLeft);
        UIFactory.Size(previous, -1f, -1f, 1f);
        var next = UIFactory.Button(nav, "Next stage", ButtonStyle.Primary, () => raise(UIAction.NextStage), UITheme.SmallButtonHeight, Icons.Leaf, true);
        UIFactory.Size(next, -1f, -1f, 1f);
        nextLabel = UIFactory.LabelOf(next);
        nextIcon = UIFactory.IconOf(next);
    }

    public void Show(Painting painting, int index)
    {
        int count = painting.Stages.Count;
        var stage = painting.Stages[index];
        stepLabel.text = $"Stage {index + 1} of {count}";
        title.text = stage.Title;
        instruction.text = stage.Instruction;

        if (previous != null)
        {
            previous.interactable = index > 0;
            bool last = index == count - 1;
            nextLabel.text = last ? "Finish" : "Next stage";
            UIFactory.SetIcon(nextIcon, last ? Icons.Check : Icons.Leaf);
        }

        ShowProgress(count, index);
        if (swatchBlock != null)
            ShowSwatches(stage);
    }

    // One leaf per stage: done in terracotta, current in sage, still to come in the pale line colour.
    void ShowProgress(int count, int index)
    {
        if (progressRow.childCount != count)
        {
            UIFactory.Clear(progressRow);
            for (int i = 0; i < count; i++)
            {
                if (UITheme.IconFont != null)
                    UIFactory.Icon(progressRow, Icons.Leaf, 22f, UITheme.Line);
                else
                    UIFactory.Size(UIFactory.Panel("Step", progressRow, UITheme.Line, false), -1f, 3f, 1f);
            }
        }
        for (int i = 0; i < progressRow.childCount; i++)
            progressRow.GetChild(i).GetComponent<Graphic>().color =
                i < index ? UITheme.Accent : i == index ? UITheme.Sage : UITheme.Line;
    }

    void ShowSwatches(Stage stage)
    {
        UIFactory.Clear(swatchRow);
        swatchBlock.SetActive(stage.Swatches.Count > 0);
        foreach (var swatch in stage.Swatches)
        {
            var item = UIFactory.Column(swatchRow, 4, "Swatch");
            UIFactory.Size(item, 70f);
            item.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
            item.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = false;

            var dab = UIFactory.Panel("Dab", item, swatch.Colour, false);
            dab.sprite = UITheme.Circle;
            UIFactory.Size(dab, 30f, 30f);
            UIFactory.AddShadow(dab);
            var label = UIFactory.Text(item, swatch.Name, 11f, UITheme.Muted, false, FontStyles.Normal, TextAlignmentOptions.Top);
            UIFactory.Size(label, 70f); // fixed width so longer names wrap onto two lines
        }
    }
}
