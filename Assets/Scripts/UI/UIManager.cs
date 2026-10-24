using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Builds and shows every screen. It reports button presses as events and never calls other managers;
// the app states listen to these events and tell the UI what to show.
public class UIManager : MonoBehaviour
{
    const float ToastSeconds = 2.4f;
    const int RecentSessionsShown = 5;

    [Tooltip("Font for titles and small labels (Copperplate CC). Leave empty to use TextMeshPro's default font.")]
    [SerializeField] TMP_FontAsset titleFont;
    [Tooltip("Sans-serif font for everything else. Leave empty to use TextMeshPro's default font.")]
    [SerializeField] TMP_FontAsset bodyFont;
    [Tooltip("Phosphor icon font. Leave empty to show buttons without icons.")]
    [SerializeField] TMP_FontAsset iconFont;
    [Tooltip("RUGA logo shown as the badge on Home. Leave empty to show a plain 'R' mark.")]
    [SerializeField] Texture2D logo;

    // Shown on the Credits screen: name, details, and the licence file in Resources/Licences (or null).
    static readonly (string name, string details, string licenceFile)[] Credits =
    {
        ("Copperplate CC", "Title font by Owen Earl (indestructible type*) for The Cowboy Collective. SIL Open Font License 1.1.", "CopperplateCC-OFL"),
        ("Phosphor Icons", "Icons © 2020-2021 Phosphor Icons. MIT License.", "Phosphor-LICENSE"),
        ("Liberation Sans", "Body text font, included with TextMeshPro. SIL Open Font License 1.1.", null),
        ("Artwork", "All paintings and warm-up guides are original artwork made for RUGA.", null)
    };

    public event Action<UIAction> OnAction;
    public event Action<Painting> OnPaintingSelected;
    public event Action<Vector2> OnSizeEntered;
    public event Action<float> OnOpacityChanged;
    public event Action<GuideColour> OnColourChanged;

    static readonly GuideColour[] Colours = { GuideColour.Cyan, GuideColour.Yellow, GuideColour.White };

    Transform canvasRoot;
    GameObject backdrop;
    readonly List<GameObject> screens = new List<GameObject>();

    // Home
    GameObject homeScreen;
    RawImage heroImage;
    TextMeshProUGUI heroLabel;
    GameObject recentHeader;
    RectTransform recentList;

    // Catalog
    GameObject catalogScreen;
    RectTransform catalogList;
    TextMeshProUGUI useButtonLabel;
    readonly Dictionary<Painting, (Image background, Outline border)> catalogCards = new Dictionary<Painting, (Image, Outline)>();

    // Settings
    GameObject settingsScreen;
    Button[] settingsDots;
    Slider settingsOpacity;
    TextMeshProUGUI settingsOpacityValue;

    // Credits
    GameObject creditsScreen;

    // Summary
    GameObject summaryScreen;
    TextMeshProUGUI summaryPill;
    TextMeshProUGUI summaryName;
    RawImage summaryPicture;
    TextMeshProUGUI summaryTotal;
    RectTransform summaryTimes;
    TextMeshProUGUI summarySaveStatus;

    // Workspace
    GameObject workspaceScreen;
    TextMeshProUGUI workspaceTitle;
    TextMeshProUGUI modeTag;
    GameObject trackingWarning;
    GameObject scanPanel, alignPanel, paintPanel;
    TextMeshProUGUI scanTitle, scanText;
    TMP_InputField widthField, heightField;
    TextMeshProUGUI lockLabel;
    RectTransform progressRow;
    TextMeshProUGUI stepLabel, stageTitle, stageInstruction;
    Slider opacitySlider;
    TextMeshProUGUI opacityValue;
    Button outlineToggle, gridToggle, previousButton;
    Button[] paintDots;
    TextMeshProUGUI nextLabel, nextIcon, hideLabel, hideIcon;

    // Overlays
    GameObject toast;
    TextMeshProUGUI toastText;
    Coroutine toastRoutine;
    GameObject modal;
    TextMeshProUGUI modalTitle, modalBody, modalOkLabel, modalCancelLabel;
    GameObject modalCancel;
    Action modalOkAction, modalCancelAction;

    public bool IsModalOpen => modal != null && modal.activeSelf;

    public void Build()
    {
        UITheme.TitleFont = titleFont;
        UITheme.BodyFont = bodyFont;
        UITheme.IconFont = iconFont;
        WarnIfNoFont();
        EnsureEventSystem();

        var canvasRect = UIFactory.Rect("RUGA UI", transform);
        var canvas = canvasRect.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasRect.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UITheme.ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;
        canvasRect.gameObject.AddComponent<GraphicRaycaster>();
        canvasRoot = canvasRect;

        // Opaque background for the non-AR screens. The workspace leaves it off so the camera shows.
        var backdropImage = UIFactory.Panel("Backdrop", canvasRect, UITheme.Background, false, true);
        UIFactory.Stretch(backdropImage.rectTransform);
        backdrop = backdropImage.gameObject;

        var safe = UIFactory.Stretch(UIFactory.Rect("SafeArea", canvasRect));
        safe.gameObject.AddComponent<SafeArea>();

        BuildHome(safe);
        BuildCatalog(safe);
        BuildSettings(safe);
        BuildCredits(safe);
        BuildSummary(safe);
        BuildWorkspace(safe);
        BuildToast(canvasRect);
        BuildModal(canvasRect);
    }

