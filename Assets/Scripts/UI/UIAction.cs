// Every button press the UI can report. The current app state decides what each one does.
public enum UIAction
{
    ChoosePainting,
    HowItWorks,
    OpenSettings,
    CloseSettings,
    OpenCredits,
    CloseCredits,
    Back,
    UsePainting,
    UseReferenceMode,
    ViewReference,
    ShowGuideView,
    ShowFinishedView,
    Lock,
    RotateLeft,
    RotateRight,
    ResetGuide,
    NextStage,
    PreviousStage,
    Unlock,
    ToggleHideGuide,
    ToggleOutline,
    ToggleGrid,
    AnotherPainting,
    GoHome
}

public enum ScanStep
{
    Preparing,
    Scanning,
    SurfaceFound
}
