# Wizzards Cauldron — Implementation Plan

## 1. Project Summary

**Wizzards Cauldron** is a small first-person VR game that presents the 0/1 knapsack problem through physical interaction. The player moves through a wizard-themed room, inspects potion bottles on a shelf, and adds selected potions to a cauldron. Bottle-cap removal is optional and is not part of the current MVP.

Each potion has:

- A **health value**, representing the benefit added to the final product.
- A **fill value**, representing how much cauldron capacity it consumes.

Only a whole potion may be added. The player must choose a combination whose total fill does not exceed the cauldron capacity while maximizing total health. The player finishes the attempt by touching or pointing at the cauldron with a wand, then sees the result and may reset the room.

The project is intended for a class and a team of three, so the priority is a short, reliable, understandable experience rather than a large amount of content.

## 2. Technical Baseline

The repository currently uses:

- Unity `6000.0.42f1`
- Universal Render Pipeline `17.0.4`
- XR Interaction Toolkit `3.4.1`
- OpenXR Plugin `1.16.1`
- Input System `1.15.0`
- XR Hands `1.7.3`

The implementation should use the existing XR Interaction Toolkit components wherever possible. Custom code should focus on game rules, state, and feedback rather than replacing grabbing, ray interaction, or controller input supplied by the toolkit.

## 2.1 Current Project Decisions

Decisions recorded on 2026-08-06:

- The player may move around the room. The initial locomotion baseline is continuous movement plus snap turning; teleportation may be retained as an accessibility option.
- Development must work without regular access to a physical headset. The XR Interaction Simulator is the primary day-to-day test method. Headset tests are milestone checks performed when hardware becomes available.
- Bottle-cap removal is no longer required for the MVP. It may be added later as an optional interaction only after the core loop is stable.
- The first reusable gameplay prefab is a generic potion bottle named `PF_PotionBottle`. Individual potion values and presentation should be supplied through data and materials instead of duplicating bottle logic.
- Naming follows the convention in Section 7: PascalCase for ordinary names, with an uppercase type prefix and one underscore for project assets that use a prefix.
- Decisions and requirement changes discussed during implementation must be added to this plan. Unresolved choices should be marked explicitly rather than silently treated as final.

### Implementation-chat workflow

The following workflow is required for future implementation chats:

1. Begin by reading the current checkpoint and handoff in this plan, then inspect the saved project assets before proposing work. Do not repeat a checkpoint that is already recorded as complete.
2. Present each implementation checkpoint as a detailed, numbered, step-by-step guide suitable for following directly in the Unity Editor and code editor.
3. Explain the purpose of every action, component, field, setting, and code responsibility so the user understands both what to do and why it is needed.
4. Include exact project paths, object names, Inspector values, code, and verification steps wherever they are relevant.
5. Keep checkpoints narrow. Clearly identify deferred behavior so later systems are not added prematurely.
6. Do not mark a checkpoint complete until the saved project state has been inspected and the user has confirmed that its acceptance tests work.
7. After each confirmed checkpoint, update this plan with the date, implemented files and scene changes, test results, important decisions, and any remaining limitations.
8. Before ending or moving to a new chat, replace the handoff with the next concrete objective, its planned actions, acceptance criteria, and explicitly deferred work.

Progress recorded on 2026-08-06:

- Continuous movement through the graybox room works in the XR Interaction Simulator.
- The simulator controls currently feel awkward but are usable; this is not treated as a locomotion defect. Familiarity and later headset testing are still required.
- The generic potion bottle prefab is the next implementation step. It should be created before Rigidbody and grab-interaction configuration is repeated across multiple bottles.
- The first bottle prefab now has the intended `PotionBottle` root and `Visual` child hierarchy. Its asset must be renamed from `PotionBottle.prefab` to `PF_PotionBottle.prefab` to follow the project convention.

Current bottle-interaction checkpoint:

1. In Prefab Mode, keep the `PotionBottle` root at local position `(0, 0, 0)`, rotation `(0, 0, 0)`, and scale `(1, 1, 1)`.
2. Add a Rigidbody to the `PotionBottle` prefab root with mass `0.25`, gravity enabled, interpolation enabled, and Continuous Dynamic collision detection.
3. Add an XR Grab Interactable to the same root and use Velocity Tracking movement for the initial physics-friendly setup.
4. Assign the `Visual` child's Capsule Collider to the interactable's Colliders list.
5. Verify in the XR Interaction Simulator that a bottle can be grabbed, moved, released, dropped onto the shelf or floor, and grabbed again.
6. Confirm that it does not pass through the floor, does not behave explosively, and produces no Console errors.
7. Treat small hand-position or grab-pose issues as tuning work; do not create custom grab code at this stage.

Checkpoint result recorded on 2026-08-06:

- `PF_PotionBottle` has a Rigidbody and XR Grab Interactable configured on its root.
- The bottle can be grabbed, moved, released, dropped, and grabbed again with the XR Interaction Simulator.
- Gravity and collision behave as expected, and the completed test produced no reported blockers.
- This interaction checkpoint is complete. Do not repeat or redesign it at the start of the next session.

### Completed checkpoint: data-driven potion identity and runtime state

**Objective:** Give potion prefab instances data-driven identity, health, and fill values while keeping grabbing independent from game rules.

Planned work:

1. Create `Assets/WizzardsCauldron/Data/Potions/`.
2. Create `Assets/WizzardsCauldron/Scripts/Core/PotionDefinition.cs` as a ScriptableObject containing a stable ID, display name, health value, fill value, and presentation color or material reference.
3. Expose read-only public properties while keeping serialized fields private using the `_camelCase` convention.
4. Add validation attributes for non-negative health and positive fill values; avoid puzzle or cauldron logic in this data class.
5. Create `Assets/WizzardsCauldron/Scripts/Core/PotionController.cs` with a serialized `PotionDefinition` reference and the minimal `Available`, `Used`, and `Locked` runtime states documented in Section 8.
6. Add `PotionController` to `PF_PotionBottle`. Do not add cauldron acceptance, reset coordination, UI, or cap behavior yet.
7. Create initial data assets such as `SO_PotionRed`, `SO_PotionGreen`, and `SO_PotionYellow`, using provisional values until the final puzzle dataset is approved.
8. Assign a different potion definition to each prefab instance in `SCN_InteractionTest`. Instance-specific data assignments are expected prefab overrides.

Acceptance criteria:

- All scripts compile without Console errors or warnings introduced by this work.
- One reusable `PF_PotionBottle` still supplies all physical and grab behavior.
- Each scene instance can reference different potion data without separate bottle scripts or duplicated prefabs.
- Entering Play Mode preserves the verified grab, drop, collision, and re-grab behavior.
- Runtime potion state exists but does not yet change through cauldron interaction.

Explicitly deferred until later:

- Cauldron intake and scoring.
- Potion acceptance and rejection feedback.
- Reset coordination.
- Potion inspection UI.
- Final materials and models.
- Removable bottle caps.

Checkpoint result recorded on 2026-08-07:

- Created `Assets/WizzardsCauldron/Data/Potions/` and `Assets/WizzardsCauldron/Scripts/Core/`.
- Implemented `PotionDefinition` as a ScriptableObject with a stable ID, display name, non-negative health value, positive fill value, and presentation color.
- Kept serialized fields private and exposed read-only public properties. Inspector validation and `OnValidate` clamping prevent negative health and fill values below one.
- Implemented `PotionController` with `Available`, `Used`, and `Locked` runtime states plus methods to mark a potion used, lock it, and reset it.
- Added `PotionController` to the root of `PF_PotionBottle` while leaving the prefab's definition reference empty so instances can supply their own data.
- Created `SO_PotionRed`, `SO_PotionGreen`, and `SO_PotionYellow` with the provisional values from the introductory dataset.
- Assigned the matching definition to the `PotionRed`, `PotionGreen`, and `PotionYellow` instances in `SCN_InteractionTest` as intentional prefab overrides.
- Verified in Play Mode that the instances begin in the `Available` state and retain the previously validated grab, move, release, collision, drop, and re-grab behavior without new Console problems.
- This checkpoint is complete. Do not duplicate the generic bottle prefab or move potion values into `PotionController`.

### Completed checkpoint: reusable puzzle configuration

**Objective:** Define a reusable puzzle configuration before implementing cauldron rules.

