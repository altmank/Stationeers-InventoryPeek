# Inventory Peek

A Stationeers mod: inventory windows you tag as hidable disappear while you play and come back the moment
you hold Alt (the game's Mouse Control key). Keep every window open without it covering the screen.

## Use

- Click the **eye** on an inventory window's title bar, beside the game's sort, dock and close buttons and drawn
  like them. Bright eye: the window is hidable. Faint eye: always shown. Hover it for a reminder.
- Play normally: hidable windows are hidden and let clicks through. Hold Alt: every window shows at once, where
  you left it, ready to use.
- A drag that starts while you hold Alt keeps the window up until you let go of the mouse button.
- Double press Alt quickly to latch hidden windows on while you play; double press again to hide them.
- The game draws inventory windows see-through. Turn on `OpaqueWindows` to give every inventory window a solid
  background, so a shown window is easy to read.

## How it decides

- **Tags are per window.** A window is tagged by the reference id of the one thing it shows, which the save keeps,
  so the choice survives sessions and each window is chosen on its own: tagging one backpack's window does not
  hide another backpack's. Reference ids only mean something inside one save, so each world keeps its own list,
  keyed by the world id the save stores (`World.CurrentId`): the `[HidableWindows]` config section, one line per
  world, which can be edited by hand. A copy of a save shares its world id and so its tags, which is right, since
  its items keep their ids. Tags saved by 1.1 and earlier move into the first world loaded.
- **Peeking** is holding the game's Mouse Control key (`KeyMap.MouseControl`, Alt unless rebound), read the way
  the game reads it (`MouseModeController.AltKeyDown`), so a rebind is followed. Screens that free the cursor on
  their own (menus, the scoreboard) do not show hidden windows: a stuck scoreboard once kept them all visible.

## Settings (BepInEx config `net.xceled.stationeers.inventorypeek.cfg`)

| Setting | Default | Meaning |
| --- | --- | --- |
| `Enabled` | true | Off shows every window as the game does. |
| `HiddenOpacity` | 0 | Opacity of a hidden window; 0.2 leaves a faint outline. |
| `[HidableWindows]` `<world id>` | empty | Per world: comma-separated reference ids of the things whose windows hide. The eye button edits it. |
| `HidableWindows` (General) | empty | Tags from 1.1 and earlier; moved into the first world loaded, then emptied. |
| `DoublePressSeconds` | 0.3 | Two presses of the Mouse Control key within this time latch hidden windows on or off; 0 turns the latch off. |
| `OpaqueWindows` | false | Every inventory window, hidable or not, gets a solid background instead of the game's see-through one. Hidden windows still hide; other UI is untouched. |
| `ToggleKey` | None | Optional key that tags or untags the window under the cursor while you hold Alt. |

## How little it changes

- One Harmony postfix, on `InventoryWindow.Assign`, adds a small component to each inventory window.
- Hiding is done with a `CanvasGroup` of the mod's own on the window: opacity and click-through only. The game's
  canvas, layout, open/closed state, docking and saved window state are never touched, so turning the mod off
  (or removing it) leaves every window exactly as the game has it.
- A docked window keeps its place in the dock while hidden, so the dock shows a gap there.
- `OpaqueWindows` changes one thing the game owns: the window's background image. The prefab gives it colour alpha
  0.75 and a sprite whose inside is alpha 0.75 too, so about 56% of the window is the window and the rest is the world.
  The mod raises the colour alpha to 1 and swaps in a copy of the sprite with its inside made opaque (the frame and
  rounded corners are unchanged). No game code writes that image, so it is set once when the setting turns on and put
  back when it (or `Enabled`) turns off; nothing is re-applied every frame. Slot buttons, the title bar and the rest
  of the UI keep the game's look; over a solid background they no longer show the world through.
- The eye button is a copy of the window's own sort button with the game's scripts and animator taken off. Its
  pictures are made once from the sort button's two sprites (at rest and under the pointer): the button's frame is
  kept, the arrows are replaced with the eye, and it grows and dims under the pointer and click as the game's buttons do.

## Multiplayer

Only the players who want it need the mod. The host does not need it, and players with and without it, or on
different versions of it, can play together.

- It changes your own inventory windows and nothing else. It sends nothing over the network, and the game never
  shares a player's window layout, so nobody else sees your hidden windows, your eye buttons or `OpaqueWindows`.
- A player without the mod sees nothing different at all.
- Your tags live in your own config, not in the host's save. They are filed under the host's world id, which the
  game hands every player who joins, and the items keep the same reference ids on every player's game, so your
  choices come back each time you join the same world.
- Every other setting belongs to each player too.
- Worked out from the game's code rather than from a multiplayer session.

## Build

Needs Stationeers with BepInEx 5.4 and [StationeersLaunchPad](https://github.com/StationeersLaunchPad/StationeersLaunchPad).
`.\build.ps1` builds and stages `package\`; `.\build.ps1 -Deploy` also copies it into the local mods folder
(close the game first).
