/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODTabBar - visionOS-style tab bar dressed over an existing row of buttons.
 ## Every motion is spring-driven (velocity carries over, slight overshoot) so
 ## it feels like a native Apple control: a selection pill that glides and
 ## stretches with its speed, hover lift, press squash + rebound, and a
 ## fade / scale-in each time the bar appears. Display only: the caller wires
 ## the clicks and tells the bar which tab is active.
 ##
 */

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ODTabBar : MonoBehaviour
{
    public struct Tab
    {
        public string                  label;
        public ODNavigationIcon.Symbol icon;
        public Tab(string label, ODNavigationIcon.Symbol icon) { this.label = label; this.icon = icon; }
    }

    // Spring tuning: (stiffness, damping). Damping ratio ~0.7 → one soft overshoot.
    const float SlideK = 300f, SlideC = 26f;     // selection pill
    const float ScaleK = 420f, ScaleC = 22f;     // item hover / press
    const float GlowK  = 220f, GlowC  = 30f;     // text / icon colour
    const float IntroK = 260f, IntroC = 24f;     // bar appearance

    const float HoverScale = 1.06f, PressScale = .92f, HoverLift = 3f;
    const float StretchPerSpeed = .035f, MaxStretch = 46f;

    static readonly Color ActiveText   = Color.white;
    static readonly Color InactiveText = new Color(1f, 1f, 1f, .66f);
    static readonly Color HoverText    = new Color(1f, 1f, 1f, .92f);

    Button[]          _buttons;
    ODTabBarItem[]    _items;
    Graphic[]         _labels;
    ODNavigationIcon[] _icons;
    float[]           _iconBaseY;
    ODSpring[]          _scale, _lift, _tint;

    ODHomepageSurface _selection;
    CanvasGroup       _group;
    ODSpring          _pillX, _pillW, _intro;
    int               _active = -1;
    bool              _placed;

    /// <summary>Restyles <paramref name="bar"/> (a row of buttons laid out by a HorizontalLayoutGroup).</summary>
    public static ODTabBar Attach(RectTransform bar, Button[] buttons, Tab[] tabs)
    {
        var tabBar = bar.GetComponent<ODTabBar>();
        if (tabBar == null) tabBar = bar.gameObject.AddComponent<ODTabBar>();
        tabBar.Build(bar, buttons, tabs);
        return tabBar;
    }

    void Build(RectTransform bar, Button[] buttons, Tab[] tabs)
    {
        _buttons = buttons;
        float height = bar.rect.height;

        _group = bar.GetComponent<CanvasGroup>();
        if (_group == null) _group = bar.gameObject.AddComponent<CanvasGroup>();

        // The old flat background becomes invisible; a frosted capsule replaces it.
        var oldBackground = bar.GetComponent<Graphic>();
        if (oldBackground != null) oldBackground.color = Color.clear;

        var glassRect = ODRuntimeUI.Stretch(bar, "Glass");
        glassRect.SetSiblingIndex(0);
        glassRect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        var glass = glassRect.gameObject.AddComponent<ODHomepageSurface>();
        glass.raycastTarget = false;
        ODChampionshipTheme.ApplyBar(glass);
        glass.cornerRadius = height * .5f;
        glass.shadowSize   = 22f;

        // Selection pill: lighter glass that glides (and stretches) under the active tab.
        var selectionRect = ODRuntimeUI.Rect(bar, "Selection", 0f, 0f, 10f, 10f);
        selectionRect.SetSiblingIndex(1);
        selectionRect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        _selection = selectionRect.gameObject.AddComponent<ODHomepageSurface>();
        _selection.raycastTarget = false;
        _selection.glass       = true;
        // Design ornament: the selected control sits on a white 22 % capsule.
        _selection.topLeft     = new Color(1f, 1f, 1f, .22f);
        _selection.bottomRight = new Color(1f, 1f, 1f, .22f);
        _selection.sheen       = 0f;
        _selection.rimWidth    = 0f;
        _selection.shadowSize  = 0f;
        _selection.gameObject.SetActive(false);

        int n = buttons.Length;
        _items = new ODTabBarItem[n];
        _labels = new Graphic[n];
        _icons = new ODNavigationIcon[n];
        _iconBaseY = new float[n];
        _scale = new ODSpring[n]; _lift = new ODSpring[n]; _tint = new ODSpring[n];

        for (int i = 0; i < n; i++)
        {
            var button = buttons[i];
            if (button == null) continue;

            // All feedback is animated here: no instant ColorBlock jumps, and no fill
            // behind the item — the graphic stays fully transparent, only for the ray.
            button.transition = Selectable.Transition.None;
            if (button.targetGraphic != null) button.targetGraphic.color = new Color(1f, 1f, 1f, 0f);
            var outline = button.GetComponent<Outline>();
            if (outline != null) outline.enabled = false;

            var label = button.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
            var icon  = button.GetComponentInChildren<ODNavigationIcon>(true);
            if (i < tabs.Length)
            {
                if (label != null)
                {
                    label.text = tabs[i].label;
                    label.fontSize = 17f;
                    ODChampionshipTheme.SetFont(label, true);
                }
                if (icon != null)
                {
                    icon.symbol = tabs[i].icon;
                    icon.SetVerticesDirty();
                }
            }
            _labels[i] = label;
            _icons[i] = icon;
            if (icon != null) _iconBaseY[i] = icon.rectTransform.anchoredPosition.y;

            _items[i] = button.GetComponent<ODTabBarItem>();
            if (_items[i] == null) _items[i] = button.gameObject.AddComponent<ODTabBarItem>();
            _scale[i].Snap(1f);
        }
    }

    /// <summary>Glides the selection pill to <paramref name="index"/> (-1 hides it).</summary>
    public void SetActive(int index)
    {
        if (index == _active) return;
        _active = index;
        bool valid = IsValid(index);
        if (_selection != null) _selection.gameObject.SetActive(valid);
        if (!valid) _placed = false;
    }

    void OnEnable()
    {
        // Fresh appearance (the wrist bar is re-activated each time it is shown):
        // fade + springy scale-in, pill placed without sliding from a stale spot.
        _intro.Snap(0f);
        _placed = false;
        if (_scale != null)
            for (int i = 0; i < _scale.Length; i++) { _scale[i].Snap(1f); _lift[i].Snap(0f); }
        if (_group != null) _group.alpha = 0f;
        transform.localScale = Vector3.one * .92f;
    }

    void Update()
    {
        if (_buttons == null) return;
        float dt = Mathf.Min(ODTime.Delta, .05f);

        // Appearance.
        _intro.Step(1f, IntroK, IntroC, dt);
        _group.alpha = Mathf.Clamp01(_intro.value * 1.4f);
        float s = Mathf.LerpUnclamped(.92f, 1f, _intro.value);
        transform.localScale = new Vector3(s, s, 1f);

        UpdateSelection(dt);

        for (int i = 0; i < _buttons.Length; i++)
        {
            var item = _items[i];
            if (item == null) continue;
            bool active = i == _active;
            bool hover = item.Hovered;

            _scale[i].Step(item.Pressed ? PressScale : hover ? HoverScale : 1f, ScaleK, ScaleC, dt);
            _lift[i].Step(hover && !item.Pressed ? HoverLift : 0f, ScaleK, ScaleC, dt);
            _tint[i].Step(active ? 1f : hover ? .75f : 0f, GlowK, GlowC, dt);

            float sc = _scale[i].value;
            item.transform.localScale = new Vector3(sc, sc, 1f);

            float t = Mathf.Clamp01(_tint[i].value);
            Color tint = t > .75f ? Color.Lerp(HoverText, ActiveText, (t - .75f) / .25f)
                                  : Color.Lerp(InactiveText, HoverText, t / .75f);
            if (_labels[i] != null) _labels[i].color = tint;
            if (_icons[i] != null)
            {
                _icons[i].color = tint;
                var p = _icons[i].rectTransform.anchoredPosition;
                _icons[i].rectTransform.anchoredPosition = new Vector2(p.x, _iconBaseY[i] + _lift[i].value);
            }
        }
    }

    /// <summary>
    /// Springs the pill's left edge and width toward the active tab; while it
    /// moves it stretches with its speed and flattens a little, then settles.
    /// </summary>
    void UpdateSelection(float dt)
    {
        if (_selection == null || !IsValid(_active)) return;

        var target = (RectTransform)_buttons[_active].transform;
        Vector2 topLeft = TopLeftOf(target);
        Vector2 size = target.rect.size;

        if (!_placed)
        {
            _pillX.Snap(topLeft.x);
            _pillW.Snap(size.x);
            _placed = true;
        }
        _pillX.Step(topLeft.x, SlideK, SlideC, dt);
        _pillW.Step(size.x, SlideK, SlideC, dt);

        float speed = Mathf.Abs(_pillX.velocity);
        float stretch = Mathf.Min(speed * StretchPerSpeed, MaxStretch);
        float squash = Mathf.Min(speed * .004f, 6f);

        var pill = _selection.rectTransform;
        // Stretch trails behind the motion: grow toward where the pill comes from.
        float x = _pillX.velocity > 0f ? _pillX.value - stretch : _pillX.value;
        pill.anchoredPosition = new Vector2(x, topLeft.y - squash * .5f);
        pill.sizeDelta = new Vector2(_pillW.value + stretch, size.y - squash);
        _selection.cornerRadius = pill.sizeDelta.y * .5f;
    }

    bool IsValid(int index) => _buttons != null && index >= 0 && index < _buttons.Length && _buttons[index] != null;

    /// <summary>Position of the item's top-left corner in the bar's top-left anchored space.</summary>
    Vector2 TopLeftOf(RectTransform item)
    {
        var bar = (RectTransform)transform;
        var corners = new Vector3[4];
        item.GetWorldCorners(corners);                  // 1 = top-left
        Vector3 local = bar.InverseTransformPoint(corners[1]);
        Rect r = bar.rect;
        // Undo the item's own hover scale so the pill tracks its resting footprint.
        float sc = Mathf.Max(.01f, item.localScale.x);
        Vector2 centre = (Vector2)bar.InverseTransformPoint(item.position);
        Vector2 offset = ((Vector2)local - centre) / sc;
        return new Vector2(centre.x + offset.x - r.xMin, centre.y + offset.y - r.yMax);
    }
}

/// <summary>Pointer state of one tab (ray hover / select from the Interaction SDK, mouse in the Editor).</summary>
public class ODTabBarItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public bool Hovered { get; private set; }
    public bool Pressed { get; private set; }

    public void OnPointerEnter(PointerEventData eventData) => Hovered = true;
    public void OnPointerExit(PointerEventData eventData) { Hovered = false; Pressed = false; }
    public void OnPointerDown(PointerEventData eventData) => Pressed = true;
    public void OnPointerUp(PointerEventData eventData) => Pressed = false;

    void OnDisable() { Hovered = false; Pressed = false; }
}
