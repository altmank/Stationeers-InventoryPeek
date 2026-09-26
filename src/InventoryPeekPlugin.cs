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
/// Inventory windows tagged hidable disappear while you play and come back while you hold the Mouse Control key
/// (Alt unless rebound), or stay back while a quick double press has latched them on. The game's own window
/// state is never touched: a hidden window is still open, docked or undocked where it was, and saves as it was.
/// </summary>
[BepInPlugin(pluginGuid, pluginName, pluginVersion)]
public class InventoryPeekPlugin : BaseUnityPlugin
{
    public const string pluginGuid = "net.xceled.stationeers.inventorypeek";
    public const string pluginName = "InventoryPeek";
    public const string pluginVersion = "1.1.0";

    private const char TagSeparator = ',';

    internal static InventoryPeekPlugin Instance { get; private set; }

    private ConfigEntry<bool> _enabled;
    private ConfigEntry<float> _hiddenOpacity;
    private ConfigEntry<string> _hidable;
    private ConfigEntry<KeyCode> _toggleKey;
    private ConfigEntry<float> _doublePress;

    private float _lastPress = float.NegativeInfinity;

    private readonly HashSet<string> _tags = new HashSet<string>(StringComparer.Ordinal);

    internal bool Enabled => _enabled.Value;

    internal float HiddenOpacity => Mathf.Clamp01(_hiddenOpacity.Value);

    /// <summary>Hidden windows are held shown until the next double press.</summary>
    internal bool Latched { get; private set; }

    private void Awake()
    {
        Instance = this;
        _enabled = Config.Bind("General", "Enabled", true,
            "Hide tagged inventory windows until you hold the Mouse Control key. Off shows every window as the game does.");
        _hiddenOpacity = Config.Bind("General", "HiddenOpacity", 0f,
            new ConfigDescription("Opacity of a tagged window while hidden: 0 is invisible, 0.2 a faint outline.",
                new AcceptableValueRange<float>(0f, 1f)));
        _hidable = Config.Bind("General", "HidableWindows", string.Empty,
            "Comma-separated reference ids of the things whose windows hide, one per window. " +
            "The eye button on a window's title bar adds or removes its thing here.");
        _toggleKey = Config.Bind("General", "ToggleKey", KeyCode.None,
            "Optional key that tags or untags the window under the cursor, as an alternative to the eye button. " +
            "Only acts while you hold the Mouse Control key.");
        _doublePress = Config.Bind("General", "DoublePressSeconds", 0.3f,
            new ConfigDescription("Two presses of the Mouse Control key within this many seconds latch hidden windows " +
                "shown until the next double press. 0 turns the latch off.", new AcceptableValueRange<float>(0f, 1f)));
        LoadTags();
        _hidable.SettingChanged += (_, _) => LoadTags();

        new Harmony(pluginGuid).PatchAll(typeof(InventoryWindowPatch));
        Logger.LogInfo($"{pluginName} {pluginVersion} loaded; {_tags.Count} hidable window(s).");
    }

    private void Update()
    {
        WatchDoublePress();
        if (_toggleKey.Value == KeyCode.None || !Input.GetKeyDown(_toggleKey.Value) || !PeekState.PeekKeyHeld)
        {
            return;
        }

        InventoryWindow window = InventoryWindow.CurrentWindow;
        if (window != null)
        {
            Toggle(PeekState.KeyOf(window));
        }
    }

    // A double press of the Mouse Control key flips the latch; the second press does not count toward another.
    private void WatchDoublePress()
    {
        if (!KeyManager.GetButtonDown(KeyMap.MouseControl))
        {
            return;
        }

        float now = Time.unscaledTime;
        if (_doublePress.Value > 0f && now - _lastPress <= _doublePress.Value)
        {
            Latched = !Latched;
            _lastPress = float.NegativeInfinity;
            return;
        }

        _lastPress = now;
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
    /// <summary>
    /// The mouse-control key (Alt unless rebound) is held: MouseModeController.AltKeyDown, the game's own read of
    /// KeyMap.MouseControl. Not the free-cursor state, which any open screen can hold (a stuck scoreboard kept every
    /// window shown), and hidden windows should only show when the player asks.
    /// </summary>
    internal static bool PeekKeyHeld => Assets.Scripts.MouseModeController.AltKeyDown;

    /// <summary>
    /// A window is tagged by the one thing it shows, its ReferenceId, which the save keeps: each window is chosen on
    /// its own, and a second backpack of the same type is not hidden because the first one is.
    /// </summary>
    internal static string KeyOf(InventoryWindow window)
    {
        if (window == null || window.ParentSlot == null)
        {
            return null;
        }

        DynamicThing occupant = window.ParentSlot.Get<DynamicThing>();
        return occupant != null ? occupant.ReferenceId.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
    }
}
