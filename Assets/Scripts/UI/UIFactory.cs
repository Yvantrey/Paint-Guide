using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ButtonStyle
{
    Primary,
    Secondary,
    Link
}

// Small helpers that build uGUI controls in RUGA's style, so screens read like a layout description.
public static class UIFactory
{
    const int UILayer = 5;

    public static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform)) { layer = UILayer };
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static RectTransform Stretch(RectTransform rt, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
        return rt;
    }

    public static Image Panel(string name, Transform parent, Color color, bool rounded = true, bool blocksTouches = false)
    {
        var image = Rect(name, parent).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = blocksTouches;
        if (rounded)
        {
            image.sprite = UITheme.Rounded;
            image.type = Image.Type.Sliced;
        }
        return image;
    }

    public static VerticalLayoutGroup Vertical(Component target, int padH = 0, int padV = 0, float spacing = 10f)
    {
        var layout = target.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(padH, padH, padV, padV);
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return layout;
    }

    public static HorizontalLayoutGroup Horizontal(Component target, float spacing = 8f, TextAnchor align = TextAnchor.MiddleLeft)
    {
        var layout = target.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = align;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return layout;
    }

    public static RectTransform Row(Transform parent, float spacing = 8f, string name = "Row")
    {
        var row = Rect(name, parent);
        Horizontal(row, spacing);
        return row;
    }

    public static RectTransform Column(Transform parent, float spacing = 10f, string name = "Column")
    {
        var column = Rect(name, parent);
        Vertical(column, 0, 0, spacing);
        return column;
    }

    public static LayoutElement Size(Component c, float width = -1f, float height = -1f, float flexibleWidth = -1f)
    {
        if (!c.TryGetComponent<LayoutElement>(out var element))
            element = c.gameObject.AddComponent<LayoutElement>();
        if (width >= 0f)
        {
            element.minWidth = width;
            element.preferredWidth = width;
        }
        if (height >= 0f)
        {
            element.minHeight = height;
            element.preferredHeight = height;
        }
        if (flexibleWidth >= 0f)
            element.flexibleWidth = flexibleWidth;
        return element;
    }

    public static RectTransform Spacer(Transform parent, float height = -1f, float flexibleWidth = -1f)
    {
        var spacer = Rect("Spacer", parent);
        Size(spacer, -1f, height, flexibleWidth);
        return spacer;
    }

    public static TextMeshProUGUI Text(Transform parent, string text, float size, Color color,
        bool title = false, FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
    {
        var tmp = Rect("Text", parent).gameObject.AddComponent<TextMeshProUGUI>();
        var font = title ? UITheme.TitleFont : UITheme.BodyFont;
        if (font != null)
            tmp.font = font;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        tmp.text = text;
        return tmp;
    }

    // Small uppercase label in the title font, like the prototype's "micro" text.
    public static TextMeshProUGUI Micro(Transform parent, string text) =>
        Text(parent, text.ToUpperInvariant(), 11f, UITheme.Muted, true, FontStyles.Normal);

    // An icon from the Phosphor font (see Icons). Shows nothing if the icon font isn't set.
    public static TextMeshProUGUI Icon(Transform parent, string icon, float size, Color color)
    {
        var tmp = Text(parent, "", size, color, false, FontStyles.Normal, TextAlignmentOptions.Center);
        tmp.name = "Icon";
        if (UITheme.IconFont != null)
            tmp.font = UITheme.IconFont;
        SetIcon(tmp, icon);
        return tmp;
    }

    public static void SetIcon(TextMeshProUGUI iconText, string icon)
    {
        iconText.text = UITheme.IconFont != null ? icon : "";
        iconText.gameObject.SetActive(UITheme.IconFont != null && !string.IsNullOrEmpty(icon));
    }

    // A button with a text label and, optionally, an icon before or after it.
    public static Button Button(Transform parent, string label, ButtonStyle style, Action onClick,
        float height = UITheme.ButtonHeight, string icon = null, bool iconAfter = false)
    {
        var bg = Panel("Button", parent, Color.white, true, true);
        var button = bg.gameObject.AddComponent<Button>();
        button.targetGraphic = bg;
        var colors = button.colors;
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.38f);
        button.colors = colors;
        button.onClick.AddListener(() => onClick?.Invoke());
        Size(bg, -1f, height);

        // Icon and label sit side by side, centred in the button.
        var content = Stretch(Rect("Content", bg.transform), 10f, 0f, 10f, 0f);
        Horizontal(content, 7f, TextAnchor.MiddleCenter);

        float textSize = style == ButtonStyle.Link ? 13f : 15f;
        TextMeshProUGUI iconText = null;
        if (icon != null && !iconAfter)
            iconText = Icon(content, icon, textSize + 3f, UITheme.Ink);
        var text = Text(content, label, textSize, UITheme.Ink, false,
            style == ButtonStyle.Link ? FontStyles.Underline : FontStyles.Bold, TextAlignmentOptions.Center);
        text.name = "Label";
        if (icon != null && iconAfter)
            iconText = Icon(content, icon, textSize + 3f, UITheme.Ink);

        Color ink;
        switch (style)
        {
            case ButtonStyle.Primary:
                bg.color = UITheme.Accent;
                ink = UITheme.Deep;
                break;
            case ButtonStyle.Secondary:
                bg.color = UITheme.Card;
                AddBorder(bg, UITheme.Line);
                ink = UITheme.Ink;
                break;
            default:
                bg.color = Color.clear;
                ink = UITheme.Muted;
                break;
        }
        text.color = ink;
        if (iconText != null)
            iconText.color = ink;
        return button;
    }

    public static TextMeshProUGUI LabelOf(Button button) => Child(button, "Label");

    public static TextMeshProUGUI IconOf(Button button) => Child(button, "Icon");

    static TextMeshProUGUI Child(Button button, string name)
    {
        foreach (var text in button.GetComponentsInChildren<TextMeshProUGUI>(true))
            if (text.name == name)
                return text;
        return null;
    }

    // Round icon button, e.g. back or settings. The fallback text shows if the icon font is missing.
    public static Button RoundButton(Transform parent, string icon, string fallback, Action onClick, float size = 44f)
    {
        var bg = Panel("RoundButton", parent, UITheme.Card, false, true);
        bg.sprite = UITheme.Circle;
        AddBorder(bg, UITheme.Line);
        var button = bg.gameObject.AddComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(() => onClick?.Invoke());
        Size(bg, size, size);

        var text = UITheme.IconFont != null
            ? Icon(bg.transform, icon, size * 0.45f, UITheme.Ink)
            : Text(bg.transform, fallback, 16f, UITheme.Ink, false, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        return button;
    }

    public static Button ColourDot(Transform parent, Color colour, Action onClick, float size = 34f)
    {
        var bg = Panel("ColourDot", parent, colour, false, true);
        bg.sprite = UITheme.Circle;
        var button = bg.gameObject.AddComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(() => onClick?.Invoke());
        Size(bg, size, size);
        return button;
    }

    // UnityEngine.UI.Outline draws offset copies of the shape, which reads as a thin border.
    public static Outline AddBorder(Graphic graphic, Color colour, float width = 1.5f)
    {
        var outline = graphic.gameObject.AddComponent<Outline>();
        outline.effectColor = colour;
        outline.effectDistance = new Vector2(width, -width);
        return outline;
    }

    public static Slider Slider(Transform parent, float min, float max, float value, Action<float> onChange)
    {
        var root = Rect("Slider", parent);
        Size(root, -1f, 36f);
        var slider = root.gameObject.AddComponent<Slider>();

        var track = Panel("Track", root, UITheme.Line, true, true);
        track.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        track.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        track.rectTransform.sizeDelta = new Vector2(0f, 6f);

        // Inset by the handle radius so the fill ends at the handle's centre.
        var fillArea = Rect("Fill Area", root);
        fillArea.anchorMin = new Vector2(0f, 0.5f);
        fillArea.anchorMax = new Vector2(1f, 0.5f);
        fillArea.offsetMin = new Vector2(12f, -3f);
        fillArea.offsetMax = new Vector2(-12f, 3f);
        var fill = Panel("Fill", fillArea, UITheme.Accent);
        fill.rectTransform.sizeDelta = Vector2.zero;

        var handleArea = Rect("Handle Area", root);
        Stretch(handleArea, 12f, 0f, 12f, 0f);
        var handle = Panel("Handle", handleArea, UITheme.Accent, false, true);
        handle.sprite = UITheme.Circle;
        handle.rectTransform.sizeDelta = new Vector2(24f, -12f);

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = true;
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(v => onChange?.Invoke(v));
        return slider;
    }

    public static TMP_InputField NumberField(Transform parent, string value, Action<string> onEndEdit)
    {
        var bg = Panel("NumberField", parent, UITheme.Card, true, true);
        AddBorder(bg, UITheme.Line);
        Size(bg, -1f, 44f);

        var viewport = Rect("Text Area", bg.transform);
        Stretch(viewport, 12f, 6f, 12f, 6f);
        viewport.gameObject.AddComponent<RectMask2D>();

        var placeholder = Text(viewport, "cm", 15f, UITheme.Muted, false, FontStyles.Normal, TextAlignmentOptions.Left);
        Stretch(placeholder.rectTransform);
        var text = Text(viewport, "", 15f, UITheme.Ink, false, FontStyles.Normal, TextAlignmentOptions.Left);
        Stretch(text.rectTransform);

        var field = bg.gameObject.AddComponent<TMP_InputField>();
        field.textViewport = viewport;
        field.textComponent = text;
        field.placeholder = placeholder;
        field.contentType = TMP_InputField.ContentType.IntegerNumber;
        field.characterLimit = 3;
        field.targetGraphic = bg;
        field.SetTextWithoutNotify(value);
        field.onEndEdit.AddListener(v => onEndEdit?.Invoke(v));
        return field;
    }

    // A picture that fills its box without stretching, cropping the edges like CSS "cover".
    public static RawImage Picture(Transform parent, Texture texture, float height)
    {
        var frame = Panel("Picture", parent, UITheme.Card);
        frame.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        Size(frame, -1f, height);

        var image = Rect("Image", frame.transform).gameObject.AddComponent<RawImage>();
        Stretch(image.rectTransform);
        image.raycastTarget = false;
        image.texture = texture;
        image.gameObject.AddComponent<CoverImage>();
        return image;
    }

    // Removes all children now. Destroy only happens at the end of the frame,
    // so children are detached first to keep counts and layouts correct immediately.
    public static void Clear(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
            child.gameObject.SetActive(false);
            child.SetParent(null, false);
            UnityEngine.Object.Destroy(child.gameObject);
        }
    }

    // A full-screen page that scrolls vertically. Returns the content to add rows to.
    public static RectTransform ScrollScreen(string name, Transform parent, out GameObject screen)
    {
        var root = Stretch(Rect(name, parent));
        screen = root.gameObject;

        var scroll = root.gameObject.AddComponent<ScrollRect>();
        var viewport = Stretch(Rect("Viewport", root));
        viewport.gameObject.AddComponent<RectMask2D>();
        var hitArea = viewport.gameObject.AddComponent<Image>();
        hitArea.color = Color.clear; // needed so the page can be dragged anywhere

        var content = Rect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = Vector2.zero;
        var layout = Vertical(content, (int)UITheme.Gutter, 18, 12f);
        layout.padding.bottom = 32;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = content;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 30f;
        return content;
    }

    public static string FormatDuration(float seconds)
    {
        var total = Mathf.Max(0, Mathf.RoundToInt(seconds));
        int h = total / 3600, m = total / 60 % 60, s = total % 60;
        return h > 0 ? $"{h}:{m:00}:{s:00}" : $"{m}:{s:00}";
    }
}
