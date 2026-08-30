# Wizzards Cauldron – Visual Pass Report

Date: 2026-08-29 (Europe/Berlin)

## Result

The full project-owned visual pass is implemented in:

`Assets/WizzardsCauldron/Scenes/SCN_InteractionTest_Visual.unity`

The protected functional source scene remains unchanged at SHA-256:

`2A13B8401B8C493386575CBBB09E09BA43F1BCFBEEE3C7420D7EB106A638B0BC`

No Runtime gameplay script, potion-evaluation algorithm, XR input logic, existing interaction collider, grab point, Asset Store original, or Alex FBX was edited. At Joel's explicit request, only the copied visual scene now uses an expanded project-owned puzzle asset, five additional unchanged potion-prefab instances, extended reset references, and four project-owned table collision volumes.

## Main changed and generated files

- Visual scene: `Assets/WizzardsCauldron/Scenes/SCN_InteractionTest_Visual.unity`
- Build Settings: `ProjectSettings/EditorBuildSettings.asset`
- Repeatable builder: `Assets/WizzardsCauldron/Editor/VisualPassBuilder.cs`
- Asset factory: `Assets/WizzardsCauldron/Editor/VisualPassAssetFactory.cs`
- Scene composer: `Assets/WizzardsCauldron/Editor/VisualPassSceneComposer.cs`
- Target-scene gameplay extension: `Assets/WizzardsCauldron/Editor/VisualPassGameplayExtension.cs`
- Validator: `Assets/WizzardsCauldron/Editor/VisualPassValidator.cs`
- Preview capture: `Assets/WizzardsCauldron/Editor/VisualPassPreviewCapture.cs`
- Runtime feedback: `Assets/WizzardsCauldron/Scripts/Presentation/VisualFeedbackController.cs`
- Portal animation: `Assets/WizzardsCauldron/Scripts/Presentation/AstralPortalAnimator.cs`
- Generated project assets under `Assets/WizzardsCauldron/Art/` and project-owned prefabs under `Assets/WizzardsCauldron/Prefabs/Visual/` and `Prefabs/Environment/`.
- New potion definitions: `SO_PotionBlue`, `SO_PotionViolet`, `SO_PotionCyan`, `SO_PotionOrange`, and `SO_PotionMagenta` under `Assets/WizzardsCauldron/Data/Potions/`.
- Expanded target puzzle: `Assets/WizzardsCauldron/Data/Puzzles/SO_PuzzleVisualExpanded.asset`.
- Expanded-puzzle test: `Assets/WizzardsCauldron/Tests/EditMode/PuzzleSolverTests.cs`.

The builder is available at:

`Tools > Wizzards Cauldron > Build Visual Pass`

It recreates only project-owned visual content in the dedicated target scene, verifies the protected source checksum before and after, and does not save shared gameplay prefab assets.

## Room and architecture

- Existing floor and three wall renderers use project-owned URP dark-stone/floor materials in the visual scene only; their original BoxColliders remain intact.
- Added collider-free stone base trim, upper cornices, two rear observatory columns, warm wooden beams, and a cyan floor alchemy ring.
- The room now has a collider-free dark-stone coffered ceiling at wall height, with low-cost warm-wood cross/length beams and one restrained violet rune. Its static opaque geometry adds no realtime light or shadow cost.
- The former empty front side is visually replaced edge-to-edge by a collider-free astral portal wall, with no sill, posts, beam, or circular architectural frame and therefore no new movement obstacle.
- The selected `FS017_Night` panoramic sky provides a static moonlit astral exterior without motion.
- Decorative geometry is static where safe and creates no invisible navigation obstacles.

## Materials

Twenty-six project-owned `MAT_VP_*` materials were generated:

