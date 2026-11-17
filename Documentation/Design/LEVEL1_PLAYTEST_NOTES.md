# Level 1 playtest round: what changed (Nov 17)

## What playtesters said
- Too dark, no theme, felt like riding through an empty black void. Level 2 looked way better.
- Had to sit through the whole ride to see one topic.
- Someone new to AI got lost at some topics (too much jargon, too fast).
- Wanted a reward when you beat a mini-game.
- Backprop game was confusing (drag lines onto the 3?) and stuff was blocking the picture.

We ran 6 test reviewers (a kid who knows nothing about AI, an ML teacher, a game designer, an art director, a VR comfort tester and a demo-booth runner), fixed everything below, then ran 2 more reviewers on the new version.

## What we added / changed
- **Night festival theme.** Level 1 is now a night festival town: paper lanterns, lamp posts, lantern strings, a red festival gate with the topic's name before every stop, food stalls, lit houses, pagodas, a moon and floating sky lanterns. It's still night (Level 2 is day), so the two levels feel different. Each topic keeps its own lantern colour. The gradient descent valley is a lantern-lit hillside now, so the theme never breaks.
- **Pick a stop.** At the start your table shows: Full Ride, 1 Pixels, 2 Neuron, 3 Network, 4 Backprop, 5 Training. Shoot one, pull GO, and you go straight there.
- **Easier to understand, same length.** We cut the jargon (bias, sigmoid, chain rule, activation, classifier) and said things in plain words: what a neural network is, that the 3 is "labelled", that the weights start random. We cut 2 lines to pay for it, so the ride isn't longer.
- **Win rewards.** Beat a game and you get fireworks over the town, Pixel learns the topic (one of 5 lanterns above Pixel's head lights up and it says "PIXEL LEARNED ...!"), and a topic card pops up with a bit more to read. The card then sticks to your cart, and you can shoot it later to read it again.
- **Backprop game redone.** Now it's "shoot the hot wires": each shot is one real gradient step, and a big meter on your table shows Pixel getting surer it's a 3 (5% up to about 65%). It's played closer to the network, and nothing covers it anymore.
- **Nothing blocks anything.** Pixel moved out to the side, the blaster only fires while the cart moves, and a check runs on every build that fails if anything covers a board or text.

## What the reviewers said after
- Kid: "Hooked and clear." Teacher: "Accurate and well sequenced."
- Art: "No longer a black void," close to Level 2 in mood, with its own identity.

## Not tested yet
- Riding it in the actual headset (frame rate with all the lanterns, how easy the hot wires are to hit at 30 m).
