# Migrating the card system from Legacy to Zenject

This guide switches the scene from the old scripts in `Scripts/Legacy/` to the
Zenject-based architecture described in `README.md`, then removes the old code.

The new setup is built **alongside** the old one and tested before anything is
deleted, so the project keeps working at every step.

---

## Where things stand today

| Piece | Old (currently running) | New (compiled, not yet wired) |
|---|---|---|
| Spawns & lays out the stack | `CardStackBuilder` on `table/CardStack` | `CardStackService`, created by `CardSceneInstaller` |
| One physical card | `InteractableCard` on `CardPrefab` | `XRCardSelectable` + `CardFacade` |
| Draws card content | `CardVisual` on `CardPrefab` | `CardView` |
| Fly-to-inspect / browse / return | `CardInspector` on `CardPrefab` | `CardMotion` (movement) + `CardInspectionService` (rules) + `InputActionInspectInput` (controller input) |
| Inspect pose marker | `CardInspectAnchor` on `InspectAnchor` | `InspectAnchorMarker` |
| Card data | `CardData`, `CardDeck` (`MainDeck`, `mito`, `vesicle`) | same — unchanged |

`SceneContext` with `CardSceneInstaller` already exists in the scene. Its
**Deck** and **Card Prefab** fields are set; **Inspect Anchor**, **Inspect
Input** and **Stack Settings → Root** are empty.

---

## Step 0 — Commit a safe point

Everything below is editor work, and a prefab or scene mistake is easiest to
undo from git.

```bash
cd "/Users/evolvers/dev/vr/desk/desk" && git add -A && git commit -m "Working legacy card stack before migration"
```

---

## Step 1 — Record the old settings you'll need to copy

Open `Assets/Prefabs/CardPrefab` (double-click) and select the root
`CardPrefab` object. Screenshot or note these before changing anything.

**Interactable Card** component (XR grab settings):

| Inspector field | Current value |
|---|---|
| Interaction Layer Mask | Default |
| Select Mode | Single |
| Attach Transform | `AttachPoint` (child object) |
| Use Dynamic Attach | off |
| Attach Ease In Time | 0.15 |
| Movement Type | Instantaneous |
| Track Position / Track Rotation | on / on |
| Smooth Position / Smooth Rotation | off / off |
| Throw On Detach | on |
| Retain Transform Parent | on |
| Hover Highlight | none |

**Card Inspector** component:

| Inspector field | Current value |
|---|---|
| Travel Duration | 0.35 |
| Browse Input | an action from *XRI Default Input Actions* — note its exact name |
| Browse Threshold | 0.6 |
| Dismiss Input | an action from *XRI Default Input Actions* — note its exact name |

**Card Visual** component:

| Inspector field | Points at child |
|---|---|
| Face Renderer | `Visual` |
| Title Text | `title` |
| Model Anchor | `modelAnchor` |
| Model Max Size / Spin Model / Spin Speed | 0.06 / on / 30 |

Close the prefab without changes.

---

## Step 2 — Create the new card prefab

`InteractableCard` and `XRCardSelectable` are both `XRGrabInteractable`s and
can't live on the same object, so the new card is a **copy** of the old one.
The old prefab stays untouched and keeps the legacy setup working.

### 2.1 Duplicate

**Project window** → `Assets/Prefabs` → select `CardPrefab` → **Cmd+D** →
rename the copy to **`CardPrefabDI`** → double-click to open it.

### 2.2 Remove the old components

**Hierarchy** (prefab mode): select the root `CardPrefabDI`.

**Inspector**, remove in this order (⋮ menu → *Remove Component*):

1. **Card Inspector** — first, because it has `[RequireComponent(InteractableCard)]`
   and Unity won't let you remove `InteractableCard` while it's there.
2. **Card Visual**
3. **Interactable Card**

Keep **Rigidbody**, the **Collider**, and all child objects (`Visual`, `bg`,
`AttachPoint`, `modelAnchor`, `title`).

### 2.3 Add the new components

Still on the root, **Add Component** for each of these and fill them in:

