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

- `XR/WindowHandle.cs`: shared world-space white grab bar. A fresh pinch,
  trigger or grip on the pointed-at bar starts a grab. Rotation is smoothed
  around the actual grip point; idle windows do not turn with the viewer.
  `GrabStarted` lets auxiliary TV views detach before following the hand/ray.
- `XR/ODWindowGrabInput.cs`: one selector adapter per hand/controller ray,
  wired by `VideoMultiviewSetup`. Tracks only that ray's press/release and
  claims one window at a time, so another held controller cannot keep a
  window grabbed. Both enabled and inactive rig rays are cached to support
  switching between hands and controllers.
- `XR/ODPointerDragThreshold.cs`: raises the EventSystem drag threshold to
  about 3 cm on the window at startup, so the ray shake of a pinch or trigger
  press no longer starts a scroll and cancels the click.
- `Events/` is empty: the project doesn't have a formalized event layer yet,
  current scripts call UnityEvent/OnClick callbacks directly. To be built out
  as the app gains more interactions.

## Allowed dependencies

None towards Core/Data/Logic/UI. Interaction exposes events as output; it's
up to Logic to subscribe to them.
