# Changelog

## 1.3.1

- **The eye button now looks like the game's own title-bar buttons.** It used to be a bare white outline, smaller
  than the sort, dock and close buttons, with no background and no reaction to the mouse. It now sits on the same
  grey button tile, the same size, with the eye drawn in the same grey as their icons and the same drop shadow. Under
  the pointer it switches to the game's dark highlighted tile with a white eye and grows slightly, and it dims while
  clicked, as the buttons beside it do. Bright eye still means hidable, faint eye always shown.
- **The eye has a tooltip**, like the buttons beside it, saying whether the window hides and what a click does.

## 1.3.0

- `OpaqueWindows` (off by default): every inventory window, hidable or not, gets a solid background instead of the
  game's see-through one, which is hard to read. The game draws the background at colour alpha 0.75 with a sprite
  whose inside is alpha 0.75 as well; the mod raises the colour to 1 and uses a copy of the sprite with an opaque
  inside, set once and restored when the setting or `Enabled` turns off. Hidden windows still hide. Other UI is
  untouched. README and the Workshop page describe it.

## 1.2.0

- Tags are kept per save. Reference ids only mean something inside one save, so a window tagged in one save could
  hide an unrelated window in another. Each world now has its own list, keyed by the world id the save stores. Tags
  saved by 1.1 and earlier move into the first world you load.

## 1.1.0

- Hidden windows show only while you hold the Mouse Control key (Alt unless rebound), not whenever the cursor is
  free. Any open screen can free the cursor, and a stuck one (the scoreboard) kept every window shown.
- Double press Alt to latch hidden windows on; double press again to hide them (`DoublePressSeconds`, 0 turns it off).

## 1.0.0

- First release. Same behaviour as 0.2.0.

## 0.2.0

- Tags are per window (the reference id of the thing it shows), not per kind of item. Tags saved by 0.1.0 were
  prefab names and no longer match anything; tag the windows again.

## 0.1.0

- First version: eye button on inventory windows, hidable windows hide while the cursor is locked and show while
  it is free, tags per item kind, hidden opacity, optional toggle key.
