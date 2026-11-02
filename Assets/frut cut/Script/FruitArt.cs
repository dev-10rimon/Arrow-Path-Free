using UnityEngine;
using UnityEngine.UIElements;

public static class FruitArt
{
    // ---------- helpers ----------
    static void FillCircle(Painter2D p, Vector2 c, float r, Color col)
    {
        p.BeginPath();
        p.Arc(c, r, Angle.Degrees(0f), Angle.Degrees(180f), ArcDirection.Clockwise);
        p.Arc(c, r, Angle.Degrees(180f), Angle.Degrees(360f), ArcDirection.Clockwise);
        p.ClosePath();
        p.fillColor = col;
        p.Fill();
    }

    // ম্যানুয়ালি উপবৃত্ত আঁকা — Scale() নেই বলে
    static void FillEllipse(Painter2D p, Vector2 c, float rx, float ry, float rotRad, Color col, int segments = 48)
    {
        p.BeginPath();
        float cos = Mathf.Cos(rotRad), sin = Mathf.Sin(rotRad);
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments * Mathf.PI * 2f;
            float x = Mathf.Cos(t) * rx;
            float y = Mathf.Sin(t) * ry;
            float xr = x * cos - y * sin;
            float yr = x * sin + y * cos;
            var pt = c + new Vector2(xr, yr);
            if (i == 0) p.MoveTo(pt);
            else p.LineTo(pt);
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

    // ---------- main ----------
    // ⚠️ সবসময় (0,0)-কেন্দ্রিক আঁকে। পজিশন/রোটেশন VisualElement style হ্যান্ডেল করে।
    public static void Draw(Painter2D p, FruitType t, float radius, float fusePhase = 0f, bool drawGlow = true)
    {
        if (t.IsBomb) { DrawBomb(p, radius, fusePhase); return; }
        if (t.IsHeart) { DrawHeartFruit(p, radius); return; }
        if (t.Id == "banana") { DrawBanana(p, radius); return; }

        if (drawGlow)
            FillCircle(p, Vector2.zero, radius + 6f, new Color(1, 1, 1, 0.10f));

        if (t.Id == "apple" || t.Id == "mango")
        {
            p.BeginPath();
            p.MoveTo(new Vector2(0, -radius + 4f));
            p.QuadraticCurveTo(new Vector2(4, -radius - 12f), new Vector2(10, -radius - 14f));
            p.strokeColor = new Color(0.47f, 0.21f, 0.06f); p.lineWidth = 4.5f; p.lineCap = LineCap.Round;
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
            FillCircle(p, Vector2.zero, radius * 0.86f, t.InnerRind);
            FillCircle(p, Vector2.zero, radius * 0.76f, t.Flesh);
        }

        switch (t.Id)
        {
            case "watermelon":
                for (int i = 0; i < 8; i++)
                    StrokeArc(p, radius - 2f, i * 45f, i * 45f + 14f, new Color(0.08f, 0.33f, 0.18f), 3.5f);
                for (int i = 0; i < 8; i++)
                {
                    float ang = i / 8f * Mathf.PI * 2f + 0.2f;
                    var pos = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (radius * 0.46f);
                    FillEllipse(p, pos, 4f, 2.3f, ang, new Color(0.12f, 0.16f, 0.22f));
                }
                break;

            case "orange":
                for (int i = 0; i < 8; i++)
                {
                    float a = i / 8f * Mathf.PI * 2f;
                    p.BeginPath();
                    p.MoveTo(Vector2.zero);
                    p.LineTo(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (radius * 0.76f));
                    p.strokeColor = new Color(1f, 0.93f, 0.84f); p.lineWidth = 2.5f; p.Stroke();
                }
                FillCircle(p, Vector2.zero, 5f, new Color(1f, 0.93f, 0.84f));
                break;

            case "apple":
                FillEllipse(p, new Vector2(-6, 0), 3f, 5f, -0.2f, new Color(0.27f, 0.10f, 0.01f));
                FillEllipse(p, new Vector2(6, 0), 3f, 5f, 0.2f, new Color(0.27f, 0.10f, 0.01f));
                break;

            case "mango":
                FillEllipse(p, Vector2.zero, radius * 0.35f, radius * 0.22f, 0.2f, new Color(1f, 0.94f, 0.54f));
                break;

            case "coconut":
                FillCircle(p, Vector2.zero, radius * 0.45f, new Color(0.89f, 0.91f, 0.94f));
                break;
        }

        FillEllipse(p, new Vector2(-radius * 0.32f, -radius * 0.32f),
                    radius * 0.24f, radius * 0.12f, -Mathf.PI / 4f, new Color(1, 1, 1, 0.42f));
    }

    static void DrawBomb(Painter2D p, float radius, float fusePhase)
    {
        FillCircle(p, Vector2.zero, radius, new Color(0.06f, 0.09f, 0.16f));
        FillCircle(p, new Vector2(-2, -2), radius * 0.75f, new Color(0.12f, 0.16f, 0.25f));
        FillCircle(p, new Vector2(-4, -4), radius * 0.38f, new Color(0.28f, 0.33f, 0.41f));

        p.BeginPath();
        p.Arc(Vector2.zero, radius, Angle.Degrees(0), Angle.Degrees(360), ArcDirection.Clockwise);
        p.strokeColor = new Color(0.94f, 0.27f, 0.27f); p.lineWidth = 3.5f; p.Stroke();

        var cap = new Rect(-10, -radius - 6, 20, 10);
        p.BeginPath();
        p.MoveTo(new Vector2(cap.xMin, cap.yMin)); p.LineTo(new Vector2(cap.xMax, cap.yMin));
        p.LineTo(new Vector2(cap.xMax, cap.yMax)); p.LineTo(new Vector2(cap.xMin, cap.yMax));
        p.ClosePath();
        p.fillColor = new Color(0.28f, 0.33f, 0.41f); p.Fill();

        p.BeginPath();
        p.MoveTo(new Vector2(0, -radius - 6f));
        p.QuadraticCurveTo(new Vector2(12, -radius - 22f), new Vector2(24, -radius - 14f));
        p.strokeColor = new Color(0.98f, 0.75f, 0.14f); p.lineWidth = 4f; p.lineCap = LineCap.Round; p.Stroke();

        float sr = 6f + Mathf.Sin(fusePhase * 0.25f) * 2.5f;
        FillCircle(p, new Vector2(24, -radius - 14f), sr, new Color(0.99f, 0.94f, 0.54f));

        p.BeginPath();
        p.MoveTo(new Vector2(-11, -11)); p.LineTo(new Vector2(11, 11));
        p.MoveTo(new Vector2(11, -11)); p.LineTo(new Vector2(-11, 11));
        p.strokeColor = new Color(0.97f, 0.44f, 0.44f); p.lineWidth = 4f; p.Stroke();
    }

    static void DrawHeartFruit(Painter2D p, float radius)
    {
        FillCircle(p, Vector2.zero, radius + 6f, new Color(0.96f, 0.45f, 0.71f, 0.35f));
        FillCircle(p, Vector2.zero, radius, new Color(0.86f, 0.15f, 0.47f));
        p.BeginPath();
        p.Arc(Vector2.zero, radius, Angle.Degrees(0), Angle.Degrees(360), ArcDirection.Clockwise);
        p.strokeColor = new Color(0.98f, 0.81f, 0.91f); p.lineWidth = 4f; p.Stroke();

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
        p.strokeColor = new Color(0.79f, 0.54f, 0.02f); p.lineWidth = 4f; p.Stroke();

        FillCircle(p, new Vector2(-radius + 2f, -radius * 0.23f), 5f, new Color(0.44f, 0.25f, 0.07f));
        FillCircle(p, new Vector2(radius - 2f, -radius * 0.23f), 5f, new Color(0.44f, 0.25f, 0.07f));

        p.BeginPath();
        p.MoveTo(new Vector2(-radius * 0.65f, -radius * 0.02f));
        p.QuadraticCurveTo(new Vector2(0, radius * 0.55f), new Vector2(radius * 0.65f, -radius * 0.02f));
        p.strokeColor = new Color(0.99f, 0.97f, 0.76f); p.lineWidth = 3.5f; p.Stroke();
    }
}