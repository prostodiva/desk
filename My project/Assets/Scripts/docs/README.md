# Card System — Layered Architecture

## Dependency direction

```
        ┌──────────────┐
        │  Installers  │  (composition root — knows everything)
        └──────┬───────┘
               │ wires
   ┌───────────┼───────────┬──────────────┐
   ▼           ▼           ▼              ▼
 Input     Adapters   Presentation   Coordination
   │           │           │              │
   └───────────┴───────────┴──────────────┘
                     ▼
                   Core
```

**Core depends on nothing.** Every other layer depends on Core and on nothing
else horizontally. Coordination never references XRI, TextMeshPro or the
Input System — it talks to `ICardSelectable`, `ICardView`, `ICardMotion`,
`ICardInspectInput` and `IInspectAnchor`.

The practical test: if you swapped XR Interaction Toolkit for hand tracking,
only `Cards.Adapters` changes. If you moved from thumbstick paging to voice
commands, only `Cards.Input` changes.

## What lives where

| Layer | Types | Why |
|---|---|---|
| **Core** | `CardData`, `CardDeck`, `CardBrowser`, all interfaces | Models and pure logic. `CardBrowser` is plain C# and unit-testable with no Unity runtime. |
| **Input** | `InputActionInspectInput` | The only place that knows about deadzones and which button dismisses. Emits intents, not raw values. |
| **Adapters** | `XRCardSelectable`, `HideRayWhileHolding` | The only types referencing XR Interaction Toolkit. |
| **Presentation** | `CardView`, `CardMotion`, `InspectAnchorMarker` | Rendering and movement. Knows nothing about selection or deck order. |
| **Coordination** | `CardFacade`, `CardInspectionService`, `CardStackService` | The rules: one card at a time, browsing swaps content, dismissal restores own content. |
| **Installers** | `CardSceneInstaller`, `CardPrefabInstaller` | Where concrete types are named. |

## Scene setup

### 1. SceneContext

Create an empty GameObject, add Zenject's **SceneContext**, then add
**CardSceneInstaller** and drag it into the SceneContext's Installers list.

Assign on the installer:
- **Deck** — your `MainDeck` asset
- **Inspect Anchor** — the empty under Main Camera with `InspectAnchorMarker`
- **Inspect Input** — the object on the XR rig with `InputActionInspectInput`
- **Card Prefab** — the card prefab (see below)
- **Stack Settings → Root** — the `CardStack` transform on the table

### 2. Card prefab

On the prefab root:
- `Rigidbody`, `Collider`
- `XRCardSelectable`
- `CardView` (assign Face Renderer, Title Text, Model Anchor)
- `CardMotion`
- `CardFacade`
- Zenject **GameObjectContext**
- `CardPrefabInstaller`, added to the GameObjectContext's Installers list,
  with its three fields pointing at the components above

The `GameObjectContext` is what gives each card its own sub-container, so
`CardFacade` receives *that card's* view and motion rather than another
card's. Without it, all cards resolve to whichever one bound last.

### 3. Input object

On the XR rig, add `InputActionInspectInput` and assign:
- **Browse Input** — a Vector2 thumbstick action
- **Dismiss Input** — a button action (grip works well)

## Notes on the migration

- **Static singletons are gone.** `CardStackBuilder.Instance`,
  `CardInspectAnchor.Instance` and `CardInspector.current` were service
  locators; they're now constructor-injected.
- **`CardInspector` split into three.** Input reading went to `Cards.Input`,
  pose animation to `CardMotion`, and the arbitration rules to
  `CardInspectionService`.
- **`InteractableCard` no longer exists.** The XRI dependency is isolated in
  `XRCardSelectable`; the domain-facing object is `CardFacade`.
- **Reset everything** is `CardStackService.Build()` — inject the service
  wherever you want to call it.

## Assembly definition references

If Unity flags a missing reference, the assembly names to add by hand are:
`Unity.XR.Interaction.Toolkit`, `Unity.InputSystem`, `Unity.TextMeshPro`,
and `Zenject`. Zenject's assembly name can vary by install method — check the
asmdef inside the Zenject package folder and match it exactly.
