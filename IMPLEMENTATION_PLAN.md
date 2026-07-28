# Wizzards Cauldron — Implementation Plan

## 1. Project Summary

**Wizzards Cauldron** is a small first-person VR game that presents the 0/1 knapsack problem through physical interaction. The player stands in a wizard-themed room, inspects potion bottles on a shelf, removes bottle caps, and pours selected potions into a cauldron.

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

## 3. Project Goals

### Core goals

1. Create a comfortable, stationary VR experience that can be understood without a long tutorial.
2. Let the player inspect potion health and fill values.
3. Let the player grab bottles and remove their caps.
4. Accept an uncapped potion when it is moved to the cauldron.
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
- Complex cap twisting or pouring animations.
- Free locomotion through a large environment.
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
4. Remove the cap with a simple interaction.
5. Place the open bottle in the cauldron's intake area.
6. Receive immediate visual or audio feedback that the potion was accepted.
7. See the cauldron totals update.
8. Be prevented from adding a potion that does not fit.
9. Finish the mixture by using the wand on the cauldron.
10. See the achieved health, maximum possible health, and whether the solution was optimal.
11. Reset all bottles, caps, totals, and UI to their initial state.

The potion may disappear, empty instantly, or be replaced by a simple empty-bottle state after acceptance. A simulated liquid stream is not required.

## 5. Recommended Game Rules

### Potion rules

- Every potion is a unique 0/1 item and can be used at most once per attempt.
- A potion may only be accepted while its cap is removed.
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
- Bottle-cap interaction.
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

- Bottle, cap, cauldron, wand, shelf, and reset-control assets.
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
- Perform headset tests at the end of every phase.
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

- Scenes: `SCN_Main`, `SCN_InteractionTest`
- Prefabs: `PF_Potion_Red`, `PF_Cauldron`, `PF_Wand`
- ScriptableObjects: `SO_Potion_Red`, `SO_Puzzle_Intro`
- Materials: `MAT_Potion_Red`, `MAT_BottleGlass`
- Scripts and C# types: PascalCase, for example `PotionDefinition`
- Private serialized fields: `_camelCase`

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
- Tracks `Capped`, `Uncapped`, `Used`, or `Locked` state.
- Exposes events when inspected, uncapped, accepted, or reset.
- Controls bottle/cap presentation but does not calculate the total score.

`CauldronController`:

- Owns maximum capacity, used capacity, and total health.
- Validates whether a potion fits.
- Accepts each potion ID only once.
- Returns a clear result: accepted, capped, too full, already used, or game finished.
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

`BottleCapInteractable`:

- Uses a simple grab, pull, socket removal, or select action.
- Changes the bottle to `Uncapped` once.
- Does not require threaded twisting physics.

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

- Maps results such as `TooFull` or `StillCapped` to short text, sound, color, or particles.

### Reset

Create an `IResettable` contract or a small registered list of resettable components. Each object restores its own known initial state:

- Bottle transform, velocity, and angular velocity.
- Cap transform and attachment.
- Potion availability and visual contents.
- Cauldron totals and liquid appearance.
- Wand transform if it can be dropped.
- Result and information panels.

Store initial transforms when the scene starts or provide explicit reset anchors. Disable physics briefly while teleporting objects back to prevent explosive collisions.

## 9. Interaction and Accessibility Guidelines

- Keep the experience stationary or room-scale; locomotion is not needed for the MVP.
- Place required objects within comfortable reach.
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
- Write a one-sentence core loop:
  - Inspect.
  - Choose.
  - Uncap.
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
- Decide how the cap is removed: recommended approach is direct grab/pull.
- Decide what happens visually to an accepted potion.
- Define simple feedback for each invalid action.
- Assign the main scene integrator.
- Create the project folder structure and a small interaction test scene.

### Designer deliverables

- Rough scale sheet for the bottle, cap, wand, cauldron, and shelf.
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
**Goal:** Produce a headset-ready graybox where the player can reach and grab placeholder objects.

### Tasks

#### Team Member B

- Create `SCN_InteractionTest`.
- Set up the XR Origin, camera, controller input, and interaction manager.
- Enable direct grabbing; add ray interaction only where useful for UI.
- Verify tracking origin and player height.
- Create a stationary player start position.
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
  - Cap pivot centered for clean grabbing.
  - Wand tip transform at the interaction end.
