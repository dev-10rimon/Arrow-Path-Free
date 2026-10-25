using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform _contentContainer;

    private List<LeaderboardItem> _leaderboardItems = new List<LeaderboardItem>();

    private void OnEnable()
    {
        // লিডারবোর্ড পপআপ ওপেন হলেই রিলোড ও সর্ট করবে
        InitializeLeaderboard();
        SortLeaderboard();
    }

    private void InitializeLeaderboard()
    {
        _leaderboardItems.Clear();
        if (_contentContainer == null) return;

        foreach (Transform child in _contentContainer)
        {
            LeaderboardItem item = child.GetComponent<LeaderboardItem>();
            if (item != null)
            {
                if (item.IsLocalPlayer)
                {
                    item.RefreshLocalPlayerData();
                }
                _leaderboardItems.Add(item);
            }
        }
    }

    public void SortLeaderboard()
    {
        if (_leaderboardItems == null || _leaderboardItems.Count == 0) return;

        // ১. স্কোরের ভিত্তিতে বড় থেকে ছোট (Descending) সাজানো হচ্ছে
        _leaderboardItems = _leaderboardItems.OrderByDescending(item => item.Score).ToList();

        // ২. UI-তে তাদের সঠিক সিরিয়াল এবং র‍্যাংক বসানো হচ্ছে
        for (int i = 0; i < _leaderboardItems.Count; i++)
        {
            // কন্টেইনারের ভেতরে তাদের পজিশন বা সিরিয়াল আপডেট করা
            _leaderboardItems[i].transform.SetSiblingIndex(i);

            // র‍্যাংক টেক্সট আপডেট (১ থেকে শুরু হবে: 1, 2, 3...)
            _leaderboardItems[i].UpdateRankUI(i + 1);
        }
    }
}