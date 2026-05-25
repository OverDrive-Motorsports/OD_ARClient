using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ContentGridItem : MonoBehaviour
{
    public Image thumbnail;
    public TextMeshProUGUI titleLabel;

    public void Setup(ContentItemData data)
    {
        if (thumbnail != null && data.thumbnail != null)
            thumbnail.sprite = data.thumbnail;

        if (titleLabel != null)
            titleLabel.text = data.title;
    }
}