    // ---------- Showing screens ----------

    public void ShowHome(IReadOnlyList<SessionRecord> sessions, Painting hero)
    {
        if (hero != null)
        {
            heroImage.texture = hero.FinalImage;
            heroImage.GetComponent<CoverImage>().Fit();
            heroLabel.text = $"{hero.Name.ToUpperInvariant()} · {hero.Stages.Count} STAGES";
        }

        UIFactory.Clear(recentList);
        var recent = sessions.Take(RecentSessionsShown).ToList();
        recentHeader.SetActive(recent.Count > 0);
        foreach (var record in recent)
            AddRecentRow(record);

        ShowScreen(homeScreen, true);
    }

    public void ShowSettings(Preferences preferences)
    {
        SelectDot(settingsDots, preferences.GuideColour);
        var percent = Mathf.RoundToInt(preferences.DefaultOpacity * 100f);
        settingsOpacity.SetValueWithoutNotify(percent);
        settingsOpacityValue.text = $"{percent}%";
        ShowScreen(settingsScreen, true);
    }

    public void ShowCredits() => ShowScreen(creditsScreen, true);

    public void ShowCatalog(IReadOnlyList<Painting> paintings, Painting selected)
    {
        UIFactory.Clear(catalogList);
        catalogCards.Clear();

        AddCatalogSection("Warm-ups", paintings.Where(p => p.Category == PaintingCategory.Warmup));
        AddCatalogSection("Paintings", paintings.Where(p => p.Category == PaintingCategory.Painting));

        SetCatalogSelection(selected);
        ShowScreen(catalogScreen, true);
    }

    public void SetCatalogSelection(Painting selected)
    {
        foreach (var pair in catalogCards)
        {
            bool isSelected = pair.Key == selected;
            pair.Value.background.color = isSelected ? UITheme.CardSelected : UITheme.Card;
            pair.Value.border.enabled = isSelected;
        }
        useButtonLabel.text = selected != null && selected.Category == PaintingCategory.Warmup
            ? "Start this warm-up"
            : "Use this painting";
    }

    public void ShowWorkspace(string title)
    {
        workspaceTitle.text = title;
        SetTrackingWarning(false);
        ShowScreen(workspaceScreen, false);
    }

    public void SetModeTag(string text) => modeTag.text = text;

    public void SetTrackingWarning(bool visible) => trackingWarning.SetActive(visible);

    public void ShowScanPanel(ScanStep step)
    {
        switch (step)
        {
            case ScanStep.Preparing:
                scanTitle.text = "Getting the camera ready…";
                scanText.text = "One moment.";
                break;
            case ScanStep.Scanning:
                scanTitle.text = "Let’s find a surface.";
                scanText.text = "Move your phone slowly over the table. Keep the canvas and some of the table in view.";
                break;
            case ScanStep.SurfaceFound:
                scanTitle.text = "Your surface is ready.";
                scanText.text = "Tap the table where your canvas lies to place the guide.";
                break;
        }
        ShowSheetPanel(scanPanel);
    }

    public void ShowAlignPanel(Vector2 sizeCm, bool resuming)
    {
        SetSizeFields(sizeCm);
        lockLabel.text = resuming ? "Lock guide & continue" : "Lock guide & start";
        ShowSheetPanel(alignPanel);
    }

    public void SetSizeFields(Vector2 sizeCm)
    {
        widthField.SetTextWithoutNotify(Mathf.RoundToInt(sizeCm.x).ToString(CultureInfo.InvariantCulture));
        heightField.SetTextWithoutNotify(Mathf.RoundToInt(sizeCm.y).ToString(CultureInfo.InvariantCulture));
    }

    public void ShowPaintPanel() => ShowSheetPanel(paintPanel);

    public void ShowStage(Painting painting, int index)
    {
        int count = painting.Stages.Count;
        var stage = painting.Stages[index];
        stepLabel.text = $"STAGE {index + 1} OF {count}";
        stageTitle.text = stage.Title;
        stageInstruction.text = stage.Instruction;
        previousButton.interactable = index > 0;
        bool last = index == count - 1;
        nextLabel.text = last ? "Finish" : "Next stage";
        UIFactory.SetIcon(nextIcon, last ? Icons.Check : Icons.ArrowRight);

        if (progressRow.childCount != count)
        {
            UIFactory.Clear(progressRow);
            for (int i = 0; i < count; i++)
            {
                var bar = UIFactory.Panel("Step", progressRow, UITheme.Line, false);
                UIFactory.Size(bar, -1f, 3f, 1f);
            }
        }
        for (int i = 0; i < progressRow.childCount; i++)
            progressRow.GetChild(i).GetComponent<Image>().color = i <= index ? UITheme.Accent : UITheme.Line;
    }

