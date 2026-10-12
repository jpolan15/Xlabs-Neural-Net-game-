# Model sheets

Every model in the Neural Ride from all sides, shot by `Convergence/Capture Model Sheets` (`Tools/Editor/RideModelSheets.cs`).
Tiles: row 1 front, front-right 45, right 90, back-right 135; row 2 back, back-left, left, front-left; row 3 top, low hero 3/4, rider eye 1.30 m, in the world.
"Front" faces the rider. The first ten tiles show the model alone; the last two show it in the ride. Do not edit by hand: re-run the capture.

Shot 2026-10-12 19:11 from Assets/Scenes/NeuralRide.unity in Edit mode (the saved scene; geometry made at run time shows as saved).

| Model | What it is | Tris | Renderers | Materials | Size (m) | Sheet |
|---|---|---:|---:|---:|---|---|
| `pod` | The rider's cart: tub, seat and the dash with GO, the subtitle screen and the region tag. | 720 | 23 | 8 | 2.8 × 1.5 × 2.8 | [pod.png](pod.png) |
| `weight_grid` | Region 2: the 7x7 weight grid on the dash the rider paints (+ cyan, - amber, orange frame = press). | 312 | 5 | 4 | 0.5 × 0.3 × 0.3 | [weight_grid.png](weight_grid.png) |
| `end_card` | The end card with the next level and the credits. | 352 | 4 | 2 | 3.5 × 1.2 × 0.0 | [end_card.png](end_card.png) |
| `rails` | The two slate rails under every leg of the track. | 1,136 | 1 | 1 | 152.2 × 20.2 × 308.5 | [rails.png](rails.png) |
| `page` | Region 0: the sheet with the real handwritten 3 (test digit) on its desk. | 8 | 4 | 4 | 40.0 × 7.6 × 24.0 | [page.png](page.png) |
| `pixel_field` | Region 1: the 3's 784 pixels as pillars, height = ink, with three values. | 7,912 | 8 | 4 | 13.2 × 13.0 × 7.6 | [pixel_field.png](pixel_field.png) |
| `input_grid_7x7` | Region 1: the 49 numbers the machine gets (4x4 averages) and their sign. | 534 | 4 | 4 | 10.4 × 11.3 × 6.7 | [input_grid_7x7.png](input_grid_7x7.png) |
| `neuron_amber` | Region 2: the big amber neuron with its 49 dendrites. | 1,456 | 3 | 3 | 8.5 × 8.5 × 11.2 | [neuron_amber.png](neuron_amber.png) |
| `why_readout` | Region 2: the why panel: the digit on show, ink x weight, the total against the line, all eight digits. | 1,228 | 30 | 9 | 2.4 × 1.1 × 0.1 | [why_readout.png](why_readout.png) |
| `network_untrained` | Region 3: the real 49-16-10 network with its untrained weights, wires by sign and size, neurons by activation. | 13,718 | 30 | 6 | 42.7 × 15.2 × 4.9 | [network_untrained.png](network_untrained.png) |
| `output_labels` | Region 3: the ten answers 0 to 9 on solid tags, and the guess glow. | 162 | 21 | 3 | 1.2 × 12.6 × 0.0 | [output_labels.png](output_labels.png) |
| `network_wrong` | Region 4: the untrained network flashing red, with the wrong guess. | 13,748 | 32 | 6 | 42.7 × 15.8 × 4.9 | [network_wrong.png](network_wrong.png) |
| `blame_wires` | Region 5: every wire burning by its real blame, and the brightest six as pointable targets. | 13,300 | 2 | 2 | 39.5 × 14.8 × 4.0 | [blame_wires.png](blame_wires.png) |
| `valley_terrain` | Region 6: the ground of real loss along the training path, with contours, the trail and the hop posts. | 6,070 | 3 | 3 | 83.2 × 32.5 × 92.0 | [valley_terrain.png](valley_terrain.png) |
| `valley_set` | Region 6: the deck at the rim, the sign, and the pink sun. | 850 | 5 | 5 | 81.0 × 70.0 × 130.0 | [valley_set.png](valley_set.png) |
| `network_trained` | Region 7: the same network with its trained weights, in gold. | 13,626 | 24 | 6 | 33.3 × 14.8 × 29.0 | [network_trained.png](network_trained.png) |
| `finale_digits` | Region 7: 60 real test digits with the trained network's real answers (wrong ones red). | 7,100 | 2 | 1 | 20.3 × 4.5 × 20.6 | [finale_digits.png](finale_digits.png) |
| `accuracy_sign` | Region 7: the real accuracy and training count. | 94 | 2 | 2 | 18.2 × 2.3 × 15.5 | [accuracy_sign.png](accuracy_sign.png) |
| `words` | Region 7: the teaser, the same machinery with words. | 48 | 2 | 2 | 9.4 × 1.5 × 7.9 | [words.png](words.png) |
