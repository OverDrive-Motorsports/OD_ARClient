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
- `XR/ODPointerDragThreshold.cs`: raises the EventSystem drag threshold to
  about 3 cm on the window at startup, so the ray shake of a pinch or trigger
  press no longer starts a scroll and cancels the click.
- `Events/` is empty: the project doesn't have a formalized event layer yet,
  current scripts call UnityEvent/OnClick callbacks directly. To be built out
  as the app gains more interactions.

## Allowed dependencies

None towards Core/Data/Logic/UI. Interaction exposes events as output; it's
up to Logic to subscribe to them.