    public void SetGuideControls(float opacity, GuideColour colour, bool outline, bool grid, bool hidden)
    {
        var percent = Mathf.RoundToInt(opacity * 100f);
        opacitySlider.SetValueWithoutNotify(percent);
        opacityValue.text = $"{percent}%";
        SetToggle(outlineToggle, outline);
        SetToggle(gridToggle, grid);
        SelectDot(paintDots, colour);
        hideLabel.text = hidden ? "Show guide" : "Hide guide";
        UIFactory.SetIcon(hideIcon, hidden ? Icons.Eye : Icons.EyeSlash);
    }

    public void ShowSummary(Painting painting, SessionRecord record, bool saved)
    {
        summaryPill.text = $"{record.stages.Count} / {record.stageCount} stages";
        summaryName.text = $"{painting.Name} · All {record.stageCount} stages completed";
        summaryPicture.texture = painting.FinalImage;
        summaryPicture.GetComponent<CoverImage>().Fit();
        summaryTotal.text = UIFactory.FormatDuration(record.totalSeconds);

        UIFactory.Clear(summaryTimes);
        for (int i = 0; i < painting.Stages.Count; i++)
        {
            var stageRecord = record.stages.FirstOrDefault(s => s.stageIndex == i);
            var row = UIFactory.Row(summaryTimes, 8);
            UIFactory.Size(row, -1f, 40f);
            UIFactory.Size(UIFactory.Text(row, painting.Stages[i].Title, 13f, UITheme.Muted), -1f, -1f, 1f);
            UIFactory.Text(row, stageRecord != null ? UIFactory.FormatDuration(stageRecord.durationSeconds) : "–", 13f, UITheme.Ink);
            UIFactory.Size(UIFactory.Panel("Line", summaryTimes, UITheme.Line, false), -1f, 1f);
        }

        summarySaveStatus.text = saved ? "Saved on this phone." : "This session couldn’t be saved on this phone.";
        ShowScreen(summaryScreen, true);
    }

    // ---------- Pop-ups ----------

    public void ShowModal(string title, string body, string okLabel, Action onOk, string cancelLabel = null, Action onCancel = null)
    {
        modalTitle.text = title;
        modalBody.text = body;
        modalOkLabel.text = okLabel;
        modalOkAction = onOk;
        modalCancelAction = onCancel;
        modalCancel.SetActive(cancelLabel != null);
        if (cancelLabel != null)
            modalCancelLabel.text = cancelLabel;
        modal.SetActive(true);
        modal.transform.SetAsLastSibling();
    }

    // Closes the pop-up as if "cancel" was chosen (used by the phone's back button).
    public void CloseModal()
    {
        var action = modalCancelAction;
        HideModal();
        action?.Invoke();
    }

    public void ShowToast(string message)
    {
        toastText.text = message;
        toast.SetActive(true);
        toast.transform.SetAsLastSibling();
        if (toastRoutine != null)
            StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(HideToastLater());
    }

    IEnumerator HideToastLater()
    {
        yield return new WaitForSecondsRealtime(ToastSeconds);
        toast.SetActive(false);
        toastRoutine = null;
    }

    void HideModal()
    {
        modal.SetActive(false);
        modalOkAction = null;
        modalCancelAction = null;
    }

    // ---------- Building screens ----------

