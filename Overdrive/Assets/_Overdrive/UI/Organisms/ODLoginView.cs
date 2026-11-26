using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Minimal glass sign-in window, with a ray/poke keyboard for hands and controllers.</summary>
public sealed class ODLoginView : MonoBehaviour
{
    public TMP_InputField email, password;
    public Button submit;
    public TextMeshProUGUI message;
    public event Action Submitted;
    RectTransform _keyboard;
    TMP_InputField _target;
    bool _shift, _symbols;
    bool _built;

    void OnEnable() => ODPassthroughBlur.Acquire(this);
    void OnDisable() => ODPassthroughBlur.Release(this);

    public void Build()
    {
        if (_built) return;
        _built = true;
        if (email != null && password != null && submit != null && message != null)
        {
            _keyboard = transform.Find("VRKeyboard") as RectTransform;
            Wire();
            return;
        }
        var surface = GetComponent<ODHomepageSurface>();
        if (surface == null) surface = gameObject.AddComponent<ODHomepageSurface>();
        ODChampionshipTheme.ApplyBackground(surface, ODChampionshipTheme.NeutralPalette);
        surface.raycastTarget = true;
        ODRuntimeUI.Text(transform, "Brand", "OVERDRIVE", 48, 34, 600, 40, 24, true);
        ODRuntimeUI.Fill(transform, "Accent", 48, 92, 40, 3, ODDesign.Gold, 1.5f);
        ODRuntimeUI.Text(transform, "Heading", "Sign in", 48, 116, 664, 60, 40, true);
        ODRuntimeUI.Text(transform, "Subtitle", "Your account. Your race weekend.", 48, 184, 664, 36, 20, color: ODDesign.Secondary);
        ODRuntimeUI.Text(transform, "EmailLabel", "EMAIL", 48, 240, 664, 24, 14, true, color: ODDesign.Secondary);
        email = Field("Email", "you@example.com", 270, false);
        ODRuntimeUI.Text(transform, "PasswordLabel", "PASSWORD", 48, 348, 664, 24, 14, true, color: ODDesign.Secondary);
        password = Field("Password", "Your password", 378, true);
        Button("RevealPassword", "Show", 622, 390, 78, 38);
        submit = Button("SignIn", "Sign in", 48, 478, 664, 64, primary: true);
        message = ODRuntimeUI.Text(transform, "Status", "Sign in to access Overdrive.", 48, 552, 664, 48, 17, color: ODDesign.Secondary);
        message.textWrappingMode = TextWrappingModes.Normal;
        message.overflowMode = TextOverflowModes.Overflow;
        _keyboard = ODRuntimeUI.Rect(transform, "VRKeyboard", 40, 622, 680, 316);
        Wire();
    }

    void Wire()
    {
        email.onSelect.AddListener(_ => { _target = email; SetKeyboard(true); });
        password.onSelect.AddListener(_ => { _target = password; SetKeyboard(true); });
        email.onSubmit.AddListener(_ => password.ActivateInputField());
        var reveal = transform.Find("RevealPassword").GetComponent<Button>();
        reveal.onClick.AddListener(() =>
        {
            bool hidden = password.contentType == TMP_InputField.ContentType.Password;
            password.contentType = hidden ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
            password.ForceLabelUpdate();
            reveal.GetComponentInChildren<TextMeshProUGUI>().text = hidden ? "Hide" : "Show";
        });
        submit.onClick.AddListener(() => Submitted?.Invoke());
        password.onSubmit.AddListener(_ => Submitted?.Invoke());
        RebuildKeyboard();
        _keyboard.gameObject.SetActive(false);
        SetKeyboard(false);
    }

    TMP_InputField Field(string name, string placeholder, float y, bool secret)
    {
        var plate = ODRuntimeUI.Fill(transform, name, 48, y, 664, 64, ODDesign.Recessed, 22);
        plate.raycastTarget = true;
        plate.gameObject.SetActive(false);
        var area = ODRuntimeUI.Rect(plate.transform, "TextArea", 22, 0, secret ? 536 : 620, 64);
        area.gameObject.AddComponent<RectMask2D>();
        var hint = ODRuntimeUI.Text(area, "Placeholder", placeholder, 0, 0, area.rect.width, 64, 22, color: ODDesign.Tertiary);
        var text = ODRuntimeUI.Text(area, "Value", "", 0, 0, area.rect.width, 64, 22);
        text.overflowMode = TextOverflowModes.Overflow;
        var input = plate.gameObject.AddComponent<TMP_InputField>();
        input.targetGraphic = plate; input.textViewport = area; input.textComponent = text; input.placeholder = hint;
        input.fontAsset = text.font; input.pointSize = 22; input.lineType = TMP_InputField.LineType.SingleLine;
        input.contentType = secret ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
        input.characterLimit = secret ? 256 : 254;
        input.shouldHideSoftKeyboard = true; input.shouldHideMobileInput = true;
        input.customCaretColor = true; input.caretColor = Color.white;
        input.selectionColor = new Color(.5f, .7f, 1, .3f);
        plate.gameObject.SetActive(true);
        return input;
    }

