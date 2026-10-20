# Task: Phase 4 — Core training and attention

Status: in-progress
Owner: Core
Created: 2026-09-29
Updated: 2026-09-29

## Description

Score every output, add gradient hooks, replace the training stub with deterministic backpropagation and SGD, and implement scaled dot-product attention. Formulas and EditMode tests land in the same change.

## Allowed paths

- Assets/Scripts/Core/**
- Documentation/Mathematics/FORMULAS.md
- Tests/EditMode/**

## Forbidden paths

- Assets/Scripts/Gameplay/**
- Assets/Scripts/XR/**
- Assets/Scripts/Presentation/**
- ProjectSettings/**
- Packages/**

## Acceptance criteria

- [ ] PuzzleEvaluator scores multi-output binary cases and continuous targets with a per-output tolerance.
- [ ] NetworkModel exposes gradient hooks used by backpropagation.
- [ ] SGD is deterministic and raises a telemetry event on every step.
- [ ] Scaled dot-product attention lives in Core Math.
- [ ] FORMULAS.md matches the code. Each new formula has an EditMode test.
- [ ] Validate-CoreBoundaries.ps1 passes.
- [ ] Handoff report filed.

## Blocked on

None for the math. The chamber wiring is Phase 5.

## Handoff notes

Step activation has a zero derivative, so training puzzles use Sigmoid or ReLU. No UnityEngine references.
