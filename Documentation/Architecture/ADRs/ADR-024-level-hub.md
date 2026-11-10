# ADR-024: The Level Hub, a Scoreboard, and the Way Home

| Field | Value |
|---|---|
| ID | ADR-024 |
| Date | 2026-11-10 |
| Status | Accepted (built; EditMode and PlayMode tests green; hub to Level 2 and back played on desktop; not yet ridden on a headset) |
| Deciders | Project owner ("add a hub where I can choose the levels that I end up adding to the game"; "make it retentive, fun, demo-ready for Level 2") |
| Builds on | ADR-022 and ADR-023 (Level 2), ADR-021 (Level 1 v7). No package changes. |

## Context

The game had two levels and no front door: the headset booted into Level 1 (build scene 0), Level 1's end card led to Level 2, and
Level 2's led back to Level 1. To demo Level 2 the owner had to open its scene in the editor. More levels are planned, so the choice
of level has to be data, not scene wiring. For a booth, a reason to ride again helps: a visible best score.

## Decision

1. **A hub scene is build scene 0.** `Assets/Scenes/LevelHub.unity`, built by `Convergence/Build Level Hub`. Dark by the owner's choice: the
   rider sits on a dark plaza under Level 1's night sky (`Convergence/RideSky`: stars, a violet horizon glow, a neon grid as the
   ground), mountains in silhouette, rings of light on the ground, neon digits and words drifting at the sides, the title, and one
   card per level in an arc at eye height, 2.8 m away. The
   seat is the ride's: the XRI rig on a `PodSeat` (eye re-centred to 1.3 m), `RideHandLasers` and `DesktopSeat`, so a laser shot or a
   desk click picks a card exactly as it fires at anything in a ride. A card grows, glows and ticks when a laser is on it. A card with no scene says COMING
   SOON and only shakes.
2b. **One clean transition.** A shot starts loading the level in the background at once (`allowSceneActivation` off); the chosen card
   glides to 1.5 m in front of the rider while the other cards, the title and the help line sink and shrink away; from 0.25 s the view
   fades to black with the music (0.6 s); the level switches in only once the black is complete and the load is done, and its own
   `RideBoot` keeps the eyes covered until it is warm, then fades in. Each animation step is capped at 1/30 s, so the hitch of
   starting the load cannot skip the animation. Both black covers now take the highest sorting order (`short.MaxValue`): the boards
   and words sort above 0, and sorting order outranks the render queue, so before this a card or a board stayed bright on the black.
2. **The catalog is an asset.** `Assets/ScriptableObjects/LevelCatalog.asset` (`LevelCatalog`, Gameplay) lists the levels: badge, title,
   tagline, stops, scene name, minutes, accent colour. `LevelHubView` (Presentation) builds the cards from it when the hub loads, so
   adding Level 3 is: add an entry, add its scene to the build. The builder fills the catalog only when it is empty, never overwriting
   hand edits.
3. **A scoreboard on the device.** `LevelProgress` (PlayerPrefs) keeps, per level scene, rides finished, best stars, the most stars
   possible, the high score and the last ride. `LevelProgressRecorder` writes it when a ride completes (the sum of every game's stars
   and the director's score); `LevelProgressBoot` puts one on every loaded scene's `RideDirector`, so Level 1 is scored without a
   rebuild. The hub card shows "NOT RIDDEN YET - BE THE FIRST!", the best stars and high score, or, for the level just ridden,
   "NEW RECORD!" or "YOUR RIDE". Level 2's end card shows the ride's points and stars over the card. Shift+Backspace in the hub (desk)
   wipes the board for a new group.
4. **Every level can go home.** `LevelHubNav.GoToHub()`: H on the desk (`DesktopSeat`), or holding the left controller's menu button
   for 1.5 s (`RideHandLasers`; it ticks every half second while held, a tap does nothing). Level 2's GO at the end now loads the hub
   ("PULL GO TO PICK YOUR NEXT LEVEL"); Level 1's still leads into Level 2. When the hub is not in the build nothing happens, and an end
   card whose next scene is missing just stays up.
4b. **The way home is faded too.** Every scene switch out of a ride (H, the menu hold, an end card's GO) goes through
   `LevelHubNav.GoTo`, which asks the scene's `RideBoot` (it sets the `LevelHubNav.FadeOut` hook while enabled) to fade the view and
   the sound to black over 0.6 s; under the black every sound is stopped and the volume put back, so the next scene's boot reads the
   real level, then the scene switches. A second press while leaving does nothing. The hub opens behind black, waits 0.15 s for its
   first frames to settle, then fades in with its music (0.7 s). A scene with no `RideBoot` switches at once, as before.
5. **Build order is one function.** `OrderBuildScenes()` (hub, Level 1, Level 2, then the rest) is called by all three builders, so
   rebuilding a level never pushes the hub out of scene 0.

## Consequences

- The Quest build starts in the hub. In the editor, press Play in `LevelHub.unity` (or in a level's scene to skip the hub).
- Level 1's scene was not rebuilt: it gets the way home and the scoreboard through code already in its scene (`RideHandLasers`,
  `DesktopSeat`, the recorder added at load), but not the end-card score banner until its next rebuild.
- PlayerPrefs are shared by the editor and its test runs: a PlayMode ride writes the scoreboard. Wipe it before a demo
  (Shift+Backspace in the hub, or `LevelProgress.ClearAll()`).
- Tests: `Tests/EditMode/Ride/LevelHubTests.cs` (catalog, build order, built scene, Level 2 ends in the hub, scoreboard rules),
  `Tests/PlayMode/Gameplay/LevelHubPlayTests.cs` (cards built, locked card stays, a shot loads Level 2, going back).
