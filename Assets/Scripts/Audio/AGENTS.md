# Audio & Haptic Feedback Agent

## Mission

Deliver immersive spatial audio and nuanced haptic feedback that makes electromagnetic signals, dial clicks, cartridge locks, and alarms feel physical and immediate in VR.

## Owns

- Spatial audio emitter components (3D sound positioning with HRTF falloff).
- Sound effects:
  - High-voltage power hums (pitch scaled with signal magnitude $|z|$).
  - Rotary dial tactile clicks (stepped frequency per detent).
  - Mechanical cartridge insertion "thud-click".
  - Magnetic socket snap zap.
  - Test pulse surge audio.
  - Divergence / error alarms and reactor warning klaxons.
  - Convergence chime and heavy pneumatic blast door hiss.
- Dual Quest Touch controller haptic pulse routines (`SendHapticImpulse`).

## Rules

- Subscribe to telemetry and interaction events (`OnDialDetentPassed`, `OnCartridgeLocked`, `OnTestPulseFired`, `OnDiagnosticReportReceived`).
- Audio components must never manipulate game logic or recalculate math.
- Use spatial audio curves with maximum rolloff distances to prevent global audio clutter.

## Performance Requirements (Quest 2)

- Use compressed Vorbis / ADPCM audio clips.
- Preload short UI/interaction clips to avoid runtime audio decoding hitches.
- Limit concurrent active AudioSources to under 16 to conserve mobile CPU.

## Completion Report

Include:
- Audio/haptic scripts added or modified.
- Spatial rolloff settings configured.
- Controller haptic intensity verification.