Planned work:

1. Create `Assets/WizzardsCauldron/Data/Puzzles/`.
2. Create `Assets/WizzardsCauldron/Scripts/Core/PuzzleDefinition.cs` as a ScriptableObject containing a non-negative cauldron capacity and an ordered list of `PotionDefinition` references.
3. Expose the capacity and potion collection through read-only public properties while keeping serialized fields private.
4. Add lightweight validation for negative capacity, missing potion references, and duplicate stable IDs without adding cauldron or solver behavior to the data class.
5. Create `SO_PuzzleIntro` with the current Red, Green, and Yellow definitions and a provisional capacity of `4`.
6. Treat this three-potion asset as a technical test puzzle: Green plus Yellow produces the provisional best health of `5`, while attempting all three will later exercise over-capacity rejection.

Acceptance criteria:

- All scripts compile without Console errors or warnings introduced by this work.
- The puzzle asset owns configuration only and has no dependency on XR interaction or scene objects.
- Reordering or replacing potion references requires no code changes.
- `SO_PuzzleIntro` references the existing potion assets instead of copying their health and fill values.
- The capacity remains editable in the Inspector and cannot be negative.

Explicitly deferred until the following checkpoint:

- `CauldronController` and potion acceptance rules.
- Duplicate-submission and capacity rejection logic.
- Automatic optimal-score calculation.
- Spawning bottles from a puzzle definition.
- Final six-potion dataset and final balancing.

Checkpoint result recorded on 2026-08-07:

- Created `Assets/WizzardsCauldron/Data/Puzzles/` and implemented `PuzzleDefinition` as a ScriptableObject.
- `PuzzleDefinition` stores a non-negative cauldron capacity and an ordered, read-only collection of potion-definition references.
- Added validation for negative capacity, missing potion references, empty stable IDs, and duplicate stable IDs without adding scene or XR dependencies.
- Created `SO_PuzzleIntro` with capacity `4` and the Red, Green, and Yellow potion definitions in that order.
- Confirmed that the puzzle references the existing potion assets instead of duplicating their health and fill values.
- The three-potion asset is a technical test puzzle. Green plus Yellow is the provisional optimum with health `5` and fill `4`.

### Completed checkpoint: cauldron rule layer

Checkpoint result recorded on 2026-08-07:

- Added `PotionAcceptanceResult` values for accepted, invalid, out-of-puzzle, already-used, too-full, and game-finished outcomes.
- Implemented `CauldronController` with authoritative maximum capacity, used capacity, remaining capacity, total health, and locked state.
- Potion validation occurs before mutation, so rejected potions do not change totals or potion state.
- Accepted stable IDs are tracked case-insensitively to protect against duplicate scoring.
- Successful acceptance marks the corresponding `PotionController` as used and emits `TotalsChanged` for later UI integration.
- Added reset, lock, and puzzle-configuration entry points while leaving cross-object reset coordination for a later checkpoint.
- Renamed the scene placeholder from `Caulron` to `Cauldron`, added `CauldronController`, and assigned `SO_PuzzleIntro`.
- Verified that Play Mode initializes used capacity and total health to `0`, remaining capacity to `4`, and the locked state to false without disturbing bottle interaction.

### Completed checkpoint: physical cauldron intake

**Objective:** Connect physical bottle movement to the cauldron rules through one dedicated intake trigger.

Planned work:

1. Create `Assets/WizzardsCauldron/Scripts/Interactions/`.
2. Add a child named `IntakeTrigger` above the opening of the graybox `Cauldron` and give it a trigger collider separate from the cauldron's physical collider.
3. Implement `CauldronIntake` as a thin interaction adapter that finds `PotionController` on an entering collider's parent hierarchy and calls `CauldronController.TryAccept`.
4. Debounce each potion while it remains inside the intake so multiple physics callbacks cannot produce repeated processing.
5. Expose a processed-result event for later presentation code and add temporary result-specific Console output for this checkpoint.
6. Test accepted, already-used, and too-full outcomes in separate Play Mode attempts using the XR Interaction Simulator.

Acceptance criteria:

- The cauldron's solid collider remains non-trigger and continues to block dropped objects.
- Only the child intake volume requests potion acceptance.
- Non-potion colliders are ignored without errors.
- A fitting, available potion updates totals once and changes to `Used`.
- Re-entering with a used potion returns `AlreadyUsed` without changing totals.
- A potion that exceeds remaining capacity returns `TooFull` without changing totals or potion state.
- Expected rejections use ordinary debug feedback rather than Console warnings or errors.
- Existing grab, release, collision, and re-grab behavior remains intact.

Explicitly deferred until after the intake checkpoint:

- Polished world-space UI, audio, particles, and liquid visuals.
- Hiding or emptying accepted bottles.
- Finish-wand interaction and game-session locking.
- Complete room reset coordination.
- Automatic optimal-score calculation.

Checkpoint result recorded on 2026-08-07:

- Created `Assets/WizzardsCauldron/Scripts/Interactions/` and implemented `CauldronIntake` as a thin adapter between Unity trigger events and `CauldronController.TryAccept`.
- Added an `IntakeTrigger` child to the scene `Cauldron`, using a dedicated Box Collider marked as a trigger while leaving the cauldron's solid Capsule Collider non-trigger.
- The intake locates `PotionController` through an entering collider's parent hierarchy, so the collider may remain on the bottle's `Visual` child.
- Added per-potion in-volume debouncing to prevent repeated processing from multiple trigger callbacks while a bottle remains inside.
- Added a `PotionProcessed` event for later presentation code and temporary result-specific Console messages for the current debug phase.
- Non-potion colliders are ignored and expected gameplay rejections are reported as ordinary debug messages rather than warnings or errors.
- Verified successful acceptance, exact capacity updates, duplicate rejection after re-entry, and over-capacity rejection in the XR Interaction Simulator.
- Verified that accepted potions change to `Used`, rejected too-full potions remain `Available`, and rejected attempts do not alter totals.
- Existing grab, release, collision, drop, and re-grab behavior remains functional.

### Completed checkpoint: world-space cauldron status panel

**Objective:** Replace Console-only cauldron feedback with a small, readable world-space status panel while keeping all puzzle decisions in the existing core controllers.

Planned work:

1. Create `Assets/WizzardsCauldron/Scripts/UI/`.
2. Implement `CauldronStatusPanel.cs` in the `WizzardsCauldron.UI` namespace with serialized references to `CauldronController`, `CauldronIntake`, and TextMesh Pro text fields.
3. Subscribe to `CauldronController.TotalsChanged` and `CauldronIntake.PotionProcessed` in `OnEnable`, and unsubscribe in `OnDisable`. Do not poll from `Update`.
4. Add a non-interactive World Space Canvas near the graybox cauldron with a high-contrast panel and separate text fields for total health, remaining capacity, and the latest acceptance result.
5. Initialize the panel from the controller's current values so it displays `Health: 0` and `Capacity: 4 / 4` before any potion enters.
6. Map the current acceptance results to short player-facing messages such as `Red Potion accepted`, `Not enough capacity`, and `Potion already used`.
7. Keep the existing Console messages during this checkpoint as secondary diagnostic output; remove or gate them only after visual feedback is verified.
8. Test the panel through the same Red/Green/Yellow sequences used for the intake checkpoint.

Acceptance criteria:

- The panel is readable in the XR Interaction Simulator from a comfortable position near the cauldron and does not sit close to the camera.
- Before any selection, it displays total health `0` and remaining/maximum capacity `4 / 4`.
- Accepting Red changes the display to health `1`, capacity `2 / 4`, and a short accepted message.
- Removing and re-entering with Red displays an already-used message without changing totals.
- Filling the cauldron and then entering with Yellow displays a not-enough-capacity message without changing totals.
- Starting a new Play Mode attempt restores the initial display.
- The UI reads existing state and events but does not calculate capacity, health, membership, or acceptance itself.
- No event subscription is duplicated after disabling and re-enabling the panel, and no new Console errors or warnings are introduced.

Explicitly deferred until after the status-panel checkpoint:

- Final fonts, frames, shaders, animation, audio, particles, and cauldron-liquid presentation.
- Potion inspection labels or hover UI.
- Hiding or emptying accepted bottles.
- Finish-wand interaction, result comparison, and game-session locking.
- Complete room reset coordination.
- Automatic optimal-score calculation and final six-potion balancing.

