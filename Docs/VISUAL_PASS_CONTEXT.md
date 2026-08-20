# Wizzards Cauldron – Visual Pass Context

Last updated: 2026-08-20 (Europe/Berlin)

## Confirmed technical baseline

- Project: `Wizzards Cauldron`
- Unity version: `6000.0.42f1` (`feb9a7235030`)
- Unity Editor: `C:\Program Files\Unity\Hub\Editor\6000.0.42f1\Editor\Unity.exe`
- Render pipeline: Universal Render Pipeline `17.0.4`
- Input System: `1.15.0`
- XR Interaction Toolkit: `3.4.1`
- OpenXR: `1.16.1`
- XR Hands: `1.7.3`
- XR Plug-in Management: `4.5.4`
- Target device: Meta Quest Standalone / Android
- Input method: VR controllers
- Quality priority: stable standalone-VR performance and clear readability before expensive effects

## Scene strategy

- Protected functional source scene: `Assets/WizzardsCauldron/Scenes/SCN_InteractionTest.unity`
- Visual-pass target scene: `Assets/WizzardsCauldron/Scenes/SCN_InteractionTest_Visual.unity`
- Strategy confirmed by Joel: work in an identical scene copy so the functional baseline remains recoverable.
- Visual pass implemented in the copied target scene; the protected functional source remains unchanged.
- Current Build Settings: only `Assets/WizzardsCauldron/Scenes/SCN_InteractionTest_Visual.unity` is enabled. `SampleScene` is no longer enabled.

## Gameplay integration boundaries

- `CauldronIntake.PotionProcessed`: `Action<PotionController, PotionAcceptanceResult>`
- `GameSessionController.AttemptFinished`: `Action<AttemptResult>`
- `GameSessionController.SessionReset`: `Action`
- `CauldronController.TotalsChanged`: `Action`
- `CauldronLiquidDisplay` listens to `TotalsChanged` and drives only its assigned liquid transform.
- Visual listeners may react to these events but must not mutate scoring, potion state, capacity, session state, or reset state.
- Preserve the cauldron body collider, `IntakeTrigger`, `FinishTarget`, `WandTip`, interactive roots, reset tracking lists, and all saved references.

## Alex model source and status

- Received archive: `Incoming/AlexModels/models_für_lars.zip`
- Archive SHA-256: `FAE6DCE43D9554481C76492DFBCE4073FA3C817CF9216157BF508E566F41BDC1`
- The archive models are newer and byte-different from the older files in `Assets/WizzardsCauldron/Models/`.
- Source units are inches (`UnitScaleFactor 2.54`). A neutral visual-wrapper scale near `10` produces plausible VR dimensions and must be verified per object.
- All five files contain one mesh and no useful child-object hierarchy. Normals are complete; tangents are calculated by Unity.
- Only the cauldron has a usable UV0 unwrap. Bottle, reset button, shelf/table, and wand have degenerate UV0 values and therefore require solid-color or triplanar/projected project-owned materials unless Alex supplies revised UVs.
- No model has UV2/lightmap UVs. Do not enable or alter import settings on Alex's originals without approval.

| Model | Final project path | Status |
| --- | --- | --- |
| Cauldron | `Assets/WizzardsCauldron/Art/Models/Alex/cauldron_1.fbx` | 2026-08-15 16:30:14, 255,568 bytes. Final candidate per Joel. The centered pivot is retained; its remaining `-90°` geometric X-axis is safely compensated with `+90°` only on the project-owned wrapper `VisualPivot`. Five declared material records; polygon faces use slots 0–1. Built orientation verified upright. |
| Wand | `Assets/WizzardsCauldron/Art/Models/Alex/wand_1.fbx` | 2026-08-15 16:39:16, 90,752 bytes. Latest candidate. Geometry and normals valid; UV0 is not usable. Three declared material records; polygon faces use slots 0–1. Keep the existing interactive wand root and `WandTip`. |
| Potion bottle | `Assets/WizzardsCauldron/Art/Models/Alex/bottle_1.fbx` | 2026-08-15 16:24:40, 287,696 bytes. Latest candidate. Geometry and normals valid; UV0 is not usable. Eleven declared material records; polygon faces use slots 0–2. Keep `PF_PotionBottle` as the interactive root. |
| Shelf and table | `Assets/WizzardsCauldron/Art/Models/Alex/shelf_and_table_!.fbx` | 2026-08-15 16:32:34, 67,456 bytes. Latest candidate; original filename preserved. Geometry and normals valid; UV0 is not usable. Its `-87.500008°` geometric X-axis is compensated with `+87.500008°` only on the project-owned wrapper `VisualPivot`. Two declared material records and both polygon slots are used. Built orientation verified upright. |
| Reset button | `Assets/WizzardsCauldron/Art/Models/Alex/reset_button.fbx` | 2026-08-15 16:41:30, 115,040 bytes. Latest candidate. Geometry and normals valid; UV0 is not usable. Four declared material records; polygon faces use slots 0–1. Preserve `PF_ResetControl`, its interaction volume, and reset reference. |

## Imported Asset Store packages

Third-party contents are read-only sources. Create project-owned wrappers and materials under `Assets/WizzardsCauldron/`.