    void BuildHome(Transform parent)
    {
        var content = UIFactory.ScrollScreen("Home", parent, out homeScreen);
        screens.Add(homeScreen);

        var brand = UIFactory.Row(content, 12);
        if (logo != null)
        {
            var badge = UIFactory.Picture(brand, logo, 48f);
            UIFactory.Size(badge.transform.parent.GetComponent<Image>(), 48f, 48f);
        }
        else
        {
            var mark = UIFactory.Panel("Mark", brand, UITheme.Accent);
            UIFactory.Size(mark, 34f, 34f);
            UIFactory.Stretch(UIFactory.Text(mark.transform, "R", 20f, UITheme.Deep, true, FontStyles.Normal, TextAlignmentOptions.Center).rectTransform);
        }
        UIFactory.Text(brand, "RUGA", 18f, UITheme.Ink, true);
        UIFactory.Spacer(brand, -1f, 1f);
        UIFactory.RoundButton(brand, Icons.Settings, "•••", () => Raise(UIAction.OpenSettings), 40f);

        UIFactory.Spacer(content, 6f);
        UIFactory.Text(content, "Every painting starts\nwith a little guidance.", 27f, UITheme.Ink, true);
        UIFactory.Text(content, "Place a guide on your canvas.\nMake the next brushstroke your own.", 14f, UITheme.Muted);

        heroImage = UIFactory.Picture(content, null, 235f);
        var pill = UIFactory.Panel("Label", heroImage.transform.parent, new Color(0.09f, 0.16f, 0.11f, 0.86f));
        pill.rectTransform.anchorMin = pill.rectTransform.anchorMax = Vector2.zero;
        pill.rectTransform.pivot = Vector2.zero;
        pill.rectTransform.anchoredPosition = new Vector2(13f, 13f);
        var pillLayout = UIFactory.Horizontal(pill);
        pillLayout.padding = new RectOffset(10, 10, 6, 6);
        pill.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        pill.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        heroLabel = UIFactory.Text(pill.transform, "", 10f, UITheme.Ink);

        UIFactory.Button(content, "Choose a painting", ButtonStyle.Primary, () => Raise(UIAction.ChoosePainting), UITheme.ButtonHeight, Icons.ArrowRight, true);
        UIFactory.Button(content, "How it works", ButtonStyle.Secondary, () => Raise(UIAction.HowItWorks), UITheme.ButtonHeight, Icons.Info);
        UIFactory.Text(content, "Warm-ups · Paintings · Your own pace", 12f, UITheme.Muted, false, FontStyles.Normal, TextAlignmentOptions.Center);

        UIFactory.Spacer(content, 6f);
        var header = UIFactory.Row(content, 6);
        UIFactory.Icon(header, Icons.History, 14f, UITheme.Muted);
        UIFactory.Micro(header, "Recent sessions");
        recentHeader = header.gameObject;
        recentList = UIFactory.Column(content, 8);
    }

    void AddRecentRow(SessionRecord record)
    {
        var card = UIFactory.Panel("Session", recentList, UITheme.Card);
        UIFactory.Vertical(card, 14, 10, 2);

        var top = UIFactory.Row(card.transform, 8);
        UIFactory.Size(UIFactory.Text(top, record.paintingName, 14f, UITheme.Ink, false, FontStyles.Bold), -1f, -1f, 1f);
        UIFactory.Text(top, $"{record.stages.Count}/{record.stageCount} · {UIFactory.FormatDuration(record.totalSeconds)}", 12f, UITheme.Muted);

        var when = record.StartTime == DateTime.MinValue
            ? ""
            : record.StartTime.ToLocalTime().ToString("d MMM, HH:mm", CultureInfo.InvariantCulture) + " · ";
        UIFactory.Text(card.transform, when + (record.completed ? "Completed" : "In progress"), 12f, UITheme.Muted);
    }

    void BuildCatalog(Transform parent)
    {
        var content = UIFactory.ScrollScreen("Catalog", parent, out catalogScreen);
        screens.Add(catalogScreen);

        var top = UIFactory.Row(content, 12);
        UIFactory.RoundButton(top, Icons.ArrowLeft, "<", () => Raise(UIAction.Back));
        UIFactory.Text(top, "Your next painting", 17f, UITheme.Ink, false, FontStyles.Bold);

        UIFactory.Spacer(content, 4f);
        UIFactory.Micro(content, "Built-in collection");
        UIFactory.Text(content, "Start with something simple.", 23f, UITheme.Ink, true);
        UIFactory.Text(content, "Choose a painting to see its guide. New to painting? Start with a warm-up.", 13f, UITheme.Muted);

        catalogList = UIFactory.Column(content, 12);

        var tip = UIFactory.Panel("Tip", content, UITheme.Background);
        UIFactory.AddBorder(tip, UITheme.Line);
        UIFactory.Vertical(tip, 14, 12, 0);
        UIFactory.Text(tip.transform, "Set your canvas flat on a well-lit table. A phone stand helps keep both hands free.", 12f, UITheme.Muted);

        var use = UIFactory.Button(content, "Use this painting", ButtonStyle.Primary, () => Raise(UIAction.UsePainting), UITheme.ButtonHeight, Icons.ArrowRight, true);
        useButtonLabel = UIFactory.LabelOf(use);
    }

    void AddCatalogSection(string title, IEnumerable<Painting> paintings)
    {
        var list = paintings.ToList();
        if (list.Count == 0)
            return;

        UIFactory.Micro(catalogList, title);
        foreach (var painting in list)
        {
            var card = UIFactory.Panel("Card", catalogList, UITheme.Card, true, true);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card;
            var p = painting;
            button.onClick.AddListener(() => OnPaintingSelected?.Invoke(p));

            var layout = UIFactory.Horizontal(card, 14);
            layout.padding = new RectOffset(9, 9, 9, 9);

            var thumb = UIFactory.Picture(card.transform, painting.FinalImage, 96f);
            UIFactory.Size(thumb.transform.parent.GetComponent<Image>(), 96f, 96f);

            var text = UIFactory.Column(card.transform, 3);
            UIFactory.Size(text, -1f, -1f, 1f);
            UIFactory.Text(text, painting.Name, 15f, UITheme.Ink, false, FontStyles.Bold);
            UIFactory.Text(text, painting.Label, 12f, UITheme.Muted);
            var stages = painting.Stages.Count == 1 ? "1 stage" : $"{painting.Stages.Count} stages";
            UIFactory.Text(text, $"{stages} · Beginner", 12f, UITheme.Muted);

            var border = UIFactory.AddBorder(card, UITheme.Accent, 2f);
            catalogCards[painting] = (card, border);
        }
    }

