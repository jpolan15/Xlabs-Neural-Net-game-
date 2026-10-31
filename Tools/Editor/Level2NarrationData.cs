using System.Collections.Generic;
using Convergence.Gameplay.Ride;

namespace Convergence.EditorTools
{
    /// <summary>
    /// The narration script of Level 2, "Inside the Word Machine" (TASK_LEVEL2_WORD_MACHINE): the same Line format as Level 1's
    /// (RideNarrationData: [p0.8] is a pause, {cue} opens a clause and names what the views show while it is spoken), one narrator, the
    /// Guide. Every number it says is the real GPT-2's: 768 numbers a word, 50,257 words, 124 million weights, the odds the
    /// bake printed. ArtSource/voice/render_voices.py reads this file next to RideNarrationData.cs. Development tooling only.
    /// </summary>
    internal static class Level2NarrationData
    {
        public static readonly RideNarrationData.Line[] Lines =
        {
            // The dock: last time, this time, the five stops.
            new RideNarrationData.Line(RideLine.L2Hello, "{chat}Last time, [p0.1] you taught Pixel to read numbers. [p0.2] Now Pixel wants to talk.", 0.2f),
            new RideNarrationData.Line(RideLine.L2Chatbot, "{type}Type a question into a chatbot, [p0.15] and an answer comes back, [p0.1] one word at a time. [p0.25] {inside}Today, [p0.1] we ride inside the machine, [p0.1] and watch how.", 0.2f),
            new RideNarrationData.Line(RideLine.L2Stops, "{stops}Five stops: [p0.15] tokens, [p0.15] meaning, [p0.15] attention, [p0.15] prediction, [p0.15] and writing. [p0.25] {real}Everything you'll see comes from a real language model, called GPT-2.", 0.2f),

            // Stop 1, tokens: chop the words.
            new RideNarrationData.Line(RideLine.TokChop, "{sentence}A computer can't read letters. [p0.2] So the machine's first job is to chop your words into pieces. [p0.25] {chop}The pieces are called tokens.", 0.2f),
            new RideNarrationData.Line(RideLine.TokNumbers, "{ids}Every token gets a number, [p0.1] like a name tag. [p0.2] These are GPT-2's real ones.", 0.2f),
            new RideNarrationData.Line(RideLine.TokRare, "{rare}Common words stay whole. [p0.15] Rare words get chopped smaller. [p0.25] {boss}This one becomes eleven tokens!", 0.2f),
            new RideNarrationData.Line(RideLine.TokSliceGo, "{slice}Your turn: [p0.1] be the chopper. [p0.2] Swipe your laser through the glowing seams, [p0.15] or shoot them. [p0.2] Leave whole words alone.", 0.1f),
            new RideNarrationData.Line(RideLine.TokWhole, "That one is a single token. [p0.1] Leave it whole.", 0.1f),
            new RideNarrationData.Line(RideLine.TokBoss, "The big one! [p0.1] Swipe all the way through.", 0.1f),
            new RideNarrationData.Line(RideLine.TokDone, "Chopped. [p0.15] The machine never sees letters, [p0.1] only these pieces.", 0.1f),

            // Stop 2, meaning: the word galaxy.
            new RideNarrationData.Line(RideLine.EmbName, "{name}A token's number is just a name tag. [p0.2] The number for cat tells you nothing about cats.", 0.2f),
            new RideNarrationData.Line(RideLine.EmbSpace, "{space}Now every word is a point in space. [p0.25] {close}Words that mean similar things end up close together.", 0.2f),
            new RideNarrationData.Line(RideLine.EmbSquash, "{squash}Inside the machine, [p0.1] each word becomes seven hundred and sixty-eight numbers. [p0.2] We squashed them down to three, [p0.1] so you can see.", 0.2f),
            new RideNarrationData.Line(RideLine.EmbMath, "{math}Even directions mean something. [p0.2] Start at king, [p0.1] subtract man, [p0.1] add woman. [p0.25] {queen}You land on queen.", 0.2f),
            new RideNarrationData.Line(RideLine.EmbSortGo, "{sort}Your turn: [p0.1] sort the loose words into their neighbourhoods. [p0.2] Point at a word, [p0.1] hold the trigger, [p0.1] and carry it home.", 0.1f),
            new RideNarrationData.Line(RideLine.EmbWrong, "Not quite. [p0.1] That word belongs somewhere else.", 0.1f),
            new RideNarrationData.Line(RideLine.EmbDone, "That's what the machine does: [p0.1] similar words end up close.", 0.1f),

            // Stop 3, attention: the spotlight hall.
            new RideNarrationData.Line(RideLine.AttMeaning, "{sentence}Words get their meaning from the words around them. [p0.25] A fluffy blue creature isn't just any creature.", 0.2f),
            new RideNarrationData.Line(RideLine.AttLookBack, "{look}So each word looks back at the words before it, [p0.15] and decides which ones matter.", 0.2f),
            new RideNarrationData.Line(RideLine.AttShares, "{shares}That's attention. [p0.2] Every word gets a share, [p0.1] and the shares add up to one hundred percent.", 0.2f),
            new RideNarrationData.Line(RideLine.AttReal, "{real}These are real numbers, from one of GPT-2's attention heads.", 0.2f),
            new RideNarrationData.Line(RideLine.AttGo, "{spot}Your turn: [p0.1] you are the attention. [p0.2] Point your laser at the words that matter, [p0.15] and hold it there. [p0.2] The longer you look, [p0.1] the bigger its share.", 0.1f),
            new RideNarrationData.Line(RideLine.AttSpread, "Look at both words. [p0.1] Share your attention.", 0.1f),
            new RideNarrationData.Line(RideLine.AttWrong, "Not that one. [p0.1] That word doesn't change the meaning.", 0.1f),
            new RideNarrationData.Line(RideLine.AttDone, "That's how a language model reads: [p0.1] every word, in context.", 0.1f),

            // Stop 4, prediction: the odds city.
            new RideNarrationData.Line(RideLine.PredJob, "{prompt}After all that, [p0.1] the machine has one job: [p0.2] guess the next word.", 0.2f),
            new RideNarrationData.Line(RideLine.PredOdds, "{bars}It scores every word it knows, [p0.15] over fifty thousand of them, [p0.15] and turns the scores into odds. [p0.2] These are the numbers GPT-2 really gave.", 0.2f),
            new RideNarrationData.Line(RideLine.PredUnsure, "{unsure}It isn't sure. [p0.15] It never is. [p0.25] {hungry}The word we meant, hungry, only got one percent.", 0.2f),
            new RideNarrationData.Line(RideLine.PredLearn, "{learn}GPT-2 read millions of web pages, [p0.1] guessing every next word, [p0.1] and nudging its weights each time it was surprised. [p0.2] Backpropagation and gradient descent, [p0.1] just like Pixel.", 0.2f),
            new RideNarrationData.Line(RideLine.PredGo, "{bet}Your turn: [p0.1] place your chips. [p0.2] Shoot a word to add a chip. [p0.1] Right hand adds, [p0.1] left hand takes one back. [p0.2] Ten chips, [p0.1] then see what the machine thinks.", 0.1f),
            new RideNarrationData.Line(RideLine.PredBeat, "You beat the machine on that one!", 0.1f),
            new RideNarrationData.Line(RideLine.PredMiss, "That was a long shot. [p0.1] That's what odds are.", 0.1f),
            new RideNarrationData.Line(RideLine.PredDone, "You just did the machine's one trick: [p0.1] weighing the odds.", 0.1f),

            // Stop 5, writing: the story road.
            new RideNarrationData.Line(RideLine.GenLoop, "{pick}The machine picks one word. [p0.2] {again}It adds that word to the sentence, [p0.1] and runs the whole machine again. [p0.2] One word at a time. [p0.15] That's all a chatbot does.", 0.2f),
            new RideNarrationData.Line(RideLine.GenTemp, "{temp}There's one knob: [p0.1] temperature. [p0.2] {cold}Turn it cold, [p0.1] and the machine plays it safe. [p0.2] {hot}Turn it hot, [p0.1] and it takes wild risks.", 0.2f),
            new RideNarrationData.Line(RideLine.GenGo, "{road}Your turn: [p0.1] steer the story. [p0.2] At every fork, [p0.1] shoot the word you want next. [p0.2] The safe road is easy. [p0.1] The wild words are rare.", 0.1f),
            new RideNarrationData.Line(RideLine.GenWild, "Wow, [p0.1] that was a long shot!", 0.1f),
            new RideNarrationData.Line(RideLine.GenSafe, "Safe and sensible.", 0.1f),
            new RideNarrationData.Line(RideLine.GenDone, "That's your story. [p0.15] The machine just helped you write it.", 0.1f),

            // The finale.
            new RideNarrationData.Line(RideLine.OutTalk, "{talk}And that's how Pixel learns to talk. [p0.2] Not from grammar rules. [p0.15] From a machine guessing the next word, [p0.1] billions of times.", 0.2f),
            new RideNarrationData.Line(RideLine.OutScale, "{scale}Pixel's number brain had nine hundred and seventy numbers. [p0.2] GPT-2 has one hundred and twenty-four million. [p0.2] The biggest chatbots have hundreds of billions.", 0.2f),
            new RideNarrationData.Line(RideLine.OutEnd, "{end}That's the machine inside the chatbot. [p0.25] Nothing magic. [p0.15] Just numbers, [p0.1] and a very good guess.", 0.5f),
        };

