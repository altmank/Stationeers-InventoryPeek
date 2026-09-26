# Inventory Peek

A Stationeers mod: inventory windows you tag as hidable disappear while you play and come back the moment
the cursor is free (hold Alt, the mouse-control key). Keep every window open without it covering the screen.

## Use

- Click the **eye** on an inventory window's title bar. Bright eye: the window is hidable. Faint eye: always shown.
- Play normally: hidable windows are hidden and let clicks through. Hold Alt: every window shows at once, where
  you left it, ready to use.
- A drag that starts while the cursor is free keeps the window up until you let go of the mouse button.

## How it decides

- **Tags are per kind of item.** A window is tagged by the prefab name of the thing it shows (`ItemHardSuit`,
  `ItemHardBackpack`, `ItemMiningBelt`...), so the choice survives sessions and swapping to another item of the
  same kind. The list is the `HidableWindows` setting and can be edited by hand.
- **"Cursor free"** is the game's own mouse-control state (`InputMouse.IsMouseControl`): true while the
  mouse-control key is held and while a screen that unlocks the cursor is open.

## Settings (BepInEx config `net.xceled.stationeers.inventorypeek.cfg`)

| Setting | Default | Meaning |
| --- | --- | --- |
| `Enabled` | true | Off shows every window as the game does. |
| `HiddenOpacity` | 0 | Opacity of a hidden window; 0.2 leaves a faint outline. |
| `HidableWindows` | empty | Comma-separated prefab names of hidable window kinds. The eye button edits it. |
| `ToggleKey` | None | Optional key that tags or untags the window under the cursor while the cursor is free. |

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
