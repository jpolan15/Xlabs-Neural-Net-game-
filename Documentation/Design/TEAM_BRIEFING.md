# Team Briefing: The Neural Ride (read this before presenting)

Written in plain words so everyone in the group can explain it and answer questions.

## The one-sentence version

You sit in a little pod and ride into the brain of a ship's AI. At three stops you teach it, step by step, that **an AI is just math**, ending with how it **teaches itself** by rolling a ball downhill.

## What we changed (simple)

**Before:** one dark, cluttered room with one big console. Lots of tiny labels, glitchy visuals (a pink cube, a blown-out white ball), and error messages on screen. Too many ideas at once, so people skipped the explanations.

**Now:** a short ride with three stops. Each stop teaches one idea and gives you one new control.

| Stop | Idea | What you do | What you see |
|---|---|---|---|
| Start | Begin | Pull the orange GO lever | The pod rides into the AI's brain |
| 1 | A neuron multiplies a signal by a weight and fires if it passes a trigger | Slide the TRIGGER lever | Rocks get zapped. Slide too low and the friendly drone gets zapped too |
| 2 | Many inputs add up (an OR gate) | Three levers: ROCK, ICE, TRIGGER | The ICE pipe starts wired backwards and drains the core. Fix it and every picture gets a check |
| 3 | Learning: the AI fixes its own mistakes | Pull LEARN and pick slow, good or crazy | A ball rolls down an "error hill" while the levers move by themselves |

Other changes:
- **One rule for color:** orange means "you can touch this". Nothing else is orange.
- **Everything is plain words and icons.** The screen tells you what to do in a short sentence.
- **After each stop the math equation builds itself** from the numbers you just set, so you feel it first and see the math second.
- **Voice and music:** 15 narrated lines (an AI voice, not a person) with subtitles, plus background music.
- **Works without a headset:** hold right mouse to look around, drag levers with the left mouse.
- **A real neural-network picture** (circles and lines) is on screen at every stop. See below.

## Where is the neural net? (they will ask)

Look at the **circles-and-lines diagram** on the left of each stop:

`[ROCK input] ──×weight──▶ ( neuron ) ──▶ [FIRE output]` (stop 2 adds an ICE input).

- Each circle on the left is an **input**. The big circle is the **neuron**. The circle on the right is the **output**.
- A **thicker line** means a bigger weight. A **cyan line** pushes the neuron to fire. An **amber line** holds it back (a negative weight).
- The same neuron is also drawn as a machine: sensors, pipes, and an energy tank with a trigger line.

**Be honest about this:** what we show is **one neuron**, which is the smallest possible neural network and the building block of every big one. A "network" means lots of neurons connected in layers. Our planned XOR stop would show a real multi-neuron network.

## What is real and what is not

**Real:**
- Whether a stop is solved is decided by the project's real math code, which checks every case. Nobody wrote "stop solved".
- In stop 3 the computer really works out the slope of the error and steps downhill. It starts at weights (-1.5, 1.5) and ends near (0.67, 0.67), the correct answer, in about 14 steps.
- Slow, good and crazy learning rates really behave that way. Crazy flies off the hill.

**Simplified on purpose:**
- It is a tiny neuron with two inputs. Real AIs have millions or billions of weights.
- The "stream of objects" is a picture of the four test cases (drone, comet, rock, rock+ice).

## What we plan to add (simple)

1. **Intro and ending scenes:** a short ship scene before the ride, and after it the ship's laser uses the brain you trained. Then a 3-question quiz and credits.
2. **A fourth stop, the "impossible one" (XOR):** one neuron cannot solve it, so the player connects neurons into a real network with a hidden layer. This is also where **backpropagation** (error flowing backwards through layers) becomes visible.
3. **Comfort for VR:** a vignette while the pod moves, and comfort settings.
4. **Polish:** nicer art and sound, and testing on a real Quest headset.
5. **Playtest:** try it with 3 to 5 classmates and fix what confuses them.

## Questions your teacher might ask (and simple answers)

**What is a neural network?** A bunch of simple "neurons" connected together. Each neuron takes numbers in, multiplies each by a weight, adds them up, and decides whether to fire.

**What is a weight?** How much a neuron listens to one input. Big positive: listens a lot. Zero: ignores it. Negative: does the opposite. (The backwards ICE pipe is a negative weight.)

**What is the trigger (threshold)?** How much total signal is needed before the neuron fires. In the math it is the "bias", the same number with the opposite sign.

**Why does the neuron fire for a rock but not the drone?** Rock gives signal 1 × weight 1 = 1, which is above the trigger 0.5, so it fires. The drone gives 0, which is below 0.5, so it does not.

**What is loss (the "error hill")?** A number for how wrong the AI is. High on the hill is very wrong. The bottom is as right as it can get.

**What is gradient descent?** Measure which way is downhill (the slope), take a small step that way, and repeat until you reach the bottom.

**What is the learning rate?** How big each step is. Too small: takes forever. Too big: jumps over the bottom and gets worse. "Good" lands at the bottom.

**Where is backpropagation?** It runs inside stop 3: the trainer does a forward pass and then a backward pass to find the slope for each weight. With one neuron that is a single step, so we do not teach it visually yet. It becomes visible in the planned XOR stop with hidden layers.

**Why can't one neuron do XOR?** XOR means "fire for one input or the other, but not both". One neuron can only draw a single straight dividing line, and no single line separates those four cases. Two layers of neurons can.

**Is this like ChatGPT?** Same core ideas: weights, and training by gradient descent with backpropagation. ChatGPT-style models just have billions of weights and learn from huge amounts of text.

**Is the AI really learning, or is it scripted?** The weights really change by the real gradient-descent math. What is scripted is the story and the instructions around it.

**Who made the voice and music?** The voice is an AI voice (synthesized), not a person. The music is a track our teammate said we have permission to use. (Be ready to say that plainly.)

## Demo cheat sheet (about 5 minutes)

1. Open `Assets/Scenes/NeuralRide.unity` and press Play. Let it settle for 2 seconds.
2. **GO:** pull the orange lever. Point out the tunnel and the circles-and-lines neural net.
3. **Stop 1:** slide TRIGGER down to 0.5. Say "weight times input, compared to a trigger". If you want a laugh, slide it below zero first and zap the drone.
4. **Stop 2:** set ROCK to 1, ICE to 1, TRIGGER to 0.5. Say "now two inputs add up: that is an OR gate".
5. **Stop 3:** pull LEARN. Watch the ball roll. Then replay with the knob on CRAZY to show the overshoot. Say "that is gradient descent".
6. If the headset acts up, switch to the mouse. Everything works from the desk.

## Known gaps (do not claim these)

- Intro and ending scenes, quiz and credits are not built yet.
- The XOR stop and the hidden layer are not built yet.
- The tunneling vignette and comfort settings are not built yet.
- It has not been run on a headset yet, and there are no frame-rate numbers.
- One old Level 01 automated test fails. It is not part of the ride, and we have not found the cause.