Checkpoint result recorded on 2026-08-10:

- Created `Assets/WizzardsCauldron/Scripts/UI/` and implemented `CauldronStatusPanel` in the `WizzardsCauldron.UI` namespace.
- The panel subscribes to `CauldronController.TotalsChanged` and `CauldronIntake.PotionProcessed` in `OnEnable`, unsubscribes in `OnDisable`, and performs no per-frame polling.
- Added the non-interactive `CauldronStatusCanvas` to `SCN_InteractionTest` with separate TextMesh Pro fields for health, remaining/maximum capacity, and the latest processing result.
- Assigned the saved scene references to the existing `CauldronController`, `CauldronIntake`, `HealthText`, `CapacityText`, and `MessageText` components.
- The Canvas uses a Y rotation of `180` so the text faces the playable side of the current graybox room; the initial opposite facing direction placed the text on the back-facing side.
- Verified the initial `Health: 0` and `Capacity: 4 / 4` display, accepted-potion updates, already-used feedback, insufficient-capacity feedback, and initial-state restoration on a new Play Mode attempt.
- Verified that rejected attempts do not change totals, disabling and re-enabling the panel does not duplicate subscriptions, and no new Console errors or warnings were introduced.

### Completed checkpoint: pure 0/1 knapsack solver

**Objective:** Implement and verify a pure C# 0/1 knapsack solver that calculates the best possible health for a configured capacity before adding finish-session or result-panel behavior.

Planned work:

1. Add a project runtime assembly definition so project-owned runtime code can be referenced by an Edit Mode test assembly without moving gameplay responsibilities.
2. Implement immutable, Unity-independent solver input and result types carrying stable ID, health, fill, maximum health, used capacity, and one optimal set of stable IDs.
3. Implement `PuzzleSolver` with a dynamic-programming 0/1 knapsack algorithm. Each item may be selected at most once, and the solver must not read scene objects or mutate potion definitions.
4. Define deterministic tie behavior: prefer greater health, then lower used capacity, then preserve the earlier discovered selection.
5. Add Edit Mode tests for the current three-potion technical puzzle, exact fit, an overweight item, empty input, zero capacity, and non-reuse of a single item.
6. Run all new Edit Mode tests and verify that ordinary Play Mode behavior and the status panel still compile and work after introducing assembly definitions.

Acceptance criteria:

- The solver has no `UnityEngine`, scene-object, XR, or UI dependencies.
- Red, Green, and Yellow at capacity `4` return maximum health `5`, used capacity `4`, and the Green plus Yellow stable IDs.
- A single potion can never be counted more than once.
- Items whose fill exceeds capacity are not selected.
- Empty input and zero capacity return a zero-valued empty solution.
- The input collection is not modified.
- All new Edit Mode tests pass and no new Console errors or warnings are introduced.
- Entering Play Mode preserves the verified bottle, intake, totals, rejection feedback, and status-panel behavior.

Explicitly deferred until after the solver checkpoint:

- Mapping `PuzzleDefinition` into solver inputs through `GameSessionController`.
- Finish-wand interaction and attempt locking.
- Result-panel presentation and optimal/valid outcome comparison.
- Cross-object reset coordination.
- Final six-potion dataset and balancing.

Checkpoint result recorded on 2026-08-10:

- Added the project runtime assembly `WizzardsCauldron.Runtime` with its required TextMesh Pro reference, allowing project-owned runtime code to be referenced from a separate Edit Mode test assembly.
- Implemented immutable `KnapsackItem` input data with stable ID, non-negative health, and positive fill validation.
- Implemented immutable `PuzzleSolution` result data containing maximum health, used capacity, and a read-only copy of one optimal stable-ID selection.
- Implemented `PuzzleSolver` as a Unity-independent dynamic-programming 0/1 knapsack solver. Descending capacity traversal prevents any item from being reused in one solution.
- Deterministic comparison prefers greater health, then lower used capacity, and otherwise preserves the earlier discovered selection.
- Added `WizzardsCauldron.EditModeTests` and eight Edit Mode tests covering the introductory Green-plus-Yellow optimum, exact fit, overweight rejection, empty input, zero capacity, single-item non-reuse, lower-capacity tie resolution, and input-order preservation.
- Confirmed that all eight solver tests pass and the existing Play Mode bottle, cauldron intake, totals, rejection feedback, and world-space status panel continue to work without new Console problems.
- The runtime assembly asset is currently saved as `WizzardCauldron.Runtime.asmdef`, while its internal assembly name is correctly `WizzardsCauldron.Runtime`; the working asset does not need to be renamed during the next checkpoint.

### Completed checkpoint: game session and immutable attempt results

**Objective:** Add the authoritative game-session and immutable attempt-result layer, connect the configured puzzle to the solver and cauldron, and verify finishing and locking through a temporary diagnostic command before adding the wand or result UI.

Planned work:

1. Define `GameSessionState` values for `Playing` and `Finished`, plus `AttemptOutcome` values for no selection, a valid non-optimal selection, and an optimal solution.
2. Add immutable `AttemptResult` data containing player health, used and maximum capacity, best possible health, outcome, and one optimal stable-ID selection.
3. Implement `GameSessionController` with serialized `PuzzleDefinition` and `CauldronController` references.
4. On session initialization, validate the puzzle, map its potion definitions into immutable `KnapsackItem` values, solve it once, configure the cauldron, and enter the `Playing` state.
5. Implement a single `TryFinishAttempt` entry point that locks the cauldron, creates the result, changes the session to `Finished`, and emits one `AttemptFinished` event.
6. Add a temporary Play Mode context-menu command for finishing and diagnostic result output; this is a test adapter, not the final wand interaction.
7. Add Edit Mode tests for result classification and manually verify no-selection, non-optimal, optimal, repeated-finish, and post-finish potion-rejection behavior.

Acceptance criteria:

- The session solves the current puzzle once at initialization and exposes optimal health `5` and optimal used capacity `4` for `SO_PuzzleIntro`.
- Finishing with no accepted potion produces `NoPotionsSelected` with player health `0`.
- Finishing after accepting only Red produces `ValidSolution` with player health `1` and best possible health `5`.
- Finishing after accepting Green and Yellow produces `OptimalSolution` with player health `5`.
- Finishing locks the cauldron; later intake attempts return `GameFinished` and do not change totals.
- Repeated finish requests do not emit another completion event or replace the first result.
- The solver and result data remain independent from XR and UI code.
- Existing status-panel and bottle behavior remains functional with no new Console errors or warnings.

Explicitly deferred until after the game-session checkpoint:

- Wand-tip and finish-target interaction.
- The world-space result panel.
- Cross-object reset coordination and a physical reset control.
- Potion inspection UI and presentation polish.
- Final six-potion dataset and balancing.

Checkpoint result recorded on 2026-08-10:

- Added `GameSessionState` with `Playing` and `Finished` states and `AttemptOutcome` with no-selection, valid non-optimal, and optimal outcomes.
- Implemented immutable `AttemptResult` data containing player health, used and maximum capacity, best possible health, outcome, and a read-only copy of one optimal stable-ID selection.
- Implemented `GameSessionController` with authoritative `PuzzleDefinition` and `CauldronController` references, one-time puzzle solving during initialization, and read-only access to the optimal solution and latest result.
- Added the scene-level `GameSession` object to `SCN_InteractionTest`, assigned `SO_PuzzleIntro` and the existing `Cauldron`, and verified the runtime optimum of health `5` using capacity `4`.
- `TryFinishAttempt` now locks the cauldron, snapshots one result, changes the state to `Finished`, and emits `AttemptFinished` only for the first successful finish request.
- Added a temporary `Finish Attempt (Play Mode)` context command for diagnostic testing without prematurely coupling the session to the wand or result UI.
- Added four `AttemptResultTests` for no selection, a valid non-optimal selection, an optimal selection, and the copied optimal stable-ID list. All twelve project-owned Edit Mode tests pass.
- Verified no-selection, Red-only, and Green-plus-Yellow finish outcomes; repeated finish requests are ignored, and post-finish intake attempts return `GameFinished` without changing totals.
- Confirmed the existing bottle interactions, cauldron status panel, rejection feedback, and Console remain in working order.

### Completed checkpoint: world-space attempt result panel

