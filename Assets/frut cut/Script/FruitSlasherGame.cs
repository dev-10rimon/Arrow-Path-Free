using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class FruitSlasherGame : MonoBehaviour
{
    [SerializeField] UIDocument document;
    [SerializeField] AudioSource sfx;

    const int MAX_LIVES = 3;
    const float HIT_PAD = 26f;

    enum State { Title, Playing, GameOver }
    State state = State.Title;
    bool zenMode = false;
    bool isPaused = false;

    int score, lives = MAX_LIVES, highScore;
    float waveCooldown, screenFlash, shakeTime;

    class Fruit
    {
        public FruitType T; public float R, X, Y, Vx, Vy, Rot, RotSpd, Fuse, Gravity = 0.135f, Height;
        public bool Sliced, Entered;
        public VisualElement El;
        public void Step(float dt)
        {
            X += Vx * dt; Y += Vy * dt; Vy += Gravity * dt;
            Rot += RotSpd * dt; Fuse += dt;
            if (Y < Height * 0.5f) Entered = true;
        }
    }

    class Half { public FruitType T; public float R, X, Y, Vx, Vy, Rot, RotSpd, W, H; public int Side; public VisualElement El; }
    class Particle { public float X, Y, Vx, Vy, R, A, Decay; public Color C; }
    class Label2 { public VisualElement El; public float A, Y; }

    readonly List<Fruit> fruits = new();
    readonly List<Half> halves = new();
    readonly List<Particle> particles = new();
    readonly List<Label2> floats = new();
    readonly List<(Vector2 p, float t)> blade = new();
    readonly List<Label> heartLabels = new(); // Pre-instantiated heart elements

    VisualElement root, playfield, fruitContainer, halfLayer, labelLayer, inputCatcher, loadingScreen, hud;
    PaintLayer particleLayer, bladeLayer;

    Label scoreValue, bestValue, finalScoreText, bestScoreText, gameOverBadge, modeIcon, modeLabel;
    VisualElement livesContainer, startModal, gameOverModal, settingsModal;

    AudioClip sliceSfx, healSfx, bombSfx, launchSfx, launchBombSfx;

    Vector2 Size => new(playfield.resolvedStyle.width, playfield.resolvedStyle.height);
    float W => Size.x;
    float H => Size.y;

    void Awake()
    {
        highScore = PlayerPrefs.GetInt("FruitSlasherBest", 0);
        sliceSfx = Sfx.Slice(); healSfx = Sfx.Heal();
        bombSfx = Sfx.Bomb(); launchSfx = Sfx.Launch(false); launchBombSfx = Sfx.Launch(true);
    }

    void OnEnable()
    {
        root = document.rootVisualElement;
        playfield = root.Q("playfield");
        inputCatcher = root.Q("input-catcher");
        loadingScreen = root.Q("loading-screen");
        hud = root.Q("hud");

        if (loadingScreen != null)
            loadingScreen.schedule.Execute(() => { loadingScreen.style.display = DisplayStyle.None; }).StartingIn(1500);

        scoreValue = root.Q<Label>("score-value");
        bestValue = root.Q<Label>("best-value");
        livesContainer = root.Q("lives-container");
        startModal = root.Q("start-modal");
        gameOverModal = root.Q("gameover-modal");
        settingsModal = root.Q("settings-modal");
        finalScoreText = root.Q<Label>("final-score");
        bestScoreText = root.Q<Label>("best-score");
        gameOverBadge = root.Q<Label>("gameover-badge");
        modeIcon = root.Q<Label>("mode-icon");
        modeLabel = root.Q<Label>("mode-label");

        // --- INIT HEART LABELS ONCE ---
        InitHearts();

        var bgTex = CreateBackgroundTexture(256, 256);
        playfield.style.backgroundImage = new StyleBackground(bgTex);
        playfield.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);

        fruitContainer = new VisualElement { pickingMode = PickingMode.Ignore };
        fruitContainer.style.position = Position.Absolute;
        fruitContainer.style.left = 0; fruitContainer.style.top = 0;
        fruitContainer.style.right = 0; fruitContainer.style.bottom = 0;
        playfield.Add(fruitContainer);

        halfLayer = MakeOverlay(playfield);
        particleLayer = AddLayer(playfield, DrawParticles);
        bladeLayer = AddLayer(playfield, DrawBlade);
        labelLayer = MakeOverlay(playfield);

        // --- UI BINDINGS ---
        var btnRestart = root.Q<Button>("btn-restart");
        if (btnRestart != null) btnRestart.clicked += () => { settingsModal.style.display = DisplayStyle.None; StartNewGame(); };

        var btnMode = root.Q<Button>("btn-mode");
        if (btnMode != null) btnMode.clicked += ToggleMode;

        var btnSound = root.Q<Button>("btn-sound");
        if (btnSound != null) btnSound.clicked += ToggleSound;

        var btnPlay = root.Q<Button>("btn-play");
        if (btnPlay != null) btnPlay.clicked += StartNewGame;

        var btnAgain = root.Q<Button>("btn-again");
        if (btnAgain != null) btnAgain.clicked += StartNewGame;

        var btnZenOver = root.Q<Button>("btn-zen-over");
        if (btnZenOver != null) btnZenOver.clicked += () => { SelectMode(true); StartNewGame(); };

        var btnSettings = root.Q<Button>("btn-settings");
        if (btnSettings != null) btnSettings.clicked += () => { isPaused = true; settingsModal.style.display = DisplayStyle.Flex; };

        var btnCloseSettings = root.Q<Button>("btn-close-settings");
        if (btnCloseSettings != null) btnCloseSettings.clicked += () => { isPaused = false; settingsModal.style.display = DisplayStyle.None; };

        // --- NEW BUTTONS ---
        var btnHomeOver = root.Q<Button>("btn-home-over");
        if (btnHomeOver != null) btnHomeOver.clicked += GoToHome;

        var btnHomeSettings = root.Q<Button>("btn-home-settings");
        if (btnHomeSettings != null) btnHomeSettings.clicked += GoToHome;

        var btnExit = root.Q<Button>("btn-exit");
        if (btnExit != null) btnExit.clicked += ExitGame;

        inputCatcher.RegisterCallback<PointerDownEvent>(e => { InitAudio(); blade.Clear(); AddBladePoint(e.position); });
        inputCatcher.RegisterCallback<PointerMoveEvent>(e => AddBladePoint(e.position));
        inputCatcher.RegisterCallback<PointerUpEvent>(_ => blade.Clear());
        inputCatcher.RegisterCallback<PointerLeaveEvent>(_ => blade.Clear());
        root.RegisterCallback<PointerDownEvent>(_ => InitAudio(), TrickleDown.TrickleDown);

        bestValue.text = highScore.ToString();
        UpdateModeUI();
        SpawnWave(2, false);
        waveCooldown = 110f;
    }

    void InitHearts()
    {
        if (livesContainer == null) return;
        livesContainer.Clear();
        heartLabels.Clear();

        for (int i = 0; i < MAX_LIVES; i++)
        {
            var l = new Label();
            l.style.fontSize = 38;
            l.style.marginLeft = 6;
            l.style.marginRight = 6;
            l.style.unityFontStyleAndWeight = FontStyle.Bold;
            livesContainer.Add(l);
            heartLabels.Add(l);
        }
    }

    Texture2D CreateBackgroundTexture(int width, int height)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[width * height];
        Vector2 center = new Vector2(width / 2f, height / 2f);
        float maxDist = Mathf.Sqrt(center.x * center.x + center.y * center.y);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                Color baseCol = new Color(0.05f, 0.08f, 0.12f);
                float light = Mathf.Pow(1f - Mathf.Clamp01(dist), 2.5f) * 0.25f;
                pixels[y * width + x] = baseCol + new Color(light * 0.5f, light * 0.6f, light * 0.8f, 0);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    PaintLayer AddLayer(VisualElement parent, Action<Painter2D> draw) { var l = new PaintLayer { OnPaint = draw }; parent.Add(l); return l; }
    static VisualElement MakeOverlay(VisualElement parent) { var e = new VisualElement { pickingMode = PickingMode.Ignore }; e.style.position = Position.Absolute; e.style.left = 0; e.style.top = 0; e.style.right = 0; e.style.bottom = 0; parent.Add(e); return e; }

    void ApplySafeArea(VisualElement element)
    {
        if (Screen.safeArea.width <= 0) return;
        Rect safeArea = Screen.safeArea;
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);

        float left = safeArea.xMin / screenSize.x;
        float right = 1f - (safeArea.xMax / screenSize.x);
        float top = 1f - (safeArea.yMax / screenSize.y);
        float bottom = safeArea.yMin / screenSize.y;

        element.style.paddingLeft = Length.Percent(left * 100f);
        element.style.paddingRight = Length.Percent(right * 100f);
        element.style.paddingTop = Length.Percent(top * 100f);
        element.style.paddingBottom = Length.Percent(bottom * 100f);
    }

    void Update()
    {
        if (hud != null) ApplySafeArea(hud);

        if (isPaused || state == State.GameOver) return;

        float dt = Time.smoothDeltaTime * 60f;
        dt = Mathf.Clamp(dt, 0.1f, 3.0f);
        if (float.IsNaN(W) || W < 50f) return;

        if (shakeTime > 0f) shakeTime -= dt;
        waveCooldown -= dt;

        float diff = Mathf.Clamp01(score / 150f);

        if (waveCooldown <= 0f && fruits.Count <= 1)
        {
            if (state == State.Playing)
            {
                int maxWave = 1 + (int)(diff * 3);
                int waveSize = UnityEngine.Random.Range(1, maxWave + 1);
                SpawnWave(waveSize, true);
                waveCooldown = Mathf.Lerp(110f, 35f, diff);
            }
            else { if (fruits.Count == 0) SpawnWave(2, false); waveCooldown = 120f; }
        }

        for (int i = fruits.Count - 1; i >= 0; i--)
        {
            var f = fruits[i]; f.Height = H; f.Step(dt);
            if (f.El != null)
            {
                f.El.style.translate = new Translate(new Length(f.X - f.R, LengthUnit.Pixel), new Length(f.Y - f.R, LengthUnit.Pixel), 0);
                f.El.style.rotate = new Rotate(Angle.Radians(f.Rot));
            }
            if (!f.Sliced && !f.T.IsBomb && blade.Count > 0)
            {
                var last = blade[blade.Count - 1];
                if (Time.time - last.t < 0.25f && Vector2.Distance(new Vector2(f.X, f.Y), last.p) <= f.R + HIT_PAD) { AddBladePoint(last.p); break; }
            }
            if (f.Vy > 0f && f.Y > H + f.R + 45f)
            {
                if (state == State.Playing && !zenMode && !f.T.IsBomb && !f.T.IsHeart && f.Entered)
                {
                    lives = Mathf.Max(0, lives - 1); RenderLives(); AddFloat(Mathf.Clamp(f.X, 80, W - 80), H - 45, "Miss! -1 ❤️", new Color(1f, 0.4f, 0.5f), 1f);
                    if (lives <= 0) { if (f.El != null) f.El.RemoveFromHierarchy(); fruits.RemoveAt(i); GameOver("missed"); break; }
                }
                if (f.El != null) f.El.RemoveFromHierarchy(); fruits.RemoveAt(i);
            }
        }

        for (int i = halves.Count - 1; i >= 0; i--)
        {
            var h = halves[i]; h.X += h.Vx * dt; h.Y += h.Vy * dt; h.Vy += 0.20f * dt; h.Rot += h.RotSpd * dt;
            if (h.El != null)
            {
                h.El.style.translate = new Translate(new Length(h.X - h.W * 0.5f, LengthUnit.Pixel), new Length(h.Side > 0 ? h.Y - h.H : h.Y, LengthUnit.Pixel), 0);
                h.El.style.rotate = new Rotate(Angle.Radians(h.Rot));
            }
            if (h.Y > H + 120f) { if (h.El != null) h.El.RemoveFromHierarchy(); halves.RemoveAt(i); }
        }

        for (int i = particles.Count - 1; i >= 0; i--) { var p = particles[i]; p.X += p.Vx * dt; p.Y += p.Vy * dt; p.Vy += 0.18f * dt; p.A -= p.Decay * dt; if (p.A <= 0f) particles.RemoveAt(i); }
        for (int i = floats.Count - 1; i >= 0; i--) { var ft = floats[i]; ft.A -= 0.016f * dt; ft.Y += -1.6f * dt; ft.El.style.top = ft.Y; ft.El.style.opacity = Mathf.Max(0f, ft.A); if (ft.A <= 0f) { ft.El.RemoveFromHierarchy(); floats.RemoveAt(i); } }

        float now = Time.time;
        blade.RemoveAll(b => now - b.t > 0.17f);
        if (screenFlash > 0f) screenFlash -= 0.04f * dt;

        particleLayer.MarkDirtyRepaint(); bladeLayer.MarkDirtyRepaint();
    }

    void DrawParticles(Painter2D p)
    {
        foreach (var pt in particles)
        {
            if (pt.A <= 0f) continue;
            p.BeginPath(); p.Arc(new Vector2(pt.X, pt.Y), pt.R, Angle.Degrees(0), Angle.Degrees(360), ArcDirection.Clockwise);
            p.fillColor = new Color(pt.C.r, pt.C.g, pt.C.b, Mathf.Clamp01(pt.A)); p.Fill();
        }
    }

    void DrawBlade(Painter2D p)
    {
        if (blade.Count < 2) return;
        for (int i = 1; i < blade.Count; i++)
        {
            float ratio = (float)i / blade.Count;
            p.BeginPath(); p.MoveTo(blade[i - 1].p); p.LineTo(blade[i].p);
            p.strokeColor = new Color(0.2f, 0.6f, 1f, ratio * 0.5f); p.lineWidth = ratio * 24f; p.lineCap = LineCap.Round; p.Stroke();

            p.BeginPath(); p.MoveTo(blade[i - 1].p); p.LineTo(blade[i].p);
            p.strokeColor = new Color(1f, 1f, 1f, ratio); p.lineWidth = ratio * 8f; p.lineCap = LineCap.Round; p.Stroke();
        }
    }

    void AddBladePoint(Vector2 pt)
    {
        if (isPaused || state == State.GameOver) return;

        blade.Add((pt, Time.time)); if (blade.Count > 18) blade.RemoveAt(0); if (blade.Count < 2) return;
        var p1 = blade[blade.Count - 2].p; var p2 = blade[blade.Count - 1].p;
        if (Vector2.Distance(p1, p2) < 2f) return;
        float sliceAngle = Mathf.Atan2(p2.y - p1.y, p2.x - p1.x);
        var sliced = new List<Fruit>();

        for (int i = fruits.Count - 1; i >= 0; i--)
        {
            var f = fruits[i]; if (f.Sliced) continue;
            float effR = f.T.IsBomb ? f.R * 0.80f : f.R + HIT_PAD;
            if (!SegCircle(p1, p2, new Vector2(f.X, f.Y), effR)) continue;
            f.Sliced = true;

            if (f.T.IsBomb)
            {
                Play(bombSfx, 1f); screenFlash = 0.55f; shakeTime = 14f;
                for (int k = 0; k < 28; k++) particles.Add(NewParticle(f.X, f.Y, k % 2 == 0 ? new Color(1f, 0.5f, 0.1f) : new Color(1f, 0.9f, 0.3f), 1.4f));
                if (f.El != null) f.El.RemoveFromHierarchy(); fruits.RemoveAt(i);

                if (state == State.Playing && !zenMode)
                {
                    lives = 0;
                    RenderLives();
                    AddFloat(f.X, f.Y, "BOMB! GAME OVER 💥", new Color(1f, 0.3f, 0.4f), 1.5f);
                    GameOver("bomb");
                    return;
                }
                continue;
            }
            sliced.Add(f); if (f.El != null) f.El.RemoveFromHierarchy(); fruits.RemoveAt(i);
        }

        if (sliced.Count == 0) return;
        Play(sliceSfx, 1f + UnityEngine.Random.Range(-0.1f, 0.1f));

        foreach (var f in sliced)
        {
            SpawnHalf(f, sliceAngle, +1); SpawnHalf(f, sliceAngle, -1);
            for (int k = 0; k < 25; k++) particles.Add(NewParticle(f.X, f.Y, f.T.Juice, 1.5f));

            if (f.T.IsHeart)
            {
                Play(healSfx, 1f);
                if (state == State.Playing && !zenMode && lives < MAX_LIVES) { lives++; RenderLives(); AddFloat(f.X, f.Y - 20, "+1 Life ❤️", new Color(1f, 0.4f, 0.8f), 1.4f); }
                else AddFloat(f.X, f.Y - 20, "Bonus +5 💖", new Color(1f, 0.4f, 0.8f), 1.3f);
            }
            else AddFloat(f.X, f.Y - 10, $"+{f.T.Points}", f.T.Juice, 1.1f);
            if (state == State.Playing) score += f.T.Points;
        }

        if (sliced.Count >= 2)
        {
            int bonus = sliced.Count * 2; if (state == State.Playing) score += bonus;
            var last = sliced[sliced.Count - 1];
            AddFloat(last.X, last.Y - 48, $"Combo +{bonus}! ✨", new Color(1f, 0.9f, 0.2f), 1.4f);
        }
        if (state == State.Playing)
        {
            scoreValue.text = score.ToString();
            if (score > highScore) { highScore = score; bestValue.text = highScore.ToString(); PlayerPrefs.SetInt("FruitSlasherBest", highScore); }
        }
    }

    void SpawnHalf(Fruit f, float sliceAngle, int side)
    {
        float r = f.R, w = 2f * r, h = r;

        var wrapper = new VisualElement { pickingMode = PickingMode.Ignore };
        wrapper.style.position = Position.Absolute;
        wrapper.style.width = w; wrapper.style.height = h;
        wrapper.style.transformOrigin = side > 0 ? new TransformOrigin(Length.Percent(50), Length.Percent(100)) : new TransformOrigin(Length.Percent(50), Length.Percent(0));

        var child = new PaintLayer();
        child.style.width = w; child.style.height = h; child.style.left = 0; child.style.top = 0;
        child.OnPaint = p => FruitArt.DrawHalf(p, f.T, new Vector2(r, side > 0 ? r : 0), r, side);

        wrapper.Add(child); halfLayer.Add(wrapper);

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
            Vx = f.Vx * 0.5f + Mathf.Cos(normal) * 4.0f + side * 1.5f,
            Vy = Mathf.Min(f.Vy * 0.35f, -1.5f) + Mathf.Sin(normal) * 4.0f,
            RotSpd = side * (0.06f + UnityEngine.Random.Range(0f, 0.04f))
        };
        halves.Add(hf);
    }

    Particle NewParticle(float x, float y, Color c, float mult)
    {
        float ang = UnityEngine.Random.value * Mathf.PI * 2f; float spd = (1.8f + UnityEngine.Random.value * 7f) * mult;
        return new Particle { X = x, Y = y, C = c, Vx = Mathf.Cos(ang) * spd, Vy = Mathf.Sin(ang) * spd - 1.5f, R = 4f + UnityEngine.Random.value * 7f, A = 1f, Decay = 0.014f + UnityEngine.Random.value * 0.015f };
    }

    void AddFloat(float x, float y, string text, Color color, float scale)
    {
        var lbl = new Label(text) { pickingMode = PickingMode.Ignore };
        lbl.style.position = Position.Absolute; lbl.style.left = x - 150; lbl.style.top = y; lbl.style.width = 300;
        lbl.style.unityTextAlign = TextAnchor.MiddleCenter; lbl.style.fontSize = 32 * scale; lbl.style.color = color;
        lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        lbl.style.textShadow = new TextShadow { color = new Color(0, 0, 0, 0.8f), offset = new Vector2(2, 2), blurRadius = 4 };
        labelLayer.Add(lbl); floats.Add(new Label2 { El = lbl, A = 1f, Y = y });
    }

    static bool SegCircle(Vector2 a, Vector2 b, Vector2 c, float r)
    {
        var ab = b - a; float lenSq = ab.sqrMagnitude;
        if (lenSq < 0.0001f) return Vector2.Distance(c, a) <= r;
        float t = Mathf.Clamp01(Vector2.Dot(c - a, ab) / lenSq); var closest = a + ab * t;
        return (c - closest).sqrMagnitude <= r * r;
    }

    void SpawnWave(int count, bool extras)
    {
        var regular = Array.FindAll(FruitType.All, f => !f.IsBomb && !f.IsHeart);
        var heart = FruitType.Get("heartfruit"); var bomb = FruitType.Get("bomb");
        float margin = Mathf.Max(110f, W * 0.16f); float usable = Mathf.Max(200f, W - margin * 2f);
        float slot = usable / count; bool bombSpawned = false;

        float diff = Mathf.Clamp01(score / 150f);
        float bombChance = Mathf.Lerp(0.05f, 0.25f, diff);
        float speedMult = Mathf.Lerp(1.0f, 1.6f, diff);

        for (int i = 0; i < count; i++)
        {
            var type = regular[UnityEngine.Random.Range(0, regular.Length)];
            if (extras)
            {
                if (!zenMode && lives < MAX_LIVES && UnityEngine.Random.value < 0.12f) type = heart;
                else if (!zenMode && !bombSpawned && UnityEngine.Random.value < bombChance) { type = bomb; bombSpawned = true; }
            }

            float laneX = margin + slot * i + slot * UnityEngine.Random.Range(0.25f, 0.75f);
            var f = new Fruit { T = type, R = type.Radius, X = laneX, Y = H + type.Radius + 8f };
            float peak = UnityEngine.Random.Range(0.45f, 0.65f);
            f.Vy = -Mathf.Sqrt(2f * f.Gravity * (H * peak)) * speedMult;
            float frames = Mathf.Abs(f.Vy / f.Gravity) * 2f; float targetX = W * UnityEngine.Random.Range(0.3f, 0.7f);
            f.Vx = (targetX - f.X) / Mathf.Max(frames * 0.75f, 55f);
            f.Rot = UnityEngine.Random.value * Mathf.PI * 2f; f.RotSpd = (UnityEngine.Random.value - 0.5f) * 0.035f;

            float r = type.Radius; var el = new VisualElement { pickingMode = PickingMode.Ignore };
            el.style.position = Position.Absolute; el.style.width = r * 2f; el.style.height = r * 2f;
            el.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(50));

            var painterChild = new PaintLayer();
            painterChild.style.left = r; painterChild.style.top = r;
            var capturedType = type;
            painterChild.OnPaint = p => FruitArt.Draw(p, capturedType, capturedType.Radius, 0f);

            el.Add(painterChild); fruitContainer.Add(el); f.El = el; fruits.Add(f);
            if (i == 0) Play(type.IsBomb ? launchBombSfx : launchSfx, 1f);
        }
    }

    void StartNewGame()
    {
        state = State.Playing; score = 0; lives = MAX_LIVES; scoreValue.text = "0";
        startModal.style.display = DisplayStyle.None; gameOverModal.style.display = DisplayStyle.None;
        settingsModal.style.display = DisplayStyle.None;
        isPaused = false;

        RenderLives();

        foreach (var f in fruits) if (f.El != null) f.El.RemoveFromHierarchy(); fruits.Clear();
        foreach (var h in halves) if (h.El != null) h.El.RemoveFromHierarchy(); halves.Clear();
        foreach (var ft in floats) if (ft.El != null) ft.El.RemoveFromHierarchy(); floats.Clear();
        particles.Clear(); blade.Clear();
        SpawnWave(2, false); waveCooldown = 115f;
    }

    void GameOver(string reason)
    {
        state = State.GameOver;
        isPaused = true;
        if (score > highScore) { highScore = score; PlayerPrefs.SetInt("FruitSlasherBest", highScore); bestValue.text = highScore.ToString(); }
        finalScoreText.text = score.ToString(); bestScoreText.text = highScore.ToString();
        gameOverBadge.text = reason == "bomb" ? "💥 Out of Lives!" : "🌟 Great Try!";
        gameOverModal.style.display = DisplayStyle.Flex;
    }

    void GoToHome()
    {
        state = State.Title;
        isPaused = false;
        settingsModal.style.display = DisplayStyle.None;
        gameOverModal.style.display = DisplayStyle.None;
        startModal.style.display = DisplayStyle.Flex;

        score = 0; lives = MAX_LIVES; scoreValue.text = "0";
        RenderLives();

        foreach (var f in fruits) if (f.El != null) f.El.RemoveFromHierarchy(); fruits.Clear();
        foreach (var h in halves) if (h.El != null) h.El.RemoveFromHierarchy(); halves.Clear();
        foreach (var ft in floats) if (ft.El != null) ft.El.RemoveFromHierarchy(); floats.Clear();
        particles.Clear(); blade.Clear();

        SpawnWave(2, false);
        waveCooldown = 110f;
    }

    void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    void SelectMode(bool zen) { zenMode = zen; if (zenMode) { for (int i = fruits.Count - 1; i >= 0; i--) { if (fruits[i].T.IsBomb) { if (fruits[i].El != null) fruits[i].El.RemoveFromHierarchy(); fruits.RemoveAt(i); } } } UpdateModeUI(); }
    void ToggleMode() => SelectMode(!zenMode);

    void UpdateModeUI() { modeIcon.text = zenMode ? "🧘" : "🌿"; modeLabel.text = zenMode ? "Zen Mode" : "Easy Mode"; RenderLives(); }

    // --- UI RENDERER ---
    void RenderLives()
    {
        if (heartLabels.Count == 0) return;

        if (zenMode)
        {
            heartLabels[0].text = "♾️";
            heartLabels[0].style.color = new Color(0.4f, 0.85f, 0.95f);
            for (int i = 1; i < heartLabels.Count; i++) heartLabels[i].style.display = DisplayStyle.None;
            return;
        }

        for (int i = 0; i < heartLabels.Count; i++)
        {
            heartLabels[i].style.display = DisplayStyle.Flex;
            heartLabels[i].text = i < lives ? "♥" : "♡";
            heartLabels[i].style.color = i < lives ? new Color(1f, 0.2f, 0.3f) : new Color(0.3f, 0.3f, 0.3f);
        }
    }

    void ToggleSound() { if (sfx == null) return; sfx.mute = !sfx.mute; var icon = root.Q<Label>("sound-icon"); if (icon != null) icon.text = sfx.mute ? "🔇" : "🔊"; }
    void InitAudio() { }
    void Play(AudioClip clip, float pitch) { if (clip == null || sfx == null || sfx.mute) return; sfx.pitch = pitch; sfx.PlayOneShot(clip); }
}