**XR Card Selectable** — the XRI grab component, re-configured from Step 1:

| Field | Set to |
|---|---|
| Interaction Layer Mask | Default |
| Select Mode | Single |
| Attach Transform | drag the `AttachPoint` child in |
| Attach Ease In Time | 0.15 |
| Movement Type | Instantaneous |
| Track Position / Track Rotation | on / on |
| Throw On Detach | on |
| Retain Transform Parent | on |
| Hover Highlight | optional — a glow child, or leave empty |

**Card View** — replaces `CardVisual`:

| Field | Drag in |
|---|---|
| Face Renderer | `Visual` child |
| Title Text | `title` child |
| Model Anchor | `modelAnchor` child |
| Model Max Size / Spin Model / Spin Speed | 0.06 / on / 30 |

**Card Motion** — the fly-to-anchor and return animation:

| Field | Set to |
|---|---|
| Travel Duration | 0.35 |

**Card Facade** — no fields. It receives its parts by injection.

**Game Object Context** (Zenject) — gives this card its own DI sub-container,
so each `CardFacade` gets *its own* view and motion rather than another card's.

**Card Prefab Installer** — binds this card's components to the interfaces:

| Field | Drag in (drag the component header, not the GameObject) |
|---|---|
| Selectable | the **XR Card Selectable** component above |
| View | the **Card View** component |
| Motion | the **Card Motion** component |

### 2.4 Register the installer

On **Game Object Context** → **Mono Installers** list → **+** → drag the
**Card Prefab Installer** component into the new slot.

Save the prefab (**Cmd+S**) and exit prefab mode.

---

## Step 3 — Inspect anchor

**Hierarchy**: `XR Origin (XR Rig)` → `Camera Offset` → `Main Camera` →
**`InspectAnchor`**.

**Inspector** → **Add Component** → **Inspect Anchor Marker**. It has no fields;
it simply exposes this transform as the inspect pose.

Leave the old **Card Inspect Anchor** component on it for now (removed in Step 7).

---

## Step 4 — Controller input

Browsing and dismissing used to be read inside every card's `CardInspector`.
Now one object on the rig reads them.

**Hierarchy**: select **`XR Origin (XR Rig)`**.

**Inspector** → **Add Component** → **Input Action Inspect Input**:

| Field | Set to |
|---|---|
| Browse Input | the same action you noted from Card Inspector (Vector2 thumbstick) |
| Browse Threshold | 0.6 |
| Dismiss Input | the same action you noted from Card Inspector (button) |

Both actions come from `Assets/Samples/XR Interaction Toolkit/3.3.2/Starter
Assets/XRI Default Input Actions` — use the object picker (◎) next to each field.

---

## Step 5 — Wire the SceneContext

**Hierarchy**: select **`SceneContext`**.

### 5.1 Card Scene Installer fields

| Field | Set to |
|---|---|
| Deck | `MainDeck` (already set) |
| Inspect Anchor | drag the `InspectAnchor` object from the Hierarchy |
| Inspect Input | drag `XR Origin (XR Rig)` from the Hierarchy |
| Card Prefab | **`CardPrefabDI`** (change it from the old `CardPrefab`) |
| Stack Settings → Root | drag `table/CardStack` from the Hierarchy |
| Stack Settings → Layout | **Fan** — see the note below |
| Stack Settings → Stack Spacing / Fan Spacing / Fan Arc Angle / Use Arc | 0.003 / 0.05 / 30 / on |

> **Layout note:** the old `Card Stack Builder` is currently set to **Draw
> Pile**. The new `CardStackService` doesn't fully support Draw Pile yet (see
> *Known gap* at the end), so test with **Fan** first.

### 5.2 Register the installer

On **Scene Context** → **Mono Installers** list: make sure **Card Scene
Installer** is in it. If not, **+** and drag the component into the slot.

---

## Step 6 — Switch over and test

### 6.1 Turn the old stack off

**Hierarchy**: select `table/CardStack` → **Inspector** → **Card Stack
Builder** ⋮ → **Remove Component**. Your rollback is the git commit from Step 0.

