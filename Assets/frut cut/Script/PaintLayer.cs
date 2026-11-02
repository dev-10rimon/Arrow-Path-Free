using System;
using UnityEngine.UIElements;

public class PaintLayer : VisualElement
{
    public Action<Painter2D> OnPaint;

    public PaintLayer()
    {
        pickingMode = PickingMode.Ignore;
        style.position = Position.Absolute;
        style.left = 0; style.top = 0; style.right = 0; style.bottom = 0;
        usageHints = UsageHints.DynamicTransform;
        generateVisualContent += ctx => OnPaint?.Invoke(ctx.painter2D);
    }
}