    void BuildSettings(Transform parent)
    {
        var content = UIFactory.ScrollScreen("Settings", parent, out settingsScreen);
        screens.Add(settingsScreen);

        var top = UIFactory.Row(content, 12);
        UIFactory.RoundButton(top, Icons.ArrowLeft, "<", () => Raise(UIAction.CloseSettings));
        UIFactory.Text(top, "Settings", 17f, UITheme.Ink, false, FontStyles.Bold);

        UIFactory.Spacer(content, 8f);
        UIFactory.Micro(content, "Default guide colour");
        settingsDots = BuildColourDots(UIFactory.Row(content, 14), 40f);

        UIFactory.Spacer(content, 8f);
        var opacityRow = UIFactory.Row(content, 8);
        UIFactory.Size(UIFactory.Micro(opacityRow, "Default guide opacity"), -1f, -1f, 1f);
        settingsOpacityValue = UIFactory.Text(opacityRow, "50%", 13f, UITheme.Ink);
        settingsOpacity = UIFactory.Slider(content, 10f, 100f, 50f, v =>
        {
            settingsOpacityValue.text = $"{Mathf.RoundToInt(v)}%";
            OnOpacityChanged?.Invoke(v / 100f);
        });
        UIFactory.Text(content, "Used each time you start a painting. You can still change both while you paint.", 12f, UITheme.Muted);

        UIFactory.Spacer(content, 8f);
        var privacy = UIFactory.Panel("Privacy", content, UITheme.Card);
        UIFactory.Vertical(privacy, 14, 12, 4);
        UIFactory.Text(privacy.transform, "Privacy", 14f, UITheme.Ink, false, FontStyles.Bold);
        UIFactory.Text(privacy.transform, "RUGA works offline. The camera only shows your canvas on screen; nothing is recorded, saved or shared.", 12f, UITheme.Muted);

        UIFactory.Button(content, "Credits & licences", ButtonStyle.Secondary, () => Raise(UIAction.OpenCredits), UITheme.SmallButtonHeight, Icons.Info);

        UIFactory.Spacer(content, 8f);
        UIFactory.Button(content, "Done", ButtonStyle.Primary, () => Raise(UIAction.CloseSettings), UITheme.ButtonHeight, Icons.Check);
    }

    // Lists every third-party resource with its licence, as the font and icon licences require.
    void BuildCredits(Transform parent)
    {
        var content = UIFactory.ScrollScreen("Credits", parent, out creditsScreen);
        screens.Add(creditsScreen);

        var top = UIFactory.Row(content, 12);
        UIFactory.RoundButton(top, Icons.ArrowLeft, "<", () => Raise(UIAction.CloseCredits));
        UIFactory.Text(top, "Credits & licences", 17f, UITheme.Ink, false, FontStyles.Bold);
        UIFactory.Text(content, "RUGA is made with these free resources. Thank you to their makers.", 13f, UITheme.Muted);

        foreach (var (name, details, licenceFile) in Credits)
        {
            var card = UIFactory.Panel("Credit", content, UITheme.Card);
            UIFactory.Vertical(card, 14, 12, 6);
            UIFactory.Text(card.transform, name, 14f, UITheme.Ink, false, FontStyles.Bold);
            UIFactory.Text(card.transform, details, 12f, UITheme.Muted);

            var licence = licenceFile != null ? Resources.Load<TextAsset>($"Licences/{licenceFile}") : null;
            if (licence != null)
                UIFactory.Text(card.transform, licence.text, 9f, UITheme.Muted);
        }
    }

    void BuildSummary(Transform parent)
    {
        var content = UIFactory.ScrollScreen("Summary", parent, out summaryScreen);
        screens.Add(summaryScreen);

        var top = UIFactory.Row(content, 8);
        UIFactory.Size(UIFactory.Micro(top, "Session summary"), -1f, -1f, 1f);
        summaryPill = UIFactory.Text(top, "", 11f, UITheme.Accent);

        var tickRow = UIFactory.Row(content);
        var circle = UIFactory.Panel("Done", tickRow, UITheme.Accent, false);
        circle.sprite = UITheme.Circle;
        UIFactory.Size(circle, 46f, 46f);
        UIFactory.Stretch(UIFactory.Icon(circle.transform, Icons.Check, 24f, UITheme.Deep).rectTransform);

        UIFactory.Text(content, "One stage at a time.\nYou made it.", 26f, UITheme.Ink, true);
        summaryName = UIFactory.Text(content, "", 13f, UITheme.Muted);
        summaryPicture = UIFactory.Picture(content, null, 170f);

        var metric = UIFactory.Panel("Total", content, UITheme.Card);
        var metricLayout = UIFactory.Horizontal(metric, 8);
        metricLayout.padding = new RectOffset(14, 14, 14, 14);
        UIFactory.Size(UIFactory.Text(metric.transform, "Total time", 13f, UITheme.Muted), -1f, -1f, 1f);
        summaryTotal = UIFactory.Text(metric.transform, "", 23f, UITheme.Ink, false, FontStyles.Bold);

        summaryTimes = UIFactory.Column(content, 0);
        summarySaveStatus = UIFactory.Text(content, "", 11f, UITheme.Muted, false, FontStyles.Normal, TextAlignmentOptions.Center);

        UIFactory.Button(content, "Try another painting", ButtonStyle.Primary, () => Raise(UIAction.AnotherPainting), UITheme.ButtonHeight, Icons.ArrowRight, true);
        UIFactory.Button(content, "Back to home", ButtonStyle.Link, () => Raise(UIAction.GoHome), 40f);
    }