- Architecture: DarkStone, FloorStone, StoneTrim, WarmWood.
- Metal and props: BlackIron, Gold, Cork, Label.
- Magic: CyanEmission, VioletEmission, OrangeEmission, PortalCore, Liquid.
- Bottles: GlassRed, GlassGreen, GlassYellow, GlassBlue, GlassViolet, GlassCyan, GlassOrange, GlassMagenta.
- VFX: FireParticle, PortalParticle, MagicCircleRuneParticle, MagicCircleRingParticle.
- Sky: AstralNightSky.

All scene-owned materials use URP-compatible shaders. GPU instancing is enabled where applicable. The non-cauldron Alex meshes have unusable UV0 data, so they intentionally use clear solid-color materials instead of unstable texture mapping.

## Alex models

All five final candidates are integrated; no placeholder remains:

| Model | Project-owned wrapper | Integration |
| --- | --- | --- |
| Cauldron | `PF_VP_AlexCauldronVisual` | Collider-free child under the existing Cauldron gameplay root; the neutral wrapper pivot uses `-90° X` so the FBX axis is vertical and the authored feet are below the rim. The previous `+90°` variant was vertically aligned but upside down. Black iron and cyan rune treatment. |
| Wand | `PF_VP_AlexWandVisual` | Collider-free child under the existing grabbable wand root; existing `Visual` collider and `WandTip` remain active. |
| Potion bottle | `PF_VP_AlexPotionVisual` | Eight interactive colored instances: the original three plus five target-scene additions. Every bottle keeps the unchanged `PF_PotionBottle` gameplay root and capsule collider; only its neutral Alex visual pivot and project-owned color material differ. |
| Shelf/table | `PF_VP_AlexFurnitureVisual` | One combined upright visual under the neutral `Placeholders` host; the FBX geometric X-axis is compensated by `+87.500008°` only on the wrapper pivot; both original shelf/table BoxColliders remain unchanged. |
| Reset button | `PF_VP_AlexResetVisual` | Visual child under the existing reset root; interaction volume and status canvas preserved. |

Each wrapper keeps Alex's source FBX as an unchanged child with its scale/orientation correction on neutral project-owned pivots.

## Astral portal

- The previously empty fourth side is now a collider-free, full-bleed astral wall: a `4.08 × 2.95 m` UV-mapped quad overlaps the floor and side-wall edges, leaving no visible geometric gap or frame.
- The project-owned backdrop uses the static `FS017_Night` stars/nebula texture, restrained cyan tinting, and small cyan, violet, and orange star accents.
- The old opaque circular core and heavy cyan/violet geometry rings remain disabled. Only a very faint rune/ring particle motif, dust, and a gentle optional light pulse remain, so the result reads as one large opening rather than a disc mounted on a wall.
- Imported Magic Circle is used through a reduced project-owned copy containing only `rot_rune`, `rot_ring`, and `dust`.
- Rune/ring systems use fixed local meshes and project-owned URP particle materials for Quest stereo stability; only dust remains a billboard.
- A maximum of ten custom inward particles is used. There is no teleport, collider, screen-space distortion, or aggressive flicker.

## Decoration and imported free assets

- Dark Stylized Castle Kit: source textures for project-owned stone/wood materials and the chest mesh.
- Stylized Magic Books: three single-material books on the back shelf plus two books on a shallow side-wall curio shelf.
- Free Mining Pack: blue/gold crystals and two lantern holders; imported colliders and lights are removed from scene-owned decorative instances.
- Free Pack – Fire Effects: glow texture used by the project-owned URP flame material.
- Free Game VFX – Magic Circle URP: reduced portal subset and source textures, copied into the project-owned portal wrapper.
- Fantasy Skybox FREE: only `FS017_Night` is referenced by the project-owned panoramic sky material.
- Four decorative bottles, two side-wall shelves, two rune plaques, a crystal pedestal, a chest, two back-wall lanterns, and four wall candles complete the room without adding gameplay colliders.
- A new front potion worktop and gold trim close the requested central table area. Three new potions stand on the raised left extension and two on the new front worktop; all five positions have matching visual docks.
- Project-owned modular worktops, shelf, legs, and a small stone hearth reinforce the upright furniture silhouette. Four target-scene-only non-trigger BoxColliders match the main workbench body, central surface, front worktop, and left extension so virtual locomotion and physics objects cannot pass through the table.

