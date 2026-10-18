using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Avatar grid er ekta cell. Prefab e lagate hobe.
/// </summary>
public class AvatarItem : MonoBehaviour
{
    [SerializeField] private Image avatarImage;      // avatar er chobi
    [SerializeField] private GameObject selectedFrame; // select korle je border/frame dekhabe
    [SerializeField] private Button button;

    private int index;
    private Action<int> onClick;

    public void Setup(int index, Sprite sprite, Action<int> onClick)
    {
        this.index = index;
        this.onClick = onClick;

        avatarImage.sprite = sprite;
        SetSelected(false);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => this.onClick?.Invoke(this.index));
    }

    public void SetSelected(bool selected)
    {
        if (selectedFrame) selectedFrame.SetActive(selected);
    }
}
