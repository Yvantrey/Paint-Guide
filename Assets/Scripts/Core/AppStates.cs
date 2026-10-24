using System.Linq;
using UnityEngine;

// Base for every app state. Each state decides what the UI, AR and guide should do
// while it is active, and how to react to buttons and gestures.
public abstract class AppStateBase
{
    protected readonly AppContext Ctx;

    protected AppStateBase(AppContext context) => Ctx = context;

    public abstract AppState Id { get; }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Tick() { }

    public virtual void OnUIAction(UIAction action) { }
    public virtual void OnPaintingSelected(Painting painting) { }
    public virtual void OnSizeEntered(Vector2 sizeCm) { }
    public virtual void OnOpacityChanged(float opacity) { }
    public virtual void OnColourChanged(GuideColour colour) { }

    public virtual void OnTap(Vector2 screenPosition) { }
    public virtual void OnDragStart(Vector2 screenPosition) { }
    public virtual void OnDrag(Vector2 screenPosition) { }
    public virtual void OnDragEnd() { }
    public virtual void OnTwist(float degrees) { }

    protected void Go(AppState state) => Ctx.Machine.ChangeState(state);

    protected void ConfirmLeaveWorkspace()
    {
        var body = Ctx.Session.IsActive && Ctx.Session.Record.stages.Count > 0
            ? "Your completed stages are saved. You can start again any time."
            : "You can choose a painting and start again any time.";
        Ctx.UI.ShowModal("Leave this session?", body, "Leave session", () => Go(AppState.Home), "Keep painting");
    }
}

public class HomeState : AppStateBase
{
    // Settings and Credits are small sub-pages of Home rather than app states of their own.
    bool inSettings;
    bool inCredits;

    public HomeState(AppContext context) : base(context) { }
    public override AppState Id => AppState.Home;

    public override void Enter()
    {
        Ctx.AR.StopAR();
        Ctx.Guide.Clear();
        Ctx.Session.End();
        Screen.sleepTimeout = SleepTimeout.SystemSetting;
        ShowHome();
    }

    void ShowHome()
    {
        if (inSettings)
            Ctx.Data.SavePreferences(); // the opacity slider changes often, so save once when leaving
        inSettings = false;
        inCredits = false;
        var hero = Ctx.Data.Paintings.FirstOrDefault(p => p.Category == PaintingCategory.Painting)
                   ?? Ctx.Data.Paintings.FirstOrDefault();
        Ctx.UI.ShowHome(Ctx.Data.Sessions, hero);
    }

    public override void OnUIAction(UIAction action)
    {
        switch (action)
        {
            case UIAction.ChoosePainting:
                Go(AppState.Catalog);
                break;
            case UIAction.HowItWorks:
                Ctx.UI.ShowModal("A guide, at your pace.",
                    "1. Choose a warm-up or a painting.\n" +
                    "2. Scan the table and tap to place the guide.\n" +
                    "3. Drag, rotate and size it to your canvas, then lock it.\n" +
                    "4. Paint one stage at a time, then review your time.\n\n" +
                    "A phone stand keeps both hands free. RUGA never records or saves your camera.",
                    "Got it", null);
                break;
            case UIAction.OpenSettings:
                inSettings = true;
                Ctx.UI.ShowSettings(Ctx.Data.Preferences);
                break;
            case UIAction.CloseSettings:
                ShowHome();
                break;
            case UIAction.OpenCredits:
                inCredits = true;
                Ctx.UI.ShowCredits();
                break;
            case UIAction.CloseCredits:
                inCredits = false;
                Ctx.UI.ShowSettings(Ctx.Data.Preferences);
                break;
            case UIAction.Back:
                if (inCredits)
                    OnUIAction(UIAction.CloseCredits);
                else if (inSettings)
                    ShowHome();
                break;
        }
    }

    // On the settings screen, colour and opacity change the saved defaults.
    public override void OnColourChanged(GuideColour colour)
    {
        Ctx.Data.Preferences.GuideColour = colour;
        Ctx.Data.SavePreferences();
    }

    public override void OnOpacityChanged(float opacity) => Ctx.Data.Preferences.DefaultOpacity = opacity;
}