## Lighting and post-processing

- One warm directional base light with no realtime shadow dependency provides the Quest-safe room illumination.
- Optional warm key, cauldron accent, and portal accent point lights exist for quality profiles that render additional lights; all have shadows disabled.
- Quest Performance settings remain unchanged and disable additional lights, so readability does not depend on them.
- Trilight ambient lighting mixes warm room tones with a restrained cool astral fill.
- Global URP volume: ACES tonemapping, post exposure `0.28`, contrast `4`, saturation `2`.
- Bloom: threshold `0.5`, intensity `0.08`, scatter `0.32`, clamp `1.5`; compatible with the Quest Performance LDR buffer.
- Motion Blur and Depth of Field are disabled. Fog and expensive screen filters are not used.

## Visual gameplay feedback

`VisualFeedbackController` subscribes read-only to the existing events and unsubscribes safely:

- `CauldronIntake.PotionProcessed`: short color-matched accepted spark or restrained rejected pulse.
- `GameSessionController.AttemptFinished`: distinct optimal, valid, and no-potion visuals.
- `GameSessionController.SessionReset`: stops active effects, restores visual baselines, then plays a short reset pulse.

The controller only changes ParticleSystems, one optional Light, and a MaterialPropertyBlock on the cauldron rune. It does not mutate potion, score, capacity, session, or reset state and creates no material instances.

`AlchemyAudioFeedback` also subscribes read-only and generates small, license-free sound clips at runtime: a glass/magic splash for processed potions, a spatial reset sweep, and distinct optimal/valid/failed solution chimes. The three one-shot sources are positioned at the cauldron, reset rune, and submit rune; ambient music remains a separate low-volume source.

The existing `CauldronLiquidDisplay` and its protected `LiquidVisual` transform remain in place; only the material was restyled.

The large primitive renderer on `FinishTarget` is hidden and replaced visually by a rune labeled `SUBMIT SOLUTION`. Joel explicitly requested a more forgiving hit area, so only the copied visual scene's existing trigger radius was enlarged from `0.5` to `0.9` local units (about 15 cm to 27 cm world diameter). The `WandActivator` reference and wand-only behavior remain intact; the source scene is unchanged. The instruction board now explains where and how to submit and reset.

The visual target scene has a deliberately expanded data set without changing the solver or session code:

- Blue: health `2`, fill `1`
- Violet: health `5`, fill `3`
- Cyan: health `7`, fill `4`
- Orange: health `4`, fill `2`
- Magenta: health `9`, fill `5`
- Expanded capacity: `8`; target health: `15`; unique optimal combination: Blue + Orange + Magenta (`15/8`).
- PotionRed was intentionally removed from the visual scene, expanded puzzle, hover-source list and reset lists. Reset now restores all seven remaining potion instances and the existing wand. The protected source scene and original Red definition asset remain unchanged and recoverable.

## Quest performance measures

- Four configured realtime/mixed lights total; no non-directional realtime shadows.
- Quest-readable directional/ambient fallback because Performance URP disables additional lights.
- LDR-safe restrained Bloom; HDR remains disabled.
- Every particle system is capped at 48 or fewer particles. The current validator reports a conservative aggregate configured maximum of `374/128`; effects are short and do not all run together, but this remains a Quest profiling item.
- No particle, portal, ordinary decoration, or Alex visual wrapper has a Collider. The only added colliders are the four explicitly requested, opaque workbench collision volumes under the project-owned gameplay-extension root.
- Opaque URP materials are preferred; transparency is limited to bottles and small particles.
- No distortion, fluid simulation, realtime reflection probe, motion blur, or depth of field.
- Shared materials, generated meshes, static flags, and GPU instancing are reused where appropriate.

## Validation and tests

