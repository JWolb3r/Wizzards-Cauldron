# Wizzards Cauldron – Visual Pass Report

Date: 2026-08-20 (Europe/Berlin)

## Result

The full project-owned visual pass is implemented in:

`Assets/WizzardsCauldron/Scenes/SCN_InteractionTest_Visual.unity`

The protected functional source scene remains unchanged at SHA-256:

`2A13B8401B8C493386575CBBB09E09BA43F1BCFBEEE3C7420D7EB106A638B0BC`

No gameplay rule, potion evaluation, XR input logic, interaction collider, grab point, Asset Store original, or Alex FBX was edited.

## Main changed and generated files

- Visual scene: `Assets/WizzardsCauldron/Scenes/SCN_InteractionTest_Visual.unity`
- Build Settings: `ProjectSettings/EditorBuildSettings.asset`
- Repeatable builder: `Assets/WizzardsCauldron/Editor/VisualPassBuilder.cs`
- Asset factory: `Assets/WizzardsCauldron/Editor/VisualPassAssetFactory.cs`
- Scene composer: `Assets/WizzardsCauldron/Editor/VisualPassSceneComposer.cs`
- Validator: `Assets/WizzardsCauldron/Editor/VisualPassValidator.cs`
- Preview capture: `Assets/WizzardsCauldron/Editor/VisualPassPreviewCapture.cs`
- Runtime feedback: `Assets/WizzardsCauldron/Scripts/Presentation/VisualFeedbackController.cs`
- Portal animation: `Assets/WizzardsCauldron/Scripts/Presentation/AstralPortalAnimator.cs`
- Generated project assets under `Assets/WizzardsCauldron/Art/` and project-owned prefabs under `Assets/WizzardsCauldron/Prefabs/Visual/` and `Prefabs/Environment/`.

The builder is available at:

`Tools > Wizzards Cauldron > Build Visual Pass`

It recreates only project-owned visual content in the dedicated target scene, verifies the protected source checksum before and after, and does not save shared gameplay prefab assets.

## Room and architecture

- Existing floor and three wall renderers use project-owned URP dark-stone/floor materials in the visual scene only; their original BoxColliders remain intact.
- Added collider-free stone base trim, upper cornices, four observatory columns, two open-roof wooden beams, a cyan floor alchemy ring, and a framed astral threshold on the fourth side.
- The ceiling remains open. The former empty front side is now visually closed by a collider-free astral portal wall, so it does not create a new movement obstacle.
- The selected `FS017_Night` panoramic sky provides a static moonlit astral exterior without motion.
- Decorative geometry is static where safe and creates no invisible navigation obstacles.

## Materials

Twenty-three project-owned `MAT_VP_*` materials were generated:

- Architecture: DarkStone, FloorStone, StoneTrim, WarmWood.
- Metal and props: BlackIron, Gold, Cork, Label.
- Magic: CyanEmission, VioletEmission, OrangeEmission, PortalCore, Liquid.
- Bottles: GlassRed, GlassGreen, GlassYellow, GlassBlue, GlassViolet.
- VFX: FireParticle, PortalParticle, MagicCircleRuneParticle, MagicCircleRingParticle.
- Sky: AstralNightSky.

All scene-owned materials use URP-compatible shaders. GPU instancing is enabled where applicable. The non-cauldron Alex meshes have unusable UV0 data, so they intentionally use clear solid-color materials instead of unstable texture mapping.

## Alex models

All five final candidates are integrated; no placeholder remains:

| Model | Project-owned wrapper | Integration |
| --- | --- | --- |
| Cauldron | `PF_VP_AlexCauldronVisual` | Collider-free child under the existing Cauldron gameplay root; the FBX geometric X-axis is compensated by `+90°` only on the neutral wrapper pivot; black iron and cyan rune treatment. |
| Wand | `PF_VP_AlexWandVisual` | Collider-free child under the existing grabbable wand root; existing `Visual` collider and `WandTip` remain active. |
| Potion bottle | `PF_VP_AlexPotionVisual` | Three colored instances under the existing potion roots; visual orientation corrected at the neutral pivot, existing capsule colliders preserved. |
| Shelf/table | `PF_VP_AlexFurnitureVisual` | One combined upright visual under the neutral `Placeholders` host; the FBX geometric X-axis is compensated by `+87.500008°` only on the wrapper pivot; both original shelf/table BoxColliders remain unchanged. |
| Reset button | `PF_VP_AlexResetVisual` | Visual child under the existing reset root; interaction volume and status canvas preserved. |