Checkpoint result recorded on 2026-08-12:

- Implemented `Assets/WizzardsCauldron/Scripts/UI/ResultPanel.cs` as an event-driven presentation adapter for `GameSessionController.AttemptFinished` and `LatestResult`.
- Added the active `ResultCanvas` and independently hideable `ResultContent` hierarchy to `SCN_InteractionTest`, with saved references for the outcome, health, capacity, and best-health TextMesh Pro fields.
- The content begins inactive, becomes visible only when an immutable `AttemptResult` is available, and restores a previously completed result after the panel is disabled and re-enabled.
- The UI maps the three existing `AttemptOutcome` values to player-facing headings and reads all numbers directly from `AttemptResult`; it does not invoke the solver or inspect potion definitions.
- Verified no-selection, Red-only, and Green-plus-Yellow displays with the expected `0 / 5`, `1 / 5`, and `5 / 5` health results and matching capacity values.
- Verified late-enable recovery, readable non-interactive world-space presentation, session locking, existing bottle/intake/status behavior, and no new Console errors or warnings.
- All twelve project-owned Edit Mode tests continue to pass.

### Completed checkpoint: physical wand finishing

Checkpoint result recorded on 2026-08-14:

- Created the normalized reusable `Assets/WizzardsCauldron/Prefabs/PF_Wand.prefab` with a root Rigidbody, XR Grab Interactable, solid `Visual` capsule collider, and separate `Tip` trigger.
- The Rigidbody uses mass `0.2`, gravity, interpolation, and Continuous Dynamic collision detection. The XR Grab Interactable uses the solid body collider rather than the tip trigger.
- Added the minimal `WandTip` marker under `Assets/WizzardsCauldron/Scripts/Interactions/` and assigned it to the prefab's trigger tip.
- Added `WandActivator` as a thin trigger adapter that accepts only a collider carrying `WandTip`, calls `GameSessionController.TryFinishAttempt`, listens for `AttemptFinished`, and disables the finish trigger after completion.
- Added the visible `FinishTarget` to `SCN_InteractionTest` at `(0.55, 1.20, 1.05)`, assigned its trigger collider, and wired it to the saved scene `GameSession`.
- The `PF_Wand` scene instance is saved under `Placeholders` on `WandTable` at `(0.85, 0.91, 0.95)` with Z rotation `90`.
- The user decided to retain `Finish Attempt (Play Mode)` on `GameSessionController` as an intentional developer diagnostic for possible future use. It is no longer scheduled for removal and is not part of the player-facing interaction loop.
- Structural inspection found the expected prefab components, collider roles, scene references, and context command. An external `dotnet build` could not complete because the sandbox account cannot write the generated `obj` directory; this was an environment-permission failure rather than a reported C# compiler error.
- Confirmed in the XR Interaction Simulator that the wand can be grabbed, moved, released onto the table or floor, and grabbed again without unstable physics.
- Confirmed that hands/controllers, potion bottles, unrelated colliders, and the solid wand body do not activate `FinishTarget`; only the marked `WandTip` finishes the attempt.
- Confirmed that the marked tip finishes exactly once, locks the cauldron, disables the finish trigger, and reveals the correct immutable result.
- Confirmed physical no-selection, Red-only, and Green-plus-Yellow outcomes with the expected `0 / 5`, `1 / 5`, and `5 / 5` health results and matching capacity values.
- Confirmed that potion intake after finishing is rejected without changing totals, all twelve project-owned Edit Mode tests pass, and no new Console errors or warnings were introduced.
- The physical wand-finishing checkpoint is complete. Retain `Finish Attempt (Play Mode)` as an intentional developer diagnostic.

### Completed checkpoint: cross-object room reset coordination

Checkpoint result recorded on 2026-08-14:

- Added `GameSessionController.TryResetSession` and the `SessionReset` event. Reset clears `LatestResult`, returns the existing solved session to `Playing`, resets and unlocks the cauldron, and does not rerun the solver or reload the scene.
- Added reusable `PhysicsResettable` behavior that records the initial parent, local transform, scale, and Rigidbody kinematic state; safely releases an assigned interaction behavior; restores the starting pose; and clears linear and angular velocity.
- Added `PhysicsResettable` to `PF_PotionBottle` and `PF_Wand`, with their root XR Grab Interactable components assigned as the optional interaction behaviors.
- Added `RoomResetCoordinator` under `Assets/WizzardsCauldron/Scripts/Interactions/` and normalized the source filename to match its MonoBehaviour class while preserving the Unity meta GUID and saved scene reference.
- Added the scene-level `RoomReset` object to `SCN_InteractionTest`, with explicit references to `GameSession`, `CauldronIntake`, all three potion controllers, all three bottle reset components, and the wand reset component.
- Added explicit cauldron-intake debounce clearing so a bottle teleported home during reset can be accepted again in the following attempt.
- Updated `ResultPanel` to hide, `WandActivator` to re-enable `FinishTarget`, and `CauldronStatusPanel` to restore `Add a potion` when `SessionReset` is emitted.
- Retained `Reset Room (Play Mode)` on `RoomResetCoordinator` as the developer test adapter for reset diagnostics.
- Confirmed reset during play and after finishing, exact bottle and wand pose restoration, cleared physics motion, available potion states, zero cauldron totals, hidden results, re-enabled finishing, repeated intake of previously used bottles, and multiple complete attempts without a scene reload.
- Confirmed reset while objects are moving or held, all twelve project-owned Edit Mode tests, and no new Console errors or warnings.

### Completed checkpoint: deliberate physical reset control

Checkpoint result recorded on 2026-08-15:

- Imported `Assets/WizzardsCauldron/Models/reset_button_1.fbx` and used it as the presentation mesh for the project-owned `PF_ResetControl` prefab without adding gameplay behavior to the source model.
- Created `PF_ResetControl` with a separate interaction collider, XR Simple Interactable, world-space status text, and `ResetHoldControl` presentation adapter.
- `ResetHoldControl` requires a one-second continuous hold, reports percentage progress, cancels cleanly on early release, blocks repeated resets until selection is released, and delegates the actual reset exclusively to `RoomResetCoordinator.TryResetRoom`.
- Saved the `ResetControl` prefab instance under `Placeholders` in `SCN_InteractionTest` and retained its intentional scene override referencing the existing `RoomResetCoordinator`.
- Corrected the prefab event wiring so XR Simple Interactable `Select Entered` invokes `BeginHold` and `Select Exited` invokes `CancelHold`.
- Confirmed controller interaction, early-release cancellation, successful reset during play and after finishing, one reset per continuous hold, return to idle feedback on release, and continued operation of the developer context-menu reset adapter.
- Confirmed that reset continues to restore potion and wand poses, potion availability, zero cauldron totals, the initial status prompt, hidden result content, and an enabled finish target without reloading the scene.

### Completed checkpoint: ordinary-hover potion inspection

Checkpoint result recorded on 2026-08-15:

- Added reusable `PotionInspectionSource` to `PF_PotionBottle`, reading identity and values exclusively through the existing `PotionController` and assigned `PotionDefinition`.
- Wired XR Grab Interactable `First Hover Entered` to `BeginInspection` and `Last Hover Exited` to `EndInspection`, avoiding premature hide behavior when multiple interactors hover one bottle.
- Added the active scene-level `PotionInspectionCanvas` with independently hideable `InspectionContent` and dedicated name, health, and fill TextMesh Pro fields.
- Implemented `PotionInspectionPanel` as an event-driven presentation adapter subscribed to the Red, Green, and Yellow scene sources.
- The panel displays the most recently hovered source and falls back to another still-hovered potion when a second interactor leaves; it hides when no source remains hovered.
- Confirmed Red, Green, and Yellow display their definition-owned names and the expected `1/2`, `2/2`, and `3/2` health/fill values.
- Confirmed ray hover without selection, direct hover, stable transitions, two-controller fallback, unchanged grab/release behavior, reset compatibility, readable placement, all twelve Edit Mode tests, and no new Console errors or warnings.
- Wand-only inspection remains optional; ordinary hover is the required discoverable baseline.

### Completed checkpoint: capacity-driven cauldron liquid display

Checkpoint result recorded on 2026-08-15:

