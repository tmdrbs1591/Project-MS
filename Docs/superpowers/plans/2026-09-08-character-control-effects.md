# Character Control Effects Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add mapped-input observation, independently timed control seals, aim distortion, and local crosshair presentation to the common `CharacterBase` API.

**Architecture:** Record semantic input before filtering, replicate press sequences and held state, then apply authority-owned timers before movement and action execution. Keep crosshair rendering local to the input owner while synchronizing only its requested presentation state.

**Tech Stack:** Unity 6, C#, Photon Fusion Shared Mode, Unity Input System, Unity UI, Unity Test Framework

**Spec:** `docs/superpowers/specs/2026-09-08-character-control-effects-design.md`

## Global Constraints

- Modify only the common character framework and its guide; do not modify character-specific scripts or prefabs.
- Observe mapped semantic actions, never raw physical key codes from another player.
- Longer duplicate effects must not be shortened by a later request.
- The operating-system mouse position must never be moved.
- Crosshair objects are local UI and are never network-spawned.

---

### Task 1: Control and input contracts

**Files:**
- Create: `Assets/00.Main/01.Script/Character/Framework/Runtime/Control/CharacterControlType.cs`
- Create: `Assets/00.Main/01.Script/Character/Framework/Runtime/Control/CharacterInputType.cs`
- Create: `Assets/00.Main/01.Script/Character/Framework/Runtime/Control/CharacterControlRules.cs`
- Test: `Assets/00.Main/01.Script/Character/Framework/Editor/CharacterControlRulesTests.cs`

**Interfaces:**
- Produces: concrete seal-slot mapping, finite-value validation, input filtering, and aim transformation helpers.

- [ ] Write tests for combined masks, input filtering, longer-duration replacement, and aim transformation.
- [ ] Run EditMode tests and confirm they fail because the contracts do not exist.
- [ ] Add the minimal enums and pure rules.
- [ ] Run EditMode tests and confirm they pass.

### Task 2: Networked state and simple character API

**Files:**
- Create: `Assets/00.Main/01.Script/Character/Framework/Runtime/Core/CharacterBase.ControlEffects.cs`
- Modify: `Assets/00.Main/01.Script/Character/Framework/Runtime/Core/CharacterBase.cs`
- Test: `Assets/00.Main/01.Script/Character/Framework/Editor/CharacterControlApiTests.cs`

**Interfaces:**
- Consumes: `CharacterControlType`, `CharacterInputType`, and `CharacterControlRules`.
- Produces: `ApplyControlSeal`, `ApplyAimInversion`, `ApplyAimAngleOffset`, `WasInputPressed`, `IsInputHeld`, and `GetObservedMoveDirection`.

- [ ] Write tests for the public/protected API shape and snapshot filtering behavior.
- [ ] Run tests and confirm the new API is missing.
- [ ] Add replicated input sequences, held state, timers, validated RPC requests, and reset behavior.
- [ ] Insert observation and filtering before movement/action execution.
- [ ] Run tests and compile the Unity assemblies.

### Task 3: Local crosshair presenter

**Files:**
- Create: `Assets/00.Main/01.Script/Character/Framework/Runtime/Visual/CharacterAimCursorPresenter.cs`
- Modify: `Assets/00.Main/01.Script/Character/Framework/Runtime/Core/CharacterBase.ControlEffects.cs`
- Modify: `Assets/00.Main/01.Script/Character/Framework/Runtime/Core/CharacterBase.cs`
- Test: `Assets/00.Main/01.Script/Character/Framework/Editor/CharacterAimCursorPresenterTests.cs`

**Interfaces:**
- Produces: `SetCrosshairOffset`, `SetCrosshairVisible`, `SetSystemCursorVisible`, and `ReplaceCursorWithCrosshair`.

- [ ] Write EditMode tests for local object construction and visible-state application.
- [ ] Run tests and confirm failure before the presenter exists.
- [ ] Add a local overlay presenter with a code-generated crosshair and cursor-state restoration.
- [ ] Bind it only for Input Authority and dispose it on despawn/destroy.
- [ ] Run presenter tests and compile the project.

### Task 4: Guide and verification

**Files:**
- Modify: `Docs/Character/CharacterFramework.md`
- Modify: `output/pdf/ProjectMS_Character_Production_Guide_20260817.pdf`

**Interfaces:**
- Documents all APIs from Tasks 2 and 3 with short copyable examples and a 2-player checklist.

- [ ] Add a beginner-oriented section in the existing guide at the appropriate gameplay-feature position.
- [ ] Explain authority, timing, input-before-seal observation, gameplay aim versus UI offset, and reset behavior.
- [ ] Rebuild the existing PDF with readable margins and code-block padding.
- [ ] Render representative pages and visually inspect spacing and page order.
- [ ] Run the full EditMode suite and Unity compile verification.
- [ ] Review `git diff` to ensure character-specific assets were not touched.