public class CatalogState : AppStateBase
{
    public CatalogState(AppContext context) : base(context) { }
    public override AppState Id => AppState.Catalog;

    public override void Enter()
    {
        if (Ctx.SelectedPainting == null)
            Ctx.SelectedPainting = Ctx.Data.Paintings.FirstOrDefault();
        Ctx.UI.ShowCatalog(Ctx.Data.Paintings, Ctx.SelectedPainting);
    }

    public override void OnPaintingSelected(Painting painting)
    {
        Ctx.SelectedPainting = painting;
        Ctx.UI.SetCatalogSelection(painting);
    }

    public override void OnUIAction(UIAction action)
    {
        switch (action)
        {
            case UIAction.UsePainting:
                if (Ctx.SelectedPainting == null)
                    return;
                // Each new painting starts from the saved defaults.
                var prefs = Ctx.Data.Preferences;
                Ctx.Guide.SetColour(prefs.GuideColour);
                Ctx.Guide.SetOpacity(prefs.DefaultOpacity);
                Ctx.Guide.SetOutlineVisible(true);
                Ctx.Guide.SetGridVisible(false);
                Ctx.Guide.SetHidden(false);
                Go(AppState.Scanning);
                break;
            case UIAction.Back:
                Go(AppState.Home);
                break;
        }
    }
}

public class ScanningState : AppStateBase
{
    // If no surface is found after this long, show a hint.
    const float HintAfterSeconds = 12f;

    bool scanning;
    float scanSeconds;
    bool hintShown;

    public ScanningState(AppContext context) : base(context) { }
    public override AppState Id => AppState.Scanning;

    public override void Enter()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Ctx.Guide.Clear();
        Ctx.UI.ShowWorkspace(Ctx.SelectedPainting.Name);
        Ctx.UI.SetModeTag("Scan surface");
        Ctx.UI.ShowScanPanel(ScanStep.Preparing);
        Ctx.AR.OnSurfaceFound += SurfaceFound;
        scanning = false;
        scanSeconds = 0f;
        hintShown = false;

        if (CameraPermission.IsGranted)
            StartAR();
        else
            Ctx.UI.ShowModal("RUGA needs your camera",
                "The camera shows your canvas so the guide can sit on top of it. Nothing is recorded, saved or shared.",
                "Continue", StartAR, "Not now", () => Go(AppState.Home));
    }

    public override void Exit()
    {
        Ctx.AR.OnSurfaceFound -= SurfaceFound;
        scanning = false;
    }

    void StartAR() => Ctx.AR.StartAR(OnARReady);

    void OnARReady(ARReadiness readiness)
    {
        if (Ctx.Machine.Current != this)
            return;

        switch (readiness)
        {
            case ARReadiness.Ready:
                Ctx.AR.ResetAR();
                Ctx.AR.SetTrackingWatch(false);
                Ctx.AR.SetPlaneDetection(true);
                Ctx.UI.ShowScanPanel(ScanStep.Scanning);
                scanning = true;
                break;
            case ARReadiness.Unsupported:
                Ctx.UI.ShowModal("This phone can’t run AR",
                    "RUGA needs Google’s ARCore, which isn’t available on this phone. You can still look through the paintings, but placing a guide needs an ARCore-supported phone.",
                    "Back to home", () => Go(AppState.Home), null, () => Go(AppState.Home));
                break;
            case ARReadiness.InstallFailed:
                Ctx.UI.ShowModal("One more app is needed",
                    "RUGA needs Google Play Services for AR. Install or update it from the Play Store (this needs the internet once), then try again.",
                    "Try again", StartAR, "Back to home", () => Go(AppState.Home));
                break;
            case ARReadiness.PermissionDenied:
                Ctx.UI.ShowModal("Camera access is off",
                    "RUGA uses the camera only to show your canvas on screen. Nothing is recorded or saved.\n\n" +
                    "To continue, allow it in <b>Settings › Apps › RUGA › Permissions › Camera</b>, then try again.",
                    "Try again", StartAR, "Back to home", () => Go(AppState.Home));
                break;
        }
    }

    void SurfaceFound()
    {
        if (scanning)
            Go(AppState.Placing);
    }

    public override void Tick()
    {
        if (!scanning || hintShown)
            return;

        scanSeconds += Time.unscaledDeltaTime;
        if (scanSeconds >= HintAfterSeconds)
        {
            hintShown = true;
            Ctx.UI.ShowToast("Still looking… Try more light, or a patterned cloth under the canvas.");
        }
    }

    public override void OnUIAction(UIAction action)
    {
        if (action == UIAction.Back)
            ConfirmLeaveWorkspace();
    }
}