    void BuildWorkspace(Transform parent)
    {
        var root = UIFactory.Stretch(UIFactory.Rect("Workspace", parent));
        workspaceScreen = root.gameObject;
        screens.Add(workspaceScreen);

        // Top bar
        var top = UIFactory.Panel("TopBar", root, UITheme.PanelBackground, true, true);
        var topRect = top.rectTransform;
        topRect.anchorMin = new Vector2(0f, 1f);
        topRect.anchorMax = new Vector2(1f, 1f);
        topRect.pivot = new Vector2(0.5f, 1f);
        topRect.anchoredPosition = new Vector2(0f, -8f);
        topRect.sizeDelta = new Vector2(-16f, 56f);
        var topLayout = UIFactory.Horizontal(top, 10);
        topLayout.padding = new RectOffset(8, 10, 6, 6);
        UIFactory.RoundButton(top.transform, Icons.ArrowLeft, "<", () => Raise(UIAction.Back), 40f);
        workspaceTitle = UIFactory.Text(top.transform, "", 15f, UITheme.Ink, false, FontStyles.Bold);
        UIFactory.Size(workspaceTitle, -1f, -1f, 1f);
        var tag = UIFactory.Panel("ModeTag", top.transform, new Color(UITheme.Accent.r, UITheme.Accent.g, UITheme.Accent.b, 0.08f));
        UIFactory.AddBorder(tag, new Color(UITheme.Accent.r, UITheme.Accent.g, UITheme.Accent.b, 0.3f), 1f);
        UIFactory.Horizontal(tag).padding = new RectOffset(10, 10, 6, 6);
        modeTag = UIFactory.Text(tag.transform, "", 11f, UITheme.Accent);

        // Tracking-lost warning, under the top bar
        var warning = UIFactory.Panel("TrackingWarning", root, UITheme.Card);
        UIFactory.AddBorder(warning, UITheme.Warning, 1.5f);
        var warningRect = warning.rectTransform;
        warningRect.anchorMin = new Vector2(0f, 1f);
        warningRect.anchorMax = new Vector2(1f, 1f);
        warningRect.pivot = new Vector2(0.5f, 1f);
        warningRect.offsetMin = new Vector2(16f, 0f);
        warningRect.offsetMax = new Vector2(-16f, -74f);
        UIFactory.Vertical(warning, 14, 12, 4);
        warning.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var warningTitle = UIFactory.Row(warning.transform, 6);
        UIFactory.Icon(warningTitle, Icons.Warning, 17f, UITheme.Warning);
        UIFactory.Text(warningTitle, "Tracking lost", 14f, UITheme.Warning, false, FontStyles.Bold);
        UIFactory.Text(warning.transform, "Hold the phone steady and keep the canvas and table in view. Check the guide still lines up.", 12f, UITheme.Ink);
        trackingWarning = warning.gameObject;

        // Bottom sheet holding the Scan, Align and Paint panels
        var sheet = UIFactory.Panel("BottomSheet", root, UITheme.PanelBackground, true, true);
        var sheetRect = sheet.rectTransform;
        sheetRect.anchorMin = new Vector2(0f, 0f);
        sheetRect.anchorMax = new Vector2(1f, 0f);
        sheetRect.pivot = new Vector2(0.5f, 0f);
        sheetRect.anchoredPosition = new Vector2(0f, 8f);
        sheetRect.sizeDelta = new Vector2(-16f, 0f);
        UIFactory.Vertical(sheet, 20, 18, 10);
        sheet.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        BuildScanPanel(sheet.transform);
        BuildAlignPanel(sheet.transform);
        BuildPaintPanel(sheet.transform);
    }

