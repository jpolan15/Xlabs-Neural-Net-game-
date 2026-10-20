# Task: Phase 5 — Chambers 02 to 04 and the ending

Status: in-progress
Owner: Gameplay / Presentation / XR / Core puzzles
Created: 2026-09-29
Updated: 2026-09-29

## Description

The owner asked to finish the remaining chambers in this pass. XOR needs a hidden layer before the telescope powers on. Photo cards train with Phase 4 SGD and fail honestly on a bad set. Attention masks a corrupted nav-log token, then the jump, Earth, credits, and a journal play.

One task lists every assembly this chamber sequence touches, because the owner asked for the whole ending rather than a stop at the first boundary.

## Allowed paths

- Assets/Scripts/Core/Puzzles/**
- Assets/Scripts/Core/Neural/NetworkModel.cs
- Assets/Scripts/Gameplay/**
- Assets/Scripts/Presentation/**
- Assets/Scripts/XR/XRInteractableBridge.cs
- Assets/Scripts/XR/PhotoShutter.cs
- Tools/Editor/Level01SceneBuilder.cs
- Tests/EditMode/Core/XorAndNavAttentionTests.cs
- Tests/PlayMode/Gameplay/Level01IntegrationTests.cs

## Forbidden paths

- Assets/Samples/**
- Packages/**
- ProjectSettings/**
- Assets/Scripts/Core/Math/** (Phase 4 owns new math)

## Acceptance criteria

- [ ] A single neuron cannot pass XOR. A hidden layer with a non-linear activation can, and that solve powers the telescope.
- [ ] Photos produce features from the capture. Training is SGD. A held-out set can fail in a way the player can read.
- [ ] Masking the corrupted nav token is what predicts JUMP_HOME.
- [ ] The jump, Earth in the canopy, credits, and a journal run only after the attention solve.
- [ ] Handoff report filed.

## Blocked on

Phase 4 core math.

## Handoff notes

PuzzleEvaluator still decides correctness. No chamber is marked passed from a trigger pull alone.
