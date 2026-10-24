using UnityEngine;
using UnityEngine.UI;

// Keeps a RawImage's picture filling its box without distortion, cropping the overflow.
[RequireComponent(typeof(RawImage))]
public class CoverImage : MonoBehaviour
{
    RawImage image;

    void OnEnable() => Fit();

    void OnRectTransformDimensionsChange() => Fit();

    public void Fit()
    {
        if (image == null)
            image = GetComponent<RawImage>();
        var tex = image.texture;
        var size = image.rectTransform.rect.size;
        if (tex == null || size.x <= 0f || size.y <= 0f)
            return;

        float boxAspect = size.x / size.y;
        float texAspect = (float)tex.width / tex.height;
        image.uvRect = texAspect > boxAspect
            ? new Rect((1f - boxAspect / texAspect) / 2f, 0f, boxAspect / texAspect, 1f)
            : new Rect(0f, (1f - texAspect / boxAspect) / 2f, 1f, texAspect / boxAspect);
    }
}
