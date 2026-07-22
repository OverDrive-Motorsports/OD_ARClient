# _Overdrive

Root of all code and assets specific to the Overdrive project. Anything
related to Unity configuration, third-party plugins or build settings
(Oculus/, Plugins/, Resources/, Settings/, TextMesh Pro/, XR/, TutorialInfo/,
root-level files under Assets/) stays outside of this folder and is not part
of this organization.

## Layers and dependency rule

```
UI            Interaction
  \               /
   \             /
      Logic  <---
      /  \
     /    \
  Core   Data
```

- **UI**: pure display (Atomic Design prefabs). Emits events, decides nothing.
- **Interaction**: input detection (VR, controllers, hand tracking...). Translates into events, no business logic.
- **Logic**: receives events from UI/Interaction, decides, orchestrates. The only layer allowed to talk to Core, Data, UI and Interaction at once.
- **Core**: backend communication (network, auth, global services). Knows nothing about UI or gameplay.
- **Data**: data structures (DTOs, entities, mocks). No dependency on any other layer.

Rule: **UI** and **Interaction** never reference Core/Data/Logic directly — they expose events that Logic listens to. See each folder's README for details.

## Folders

| Folder | Content |
|---|---|
| [Core/](../Core/Doc/README.md) | Backend communication, global services |
| [Data/](../Data/Doc/README.md) | Data models (DTOs, entities, mocks) |
| [Interaction/](../Interaction/Doc/README.md) | User input detection |
| [UI/](../UI/Doc/README.md) | Visual components (Atomic Design) |
| [Logic/](../Logic/Doc/README.md) | Business logic / orchestration |
| [Editor/](../Editor/Doc/README.md) | Editor-only scripts (build-time) |
| [DevAssets/](../DevAssets/Doc/README.md) | Prototyping/demo assets |
| Scenes/ | Project's Unity scenes |
