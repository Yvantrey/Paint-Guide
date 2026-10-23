using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Reads touches (or the mouse in the editor) and reports simple gestures:
// tap, one-finger drag and two-finger twist. Touches that start on a button or panel are ignored.
// It doesn't decide what a gesture does; the current app state does.
public class GuideGestures : MonoBehaviour
{
    // Movement allowed before a press counts as a drag instead of a tap.
    const float TapMaxMovePixels = 20f;

    public event Action<Vector2> OnTap;
    public event Action<Vector2> OnDragStart;
    public event Action<Vector2> OnDrag;
    public event Action OnDragEnd;
    // Degrees turned since the last frame; positive is counter-clockwise on screen.
    public event Action<float> OnTwist;

    bool pressing;
    bool pressStartedOnUI;
    bool dragging;
    Vector2 pressStart;
    float? lastTwistAngle;

    PointerEventData uiPointer;
    EventSystem uiPointerOwner;
    readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    void Update()
    {
        if (TryGetTwoTouches(out var a, out var b))
        {
            EndPress(); // a second finger turns a drag into a twist
            var angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            if (lastTwistAngle.HasValue)
            {
                var delta = Mathf.DeltaAngle(lastTwistAngle.Value, angle);
                if (delta != 0f)
                    OnTwist?.Invoke(delta);
            }
            lastTwistAngle = angle;
            return;
        }
        lastTwistAngle = null;

        var pointer = Pointer.current;
        if (pointer == null)
            return;

        var position = pointer.position.ReadValue();

        if (pointer.press.wasPressedThisFrame)
        {
            pressing = true;
            dragging = false;
            pressStart = position;
            pressStartedOnUI = IsOverUI(position);
        }

        if (pressing && !pressStartedOnUI && pointer.press.isPressed)
        {
            if (!dragging && (position - pressStart).magnitude > TapMaxMovePixels)
            {
                dragging = true;
                OnDragStart?.Invoke(pressStart);
            }
            if (dragging)
                OnDrag?.Invoke(position);
        }

        if (pointer.press.wasReleasedThisFrame)
        {
            if (pressing && !pressStartedOnUI && !dragging)
                OnTap?.Invoke(position);
            EndPress();
        }
    }

    void EndPress()
    {
        if (dragging)
            OnDragEnd?.Invoke();
        pressing = false;
        dragging = false;
    }

    static bool TryGetTwoTouches(out Vector2 a, out Vector2 b)
    {
        a = b = default;
        var touchscreen = Touchscreen.current;
        if (touchscreen == null)
            return false;

        int found = 0;
        var touches = touchscreen.touches;
        for (int i = 0; i < touches.Count && found < 2; i++)
        {
            if (!touches[i].isInProgress)
                continue;
            if (found == 0)
                a = touches[i].position.ReadValue();
            else
                b = touches[i].position.ReadValue();
            found++;
        }
        return found == 2;
    }

    bool IsOverUI(Vector2 screenPosition)
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null)
            return false;

        if (uiPointer == null || uiPointerOwner != eventSystem)
        {
            uiPointer = new PointerEventData(eventSystem);
            uiPointerOwner = eventSystem;
        }
        uiPointer.position = screenPosition;
        uiHits.Clear();
        eventSystem.RaycastAll(uiPointer, uiHits);
        return uiHits.Count > 0;
    }
}
