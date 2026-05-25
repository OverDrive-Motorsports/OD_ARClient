using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ApplyRoundedCorners
{
    [MenuItem("Overdrive/Apply Rounded Corners")]
    public static void Apply()
    {
        string[] paths = {
            "OverdriveMenuCanvas/OverdriveMenuPanel",
            "OverdriveMenuCanvas/OverdriveMenuPanel/SearchBar"
        };
        float[] radii = { 32f, 20f };

        for (int i = 0; i < paths.Length; i++)
        {
            GameObject go = GameObject.Find(paths[i]);
            if (go == null) { Debug.LogWarning("Not found: " + paths[i]); continue; }

            Image img = go.GetComponent<Image>();
            Color col = img != null ? img.color : Color.white;
            if (img != null) Object.DestroyImmediate(img);

            RoundedImage r = go.AddComponent<RoundedImage>();
            r.color = col;
            r.cornerRadius = radii[i];
            r.cornerSegments = 12;
            r.raycastTarget = false;

            EditorUtility.SetDirty(go);
            Debug.Log("Rounded: " + go.name + " radius=" + radii[i]);
        }

        Debug.Log("Done — rounded corners applied!");
    }
}
