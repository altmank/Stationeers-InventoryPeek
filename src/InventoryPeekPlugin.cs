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
    public const string pluginVersion = "1.3.0";

    private const char TagSeparator = ',';
    private const string WorldsSection = "HidableWindows";

    internal static InventoryPeekPlugin Instance { get; private set; }

    private ConfigEntry<bool> _enabled;
    private ConfigEntry<float> _hiddenOpacity;
    private ConfigEntry<string> _hidable;
    private ConfigEntry<KeyCode> _toggleKey;
    private ConfigEntry<float> _doublePress;
    private ConfigEntry<bool> _opaqueWindows;

    private float _lastPress = float.NegativeInfinity;

    // The current world's tags. Reference ids are only unique within one save, so every world keeps its own list,
    // keyed by the save's world id (World.CurrentId, a GUID the save stores).
    private readonly HashSet<string> _tags = new HashSet<string>(StringComparer.Ordinal);
    private readonly Dictionary<string, ConfigEntry<string>> _worlds =
        new Dictionary<string, ConfigEntry<string>>(StringComparer.Ordinal);

    private string _worldId;
    private ConfigEntry<string> _world;

    internal bool Enabled => _enabled.Value;

    internal float HiddenOpacity => Mathf.Clamp01(_hiddenOpacity.Value);

    internal bool OpaqueWindows => _opaqueWindows.Value;

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
            "Tags saved by 1.1 and earlier, for every world at once. Moved into the first world loaded, then emptied. " +
            "Each world's tags are now in the [HidableWindows] section, one line per world id.");
        _toggleKey = Config.Bind("General", "ToggleKey", KeyCode.None,
            "Optional key that tags or untags the window under the cursor, as an alternative to the eye button. " +
            "Only acts while you hold the Mouse Control key.");
        _doublePress = Config.Bind("General", "DoublePressSeconds", 0.3f,
            new ConfigDescription("Two presses of the Mouse Control key within this many seconds latch hidden windows " +
                "shown until the next double press. 0 turns the latch off.", new AcceptableValueRange<float>(0f, 1f)));
        _opaqueWindows = Config.Bind("General", "OpaqueWindows", false,
            "Give every inventory window a solid background instead of the game's see-through one, hidable or not. " +
            "Hidden windows still hide. Other UI is left as the game draws it.");

        new Harmony(pluginGuid).PatchAll(typeof(InventoryWindowPatch));
        Logger.LogInfo($"{pluginName} {pluginVersion} loaded.");
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

    internal void LogWarning(string message) => Logger.LogWarning(message);

    internal bool IsHidable(string key)
    {
        SyncWorld();
        return key != null && _tags.Contains(key);
    }

    internal void Toggle(string key)
    {
        SyncWorld();
        if (key == null || _world == null)
        {
            return;
        }

        if (!_tags.Remove(key))
        {
            _tags.Add(key);
        }

        List<string> sorted = new List<string>(_tags);
        sorted.Sort(StringComparer.Ordinal);
        _world.Value = string.Join(TagSeparator.ToString(), sorted);
    }

    // Follows the loaded world. No world id (the main menu, a client before the host's world arrives) means no tags.
    private void SyncWorld()
    {
        string id = Assets.Scripts.Objects.World.CurrentId;
        if (string.Equals(id, _worldId, StringComparison.Ordinal))
        {
            return;
        }

        _worldId = id;
        _world = null;
        _tags.Clear();
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        if (!_worlds.TryGetValue(id, out _world))
        {
            _world = Config.Bind(WorldsSection, id, string.Empty,
                "Reference ids of the things whose windows hide in this world, comma-separated. The eye button edits it.");
            _world.SettingChanged += (_, _) => ReadWorld();
            _worlds[id] = _world;
        }

        MigrateLegacy();
        ReadWorld();
    }

    // 1.1 and earlier kept one list for every world; it belongs to whichever world is loaded first.
    private void MigrateLegacy()
    {
        if (string.IsNullOrWhiteSpace(_hidable.Value))
        {
            return;
        }

        _world.Value = string.IsNullOrWhiteSpace(_world.Value)
            ? _hidable.Value
            : _world.Value + TagSeparator + _hidable.Value;
        _hidable.Value = string.Empty;
        Logger.LogInfo($"Moved the tags saved by an older version into world {_worldId}.");
    }

    private void ReadWorld()
    {
        _tags.Clear();
        if (_world == null)
        {
            return;
        }

        foreach (string part in _world.Value.Split(TagSeparator))
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
