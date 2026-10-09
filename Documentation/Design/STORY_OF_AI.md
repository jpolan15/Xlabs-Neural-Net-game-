# The Story of AI on the Ride

The history the ride teaches, mapped to its stops. Source: `.agents/tasks/TASK_IGNITE_RIDE_V3_MASTER_PLAN.md` (WP4, WP9). Dates and names come from that plan and were sanity-checked against general knowledge, not primary sources. The 1969 Minsky and Papert book is a cause of the first AI winter (mid 1970s) rather than the start of it, so the narration only says that one neuron can never solve XOR.

## Story logic

The pod flies through a large, glowing, layered network: the ship's brain. Each stop docks at one neuron in that network. Solving a stop lights up that neuron's connections. At the end, the whole network glows.

## Chapters and stops

| Year | What happened | Where it plays | Why it fits |
|---|---|---|---|
| 1943 | McCulloch and Pitts build the first artificial neuron: add the signals, fire past a threshold. | Travel 1 briefing, then Stop 1 (one signal, trigger) | Exactly the stop 1 trigger. |
| 1958 | Rosenblatt's perceptron: each input gets its own weight. | Travel 2 briefing, then Stop 2 (two signals) | The player is the learning rule. |
| 1960 | Widrow and Hoff: measure the squared error, step downhill. | Travel 3 briefing, then Stop 3 (LEARN) | Linear neuron, squared error, batch gradient descent. |
| 1969 | Minsky and Papert: one neuron cannot solve XOR. Interest and funding fell in the following years (the first "AI winter"). | Outro teaser | Sets up the XOR stop. |
| 1986 | Backpropagation and hidden layers fix it. | Outro teaser now; a future stop (restructure note 1) | The next restructure. |

## Restructure notes (later, not this week)

1. Add the XOR stop as chapter 4: 1969 to 1986 backpropagation, a hidden layer, and error flowing backwards, shown visibly.
2. Add a "scale" finale: 2012 deep learning, 2017 transformers, 2022 ChatGPT. Same ideas, billions of weights.
3. Consider letting the player start the big network dark and light it chapter by chapter.
