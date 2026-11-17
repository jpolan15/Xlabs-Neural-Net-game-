# ADR-025: Level 1 Becomes a Night Festival, with a Chapter Picker, Topic Cards and a Clearer Backprop Game

| Field | Value |
|---|---|
| ID | ADR-025 |
| Date | 2026-11-17 |
| Status | Accepted (built; sightline, budget and model-sheet checks pass; chapter jump, hunt and win beat played live in the editor; not yet ridden on a headset) |
| Deciders | Project owner, after a playtest round and the teacher's notes ("level one is too dark, an empty void"; "keep it night, but a Japanese-style night festival, lanterns, lit up"; "pick a section on the console and jump there"; "a card on your cart and confetti when you win"; "make sure none of the explanations or minigames are blocked") |
| Builds on | ADR-021 (v7 look pass), ADR-023 (Level 2 must never be mistaken for Level 1's night). No package changes. |

## Context

Playtesters found Level 1 a void next to Level 2's daylight ride; the backprop "drag the wire to the 3" game was unreadable from 40 m
(the 3 was a label, the hint text overprinted the answers, Pixel sat on the right of every board); beginners lost the thread at
jargon (bias, sigmoid, chain rule, activation, classifier); a demo had to sit through the whole ride to show one topic.

## Decision

1. **A night festival town** (`Tools/Editor/NeuralRideBuilder.Festival.cs`, shaders `Festival`, `FestivalGlow`): the track is an
   elevated tram line over a street 7 m below; lamp posts and lantern strings line it, lantern canopies cross it between stops, a
   vermilion gate with the stop's number and name stands 11-12 m before each stop, stalls and lit houses fill the street, a pagoda and
   a low skyline stand behind each network (never behind the diagram), sky lanterns rise, the moon is in `RideSky` (`theme.moon`).
   Each stop keeps its topic colour as its lantern colour. The Valley is a dark lantern-lit hillside (gold contours, a great lantern
   where the pink sun was) with retaining walls where the street meets it. Per region: one opaque and one glow mesh.
2. **Chapter picker** (`ChapterPicker`, `ChapterBoardView`, `NeuralRideBuilder.Chapters.cs`): at the dock the table shows six tiles
   (full ride, pixels, neuron, network, backprop, training); `RideDirector.SetStartSegment` places the pod where the previous segment
   ended and the ride runs on from there. Desk keys 1-6.
3. **The win beat** (`WinCelebrationView`, `TopicCardSlot`, `NeuralRideBuilder.Celebrate.cs`): fireworks over the town, Pixel's five
   brain lanterns light one by one, "PIXEL LEARNED ...!", and a topic card (title, big idea, four facts) stays up until GO, then flies
   onto the card rail on the tub's left rim; shooting a rail card opens it again.
4. **Backprop: cool the hot wires.** The six hottest wires carry big glowing orbs; a laser shot is one real gradient step
   (w <- w - eta dL/dw) and the table's meter shows the real softmax probability of 3 (5% to about 65% over six shots in the bake).
   It is played at the network view (30 m), not after rolling back to 40 m. No drag, no socket.
5. **Plain words**: the script drops bias, sigmoid, chain rule, activation and classifier, defines a neural network in one clause,
   says the 3 is labelled, says the weights start random; two lines were cut to pay for it. 13 lines re-voiced (Kokoro af_heart 1.0).
6. **Pixel rides at about 55 degrees right** (was 25) so it never covers a board; the blaster only fires while the cart moves.
7. **Budget**: `RideBudget.MaxDrawCalls` 100 -> 150. The count is a worst case (hidden boards and cards included); Meta's Quest 2
   guidance is about 200-300 draw calls. Triangles stay under 100k at every viewpoint.

## Consequences

- A headset ride must confirm frame time with the festival (risk: fill rate of the glow halos), the card's reading distance, and the
  laser on the orbs at 30 m. Level 2 is untouched; the hub shares `RideSky`, whose moon is off unless a theme turns it on.
