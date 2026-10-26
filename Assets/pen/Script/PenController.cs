using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PenController : MonoBehaviour
{
    private Rigidbody2D rb;
    private Camera mainCam;
    private Vector3 dragStartPos;
    private bool isDragging = false;

    [Header("Shooting Settings")]
    public float maxForce = 15f; // সর্বোচ্চ কত জোরে ছোড়া যাবে

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;
        rb.bodyType = RigidbodyType2D.Kinematic; // শুরুতে পেন নড়বে না
    }

    void Update()
    {
        // মাউস বা টাচ ইনপুট হ্যান্ডলিং
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            // চেক করা হচ্ছে আমরা পেনের উপর ট্যাপ করেছি কি না
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                isDragging = true;
                dragStartPos = mousePos;
            }
        }

        if (Input.GetMouseButton(0) && isDragging)
        {
            // এখানে আমরা চাইলে ভিজুয়ালি তীর (Arrow) দেখাতে পারি (পরের ধাপে)
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
            Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            // পেন থেকে মাউস যেদিকে টানা হয়েছে, তার উল্টো দিকে ফোর্স দিতে হবে
            Vector2 direction = (dragStartPos - mousePos).normalized;

            // দূরত্ব অনুযায়ী ফোর্স ক্যালকুলেট করা
            float distance = Vector2.Distance(dragStartPos, mousePos);
            float power = Mathf.Clamp(distance, 0f, maxForce);

            // পেনকে ছেড়ে দেওয়া (Dynamic করা) এবং ফোর্স প্রয়োগ করা
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.AddForce(direction * power, ForceMode2D.Impulse);

            // গেম ম্যানেজারকে জানানো যে টার্ন শেষ (এটি পরে করবো)
            // GameManager.Instance.EndTurn();
        }
    }
}