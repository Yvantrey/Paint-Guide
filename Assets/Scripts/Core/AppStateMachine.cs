using System;
using System.Collections.Generic;
using UnityEngine;

public enum AppState
{
    Home,
    Catalog,
    Scanning,
    Placing,
    Aligning,
    Painting,
    Reference,
    Summary
}

// Everything the states need. Only the states and AppController see all the managers;
// the managers themselves never reference each other.
public class AppContext
{
    public UIManager UI;
    public ARTrackingManager AR;
    public GuideManager Guide;
    public SessionManager Session;
    public DataManager Data;
    public Camera ARCamera;
    public AppStateMachine Machine;

    public Painting SelectedPainting;
    public SessionRecord LastRecord;
    public bool LastSaveSucceeded = true;
}

// State pattern: holds the current state and switches between them.
public class AppStateMachine
{
    readonly Dictionary<AppState, AppStateBase> states = new Dictionary<AppState, AppStateBase>();
    bool changing;
    AppState? pending;

    public AppStateBase Current { get; private set; }
    public event Action<AppState> OnStateChanged;

    public void Add(AppStateBase state) => states[state.Id] = state;

    public void ChangeState(AppState next)
    {
        // A state may ask for another change while entering; finish this one first.
        if (changing)
        {
            pending = next;
            return;
        }

        changing = true;
        Current?.Exit();
        Current = states[next];
        Current.Enter();
        changing = false;
        OnStateChanged?.Invoke(next);

        if (pending.HasValue)
        {
            var queued = pending.Value;
            pending = null;
            ChangeState(queued);
        }
    }
}