    void BuildScanPanel(Transform sheet)
    {
        var panel = UIFactory.Column(sheet, 8, "ScanPanel");
        scanPanel = panel.gameObject;
        var heading = UIFactory.Row(panel, 6);
        UIFactory.Icon(heading, Icons.Camera, 14f, UITheme.Muted);
        UIFactory.Micro(heading, "01 / Find your canvas");
        scanTitle = UIFactory.Text(panel, "", 19f, UITheme.Ink, true);
        scanText = UIFactory.Text(panel, "", 13f, UITheme.Muted);
        UIFactory.Text(panel, "Tip: good light and a patterned cloth under the canvas help.", 11f, UITheme.Muted);
    }

    void BuildAlignPanel(Transform sheet)
    {
        var panel = UIFactory.Column(sheet, 10, "AlignPanel");
        alignPanel = panel.gameObject;
        UIFactory.Micro(panel, "02 / Make it fit");
        UIFactory.Text(panel, "Align your guide.", 19f, UITheme.Ink, true);
        UIFactory.Text(panel, "Drag to move it. Twist with two fingers to rotate. Then type your canvas size.", 13f, UITheme.Muted);

        var fields = UIFactory.Row(panel, 10);
        widthField = SizeField(fields, "Width (cm)");
        heightField = SizeField(fields, "Height (cm)");

        var rotate = UIFactory.Row(panel, 8);
        var rotateButtons = new[]
        {
            ("Left", Icons.RotateLeft, UIAction.RotateLeft),
            ("Right", Icons.RotateRight, UIAction.RotateRight),
            ("Reset", Icons.Recenter, UIAction.ResetGuide)
        };
        foreach (var (label, icon, action) in rotateButtons)
        {
            var a = action;
            var b = UIFactory.Button(rotate, label, ButtonStyle.Secondary, () => Raise(a), UITheme.SmallButtonHeight, icon);
            UIFactory.Size(b, -1f, -1f, 1f);
            UIFactory.LabelOf(b).fontSize = 13f;
        }

        var lockButton = UIFactory.Button(panel, "Lock guide & start", ButtonStyle.Primary, () => Raise(UIAction.Lock), UITheme.ButtonHeight, Icons.Lock);
        lockLabel = UIFactory.LabelOf(lockButton);
    }

    TMP_InputField SizeField(Transform row, string label)
    {
        var column = UIFactory.Column(row, 4);
        UIFactory.Size(column, -1f, -1f, 1f);
        UIFactory.Text(column, label, 11f, UITheme.Muted);
        return UIFactory.NumberField(column, "", _ => ReportSize());
    }

    void ReportSize()
    {
        float.TryParse(widthField.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var w);
        float.TryParse(heightField.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var h);
        OnSizeEntered?.Invoke(new Vector2(w, h));
    }

    void BuildPaintPanel(Transform sheet)
    {
        var panel = UIFactory.Column(sheet, 9, "PaintPanel");
        paintPanel = panel.gameObject;

        progressRow = UIFactory.Row(panel, 5);
        UIFactory.Size(progressRow, -1f, 3f);
        stepLabel = UIFactory.Micro(panel, "");
        stageTitle = UIFactory.Text(panel, "", 19f, UITheme.Ink, true);
        stageInstruction = UIFactory.Text(panel, "", 13f, UITheme.Muted);

        var opacityRow = UIFactory.Row(panel, 8);
        UIFactory.Size(UIFactory.Text(opacityRow, "Guide opacity", 12f, UITheme.Ink), -1f, -1f, 1f);
        opacityValue = UIFactory.Text(opacityRow, "50%", 12f, UITheme.Ink);
        opacitySlider = UIFactory.Slider(panel, 10f, 100f, 50f, v =>
        {
            opacityValue.text = $"{Mathf.RoundToInt(v)}%";
            OnOpacityChanged?.Invoke(v / 100f);
        });

        var toggles = UIFactory.Row(panel, 7);
        outlineToggle = ToggleButton(toggles, "Outline", Icons.Outline, UIAction.ToggleOutline);
        gridToggle = ToggleButton(toggles, "Grid", Icons.Grid, UIAction.ToggleGrid);
        UIFactory.Spacer(toggles, -1f, 1f);
        paintDots = BuildColourDots(toggles, 32f);

        var nav = UIFactory.Row(panel, 8);
        previousButton = UIFactory.Button(nav, "Previous", ButtonStyle.Secondary, () => Raise(UIAction.PreviousStage), UITheme.SmallButtonHeight, Icons.ArrowLeft);
        UIFactory.Size(previousButton, -1f, -1f, 1f);
        var next = UIFactory.Button(nav, "Next stage", ButtonStyle.Primary, () => Raise(UIAction.NextStage), UITheme.SmallButtonHeight, Icons.ArrowRight, true);
        UIFactory.Size(next, -1f, -1f, 1f);
        nextLabel = UIFactory.LabelOf(next);
        nextIcon = UIFactory.IconOf(next);

        var links = UIFactory.Row(panel, 4);
        links.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
        UIFactory.Size(UIFactory.Button(links, "Unlock & realign", ButtonStyle.Link, () => Raise(UIAction.Unlock), 40f, Icons.Unlock), 165f, 40f);
        var hide = UIFactory.Button(links, "Hide guide", ButtonStyle.Link, () => Raise(UIAction.ToggleHideGuide), 40f, Icons.EyeSlash);
        UIFactory.Size(hide, 135f, 40f);
        hideLabel = UIFactory.LabelOf(hide);
        hideIcon = UIFactory.IconOf(hide);
    }

