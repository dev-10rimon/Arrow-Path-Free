using System;
using UnityEngine;
using UnityEngine.UIElements;

public class DragManipulator : PointerManipulator
{
    private VisualElement target;
    private bool isDragging = false;
    private Vector2 pointerStartPos;
    private Vector2 elementStartPos;

    public Action<Vector2> OnDragMove; // ডটেড ট্রেইলের জন্য কলব্যাক
    public Action<Vector2> OnDragEnd;  // ড্রপ করার জন্য কলব্যাক (মাউসের পজিশন সহ)

    public DragManipulator(VisualElement target)
    {
        this.target = target;
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerDownEvent>(OnPointerDown);
        target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        target.RegisterCallback<PointerUpEvent>(OnPointerUp);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (evt.button != 0) return;

        isDragging = true;
        pointerStartPos = evt.position;

        // 🔥 এরর ফিক্স: ToVector2() এর বদলে x এবং y আলাদাভাবে নেওয়া হলো
        if (target.style.translate.keyword == StyleKeyword.None)
        {
            elementStartPos = Vector2.zero;
        }
        else
        {
            Translate t = target.style.translate.value;
            elementStartPos = new Vector2(t.x.value, t.y.value);
        }

        target.AddToClassList("ball-dragging");
        target.BringToFront();
        target.CapturePointer(evt.pointerId);
        evt.StopPropagation();
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!isDragging) return;

        Vector2 delta = (Vector2)evt.position - pointerStartPos;
        target.style.translate = new StyleTranslate(new Translate(elementStartPos.x + delta.x, elementStartPos.y + delta.y));

        OnDragMove?.Invoke((Vector2)evt.position);
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (!isDragging) return;

        isDragging = false;
        target.RemoveFromClassList("ball-dragging");
        target.ReleasePointer(evt.pointerId);

        // মাউস ছাড়ার সঠিক পজিশনটি নেওয়া হচ্ছে
        Vector2 dropPos = (Vector2)evt.position;
        evt.StopPropagation();

        OnDragEnd?.Invoke(dropPos);
    }
}