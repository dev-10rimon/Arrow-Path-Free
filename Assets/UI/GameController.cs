using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class GameController : MonoBehaviour
{
    public UIDocument uiDocument;

    [Header("Audio Settings (Optional)")]
    public AudioSource sfxSource;
    public AudioClip popSound;
    public AudioClip winSound;

    // UI Elements
    private VisualElement root, gameBoard, animLayer, effectLayer, victoryModal, liveBgLayer;
    private Label lblLevel, lblMoves, lblTime, finalMoves, finalTime;
    private Button btnUndo, btnRestart, btnAddTube, btnNextLevel, btnModalContinue;
    private DropdownField dropdownDifficulty;

    // Game State
    private int currentLevel = 1;
    private int moves = 0;
    private int timerSeconds = 0;
    private bool isAnimating = false;
    private bool isTimerRunning = false;
    private string difficulty = "Medium"; // Easy, Medium, Hard

    private List<List<int>> tubes = new List<List<int>>();
    private int selectedTubeIndex = -1;
    private List<List<List<int>>> history = new List<List<List<int>>>();

    private const int TUBE_CAPACITY = 4;
    private string[] colorNames = { "red", "blue", "green", "yellow", "purple", "orange", "pink", "cyan" };

    private Coroutine timerCoroutine;
    private List<VisualElement> bgOrbs = new List<VisualElement>();
    private List<Vector2> bgSpeeds = new List<Vector2>();

    void Start()
    {
        root = uiDocument.rootVisualElement;

        // UI Bindings
        liveBgLayer = root.Q<VisualElement>("live-bg-layer");
        gameBoard = root.Q<VisualElement>("game-board");
        animLayer = root.Q<VisualElement>("anim-layer");
        effectLayer = root.Q<VisualElement>("effect-layer");
        victoryModal = root.Q<VisualElement>("victory-modal");

        lblLevel = root.Q<Label>("lbl-level");
        lblMoves = root.Q<Label>("lbl-moves");
        lblTime = root.Q<Label>("lbl-time");
        finalMoves = root.Q<Label>("final-moves");
        finalTime = root.Q<Label>("final-time");

        btnUndo = root.Q<Button>("btn-undo");
        btnRestart = root.Q<Button>("btn-restart");
        btnAddTube = root.Q<Button>("btn-add-tube");
        btnNextLevel = root.Q<Button>("btn-next-level");
        btnModalContinue = root.Q<Button>("btn-modal-continue");
        dropdownDifficulty = root.Q<DropdownField>("dropdown-difficulty");

        // Button Events
        btnUndo.clicked += UndoMove;
        btnRestart.clicked += RestartLevel;
        btnAddTube.clicked += AddExtraTube;
        btnNextLevel.clicked += NextLevel;
        btnModalContinue.clicked += NextLevelFromModal;
        dropdownDifficulty.RegisterValueChangedCallback(evt => {
            difficulty = evt.newValue;
            currentLevel = 1;
            StartNewLevel();
        });

        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

        // 🔥 Start Live Background & Game
        StartCoroutine(LiveBackgroundRoutine());
        StartNewLevel();
    }

    // ================= LIVE BACKGROUND =================
    IEnumerator LiveBackgroundRoutine()
    {
        liveBgLayer.Clear();
        bgOrbs.Clear();
        bgSpeeds.Clear();

        // Create Floating Neon Orbs
        for (int i = 0; i < 15; i++)
        {
            VisualElement orb = new VisualElement();
            orb.style.position = Position.Absolute;
            float size = Random.Range(150f, 350f);
            orb.style.width = size;
            orb.style.height = size;
            orb.style.borderTopLeftRadius = size / 2;
            orb.style.borderTopRightRadius = size / 2;
            orb.style.borderBottomLeftRadius = size / 2;
            orb.style.borderBottomRightRadius = size / 2;

            // Neon Colors (Blue/Purple mix with low opacity)
            Color c = Random.value > 0.5f ? new Color(0.22f, 0.74f, 0.97f, 0.08f) : new Color(0.65f, 0.51f, 1f, 0.08f);
            orb.style.backgroundColor = c;

            orb.style.left = Random.Range(-200f, 1920f);
            orb.style.top = Random.Range(-200f, 1080f);

            liveBgLayer.Add(orb);
            bgOrbs.Add(orb);
            // Random speed and direction
            bgSpeeds.Add(new Vector2(Random.Range(-15f, 15f), Random.Range(-25f, -5f)));
        }

        while (true)
        {
            for (int i = 0; i < bgOrbs.Count; i++)
            {
                var orb = bgOrbs[i];
                var speed = bgSpeeds[i];
                float curX = orb.resolvedStyle.left + speed.x * Time.deltaTime;
                float curY = orb.resolvedStyle.top + speed.y * Time.deltaTime;

                // Wrap around screen
                if (curY < -400) { curY = 1100; curX = Random.Range(-200f, 1920f); }
                if (curX < -400) curX = 1920 + 100;
                if (curX > 1920 + 400) curX = -100;

                orb.style.left = curX;
                orb.style.top = curY;
            }
            yield return null;
        }
    }

    // ================= GAME LOGIC =================
    void StartNewLevel()
    {
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);

        victoryModal.style.display = DisplayStyle.None;
        btnNextLevel.style.display = DisplayStyle.None;
        btnAddTube.style.display = DisplayStyle.Flex;

        moves = 0;
        timerSeconds = 0;
        lblMoves.text = "0";
        lblTime.text = "00:00";
        lblLevel.text = currentLevel.ToString();

        int numColors = 4;
        if (difficulty == "Medium") numColors = 5;
        else if (difficulty == "Hard") numColors = 6;

        int emptyTubes = 2;
        int totalTubes = numColors + emptyTubes;

        tubes.Clear();
        history.Clear();
        effectLayer.Clear();
        animLayer.Clear();

        for (int i = 0; i < totalTubes; i++) tubes.Add(new List<int>());

        List<int> pool = new List<int>();
        for (int c = 0; c < numColors; c++)
            for (int i = 0; i < TUBE_CAPACITY; i++) pool.Add(c);

        // Shuffle
        for (int i = 0; i < pool.Count; i++)
        {
            int temp = pool[i];
            int rand = Random.Range(i, pool.Count);
            pool[i] = pool[rand];
            pool[rand] = temp;
        }

        int counter = 0;
        for (int i = 0; i < numColors; i++)
            for (int j = 0; j < TUBE_CAPACITY; j++)
                tubes[i].Add(pool[counter++]);

        selectedTubeIndex = -1;
        DrawBoard();

        isTimerRunning = true;
        timerCoroutine = StartCoroutine(TimerRoutine());
    }

    IEnumerator TimerRoutine()
    {
        while (isTimerRunning)
        {
            yield return new WaitForSeconds(1f);
            timerSeconds++;
            lblTime.text = $"{timerSeconds / 60:D2}:{timerSeconds % 60:D2}";
        }
    }

    void DrawBoard()
    {
        gameBoard.Clear();
        btnUndo.SetEnabled(history.Count > 0);

        for (int i = 0; i < tubes.Count; i++)
        {
            VisualElement tubeContainer = new VisualElement();
            tubeContainer.AddToClassList("tube-container");
            int index = i;
            tubeContainer.RegisterCallback<ClickEvent>(evt => OnTubeClick(index));

            if (selectedTubeIndex == i) tubeContainer.AddToClassList("selected");
            if (tubes[i].Count == TUBE_CAPACITY && tubes[i].All(x => x == tubes[i][0]))
                tubeContainer.AddToClassList("completed");

            VisualElement rim = new VisualElement(); rim.AddToClassList("tube-rim"); tubeContainer.Add(rim);
            VisualElement body = new VisualElement(); body.AddToClassList("tube-body");

            for (int j = 0; j < tubes[i].Count; j++)
            {
                VisualElement ball = new VisualElement();
                ball.AddToClassList("ball");
                ball.AddToClassList($"ball-{colorNames[tubes[i][j]]}");
                VisualElement highlight = new VisualElement(); highlight.AddToClassList("ball-highlight"); ball.Add(highlight);

                // 🔥 Only the top ball lifts up when selected
                if (selectedTubeIndex == i && j == tubes[i].Count - 1)
                {
                    ball.AddToClassList("selected-top");
                }

                body.Add(ball);
            }
            tubeContainer.Add(body);
            gameBoard.Add(tubeContainer);
        }
    }

    void OnTubeClick(int clickedIndex)
    {
        if (isAnimating) return;

        if (selectedTubeIndex == -1)
        {
            if (tubes[clickedIndex].Count > 0)
            {
                selectedTubeIndex = clickedIndex;
                PlaySound(popSound);
                DrawBoard();
            }
        }
        else if (selectedTubeIndex == clickedIndex)
        {
            selectedTubeIndex = -1;
            DrawBoard();
        }
        else
        {
            int flowCount = GetFlowTransferCount(selectedTubeIndex, clickedIndex);
            if (flowCount > 0)
            {
                StartCoroutine(AnimateFlowRoutine(selectedTubeIndex, clickedIndex, flowCount));
            }
            else
            {
                if (tubes[clickedIndex].Count > 0) selectedTubeIndex = clickedIndex;
                else selectedTubeIndex = -1;
                DrawBoard();
            }
        }
    }

    int GetFlowTransferCount(int fromIdx, int toIdx)
    {
        var fromTube = tubes[fromIdx];
        var toTube = tubes[toIdx];
        if (fromTube.Count == 0 || toTube.Count >= TUBE_CAPACITY) return 0;

        int topColor = fromTube.Last();
        if (toTube.Count > 0 && toTube.Last() != topColor) return 0;

        int consecutive = 0;
        for (int i = fromTube.Count - 1; i >= 0; i--)
        {
            if (fromTube[i] == topColor) consecutive++;
            else break;
        }

        int spaceAvailable = TUBE_CAPACITY - toTube.Count;
        return Mathf.Min(consecutive, spaceAvailable);
    }

    // 🔥 Smooth Multi-Ball Flow Sequence
    IEnumerator AnimateFlowRoutine(int fromIdx, int toIdx, int count)
    {
        isAnimating = true;
        history.Add(tubes.Select(t => new List<int>(t)).ToList());

        var tubeEls = gameBoard.Children().ToList();
        Rect fromRect = tubeEls[fromIdx].worldBound;
        Rect toRect = tubeEls[toIdx].worldBound;

        List<int> ballsToMove = new List<int>();
        for (int i = 0; i < count; i++)
        {
            ballsToMove.Add(tubes[fromIdx].Last());
            tubes[fromIdx].RemoveAt(tubes[fromIdx].Count - 1);
        }

        selectedTubeIndex = -1;
        DrawBoard();

        foreach (var color in ballsToMove)
        {
            VisualElement flying = new VisualElement();
            flying.AddToClassList("flying-ball");
            flying.AddToClassList($"ball-{colorNames[color]}");
            VisualElement highlight = new VisualElement(); highlight.AddToClassList("ball-highlight"); flying.Add(highlight);

            float startX = fromRect.center.x - 29;
            float startY = fromRect.y - 65; // Start from top of source tube

            float targetX = toRect.center.x - 29;
            int currentDestCount = tubes[toIdx].Count;
            float targetY = toRect.yMax - 12 - ((currentDestCount + 1) * 61);

            flying.style.left = startX;
            flying.style.top = startY;
            animLayer.Add(flying);

            // 1. Lift up
            float elapsed = 0f;
            float liftDuration = 0.05f;
            while (elapsed < liftDuration)
            {
                elapsed += Time.deltaTime;
                flying.style.top = Mathf.Lerp(startY, startY - 20, elapsed / liftDuration);
                yield return null;
            }

            // 2. Move Horizontally (Fast)
            elapsed = 0f;
            float moveDuration = 0.15f;
            float currentStartX = flying.resolvedStyle.left;
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                flying.style.left = Mathf.Lerp(currentStartX, targetX, elapsed / moveDuration);
                yield return null;
            }

            // 3. Drop Down
            elapsed = 0f;
            float dropDuration = 0.1f;
            float currentDropY = flying.resolvedStyle.top;
            while (elapsed < dropDuration)
            {
                elapsed += Time.deltaTime;
                flying.style.top = Mathf.Lerp(currentDropY, targetY, elapsed / dropDuration);
                yield return null;
            }

            // 4. Small Bounce
            elapsed = 0f;
            float bounceDuration = 0.08f;
            float bounceHeight = 10f;
            while (elapsed < bounceDuration)
            {
                elapsed += Time.deltaTime;
                flying.style.top = targetY - Mathf.Sin((elapsed / bounceDuration) * Mathf.PI) * bounceHeight;
                yield return null;
            }
            flying.style.top = targetY;

            flying.RemoveFromHierarchy();
            tubes[toIdx].Add(color);
            DrawBoard();

            // Small delay between consecutive balls
            yield return new WaitForSeconds(0.04f);
        }

        moves++;
        lblMoves.text = moves.ToString();
        isAnimating = false;
        DrawBoard();

        // Tube Complete Check
        if (tubes[toIdx].Count == TUBE_CAPACITY && tubes[toIdx].All(x => x == tubes[toIdx][0]))
        {
            PlaySound(winSound);
            tubeEls[toIdx].AddToClassList("completed");
            StartCoroutine(SpawnParticles(toRect.center, colorNames[tubes[toIdx][0]]));
        }

        // Win Check
        if (tubes.All(t => t.Count == 0 || (t.Count == TUBE_CAPACITY && t.All(x => x == t[0]))))
        {
            isTimerRunning = false;
            ShowVictory();
        }
    }

    // 🎇 Particle Effects (Shockwave + Confetti)
    IEnumerator SpawnParticles(Vector2 center, string colorName)
    {
        Color color = Color.white;
        switch (colorName)
        {
            case "red": color = new Color(0.93f, 0.32f, 0.33f); break;
            case "blue": color = new Color(0.04f, 0.74f, 0.89f); break;
            case "green": color = new Color(0.06f, 0.67f, 0.52f); break;
            case "yellow": color = new Color(0.99f, 0.79f, 0.43f); break;
            case "purple": color = new Color(0.61f, 0.53f, 1f); break;
            case "orange": color = new Color(1f, 0.47f, 0.47f); break;
            case "pink": color = new Color(0.95f, 0.41f, 0.88f); break;
            case "cyan": color = new Color(0.04f, 0.52f, 0.89f); break;
        }

        // Shockwave Ring
        VisualElement ring = new VisualElement();
        ring.style.position = Position.Absolute;
        ring.style.width = 20; ring.style.height = 20;
        ring.style.borderTopLeftRadius = 10; ring.style.borderTopRightRadius = 10;
        ring.style.borderBottomLeftRadius = 10; ring.style.borderBottomRightRadius = 10;
        ring.style.borderTopWidth = 4; ring.style.borderBottomWidth = 4; ring.style.borderLeftWidth = 4; ring.style.borderRightWidth = 4;
        ring.style.borderTopColor = color; ring.style.borderBottomColor = color; ring.style.borderLeftColor = color; ring.style.borderRightColor = color;
        ring.style.left = center.x - 10; ring.style.top = center.y - 10;
        effectLayer.Add(ring);

        // Confetti Particles
        int pCount = 35;
        List<VisualElement> particles = new List<VisualElement>();
        List<Vector2> velocities = new List<Vector2>();

        for (int i = 0; i < pCount; i++)
        {
            VisualElement p = new VisualElement();
            p.style.position = Position.Absolute;
            float size = Random.Range(6f, 12f);
            p.style.width = size; p.style.height = size;
            p.style.borderTopLeftRadius = size / 2; p.style.borderTopRightRadius = size / 2;
            p.style.borderBottomLeftRadius = size / 2; p.style.borderBottomRightRadius = size / 2;
            p.style.backgroundColor = (i % 3 == 0) ? Color.white : color;
            p.style.left = center.x; p.style.top = center.y;
            effectLayer.Add(p); particles.Add(p);
            velocities.Add(new Vector2(Random.Range(-200f, 200f), Random.Range(-300f, -100f)));
        }

        float elapsed = 0f, duration = 1f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime; float t = elapsed / duration;
            ring.style.scale = new StyleScale(new Scale(new Vector3(Mathf.Lerp(1f, 8f, t), Mathf.Lerp(1f, 8f, t), 1f)));
            ring.style.opacity = Mathf.Lerp(1f, 0f, t);

            for (int i = 0; i < pCount; i++)
            {
                velocities[i] += new Vector2(0, 600f * Time.deltaTime); // Gravity
                particles[i].style.left = particles[i].resolvedStyle.left + velocities[i].x * Time.deltaTime;
                particles[i].style.top = particles[i].resolvedStyle.top + velocities[i].y * Time.deltaTime;
                particles[i].style.opacity = Mathf.Lerp(1f, 0f, t);
            }
            yield return null;
        }
        ring.RemoveFromHierarchy();
        foreach (var p in particles) p.RemoveFromHierarchy();
    }

    void ShowVictory()
    {
        finalMoves.text = moves.ToString();
        finalTime.text = lblTime.text;
        victoryModal.style.display = DisplayStyle.Flex;
    }

    // ================= BUTTON ACTIONS =================
    void NextLevelFromModal() { victoryModal.style.display = DisplayStyle.None; currentLevel++; StartNewLevel(); }
    void NextLevel() { currentLevel++; StartNewLevel(); }

    void UndoMove()
    {
        if (history.Count == 0 || isAnimating) return;
        tubes = history.Last().Select(t => new List<int>(t)).ToList();
        history.RemoveAt(history.Count - 1);
        selectedTubeIndex = -1;
        DrawBoard();
    }

    void RestartLevel()
    {
        if (isAnimating) return;
        StartNewLevel();
    }

    void AddExtraTube()
    {
        if (isAnimating) return;
        tubes.Add(new List<int>());
        btnAddTube.style.display = DisplayStyle.None;
        DrawBoard();
    }

    void PlaySound(AudioClip clip)
    {
        if (sfxSource != null && clip != null) sfxSource.PlayOneShot(clip);
    }
}