using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class FruitSlasherGame : MonoBehaviour
{
    [SerializeField] UIDocument document;
    [SerializeField] AudioSource sfx;

    // ---------------- state ----------------
    const int MAX_LIVES = 5;
    const float HIT_PAD = 26f;

    enum State { Title, Playing, GameOver }
    State state = State.Title;
    bool zenMode = false;

    int score, lives = MAX_LIVES, highScore;
    float waveCooldown, screenFlash, shakeTime;

    // ---------------- entities ----------------
    class Fruit
    {
        public FruitType T;
        public float R, X, Y, Vx, Vy, Rot, RotSpd, Fuse;
        public float Gravity = 0.135f;
        public float Height;
        public bool Sliced, Entered;
        public VisualElement El;

        public void Step(float dt)
        {
            X += Vx * dt; Y += Vy * dt; Vy += Gravity * dt;
            Rot += RotSpd * dt; Fuse += dt;
            if (Y < Height * 0.5f) Entered = true;
        }
    }

    class Half
    {
        public FruitType T;
        public float R, X, Y, Vx, Vy, Rot, RotSpd, W, H;
        public int Side;
        public VisualElement El;
    }

    class Particle { public float X, Y, Vx, Vy, R, A, Decay; public Color C; }
    class Label2 { public VisualElement El; public float A, X, Y; public Color Color; }

    readonly List<Fruit> fruits = new();
    readonly List<Half> halves = new();
    readonly List<Particle> particles = new();
    readonly List<(float x, float y, float r, Color c, float a)> stains = new();
    readonly List<Label2> floats = new();
    readonly List<(Vector2 p, float t)> blade = new();

    // ---------------- UI ----------------
    VisualElement root, playfield, fruitContainer, halfLayer, labelLayer, inputCatcher;
    PaintLayer bgLayer, stainLayer, particleLayer, bladeLayer;

    Label scoreValue, bestValue, finalScoreText, bestScoreText, gameOverBadge, modeIcon, modeLabel;
    VisualElement livesContainer, startModal, gameOverModal;

    AudioClip sliceSfx, healSfx, bombSfx, launchSfx, launchBombSfx;

    Vector2 Size => new(playfield.resolvedStyle.width, playfield.resolvedStyle.height);
    float W => Size.x;
    float H => Size.y;

    // ==========================================
    // SETUP
    // ==========================================
    void Awake()
    {
        highScore = PlayerPrefs.GetInt("FruitSlasherBest", 0);

        sliceSfx = Sfx.Slice();
        healSfx = Sfx.Heal();
        bombSfx = Sfx.Bomb();
        launchSfx = Sfx.Launch(false);
        launchBombSfx = Sfx.Launch(true);
    }

    void OnEnable()
    {
        root = document.rootVisualElement;
        playfield = root.Q("playfield");
        inputCatcher = root.Q("input-catcher");

        scoreValue = root.Q<Label>("score-value");
        bestValue = root.Q<Label>("best-value");
        livesContainer = root.Q("lives-container");
        startModal = root.Q("start-modal");
        gameOverModal = root.Q("gameover-modal");
        finalScoreText = root.Q<Label>("final-score");
        bestScoreText = root.Q<Label>("best-score");
        gameOverBadge = root.Q<Label>("gameover-badge");
        modeIcon = root.Q<Label>("mode-icon");
        modeLabel = root.Q<Label>("mode-label");

        // ---- canvas layers (bottom → top) ----
        bgLayer = AddLayer(playfield, DrawBackground);
        stainLayer = AddLayer(playfield, DrawStains);

        fruitContainer = new VisualElement { pickingMode = PickingMode.Ignore };
        fruitContainer.style.position = Position.Absolute;
        fruitContainer.style.left = 0; fruitContainer.style.top = 0;
        fruitContainer.style.right = 0; fruitContainer.style.bottom = 0;
        playfield.Add(fruitContainer);

        halfLayer = MakeOverlay(playfield);

        particleLayer = AddLayer(playfield, DrawParticles);
        bladeLayer = AddLayer(playfield, DrawBlade);
        labelLayer = MakeOverlay(playfield);

        // ---- input (on full-screen catcher) ----
        inputCatcher.RegisterCallback<PointerDownEvent>(e => { InitAudio(); blade.Clear(); AddBladePoint(e.position); });
        inputCatcher.RegisterCallback<PointerMoveEvent>(e => AddBladePoint(e.position));
        inputCatcher.RegisterCallback<PointerUpEvent>(_ => blade.Clear());
        inputCatcher.RegisterCallback<PointerLeaveEvent>(_ => blade.Clear());

        // Global click just to unlock audio in WebGL
        root.RegisterCallback<PointerDownEvent>(_ => InitAudio(), TrickleDown.TrickleDown);

        // ---- buttons ----
        root.Q<Button>("btn-restart").clicked += StartNewGame;
        root.Q<Button>("btn-mode").clicked += ToggleMode;
        root.Q<Button>("btn-sound").clicked += ToggleSound;
        root.Q<Button>("btn-play").clicked += StartNewGame;
        root.Q<Button>("btn-again").clicked += StartNewGame;
        root.Q<Button>("btn-zen-over").clicked += () => { SelectMode(true); StartNewGame(); };
        root.Q<Button>("btn-easy-sel").clicked += () => SelectMode(false);
        root.Q<Button>("btn-zen-sel").clicked += () => SelectMode(true);

        bestValue.text = highScore.ToString();
        UpdateModeUI();

        SpawnWave(2, false);
        waveCooldown = 110f;
    }

    PaintLayer AddLayer(VisualElement parent, Action<Painter2D> draw)
    {
        var l = new PaintLayer { OnPaint = draw };
        parent.Add(l);
        return l;
    }

    static VisualElement MakeOverlay(VisualElement parent)
    {
        var e = new VisualElement { pickingMode = PickingMode.Ignore };
        e.style.position = Position.Absolute;
        e.style.left = 0; e.style.top = 0; e.style.right = 0; e.style.bottom = 0;
        parent.Add(e);
        return e;
    }

    // ==========================================
    // UPDATE
    // ==========================================
    void Update()
    {
        float dt = Mathf.Min(Time.deltaTime * 60f, 2.2f);
        if (dt <= 0f || float.IsNaN(W) || W < 50f) return;

        if (shakeTime > 0f) shakeTime -= dt;

        // wave spawning
        waveCooldown -= dt;
        if (waveCooldown <= 0f && fruits.Count <= 1)
        {
            if (state == State.Playing)
            {
                float rnd = UnityEngine.Random.value;
                SpawnWave(rnd < 0.35f ? 1 : (rnd < 0.75f ? 2 : 3), true);
                waveCooldown = 115f;
            }
            else
            {
                if (fruits.Count == 0) SpawnWave(2, false);
                waveCooldown = 120f;
            }
        }

        // ---- fruits ----
        for (int i = fruits.Count - 1; i >= 0; i--)
        {
            var f = fruits[i];
            f.Height = H;
            f.Step(dt);

            if (f.El != null)
            {
                f.El.style.left = f.X - f.R;
                f.El.style.top = f.Y - f.R;
                f.El.style.rotate = new Rotate(Angle.Radians(f.Rot));
            }

            // passive hover slicing
            if (!f.Sliced && !f.T.IsBomb && blade.Count > 0)
            {
                var last = blade[blade.Count - 1];
                if (Time.time - last.t < 0.25f &&
                    Vector2.Distance(new Vector2(f.X, f.Y), last.p) <= f.R + HIT_PAD)
                {
                    AddBladePoint(last.p);
                    break;
                }
            }

            // fell off bottom
            if (f.Vy > 0f && f.Y > H + f.R + 45f)
            {
                if (state == State.Playing && !zenMode && !f.T.IsBomb && !f.T.IsHeart && f.Entered)
                {
                    lives = Mathf.Max(0, lives - 1);
                    RenderLives();
                    AddFloat(Mathf.Clamp(f.X, 80, W - 80), H - 45,
                             "মিস! -১ ❤️", new Color(0.99f, 0.64f, 0.69f), 0.95f);
                    if (lives <= 0)
                    {
                        if (f.El != null) f.El.RemoveFromHierarchy();
                        fruits.RemoveAt(i);
                        GameOver("missed");
                        break;
                    }
                }
                if (f.El != null) f.El.RemoveFromHierarchy();
                fruits.RemoveAt(i);
            }
        }

        // ---- halves ----
        for (int i = halves.Count - 1; i >= 0; i--)
        {
            var h = halves[i];
            h.X += h.Vx * dt; h.Y += h.Vy * dt;
            h.Vy += 0.20f * dt;
            h.Rot += h.RotSpd * dt;

            if (h.El != null)
            {
                h.El.style.left = h.X - h.W * 0.5f;
                h.El.style.top = h.Side > 0 ? h.Y - h.H : h.Y;
                h.El.style.rotate = new Rotate(Angle.Radians(h.Rot));
            }

            if (h.Y > H + 120f)
            {
                if (h.El != null) h.El.RemoveFromHierarchy();
                halves.RemoveAt(i);
            }
        }

        // ---- particles ----
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            var p = particles[i];
            p.X += p.Vx * dt; p.Y += p.Vy * dt; p.Vy += 0.18f * dt;
            p.A -= p.Decay * dt;
            if (p.A <= 0f) particles.RemoveAt(i);
        }

        // ---- stains ----
        for (int i = stains.Count - 1; i >= 0; i--)
        {
            var s = stains[i];
            s.a -= 0.0007f * dt;
            stains[i] = s;
            if (s.a <= 0f) stains.RemoveAt(i);
        }

        // ---- floating labels ----
        for (int i = floats.Count - 1; i >= 0; i--)
        {
            var ft = floats[i];
            ft.A -= 0.016f * dt;
            ft.Y += -1.6f * dt;
            ft.El.style.top = ft.Y;
            ft.El.style.opacity = Mathf.Max(0f, ft.A);
            if (ft.A <= 0f)
            {
                ft.El.RemoveFromHierarchy();
                floats.RemoveAt(i);
            }
        }

        // ---- trail expiry ----
        float now = Time.time;
        blade.RemoveAll(b => now - b.t > 0.17f);
        if (screenFlash > 0f) screenFlash -= 0.04f * dt;

        // repaint painters
        stainLayer.MarkDirtyRepaint();
        particleLayer.MarkDirtyRepaint();
        bladeLayer.MarkDirtyRepaint();
    }

    // ==========================================
    // DRAWING (Painter2D only, no transforms)
    // ==========================================
    void DrawBackground(Painter2D p)
    {
        if (float.IsNaN(W) || W < 50f) return;

        p.fillColor = new Color(0.16f, 0.09f, 0.06f);
        p.BeginPath();
        p.MoveTo(Vector2.zero); p.LineTo(new Vector2(W, 0));
        p.LineTo(new Vector2(W, H)); p.LineTo(new Vector2(0, H));
        p.ClosePath(); p.Fill();

        for (float y = 90f; y < H; y += 90f)
        {
            p.BeginPath();
            p.MoveTo(new Vector2(0, y)); p.LineTo(new Vector2(W, y));
            p.strokeColor = new Color(0, 0, 0, 0.22f); p.lineWidth = 2f; p.Stroke();
        }

        var c = new Vector2(W * 0.5f, H * 0.48f);
        float maxR = Mathf.Max(W, H) * 0.65f;
        for (int i = 4; i >= 1; i--)
        {
            p.BeginPath();
            p.Arc(c, maxR * i / 4f, Angle.Degrees(0), Angle.Degrees(360), ArcDirection.Clockwise);
            p.fillColor = new Color(0.98f, 0.75f, 0.14f, 0.02f);
            p.Fill();
        }
    }

    void DrawStains(Painter2D p)
    {
        foreach (var s in stains)
        {
            p.BeginPath();
            p.Arc(new Vector2(s.x, s.y), s.r, Angle.Degrees(0), Angle.Degrees(360), ArcDirection.Clockwise);
            p.fillColor = new Color(s.c.r, s.c.g, s.c.b, s.a);
            p.Fill();
        }
    }

    void DrawParticles(Painter2D p)
    {
        foreach (var pt in particles)
        {
            if (pt.A <= 0f) continue;
            p.BeginPath();
            p.Arc(new Vector2(pt.X, pt.Y), pt.R, Angle.Degrees(0), Angle.Degrees(360), ArcDirection.Clockwise);
            p.fillColor = new Color(pt.C.r, pt.C.g, pt.C.b, Mathf.Clamp01(pt.A));
            p.Fill();
        }
    }

    void DrawBlade(Painter2D p)
    {
        if (blade.Count < 2) return;
        for (int i = 1; i < blade.Count; i++)
        {
            float ratio = (float)i / blade.Count;
            p.BeginPath();
            p.MoveTo(blade[i - 1].p); p.LineTo(blade[i].p);
            p.strokeColor = new Color(0.20f, 0.83f, 0.60f, ratio * 0.8f);
            p.lineWidth = ratio * 20f; p.lineCap = LineCap.Round; p.Stroke();

            p.BeginPath();
            p.MoveTo(blade[i - 1].p); p.LineTo(blade[i].p);
            p.strokeColor = new Color(1, 1, 1, ratio);
            p.lineWidth = ratio * 7f; p.lineCap = LineCap.Round; p.Stroke();
        }
    }

    // ==========================================
    // SLICING
    // ==========================================
    void AddBladePoint(Vector2 pt)
    {
        blade.Add((pt, Time.time));
        if (blade.Count > 18) blade.RemoveAt(0);
        if (blade.Count < 2) return;

        var p1 = blade[blade.Count - 2].p;
        var p2 = blade[blade.Count - 1].p;
        if (Vector2.Distance(p1, p2) < 2f) return;

        float sliceAngle = Mathf.Atan2(p2.y - p1.y, p2.x - p1.x);
        var sliced = new List<Fruit>();

        for (int i = fruits.Count - 1; i >= 0; i--)
        {
            var f = fruits[i];
            if (f.Sliced) continue;

            float effR = f.T.IsBomb ? f.R * 0.80f : f.R + HIT_PAD;
            if (!SegCircle(p1, p2, new Vector2(f.X, f.Y), effR)) continue;

            f.Sliced = true;

            if (f.T.IsBomb)
            {
                Play(bombSfx, 1f);
                screenFlash = 0.55f; shakeTime = 14f;
                for (int k = 0; k < 28; k++)
                    particles.Add(NewParticle(f.X, f.Y,
                        k % 2 == 0 ? new Color(0.98f, 0.45f, 0.09f) : new Color(0.99f, 0.94f, 0.54f), 1.4f));

                if (f.El != null) f.El.RemoveFromHierarchy();
                fruits.RemoveAt(i);

                if (state == State.Playing && !zenMode)
                {
                    lives = Mathf.Max(0, lives - 1);
                    RenderLives();
                    AddFloat(f.X, f.Y, "বোমা! -১ লাইফ 💔", new Color(0.98f, 0.44f, 0.52f), 1.2f);
                    if (lives <= 0) { GameOver("bomb"); return; }
                }
                else AddFloat(f.X, f.Y, "বুম! 💥", new Color(0.98f, 0.57f, 0.24f), 1.2f);
                continue;
            }

            sliced.Add(f);
            if (f.El != null) f.El.RemoveFromHierarchy();
            fruits.RemoveAt(i);
        }

        if (sliced.Count == 0) return;
        Play(sliceSfx, 1f + UnityEngine.Random.Range(-0.1f, 0.1f));

        foreach (var f in sliced)
        {
            SpawnHalf(f, sliceAngle, +1);
            SpawnHalf(f, sliceAngle, -1);

            stains.Add((f.X, f.Y, f.R * UnityEngine.Random.Range(0.85f, 1.2f), f.T.Juice, 0.30f));

            for (int k = 0; k < 16; k++)
                particles.Add(NewParticle(f.X, f.Y, f.T.Flesh, 1f));

            if (f.T.IsHeart)
            {
                Play(healSfx, 1f);
                if (state == State.Playing && !zenMode && lives < MAX_LIVES)
                {
                    lives++; RenderLives();
                    AddFloat(f.X, f.Y - 20, "+১ লাইফ ❤️", new Color(0.96f, 0.45f, 0.71f), 1.35f);
                }
                else AddFloat(f.X, f.Y - 20, "বোনাস +৫ 💖", new Color(0.96f, 0.45f, 0.71f), 1.3f);
            }
            else AddFloat(f.X, f.Y - 10, $"+{f.T.Points}", f.T.Juice, 1.05f);

            if (state == State.Playing) score += f.T.Points;
        }

        if (sliced.Count >= 2)
        {
            int bonus = sliced.Count * 2;
            if (state == State.Playing) score += bonus;
            var last = sliced[sliced.Count - 1];
            AddFloat(last.X, last.Y - 48, $"দারুণ কম্বো +{bonus}! ✨", new Color(0.99f, 0.88f, 0.28f), 1.3f);
        }

        if (state == State.Playing)
        {
            scoreValue.text = score.ToString();
            if (score > highScore)
            {
                highScore = score;
                bestValue.text = highScore.ToString();
                PlayerPrefs.SetInt("FruitSlasherBest", highScore);
            }
        }
    }

    void SpawnHalf(Fruit f, float sliceAngle, int side)
    {
        float r = f.R, pad = 4f;
        float w = 2f * r + pad * 2f;
        float h = r + pad;

        var wrapper = new VisualElement { pickingMode = PickingMode.Ignore };
        wrapper.style.position = Position.Absolute;
        wrapper.style.width = w;
        wrapper.style.height = h;
        wrapper.style.overflow = Overflow.Hidden;
        wrapper.style.transformOrigin = side > 0
            ? new TransformOrigin(Length.Percent(50), Length.Percent(100))
            : new TransformOrigin(Length.Percent(50), Length.Percent(0));

        wrapper.style.left = f.X - w * 0.5f;
        wrapper.style.top = side > 0 ? f.Y - h : f.Y;
        wrapper.style.rotate = new Rotate(Angle.Radians(sliceAngle));

        var child = new PaintLayer();
        child.style.position = Position.Absolute;
        child.style.width = 0; child.style.height = 0;
        child.style.left = w * 0.5f;
        child.style.top = side > 0 ? h : 0f;
        child.OnPaint = p => FruitArt.Draw(p, f.T, r, f.Fuse, false);
        wrapper.Add(child);
        halfLayer.Add(wrapper);

        var normal = sliceAngle + (side > 0 ? -Mathf.PI / 2 : Mathf.PI / 2);
        var hf = new Half
        {
            T = f.T,
            R = r,
            X = f.X,
            Y = f.Y,
            Side = side,
            W = w,
            H = h,
            El = wrapper,
            Rot = sliceAngle,
            Vx = f.Vx * 0.5f + Mathf.Cos(normal) * 2.4f + side * 1.1f,
            Vy = Mathf.Min(f.Vy * 0.35f, -1.5f) + Mathf.Sin(normal) * 2.4f,
            RotSpd = side * (0.04f + UnityEngine.Random.Range(0f, 0.03f))
        };
        halves.Add(hf);
    }

    Particle NewParticle(float x, float y, Color c, float mult)
    {
        float ang = UnityEngine.Random.value * Mathf.PI * 2f;
        float spd = (1.8f + UnityEngine.Random.value * 7f) * mult;
        return new Particle
        {
            X = x,
            Y = y,
            C = c,
            Vx = Mathf.Cos(ang) * spd,
            Vy = Mathf.Sin(ang) * spd - 1.5f,
            R = 3.5f + UnityEngine.Random.value * 6f,
            A = 1f,
            Decay = 0.014f + UnityEngine.Random.value * 0.015f
        };
    }

    void AddFloat(float x, float y, string text, Color color, float scale)
    {
        var lbl = new Label(text) { pickingMode = PickingMode.Ignore };
        lbl.style.position = Position.Absolute;
        lbl.style.left = x - 150;
        lbl.style.top = y;
        lbl.style.width = 300;
        lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
        lbl.style.fontSize = 28 * scale;
        lbl.style.color = color;
        lbl.style.opacity = 1;
        labelLayer.Add(lbl);
        floats.Add(new Label2 { El = lbl, A = 1f, X = x, Y = y, Color = color });
    }

    static bool SegCircle(Vector2 a, Vector2 b, Vector2 c, float r)
    {
        var ab = b - a;
        float lenSq = ab.sqrMagnitude;
        if (lenSq < 0.0001f) return Vector2.Distance(c, a) <= r;
        float t = Mathf.Clamp01(Vector2.Dot(c - a, ab) / lenSq);
        var closest = a + ab * t;
        return (c - closest).sqrMagnitude <= r * r;
    }

    // ==========================================
    // GAME FLOW
    // ==========================================
    void SpawnWave(int count, bool extras)
    {
        var regular = Array.FindAll(FruitType.All, f => !f.IsBomb && !f.IsHeart);
        var heart = FruitType.Get("heartfruit");
        var bomb = FruitType.Get("bomb");

        float margin = Mathf.Max(110f, W * 0.16f);
        float usable = Mathf.Max(200f, W - margin * 2f);
        float slot = usable / count;
        bool bombSpawned = false;

        for (int i = 0; i < count; i++)
        {
            var type = regular[UnityEngine.Random.Range(0, regular.Length)];

            if (extras)
            {
                if (!zenMode && lives < MAX_LIVES && UnityEngine.Random.value < 0.14f)
                    type = heart;
                else if (!zenMode && !bombSpawned && UnityEngine.Random.value < 0.08f)
                { type = bomb; bombSpawned = true; }
            }

            float laneX = margin + slot * i + slot * UnityEngine.Random.Range(0.25f, 0.75f);
            var f = new Fruit { T = type, R = type.Radius, X = laneX, Y = H + type.Radius + 8f };

            float peak = UnityEngine.Random.Range(0.62f, 0.78f);
            f.Vy = -Mathf.Sqrt(2f * f.Gravity * (H * peak));
            float frames = Mathf.Abs(f.Vy / f.Gravity) * 2f;
            float targetX = W * UnityEngine.Random.Range(0.3f, 0.7f);
            f.Vx = (targetX - f.X) / Mathf.Max(frames * 0.75f, 55f);
            f.Rot = UnityEngine.Random.value * Mathf.PI * 2f;
            f.RotSpd = (UnityEngine.Random.value - 0.5f) * 0.035f;

            // ---- VisualElement for this fruit ----
            float r = type.Radius;
            var el = new VisualElement { pickingMode = PickingMode.Ignore };
            el.style.position = Position.Absolute;
            el.style.width = r * 2f;
            el.style.height = r * 2f;
            el.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(50));
            el.style.left = f.X - r;
            el.style.top = f.Y - r;
            el.style.rotate = new Rotate(Angle.Radians(f.Rot));

            var painterChild = new PaintLayer();
            painterChild.style.position = Position.Absolute;
            painterChild.style.width = 0; painterChild.style.height = 0;
            painterChild.style.left = r;
            painterChild.style.top = r;

            var capturedType = type;
            painterChild.OnPaint = p => FruitArt.Draw(p, capturedType, capturedType.Radius, 0f);
            el.Add(painterChild);
            fruitContainer.Add(el);
            f.El = el;

            fruits.Add(f);
            if (i == 0) Play(type.IsBomb ? launchBombSfx : launchSfx, 1f);
        }
    }

    void StartNewGame()
    {
        state = State.Playing;
        score = 0; lives = MAX_LIVES;
        scoreValue.text = "0";
        RenderLives();
        startModal.style.display = DisplayStyle.None;
        gameOverModal.style.display = DisplayStyle.None;

        foreach (var f in fruits) if (f.El != null) f.El.RemoveFromHierarchy();
        fruits.Clear();
        foreach (var h in halves) if (h.El != null) h.El.RemoveFromHierarchy();
        halves.Clear();
        foreach (var ft in floats) if (ft.El != null) ft.El.RemoveFromHierarchy();
        floats.Clear();
        particles.Clear();
        stains.Clear();
        blade.Clear();

        SpawnWave(2, false);
        waveCooldown = 115f;
    }

    void GameOver(string reason)
    {
        state = State.GameOver;
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt("FruitSlasherBest", highScore);
            bestValue.text = highScore.ToString();
        }
        finalScoreText.text = score.ToString();
        bestScoreText.text = highScore.ToString();
        gameOverBadge.text = reason == "bomb" ? "💥 সবগুলো লাইফ শেষ!" : "🌟 দারুণ চেষ্টা!";
        gameOverModal.style.display = DisplayStyle.Flex;
    }

    void SelectMode(bool zen)
    {
        zenMode = zen;
        if (zenMode)
        {
            for (int i = fruits.Count - 1; i >= 0; i--)
            {
                if (fruits[i].T.IsBomb)
                {
                    if (fruits[i].El != null) fruits[i].El.RemoveFromHierarchy();
                    fruits.RemoveAt(i);
                }
            }
        }
        UpdateModeUI();
    }

    void ToggleMode() => SelectMode(!zenMode);

    void UpdateModeUI()
    {
        modeIcon.text = zenMode ? "🧘" : "🌿";
        modeLabel.text = zenMode ? "রিল্যাক্স মোড" : "সহজ মোড (৫ লাইফ)";
        RenderLives();
    }

    void RenderLives()
    {
        livesContainer.Clear();
        if (zenMode)
        {
            var lbl = new Label("♾️ আনলিমিটেড");
            lbl.style.color = new Color(0.4f, 0.85f, 0.95f);
            lbl.style.fontSize = 14;
            livesContainer.Add(lbl);
            return;
        }
        for (int i = 0; i < MAX_LIVES; i++)
        {
            var l = new Label(i < lives ? "❤️" : "🖤");
            l.style.fontSize = 20;
            l.style.opacity = i < lives ? 1f : 0.35f;
            livesContainer.Add(l);
        }
    }

    // ==========================================
    // AUDIO
    // ==========================================
    void ToggleSound()
    {
        if (sfx == null) return;
        sfx.mute = !sfx.mute;
        var icon = root.Q<Label>("sound-icon");
        if (icon != null) icon.text = sfx.mute ? "🔇" : "🔊";
    }

    void InitAudio() { /* AudioSource is used directly */ }

    void Play(AudioClip clip, float pitch)
    {
        if (clip == null || sfx == null || sfx.mute) return;
        sfx.pitch = pitch;
        sfx.PlayOneShot(clip);
    }
}