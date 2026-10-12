# Model sheets

Every model in the Neural Ride from all sides, shot by `Convergence/Capture Model Sheets` (`Tools/Editor/RideModelSheets.cs`).
Tiles: row 1 front, front-right 45, right 90, back-right 135; row 2 back, back-left, left, front-left; row 3 top, low hero 3/4, rider eye 1.30 m, in the world.
"Front" faces the rider. The first ten tiles show the model alone; the last two show it in the ride. Do not edit by hand: re-run the capture.

Shot 2026-10-12 14:38 from Assets/Scenes/NeuralRide.unity in Edit mode (the saved scene; geometry made at run time shows as saved).

| Model | What it is | Tris | Renderers | Materials | Size (m) | Sheet |
|---|---|---:|---:|---:|---|---|
| `pod` | The rider's cart: tub, seat and dash with the levers. | 1,050 | 60 | 12 | 2.8 × 1.5 × 2.8 | [pod.png](pod.png) |
| `network` | The big neural network the track runs through: links, nodes and the rails, unlit as at the dock. | 6,618 | 3 | 3 | 101.7 × 60.0 × 185.6 | [network.png](network.png) |
| `network_lit` | The same network fully lit, as the finale shows it (LitUpTo 1.2, Ignite 0.35). | 6,618 | 3 | 3 | 101.7 × 60.0 × 185.6 | [network_lit.png](network_lit.png) |
| `neuron_core` | The neuron each stop docks at: tank, shell, labels and both input pipes. | 1,078 | 11 | 7 | 4.0 × 1.6 × 2.9 | [neuron_core.png](neuron_core.png) |
| `scanner_arch` | The scanner arch and belt the test objects ride through, with both sensor lamps. | 68 | 9 | 6 | 8.0 × 0.7 × 2.7 | [scanner_arch.png](scanner_arch.png) |
| `drone` | Test object: the friendly drone (fire: no). | 1,904 | 10 | 4 | 1.0 × 0.3 × 1.0 | [drone.png](drone.png) |
| `comet` | Test object: the ice comet. | 1,538 | 3 | 3 | 1.0 × 0.5 × 0.4 | [comet.png](comet.png) |
| `rock` | Test object: the rock. | 2,304 | 3 | 2 | 0.6 × 0.4 × 0.6 | [rock.png](rock.png) |
| `ice_chunk` | Test object: the rock with ice crystals. | 2,328 | 5 | 3 | 0.6 × 0.6 × 0.6 | [ice_chunk.png](ice_chunk.png) |
| `live_board` | The board of the four test cases and how each one went. | 32 | 16 | 7 | 0.8 × 1.1 × 0.8 | [live_board.png](live_board.png) |
| `diagram` | The input, neuron, output diagram with the live weights. | 354 | 13 | 8 | 2.0 × 1.3 × 1.3 | [diagram.png](diagram.png) |
| `equation` | The equation plate above the stop. | 4 | 2 | 2 | 3.6 × 0.9 × 1.2 | [equation.png](equation.png) |
| `loss_landscape` | The error landscape with the marble, trail and slope arrow. | 782 | 3 | 3 | 3.6 × 2.5 × 3.1 | [loss_landscape.png](loss_landscape.png) |
| `chapter_card` | The card of the briefing that is playing. | 46 | 5 | 3 | 4.8 × 2.0 × 0.4 | [chapter_card.png](chapter_card.png) |
| `explainer` | The diagrams shown under the chapter card while the Guide explains. | 6 | 3 | 3 | 4.2 × 1.2 × 0.3 | [explainer.png](explainer.png) |
| `finale` | The end panel and the credits. | 486 | 11 | 5 | 3.4 × 2.2 × 0.6 | [finale.png](finale.png) |
