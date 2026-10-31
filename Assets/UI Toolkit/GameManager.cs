using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{
    public UIDocument uiDocument;

    private VisualElement root;
    private VisualElement tubesContainer;
    private VisualElement dotTrailElement;
    private Button resetButton;

    private List<List<string>> tubesData = new List<List<string>>();
    private List<VisualElement> tubeElements = new List<VisualElement>();

    private const int TUBE_CAPACITY = 4;
    private const int TOTAL_TUBES = 6;
    private const int EMPTY_TUBES = 2;
    private string[] allColors = { "green", "cyan", "yellow", "pink", "orange", "blue" };

    // ড্র্যাগ ভেরিয়েবল
    private VisualElement draggedBall = null;
    private int draggedSourceTube = -1;
    private string draggedColor = "";
    private Vector2 pointerStartPos;
    private Vector2 ballStartPos;
    private List<Vector2> trailPoints = new List<Vector2>();

    void Start()
    {
        root = uiDocument.rootVisualElement;
        tubesContainer = root.Q<VisualElement>("tubes-container");
        resetButton = root.Q<Button>("reset-button");

        // ডটেড ট্রেইল আঁকার জন্য একটি ফুল-স্ক্রিন এলিমেন্ট
        dotTrailElement = new VisualElement();
        dotTrailElement.style.position = Position.Absolute;
        dotTrailElement.style.top = 0; dotTrailElement.style.left = 0;
        dotTrailElement.style.width = Length.Percent(100); dotTrailElement.style.height = Length.Percent(100);
        dotTrailElement.pickingMode = PickingMode.Ignore;
        dotTrailElement.generateVisualContent += DrawTrail;
        root.Add(dotTrailElement);

        // উইন ওভারলে
        var winOverlay = new VisualElement();
        winOverlay.name = "win-overlay";
        winOverlay.AddToClassList("win-overlay");
        winOverlay.pickingMode = PickingMode.Ignore;
        var winText = new Label("LEVEL COMPLETE!");
        winText.AddToClassList("win-text");
        winOverlay.Add(winText);
        root.Add(winOverlay);

        if (resetButton != null) resetButton.clicked += ResetGame;
        ResetGame();
    }

    void ResetGame()
    {
        var winOverlay = root.Q<VisualElement>("win-overlay");
        winOverlay.RemoveFromClassList("win-overlay-active");

        GenerateRandomLevel();
        trailPoints.Clear();
        dotTrailElement.MarkDirtyRepaint();

        draggedBall = null;
        draggedSourceTube = -1;
        draggedColor = "";

        RebuildUI();
    }

    void GenerateRandomLevel()
    {
        tubesData.Clear();
        List<string> allBalls = new List<string>();
        int filledTubes = TOTAL_TUBES - EMPTY_TUBES;

        for (int i = 0; i < filledTubes; i++)
        {
            string color = allColors[i % allColors.Length];
            for (int j = 0; j < TUBE_CAPACITY; j++) allBalls.Add(color);
        }

        for (int i = 0; i < allBalls.Count; i++)
        {
            string temp = allBalls[i];
            int randomIndex = Random.Range(i, allBalls.Count);
            allBalls[i] = allBalls[randomIndex];
            allBalls[randomIndex] = temp;
        }

        for (int i = 0; i < TOTAL_TUBES; i++) tubesData.Add(new List<string>());

        int ballCounter = 0;
        for (int i = 0; i < filledTubes; i++)
        {
            for (int j = 0; j < TUBE_CAPACITY; j++)
            {
                tubesData[i].Add(allBalls[ballCounter]);
                ballCounter++;
            }
        }
    }

    void RebuildUI()
    {
        tubesContainer.Clear();
        tubeElements.Clear();

        for (int i = 0; i < tubesData.Count; i++)
        {
            int currentTubeIndex = i;
            VisualElement tube = new VisualElement();
            tube.AddToClassList("tube");

            for (int j = 0; j < tubesData[i].Count; j++)
            {
                string color = tubesData[i][j];
                VisualElement ball = new VisualElement();
                ball.AddToClassList("ball");
                ball.AddToClassList("color-" + color);

                // শুধুমাত্র একদম উপরের বলের উপর পয়েন্টার ডাউন ইভেন্ট
                if (j == tubesData[i].Count - 1)
                {
                    ball.RegisterCallback<PointerDownEvent>(evt => OnBallPointerDown(evt, ball, currentTubeIndex, color));
                }

                tube.Add(ball);
            }
            tubesContainer.Add(tube);
            tubeElements.Add(tube);
        }
    }

    void OnBallPointerDown(PointerDownEvent evt, VisualElement ball, int tubeIndex, string color)
    {
        if (evt.button != 0) return;

        draggedBall = ball;
        draggedSourceTube = tubeIndex;
        draggedColor = color;

        pointerStartPos = evt.position;
        // বর্তমান translate পজিশন সেভ করা (সাধারণত 0 থাকে)
        ballStartPos = Vector2.zero;

        draggedBall.AddToClassList("ball-dragging");
        draggedBall.BringToFront(); // বলটি সবার উপরে নিয়ে আসা
        trailPoints.Clear();

        // গোটা স্ক্রিনে মাউস মুভ এবং আপ ইভেন্ট রেজিস্টার করা
        root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        root.RegisterCallback<PointerUpEvent>(OnPointerUp);

        evt.StopPropagation();
    }

    void OnPointerMove(PointerMoveEvent evt)
    {
        if (draggedBall == null) return;

        // মাউস কতটুকু সরেছে তা হিসাব করে বলটিকে সরানো
        Vector2 delta = (Vector2)evt.position - pointerStartPos;
        draggedBall.style.translate = new StyleTranslate(new Translate(ballStartPos.x + delta.x, ballStartPos.y + delta.y));

        // ডটেড ট্রেইলের জন্য মাউসের বর্তমান পজিশন সেভ করা
        trailPoints.Add((Vector2)evt.position);
        if (trailPoints.Count > 50) trailPoints.RemoveAt(0); // পারফরম্যান্সের জন্য পুরনো ডট মুছে ফেলা
        dotTrailElement.MarkDirtyRepaint(); // স্ক্রিনে ডট দেখানো
    }

    void OnPointerUp(PointerUpEvent evt)
    {
        if (draggedBall == null) return;

        // ইভেন্ট আন-রেজিস্টার করা
        root.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        root.UnregisterCallback<PointerUpEvent>(OnPointerUp);

        draggedBall.RemoveFromClassList("ball-dragging");

        // ড্রপ টার্গেট খোঁজা
        Vector2 dropPos = (Vector2)evt.position;

        // 🔥 গুরুত্বপূর্ণ: যে বলটি ড্র্যাগ করছি সেটি যাতে পিকিং ব্লক না করে
        draggedBall.pickingMode = PickingMode.Ignore;
        VisualElement picked = root.panel.Pick(dropPos);
        draggedBall.pickingMode = PickingMode.Position;

        VisualElement targetTubeElement = null;
        while (picked != null)
        {
            if (picked.ClassListContains("tube"))
            {
                targetTubeElement = picked;
                break;
            }
            picked = picked.parent;
        }

        // যদি ভ্যালিড টিউব পাওয়া যায়
        if (targetTubeElement != null)
        {
            int targetIndex = tubeElements.IndexOf(targetTubeElement);
            if (targetIndex != -1 && targetIndex != draggedSourceTube && IsValidMove(draggedSourceTube, targetIndex, draggedColor))
            {
                ExecuteMove(draggedSourceTube, targetIndex);
                ClearDragState();
                return;
            }
        }

        // মুভ ফেইল করলে বা বাইরে ছেড়ে দিলে আগের অবস্থায় ফিরে যাবে
        RebuildUI();
        ClearDragState();
    }

    void ClearDragState()
    {
        draggedBall = null;
        draggedSourceTube = -1;
        draggedColor = "";
        trailPoints.Clear();
        dotTrailElement.MarkDirtyRepaint();
    }

    bool IsValidMove(int fromTube, int toTube, string color)
    {
        if (tubesData[fromTube].Count == 0) return false;
        if (tubesData[toTube].Count >= TUBE_CAPACITY) return false;
        if (tubesData[toTube].Count == 0) return true;

        string targetTopColor = tubesData[toTube][tubesData[toTube].Count - 1];
        return targetTopColor == color;
    }

    void ExecuteMove(int fromTube, int toTube)
    {
        string ballToMove = tubesData[fromTube][tubesData[fromTube].Count - 1];
        tubesData[fromTube].RemoveAt(tubesData[fromTube].Count - 1);
        tubesData[toTube].Add(ballToMove);

        RebuildUI();
        CheckTubeCompletion();
    }

    void CheckTubeCompletion()
    {
        for (int i = 0; i < tubesData.Count; i++)
        {
            if (tubesData[i].Count == TUBE_CAPACITY)
            {
                string firstColor = tubesData[i][0];
                if (tubesData[i].All(c => c == firstColor))
                {
                    var tubeElement = tubeElements[i];
                    if (!tubeElement.ClassListContains("tube-complete"))
                    {
                        tubeElement.AddToClassList("tube-complete");
                        tubeElement.schedule.Execute(() => tubeElement.RemoveFromClassList("tube-complete")).ExecuteLater(800);
                    }
                }
            }
        }
        CheckWinCondition();
    }

    void CheckWinCondition()
    {
        bool isWin = true;
        foreach (var tube in tubesData)
        {
            if (tube.Count > 0 && tube.Count < TUBE_CAPACITY) { isWin = false; break; }
            if (tube.Count == TUBE_CAPACITY)
            {
                string firstColor = tube[0];
                foreach (var color in tube) { if (color != firstColor) { isWin = false; break; } }
            }
        }

        if (isWin)
        {
            root.schedule.Execute(() => {
                var winOverlay = root.Q<VisualElement>("win-overlay");
                winOverlay.AddToClassList("win-overlay-active");
            }).ExecuteLater(1000);
        }
    }

    // 🎨 ডটেড ট্রেইল আঁকার ফাংশন (Painter2D)
    void DrawTrail(MeshGenerationContext mgc)
    {
        if (trailPoints.Count < 2) return;

        var painter = mgc.painter2D;
        painter.fillColor = Color.white;

        // মাউসের পজিশন UI Toolkit-এর স্ক্রিন পজিশন হওয়ায় এটি সরাসরি আঁকা যাবে
        foreach (var point in trailPoints)
        {
            painter.BeginPath();
            painter.Arc(point, 4f, Angle.Degrees(0), Angle.Degrees(360));
            painter.Fill();
        }
    }
}