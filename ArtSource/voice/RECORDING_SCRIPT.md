# Level 1 narration: recording script

One take per line. Read it like you are explaining something cool to a friend: calm, curious, clear, a little faster than everyday talk (think 3Blue1Brown). A " / " is a short breath. Leave half a second of silence before and after each take. A phone in a quiet room is good enough (a closet full of clothes is a great vocal booth).

Name each file after its number (for example `vo_ride_119.wav`, or .m4a from a phone) and put them all in one folder. Claude then levels the volume, trims the silence and syncs the subtitles to your timing (that import step gets built when the recordings exist).

| File | Line |
|---|---|
| | **E0, the dock: every one of these is a three, and Pixel can't read any of them (intro and dock line).** |
| `vo_ride_119` | Every one of these is a three. / Different shapes, / different slants, / and you read them all without thinking. |
| `vo_ride_120` | This is Pixel. / Pixel can't read a single one. |
| `vo_ride_121` | So let's build a brain for Pixel, / and teach it, / right here. |
| `vo_ride_122` | Pull the orange lever, / or press A. |
| | **E1, pixels, then draw your 3 (the drawing is the input for the rest of the ride).** |
| `vo_ride_123` | To a computer, / a picture is a grid of tiny squares, / called pixels. |
| `vo_ride_124` | And every pixel is just a number. / Zero for blank. / One for full ink. |
| `vo_ride_125` | We'll use seven by seven. / Forty-nine numbers. / That's all the network ever sees. |
| `vo_ride_126` | Now draw your own three. / Hold the trigger, / and write on the board. / Shoot DONE when you're happy. |
| `vo_ride_127` | Bigger! / Fill the board. |
| `vo_ride_128` | Looks like a three to me. / Shoot DONE. |
| `vo_ride_129` | That's your three, / as forty-nine numbers. / Hang on to it. |
| `vo_ride_130` | Noise incoming. / Zap it! |
| | **E2, one neuron, then spray its weights.** |
| `vo_ride_131` | This is a neuron. / It holds one number, / its activation. / The bigger the number, / the brighter it glows. |
| `vo_ride_132` | It's connected to all forty-nine pixels, / and every connection has a weight. |
| `vo_ride_133` | A positive weight says: / ink in this spot means three. / A negative weight says: / ink in this spot means not a three. |
| `vo_ride_134` | Multiply each pixel by its weight, / add them up, / and add a bias. / A sigmoid squishes the total between zero and one. / High means the neuron fires. |
| `vo_ride_135` | Your turn. / Paint the weights so this neuron fires for threes, / and only threes. / Right hand paints positive, / left hand negative. / Hold the trigger and sweep. |
| `vo_ride_136` | It fired for the wrong digit. / Paint negative where that one has ink. |
| `vo_ride_137` | A three slipped past. / Paint positive where the threes have ink. |
| `vo_ride_138` | You just built a three detector, / by hand. |
| | **E3, the network: send your 3 through it.** |
| `vo_ride_139` | One neuron spots one pattern. / Put sixteen side by side, / and you have a layer. / Each one can learn a different stroke. |
| `vo_ride_140` | Then ten output neurons, / one for each digit. |
| `vo_ride_141` | Connect every neuron to the next layer, / and that's a neural network. / Nine hundred and forty-four weights. |
| `vo_ride_142` | Shoot your three, / and send it in. |
| | **E4, the guess and the loss (the guess itself is on the network: it depends on the rider's drawing).** |
| `vo_ride_143` | Pixel's guess is in. / But look how unsure it is. / Every weight is still random, / so it's only guessing. |
| `vo_ride_144` | What we want is the three's neuron at one, / and every other at zero. |
| `vo_ride_145` | Add up how far off it is, / and you get one number: / the loss. / Training means just one thing. / Make the loss small. |
| | **E5, backpropagation, then the hot-wire hunt on the rider's own 3.** |
| `vo_ride_146` | So which weights should change, / and which way? / The three's output wants to go up. / Every other output wants to go down. |
| `vo_ride_147` | Those wishes flow backwards, / layer by layer, / with the chain rule. / That's backpropagation. / It gives every weight a gradient: / which way to move, / and how much. |
| `vo_ride_148` | The hottest wires matter most. / Grab each one, / and drag it to the three, / before it cools. |
| `vo_ride_149` | Quick, / they're cooling! |
| `vo_ride_150` | Each wire you moved was one real gradient step. / Now imagine every weight, / over thousands of digits. |
| `vo_ride_151` | Stage clear. / Pull GO. |
| | **E6, gradient descent, then the training game: real training, live.** |
| `vo_ride_152` | Picture the loss as a landscape. / Where you stand is a choice of weights. / The height is how wrong they are. |
| `vo_ride_153` | The gradient points uphill, / so take a step the other way. / That's gradient descent. |
| `vo_ride_154` | The size of the step is the learning rate. / Too big, / and you leap clean over the valley. / Too small, / and you'll be here all day. |
| `vo_ride_155` | This valley is Pixel's real loss. / Pick a step size, / and hold its button to train. / Every step learns from thirty-two real digits. |
| `vo_ride_156` | Way too big! / The loss exploded. / Let's undo that. / Try a smaller step. |
| `vo_ride_157` | It's learning, / but painfully slowly. / Try a bigger step. |
| `vo_ride_158` | That's the one. / Watch the loss go down. |
| `vo_ride_159` | Pixel is trained. / You just ran gradient descent on a real neural network. |
| | **E7, Pixel reads the rider's own 3 with the weights the rider trained.** |
| `vo_ride_160` | Now, / the real test: / the three you drew. / Pixel says... |
| `vo_ride_161` | Three! / It can read your handwriting. |
| `vo_ride_162` | Not quite. / Your three is unusual. / So Pixel studies it, / a few more steps... |
| `vo_ride_163` | And now it's a three. |
| `vo_ride_164` | On five hundred digits it has never seen, / Pixel gets almost nine out of ten right. |
| `vo_ride_165` | That's how every neural network learns. / Guess. / Measure the loss. / Backpropagate. / Step. / Repeat. |
| | **Bonus: stump Pixel.** |
| `vo_ride_166` | Bonus round. / Draw the digit on the screen, / and shoot DONE. / Can Pixel read it? |
| `vo_ride_167` | Got it! |
| `vo_ride_168` | Fooled it! / Even trained networks make mistakes. |
| `vo_ride_169` | Not bad, / for nine hundred and seventy numbers. |
| | **E8, tokens: the same idea, for words.** |
| `vo_ride_170` | Language works the same way. / Text gets chopped into pieces called tokens. |
| `vo_ride_171` | Each token becomes a list of numbers, / just like your pixels. |
| `vo_ride_172` | A far bigger network, / trained the same way, / predicts the next token. / That's how a chatbot writes. |
| `vo_ride_173` | Next time, / Pixel learns to read words. |