> Unticking the component is **not** enough: Unity still calls `Awake()` on
> disabled components, and `CardStackBuilder` builds its stack in `Awake()` —
> so you'd get the old cards *and* the new ones.

### 6.1b Take `CardStack` out from under the scaled table

`table` has Scale **(1.2, 0.05, 1)**. Anything parented under it inherits that
squash, and once cards are rotated (Fan layout) or re-parented (inspect) it
shows up as stretched/skewed cards.

1. **Hierarchy**: drag `CardStack` out of `table` onto an empty spot, so it
   sits at the scene root. Unity keeps its world position.
2. **Inspector** → Transform → set **Scale** to **1, 1, 1** (Unity will have
   put 1.2 / 0.05 / 1 there to keep the old look).
3. **SceneContext** → Card Scene Installer → **Root** should still point at
   `CardStack`; re-drag it if not.

### 6.2 Play-test

Press **Play** and check each of these:

- [ ] Cards appear fanned on the table under `CardStack`.
- [ ] Each card shows its own title, thumbnail and spinning model.
- [ ] Selecting a card flies it to the inspect pose in front of the camera.
- [ ] The thumbstick pages through the deck; the card's content changes, the object doesn't move.
- [ ] Dismiss button, or selecting the inspected card again, sends it home showing its own content.
- [ ] Selecting a second card while one is inspected sends the first one home.

### 6.3 If something fails

| Console message | Cause | Fix |
|---|---|---|
| `Unable to resolve 'ICardSelectable'` (or `ICardView`, `ICardMotion`) | The card's sub-container didn't run | Step 2.3 / 2.4: Game Object Context present, Card Prefab Installer in its Mono Installers list, all three fields set |
| `Unable to resolve 'IInspectAnchor'` / `'ICardInspectInput'` | Empty installer field | Step 5.1: Inspect Anchor / Inspect Input |
| `InvalidCastException` in `CardPrefabInstaller` | A field got the GameObject or the wrong component | Step 2.3: drag the specific component header into each field |
| No cards, no errors | Installer not registered or old prefab still assigned | Step 5.1 Card Prefab = `CardPrefabDI`; Step 5.2 installer in list |
| Cards spawn at scene origin | Root not set | Step 5.1 Stack Settings → Root |

To roll back at any point: re-tick **Card Stack Builder** and remove **Card
Scene Installer** from the SceneContext's Mono Installers list.

---

## Step 7 — Remove the legacy parts

Only after every box in 6.2 is ticked. Commit first:

```bash
cd "/Users/evolvers/dev/vr/desk/desk" && git add -A && git commit -m "Zenject card setup working"
```

Then, **inside Unity** (never in Finder — the `.meta` files must go too):

**Scene components**

| Hierarchy object | Inspector action |
|---|---|
| `table/CardStack` | Remove Component → **Card Stack Builder** |
| `…/Main Camera/InspectAnchor` | Remove Component → **Card Inspect Anchor** |

Save the scene (**Cmd+S**).

**Project window**

| Delete | Why it's safe |
|---|---|
| `Assets/Prefabs/CardPrefab` | Replaced by `CardPrefabDI` |
| `Assets/Scripts/Legacy/` (whole folder) | Nothing outside it references these scripts |
| `Assets/Plugins/Zenject/OptionalExtras/` *(optional)* | Zenject samples and tests (~24 MB); keep `Plugins/Zenject/Source` |

Optionally rename `CardPrefabDI` → `CardPrefab` in the Project window. Unity
keeps the reference in `SceneContext` because it tracks the asset by GUID.

Press **Play** once more to confirm, then commit.

---

## Known gap: Draw Pile mode

The old `CardStackBuilder` in **Draw Pile** layout made the next card
grabbable once the top one was taken. `CardStackService` doesn't do this yet:
in Draw Pile mode only the first top card is ever selectable. **Fan** mode is
unaffected. If you need Draw Pile, `CardStackService` has to subscribe to each
card's selection and enable the next card down.
