using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public enum ARReadiness
{
    Ready,
    Unsupported,
    InstallFailed,
    PermissionDenied
}

// The one place that talks to AR Foundation: starting the AR session, finding surfaces,
// anchors, and noticing when tracking is lost. Other scripts use AR only through this manager.
public class ARTrackingManager : MonoBehaviour
{
    // How long tracking must be lost before the warning appears.
    const float TrackingLostDelaySeconds = 2f;

    [SerializeField] ARSession session;
    [SerializeField] ARPlaneManager planeManager;
    [SerializeField] ARRaycastManager raycastManager;
    [SerializeField] ARAnchorManager anchorManager;

    // Raised once per session, when the first horizontal surface is detected.
    public event Action OnSurfaceFound;
    public event Action OnTrackingLost;
    public event Action OnTrackingRestored;

    public bool HasSurface { get; private set; }

    // Reused every tap so raycasts don't allocate a new list.
    static readonly List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    bool watchTracking;
    ARAnchor watchedAnchor;
    float lostSeconds;
    bool lostReported;

    void OnEnable()
    {
        planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
        planeManager.trackablesChanged.AddListener(OnPlanesChanged);
    }

    void OnDisable()
    {
        planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);
    }

    // Checks the phone supports AR, installs Google Play Services for AR if needed,
    // asks for the camera, then starts the session. Reports the outcome once.
    public void StartAR(Action<ARReadiness> onDone) => StartCoroutine(StartARRoutine(onDone));

    IEnumerator StartARRoutine(Action<ARReadiness> onDone)
    {
        if (ARSession.state == ARSessionState.None || ARSession.state == ARSessionState.CheckingAvailability)
            yield return ARSession.CheckAvailability();

        if (ARSession.state == ARSessionState.Unsupported)
        {
            onDone(ARReadiness.Unsupported);
            yield break;
        }

        if (ARSession.state == ARSessionState.NeedsInstall)
        {
            yield return ARSession.Install();
            if (ARSession.state == ARSessionState.NeedsInstall || ARSession.state == ARSessionState.Unsupported)
            {
                onDone(ARReadiness.InstallFailed);
                yield break;
            }
        }

        if (!CameraPermission.IsGranted)
        {
            bool? granted = null;
            CameraPermission.Request(result => granted = result);
            while (granted == null)
                yield return null;

            if (!granted.Value)
            {
                onDone(ARReadiness.PermissionDenied);
                yield break;
            }
        }

        session.enabled = true;
        onDone(ARReadiness.Ready);
    }

    public void StopAR()
    {
        SetTrackingWatch(false);
        SetPlaneDetection(false);
        session.enabled = false;
    }

    // Clears old surfaces and anchors before a new painting.
    public void ResetAR()
    {
        if (watchedAnchor != null)
            anchorManager.TryRemoveAnchor(watchedAnchor);
        watchedAnchor = null;
        session.Reset();
        HasSurface = false;
    }

    void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
    {
        if (HasSurface || args.added.Count == 0)
            return;

        HasSurface = true;
        OnSurfaceFound?.Invoke();
    }

    // Finds the detected surface under a screen point. The returned pose lies on the surface,
    // turned so the guide's top edge points away from the viewer.
    public bool TryGetSurfacePose(Vector2 screenPoint, Transform viewer, out Pose pose, out ARPlane plane)
    {
        pose = default;
        plane = null;

        if (!raycastManager.Raycast(screenPoint, s_Hits, TrackableType.PlaneWithinPolygon))
            return false;

        var hit = s_Hits[0];
        plane = hit.trackable as ARPlane;
        if (plane == null)
            return false;

        var up = hit.pose.up;
        var forward = Vector3.ProjectOnPlane(viewer.forward, up);
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.ProjectOnPlane(viewer.up, up); // phone pointing straight down

        pose = new Pose(hit.pose.position, Quaternion.LookRotation(forward.normalized, up));
        return true;
    }

    // An anchor keeps the guide fixed to the real table as ARCore refines its map.
    public ARAnchor CreateAnchor(ARPlane plane, Pose pose)
    {
        ARAnchor anchor = null;
        try
        {
            anchor = anchorManager.AttachAnchor(plane, pose);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"RUGA: Could not attach an anchor to the surface ({e.Message}). Using a free anchor instead.");
        }

        if (anchor == null)
        {
            // Fallback: a free-standing anchor at the same pose.
            var go = new GameObject("Guide Anchor");
            go.transform.SetPositionAndRotation(pose.position, pose.rotation);
            anchor = go.AddComponent<ARAnchor>();
        }
        return anchor;
    }

    // Turns surface scanning and the surface outlines on or off (off once the guide is placed).
    public void SetPlaneDetection(bool enabled)
    {
        planeManager.enabled = enabled;
        foreach (var plane in planeManager.trackables)
            plane.gameObject.SetActive(enabled);
    }

    // Start watching for lost tracking; also watches this anchor if one is given.
    public void WatchAnchor(ARAnchor anchor) => watchedAnchor = anchor;

    public void SetTrackingWatch(bool watch)
    {
        watchTracking = watch;
        lostSeconds = 0f;
        if (!watch && lostReported)
        {
            lostReported = false;
            OnTrackingRestored?.Invoke();
        }
    }

    void Update()
    {
        if (!watchTracking || !session.enabled)
            return;

        bool tracking = ARSession.state == ARSessionState.SessionTracking &&
                        (watchedAnchor == null || watchedAnchor.trackingState == TrackingState.Tracking);

        if (tracking)
        {
            lostSeconds = 0f;
            if (lostReported)
            {
                lostReported = false;
                OnTrackingRestored?.Invoke();
            }
            return;
        }

        lostSeconds += Time.unscaledDeltaTime;
        if (!lostReported && lostSeconds >= TrackingLostDelaySeconds)
        {
            lostReported = true;
            OnTrackingLost?.Invoke();
        }
    }
}
