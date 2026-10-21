/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODFontAssetsSetup - Creates the DM Sans TextMesh Pro font assets used by the
 ## visionOS-style pages (UI/Resources/Fonts) from the TTFs in Art/Fonts/DMSans
 ## (the design system's typeface), whenever they are missing. Runs once after each script
 ## reload; does nothing when the assets already exist.
 ##
 */

using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

[InitializeOnLoad]
public static class ODFontAssetsSetup
{
    const string SourceDir = "Assets/_Overdrive/Art/Fonts/DMSans/";
    const string TargetDir = "Assets/_Overdrive/UI/Resources/Fonts/";
    static readonly string[] Weights = { "DMSans-Regular", "DMSans-SemiBold", "DMSans-Bold" };

    // Pre-rendered glyphs (French UI + symbols used by the pages); others are added on demand (dynamic atlas).
    const string Characters =
        " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
        "ÀÂÄÆÇÈÉÊËÎÏÔŒÙÛÜŸàâäæçèéêëîïôœùûüÿ°·—–−’‘“”«»…›‹→←€ñÑáíóúÁÍÓÚãõÃÕ";

    static ODFontAssetsSetup() => EditorApplication.delayCall += EnsureFontAssets;

    public static void EnsureFontAssets()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        bool created = false;
        foreach (string weight in Weights)
        {
            string path = TargetDir + weight + " SDF.asset";
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path) != null) continue;

            var font = AssetDatabase.LoadAssetAtPath<Font>(SourceDir + weight + ".ttf");
            if (font == null)
            {
                Debug.LogWarning($"[ODFontAssetsSetup] {SourceDir}{weight}.ttf not found.");
                continue;
            }

            System.IO.Directory.CreateDirectory(TargetDir);
            var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            asset.name = weight + " SDF";
            AssetDatabase.CreateAsset(asset, path);

            // The atlas and material must live inside the asset, or they're lost on reload.
            asset.atlasTextures[0].name = weight + " Atlas";
            asset.material.name = weight + " Material";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            AssetDatabase.AddObjectToAsset(asset.material, asset);

            asset.TryAddCharacters(Characters, out string missing);
            if (!string.IsNullOrEmpty(missing)) Debug.Log($"[ODFontAssetsSetup] {weight}: no glyph for \"{missing}\".");
            EditorUtility.SetDirty(asset);
            created = true;
            Debug.Log($"[ODFontAssetsSetup] Created {path}");
        }

        if (created) AssetDatabase.SaveAssets();
    }
}
