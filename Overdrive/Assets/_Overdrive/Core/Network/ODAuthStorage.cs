using System;
using UnityEngine;

/// <summary>Quest: AES/GCM protected by AndroidKeyStore. Editor/other platforms: memory only, never PlayerPrefs.</summary>
static class ODAuthStorage
{
#if UNITY_ANDROID && !UNITY_EDITOR
    const string Alias = "overdrive.user.session.v1";
    static AndroidJavaObject Preferences()
    {
        using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
        return activity.Call<AndroidJavaObject>("getSharedPreferences", "overdrive_secure_session", 0);
    }
    static AndroidJavaObject Key()
    {
        using var keyStoreClass = new AndroidJavaClass("java.security.KeyStore");
        using var store = keyStoreClass.CallStatic<AndroidJavaObject>("getInstance", "AndroidKeyStore");
        store.Call("load", new object[] { null });
        if (!store.Call<bool>("containsAlias", Alias))
        {
            using var generators = new AndroidJavaClass("javax.crypto.KeyGenerator");
            using var generator = generators.CallStatic<AndroidJavaObject>("getInstance", "AES", "AndroidKeyStore");
            using var builder = new AndroidJavaObject("android.security.keystore.KeyGenParameterSpec$Builder", Alias, 3);
            builder.Call<AndroidJavaObject>("setBlockModes", (object)new[] { "GCM" }).Dispose();
            builder.Call<AndroidJavaObject>("setEncryptionPaddings", (object)new[] { "NoPadding" }).Dispose();
            using var spec = builder.Call<AndroidJavaObject>("build");
            generator.Call("init", spec);
            generator.Call<AndroidJavaObject>("generateKey").Dispose();
        }
        return store.Call<AndroidJavaObject>("getKey", Alias, (AndroidJavaObject)null);
    }
    public static string Read()
    {
        try
        {
            using var prefs = Preferences();
            string value = prefs.Call<string>("getString", "session", "");
            if (string.IsNullOrEmpty(value)) return null;
            var parts = value.Split('.');
            using var ciphers = new AndroidJavaClass("javax.crypto.Cipher");
            using var cipher = ciphers.CallStatic<AndroidJavaObject>("getInstance", "AES/GCM/NoPadding");
            using var key = Key();
            using var spec = new AndroidJavaObject("javax.crypto.spec.GCMParameterSpec", 128, Convert.FromBase64String(parts[0]));
            cipher.Call("init", 2, key, spec);
            return System.Text.Encoding.UTF8.GetString(cipher.Call<byte[]>("doFinal", Convert.FromBase64String(parts[1])));
        }
        catch { Clear(); return null; }
    }
    public static void Write(string json)
    {
        using var ciphers = new AndroidJavaClass("javax.crypto.Cipher");
        using var cipher = ciphers.CallStatic<AndroidJavaObject>("getInstance", "AES/GCM/NoPadding");
        using var key = Key();
        cipher.Call("init", 1, key);
        string iv = Convert.ToBase64String(cipher.Call<byte[]>("getIV"));
        string encrypted = Convert.ToBase64String(cipher.Call<byte[]>("doFinal", System.Text.Encoding.UTF8.GetBytes(json)));
        using var prefs = Preferences();
        using var editor = prefs.Call<AndroidJavaObject>("edit");
        editor.Call<AndroidJavaObject>("putString", "session", iv + "." + encrypted).Dispose();
        if (!editor.Call<bool>("commit")) throw new InvalidOperationException("Secure session could not be saved.");
    }
    public static void Clear()
    {
        try
        {
            using var prefs = Preferences();
            using var editor = prefs.Call<AndroidJavaObject>("edit");
            editor.Call<AndroidJavaObject>("remove", "session").Dispose();
            editor.Call<bool>("commit");
        }
        catch { /* No unencrypted fallback. */ }
    }
#else
    public static string Read() => null;
    public static void Write(string json) { }
    public static void Clear() { }
#endif
}
