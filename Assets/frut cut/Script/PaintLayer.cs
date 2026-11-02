using System;
using UnityEngine.UIElements;

public class PaintLayer : VisualElement
{
    public Action<Painter2D> OnPaint;

    public PaintLayer()
    {
        pickingMode = PickingMode.Ignore;
        style.position = Position.Absolute;
        generateVisualContent += ctx => OnPaint?.Invoke(ctx.painter2D);
    }
}