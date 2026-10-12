# Team Briefing: Level 1 — How a Machine Learns to Read a Digit

Plain words, so you can explain it and answer questions. (The old spaceship/AURA version is in git history.)

## The one-sentence version

You ride inside a real AI and watch it go from guessing wrong to reading a handwritten 3, and you help it learn.

## Why it is real, not a cartoon

- The AI was really trained on **60,000 handwritten digits** (the famous MNIST set). It gets **92 %** of 10,000
  digits it has never seen right.
- Before training it calls the 3 a **1**. That is its real answer, not a scripted one.
- What lights up is what the math computes. The puzzle is graded by the same code that runs the network.
- The hill you drop down in the Valley is the real "how wrong" score along the real training run, including the
  first step that was too big and flew past the bottom.

## The ride, stop by stop (about 5 minutes)

| Stop | The idea | What the rider does |
|---|---|---|
| The Page | A computer can't see a picture, only numbers | Pull the orange lever |
| The Grid | Every pixel becomes a number: blank 0, ink 1. Shrunk to 7×7 = 49 numbers | Watch the pillars |
| One Neuron | Each pixel has a weight: plus = "I want ink here", minus = "I don't". Add it all up; past the line, it fires | **Paint the weights** so it fires for 1s, not 0s. The panel shows *why* each digit passed or failed |
| The Network | Many neurons in layers: 49 in, 16 hidden, 10 answers | Watch the 3 travel through |
| Wrong | It has never been taught; its weights are random | It says "1" |
| Rewind | **Backpropagation**: the mistake flows back; the more a wire caused it, the brighter it burns | The cart rolls backwards. **Point at the brightest wires** |
| The Valley | **Gradient descent**: height = how wrong; each step goes downhill | Hold on: the ride drops by itself, overshoots once, then settles |
| Right | Same 3, new weights | It says "3". Then: do the same with words and billions of weights, and you get the AI you can talk to |

## Questions people ask

- **"Is it really learning in the headset?"** The training happened beforehand on a normal computer (3,000
  rounds of math over all 60,000 images). The ride shows the real before and after, and the real path in between.
- **"Why only 49 pixels?"** The real digits are 28×28 = 784 pixels. We average them down to 7×7 so you can see
  every single connection. It still gets 92 % right.
- **"What is backpropagation?"** Working out, for every wire, how much it caused the mistake, so you know which way
  to nudge it.
- **"What is gradient descent?"** Nudging every weight a little bit downhill on the "how wrong" landscape, over and over.

## Running the demo

- Unity: open `Assets/Scenes/NeuralRide.unity`, press Play (or run it on the Quest).
- **Space** skips a line or hurries a stop, **R** restarts for the next person, **G** gentle mode for anyone
  nervous about drops (half the drop, slower).
- Anything orange can be grabbed.

## Still to check before Ignite (April 2027)

- Ride it in the headset: comfort on the backward roll and the drop, text readability, both hands, smooth frame rate.
- The voice is synthetic for now; record a human narrator once the script is final.
- Music "Telstar": confirm we are allowed to use it in public, or swap it.