- Created the opaque emissive `Assets/WizzardsCauldron/Materials/MAT_CauldronLiquid.mat` placeholder material.
- Added the collider-free `LiquidVisual` cylinder under the scene `Cauldron`, initially inactive with local scale `(0.8, 0.01, 0.8)`.
- Created `Assets/WizzardsCauldron/Scripts/Presentation/` and implemented `CauldronLiquidDisplay` as an event-driven presentation component subscribed to `CauldronController.TotalsChanged`.
- The display reads authoritative used and maximum capacity, hides at zero, maps the fill ratio to configured local height, and keeps the cylinder's lower edge fixed. It never changes totals, potion state, session state, or intake decisions.
- Confirmed no liquid at zero capacity, half-full presentation after one current two-fill potion, and full presentation after two accepted potions.
- Confirmed duplicate, too-full, and post-finish rejections leave the liquid unchanged; physical reset hides it; and the next attempt fills it normally.
- Confirmed the liquid behavior works without new gameplay or reset regressions and remains suitable for handoff.
- Known presentation limitation: the current cauldron is an opaque solid capsule rather than a hollow final model, so the temporary purple cylinder visibly protrudes from the placeholder. This is accepted graybox behavior, not the intended final appearance.
- Future art integration should replace or reshape only the cauldron visual and liquid presentation geometry/material, then retune `_bottomLocalY`, `_emptyScaleY`, and `_fullScaleY`; it must preserve `CauldronController`, `IntakeTrigger`, `CauldronLiquidDisplay`, and their existing references and responsibilities.

### Handoff: next implementation owner

**Next objective:** Integrate the final or improved cauldron presentation and add lightweight acceptance/rejection effects without changing the verified gameplay loop.

Handoff requirements:

1. Replace the graybox capsule appearance through child visual geometry or a project-owned cauldron prefab; do not remove or relocate the authoritative `CauldronController` without updating and retesting every saved reference.
2. Preserve a non-trigger physical collider for the cauldron body and the separate `IntakeTrigger` trigger used by `CauldronIntake`.
3. Fit the existing `LiquidVisual` or a replacement collider-free liquid mesh to the visible basin and retune only `CauldronLiquidDisplay` presentation values.
4. Keep liquid state driven by `CauldronController.TotalsChanged` so rejected attempts and reset remain correct automatically.
5. If adding splash particles or audio, implement a thin presentation listener for `CauldronIntake.PotionProcessed`; accepted and rejected effects must remain result-specific and must not mutate gameplay state.
6. Re-run half/full/rejection/finish/reset checks, the full interaction loop, and all twelve Edit Mode tests after replacing visuals.

Explicitly deferred:

- Fluid simulation, pouring streams, dynamic ripples, and complex transparent shaders.
- Mandatory color mixing or per-potion liquid blending.
- Empty-bottle or hidden-content presentation after acceptance.
- Final UI, reset-control, wand, and finish-target art.
- Showing display names for one optimal potion combination.
- Final six-potion dataset and balancing.

### Headset-free test workflow

1. In Unity, open **Window > Package Manager** and select **XR Interaction Toolkit**.
2. Under **Samples**, import **XR Interaction Simulator**. Use the new simulator, not the legacy XR Device Simulator.
3. Drag the imported `XR Interaction Simulator` prefab into `SCN_InteractionTest`, or enable **Use XR Interaction Simulator in scenes** under **Edit > Project Settings > XR Plug-in Management > XR Interaction Toolkit**.
4. Use the existing `XR Origin (XR Rig)`, which already contains Move, Turn, Teleportation, and Character Controller support.
5. Enter Play Mode. Use the simulator's on-screen controls; `Tab` switches between FPS and device modes, and `[` / `]` activate the simulated controllers.
6. Test movement, snap turning, wall collision, controller positioning, grabbing, dropping, and UI interaction in the simulator.
7. Treat comfort, physical reach, controller feel, tracking, performance, and device-specific input as requiring later headset validation.

## 3. Project Goals

### Core goals

1. Create a comfortable room-scale VR experience with controller locomotion that can be understood without a long tutorial.
2. Let the player inspect potion health and fill values.
3. Let the player move around the room and grab bottles.
4. Accept an unused potion when it is moved to the cauldron.
5. Track total health and remaining cauldron capacity correctly.
6. Prevent invalid additions that exceed capacity.
7. Let the player finish the attempt with a wand.
8. Display a clear result and allow a complete reset.
9. Present the knapsack problem through a polished but small wizard-themed room.

### Learning goals

- Demonstrate that the locally best-looking potion is not always part of the optimal combination.
- Show the trade-off between value and capacity.
- Make it possible to replay the same problem and test another selection.
- Keep the rules visible enough that the player can reason about the problem in VR.

### Non-goals for the first release

- Realistic fluid simulation.
- Partial bottle pouring.
- Bottle-cap removal or complex pouring animations.
- A large environment or advanced locomotion mechanics such as climbing and jumping.
- Procedural room generation.
- Multiplayer.
- A large campaign or many levels.
- A full animated visualization of dynamic programming.
- Hand tracking as a release requirement.

## 4. Minimum Viable Product

The minimum viable product is complete when one player can:

1. Start in front of a shelf and cauldron.
2. View the capacity of the cauldron and the value/fill of each bottle.
3. Grab a potion bottle using a VR controller.
4. Place the bottle in the cauldron's intake area.
5. Receive immediate visual or audio feedback that the potion was accepted.
6. See the cauldron totals update.
7. Be prevented from adding a potion that does not fit.
8. Finish the mixture by using the wand on the cauldron.
9. See the achieved health, maximum possible health, and whether the solution was optimal.
10. Reset all bottles, totals, and UI to their initial state.

The potion may disappear, empty instantly, or be replaced by a simple empty-bottle state after acceptance. A simulated liquid stream is not required.

## 5. Recommended Game Rules

### Potion rules

- Every potion is a unique 0/1 item and can be used at most once per attempt.
- A potion is accepted as one atomic action; partial contents are not supported.
- Once accepted, its full health and fill values are added.
- A used potion cannot be added again until the room is reset.

### Cauldron rules

- The cauldron has a fixed integer capacity for an attempt.
- A potion fits when:

  `current fill + potion fill <= maximum capacity`

- If it fits, the cauldron accepts it and updates its totals.
- If it does not fit, the cauldron rejects it without changing any totals.
- The cauldron UI displays:
  - Total health.
  - Remaining capacity.
  - Optionally, current fill as `used / maximum`.

### End rules

- The player activates the cauldron with the wand to finish.
- Finishing locks further potion additions until reset.
- The result screen displays:
  - Player health score.
  - Used and maximum capacity.
  - Best possible health score.
  - An outcome such as **Optimal solution**, **Valid solution**, or **No potions selected**.
- There is no conventional failure state. A non-optimal mixture is a completed learning attempt.

### Suggested first puzzle

Use five or six potions instead of only the three from the introductory example. The values should make the optimal combination non-obvious but still easy to verify.

| Potion | Health | Fill |
|---|---:|---:|
| Red | 1 | 2 |
| Green | 2 | 2 |
| Yellow | 3 | 2 |
| Blue | 5 | 4 |
| Purple | 6 | 5 |
| Orange | 4 | 3 |

Suggested cauldron capacity: **8 units**.

These values are placeholders and must be playtested. Store them as editable data rather than hard-coding them into bottle logic.

## 6. Team Responsibilities

The team has two programmers/game builders and one interactable-object designer. Each feature should have one clear owner, but team members should review each other's work at phase gates.

### Team Member A — Gameplay and systems programmer

Primary responsibilities:

- Potion data and state.
- Cauldron capacity and score logic.
- Potion acceptance/rejection.
- End-game evaluation.
- Optimal-score calculation.
- Reset system.
- Edit Mode tests for pure game logic.

Secondary responsibilities:

- Support prefab integration.
- Review merge requests or commits affecting game state.
- Maintain the central scene and build settings.

### Team Member B — VR interaction and scene programmer

Primary responsibilities:

- XR Origin and controller setup.
- Direct grab and ray interaction.
- Continuous movement, snap turning, and optional teleportation.
- Editor-based XR simulation setup.
- Cauldron intake trigger.
- Wand interaction.
- World-space UI behavior.
- Audio/visual interaction feedback.
- Device builds and VR playtesting.

Secondary responsibilities:

- Graybox room layout.
- Performance and comfort checks.
- Play Mode integration tests.