- Final Unity compile/build: success, return code `0`, no C# errors or warnings (`Logs/VisualPassBuild_Idempotence_ExpandedFinal.log`).
- A second consecutive builder run completed successfully without duplicating gameplay objects, confirming repeatability.
- Builder safety gate confirmed all 121 protected serialized components and 59 protected poses before generating content (`Logs/VisualPassBuild_Idempotence_ExpandedFinal.log`).
- Final validator: `42 OK`, `0 WARN`, `0 ERROR` (`Logs/VisualPassValidation_ExpandedFinal2.log`).
- Expanded-scene checks passed: five additional defined/grabbable/resettable potions, seven total PotionControllers after the intentional removal of PotionRed, eight physics-reset entries, capacity-eight puzzle, unique optimum, and four non-trigger table collision volumes.
- Missing scripts: none across 341 target-scene GameObjects.
- Follow-up PlayMode test after the sound and submit-rune update: passed (`Logs/interaction-audio-results.xml`). It verifies all seven potions, reset stability, generated audio readiness/playback, the enlarged submit trigger, and its label.
- Latest read-only validator: the enlarged submit trigger passes, with `42 OK`, `1 WARN`, and `3 ERROR` (`Logs/interaction-audio-validation.log`). The remaining errors describe older target-layout drift already present in the working scene (reparented `WandTable`, changed Reset/Wand poses, and target-only expanded-puzzle reference); this update did not reset those user-approved scene positions.
- Protected coverage: 16 Colliders, 4 Rigidbodies, 80 XR components, and 21 gameplay/controller components.
- Shared gameplay prefab hashes remain unchanged:
  - `PF_PotionBottle.prefab`: `AC95364430EE70DC1DE6663189BD4465B38B835DCE4DA7D67A1FFFADC561E917`
  - `PF_Wand.prefab`: `DDFAC87B506BCCD3F640A2409D5CAAD0B106BBE61C732860F3BC7270B5EEC5BF`
  - `PF_ResetControl.prefab`: `A7C3F5C44186922C80B4E8C35F16F2B028420D3BE1BA4FD634D832E8043AEA88`
- EditMode tests: `13/13 passed`, `0 failed` (`Logs/VisualPassEditMode_ExpandedFinal.xml`). The new exhaustive subset test proves Blue + Orange + Magenta is the only optimal `15/8` result.
- Six automated desktop preview images: `Logs/VisualPassPreviews/`, including dedicated potion-layout and coffered-ceiling views.

## Build Settings

`ProjectSettings/EditorBuildSettings.asset` now contains exactly one enabled scene:

`Assets/WizzardsCauldron/Scenes/SCN_InteractionTest_Visual.unity`

`Assets/Scenes/SampleScene.unity` is no longer enabled.

## Remaining manual checks

- Unity Editor Play Mode: verify all eight bottles are visible/grabbable and show the intended values; then test Blue + Orange + Magenta, finish/result, and Reset restoring all eight.
- Walk into the table using controller locomotion from the front and side; the rig should stop at the workbench. Physical head movement cannot be prevented by Unity colliders and must be handled by normal guardian/comfort behavior.
- Look upward and around the full room to confirm the coffered ceiling has no gaps and the five new bottles do not overlap UI, portal, cauldron, or grab space.
- Confirm world-space UI readability, potion labels, reset feedback, and no decorative overlap from the headset view.
- Meta Quest Android build: controller alignment, reach/scale, comfort, portal stereo stability, particle overdraw, post-processing, and sustained frame rate.
- If profiling shows a device-specific issue, tune project-owned visual assets only; gameplay and XR logic remain out of scope.

## Known limitations

- Automated still renders do not simulate portal rotation, torch particles, or event-driven feedback.
- Desktop validation proves the table colliders exist and are non-trigger; actual locomotion response still requires Play Mode and headset confirmation.
- No physical Quest performance measurement has been completed yet.
- The source Alex UV limitations prevent conventional detailed texturing on four of the five meshes.
- Point-light pulses are intentionally optional on the Quest Performance quality tier; rune/material emission remains the guaranteed device-visible feedback.