public class PlacingState : AppStateBase
{
    public PlacingState(AppContext context) : base(context) { }
    public override AppState Id => AppState.Placing;

    public override void Enter()
    {
        Ctx.UI.SetModeTag("Surface found");
        Ctx.UI.ShowScanPanel(ScanStep.SurfaceFound);
        Ctx.AR.SetPlaneDetection(true);
        Ctx.AR.SetTrackingWatch(true);
    }

    public override void OnTap(Vector2 screenPosition)
    {
        if (!Ctx.AR.TryGetSurfacePose(screenPosition, Ctx.ARCamera.transform, out var pose, out var plane))
        {
            Ctx.UI.ShowToast("Tap on the highlighted surface.");
            return;
        }

        var anchor = Ctx.AR.CreateAnchor(plane, pose);
        Ctx.AR.WatchAnchor(anchor);
        var stageIndex = Ctx.Session.IsActive ? Ctx.Session.StageIndex : 0;
        Ctx.Guide.SetStageImage(Ctx.SelectedPainting.Stages[stageIndex].GuideImage);
        Ctx.Guide.Place(anchor.transform);
        Go(AppState.Aligning);
    }

    public override void OnUIAction(UIAction action)
    {
        if (action == UIAction.Back)
            ConfirmLeaveWorkspace();
    }
}

public class AligningState : AppStateBase
{
    const float RotateStepDegrees = 2f;

    bool dragging;
    Vector3 dragOffset;

    public AligningState(AppContext context) : base(context) { }
    public override AppState Id => AppState.Aligning;

    public override void Enter()
    {
        Ctx.Session.PauseTimer();
        Ctx.Guide.SetLocked(false);
        Ctx.Guide.SetHidden(false);
        Ctx.AR.SetPlaneDetection(false); // the guide is placed; surface outlines would only clutter the view
        Ctx.UI.SetModeTag("Align guide");
        Ctx.UI.ShowAlignPanel(Ctx.Guide.SizeCm, Ctx.Session.IsActive);
    }

    public override void Exit() => dragging = false;

    public override void OnDragStart(Vector2 screenPosition)
    {
        dragging = Ctx.Guide.TryGetSurfacePoint(Ctx.ARCamera.ScreenPointToRay(screenPosition), out var point);
        if (dragging)
            dragOffset = Ctx.Guide.Position - point;
    }

    public override void OnDrag(Vector2 screenPosition)
    {
        if (dragging && Ctx.Guide.TryGetSurfacePoint(Ctx.ARCamera.ScreenPointToRay(screenPosition), out var point))
            Ctx.Guide.MoveTo(point + dragOffset);
    }

    public override void OnDragEnd() => dragging = false;

    // Twisting counter-clockwise on screen turns the guide counter-clockwise as seen from above.
    public override void OnTwist(float degrees) => Ctx.Guide.Rotate(-degrees);

    public override void OnSizeEntered(Vector2 sizeCm)
    {
        // An empty or invalid field keeps the current value.
        var current = Ctx.Guide.SizeCm;
        Ctx.Guide.SetSizeCm(new Vector2(sizeCm.x > 0f ? sizeCm.x : current.x, sizeCm.y > 0f ? sizeCm.y : current.y));
        Ctx.UI.SetSizeFields(Ctx.Guide.SizeCm);
    }

