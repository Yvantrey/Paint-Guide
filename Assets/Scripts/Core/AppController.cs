using UnityEngine;
using UnityEngine.InputSystem;

// Starts RUGA: loads data, builds the UI, creates the states and connects manager events to them.
// This is the only script that wires the managers together.
public class AppController : MonoBehaviour
{
    [SerializeField] UIManager ui;
    [SerializeField] ARTrackingManager ar;
    [SerializeField] GuideManager guide;
    [SerializeField] GuideGestures gestures;
    [SerializeField] SessionManager session;
    [SerializeField] DataManager data;
    [SerializeField] Camera arCamera;

    AppStateMachine machine;

    AppStateBase Current => machine.Current;

    void Start()
    {
        data.Load();
        ui.Build();

        machine = new AppStateMachine();
        var context = new AppContext
        {
            UI = ui,
            AR = ar,
            Guide = guide,
            Session = session,
            Data = data,
            ARCamera = arCamera,
            Machine = machine
        };

        machine.Add(new HomeState(context));
        machine.Add(new CatalogState(context));
        machine.Add(new ScanningState(context));
        machine.Add(new PlacingState(context));
        machine.Add(new AligningState(context));
        machine.Add(new PaintingState(context));
        machine.Add(new ReferenceState(context));
        machine.Add(new SummaryState(context));

        // UI and gestures go to whichever state is active.
        ui.OnAction += a => Current.OnUIAction(a);
        ui.OnPaintingSelected += p => Current.OnPaintingSelected(p);
        ui.OnSizeEntered += s => Current.OnSizeEntered(s);
        ui.OnOpacityChanged += o => Current.OnOpacityChanged(o);
        ui.OnColourChanged += c => Current.OnColourChanged(c);
        gestures.OnTap += p => Current.OnTap(p);
        gestures.OnDragStart += p => Current.OnDragStart(p);
        gestures.OnDrag += p => Current.OnDrag(p);
        gestures.OnDragEnd += () => Current.OnDragEnd();
        gestures.OnTwist += d => Current.OnTwist(d);

        // Messages that apply in any state.
        ar.OnTrackingLost += () => ui.SetTrackingWarning(true);
        ar.OnTrackingRestored += () => ui.SetTrackingWarning(false);

        // Save after every completed stage.
        session.OnStageCompleted += record => context.LastSaveSucceeded = data.SaveSession(record);

        machine.ChangeState(AppState.Home);
    }

    void Update()
    {
        if (machine == null)
            return;

        Current.Tick();

        // Android's back button arrives as the Escape key.
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            if (ui.IsModalOpen)
                ui.CloseModal();
            else
                Current.OnUIAction(UIAction.Back);
        }
    }
}
