using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// A race card in the grid. Handles hover highlight and click.
/// </summary>
[RequireComponent(typeof(Button))]
public class ContentGridItem : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    public Image thumbnail;
    public TextMeshProUGUI titleLabel;

    [Header("Hover")]
    public Color normalColor = new Color(0.22f, 0.19f, 0.15f, 1f);
    public Color hoverColor = new Color(0.32f, 0.28f, 0.22f, 1f);

    private Button _btn;
    private Image _bg;
    private ContentItemData _data;

    private void Awake()
    {
        _btn = GetComponent<Button>();
        _bg = GetComponent<Image>();
        _btn.onClick.AddListener(OnClick);
    }

    public void Setup(ContentItemData data)
    {
        _data = data;
        if (thumbnail != null && data.thumbnail != null)
            thumbnail.sprite = data.thumbnail;
        if (titleLabel != null)
            titleLabel.text = data.title;
    }

    private void OnClick()
    {
        string title = titleLabel != null ? titleLabel.text
                     : (_data != null ? _data.title : gameObject.name);
        Debug.Log($"[Overdrive] Opening: {title}");

        if (VideoPlayerController.Instance != null)
            VideoPlayerController.Instance.Open(title);
        else
            Debug.LogWarning("[Overdrive] No VideoPlayerController in scene.");
    }

    public void OnPointerEnter(PointerEventData _)
    {
        if (_bg) _bg.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData _)
    {
        if (_bg) _bg.color = normalColor;
    }
}