        /// <summary>The intro at the dock: GO holds until it ends. Then the dock says Level 1's "Pull the orange lever" line and GO goes live.</summary>
        public static readonly RideLine[] IntroLines = { RideLine.L2Hello, RideLine.L2Chatbot, RideLine.L2Stops };

        public const RideLine DockLine = RideLine.PullGo;

        /// <summary>Level 1's lines Level 2 says as they are: the dock's "pull the lever" and the win line a game ends with when it has none of its own.</summary>
        public static readonly RideLine[] SharedLines = { RideLine.PullGo, RideLine.StopClear };

        /// <summary>The sequences the segments play, in the order NeuralRideBuilder.Level2 refers to them by index.</summary>
        public static readonly (string Name, RideLine[] Lines)[] Sequences =
        {
            ("T1 tokens: explain", new[] { RideLine.TokChop, RideLine.TokNumbers, RideLine.TokRare }),              // 0
            ("T1 tokens: your turn", new[] { RideLine.TokSliceGo }),                                                  // 1
            ("T2 meaning: explain", new[] { RideLine.EmbName, RideLine.EmbSquash, RideLine.EmbSpace, RideLine.EmbMath }), // 2
            ("T2 meaning: your turn", new[] { RideLine.EmbSortGo }),                                                  // 3
            ("T3 attention: explain", new[] { RideLine.AttMeaning, RideLine.AttLookBack, RideLine.AttShares, RideLine.AttReal }), // 4
            ("T3 attention: your turn", new[] { RideLine.AttGo }),                                                    // 5
            ("T4 prediction: explain", new[] { RideLine.PredJob, RideLine.PredOdds, RideLine.PredUnsure, RideLine.PredLearn }), // 6
            ("T4 prediction: your turn", new[] { RideLine.PredGo }),                                                  // 7
            ("T5 writing: explain", new[] { RideLine.GenLoop, RideLine.GenTemp }),                                    // 8
            ("T5 writing: your turn", new[] { RideLine.GenGo }),                                                      // 9
            ("Finale", new[] { RideLine.OutTalk, RideLine.OutScale, RideLine.OutEnd }),                               // 10
        };

