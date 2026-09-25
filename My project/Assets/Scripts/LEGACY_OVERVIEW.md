# Legacy card system — overview

How the original card system worked, step by step, and why it was replaced.

> The legacy scripts have been removed from the project. To read the code this
> document refers to (file names and line numbers), check out the git tag
> `legacy-working`, or browse it on GitHub under that tag.

---

## Key terms

| Term | Meaning |
|---|---|
| **Spawn** | Create a new object in the scene *while the game is running*, from code. Here, `Instantiate(cardPrefab)` makes a live copy of the card prefab. The cards don't exist in the scene before you press Play — they're spawned when it starts. |
| **Prefab** | A saved template of a GameObject (with its children and components) stored in the Project window. `CardPrefab` is the template every card is copied from. |
| **Component** | A script or built-in piece (Rigidbody, Collider…) attached to a GameObject. Each file below is a component. |
| **ScriptableObject** | A data asset stored in the Project, not in the scene. `MainDeck`, `mito` and `vesicle` are ScriptableObjects. |
| **XRI** | Unity's XR Interaction Toolkit. Provides pointing, hovering, selecting and grabbing with VR controllers. |
| **Singleton** | A class that keeps one global instance reachable from anywhere via a `static` field, e.g. `CardStackBuilder.Instance`. |
| **Coroutine** | A method that runs a little every frame (`yield return null`), used here for smooth movement over time. |
| **Parenting** | Putting an object under another in the Hierarchy. A child moves with its parent. |

---

## The pieces and where they live

```
PROJECT (assets)                         SCENE (while playing)
─────────────────                        ─────────────────────
MainDeck ── list of ──► mito, vesicle     table
 (CardDeck)             (CardData)          └─ CardStack  [CardStackBuilder]
                                                 ├─ Card (copy of CardPrefab)
CardPrefab                                       ├─ Card
 ├─ InteractableCard                             └─ …one per CardData
 ├─ CardVisual
 └─ CardInspector                         XR Origin
                                            └─ Main Camera
                                                 └─ InspectAnchor [CardInspectAnchor]
```

---

## What happens at runtime

1. **Press Play.** `CardStackBuilder.Awake()` runs on `CardStack`.
2. **Spawn.** For each `CardData` in `MainDeck`, it spawns a copy of
   `CardPrefab` as a child of `CardStack` and gives it that card's data.
3. **Draw.** Each card's `InteractableCard` passes its data to `CardVisual`,
   which puts the thumbnail on the face, sets the title and spawns the small
   3D model.
4. **Arrange.** The builder lays the cards out as a Draw Pile or a Fan.
5. **Point & select.** The controller ray hovers a card (optional highlight)
   and the player presses select. XRI fires `selectEntered`.
6. **Inspect.** `CardInspector` hears it, remembers where the card was, finds
   `InspectAnchor`, and flies the card there over 0.35 s. If another card was
   already being inspected, that one is sent home first.
7. **Browse.** While inspecting, flicking the thumbstick left/right swaps the
   card's *content* to the previous/next deck entry (with a small "pulse"). The
   object itself stays put.
8. **Return.** Pressing dismiss, or selecting the card again, flies it back to
   its saved spot and restores its original content.

---

## The files

### `CardStackBuilder` — on `table/CardStack`
**Job:** build and lay out the stack of cards.

- **Inspector fields:** Card Deck (`MainDeck`), Card Prefab, Layout Mode, and
  spacing/arc settings.
- **`BuildStack()`** destroys any old cards, spawns one per deck entry, calls
  `SetData` on each, then arranges them. You can also run it from the
  component's ⋮ menu → *Rebuild Stack*.
- **Draw Pile:** cards stacked 3 mm apart; only the top one is enabled. When
  it's grabbed, the next one down is enabled.
- **Fan:** cards spread in an arc (or a straight row); all are enabled. A tiny
  height offset per card stops their colliders overlapping exactly.