    Button Button(string name, string label, float x, float y, float w, float h, bool primary = false, Transform parent = null)
    {
        var image = ODRuntimeUI.Fill(parent == null ? transform : parent, name, x, y, w, h, primary ? Color.white : ODDesign.ButtonFill, h * .5f);
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image; button.navigation = new Navigation { mode = Navigation.Mode.None };
        var colours = button.colors;
        colours.highlightedColor = new Color(1.18f, 1.18f, 1.18f); colours.pressedColor = new Color(.8f, .8f, .8f);
        button.colors = colours;
        ODRuntimeUI.Text(image.transform, "Label", label, 0, 0, w, h, primary ? 23 : 18, true, true, primary ? ODDesign.Ink : Color.white);
        return button;
    }

    void RebuildKeyboard()
    {
        for (int i = _keyboard.childCount - 1; i >= 0; --i)
        {
            var old = _keyboard.GetChild(i).gameObject;
            old.SetActive(false);
            if (Application.isPlaying) Destroy(old); else DestroyImmediate(old);
        }
        string[] rows = _symbols
            ? new[] { "!@#$%^&*()", "-_=+[]{}\\|", ";:'\",.<>/?", "`~01234567" }
            : new[] { "1234567890", "qwertyuiop", "asdfghjkl", "zxcvbnm.@" };
        for (int row = 0; row < rows.Length; row++)
        {
            string chars = _shift ? rows[row].ToUpperInvariant() : rows[row];
            for (int i = 0; i < chars.Length; i++)
            {
                string value = chars[i].ToString();
                var key = Button("Key" + row + "_" + i, value, (680 - chars.Length * 66 + 6) * .5f + i * 66, row * 55, 60, 48, parent: _keyboard);
                key.onClick.AddListener(() => Insert(value));
            }
        }
        Button("Shift", _shift ? "abc" : "ABC", 13, 228, 105, 52, parent: _keyboard).onClick.AddListener(() => { _shift = !_shift; RebuildKeyboard(); });
        Button("Symbols", _symbols ? "Letters" : "#+=", 126, 228, 105, 52, parent: _keyboard).onClick.AddListener(() => { _symbols = !_symbols; RebuildKeyboard(); });
        Button("Space", "Space", 239, 228, 175, 52, parent: _keyboard).onClick.AddListener(() => Insert(" "));
        Button("Backspace", "Delete", 422, 228, 125, 52, parent: _keyboard).onClick.AddListener(Backspace);
        Button("Done", "Done", 555, 228, 112, 52, parent: _keyboard).onClick.AddListener(() => SetKeyboard(false));
    }

    public void Insert(string value)
    {
        if (_target == null || !_target.interactable) return;
        int start = Mathf.Clamp(Mathf.Min(_target.selectionAnchorPosition, _target.selectionFocusPosition), 0, _target.text.Length);
        int end = Mathf.Clamp(Mathf.Max(_target.selectionAnchorPosition, _target.selectionFocusPosition), start, _target.text.Length);
        string next = _target.text.Remove(start, end - start).Insert(start, value);
        if (next.Length > _target.characterLimit) return;
        _target.text = next; _target.caretPosition = start + value.Length;
        _target.selectionAnchorPosition = _target.selectionFocusPosition = _target.caretPosition;
    }
    void Backspace()
    {
        if (_target == null || !_target.interactable) return;
        int start = Mathf.Clamp(Mathf.Min(_target.selectionAnchorPosition, _target.selectionFocusPosition), 0, _target.text.Length);
        int end = Mathf.Clamp(Mathf.Max(_target.selectionAnchorPosition, _target.selectionFocusPosition), start, _target.text.Length);
        if (start == end && start > 0) --start;
        _target.text = _target.text.Remove(start, end - start);
        _target.caretPosition = start; _target.selectionAnchorPosition = _target.selectionFocusPosition = start;
    }

    public void SetKeyboard(bool visible)
    {
        if (_keyboard != null) _keyboard.gameObject.SetActive(visible);
        var rect = (RectTransform)transform;
        rect.sizeDelta = new Vector2(760, visible ? 940 : 624);
        var clip = GetComponent<Oculus.Interaction.Surfaces.BoundsClipper>();
        if (clip != null) { clip.Position = new Vector3(rect.rect.center.x, rect.rect.center.y, 0); clip.Size = new Vector3(rect.rect.width, rect.rect.height, .01f); }
    }
    public void SetBusy(bool busy, string status)
    {
        email.interactable = password.interactable = submit.interactable = !busy;
        message.text = status; message.color = ODDesign.Secondary;
        if (busy) SetKeyboard(false);
    }
    public void ClearPassword()
    {
        password.text = ""; password.contentType = TMP_InputField.ContentType.Password; password.ForceLabelUpdate();
        var reveal = transform.Find("RevealPassword")?.GetComponentInChildren<TextMeshProUGUI>();
        if (reveal != null) reveal.text = "Show";
        _target = null; SetKeyboard(false);
    }
}
