using UnityEngine;
using TMPro;

[RequireComponent(typeof(Rigidbody2D))]
public class PenFlickController : MonoBehaviour
{
    private Rigidbody2D rb;
    private Camera mainCam;

    [Header("Flick Settings")]
    public float hitForce = 8f;    // কলমের জন্য ৮-১০ ভালো
    public float maxDragDistance = 4f;
    public float torqueMultiplier = 0.2f; // ঘূর্ণন কমিয়ে দিলাম

    [Header("UI & Visuals")]
    public TextMeshPro powerText;
    public LineRenderer aimLine;
    public GameObject hitDot;
    public GameObject aimCircle;

    [Header("In-Scene Power Meter")]
    public GameObject powerMeterContainer;
    public Transform powerMeterFill;
    public SpriteRenderer powerMeterFillSprite;

    private bool isAiming = false;
    private Vector2 hitPoint;
    private Vector2 forceDirection;
    private float currentPower = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (powerText != null) powerText.gameObject.SetActive(false);
        if (aimLine != null) aimLine.enabled = false;
        if (hitDot != null) hitDot.SetActive(false);
        if (aimCircle != null) aimCircle.SetActive(false);
        if (powerMeterContainer != null) powerMeterContainer.SetActive(false);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                isAiming = true;
                hitPoint = mousePos;

                if (powerText != null) powerText.gameObject.SetActive(true);
                if (aimLine != null) aimLine.enabled = true;

                if (hitDot != null)
                {
                    hitDot.SetActive(true);
                    hitDot.transform.position = hitPoint;
                }
                if (aimCircle != null)
                {
                    aimCircle.SetActive(true);
                    aimCircle.transform.position = hitPoint;
                }
                if (powerMeterContainer != null) powerMeterContainer.SetActive(true);
            }
        }

        if (Input.GetMouseButton(0) && isAiming)
        {
            Vector2 currentMousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);

            forceDirection = (hitPoint - currentMousePos).normalized;

            float dragDistance = Vector2.Distance(hitPoint, currentMousePos);
            currentPower = Mathf.Clamp(dragDistance, 0f, maxDragDistance) / maxDragDistance;

            if (powerText != null)
            {
                powerText.text = (currentPower * 100f).ToString("F0") + "%";
                powerText.transform.position = currentMousePos + new Vector2(0.5f, 0.5f);
            }

            if (powerMeterFill != null && powerMeterFillSprite != null)
            {
                Vector3 newScale = powerMeterFill.localScale;
                newScale.y = currentPower;
                powerMeterFill.localScale = newScale;

                if (currentPower <= 0.5f)
                    powerMeterFillSprite.color = Color.Lerp(Color.green, Color.yellow, currentPower / 0.5f);
                else
                    powerMeterFillSprite.color = Color.Lerp(Color.yellow, Color.red, (currentPower - 0.5f) / 0.5f);
            }

            if (aimLine != null)
            {
                aimLine.positionCount = 2;
                aimLine.SetPosition(0, hitPoint);
                aimLine.SetPosition(1, currentMousePos);
            }
        }

        if (Input.GetMouseButtonUp(0) && isAiming)
        {
            isAiming = false;

            if (powerText != null) powerText.gameObject.SetActive(false);
            if (aimLine != null) aimLine.enabled = false;
            if (hitDot != null) hitDot.SetActive(false);
            if (aimCircle != null) aimCircle.SetActive(false);
            if (powerMeterContainer != null) powerMeterContainer.SetActive(false);

            if (currentPower > 0.1f)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;

                // হালকা ধাক্কা
                Vector2 force = forceDirection * (hitForce * currentPower);
                rb.AddForce(force, ForceMode2D.Impulse);

                // ঘূর্ণন কমানো
                Vector2 offset = hitPoint - rb.worldCenterOfMass;
                float crossProduct = (offset.x * forceDirection.y) - (offset.y * forceDirection.x);
                float torqueDirection = Mathf.Sign(crossProduct);
                float torqueAmount = force.magnitude * offset.magnitude * torqueMultiplier;

                rb.AddTorque(torqueAmount * torqueDirection, ForceMode2D.Impulse);
            }
        }
    }
}