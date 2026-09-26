using System;
using System.Collections.Generic;
using Assets.Scripts.Objects;
using Assets.Scripts.UI;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace InventoryPeek;

/// <summary>
/// Inventory windows tagged hidable disappear while the cursor is locked and come back the moment it is free
/// (holding the mouse-control key, Alt by default, or any screen that frees the cursor). The game's own window
/// state is never touched: a hidden window is still open, docked or undocked where it was, and saves as it was.
/// </summary>
[BepInPlugin(pluginGuid, pluginName, pluginVersion)]
public class InventoryPeekPlugin : BaseUnityPlugin
{
    public const string pluginGuid = "net.xceled.stationeers.inventorypeek";
    public const string pluginName = "InventoryPeek";
    public const string pluginVersion = "0.1.0";

    private const char TagSeparator = ',';

    internal static InventoryPeekPlugin Instance { get; private set; }

    private ConfigEntry<bool> _enabled;
    private ConfigEntry<float> _hiddenOpacity;
    private ConfigEntry<string> _hidable;
    private ConfigEntry<KeyCode> _toggleKey;

    private readonly HashSet<string> _tags = new HashSet<string>(StringComparer.Ordinal);

    internal bool Enabled => _enabled.Value;

    internal float HiddenOpacity => Mathf.Clamp01(_hiddenOpacity.Value);

    private void Awake()
    {
        Instance = this;
        _enabled = Config.Bind("General", "Enabled", true,
            "Hide tagged inventory windows while the cursor is locked. Off shows every window as the game does.");
        _hiddenOpacity = Config.Bind("General", "HiddenOpacity", 0f,
            new ConfigDescription("Opacity of a tagged window while hidden: 0 is invisible, 0.2 a faint outline.",
                new AcceptableValueRange<float>(0f, 1f)));
        _hidable = Config.Bind("General", "HidableWindows", string.Empty,
            "Comma-separated prefab names of the things whose windows hide, e.g. ItemHardSuit,ItemHardBackpack. " +
            "The eye button on a window's title bar adds or removes its thing here.");
        _toggleKey = Config.Bind("General", "ToggleKey", KeyCode.None,
            "Optional key that tags or untags the window under the cursor, as an alternative to the eye button. " +
            "Only acts while the cursor is free.");
        LoadTags();
        _hidable.SettingChanged += (_, _) => LoadTags();

        new Harmony(pluginGuid).PatchAll(typeof(InventoryWindowPatch));
        Logger.LogInfo($"{pluginName} {pluginVersion} loaded; {_tags.Count} hidable window kind(s).");
    }

    private void Update()
    {
        if (_toggleKey.Value == KeyCode.None || !Input.GetKeyDown(_toggleKey.Value) || !PeekState.CursorFree)
        {
            return;
        }

        InventoryWindow window = InventoryWindow.CurrentWindow;
        if (window != null)
        {
            Toggle(PeekState.KeyOf(window));
        }
    }

    internal bool IsHidable(string key) => key != null && _tags.Contains(key);

    internal void Toggle(string key)
    {
        if (key == null)
        {
            return;
        }

        if (!_tags.Remove(key))
        {
            _tags.Add(key);
        }

        List<string> sorted = new List<string>(_tags);
        sorted.Sort(StringComparer.Ordinal);
        _hidable.Value = string.Join(TagSeparator.ToString(), sorted);
    }

    private void LoadTags()
    {
        _tags.Clear();
        foreach (string part in _hidable.Value.Split(TagSeparator))
        {
            string key = part.Trim();
            if (key.Length > 0)
            {
                _tags.Add(key);
            }
        }
    }
}

/// <summary>The one hook: every inventory window gets a peek component when the game assigns it a slot.</summary>
[HarmonyPatch(typeof(InventoryWindow), nameof(InventoryWindow.Assign))]
internal static class InventoryWindowPatch
{
    [HarmonyPostfix]
    private static void Postfix(InventoryWindow __instance)
    {
        if (__instance != null && __instance.GetComponent<PeekWindow>() == null)
        {
            __instance.gameObject.AddComponent<PeekWindow>();
        }
    }
}

/// <summary>Read-only views of game state the mod decides on.</summary>
internal static class PeekState
{
    /// <summary>The cursor is free: the mouse-control key is held or a screen unlocked it (InputMouse.SetMouseControl).</summary>
    internal static bool CursorFree => InputMouse.IsMouseControl;

    /// <summary>A window is tagged by the kind of thing it shows, so a new suit of the same type stays hidable.</summary>
    internal static string KeyOf(InventoryWindow window)
    {
        if (window == null || window.ParentSlot == null)
        {
            return null;
        }

        DynamicThing occupant = window.ParentSlot.Get<DynamicThing>();
        return occupant != null ? occupant.PrefabName : null;
    }
}