    public override void OnUIAction(UIAction action)
    {
        switch (action)
        {
            case UIAction.RotateLeft:
                Ctx.Guide.Rotate(-RotateStepDegrees);
                break;
            case UIAction.RotateRight:
                Ctx.Guide.Rotate(RotateStepDegrees);
                break;
            case UIAction.ResetGuide:
                Ctx.Guide.ResetPlacement();
                Ctx.UI.ShowToast("Guide moved back to where you tapped.");
                break;
            case UIAction.Lock:
                Go(AppState.Painting);
                break;
            case UIAction.Back:
                ConfirmLeaveWorkspace();
                break;
        }
    }
}

public class PaintingState : AppStateBase
{
    public PaintingState(AppContext context) : base(context) { }
    public override AppState Id => AppState.Painting;

    public override void Enter()
    {
        Ctx.Session.OnStageChanged += ShowStage;
        Ctx.Session.OnSessionCompleted += Completed;

        Ctx.Guide.SetLocked(true);
        Ctx.UI.SetModeTag("Guide locked");
        Ctx.UI.ShowPaintPanel();
        RefreshControls();

        if (Ctx.Session.IsActive && Ctx.Session.Painting == Ctx.SelectedPainting)
            ShowStage(Ctx.Session.StageIndex);
        else
            Ctx.Session.Begin(Ctx.SelectedPainting); // raises OnStageChanged for stage 1

        Ctx.Session.StartTimer();
    }

    public override void Exit()
    {
        Ctx.Session.OnStageChanged -= ShowStage;
        Ctx.Session.OnSessionCompleted -= Completed;
        Ctx.Session.PauseTimer();
    }

    void ShowStage(int index)
    {
        var painting = Ctx.Session.Painting;
        Ctx.UI.ShowStage(painting, index);
        Ctx.Guide.SetStageImage(painting.Stages[index].GuideImage);
    }

    void Completed(SessionRecord record)
    {
        Ctx.LastRecord = record;
        Go(AppState.Summary);
    }

    void RefreshControls()
    {
        var g = Ctx.Guide;
        Ctx.UI.SetGuideControls(g.Opacity, g.Colour, g.OutlineVisible, g.GridVisible, g.Hidden);
    }

    public override void OnOpacityChanged(float opacity) => Ctx.Guide.SetOpacity(opacity);

    // While painting, the chosen colour is also remembered for next time.
    public override void OnColourChanged(GuideColour colour)
    {
        Ctx.Guide.SetColour(colour);
        Ctx.Data.Preferences.GuideColour = colour;
        Ctx.Data.SavePreferences();
    }

    public override void OnUIAction(UIAction action)
    {
        switch (action)
        {
            case UIAction.NextStage:
                Ctx.Session.Next();
                break;
            case UIAction.PreviousStage:
                Ctx.Session.Previous();
                break;
            case UIAction.Unlock:
                Go(AppState.Aligning);
                break;
            case UIAction.ToggleHideGuide:
                Ctx.Guide.SetHidden(!Ctx.Guide.Hidden);
                RefreshControls();
                break;
            case UIAction.ToggleOutline:
                Ctx.Guide.SetOutlineVisible(!Ctx.Guide.OutlineVisible);
                RefreshControls();
                break;
            case UIAction.ToggleGrid:
                Ctx.Guide.SetGridVisible(!Ctx.Guide.GridVisible);
                RefreshControls();
                break;
            case UIAction.Back:
                ConfirmLeaveWorkspace();
                break;
        }
    }
}

public class SummaryState : AppStateBase
{
    public SummaryState(AppContext context) : base(context) { }
    public override AppState Id => AppState.Summary;

    public override void Enter()
    {
        var painting = Ctx.Session.Painting;
        Ctx.AR.StopAR();
        Ctx.Guide.Clear();
        Ctx.Session.End();
        Screen.sleepTimeout = SleepTimeout.SystemSetting;
        Ctx.UI.ShowSummary(painting, Ctx.LastRecord, Ctx.LastSaveSucceeded);
    }

    public override void OnUIAction(UIAction action)
    {
        switch (action)
        {
            case UIAction.AnotherPainting:
                Go(AppState.Catalog);
                break;
            case UIAction.GoHome:
            case UIAction.Back:
                Go(AppState.Home);
                break;
        }
    }
}
