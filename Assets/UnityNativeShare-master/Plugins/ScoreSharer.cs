using UnityEngine;
using System.IO;
using System.Collections;

public class ScoreSharer : MonoBehaviour
{
    // Eita UI Button-er OnClick()-e call korben
    public void ShareScore(int score)
    {
        StartCoroutine(TakeScreenshotAndShare(score));
    }

    private IEnumerator TakeScreenshotAndShare(int score)
    {
        yield return new WaitForEndOfFrame();

        // 1. Game Screen-er Screenshot newa
        Texture2D ss = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        ss.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        ss.Apply();

        // 2. Temporary Path-e image save kora
        string filePath = Path.Combine(Application.temporaryCachePath, "shared_score.png");
        File.WriteAllBytes(filePath, ss.EncodeToPNG());

        // Memory clear kora
        Destroy(ss);

        // 3. Share Message ebong Dialog open kora
        string shareText = $"Ami game-e {score} score korechi! Tumi ki eita bhangte parbe? 🎮";

        new NativeShare()
            .AddFile(filePath)
            .SetSubject("High Score Challenge!")
            .SetText(shareText)
            .SetUrl("https://play.google.com/store/apps/details?id=" + Application.identifier) // Google Play Store link
            .SetCallback((result, shareTarget) => Debug.Log("Share result: " + result + ", Selected app: " + shareTarget))
            .Share();
    }
}