        /// <summary>Lines the moments say (not in a sequence): reactions and win lines.</summary>
        public static readonly RideLine[] MomentLines =
        {
            RideLine.TokWhole, RideLine.TokBoss, RideLine.TokDone, RideLine.EmbWrong, RideLine.EmbDone,
            RideLine.AttSpread, RideLine.AttWrong, RideLine.AttDone, RideLine.PredBeat, RideLine.PredMiss, RideLine.PredDone,
            RideLine.GenWild, RideLine.GenSafe, RideLine.GenDone,
        };

        /// <summary>The end card's credits (ADR-022).</summary>
        public const string Credits =
            "MODEL  GPT-2, OpenAI (MIT license): tokens, odds, attention\n"
            + "VOICE  Kokoro-82M text-to-speech (synthetic), Apache 2.0\n"
            + "MUSIC  \"Telstar\"    SOUND EFFECTS  Kenney, CC0\n"
            + "TYPE  Computer Modern Unicode (CMU) fonts, SIL OFL 1.1";

        /// <summary>The words of every line of this level and the shared ones, for the builder.</summary>
        public static IEnumerable<RideNarrationData.Line> All()
        {
            foreach (RideNarrationData.Line line in Lines) yield return line;
            foreach (RideLine id in SharedLines) yield return RideNarrationData.Get(id);
        }
    }
}
