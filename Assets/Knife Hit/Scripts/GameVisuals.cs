using UnityEngine;
using UnityEngine.UIElements;

// ═══════════ নিওন ব্যাকগ্রাউন্ড ═══════════
public class BackgroundVisual : VisualElement
{
    float time = 0f;
    public BackgroundVisual()
    {
        pickingMode = PickingMode.Ignore;
        generateVisualContent += OnPaint;
    }
    public void Update(float dt) { time += dt; MarkDirtyRepaint(); }

    void OnPaint(MeshGenerationContext mgc)
    {
        var p = mgc.painter2D;
        float w = resolvedStyle.width, h = resolvedStyle.height;
        if (w <= 0 || h <= 0) return;

        // ডার্ক বেস
        p.BeginPath();
        p.MoveTo(new Vector2(0, 0));
        p.LineTo(new Vector2(w, 0));
        p.LineTo(new Vector2(w, h));
        p.LineTo(new Vector2(0, h));
        p.ClosePath();
        p.fillColor = new Color(.04f, .06f, .12f);
        p.Fill();

        // ভাসমান নিওন গ্লো (Neon Orbs)
        for (int i = 0; i < 5; i++)
        {
            float offsetX = Mathf.Sin(time * 0.5f + i) * 150f;
            float offsetY = Mathf.Cos(time * 0.3f + i) * 150f;
            float x = (w / 2) + offsetX + (i * 50f) - 100f;
            float y = (h / 2) + offsetY + (i * 80f) - 150f;
            float radius = 150f + Mathf.Sin(time + i) * 50f;

            p.BeginPath();
            p.Arc(new Vector2(x, y), radius, new Angle(0, AngleUnit.Degree), new Angle(360, AngleUnit.Degree), ArcDirection.Clockwise);
            p.ClosePath();

            Color glowColor = i % 2 == 0 ? new Color(1f, .45f, 0f, .04f) : new Color(0.2f, .4f, 1f, .04f);
            p.fillColor = glowColor;
            p.Fill();
        }
    }
}

// ═══════════ ছুরি ═══════════
public class KnifeVisual : VisualElement
{
    public KnifeVisual()
    {
        style.position = Position.Absolute;
        pickingMode = PickingMode.Ignore;
        generateVisualContent += OnPaint;
    }
    void OnPaint(MeshGenerationContext mgc)
    {
        var p = mgc.painter2D;
        float w = resolvedStyle.width, h = resolvedStyle.height;
        if (w <= 0 || h <= 0) return;
        float cx = w * .5f;

        p.BeginPath(); // ব্লেড
        p.MoveTo(new Vector2(cx, 0));
        p.LineTo(new Vector2(cx + w * .3f, h * .4f));
        p.LineTo(new Vector2(cx + w * .2f, h * .55f));
        p.LineTo(new Vector2(cx - w * .2f, h * .55f));
        p.LineTo(new Vector2(cx - w * .3f, h * .4f));
        p.ClosePath();
        p.fillColor = new Color(.9f, .95f, 1f); p.Fill();

        p.BeginPath(); // গার্ড
        p.MoveTo(new Vector2(cx - w * .4f, h * .55f));
        p.LineTo(new Vector2(cx + w * .4f, h * .55f));
        p.LineTo(new Vector2(cx + w * .4f, h * .62f));
        p.LineTo(new Vector2(cx - w * .4f, h * .62f));
        p.ClosePath();
        p.fillColor = new Color(.3f, .35f, .4f); p.Fill();

        p.BeginPath(); // হ্যান্ডেল
        p.MoveTo(new Vector2(cx - w * .2f, h * .62f));
        p.LineTo(new Vector2(cx + w * .2f, h * .62f));
        p.LineTo(new Vector2(cx + w * .2f, h));
        p.LineTo(new Vector2(cx - w * .2f, h));
        p.ClosePath();
        p.fillColor = new Color(.85f, .45f, 0f); p.Fill();
    }
}

// ═══════════ কাঠ/টার্গেট ═══════════
public class LogVisual : VisualElement
{
    Color outer = new(.545f, .353f, .169f);
    Color inner = new(.627f, .322f, .176f);
    Color ring = new(.42f, .259f, .149f);
    string type = "wood";