- **`static Instance`** lets any script reach the builder, and through it the
  deck. `CardInspector` uses this.

### `InteractableCard` — on `CardPrefab`
**Job:** make the card grabbable/selectable and hold its data.

- **Extends XRI's `XRGrabInteractable`**, so it inherits all the grab settings
  in the Inspector (Movement Type, Attach Transform, Throw On Detach…).
- **`Data`** is the `CardData` this object currently shows.
- **`SetData(data)`** stores the data and asks `CardVisual` to redraw.
- **Hover:** shows/hides an optional highlight object when the ray enters or
  leaves the card.
- **Enabling:** other scripts switch this component off to make the card
  temporarily unselectable (while flying, or buried in the draw pile).

### `CardVisual` — on `CardPrefab`
**Job:** turn a `CardData` into what you see. No gameplay logic.

- **Thumbnail:** puts `CardData.Thumbnail` on the face renderer (`Visual`
  child). Each card gets its own material copy so they can differ.
- **Title:** writes `CardData.Title` into the TextMeshPro `title` child.
- **3D model:** spawns `CardData.ModelPrefab` under `modelAnchor`, disables its
  colliders (so it doesn't block the controller ray), and scales it to fit
  6 cm.
- **Spin:** rotates the model slowly every frame so it reads as 3D.

### `CardInspector` — on `CardPrefab`
**Job:** everything about inspecting: moving, browsing, returning. The largest
script.

- **Listens** to the card's `selectEntered`. First select → inspect, second →
  send home.
- **One at a time:** `static current` remembers which card is at the anchor.
  A new inspect sends the previous card home first, so two cards never overlap
  (overlapping faces flicker, called *z-fighting*).
- **Fly out (`MoveToAnchor`):** freezes physics, parents the card to the
  anchor, then smoothly moves and rotates it to the anchor's exact pose.
- **Browse (`Update` + `Step`):** reads the thumbstick X value. It needs the
  stick to return to centre between flicks, so one flick = one card. Uses
  `CardStackBuilder.Instance.Deck` to know the order.
- **Fly home (`MoveHome`):** restores the card's own data, re-parents it to
  `CardStack`, and moves it back to its saved local position/rotation.
- **Input:** its own Browse and Dismiss action fields, which it enables in
  `OnEnable`. Every card enables the same actions.

### `CardInspectAnchor` — on `Main Camera/InspectAnchor`
**Job:** mark the spot, in front of the player's eyes, where inspected cards go.

- Because it's a child of the camera, the pose follows the player's head.
- **`static Instance`** lets `CardInspector` find it without any Inspector link.
- Draws a cyan card-sized box and a yellow "face" line in the Scene view so
  you can position it.

### Not legacy, still used

- **`CardData`**: one card's title, 3D model and thumbnail.
- **`CardDeck`**: the ordered list of `CardData` (`MainDeck`).
- **`HideRayWhileHolding`** on each controller: hides the laser line while the
  controller is holding something, so it doesn't cut across the card.

---

## Why replace it

### 1. Hidden connections

Scripts find each other through `static` singletons. You can't see these links
in the Inspector, and they fail silently if an object is missing or duplicated.

**In the code:** `CardStackBuilder.cs:40–50`. The builder makes itself globally
reachable, and falls back to searching the whole scene:

```csharp
private static CardStackBuilder instance;

public static CardStackBuilder Instance
{
    get
    {
        if (instance == null)
            instance = FindFirstObjectByType<CardStackBuilder>();
        return instance;
    }
}
```

`CardInspector.cs:120` and `:163` then reach out to it and to the anchor:

```csharp
Transform anchor = CardInspectAnchor.Instance != null ? CardInspectAnchor.Instance.transform : null;
...
var builder = CardStackBuilder.Instance;
deckCards = builder.Deck.Cards;
```

**What goes wrong:** if `InspectAnchor` is missing, you only find out when a
card is clicked (a Console warning). With two stacks in a scene, every card
browses whichever deck's builder ran `Awake` last. Nothing in the Inspector
shows that `CardInspector` depends on either.

**In the new design:** dependencies are constructor parameters, so they're
visible and must be provided. `CardInspectionService.cs:23`:

```csharp
public CardInspectionService(IInspectAnchor anchor, ICardInspectInput input, ICardProvider provider)
```

### 2. One script does too much

`CardInspector` mixes four jobs. Changing one part risks breaking another.

**In the code:** all in `CardInspector.cs`:

| Job | Where |
|---|---|
| Reading controller input | `OnEnable` (lines 74–83), `Update` (191–210) |
| Animating the card | `MoveToAnchor` (243–270), `MoveHome` (272–308), `PulseRoutine` (225–241) |
| Browsing the deck | `ResolveDeck` (158–177), `Step` (212–219) |
| "One card at a time" rule | `static current` (51), `BeginInspect` (136–139) |

**What goes wrong:** to change the flight animation you have to work inside a
script that also handles input and deck order. To test the "one card at a
time" rule you need a real scene, a camera anchor and a controller.

**In the new design:** each job is its own class: input → `InputActionInspectInput`,
animation → `CardMotion`, browsing → `CardBrowser`, rules → `CardInspectionService`.

### 3. Tied to XR everywhere

XRI and Input System types run through the card scripts, so the card logic
can't be reused with hand tracking, a desktop build or automated tests without
rewriting it.

**In the code:** the card *is* an XRI class (`InteractableCard.cs:12`):

```csharp
public class InteractableCard : XRGrabInteractable
```

and the inspector listens to an XRI-specific event and Input System types
(`CardInspector.cs:5–6, 28, 76, 110`):

```csharp
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
[SerializeField] private InputActionReference browseInput;
card.selectEntered.AddListener(OnSelected);
private void OnSelected(SelectEnterEventArgs args) { ... }
```

`CardStackBuilder.cs:174` does the same: `card.selectEntered.AddListener(...)`.

**What goes wrong:** a mouse-click desktop version, or a test that "selects" a
card from code, would need its own copy of all three scripts.

**In the new design:** only one small adapter knows about XRI.
`XRCardSelectable.cs:16` turns XRI's grab into a plain C# event, and everything
else uses the interface (`ICardSelectable.cs:9`):

```csharp
public interface ICardSelectable
{
    event Action Selected;
    bool InteractionEnabled { get; set; }
}
```

A desktop build would add a `MouseCardSelectable` and change nothing else.

### 4. Duplicated input

Every card enables the same browse/dismiss actions, instead of one object
reading input once.

**In the code:** `CardInspector.cs:74–94` runs on *every* card:

```csharp
private void OnEnable()
{
    if (browseInput != null) browseInput.action.Enable();
    if (dismissInput != null)
    {
        dismissInput.action.Enable();
        dismissInput.action.performed += OnDismissPressed;
    }
}

private void OnDisable()
{
    if (browseInput != null) browseInput.action.Disable();
    ...
}
```

**What goes wrong:** with 10 cards there are 10 dismiss listeners on one
action. The actions are shared assets, so when *any one* card's
`CardInspector` is disabled or destroyed, its `OnDisable` turns browsing and
dismiss **off for every card**.

**In the new design:** one component on the XR rig reads input and raises
`NextRequested` / `PreviousRequested` / `DismissRequested`
(`ICardInspectInput.cs:9–13`). Only `CardInspectionService` listens, and only
while a card is being inspected.

---

**The new design** splits these jobs into small single-purpose pieces behind
interfaces (`ICardView`, `ICardMotion`, `ICardSelectable`…). **Zenject** wires
them together in one visible place (`CardSceneInstaller`). Dependencies become
explicit, swappable and testable. See `README.md` for the design and
`MIGRATION.md` for the switch-over steps.
