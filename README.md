# VR Card Desk — Interactive 3D Learning Cards

A VR learning experience built in **Unity 6 (C#)**: a deck of cards on a
classroom desk, each showing a 3D model (e.g. cell organelles). Players grab
cards by hand, pull them up to inspect, browse the whole deck, and flip cards to
read a description on the back.

<!-- Add a GIF or screenshot here, e.g.:
![Inspecting and flipping a card](docs/media/demo.gif) -->

---

## Features

- **Grab** a card with the grip — hold it, turn it, read the back; let go and it flies back to its slot.
- **Inspect** a card with the trigger — it flies in front of your eyes and follows your head.
- **Browse** the deck with the thumbstick while inspecting — the card's content changes in place.
- **Flip** the inspected card to read its title and description on the back.
- **One card at a time** — inspecting another card sends the current one home first.
- **Data-driven content** — each card is a `ScriptableObject` asset (title, 3D model, thumbnail, description); new cards need no code.

## Controls

| Action | VR controller | XR Device Simulator (keyboard / mouse) |
|---|---|---|
| Inspect / dismiss a card | Trigger (pointing at the card) | Left mouse button |
| Grab a card | Grip | G |
| Browse while inspecting | Thumbstick left / right | Simulator thumbstick keys |
| Flip the inspected card | A / X | F |

## Tech stack

Unity 6 (6000.3.20f1) · C# · URP · XR Interaction Toolkit 3 · OpenXR · Input System ·
Zenject (dependency injection) · TextMeshPro · tested on PICO (OpenXR via SteamVR / PICO Connect)

---

## Architecture

The code is split into **six assemblies** (assembly definitions), and the
dependency direction is **enforced by the compiler**: everything points inward
to `Core`, and `Core` depends on nothing.

```
                 Installers          composition root (Zenject)
     ┌──────┬────────┼─────────┬──────────┐
     ▼      ▼        ▼         ▼          ▼
  Input  Adapters  Presentation  Coordination
     └──────┴────────┼─────────┴──────────┘
                     ▼
                    Core              data + interfaces (ports)
```

| Layer | Responsibility | Key types |
|---|---|---|
| **Core** | Card data and the interfaces (ports) the rules depend on | `CardData`, `CardDeck`, `CardBrowser`, `ICardSelectable`, `ICardView`, `ICardMotion`, `ICardInspectInput` |
| **Input** | Turns buttons into intents (next, previous, dismiss, flip) | `InputActionInspectInput` |
| **Adapters** | The only code that knows XR Interaction Toolkit | `XRCardSelectable` |
| **Presentation** | How cards look and move | `CardView`, `CardMotion`, `InspectAnchorMarker` |
| **Coordination** | The rules: one card at a time, browse, flip, grab and release | `CardInspectionService`, `CardStackService`, `CardFacade` |
| **Installers** | Wiring with Zenject — the only place concrete types are named | `CardSceneInstaller`, `CardPrefabInstaller` |

### Key design decisions

- **Ports and adapters.** Coordination only talks to interfaces in Core. XR input
  lives in one adapter (`XRCardSelectable`), so a mouse or hand-tracking input
  would be a new adapter with no changes to the rules.
- **Intents, not buttons.** Events are named `InspectRequested`, `Grabbed`,
  `Released` — never `TriggerPressed` — so button mapping stays in one place.
- **Dependency injection instead of singletons.** One scene installer is the
  composition root; each card gets its own sub-container (`GameObjectContext`),
  so it receives its own view and motion while sharing the inspection service.
  Missing wiring fails at startup, by name.
- **Plain C# rules.** `CardInspectionService` and `CardBrowser` are not
  MonoBehaviours, so the rules can be unit-tested without a scene or headset.

### Example flow — trigger on a card

```
XRCardSelectable.OnActivated          (Adapters)      raises InspectRequested
 → CardFacade                         (Coordination)  forwards it with its identity
 → CardInspectionService.Toggle/Inspect (Coordination) applies the rules
 → CardMotion.MoveToAnchor            (Presentation)  coroutine flies the card into view
 → onComplete → card is interactable again
```

More detail: [architecture notes](My%20project/Assets/Scripts/docs/README.md).

---

## Getting started

1. Install **Unity 6000.3.20f1** via Unity Hub.
2. Clone the repository:
   ```bash
   git clone https://github.com/prostodiva/desk.git
   ```
3. In Unity Hub: **Add → Add project from disk** → select the **`My project`** folder.
4. Open **`Assets/Scenes/SampleScene`** and press **Play**.

### Testing without a headset
Enable the **XR Device Simulator** object in the scene, and untick **OpenXR**
under *Project Settings → XR Plug-in Management → desktop tab*.

### Testing with a PICO headset (Windows)
1. Install **PICO Connect** and **SteamVR**; set SteamVR as the active OpenXR runtime.
2. In *XR Plug-in Management → desktop tab*: tick **OpenXR**, add the
   **Oculus Touch** and **Khronos Simple** controller profiles, run **Project Validation → Fix All**.
3. Disable the **XR Device Simulator** object, connect the headset and press **Play**.

---

## Project structure

```
My project/
  Assets/
    Scripts/
      Core/  Input/  Adapters/  Presentation/  Coordination/  Installers/
      docs/            architecture notes
    Input/             CardInputActions (Flip, Dismiss)
    Prefabs/           CardPrefabDI — the card
    Scriptable/        MainDeck + one asset per card
    Scenes/            SampleScene
```

## Credits

- Classroom environment: *Cosmic Retro Laboratory 2* (demo) — Unity Asset Store
- Table model: *Fast Mesh* — Unity Asset Store
- [Zenject / Extenject](https://github.com/modesttree/Zenject) — dependency injection
- XR Interaction Toolkit Starter Assets — Unity

<!-- License: add one if you want others to reuse the code (e.g. MIT). -->
