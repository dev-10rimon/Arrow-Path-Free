using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class KnifeHitGame : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private AudioSource audioSource;

    const float KNIFE_SPEED = 2200f;
    const float MIN_GAP_RAD = 0.22f;
    const float KNIFE_W = 22f, KNIFE_H = 90f;

    enum State { Loading, Home, Settings, Playing, GameOver }
    State currentState = State.Loading;

    int stage = 1, score = 0, highScore = 0;
    int remainingKnives = 7, stageTargetKnives = 7;
    bool stageClearing = false;

    float logAngleDeg = 0f, logSpeedDeg = 120f;
    string logType = "wood";
    Vector2 logCenter;
    float logRadius;

    readonly List<float> stuckAngles = new();
    readonly List<KnifeVisual> stuckVisuals = new();
    KnifeVisual flyingKnife = null;
    float flyingY, throwStartY;

    readonly List<ShatterPiece> shatterPieces = new();
    readonly List<BounceKnife> bouncing = new();
    readonly List<ParticleVisual> particles = new();

    VisualElement root, gameArea, background;
    VisualElement loadingPanel, homePanel, settingsPanel, gameoverPanel;
    VisualElement topHud, knivesIndicator, statsContainer;
    Label stageLabel, bossLabel, scoreText, overlayTitle, finalStage, finalScore, bestScore;

    Button btnPlay, btnSettings, btnExit, btnSound, btnBack, btnRetry, btnHome;
    VisualElement loadingFill;
    BackgroundVisual bgVisual;
    LogVisual logVisual;

    AudioClip hitSnd, clankSnd, clearSnd;
    bool soundOn = true;

    void Awake()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        highScore = PlayerPrefs.GetInt("knife_hit_high", 0);
    }

    void OnEnable()
    {
        if (uiDocument == null) return;
        root = uiDocument.rootVisualElement;

        gameArea = root.Q("game-area");
        background = root.Q("background");
        loadingPanel = root.Q("loading-panel");
        homePanel = root.Q("home-panel");
        settingsPanel = root.Q("settings-panel");
        gameoverPanel = root.Q("gameover-panel");
        topHud = root.Q("top-hud");
        knivesIndicator = root.Q("knives-indicator");
        statsContainer = root.Q("stats-container");

        stageLabel = root.Q<Label>("stage-label");
        bossLabel = root.Q<Label>("boss-label");
        scoreText = root.Q<Label>("score-text");
        overlayTitle = root.Q<Label>("overlay-title");
        finalStage = root.Q<Label>("final-stage");
        finalScore = root.Q<Label>("final-score");
        bestScore = root.Q<Label>("best-score");
        loadingFill = root.Q("loading-bar-fill");

        btnPlay = root.Q<Button>("btn-play");
        btnSettings = root.Q<Button>("btn-settings");
        btnExit = root.Q<Button>("btn-exit");
        btnSound = root.Q<Button>("btn-sound");
        btnBack = root.Q<Button>("btn-back");
        btnRetry = root.Q<Button>("btn-retry");
        btnHome = root.Q<Button>("btn-home");

        background.pickingMode = PickingMode.Ignore;

        btnPlay.clicked += () => SwitchState(State.Playing);
        btnSettings.clicked += () => SwitchState(State.Settings);
        btnBack.clicked += () => SwitchState(State.Home);
        btnRetry.clicked += () => SwitchState(State.Playing);
        btnHome.clicked += () => SwitchState(State.Home);
        btnExit.clicked += QuitGame;
        btnSound.clicked += ToggleSound;

        gameArea.RegisterCallback<PointerDownEvent>(OnTap);
        gameArea.RegisterCallback<GeometryChangedEvent>(_ => { if (currentState == State.Playing) RecomputeLayout(); });

        hitSnd = ProceduralAudio.GenerateHit();
        clankSnd = ProceduralAudio.GenerateClank();
        clearSnd = ProceduralAudio.GenerateStageClear();

        bgVisual = new BackgroundVisual();
        background.Add(bgVisual);

        SwitchState(State.Loading);
        SimulateLoading();
    }

    void SimulateLoading()
    {
        float progress = 0f;

        // ✅ FIX: timer ভেরিয়েবলটি আগে ডিক্লেয়ার করা হয়েছে
        IVisualElementScheduledItem timer = null;
        timer = root.schedule.Execute(() => {
            progress += 10f;
            loadingFill.style.width = Length.Percent(progress);
            if (progress >= 100f)
            {
                if (timer != null) timer.Pause();
                SwitchState(State.Home);
            }
        }).Every(50);
    }

    void SwitchState(State newState)
    {
        currentState = newState;

        loadingPanel.style.display = DisplayStyle.None;
        homePanel.style.display = DisplayStyle.None;
        settingsPanel.style.display = DisplayStyle.None;
        gameoverPanel.style.display = DisplayStyle.None;
        topHud.style.display = DisplayStyle.None;
        knivesIndicator.style.display = DisplayStyle.None;

        switch (currentState)
        {
            case State.Home:
                homePanel.style.display = DisplayStyle.Flex;
                break;
            case State.Settings:
                settingsPanel.style.display = DisplayStyle.Flex;
                break;
            case State.Playing:
                topHud.style.display = DisplayStyle.Flex;
                knivesIndicator.style.display = DisplayStyle.Flex;
                StartGame();
                break;
            case State.GameOver:
                gameoverPanel.style.display = DisplayStyle.Flex;
                break;
        }
    }

    void StartGame()
    {
        score = 0; scoreText.text = "0";
        stageClearing = false;

        if (logVisual != null) { logVisual.RemoveFromHierarchy(); logVisual = null; }
        foreach (var kv in stuckVisuals) kv.RemoveFromHierarchy();
        stuckVisuals.Clear(); stuckAngles.Clear();

        InitStage(1);

        root.schedule.Execute(() => {
            RecomputeLayout();
        }).StartingIn(100);
    }

    void RecomputeLayout()
    {
        if (gameArea == null) return;
        float w = gameArea.resolvedStyle.width, h = gameArea.resolvedStyle.height;
        if (w <= 100 || h <= 100) return;

        logCenter = new Vector2(w * .5f, h * .3f);
        logRadius = Mathf.Min(w, h) * .16f;
        throwStartY = h * .85f;

        if (logVisual == null)
        {
            logVisual = new LogVisual();
            gameArea.Add(logVisual);
        }
        logVisual.style.width = logRadius * 2;
        logVisual.style.height = logRadius * 2;
        logVisual.style.left = logCenter.x - logRadius;
        logVisual.style.top = logCenter.y - logRadius;
        logVisual.style.display = DisplayStyle.Flex;
    }

    void OnTap(PointerDownEvent evt) { if (currentState == State.Playing && !stageClearing) TryThrow(); }

    void Update()
    {
        float dt = Time.deltaTime;
        if (bgVisual != null) bgVisual.Update(dt);

        if (currentState == State.Playing && logVisual == null) RecomputeLayout();

        if (currentState == State.Playing && !stageClearing)
        {
            logAngleDeg += logSpeedDeg * dt;
            if (logVisual != null) logVisual.style.rotate = new Rotate(new Angle(logAngleDeg, AngleUnit.Degree));

            UpdateStuckPositions();

            if (flyingKnife != null)
            {
                flyingY -= KNIFE_SPEED * dt;
                flyingKnife.style.top = flyingY;

                if (flyingY <= logCenter.y + logRadius - 15f) ResolveHit();
            }
        }

        for (int i = particles.Count - 1; i >= 0; i--)
        {
            var p = particles[i]; p.Update(dt);
            if (p.Dead) { p.RemoveFromHierarchy(); particles.RemoveAt(i); }
        }

        for (int i = shatterPieces.Count - 1; i >= 0; i--)
        {
            var sp = shatterPieces[i];
            sp.vy += 1400 * dt;
            sp.x += sp.vx * dt; sp.y += sp.vy * dt;
            sp.rot += sp.vrot * dt;
            sp.visual.style.left = sp.x - sp.size * .5f;
            sp.visual.style.top = sp.y - sp.size * .5f;
            sp.visual.style.rotate = new Rotate(new Angle(sp.rot, AngleUnit.Degree));
            if (sp.y > gameArea.resolvedStyle.height + 200)
            { sp.visual.RemoveFromHierarchy(); shatterPieces.RemoveAt(i); }
        }

        for (int i = bouncing.Count - 1; i >= 0; i--)
        {
            var bk = bouncing[i];
            bk.vy += 1400 * dt; bk.x += bk.vx * dt; bk.y += bk.vy * dt; bk.rot += bk.vrot * dt;
            bk.visual.style.left = bk.x; bk.visual.style.top = bk.y;
            bk.visual.style.rotate = new Rotate(new Angle(bk.rot, AngleUnit.Degree));
            if (bk.y > gameArea.resolvedStyle.height + 200)
            { bk.visual.RemoveFromHierarchy(); bouncing.RemoveAt(i); }
        }
    }

    void TryThrow()
    {
        if (flyingKnife != null || remainingKnives <= 0) return;
        remainingKnives--; UpdateKnivesUI();

        flyingKnife = new KnifeVisual();
        flyingKnife.style.width = KNIFE_W; flyingKnife.style.height = KNIFE_H;
        flyingKnife.style.left = logCenter.x - KNIFE_W * .5f;
        flyingKnife.style.top = throwStartY;
        flyingKnife.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(0));
        flyingY = throwStartY;
        gameArea.Add(flyingKnife);
    }

    void ResolveHit()
    {
        float relAngle = Norm(Mathf.PI * .5f - logAngleDeg * Mathf.Deg2Rad);
        foreach (var a in stuckAngles)
        {
            if (Mathf.Abs(Norm(relAngle - a)) < MIN_GAP_RAD) { TriggerGameOver(); return; }
        }

        stuckAngles.Add(relAngle);
        score++; scoreText.text = score.ToString();
        if (flyingKnife != null) { flyingKnife.RemoveFromHierarchy(); flyingKnife = null; }

        var kv = new KnifeVisual();
        kv.style.width = KNIFE_W; kv.style.height = KNIFE_H;
        kv.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(0));
        gameArea.Add(kv); stuckVisuals.Add(kv);
        UpdateStuckPositions();

        if (soundOn) audioSource.PlayOneShot(hitSnd);

        if (remainingKnives == 0)
        {
            stageClearing = true;
            if (soundOn) audioSource.PlayOneShot(clearSnd);
            BreakBoard();
        }
    }

    void BreakBoard()
    {
        if (logVisual != null) logVisual.style.display = DisplayStyle.None;
        foreach (var kv in stuckVisuals) kv.RemoveFromHierarchy();
        stuckVisuals.Clear(); stuckAngles.Clear();

        var cfg = GetLogColor();
        int piecesCount = 15;
        for (int i = 0; i < piecesCount; i++)
        {
            float angle = (360f / piecesCount) * i;
            float rad = angle * Mathf.Deg2Rad;

            Vector2[] pts = new Vector2[] {
                new Vector2(0, 0),
                new Vector2(Random.Range(15, 30), Random.Range(5, 15)),
                new Vector2(Random.Range(-15, -5), Random.Range(20, 35))
            };

            var piece = new ShatterPieceVisual(cfg.outer, pts);
            float size = 40f;
            piece.style.width = size; piece.style.height = size;

            shatterPieces.Add(new ShatterPiece
            {
                x = logCenter.x + Mathf.Cos(rad) * logRadius * .5f,
                y = logCenter.y + Mathf.Sin(rad) * logRadius * .5f,
                vx = Mathf.Cos(rad) * Random.Range(200f, 500f),
                vy = Mathf.Sin(rad) * Random.Range(200f, 500f) - 300f,
                rot = angle,
                vrot = Random.Range(-360f, 360f),
                size = size,
                visual = piece
            });
            gameArea.Add(piece);
        }

        gameArea.schedule.Execute(() => {
            foreach (var sp in shatterPieces) sp.visual.RemoveFromHierarchy();
            shatterPieces.Clear();

            InitStage(stage + 1);
            if (logVisual != null) logVisual.style.display = DisplayStyle.Flex;
            stageClearing = false;
        }).StartingIn(1200);
    }

    (Color outer, Color inner, Color ring) GetLogColor()
    {
        switch (logType)
        {
            case "watermelon": return (new Color(.18f, .49f, .20f), new Color(.78f, .16f, .16f), new Color(.11f, .37f, .13f));
            case "shield": return (new Color(.27f, .35f, .39f), new Color(.47f, .56f, .61f), new Color(.15f, .20f, .22f));
            case "cheese": return (new Color(.98f, .75f, .18f), new Color(1f, .945f, .463f), new Color(.96f, .498f, .09f));
            default: return (new Color(.545f, .353f, .169f), new Color(.627f, .322f, .176f), new Color(.42f, .259f, .149f));
        }
    }

    void UpdateStuckPositions()
    {
        if (logRadius <= 0) return;
        float logRad = logAngleDeg * Mathf.Deg2Rad;
        for (int i = 0; i < stuckAngles.Count && i < stuckVisuals.Count; i++)
        {
            float world = logRad + stuckAngles[i];
            Vector2 outward = new(Mathf.Cos(world), Mathf.Sin(world));
            Vector2 tip = logCenter + outward * (logRadius - 15f);
            var kv = stuckVisuals[i];
            kv.style.left = tip.x - KNIFE_W * .5f;
            kv.style.top = tip.y;
            float rotDeg = Mathf.Atan2(outward.y, outward.x) * Mathf.Rad2Deg - 90f;
            kv.style.rotate = new Rotate(new Angle(rotDeg, AngleUnit.Degree));
        }
    }

    static float Norm(float a)
    {
        a = (a + Mathf.PI) % (Mathf.PI * 2);
        if (a < 0) a += Mathf.PI * 2;
        return a - Mathf.PI;
    }

    void InitStage(int s)
    {
        stage = s;
        bool boss = stage % 5 == 0;
        stageLabel.text = $"STAGE {stage}";

        if (boss)
        {
            bossLabel.AddToClassList("visible");
            string[] bt = { "watermelon", "shield", "cheese" };
            logType = bt[((stage / 5) - 1) % 3];
            stageTargetKnives = 9 + Mathf.Min(stage / 5, 4);
        }
        else
        {
            bossLabel.RemoveFromClassList("visible");
            logType = "wood";
            stageTargetKnives = Mathf.Min(6 + stage / 2, 12);
        }

        remainingKnives = stageTargetKnives;
        logSpeedDeg = (120f + stage * 20f) * (Random.value > .5f ? 1f : -1f);
        logAngleDeg = 0f;

        if (logVisual != null)
        {
            var cfg = GetLogColor();
            logVisual.Setup(cfg.outer, cfg.inner, cfg.ring, logType);
        }

        UpdateKnivesUI();
    }

    void UpdateKnivesUI()
    {
        knivesIndicator.Clear();
        for (int i = 0; i < stageTargetKnives; i++)
        {
            var icon = new KnifeIcon { Available = i < remainingKnives };
            knivesIndicator.Add(icon);
        }
    }

    void TriggerGameOver()
    {
        SwitchState(State.GameOver);
        if (soundOn) audioSource.PlayOneShot(clankSnd);

        if (flyingKnife != null)
        {
            bouncing.Add(new BounceKnife
            {
                x = logCenter.x - KNIFE_W * .5f,
                y = flyingY,
                vx = Random.Range(-500f, 500f),
                vy = -600f,
                rot = 0f,
                vrot = Random.Range(-360f, 360f),
                visual = flyingKnife
            });
            flyingKnife = null;
        }

        if (score > highScore)
        {
            highScore = score; PlayerPrefs.SetInt("knife_hit_high", highScore); PlayerPrefs.Save();
        }

        finalStage.text = stage.ToString();
        finalScore.text = score.ToString();
        bestScore.text = highScore.ToString();
    }

    void ToggleSound()
    {
        soundOn = !soundOn;
        btnSound.text = soundOn ? "SOUND: ON" : "SOUND: OFF";
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    class ShatterPiece { public float x, y, vx, vy, rot, vrot, size; public ShatterPieceVisual visual; }
    class BounceKnife { public float x, y, vx, vy, rot, vrot; public KnifeVisual visual; }
}