    public LogVisual()
    {
        style.position = Position.Absolute;
        pickingMode = PickingMode.Ignore;
        generateVisualContent += OnPaint;
    }
    public void Setup(Color o, Color i, Color r, string t)
    {
        outer = o; inner = i; ring = r; type = t; MarkDirtyRepaint();
    }
    void OnPaint(MeshGenerationContext mgc)
    {
        var p = mgc.painter2D;
        float size = resolvedStyle.width;
        float r = size * .5f;
        if (r <= 0) return;
        Vector2 c = new(r, r);

        p.BeginPath();
        p.Arc(c, r - 4, new Angle(0, AngleUnit.Degree), new Angle(360, AngleUnit.Degree), ArcDirection.Clockwise);
        p.ClosePath();
        p.fillColor = outer; p.Fill();

        p.BeginPath();
        p.Arc(c, r - 5, new Angle(0, AngleUnit.Degree), new Angle(360, AngleUnit.Degree), ArcDirection.Clockwise);
        p.strokeColor = ring; p.lineWidth = 8f; p.Stroke();

        p.BeginPath();
        p.Arc(c, r * .65f, new Angle(0, AngleUnit.Degree), new Angle(360, AngleUnit.Degree), ArcDirection.Clockwise);
        p.ClosePath();
        p.fillColor = inner; p.Fill();

        p.BeginPath();
        p.Arc(c, r * .35f, new Angle(0, AngleUnit.Degree), new Angle(360, AngleUnit.Degree), ArcDirection.Clockwise);
        p.strokeColor = ring; p.lineWidth = 3f; p.Stroke();
    }
}

// ═══════════ বোর্ড ভাঙার টুকরো ═══════════
public class ShatterPieceVisual : VisualElement
{
    Color color;
    Vector2[] points;

    public ShatterPieceVisual(Color c, Vector2[] pts)
    {
        style.position = Position.Absolute;
        pickingMode = PickingMode.Ignore;
        color = c;
        points = pts;
        generateVisualContent += OnPaint;
    }

    void OnPaint(MeshGenerationContext mgc)
    {
        if (points == null || points.Length < 3) return;
        var p = mgc.painter2D;
        p.BeginPath();
        p.MoveTo(points[0]);
        for (int i = 1; i < points.Length; i++) p.LineTo(points[i]);
        p.ClosePath();
        p.fillColor = color;
        p.Fill();
    }
}

// ═══════════ পার্টিকেল ═══════════
public class ParticleVisual : VisualElement
{
    public float vx, vy;
    float life = 1f;
    public bool Dead => life <= 0f;

    public ParticleVisual(Color c)
    {
        float radius = Random.Range(2f, 5f);
        style.width = radius * 2; style.height = radius * 2;
        style.position = Position.Absolute;
        style.backgroundColor = c;
        style.borderTopLeftRadius = Length.Percent(50);
        style.borderTopRightRadius = Length.Percent(50);
        style.borderBottomLeftRadius = Length.Percent(50);
        style.borderBottomRightRadius = Length.Percent(50);
        pickingMode = PickingMode.Ignore;
    }

    public void Update(float dt)
    {
        vx *= 0.97f; vy = vy * 0.97f + 600f * dt;
        style.left = style.left.value.value + vx * dt;
        style.top = style.top.value.value + vy * dt;
        life -= dt * 1.6f;
        style.opacity = Mathf.Clamp01(life);
    }
}

// ═══════════ সাইডবার আইকন ═══════════
public class KnifeIcon : VisualElement
{
    bool available = true;
    public bool Available
    {
        get => available;
        set { available = value; MarkDirtyRepaint(); }
    }
    public KnifeIcon()
    {
        style.width = 16; style.height = 36; style.marginTop = 4;
        generateVisualContent += OnPaint;
    }
    void OnPaint(MeshGenerationContext mgc)
    {
        var p = mgc.painter2D;
        float w = resolvedStyle.width, h = resolvedStyle.height;
        float cx = w * .5f;
        Color col = available ? new Color(1f, .75f, .18f) : new Color(.4f, .45f, .5f, .6f);

        p.BeginPath();
        p.MoveTo(new Vector2(cx, 2));
        p.LineTo(new Vector2(cx + w * .35f, h * .55f));
        p.LineTo(new Vector2(cx - w * .35f, h * .55f));
        p.ClosePath();
        p.fillColor = col; p.Fill();

        p.BeginPath();
        p.MoveTo(new Vector2(cx - w * .15f, h * .6f));
        p.LineTo(new Vector2(cx + w * .15f, h * .6f));
        p.LineTo(new Vector2(cx + w * .15f, h - 2));
        p.LineTo(new Vector2(cx - w * .15f, h - 2));
        p.ClosePath();
        p.fillColor = new Color(.55f, .6f, .65f); p.Fill();
    }
}