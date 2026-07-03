/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ODNavBar - Floating bottom navigation bar organism managing tab selection across ODNavItem children.
 ##
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages a horizontal row of ODNavItem tabs inside ItemsContainer.
/// Tab children are expected to be pre-placed in the hierarchy (built by ODUIBuilder);
/// RebuildItems() discovers them via GetComponentsInChildren and wires click callbacks.
/// </summary>
public class ODNavBar : MonoBehaviour
{
    /// <summary>Data descriptor for a single navigation tab.</summary>
    [System.Serializable]
    public class NavItemData
    {
        public string label;
        public Sprite icon;
    }

    [Header("Items")]
    public List<NavItemData> items = new List<NavItemData>
    {
        new NavItemData { label = "Home"    },
        new NavItemData { label = "Race"    },
        new NavItemData { label = "Profile" },
    };

    [Header("State")]
    public int selectedIndex = 0;

    [Header("Events")]
    /// <summary>Fires when the user selects a tab, with the new tab's index.</summary>
    public UnityEvent<int> OnTabChanged;

    [Header("References — wired by ODUIBuilder")]
    public Transform itemsContainer;

    private readonly List<ODNavItem> _navItems = new List<ODNavItem>();

    private void Start()
    {
        RebuildItems();
        SetSelected(selectedIndex);
    }

    // ── Public API ────────────────────────────────────────────────────────────────

    /// <summary>Replaces the items list, re-applies labels/icons to existing children, and resets selection to 0.</summary>
    public void SetItems(List<NavItemData> newItems)
    {
        items = newItems;
        RebuildItems();
        SetSelected(0);
    }

    /// <summary>Highlights the tab at index with accentGold and dims all others with textSecondary.</summary>
    public void SetSelected(int index)
    {
        if (_navItems.Count == 0) return;
        selectedIndex = Mathf.Clamp(index, 0, _navItems.Count - 1);
        for (int i = 0; i < _navItems.Count; i++)
            _navItems[i].SetSelected(i == selectedIndex);
    }

    // ── Internal ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Discovers ODNavItem children and wires click listeners.
    /// Uses a captured int to avoid the C# closure-loop variable capture issue.
    /// </summary>
    private void RebuildItems()
    {
        _navItems.Clear();
        if (itemsContainer == null) return;

        ODNavItem[] children = itemsContainer.GetComponentsInChildren<ODNavItem>(true);
        for (int i = 0; i < children.Length; i++)
        {
            ODNavItem item = children[i];
            _navItems.Add(item);

            if (i < items.Count)
                item.Setup(items[i].label, items[i].icon);

            // Capture loop variable so each lambda closes over the correct index
            int captured = i;
            item.Button.onClick.RemoveAllListeners();
            item.Button.onClick.AddListener(() =>
            {
                SetSelected(captured);
                OnTabChanged?.Invoke(captured);
            });
        }
    }
}
