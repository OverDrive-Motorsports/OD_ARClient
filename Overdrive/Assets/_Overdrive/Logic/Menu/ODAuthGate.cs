using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>Fail-closed entry point. Protected windows never activate before a backend-validated session.</summary>
[DefaultExecutionOrder(-10000)]
public sealed class ODAuthGate : MonoBehaviour
{
    public ODLoginView login;
    public GameObject[] protectedWindows;
    public AppLauncher launcher;
    public LeftWristNavigation wrist;
    public OverdriveMainMenu menu;
    bool _busy, _open;

    void Awake()
    {
        if (login != null) login.Build();
        Lock();
        if (login == null) { enabled = false; Debug.LogError("[Auth] Login window is not connected. Application remains locked."); return; }
        login.Submitted += Submit;
        ODAuthService.Changed += SessionChanged;
        _busy = true;
        login.SetBusy(true, "Preparing sign-in…");
    }

    IEnumerator Start()
    {
        // Tracking may settle after the first frame; use the same front-facing placement as the app.
        float elapsed = 0;
        while (elapsed < 5)
        {
            bool tracked;
            bool ready = Application.isEditor || (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head)
                .TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out tracked) && tracked);
            if (Camera.main != null && ready) break;
            elapsed += Time.unscaledDeltaTime; yield return null;
        }
        yield return new WaitForSecondsRealtime(.4f);
        PlaceLogin();
        _ = Restore();
    }

    async Task Restore()
    {
        _busy = true; login.SetBusy(true, "Checking your session…");
        await ODAuthService.RestoreAsync();
        if (this == null || login == null) return;
        _busy = false;
        if (!ODAuthService.IsAuthenticated) login.SetBusy(false, "Sign in to access Overdrive.");
    }

    async void Submit()
    {
        if (_busy) return;
        _busy = true;
        string email = login.email.text, password = login.password.text;
        login.SetBusy(true, "Signing in…");
        try { await ODAuthService.LoginAsync(email, password); }
        catch (System.Exception error)
        {
            if (this == null || login == null) return;
            login.SetBusy(false, error.Message); login.message.color = ODDesign.Destructive;
        }
        finally
        {
            password = null; _busy = false;
            if (login != null) { login.ClearPassword(); if (!ODAuthService.IsAuthenticated) login.email.interactable = login.password.interactable = login.submit.interactable = true; }
        }
    }

    void SessionChanged()
    {
        if (ODAuthService.IsAuthenticated) Unlock();
        else Lock();
    }
    void Lock()
    {
        bool wasOpen = _open; _open = false;
        if (wrist != null) wrist.enabled = false;
        if (protectedWindows != null) foreach (var window in protectedWindows) if (window != null) window.SetActive(false);
        if (login != null)
        {
            login.gameObject.SetActive(true);
            if (!_busy) { login.ClearPassword(); login.SetBusy(false, wasOpen ? "Your session ended. Please sign in again." : "Sign in to access Overdrive."); }
            PlaceLogin();
        }
    }
    void Unlock()
    {
        if (_open) return;
        _open = true;
        login.ClearPassword(); login.gameObject.SetActive(false);
        if (launcher != null && launcher.menuCanvas != null) launcher.menuCanvas.gameObject.SetActive(true);
        var group = launcher != null && launcher.menuCanvas != null ? launcher.menuCanvas.GetComponent<CanvasGroup>() : null;
        if (group != null) group.alpha = 1;
        if (menu != null) menu.ShowHomepage();
        if (launcher != null) launcher.SnapMenuInFront();
        if (wrist != null) wrist.enabled = true;
    }
    void PlaceLogin()
    {
        var head = launcher != null ? launcher.centerEyeAnchor : null;
        if (head == null && Camera.main != null) head = Camera.main.transform;
        if (head == null || login == null) return;
        Vector3 forward = head.forward; forward.y = 0;
        if (forward.sqrMagnitude < .001f) forward = Vector3.forward;
        forward.Normalize();
        // Top-centred pivot keeps the form still when the VR keyboard unfolds.
        login.transform.SetPositionAndRotation(head.position + forward * 1.15f + Vector3.up * .27f, Quaternion.LookRotation(forward, Vector3.up));
    }
    void OnDestroy()
    {
        ODAuthService.Changed -= SessionChanged;
        if (login != null) login.Submitted -= Submit;
    }
}
