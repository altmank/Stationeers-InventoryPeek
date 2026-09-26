# Changelog

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
