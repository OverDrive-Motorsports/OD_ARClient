# Interaction

Handles all user interactions: input detection (VR controllers, hand
tracking, gaze, clicks...) and translation into events usable by the rest of
the application. Contains **no business logic**: this folder only signals
what the user did, it's [Logic](../../Logic/Doc/README.md) that decides what
to do about it.

## Structure

```
Interaction/
├── XR/      VR/XR interactions (controllers, hand tracking, Meta Interaction SDK)
└── Events/  Interaction events, Input Actions wrappers
```

## Current state

- `XR/WindowHandle.cs` (moved from `Scripts/UI/`): the project's only real XR
  interaction script — a world-space draggable handle using the Meta/Oculus
  Interaction SDK (ray/pinch/grip).
- `XR/ODWindowPlacement.cs`: where windows sit around the user, as pure
  geometry. Windows sit on an arc around the headset, 1.3 m away, at the same
  height, each turned to face the head, with a small gap between neighbours.
- `XR/ODWindowMotion.cs`: glides a window to a new pose with a damped spring
  (no overshoot). It only runs while the window is moving, and grabbing the
  window's bar stops it.
- Both are used by `Logic/Menu/ODWindowLayout`. The race TV takes the centre
  when it appears and the main window glides to its right. When the TV
  closes, the main window glides back to the centre. The widget catalogue
  lines up on the left.
- `Events/` is empty: the project doesn't have a formalized event layer yet,
  current scripts call UnityEvent/OnClick callbacks directly. To be built out
  as the app gains more interactions.

## Allowed dependencies

None towards Core/Data/Logic/UI. Interaction exposes events as output; it's
up to Logic to subscribe to them.