Each wrapper keeps Alex's source FBX as an unchanged child with its scale/orientation correction on neutral project-owned pivots.

## Astral portal

- The previously empty fourth side is now a collider-free astral threshold: a full dark backdrop, stone posts and sill, star points, and a centered portal approximately 2.3 m in diameter.
- Deep black core, fixed cyan outer ring, violet rune ring, slow counter-rotation (`2.2°/s` and `3.1°/s`), and a gentle `0.09 Hz` pulse.
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
- Project-owned modular worktop, shelf, legs, and a small stone hearth reinforce the upright furniture silhouette while the original interaction colliders remain untouched.

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

The existing `CauldronLiquidDisplay` and its protected `LiquidVisual` transform remain in place; only the material was restyled.

## Quest performance measures

- Four configured realtime/mixed lights total; no non-directional realtime shadows.
- Quest-readable directional/ambient fallback because Performance URP disables additional lights.
- LDR-safe restrained Bloom; HDR remains disabled.
- Every particle system is capped at 48 or fewer particles; total configured maximum is `122/128`, including the four low-density candle flames.
- No particle, portal, decoration, or Alex visual wrapper has a Collider.
- Opaque URP materials are preferred; transparency is limited to bottles and small particles.
- No distortion, fluid simulation, realtime reflection probe, motion blur, or depth of field.
- Shared materials, generated meshes, static flags, and GPU instancing are reused where appropriate.

## Validation and tests

- Final Unity compile: success, return code `0`, no C# errors or warnings (`Logs/VisualPassCompile-enriched-final.log`).
- Builder: success, return code `0`; its pre-save safety gate confirmed all 121 protected serialized components and 59 protected poses before generating content (`Logs/VisualPassBuild-enriched-final.log`).
- Final validator: `38 OK`, `0 WARN`, `0 ERROR` (`Logs/VisualPassValidation-enriched-final.log`).
- Missing scripts: none across 270 target-scene GameObjects.
- Protected source/target state: identical across 121 serialized protected components and 59 protected object poses.
- Protected coverage: 16 Colliders, 4 Rigidbodies, 80 XR components, and 21 gameplay/controller components.
- Shared gameplay prefab hashes remain unchanged:
  - `PF_PotionBottle.prefab`: `AC95364430EE70DC1DE6663189BD4465B38B835DCE4DA7D67A1FFFADC561E917`
  - `PF_Wand.prefab`: `737192EB226728DAF8E098BD490C9387680A413566D991DED10FC9CB281BC5D1`
  - `PF_ResetControl.prefab`: `A7C3F5C44186922C80B4E8C35F16F2B028420D3BE1BA4FD634D832E8043AEA88`
- EditMode tests: `12/12 passed`, `0 failed` (`Logs/VisualPassEditModeResults-enriched.xml`).
- Four automated desktop preview images: `Logs/VisualPassPreviews/`.

## Build Settings

`ProjectSettings/EditorBuildSettings.asset` now contains exactly one enabled scene:

`Assets/WizzardsCauldron/Scenes/SCN_InteractionTest_Visual.unity`

`Assets/Scenes/SampleScene.unity` is no longer enabled.

## Remaining manual checks

- Unity Editor Play Mode: full grab/potion/finish/result/reset loop and visual event timing.
- Confirm world-space UI readability, potion labels, reset feedback, and no decorative overlap from the headset view.
- Meta Quest Android build: controller alignment, reach/scale, comfort, portal stereo stability, particle overdraw, post-processing, and sustained frame rate.
- If profiling shows a device-specific issue, tune project-owned visual assets only; gameplay and XR logic remain out of scope.

## Known limitations

- Automated still renders do not simulate portal rotation, torch particles, or event-driven feedback.
- No physical Quest performance measurement has been completed yet.
- The source Alex UV limitations prevent conventional detailed texturing on four of the five meshes.
- Point-light pulses are intentionally optional on the Quest Performance quality tier; rune/material emission remains the guaranteed device-visible feedback.
