using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class ApiUser
{
    public int id;
    public string name;
    public string username;
}

[System.Serializable]
public class ApiUserListWrapper
{
    public List<ApiUser> users;
}

public class ApiToLeaderboardBridge : MonoBehaviour
{
    private string apiUrl = "https://jsonplaceholder.typicode.com/users";

    [Header("References")]
    [SerializeField] private LeaderboardManager leaderboardManager;
    [SerializeField] private Transform contentContainer;

    void Start()
    {
        StartCoroutine(FetchUserDataAndApply());
    }

    IEnumerator FetchUserDataAndApply()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("API থেকে ডেটা আনতে সমস্যা হয়েছে: " + request.error);
            }
            else
            {
                string jsonResponse = request.downloadHandler.text;
                string wrappedJson = "{\"users\":" + jsonResponse + "}";
                ApiUserListWrapper wrapper = JsonUtility.FromJson<ApiUserListWrapper>(wrappedJson);

                // কন্টেইনারের ভেতর থাকা সব লিডারবোর্ড আইটেমগুলো খুঁজে বের করা
                LeaderboardItem[] items = contentContainer.GetComponentsInChildren<LeaderboardItem>();

                for (int i = 0; i < items.Length; i++)
                {
                    // লোকাল প্লেয়ার বাদে বাকি ফেক প্লেয়ারগুলোর নাম ও স্কোর API এর ডেটা দিয়ে বদলে দেওয়া
                    if (!items[i].IsLocalPlayer && i < wrapper.users.Count)
                    {
                        string apiName = wrapper.users[i].name;
                        int randomScore = Random.Range(500, 5000);

                        // এখন নাম এবং স্কোর উভয়ই আপডেট হবে
                        items[i].UpdateName(apiName);
                        items[i].UpdateScore(randomScore);
                    }
                }

                // লিডারবোর্ডটি সর্ট করে সাজিয়ে নেওয়া
                if (leaderboardManager != null)
                {
                    leaderboardManager.SortLeaderboard();
                }

                Debug.Log("API থেকে নাম ও স্কোর সফলভাবে লিডারবোর্ডে বসেছে!");
            }
        }
    }
}