### Team Member C — Interactable-object and environment designer

Primary responsibilities:

- Bottle, cauldron, wand, shelf, and reset-control assets.
- An optional cap asset only if cap removal is later restored to scope.
- Colliders, grab points, pivots, scale, and readable silhouettes.
- Materials and color language for potions.
- Wizard-room dressing and lighting support.
- Simple particles, icons, and UI visual assets if time allows.

Secondary responsibilities:

- Test physical reach, visibility, and object placement in the headset.
- Create optimized variants or collision meshes where needed.
- Maintain consistent units and prefab-ready exports.

### Shared responsibilities

- Agree on prefab interfaces before asset production.
- Use the XR Interaction Simulator continuously and perform headset tests at major stable milestones when hardware is available.
- Keep optional features out of the core branch until the MVP is stable.
- Record bugs with reproduction steps, expected result, and owner.
- Ensure at least one other team member can explain each major system.

## 7. Collaboration and Asset Conventions

### Recommended folders

```text
Assets/
  WizzardsCauldron/
    Art/
      Materials/
      Models/
      Textures/
      VFX/
    Audio/
    Data/
      Potions/
      Puzzles/
    Prefabs/
      Environment/
      Interactables/
      UI/
    Scenes/
    Scripts/
      Core/
      Interactions/
      UI/
      Tests/
```

Do not modify sample-package content directly. Copy useful sample prefabs or scripts into the project's own folder before adapting them.

### Naming conventions

- Folders and ordinary GameObjects: PascalCase without separators, for example `PotionShelf`, `PotionRed`, and `Environment`.
- Project assets with a type prefix: uppercase prefix, one underscore, then PascalCase.
- Scenes: `SCN_Main`, `SCN_InteractionTest`.
- Prefabs: `PF_PotionBottle`, `PF_Cauldron`, `PF_Wand`.
- ScriptableObjects: `SO_PotionRed`, `SO_PuzzleIntro`.
- Materials: `MAT_PotionRed`, `MAT_BottleGlass`.
- Scripts, C# types, methods, and properties: PascalCase, for example `PotionDefinition`.
- Private serialized fields: `_camelCase`

The underscore after `SCN`, `PF`, `SO`, or `MAT` separates an asset-type prefix; it is not snake_case. Avoid mixed names such as `Potion_Red` for ordinary GameObjects.

### Prefab strategy

- Create `PF_PotionBottle` as the first gameplay prefab as soon as one placeholder bottle has the correct scale, collider, Rigidbody, and grab behavior.
- Use instances of this generic prefab for all potions. Assign a `PotionDefinition` and material to make each instance distinct.
- Do not create separate red, green, and yellow bottle prefabs unless their geometry or component structure genuinely differs.
- Convert the cauldron, wand, shelf, and reset control into prefabs after each placeholder's basic structure is stable.
- Keep room walls and floors directly in the graybox scene until a reusable room kit is actually useful.

### Version-control rules

- Commit small, coherent changes.
- Avoid having multiple team members edit the main scene at the same time.
- Build reusable content as prefabs so scene conflicts are minimized.
- Assign one person to integrate scene changes during each phase.
- Do not commit `Library`, `Temp`, `Logs`, `obj`, or local IDE settings.
- Create a playable checkpoint tag or branch at each phase gate.

## 8. Proposed System Design

Keep game rules independent of VR interaction so they can be tested without a headset.

### Data

`PotionDefinition` ScriptableObject:

- Stable ID.
- Display name.
- Health value.
- Fill value.
- Potion color/material reference.
- Optional description or icon.

`PuzzleDefinition` ScriptableObject:

- Cauldron capacity.
- List of potion definitions or potion spawn entries.
- Optional expected optimal health.
- Optional explanation text.

### Runtime state

`PotionController`:

- References a `PotionDefinition`.
- Tracks `Available`, `Used`, or `Locked` state.
- Exposes events when inspected, accepted, or reset.
- Controls bottle presentation but does not calculate the total score.

`CauldronController`:

- Owns maximum capacity, used capacity, and total health.
- Validates whether a potion fits.
- Accepts each potion ID only once.
- Returns a clear result: accepted, too full, already used, or game finished.
- Emits an event whenever totals change.

`GameSessionController`:

- Starts and ends an attempt.
- Tracks `Playing` and `Finished` states.
- Calculates or reads the optimal score.
- Produces result data for the result UI.
- Coordinates reset across all resettable objects.

`PuzzleSolver`:

- Pure C# implementation of a small 0/1 knapsack solver.
- Takes capacity and potion values as input.
- Returns maximum health and optionally the IDs of one optimal selection.
- Has no Unity scene dependencies.

### Interaction adapters

`PotionGrabInteractable`:

- Uses `XRGrabInteractable`.
- Notifies the potion controller of grab/release events if required.

`CauldronIntake`:

- Uses a trigger volume near the cauldron opening.
- Attempts acceptance only when a valid potion enters.
- Includes a short per-potion debounce to avoid repeated trigger calls.
- Shows rejection feedback without modifying game state.

`WandActivator`:

- Recognizes a wand tip entering/selecting the cauldron finish target.
- Requests confirmation only if playtests show accidental finishes are common.

### UI

`PotionInfoPanel`:

- Displays name, health, and fill.
- Appears on hover/focus and hides when focus ends.
- Faces the player or is attached at a readable fixed angle.

`CauldronStatusPanel`:

- Displays total health and remaining capacity.
- Updates from cauldron events rather than polling every frame.

`ResultPanel`:

- Displays attempt summary and reset option.
- May also display one optimal combination after the attempt.

`FeedbackPresenter`:

- Maps results such as `TooFull`, `AlreadyUsed`, or `GameFinished` to short text, sound, color, or particles.

### Reset

Create an `IResettable` contract or a small registered list of resettable components. Each object restores its own known initial state:

- Bottle transform, velocity, and angular velocity.
- Potion availability and visual contents.
- Cauldron totals and liquid appearance.
- Wand transform if it can be dropped.
- Result and information panels.

Store initial transforms when the scene starts or provide explicit reset anchors. Disable physics briefly while teleporting objects back to prevent explosive collisions.

## 9. Interaction and Accessibility Guidelines

- Support continuous controller movement and snap turning through the room.
- Keep teleportation available as an accessibility option if it remains reliable.
- Use a Character Controller or XRI body collision so the player cannot move through walls and large furniture.
- Keep required objects comfortably reachable after the player walks up to them.
- Use generous colliders and grab volumes.
- Avoid requiring precise rotation or hand poses.
- Make potion colors visually distinct, but also use names, symbols, or labels so color is not the only identifier.
- Use large, high-contrast world-space text.
- Avoid UI very close to the player's face.
- Provide both visual and audio feedback for accepted and rejected potions.
- Do not cause camera motion, forced head motion, or sudden large flashes.
- Ensure interactions work when the player is seated if the class requirements permit it.

## 10. Phased Implementation

The schedule below assumes approximately six working weeks. If the class schedule is shorter, Phases 0–3 form the MVP and later phases should be reduced.

## Phase 0 — Pre-production and Scope Lock

**Estimated time:** 2–3 days  
**Goal:** Agree on exactly what will be built before creating polished assets.

### Tasks

- Confirm target headset, controller type, and deployment platform.
- Confirm whether the class demonstration will use a tethered PC build or standalone headset build.
- Record that routine development currently occurs without headset access and schedule hardware validation at stable milestones.
- Write a one-sentence core loop:
  - Inspect.
  - Choose.
  - Add.
  - Finish.
  - Review.
  - Reset.
- Mark features as:
  - Must have.
  - Should have.
  - Optional.
  - Out of scope.
- Agree on the first puzzle capacity and potion values.
- Sketch the room from above, including player position, shelf, cauldron, wand, and reset control.
- Keep bottle-cap removal outside the MVP unless the requirement changes again.
- Decide what happens visually to an accepted potion.
- Define simple feedback for each invalid action.
- Assign the main scene integrator.
- Create the project folder structure and a small interaction test scene.

### Designer deliverables

- Rough scale sheet for the bottle, wand, cauldron, and shelf.
- Blockout meshes or primitive-based placeholders.
- Visual direction board limited to a small set of colors and materials.

### Programmer deliverables