- Provide simple collision meshes.
- Test reach and readability in the headset with Team Member B.

### Verification

- Player position is comfortable and correctly scaled.
- Every required object is visible without walking.
- Bottles can be picked up and released with either controller.
- Dropped objects do not fall through the room.
- The main scene can be launched directly into a playable VR state.

### Exit criteria

- A five-minute headset session can be completed without tracking, input, or scale blockers.
- The team approves the graybox layout before detailed art begins.

## Phase 2 — Core Knapsack Game Logic

**Estimated time:** Week 2  
**Goal:** Make the full puzzle playable with placeholder interactions and reliable rules.

### Tasks

#### Team Member A

- Implement `PotionDefinition` and `PuzzleDefinition`.
- Implement `PotionController` state transitions.
- Implement `CauldronController`.
- Reject capped potions.
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
  - Capped-potion rejection.
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
- Test bottle/cap collider separation and handling.

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
**Goal:** Replace debug controls with the intended bottle, cap, cauldron, wand, and UI interactions.

### Bottle and cap tasks

- Add a separate grabbable cap to each bottle prefab.
- Use a snap point or constrained starting pose so the cap begins aligned.
- When the cap is pulled beyond a small threshold or removed from its socket:
  - Mark the potion as uncapped.
  - Play a short sound.
  - Disable the cap constraint.
- Do not require twisting unless it proves reliable and quick to implement.
- Make the bottle grab pose comfortable.
- Prevent the cap interaction from accidentally grabbing the bottle.

### Cauldron tasks

- Tune the intake volume to accept bottles near the opening.
- Require the bottle, not the detached cap, to enter the volume.
- On acceptance:
  - Update totals once.
  - Play a splash sound and small particle effect.
  - Change the cauldron liquid level or color with a simple visual step.
  - Mark the bottle used and disable its potion contents.
- On rejection:
  - Show `Remove the cap first` or `Not enough space`.
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
  - Removable caps.
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
- Add audio for grab, uncap, accept, reject, finish, and reset.
- Add hover and select affordances such as outline, emission, or subtle scale change.
- Ensure decorative objects do not block interaction targets.
- Profile after art integration.

### Content and UI polish

- Add a one-panel instruction card near the player start:
  1. Inspect potions.
  2. Remove a cap.
  3. Add whole potions without exceeding capacity.
  4. Touch the cauldron with the wand to finish.
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
| Add an uncapped potion that fits | Potion is accepted once; totals update |
| Add a capped potion | Rejected; message requests cap removal |
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

### VR usability tests

Run at least three short tests with people who did not implement the feature.

Observe without immediately helping:

- Can the tester identify what to do?
- Do they understand the two potion numbers?
- Do they know the cauldron capacity?
- Do they try to pour partially?
- Do they discover cap removal?
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
| Week 1 | VR graybox | Grab bottles in the headset |
| Week 2 | Rules playable | Complete and reset a placeholder puzzle |
| Week 3 | Interaction-complete alpha | Uncap, add, inspect, finish with wand |
| Week 4 | Themed beta | Wizard room, final interactables, feedback |
| Week 5 | Release candidate | Tested device build with stable scoring/reset |
| Week 6, if available | Optional features and presentation | Educational enhancement plus final rehearsal |

If time is lost, cut in this order:

1. Animated solution visualization.
2. Multiple puzzles.
3. Wand-only inspection.
4. Decorative props and advanced effects.
5. Complex cap or pouring animation.

Never cut correct scoring, capacity validation, clear feedback, finish, or reset.

## 12. Definition of Done

A feature is done only when:

- It works in the headset, not only with keyboard or mouse simulation.
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
| Cap physics is unreliable | Core action becomes frustrating | Use a simple grab-and-pull threshold or socket removal |
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

1. Confirm the target headset and build platform.
2. Assign Team Members A, B, and C to the roles above.
3. Approve the MVP/non-goal split.
4. Choose the final capacity and first potion dataset.
5. Create the project-owned folder structure.
6. Duplicate or adapt the existing XR setup into an interaction test scene.
7. Build one complete vertical slice with:
   - One bottle.
   - One removable cap.
   - One cauldron intake.
   - One UI panel.
   - One reset control.
8. Validate that slice in the target headset before producing the remaining bottles.

This vertical slice should be the first implementation target. Once it is reliable, the team can multiply content and add presentation polish without redesigning the core interaction.