| Package | Imported path | Setup status |
| --- | --- | --- |
| Dark Stylized Castle Kit | `Assets/StylizedDarkCastle/` | Complete: 109 prefabs. Source materials use Built-in shaders; create URP project-owned replacements rather than converting the package in place. |
| Stylized Magic Books | `Assets/GreyratsLab/Stylized magic books/` | Complete and URP Lit-compatible. |
| Free Mining Pack | `Assets/PurePoly/Mining_Free_Assets/` | Complete: 23 prefabs. Main materials use URP Lit. Two unresolved lighting references exist only in its demo scenes. |
| Free Pack – Fire Effects | `Assets/PolyOne/Fire Effects/` | Five effect prefabs present. Some particle materials use legacy built-in particle shaders; make optimized project-owned URP variants. One unresolved lighting-settings reference exists only in the demo scene. |
| Free Game VFX – Magic Circle URP | `Assets/Eric VFX Studio/Game VFX - Magic Circle(Free)/` with dependencies in `Assets/Eric VFX Studio/Resource/` | Complete; custom effect shader declares the Universal Pipeline and project references resolve. |
| Fantasy Skybox FREE | `Assets/Fantasy Skybox FREE/` | Imported but optional and not planned by default. Approximately 133 MB. Prefer at most one selected skybox if the enclosed room later exposes an exterior view. Do not include its demo scene. |

## Known issues and observations

- Build Settings now enable only `Assets/WizzardsCauldron/Scenes/SCN_InteractionTest_Visual.unity`, as authorized for the completed visual pass.
- `ProjectSettings/EditorBuildSettings.asset` contains a global Input Actions GUID (`985fdffee33a5944396199c6b7145dd4`) that does not resolve to a local asset. Existing XR scene input references are otherwise present; verify this setting before a device build.
- OpenXR loaders are assigned for Android and Standalone, while serialized automatic loading/running flags are disabled. Verify actual XR startup in Play Mode and on the Quest before release.
- Android and Standalone OpenXR settings include both Meta and Android XR-related features. Narrow these only after confirming the final Quest build configuration; do not change them during visual setup.
- `manifest.json` requests AR Foundation `6.4.1`, while package resolution selects `6.5.0` because Meta OpenXR requires it. The project compiled successfully with the resolved version; keep this under observation rather than forcing a downgrade.
- Third-party demo-scene reference issues listed above do not affect the project-owned interaction scene or the usable prefabs.
- The first sandboxed batch launches could not reach Unity Licensing. A narrowly approved Unity launch succeeded and produced a clean compile.
- Git branch: `joel_visuals`. Asset Store imports and setup files are currently uncommitted. Preserve the baseline commit `e85e473` and do not reset team changes.

## Performance requirements for the visual pass

- Treat Meta Quest Standalone / Android as the primary platform and profile on the actual headset.
- Keep the room compact and favor baked or otherwise low-cost lighting.
- Use few realtime lights and short shadow distances; avoid overlapping realtime shadow casters.
- Prefer opaque materials. Use transparency, emission, distortion, and particles only where they materially improve feedback.
- Keep cauldron liquid presentation simple; no fluid simulation, dynamic ripples, or expensive transparent volume.
- Limit particle counts and screen-filling overdraw. Create short-lived acceptance/rejection effects driven by `PotionProcessed`.
- Avoid unnecessary realtime reflection probes, post-processing, and high-resolution textures.
- Ensure UI, potion identity, capacity, result, finish target, and reset control remain readable at VR viewing distances.

## Remaining manual tasks

- Open and visually inspect the completed target scene in the Unity Editor.
- Run the full grab, potion, finish, result, and reset loop in Play Mode.
- Test the Android build on Meta Quest Standalone for scale, comfort, controller alignment, readability, portal/VFX behavior, and stable frame rate.
- Profile on the headset and reduce particles or post-processing further only if the device measurement requires it.

## Validation status

- Final compile: successful on 2026-08-20 19:01 CEST, return code `0`, no C# errors or warnings; `Logs/VisualPassCompile-enriched-final.log`.
- Visual builder: successful, return code `0`; its pre-save safety gate confirmed 121 protected serialized components and 59 protected poses before generating content (`Logs/VisualPassBuild-enriched-final.log`).
- Final visual validator: 38 checks passed, 0 warnings, 0 errors; `Logs/VisualPassValidation-enriched-final.log`.
- Missing-script scan: none in the 270 target-scene GameObjects.
- Protected-state comparison: 121 serialized components and 59 protected poses match the source, covering 16 colliders, 4 rigidbodies, 80 XR components, and 21 gameplay/controller components.
- Performance validation: 4/4 realtime-light budget, no small realtime shadows, all particle systems at or below 48 particles, total configured maximum 122/128, and no particle colliders.
- Quest Performance configuration is intentionally unchanged: additional lights and HDR remain disabled. The target therefore uses a warm directional/ambient fallback and LDR-safe Bloom threshold `0.5`.
- EditMode tests: 12/12 passed, 0 failed; `Logs/VisualPassEditModeResults-enriched.xml`.
- Automated desktop previews: `Logs/VisualPassPreviews/`.
- Protected source SHA-256 remains `2A13B8401B8C493386575CBBB09E09BA43F1BCFBEEE3C7420D7EB106A638B0BC`.
- Shared gameplay prefabs remain byte-identical to the pre-pass hashes. No Asset Store original or Alex FBX was edited.
- Final manual Editor Play Mode and Meta Quest device tests are still required.
