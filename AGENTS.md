<!-- UNITY CODE ASSIST INSTRUCTIONS START -->
- Project name: Wizzards Cauldron
- Unity version: Unity 6000.0.42f1
- Active game object:
  - Name: InstructionCanvas
  - Tag: Untagged
  - Layer: Default
<!-- UNITY CODE ASSIST INSTRUCTIONS END -->

## Wizzards Cauldron project rules

- Do not change gameplay logic unless the user explicitly requests it. In particular, preserve the responsibilities and public events of `CauldronIntake`, `GameSessionController`, `CauldronController`, and `CauldronLiquidDisplay`.
- Preserve existing and uncommitted team changes. Never overwrite, reset, clean, or delete them; keep baseline commit `e85e473` recoverable.
- Treat `Assets/WizzardsCauldron/Scenes/SCN_InteractionTest.unity` as the protected functional baseline. Perform later visual work in `Assets/WizzardsCauldron/Scenes/SCN_InteractionTest_Visual.unity`.
- Preserve existing colliders, trigger volumes, XR interaction points, wand-tip behavior, reset lists, and serialized gameplay references.
- Add replaceable art as collider-free children below neutral visual pivots or project-owned wrapper prefabs. Do not replace interactive root objects with imported model roots.
- Do not edit Alex's source FBX files in place. Keep source models replaceable and apply scale, orientation, materials, and presentation through wrappers or child transforms.
- Do not edit third-party Asset Store content or Unity sample content directly. Build project-owned materials, prefabs, variants, and wrappers under `Assets/WizzardsCauldron/`.
- Use only URP-compatible project-owned materials and shaders in the visual scene. Do not convert third-party materials in place.
- Prioritize stable Meta Quest Standalone performance and clear VR readability over expensive effects. Limit transparent overdraw, particle counts, realtime lights, dynamic shadows, and realtime reflections.
- Treat VR controllers as the primary input method. Do not add a hand-tracking dependency during the visual pass.
- Do not change `ProjectSettings/EditorBuildSettings.asset` or the active build scene without explicit user approval.
- Do not upgrade Unity or project packages without explicit user approval. The required editor is Unity `6000.0.42f1`.
- Before completion, run a Unity compile test and check for compiler errors, missing scripts, missing asset references, model availability, required shaders, and build-settings status.
- Use `Tools > Wizzards Cauldron > Validate Visual Setup` for a read-only setup check when the Unity Editor is available.