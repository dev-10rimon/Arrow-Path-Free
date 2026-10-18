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

        _leaderboardItems = _leaderboardItems.OrderByDescending(item => item.Score).ToList();

        for (int i = 0; i < _leaderboardItems.Count; i++)
        {
            _leaderboardItems[i].transform.SetSiblingIndex(i);
            _leaderboardItems[i].UpdateRankUI(i + 1);
        }
    }
}