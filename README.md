# Inventory Peek

A Stationeers mod: inventory windows you tag as hidable disappear while you play and come back the moment
you hold Alt (the game's Mouse Control key). Keep every window open without it covering the screen.

## Use

- Click the **eye** on an inventory window's title bar. Bright eye: the window is hidable. Faint eye: always shown.
- Play normally: hidable windows are hidden and let clicks through. Hold Alt: every window shows at once, where
  you left it, ready to use.
- A drag that starts while you hold Alt keeps the window up until you let go of the mouse button.
- Double press Alt quickly to latch hidden windows on while you play; double press again to hide them.

## How it decides

- **Tags are per window.** A window is tagged by the reference id of the one thing it shows, which the save keeps,
  so the choice survives sessions and each window is chosen on its own: tagging one backpack's window does not
  hide another backpack's. The list is the `HidableWindows` setting and can be edited by hand.
- **Peeking** is holding the game's Mouse Control key (`KeyMap.MouseControl`, Alt unless rebound), read the way
  the game reads it (`MouseModeController.AltKeyDown`), so a rebind is followed. Screens that free the cursor on
  their own (menus, the scoreboard) do not show hidden windows: a stuck scoreboard once kept them all visible.

## Settings (BepInEx config `net.xceled.stationeers.inventorypeek.cfg`)

| Setting | Default | Meaning |
| --- | --- | --- |
| `Enabled` | true | Off shows every window as the game does. |
| `HiddenOpacity` | 0 | Opacity of a hidden window; 0.2 leaves a faint outline. |
| `HidableWindows` | empty | Comma-separated reference ids of the things whose windows hide. The eye button edits it. |
| `DoublePressSeconds` | 0.3 | Two presses of the Mouse Control key within this time latch hidden windows on or off; 0 turns the latch off. |
| `ToggleKey` | None | Optional key that tags or untags the window under the cursor while you hold Alt. |

## How little it changes

- One Harmony postfix, on `InventoryWindow.Assign`, adds a small component to each inventory window.
- Hiding is done with a `CanvasGroup` of the mod's own on the window: opacity and click-through only. The game's
  canvas, layout, open/closed state, docking and saved window state are never touched, so turning the mod off
  (or removing it) leaves every window exactly as the game has it.
- A docked window keeps its place in the dock while hidden, so the dock shows a gap there.
- Client-side UI only: nothing is sent over the network; safe in multiplayer.

## Build

Needs Stationeers with BepInEx 5.4 and [StationeersLaunchPad](https://github.com/StationeersLaunchPad/StationeersLaunchPad).
`.\build.ps1` builds and stages `package\`; `.\build.ps1 -Deploy` also copies it into the local mods folder
(close the game first).