- Confirmed XR rig and input strategy.
- Data and component interface notes.
- Testable list of game rules.

### Exit criteria

- All team members can describe the same MVP.
- The target device is known.
- The puzzle data is written down.
- The room and interaction flow are sketched.
- No optional feature is required for the first playable build.

## Phase 1 — VR Foundation and Graybox

**Estimated time:** Week 1  
**Goal:** Produce an editor-simulatable, headset-ready graybox where the player can move through the room and grab placeholder objects.

### Tasks

#### Team Member B

- Create `SCN_InteractionTest`.
- Set up the XR Origin, camera, controller input, and interaction manager.
- Enable continuous movement and snap turning on the existing XR Origin. Keep teleportation optional.
- Import and configure the XR Interaction Simulator sample for headset-free Play Mode testing.
- Enable direct grabbing; add ray interaction only where useful for UI.
- Verify tracking origin and player height.
- Create a player start position with enough clearance for the Character Controller.
- Add placeholder shelf, cauldron, wand, and table/reset control.
- Validate grabbing, dropping, collision, and object mass.

#### Team Member A

- Create the folder structure and initial assemblies if the team uses assembly definitions.
- Create placeholder potion data assets.
- Define basic runtime states and game-session interfaces.
- Prepare a small pure-C# rules test harness or Edit Mode test assembly.

#### Team Member C

- Export blockout versions of the required interactables at correct Unity scale.
- Establish pivots:
  - Bottle pivot near its center of mass.
  - Wand tip transform at the interaction end.
- Provide simple collision meshes.
- Test scale, reach, and readability in the simulator first and repeat the check in a headset at the next hardware milestone.

### Verification

- Player position is comfortable and correctly scaled.
- Continuous movement and snap turning work with simulated controller input.
- Walls and large furniture block player movement correctly.
- The important landmarks are visible from the start, and every required object is accessible after moving a short distance.
- Bottles can be picked up and released with either controller.
- Dropped objects do not fall through the room.
- The main scene can be launched directly into a playable VR state.

### Exit criteria

- A five-minute simulator session can be completed without input, locomotion, collision, or scale blockers.
- The same checks are repeated in a headset when one becomes available; headset-only issues are logged separately rather than blocking all editor work.
- The team approves the graybox layout before detailed art begins.

## Phase 2 — Core Knapsack Game Logic

**Estimated time:** Week 2  
**Goal:** Make the full puzzle playable with placeholder interactions and reliable rules.

### Tasks

#### Team Member A

- Implement `PotionDefinition` and `PuzzleDefinition`.
- Implement `PotionController` state transitions.
- Implement `CauldronController`.
- Reject potions that exceed remaining capacity.
- Reject duplicate potion submissions.
- Implement `PuzzleSolver` for maximum health.
- Implement `GameSessionController`.
- Implement an event-driven reset system.
- Write Edit Mode tests for:
  - Exact-fit acceptance.
  - Under-capacity acceptance.
  - Over-capacity rejection.
  - Duplicate rejection.
  - Correct health accumulation.
  - Correct remaining capacity.
  - Correct optimal result for known puzzle sets.
  - Empty puzzle and zero-capacity edge cases.

#### Team Member B

- Create the cauldron intake trigger and connect it to game logic.
- Add temporary debug text for accepted/rejected outcomes.
- Connect bottle objects to potion definitions.
- Build a temporary finish button or wand-trigger placeholder.
- Connect the reset control.

#### Team Member C

- Continue interactable models after the blockout is approved.
- Provide visually distinct placeholder materials for each potion.
- Test bottle collider, pivot, mass, and handling.

### Verification

- A complete attempt works in the Unity Editor.
- Totals always match accepted potions.
- Invalid potions never change totals.
- The solver produces the correct maximum for the configured puzzle.
- Reset can be performed repeatedly without duplicated values or missing objects.

### Exit criteria

- The full game loop works with debug visuals.
- Core rules pass automated tests.
- No physics event can add a potion twice.

## Phase 3 — Complete VR Interactions and UI

**Estimated time:** Week 3  
**Goal:** Replace debug controls with the intended bottle, cauldron, wand, and UI interactions.

### Bottle tasks

- Create one project-owned `PF_PotionBottle` prefab from the validated placeholder.
- Put the visual mesh, collider, Rigidbody, XR Grab Interactable, attach point, and potion controller on a consistent prefab hierarchy.
- Configure potion identity, values, and appearance through data rather than unique interaction scripts.
- Make the bottle grab pose comfortable.
- Treat a decorative cap as part of the bottle mesh unless removable caps return as an optional feature.

### Cauldron tasks

- Tune the intake volume to accept bottles near the opening.
- Require a potion bottle, not unrelated objects, to enter the volume.
- On acceptance:
  - Update totals once.
  - Play a splash sound and small particle effect.
  - Change the cauldron liquid level or color with a simple visual step.
  - Mark the bottle used and disable its potion contents.
- On rejection:
  - Show `Not enough space` or another result-specific message.
  - Play a short, distinct rejection sound.
  - Leave the bottle usable.

### Inspection UI tasks

- Show potion name, health, and fill while an XR interactor hovers over a bottle.
- Hide the panel when no interactor hovers.
- Prevent UI flicker when switching between the bottle and its child colliders.
- Show cauldron total health and remaining capacity on hover or through a small persistent panel.
- Use TextMeshPro world-space canvases with a comfortable size and distance.

### Wand and completion tasks

- Add a grab interactable to the wand.
- Add a wand-tip trigger or ray target.
- Define a clear finish target on the cauldron.
- Require a deliberate touch or select action to finish.
- Disable finishing interactions after the attempt is complete.
- Display the result panel.

### Reset tasks

- Add a large lever, button, rune, or other simple reset control.
- Allow reset during play and after results.
- If accidental resets occur, require a short hold or second confirmation.
- Restore every tracked object and hide the result panel.

### Verification

- A new player can discover the core actions with minimal instruction.
- Hover information is readable and stable.
- Cap removal succeeds consistently.
- Every accepted potion causes one and only one state update.
- The wand cannot finish the game accidentally during normal potion handling.
- Reset works both before and after finishing.

### Exit criteria

- The intended VR interaction loop is complete without debug buttons.
- Three consecutive full attempts can be played without restarting the scene.

## Phase 4 — Art, Theme, and Feedback

**Estimated time:** Week 4  
**Goal:** Turn the functional graybox into a coherent wizard-room presentation without increasing mechanical scope.

### Designer tasks

- Finalize low- or medium-detail models for:
  - Potion bottles.
  - Cauldron.
  - Wand.
  - Shelf.
  - Reset control.
- Create a small wizard-room kit:
  - Walls, floor, and ceiling.
  - Wooden or stone shelf/table.
  - A few books, candles, jars, or runes.
- Use environment props mostly as static decoration.
- Create consistent materials with restrained transparency and shader complexity.
- Provide collision meshes separately where detailed meshes are unsuitable.
- Ensure potion appearance remains distinguishable under scene lighting.

### Programmer tasks

- Replace placeholder objects prefab by prefab.
- Add baked lighting or another low-cost lighting setup.
- Add a cauldron glow and restrained particle effect.
- Add audio for grab, accept, reject, finish, and reset.
- Add hover and select affordances such as outline, emission, or subtle scale change.
- Ensure decorative objects do not block interaction targets.
- Profile after art integration.

### Content and UI polish

- Add a one-panel instruction card near the player start:
  1. Inspect potions.
  2. Add whole potions without exceeding capacity.
  3. Touch the cauldron with the wand to finish.
- Use icons alongside numeric labels where helpful.
- Keep feedback text short enough to read at a glance.
- Make the end result celebratory even when it is not optimal, then clearly show room for improvement.

### Exit criteria

- The room reads as a wizard workspace.
- The theme does not reduce UI contrast or object visibility.
- The build maintains the agreed target frame rate on the demonstration hardware.
- All placeholder interactables used in the core loop have been replaced or intentionally retained.

## Phase 5 — Testing, Balancing, and Stabilization

**Estimated time:** Week 5  
**Goal:** Make the game robust enough for an unfamiliar player and a classroom demonstration.

### Functional test matrix

