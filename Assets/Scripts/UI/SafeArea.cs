using UnityEngine;

// Keeps the UI inside the phone's safe area, away from notches and the navigation bar.
[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    Rect applied;
    Vector2Int screenSize;

    void OnEnable() => Apply();

    void Update()
    {
        if (Screen.safeArea != applied || screenSize.x != Screen.width || screenSize.y != Screen.height)
            Apply();
    }

    void Apply()
    {
        if (Screen.width <= 0 || Screen.height <= 0)
            return;

        applied = Screen.safeArea;
        screenSize = new Vector2Int(Screen.width, Screen.height);
        var rt = (RectTransform)transform;
        rt.anchorMin = new Vector2(applied.xMin / Screen.width, applied.yMin / Screen.height);
        rt.anchorMax = new Vector2(applied.xMax / Screen.width, applied.yMax / Screen.height);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
