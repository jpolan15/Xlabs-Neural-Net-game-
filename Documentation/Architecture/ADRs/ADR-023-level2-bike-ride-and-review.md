# ADR-023: Level 2 v2, a Daylight Bike Ride, and a Professor's Review of the Story

| Field | Value |
|---|---|
| ID | ADR-023 |
| Date | 2026-10-31 |
| Status | Accepted (built, played end to end by the bot on desktop, EditMode tests green; not yet ridden on a headset or on the exercise bike) |
| Deciders | Project owner ("Level 2 looks the same as Level 1"; "make it a bike ride: on the school's exercise bike or on a chair"; "review it as a master AI professor: logic, pauses, fun games") |
| Builds on | ADR-022 (Level 2, the Word Machine). Its honesty rules, data and games hold. |

## Context

Sat down cold, the owner would have guessed Level 2 was Level 1: the same night sky, the same neon grid floor, the same pod and rails.
The school's VR room has an exercise bike. The owner wants Level 2 to be a bike ride in the headset; a rider may sit on the real bike
and pedal, or sit on a chair and simply ride along. They also asked for an expert review of the story: does it make sense, does a
student who has never met AI follow it, are the pauses long enough, are the games fun and not repeats of Level 1's.

## Decision

1. **A daylight world, never Level 1's night.** Level 2 has its own sky (`Convergence/DaySky`): a gradient, a sun, drifting clouds
   from one tileable noise, and a meadow on the ground plane that fades into the haze exactly as the fog fades objects. The stops are
   a day passing: morning at the dock, noon at the chopper and the observatory, golden hour at the theatre, late afternoon at the fair,
   sunset on the story road, dusk at the finish. The sky still blends stop to stop through `RegionAtmosphere`; while a board explains,
   the sky only softens (`blackboardDim` 0.9) and the meadow stays green (`RideTheme.blackboardFloor`, 1 here, 0.5 in Level 1).
2. **The pod is a bike.** `PodL2` keeps Level 1's dash (subtitles, topic, GO lever) and game table at their tuned distances, mounted on a
   bright cargo bike: the dash is the lid of the front box, the handlebars sit below the dash's sight lines (the GO lever stays in plain
   view), the crank and pedals turn between the knees, the wheels roll. `BikeView` turns wheels and pedals exactly as fast as the ride
   moves and stops them when it stops (a rider on the real bike pedals along); it turns the handlebars when the story road changes lane.
   The rider's view never tilts or bobs. No legs are drawn: the rider's own body is on the real bike or the chair.
3. **A road through picture-book country.** `Track/TrackWorld` lays one bike path from the dock to the end (dashed line, verges, trees,
   firs, lamps, far hills: one mesh) and keeps the roadside clear where boards and games stand. Each stop has its own landmark: a start
   arch and a windmill, the chopper sawmill with its spinning saw and letter blocks, the word observatory with balloons outside the
   galaxy's cone, an open-air theatre whose dark curtain sits right behind the attention words, the odds fair with tents and a big wheel,
   farm fields with a signpost at the story forks, a finish arch with rising balloons.
4. **Readable in daylight.** World text uses the font with a dark outline (`Level2Kit.outline`); glows and ribbons are alpha-blended, not
   additive (additive light vanishes on a bright sky); every board is nearly opaque; scenery words sort behind boards; the stamp and
   popups sort in front of world text; no game text sits low enough for the road to hide it; the finale has its own big screen.
5. **The story, reviewed.** Gaps a teacher would flag are filled, each with its own picture: why tokens (no dictionary holds every word,
   a letter means too little; v1 said "a computer can't read letters", which is not true); where the 768 numbers come from (a table the
   machine learned in training, like Pixel's weights); what attention does with its shares (each word mixes in the others' meaning, so
   "creature" now means a fluffy, blue creature), and that GPT-2 has 144 heads of which we picked the one that links words the way a
   person would (said aloud, since the head was chosen by search); that a low probability is not a wrong word (too, so, scared fit as
   well as hungry); that the surprise it learned from is its loss; that writing draws the next word from the odds like loaded dice, which
   is why a chatbot can answer twice differently, and what temperature does to the dice; the whole loop recapped at the end; that big
   chatbots add training to be helpful but run the same trick; and the warning every student should leave with: the machine picks words
   that sound likely, not words that are true, so always check. A three-word story that stops on "a" ends in "..." (`StoryMoment.EndsOpen`).
6. **Slower.** In-line pauses are about twice as long and every explaining line breathes about a second after it ends; all 47 lines
   are re-voiced with the temporary Kokoro voice (Whisper word error at most 5.6 %). Level 2 speaks 374 s in all (was about 300).
7. **The games stay, the story game becomes a bike game.** The five games already differ from Level 1's (ADR-022). At the story forks the
   rider now points at a word and pulls the trigger to ride down that lane: the bike swings into it and the handlebars turn.

## Consequences

- The ride is longer: a quick, perfect rider takes about 9 minutes; a person 10 to 12. Space still skips a line, Shift+Space hurries a game.
- Explainers hear every line of their stop's sequences straight from `Level2NarrationData` (the builder no longer lists them by hand),
  and a test checks it: v2's first build missed four new lines, so their steps fired late, all at once.
- Not verified: the headset (comfort of riding through the countryside, the bike under the rider's real body, draw calls and triangles of
  the countryside on a Quest 2), and the real exercise bike. The voice is still the temporary synthetic one.
