using System;
using UnityEngine;

// Owns the guide on the canvas: where it sits, its real-world size, and how it looks.
// It doesn't read input or talk to AR; the app states tell it what to do.
public class GuideManager : MonoBehaviour
{
    public const float MinSizeCm = 10f;
    public const float MaxSizeCm = 150f;
    public static readonly Vector2 DefaultSizeCm = new Vector2(30f, 40f);

    // Unity units are metres.
    const float CmToMetres = 0.01f;
    // Stops a drag from sending the guide far from where it was placed.
    const float MaxDistanceFromAnchor = 1.5f;
    // While aligning, the grid frame stays visible at least this strongly so the edges can be seen.
    const float AlignFrameMinOpacity = 0.6f;

    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    // Prefab with two flat quads as children: "Art" (the stage image) and "Grid".
    [SerializeField] GameObject guidePrefab;

    public event Action OnGuidePlaced;
    public event Action<bool> OnGuideLocked;

    Transform guide;
    Transform artQuad;
    Transform gridQuad;
    Renderer artRenderer;
    Renderer gridRenderer;
    Material artMaterial;
    Material gridMaterial;
    Texture2D stageImage;

    public bool IsPlaced => guide != null;
    public bool IsLocked { get; private set; }
    public Vector2 SizeCm { get; private set; } = DefaultSizeCm;
    public GuideColour Colour { get; private set; } = GuideColour.Cyan;
    public float Opacity { get; private set; } = 0.5f;
    public bool OutlineVisible { get; private set; } = true;
    public bool GridVisible { get; private set; }
    public bool Hidden { get; private set; }
    public Vector3 Position => guide != null ? guide.position : Vector3.zero;

    public void Place(Transform anchor)
    {
        if (guide == null)
            CreateGuide();

        guide.SetParent(anchor, false);
        guide.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        IsLocked = false;
        ApplySize();
        ApplyLook();
        OnGuidePlaced?.Invoke();
    }

    public void Clear()
    {
        if (guide != null)
            Destroy(guide.gameObject);
        if (artMaterial != null)
            Destroy(artMaterial);
        if (gridMaterial != null)
            Destroy(gridMaterial);

        guide = null;
        artMaterial = null;
        gridMaterial = null;
        IsLocked = false;
    }

    public void SetStageImage(Texture2D image)
    {
        stageImage = image;
        if (artMaterial != null)
            artMaterial.SetTexture(BaseMapId, image);
    }

    // Finds where a screen ray meets the table surface the guide lies on.
    public bool TryGetSurfacePoint(Ray ray, out Vector3 point)
    {
        point = default;
        if (guide == null || guide.parent == null)
            return false;

        var surface = new Plane(guide.parent.up, guide.parent.position);
        if (!surface.Raycast(ray, out var distance))
            return false;

        point = ray.GetPoint(distance);
        return true;
    }

    public void MoveTo(Vector3 worldPosition)
    {
        if (guide == null || IsLocked)
            return;

        var local = guide.parent.InverseTransformPoint(worldPosition);
        local.y = 0f; // stay on the surface
        guide.localPosition = Vector3.ClampMagnitude(local, MaxDistanceFromAnchor);
    }

    // Positive degrees turn clockwise when seen from above.
    public void Rotate(float degrees)
    {
        if (guide == null || IsLocked)
            return;
        guide.localRotation = Quaternion.AngleAxis(degrees, Vector3.up) * guide.localRotation;
    }

    // Back to where the user tapped, keeping the typed size.
    public void ResetPlacement()
    {
        if (guide == null || IsLocked)
            return;
        guide.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    public void SetSizeCm(Vector2 sizeCm)
    {
        SizeCm = new Vector2(
            Mathf.Clamp(sizeCm.x, MinSizeCm, MaxSizeCm),
            Mathf.Clamp(sizeCm.y, MinSizeCm, MaxSizeCm));
        ApplySize();
    }

    public void SetLocked(bool locked)
    {
        if (IsLocked == locked)
            return;
        IsLocked = locked;
        ApplyLook();
        OnGuideLocked?.Invoke(locked);
    }

    public void SetColour(GuideColour colour)
    {
        Colour = colour;
        ApplyLook();
    }

    public void SetOpacity(float opacity)
    {
        Opacity = Mathf.Clamp(opacity, Preferences.MinOpacity, Preferences.MaxOpacity);
        ApplyLook();
    }

    public void SetOutlineVisible(bool visible)
    {
        OutlineVisible = visible;
        ApplyLook();
    }

    public void SetGridVisible(bool visible)
    {
        GridVisible = visible;
        ApplyLook();
    }

    public void SetHidden(bool hidden)
    {
        Hidden = hidden;
        ApplyLook();
    }

    void CreateGuide()
    {
        guide = Instantiate(guidePrefab).transform;
        guide.name = "Guide";
        artQuad = guide.Find("Art");
        gridQuad = guide.Find("Grid");
        artRenderer = artQuad.GetComponent<Renderer>();
        gridRenderer = gridQuad.GetComponent<Renderer>();

        // .material makes a copy for this guide, so changing colour doesn't edit the asset.
        artMaterial = artRenderer.material;
        gridMaterial = gridRenderer.material;
        gridMaterial.SetTexture(BaseMapId, GuideTextures.Grid);
        if (stageImage != null)
            artMaterial.SetTexture(BaseMapId, stageImage);
    }

    void ApplySize()
    {
        if (guide == null)
            return;
        var scale = new Vector3(SizeCm.x * CmToMetres, SizeCm.y * CmToMetres, 1f);
        artQuad.localScale = scale;
        gridQuad.localScale = scale;
    }

    void ApplyLook()
    {
        if (guide == null)
            return;

        var tint = GuideColours.ToColor(Colour);
        tint.a = Opacity;
        artMaterial.SetColor(BaseColorId, tint);

        var gridTint = tint;
        if (!IsLocked)
            gridTint.a = Mathf.Max(Opacity, AlignFrameMinOpacity);
        gridMaterial.SetColor(BaseColorId, gridTint);

        artRenderer.enabled = !Hidden && OutlineVisible;
        // While aligning, the grid doubles as the guide's frame, so it is always shown.
        gridRenderer.enabled = !Hidden && (GridVisible || !IsLocked);
    }
}
