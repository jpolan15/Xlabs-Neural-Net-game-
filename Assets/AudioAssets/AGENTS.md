# Moved

> **This directory is deprecated.** The canonical audio assets location is now `Assets/_Project/Audio/`.
>
> Add new audio assets to `Assets/_Project/Audio/`, not here.
>
> This directory is retained to avoid breaking any existing `.meta` file references.  
> Do not delete it until all `.meta` GUIDs have been verified as unreferenced.

## Migration status (2026-09-14)

At time of migration, this directory contained no audio asset files — only this `AGENTS.md`.  
No `.meta` file references needed updating.

## Historical import specs (for reference)

These specs apply to `Assets/_Project/Audio/`:

- Compression: Vorbis for ambience (quality 50–70%); ADPCM for short interaction SFX (clicks, snaps, detents).
- Channels: Mono for all 3D spatial sources; Stereo only for 2D UI or non-diegetic music.
- Sample rate: 44.1 kHz or 48.0 kHz, consistent across all assets.
- Do not import uncompressed 32-bit float WAV > 10 MB into production builds.
- Do not use multi-channel surround tracks for spatial objects.

See `Assets/_Project/Audio/AGENTS.md` for current conventions.


