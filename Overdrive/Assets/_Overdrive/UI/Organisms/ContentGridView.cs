using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ContentGridView : MonoBehaviour
{
    [Header("References")]
    public Transform gridContainer;
    public GameObject gridItemPrefab;

    public void Populate(ContentItemData[] items)
    {
        foreach (Transform child in gridContainer)
            Destroy(child.gameObject);

        foreach (var item in items)
        {
            GameObject go = Instantiate(gridItemPrefab, gridContainer);
            go.GetComponent<ContentGridItem>()?.Setup(item);
        }
    }
}

[System.Serializable]
public class ContentItemData
{
    public string title;
    public Sprite thumbnail;
}
