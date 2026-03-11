using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DriverImageLoader : MonoBehaviour
{
    public Image headshotImage;

    public void SetHeadshot(string url)
    {
        StartCoroutine(LoadImageCoroutine(url));
    }

    IEnumerator LoadImageCoroutine(string url)
    {
        using (var www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Error loading image: " + www.error);
                yield break;
            }

            Texture2D tex = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
            Sprite sprite = Sprite.Create(tex,
                                          new Rect(0, 0, tex.width, tex.height),
                                          new Vector2(0.5f, 0.5f));
            headshotImage.sprite = sprite;
        }
    }
}