| Test | Expected result |
|---|---|
| Add an unused potion that fits | Potion is accepted once; totals update |
| Add a potion that exceeds capacity | Rejected; totals remain unchanged |
| Add a potion that exactly fills capacity | Accepted; remaining capacity becomes zero |
| Re-enter intake with a used bottle | No second update |
| Drop a bottle on the floor | It remains recoverable or can be reset |
| Drop the wand | It remains recoverable or can be reset |
| Finish with no potions | Valid result with zero score |
| Finish with a valid non-optimal set | Correct score and comparison |
| Finish with an optimal set | Optimal outcome displayed |
| Try adding a potion after finishing | No change until reset |
| Reset during play | All objects and values return to start |
| Reset after finishing | Result closes and a fresh attempt starts |
| Reset repeatedly | No accumulated state or duplicate objects |
| Move into walls or large furniture | Character collision prevents passing through them |
| Move and snap-turn using simulated input | XR Origin responds without a headset |

### VR usability tests

Run at least three short tests with people who did not implement the feature.

Observe without immediately helping:

- Can the tester identify what to do?
- Do they understand the two potion numbers?
- Do they know the cauldron capacity?
- Do they try to pour partially?
- Can they read the UI comfortably?
- Do they know how to finish?
- Do they understand the result?
- Can they reset?
- Do they experience discomfort or awkward reach?

Record:

- Completion time.
- Number of verbal hints required.
- Failed interaction attempts.
- Incorrect assumptions.
- Comfort complaints.
- Bugs and severity.

### Puzzle balancing

- Ensure at least two combinations seem initially plausible.
- Avoid a puzzle where simply taking potions in descending health order is optimal.
- Keep the number of bottles between five and eight for the first version.
- Ensure all values are legible integers.
- Verify the optimal result both with automated tests and manual calculation.

### Stabilization rules

- Freeze new mandatory features at the start of this phase.
- Prioritize bugs in this order:
  1. Crashes, build failures, and lost tracking.
  2. Incorrect scoring or reset.
  3. Interaction blockers.
  4. Unreadable instructions or feedback.
  5. Visual polish.
- Test the actual packaged build, not only Play Mode.
- Prepare a known-good build before the final presentation day.

### Exit criteria

- All core functional tests pass.
- At least three external playtests are complete.
- A new player can finish with no more than one short hint.
- There are no known critical or high-severity bugs.
- The packaged build runs on the target hardware.

## Phase 6 — Optional Educational Features

**Estimated time:** Only after MVP stabilization  
**Goal:** Add educational depth without risking the final build.

Implement optional features one at a time and keep each removable.

### Option A — Wand-based inspection

Instead of showing potion information on any hover:

- The player points the wand at a potion.
- A raycast or XR ray hover identifies the potion.
- The information panel appears only while the wand targets it.

This reinforces the wand theme but adds discovery and input complexity. Keep ordinary hover inspection as the fallback until this version tests well.

### Option B — Text explanation after finishing

Add a short result explanation:

- **Brute force:** Try every possible subset and keep the best valid one.
- **Value density heuristic:** Compare health per fill unit, but explain that this does not always guarantee the best answer for whole items.
- **Dynamic programming:** Build the best result for smaller capacities and reuse those results.

Keep each explanation to a few sentences and reveal it after the result so it does not interrupt play.

### Option C — Show one optimal combination

Use the solver's selected item IDs to:

- Display potion icons or names in the result panel.
- Highlight the corresponding bottles after the attempt.
- Show `One optimal selection: Green + Yellow + ...`.

This is the recommended optional feature because it reuses the solver and needs little new interaction.

### Option D — Multiple puzzle presets

Create two or three `PuzzleDefinition` assets:

- Tutorial puzzle with three bottles.
- Standard puzzle with five or six bottles.
- Challenge puzzle with seven or eight bottles.

Use a simple menu or physical selector before the attempt. Do not add procedural generation unless all presets are already stable.

### Option E — Selection-process visualization

A step-by-step dynamic-programming table or animated choice tree is explicitly low priority. It has high UI and explanation cost in VR and should only be attempted if every other deliverable is complete.

## 11. Milestones and Suggested Schedule

| End of | Milestone | Required demonstration |
|---|---|---|
| Pre-production | Scope lock | Written MVP, puzzle, room sketch, device target |
| Week 1 | VR graybox | Move and grab bottles in the XR Interaction Simulator; repeat in a headset when available |
| Week 2 | Rules playable | Complete and reset a placeholder puzzle |
| Week 3 | Interaction-complete alpha | Move, add, inspect, and finish with the wand |
| Week 4 | Themed beta | Wizard room, final interactables, feedback |
| Week 5 | Release candidate | Tested device build with stable scoring/reset |
| Week 6, if available | Optional features and presentation | Educational enhancement plus final rehearsal |

If time is lost, cut in this order:

1. Animated solution visualization.
2. Multiple puzzles.
3. Wand-only inspection.
4. Decorative props and advanced effects.
5. Removable caps or pouring animation.

Never cut correct scoring, capacity validation, clear feedback, finish, or reset.

## 12. Definition of Done

A feature is done only when:

- It works in the XR Interaction Simulator during routine development.
- It receives headset validation before a release milestone when hardware is available; simulator testing does not replace final device testing.
- Its success and failure paths have clear feedback.
- It survives reset.
- It does not add console errors.
- It is represented by a prefab or documented scene setup.
- Its inspector references are assigned and validated.
- Another team member has tested it.
- Relevant automated or manual tests are recorded.
- It is included in a packaged build if it affects the final experience.

The project is done when:

- The player can complete the full core loop.
- Game totals are always correct.
- The best possible score is calculated correctly.
- Invalid additions are handled clearly.
- Reset produces a clean new attempt.
- The room meets the agreed visual theme.
- The final build runs reliably on the target headset.
- The team has a presentation build and a backup copy.

## 13. Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Simulator behavior differs from a physical headset | Late interaction or comfort issues | Use the simulator continuously, keep interactions generous, and reserve stable milestone builds for headset testing |
| Trigger adds one bottle multiple times | Score becomes incorrect | Track stable potion IDs and mark used before playing feedback |
| Transparent bottle materials are expensive or unclear | Poor performance/readability | Use simple shaders, opaque labels, and restrained transparency |
| Main scene merge conflicts | Lost integration time | Work through prefabs and assign a scene integrator |
| Team spends too long on fluid simulation | MVP remains incomplete | Use instant acceptance plus particles and a stepped liquid level |
| Player cannot infer the rules | Demo requires constant explanation | Add a four-step instruction panel and clear rejection messages |
| Player accidentally finishes or resets | Attempt is lost | Use deliberate wand contact and a hold/confirmation for reset if needed |
| Objects fall out of reach | Attempt becomes blocked | Add floor bounds and ensure reset is always reachable |
| Standalone headset performance drops | Discomfort or failed demo | Profile early on target hardware and bake/simplify visuals |
| Optional features destabilize the build | Final release regresses | Add them only after a tagged MVP build and keep them isolated |

## 14. Presentation Preparation

Before the class presentation:

- Prepare a 30–60 second explanation of the knapsack problem.
- Demonstrate one valid but non-optimal attempt, then an optimal attempt.
- Explain that whole bottles make this a 0/1 selection problem.
- Show the data-driven potion setup in Unity.
- Briefly identify how responsibilities were split across the team.
- Rehearse headset setup, recentering, audio, and controller pairing.
- Keep a prebuilt executable and a second backup copy.
- Capture a short gameplay video in case hardware is unavailable.
- Avoid making untested changes on presentation day.

## 15. Immediate Next Actions

1. Confirm the eventual target headset and build platform when that information becomes available.
2. Assign Team Members A, B, and C to the roles above.
3. Approve the MVP/non-goal split.
4. Choose the final capacity and first potion dataset.
5. Create the project-owned folder structure.
6. Use the existing XR setup in `SCN_InteractionTest`, enable continuous movement and snap turning, and verify collision.
7. Import the XR Interaction Simulator sample and establish a repeatable headset-free test workflow.
8. Convert one validated placeholder bottle into the generic `PF_PotionBottle` prefab.
9. Build one complete vertical slice with:
   - One bottle.
   - One cauldron intake.
   - One UI panel.
   - One reset control.
10. Validate the slice in the simulator throughout development and in the target headset at the first practical stable milestone.

This vertical slice should be the first implementation target. Once it is reliable, the team can multiply content and add presentation polish without redesigning the core interaction.
