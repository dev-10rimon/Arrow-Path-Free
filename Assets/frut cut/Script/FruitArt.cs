using UnityEngine;
using UnityEngine.UIElements;

public static class FruitArt
{
    static void FillCircle(Painter2D p, Vector2 c, float r, Color col)
    {
        p.BeginPath();
        p.Arc(c, r, Angle.Degrees(0f), Angle.Degrees(180f), ArcDirection.Clockwise);
        p.Arc(c, r, Angle.Degrees(180f), Angle.Degrees(360f), ArcDirection.Clockwise);
        p.ClosePath();
        p.fillColor = col;
        p.Fill();
    }

    static void FillEllipse(Painter2D p, Vector2 c, float rx, float ry, float rotRad, Color col, int segments = 48)
    {
        p.BeginPath();
        float cos = Mathf.Cos(rotRad), sin = Mathf.Sin(rotRad);
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments * Mathf.PI * 2f;
            float x = Mathf.Cos(t) * rx;
            float y = Mathf.Sin(t) * ry;
            var pt = c + new Vector2(x * cos - y * sin, x * sin + y * cos);
            if (i == 0) p.MoveTo(pt); else p.LineTo(pt);
        }
        p.ClosePath();
        p.fillColor = col;
        p.Fill();
    }

    static void StrokeArc(Painter2D p, float r, float a0, float a1, Color col, float w)
    {
        p.BeginPath();
        p.Arc(Vector2.zero, r, Angle.Degrees(a0), Angle.Degrees(a1), ArcDirection.Clockwise);
        p.strokeColor = col; p.lineWidth = w; p.lineCap = LineCap.Round;
        p.Stroke();
    }

    public static void Draw(Painter2D p, FruitType t, float radius, float fusePhase = 0f)
    {
        if (!t.IsBomb)
            FillCircle(p, new Vector2(0, 8), radius + 2f, new Color(0, 0, 0, 0.4f));

        if (t.IsBomb) { DrawBomb(p, radius, fusePhase); return; }
        if (t.IsHeart) { DrawHeartFruit(p, radius); return; }
        if (t.Id == "banana") { DrawBanana(p, radius); return; }

        if (t.Id == "apple" || t.Id == "mango")
        {
            p.BeginPath();
            p.MoveTo(new Vector2(0, -radius + 4f));
            p.QuadraticCurveTo(new Vector2(4, -radius - 12f), new Vector2(10, -radius - 14f));
            p.strokeColor = new Color(0.47f, 0.21f, 0.06f); p.lineWidth = 5f; p.lineCap = LineCap.Round;
            p.Stroke();

            p.BeginPath();
            p.MoveTo(new Vector2(2, -radius - 4f));
            p.QuadraticCurveTo(new Vector2(20, -radius - 18f), new Vector2(25, -radius - 2f));
            p.QuadraticCurveTo(new Vector2(12, -radius + 3f), new Vector2(2, -radius - 4f));
            p.fillColor = new Color(0.13f, 0.77f, 0.37f);
            p.Fill();
        }

        if (t.Id == "mango")
        {
            FillEllipse(p, Vector2.zero, radius * 1.05f, radius * 0.88f, 0.2f, t.Rind);
            FillEllipse(p, Vector2.zero, radius * 0.92f, radius * 0.76f, 0.2f, t.InnerRind);
            FillEllipse(p, Vector2.zero, radius * 0.84f, radius * 0.68f, 0.2f, t.Flesh);
        }
        else
        {
            FillCircle(p, Vector2.zero, radius, t.Rind);
            FillCircle(p, Vector2.zero, radius * 0.88f, t.InnerRind);
            FillCircle(p, Vector2.zero, radius * 0.80f, t.Flesh);
        }

        switch (t.Id)
        {
            case "watermelon":
                for (int i = 0; i < 8; i++)
                    StrokeArc(p, radius - 2f, i * 45f, i * 45f + 18f, new Color(0.08f, 0.33f, 0.18f), 4f);
                for (int i = 0; i < 8; i++)
                {
                    float ang = i / 8f * Mathf.PI * 2f + 0.2f;
                    var pos = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (radius * 0.46f);
                    FillEllipse(p, pos, 5f, 3f, ang, new Color(0.1f, 0.1f, 0.15f));
                }
                break;
            case "orange":
                for (int i = 0; i < 8; i++)
                {
                    float a = i / 8f * Mathf.PI * 2f;
                    p.BeginPath(); p.MoveTo(Vector2.zero);
                    p.LineTo(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (radius * 0.80f));
                    p.strokeColor = new Color(1f, 0.93f, 0.84f, 0.8f); p.lineWidth = 3f; p.Stroke();
                }
                break;
            case "apple":
                FillEllipse(p, new Vector2(-6, 0), 4f, 6f, -0.2f, new Color(0.27f, 0.10f, 0.01f));
                FillEllipse(p, new Vector2(6, 0), 4f, 6f, 0.2f, new Color(0.27f, 0.10f, 0.01f));
                break;
            case "coconut":
                FillCircle(p, Vector2.zero, radius * 0.45f, new Color(0.89f, 0.91f, 0.94f));
                break;
        }

        FillEllipse(p, new Vector2(-radius * 0.32f, -radius * 0.32f),
                    radius * 0.28f, radius * 0.14f, -Mathf.PI / 4f, new Color(1, 1, 1, 0.6f));
    }

    public static void DrawHalf(Painter2D p, FruitType t, Vector2 center, float radius, int side)
    {
        float startAngle = side > 0 ? 180f : 0f;
        float endAngle = side > 0 ? 360f : 180f;

        // কলার জন্য বিশেষ আকৃতি
        if (t.Id == "banana")
        {
            p.BeginPath();
            if (side > 0)
            {
                p.MoveTo(center + new Vector2(-radius, 0));
                p.QuadraticCurveTo(center + new Vector2(0, radius * 0.5f), center + new Vector2(radius, 0));
                p.LineTo(center + new Vector2(radius, radius * 0.2f));
                p.QuadraticCurveTo(center + new Vector2(0, radius * 0.8f), center + new Vector2(-radius, radius * 0.2f));
            }
            else
            {
                p.MoveTo(center + new Vector2(-radius, 0));
                p.QuadraticCurveTo(center + new Vector2(0, -radius * 0.5f), center + new Vector2(radius, 0));
                p.LineTo(center + new Vector2(radius, -radius * 0.2f));
                p.QuadraticCurveTo(center + new Vector2(0, -radius * 0.8f), center + new Vector2(-radius, -radius * 0.2f));
            }
            p.ClosePath();
            p.fillColor = t.Rind; p.Fill();

            p.BeginPath();
            if (side > 0)
            {
                p.MoveTo(center + new Vector2(-radius * 0.8f, radius * 0.1f));
                p.QuadraticCurveTo(center + new Vector2(0, radius * 0.4f), center + new Vector2(radius * 0.8f, radius * 0.1f));
                p.QuadraticCurveTo(center + new Vector2(0, radius * 0.6f), center + new Vector2(-radius * 0.8f, radius * 0.1f));
            }
            else
            {
                p.MoveTo(center + new Vector2(-radius * 0.8f, -radius * 0.1f));
                p.QuadraticCurveTo(center + new Vector2(0, -radius * 0.4f), center + new Vector2(radius * 0.8f, -radius * 0.1f));
                p.QuadraticCurveTo(center + new Vector2(0, -radius * 0.6f), center + new Vector2(-radius * 0.8f, -radius * 0.1f));
            }
            p.ClosePath();
            p.fillColor = t.Flesh; p.Fill();
            return;
        }

        // অন্যান্য গোল ফলের জন্য
        p.BeginPath();
        p.Arc(center, radius, Angle.Degrees(startAngle), Angle.Degrees(endAngle), ArcDirection.Clockwise);
        p.ClosePath();
        p.fillColor = t.Rind; p.Fill();

        p.BeginPath();
        p.Arc(center, radius * 0.86f, Angle.Degrees(startAngle), Angle.Degrees(endAngle), ArcDirection.Clockwise);
        p.ClosePath();
        p.fillColor = t.InnerRind; p.Fill();

        p.BeginPath();
        p.Arc(center, radius * 0.76f, Angle.Degrees(startAngle), Angle.Degrees(endAngle), ArcDirection.Clockwise);
        p.ClosePath();
        p.fillColor = t.Flesh; p.Fill();

        if (t.Id == "watermelon")
        {
            for (int i = 0; i < 4; i++)
            {
                float ang = (side > 0 ? 180f : 0f) + (i + 0.5f) * 45f;
                var pos = center + new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad)) * (radius * 0.5f);
                FillEllipse(p, pos, 5f, 3f, ang * Mathf.Deg2Rad, new Color(0.1f, 0.1f, 0.15f));
            }
        }
        else if (t.Id == "orange")
        {
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                if (side > 0 && a < Mathf.PI) continue;
                if (side < 0 && a >= Mathf.PI) continue;
                p.BeginPath(); p.MoveTo(center);
                p.LineTo(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (radius * 0.76f));
                p.strokeColor = new Color(1f, 0.93f, 0.84f, 0.8f); p.lineWidth = 3f; p.Stroke();
            }
        }
        else if (t.Id == "apple")
        {
            float cy = side > 0 ? -radius * 0.3f : radius * 0.3f;
            FillEllipse(p, center + new Vector2(0, cy), 8f, 12f, 0, new Color(0.9f, 0.9f, 0.8f));
            FillEllipse(p, center + new Vector2(-4, cy), 2f, 4f, 0, new Color(0.27f, 0.10f, 0.01f));
            FillEllipse(p, center + new Vector2(4, cy), 2f, 4f, 0, new Color(0.27f, 0.10f, 0.01f));
        }
        else if (t.Id == "mango")
        {
            float cy = side > 0 ? -radius * 0.2f : radius * 0.2f;
            FillEllipse(p, center + new Vector2(0, cy), radius * 0.35f, radius * 0.22f, 0.2f, new Color(1f, 0.94f, 0.54f));
        }
        else if (t.Id == "coconut")
        {
            p.BeginPath();
            p.Arc(center, radius * 0.45f, Angle.Degrees(startAngle), Angle.Degrees(endAngle), ArcDirection.Clockwise);
            p.ClosePath();
            p.fillColor = new Color(0.89f, 0.91f, 0.94f);
            p.Fill();
        }
    }

    static void DrawBomb(Painter2D p, float radius, float fusePhase)
    {
        FillCircle(p, Vector2.zero, radius, new Color(0.06f, 0.09f, 0.16f));
        FillCircle(p, new Vector2(-2, -2), radius * 0.75f, new Color(0.15f, 0.2f, 0.3f));
        FillCircle(p, new Vector2(-4, -4), radius * 0.38f, new Color(0.35f, 0.4f, 0.5f));

        p.BeginPath();
        p.Arc(Vector2.zero, radius, Angle.Degrees(0), Angle.Degrees(360), ArcDirection.Clockwise);
        p.strokeColor = new Color(0.94f, 0.27f, 0.27f); p.lineWidth = 4f; p.Stroke();

        p.BeginPath();
        p.MoveTo(new Vector2(0, -radius - 6f));
        p.QuadraticCurveTo(new Vector2(12, -radius - 22f), new Vector2(24, -radius - 14f));
        p.strokeColor = new Color(0.98f, 0.75f, 0.14f); p.lineWidth = 5f; p.lineCap = LineCap.Round; p.Stroke();

        float sr = 8f + Mathf.Sin(fusePhase * 0.3f) * 3f;
        FillCircle(p, new Vector2(24, -radius - 14f), sr, new Color(1f, 0.9f, 0.3f));

        p.BeginPath();
        p.MoveTo(new Vector2(-12, -12)); p.LineTo(new Vector2(12, 12));
        p.MoveTo(new Vector2(12, -12)); p.LineTo(new Vector2(-12, 12));
        p.strokeColor = new Color(0.97f, 0.44f, 0.44f); p.lineWidth = 5f; p.Stroke();
    }

    static void DrawHeartFruit(Painter2D p, float radius)
    {
        FillCircle(p, Vector2.zero, radius, new Color(0.86f, 0.15f, 0.47f));
        p.BeginPath();
        p.Arc(Vector2.zero, radius, Angle.Degrees(0), Angle.Degrees(360), ArcDirection.Clockwise);
        p.strokeColor = new Color(0.98f, 0.81f, 0.91f); p.lineWidth = 5f; p.Stroke();

        FillCircle(p, Vector2.zero, radius * 0.84f, new Color(0.96f, 0.45f, 0.71f));

        float hr = radius * 0.42f;
        p.BeginPath();
        p.MoveTo(new Vector2(0, hr * 0.8f));
        p.BezierCurveTo(new Vector2(-hr * 1.4f, -hr * 0.2f), new Vector2(-hr * 0.7f, -hr * 1.2f), new Vector2(0, -hr * 0.4f));
        p.BezierCurveTo(new Vector2(hr * 0.7f, -hr * 1.2f), new Vector2(hr * 1.4f, -hr * 0.2f), new Vector2(0, hr * 0.8f));
        p.fillColor = new Color(1f, 0.95f, 0.95f); p.Fill();
    }

    static void DrawBanana(Painter2D p, float radius)
    {
        p.BeginPath();
        p.MoveTo(new Vector2(-radius, -radius * 0.25f));
        p.QuadraticCurveTo(new Vector2(0, radius * 0.95f), new Vector2(radius, -radius * 0.25f));
        p.QuadraticCurveTo(new Vector2(0, radius * 0.35f), new Vector2(-radius, -radius * 0.25f));
        p.ClosePath();
        p.fillColor = new Color(0.98f, 0.80f, 0.08f); p.Fill();
        p.strokeColor = new Color(0.79f, 0.54f, 0.02f); p.lineWidth = 5f; p.Stroke();

        p.BeginPath();
        p.MoveTo(new Vector2(-radius * 0.65f, -radius * 0.02f));
        p.QuadraticCurveTo(new Vector2(0, radius * 0.55f), new Vector2(radius * 0.65f, -radius * 0.02f));
        p.strokeColor = new Color(0.99f, 0.97f, 0.76f); p.lineWidth = 4f; p.Stroke();
    }
}