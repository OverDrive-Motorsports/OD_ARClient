using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One row of the live race standings widget:
/// rank · team dot · driver code · gap. Slides smoothly when overtaken.
/// </summary>
public class RaceStandingCard : MonoBehaviour
{
    [Header("References (wired by builder)")]
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI codeText;
    public TextMeshProUGUI gapText;
    public Image           teamDot;

    private RectTransform _rt;
    private Coroutine     _move;

    private void Awake() { _rt = GetComponent<RectTransform>(); }

    public void Set(int rank, string code, Color teamColor, string gap)
    {
        if (rankText != null) rankText.text = rank.ToString();
        if (codeText != null) codeText.text = code;
        if (gapText  != null) gapText.text  = gap;
        if (teamDot  != null) teamDot.color = teamColor;
    }

    public void SetYInstant(float y)
    {
        if (_rt == null) _rt = GetComponent<RectTransform>();
        var p = _rt.anchoredPosition; p.y = y;
        _rt.anchoredPosition = p;
    }

    public void MoveToY(float y, float duration)
    {
        if (_move != null) StopCoroutine(_move);
        _move = StartCoroutine(MoveRoutine(y, duration));
    }

    private IEnumerator MoveRoutine(float targetY, float duration)
    {
        if (_rt == null) _rt = GetComponent<RectTransform>();
        Vector2 start = _rt.anchoredPosition;
        Vector2 end   = new Vector2(start.x, targetY);

        for (float e = 0f; e < duration; e += Time.deltaTime)
        {
            float t = e / duration;
            t = t * t * (3f - 2f * t); // smoothstep
            _rt.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }
        _rt.anchoredPosition = end;
        _move = null;
    }
}
