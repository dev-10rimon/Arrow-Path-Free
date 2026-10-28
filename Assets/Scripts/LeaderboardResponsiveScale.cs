using UnityEngine;

namespace MultiTechStudio.EscapeGame
{
    public class LeaderboardResponsiveScale : MonoBehaviour
    {
        [Header("Scale Settings")]
        [SerializeField] private RectTransform parentPanel; // Je parent empty object-er scale komate hobe
        [SerializeField] private float phoneScale = 1.0f;    // Phone-er jonno scale
        [SerializeField] private float tabletScale = 0.8f;   // Tablet-er jonno scale (prakoyojon moto barate/komate paren)

        void Start()
        {
            if (parentPanel == null)
            {
                parentPanel = GetComponent<RectTransform>();
            }

            AdjustScaleForDevice();
        }

        void AdjustScaleForDevice()
        {
            // Screen Aspect Ratio ba Width/Height check kore tablet naki phone ta ber kora
            float screenAspect = (float)Screen.width / Screen.height;

            // Sadharonoto Phone-er aspect ratio 16:9 (0.56) ba tar theke beshi লম্বা (tall) hoy, 
            // ebong Tablet ba iPad-er aspect ratio 4:3 (0.75) ba tar k kachaakachi (chouda) hoy.
            // Ekhane 1.3 tar theke choto ba 4:3 ratio (approx 0.77 ba tar niche) hole tablet hishebe dhora jay.

            if (screenAspect > 1.4f) // Landscape mode ba tablet/PC hole
            {
                // Jodi tablet landscape ba onno screen hoy
                parentPanel.localScale = new Vector3(tabletScale, tabletScale, 1f);
            }
            else if (screenAspect < 0.7f) // Portrait Phone
            {
                // Standard Phone screen
                parentPanel.localScale = new Vector3(phoneScale, phoneScale, 1f);
            }
            else
            {
                // Tablet ba iPad Portrait mode-er jonno (Aspect ratio 4:3 ba 16:10 er moto hole)
                // Ekhane width ektu beshi thake bole scale ektu komiye dewa valo
                parentPanel.localScale = new Vector3(tabletScale, tabletScale, 1f);
            }
        }
    }
}