    Button ToggleButton(Transform row, string label, string icon, UIAction action)
    {
        var button = UIFactory.Button(row, label, ButtonStyle.Secondary, () => Raise(action), 38f, icon);
        UIFactory.Size(button, 92f, 38f);
        UIFactory.LabelOf(button).fontSize = 12f;
        return button;
    }

    static void SetToggle(Button button, bool on)
    {
        button.GetComponent<Outline>().effectColor = on ? UITheme.Accent : UITheme.Line;
        foreach (var text in button.GetComponentsInChildren<TextMeshProUGUI>(true))
            text.color = on ? UITheme.Accent : UITheme.Ink;
    }

    Button[] BuildColourDots(Transform row, float size)
    {
        var dots = new Button[Colours.Length];
        for (int i = 0; i < Colours.Length; i++)
        {
            var colour = Colours[i];
            dots[i] = UIFactory.ColourDot(row, GuideColours.ToColor(colour), () => OnColourChanged?.Invoke(colour), size);
            UIFactory.AddBorder(dots[i].targetGraphic, Color.white, 2.5f).enabled = false;
        }

        // Each set of dots shows its own selection straight away.
        for (int i = 0; i < Colours.Length; i++)
        {
            var colour = Colours[i];
            dots[i].onClick.AddListener(() => SelectDot(dots, colour));
        }
        return dots;
    }

    static void SelectDot(Button[] dots, GuideColour selected)
    {
        for (int i = 0; i < dots.Length; i++)
            dots[i].GetComponent<Outline>().enabled = Colours[i] == selected;
    }

    void BuildToast(Transform canvas)
    {
        var panel = UIFactory.Panel("Toast", canvas, UITheme.ToastBackground);
        var rect = panel.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(20f, 0f);
        rect.offsetMax = new Vector2(-20f, -84f);
        UIFactory.Vertical(panel, 16, 12, 0);
        panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        panel.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
        toastText = UIFactory.Text(panel.transform, "", 13f, UITheme.Deep);
        toast = panel.gameObject;
        toast.SetActive(false);
    }

    void BuildModal(Transform canvas)
    {
        var dim = UIFactory.Panel("Modal", canvas, UITheme.Overlay, false, true);
        UIFactory.Stretch(dim.rectTransform);
        modal = dim.gameObject;

        var card = UIFactory.Panel("Card", dim.transform, new Color32(0x24, 0x2d, 0x26, 0xff), true, true);
        UIFactory.AddBorder(card, new Color32(0x54, 0x66, 0x4a, 0xff));
        var rect = card.rectTransform;
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(-40f, 0f);
        UIFactory.Vertical(card, 24, 24, 12);
        card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        modalTitle = UIFactory.Text(card.transform, "", 21f, UITheme.Ink, true);
        modalBody = UIFactory.Text(card.transform, "", 14f, new Color32(0xc2, 0xcc, 0xbe, 0xff));
        modalBody.richText = true;
        UIFactory.Spacer(card.transform, 4f);

        var ok = UIFactory.Button(card.transform, "OK", ButtonStyle.Primary, () =>
        {
            var action = modalOkAction;
            HideModal();
            action?.Invoke();
        });
        modalOkLabel = UIFactory.LabelOf(ok);

        var cancel = UIFactory.Button(card.transform, "Cancel", ButtonStyle.Secondary, CloseModal);
        modalCancel = cancel.gameObject;
        modalCancelLabel = UIFactory.LabelOf(cancel);

        modal.SetActive(false);
    }

    // ---------- Helpers ----------

    void ShowScreen(GameObject screen, bool opaque)
    {
        foreach (var s in screens)
            s.SetActive(s == screen);
        backdrop.SetActive(opaque);

        var scroll = screen.GetComponent<ScrollRect>();
        if (scroll != null)
            scroll.content.anchoredPosition = Vector2.zero;
    }

    void ShowSheetPanel(GameObject panel)
    {
        scanPanel.SetActive(panel == scanPanel);
        alignPanel.SetActive(panel == alignPanel);
        paintPanel.SetActive(panel == paintPanel);
    }

    void Raise(UIAction action) => OnAction?.Invoke(action);

    static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
            return;
        var go = new GameObject("EventSystem", typeof(EventSystem));
        go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    void WarnIfNoFont()
    {
        if (titleFont != null && bodyFont != null)
            return;
        var settings = TMP_Settings.instance;
        if (settings == null || TMP_Settings.defaultFontAsset == null)
            Debug.LogError("RUGA: No font for the UI. Run Window > TextMeshPro > Import TMP Essential Resources, or assign fonts on the UIManager.");
    }
}
