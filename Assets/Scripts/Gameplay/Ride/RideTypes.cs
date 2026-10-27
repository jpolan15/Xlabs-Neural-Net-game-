namespace Convergence.Gameplay.Ride
{
    /// <summary>Where the pod is in the ride.</summary>
    public enum RideState
    {
        Dock,
        Traveling,
        AtStop,
        Complete,
        /// <summary>The narrated opening at the dock. GO does not launch until it ends or is skipped. Appended, so older ids keep their numbers.</summary>
        Intro
    }

    /// <summary>
    /// The narration lines. The number is the file suffix (vo_ride_NN) and never changes. 1 to 31 belonged to the v3
    /// ride (AURA, JFK, Apollo), which Level 1 cut; their numbers stay reserved so old files can never be mistaken for
    /// new ones. 32 to 63 were the first Level 1 script (TASK_LEVEL1_DIGITS_RESTRUCTURE); v4 rewrote it as Explain,
    /// Unfold, Play (TASK_LEVEL1_V4_EXPLAINERS section 3), so those numbers are reserved too (the four whose names v4
    /// reuses carry a V3 prefix) and the v4 lines are appended from 64. v6 rewrote it again (TASK_LEVEL1_V6_TRAIN): the
    /// v4/v5 lines, 64 to 118, carry a V5 prefix and are reserved, and the v6 lines are appended from 119.
    /// </summary>
    public enum RideLine
    {
        Launch = 1,
        NeuronIntro = 2,
        LowerTrigger = 3,
        DroneOops = 4,
        WeightTimesInput = 5,
        TwoSensors = 6,
        TurnPipes = 7,
        OrBuilt = 8,
        PullLearn = 9,
        HillIsError = 10,
        GradientDescent = 11,
        LrTooHigh = 12,
        Converged = 13,
        TargetingRestored = 14,
        CourseLocked = 15,
        ArchivalMoon = 16,
        GuideHardProblem = 17,
        AuraOffline = 18,
        GuideAuraBrain = 19,
        GuideWeights = 20,
        GuidePlan = 21,
        GuideOrange = 22,
        // 23 is intentionally unused.
        ApolloLiftoff = 24,
        BriefingFirstNeuron = 25,
        BriefingPerceptron = 26,
        BriefingLearning = 27,
        GuideHeartOfNetworks = 28,
        GuideTheProblem = 29,
        GuideWakeAura = 30,
        GuideMakeItFire = 31,

        // Level 1: how a machine learns to read a digit.
        PageLook = 32,
        PageOnlyNumbers = 33,
        PageFromInside = 34,
        PagePullToDive = 35,
        GridInsidePicture = 36,
        GridZeroAndOne = 37,
        GridSevenBySeven = 38,
        NeuronThisIsOne = 39,
        NeuronWeights = 40,
        NeuronMultiplyAdd = 41,
        V3NeuronYourTurn = 42,
        NeuronFiredForZero = 43,
        V3NeuronMissedOne = 44,
        V3NeuronSolved = 45,
        NetworkManyNeurons = 46,
        NetworkLayers = 47,
        NetworkSendItThrough = 48,
        WrongItSays = 49,
        WrongNeverTaught = 50,
        RewindBackwards = 51,
        RewindBackprop = 52,
        RewindPointAtWires = 53,
        RewindWiresKnow = 54,
        ValleyDownhill = 55,
        ValleyGradientDescent = 56,
        ValleyOvershoot = 57,
        ValleySmallerSteps = 58,
        RightBackInNetwork = 59,
        RightItSays = 60,
        V3RightAccuracy = 61,
        RightWords = 62,
        RightNextLevel = 63,

        // Level 1 v4, "Teach Pixel to read". E0 the dock.
        V5Threes = 64,
        V5MeetPixel = 65,
        V5PixelBrain = 66,
        V5PullGo = 67,

        // E1 pixels.
        V5PixelsSquares = 68,
        V5PixelsNumbers = 69,
        V5PixelsFortyNine = 70,
        // 71 was PixelsFly, cut in v5 (reserved).

        // E2 one neuron.
        V5NeuronHolds = 72,
        V5NeuronWires = 73,
        V5NeuronGreenRed = 74,
        V5NeuronAdds = 75,
        V5NeuronYourTurn = 76,
        V5NeuronLitForOther = 77,
        V5NeuronMissedThree = 78,
        V5NeuronSolved = 79,

        // E3 the network.
        V5NetworkTeam = 80,
        V5NetworkAnswers = 81,
        V5NetworkWire = 82,
        V5NetworkSend = 83,

        // E4 wrong, and the loss.
        V5WrongSays = 84,
        V5WrongRandom = 85,
        V5WrongWanted = 86,
        V5WrongLoss = 87,

        // E5 backpropagation.
        V5BackWish = 88,
        V5BackFlows = 89,
        V5BackHot = 90,
        V5BackGrab = 91,
        V5BackDone = 92,

        // E6 gradient descent.
        V5DescentHill = 93,
        V5DescentStep = 94,
        V5DescentTooBig = 95,
        V5DescentRealHill = 96,
        V5DescentOvershoot = 97,
        V5DescentSettle = 98,

        // E7 right, and training.
        V5RightSays = 99,
        V5RightTraining = 100,
        V5RightAccuracy = 101,

        // E8 tokens.
        V5TokensWords = 102,
        V5TokensNumbers = 103,
        V5TokensPredict = 104,
        V5TokensNext = 105,

        // Level 1 v5, the mini-games (TASK_LEVEL1_V5_GAMEPLAY). 71 (PixelsFly) is cut; its number stays reserved.
        V5StopClear = 106,
        V5DrawYourTurn = 107,
        V5DrawHint = 108,
        V5DrawDone = 109,
        V5ZapIntro = 110,
        V5PlugYourTurn = 111,
        V5PlugWrong = 112,
        V5PlugDone = 113,
        V5PlugTooMany = 114,
        V5BackHurry = 115,
        V5DescentYourTurn = 116,
        V5DescentGameOvershoot = 117,
        V5DescentGameSlow = 118,

        // Level 1 v6, "train a real brain" (TASK_LEVEL1_V6_TRAIN, ADR-020): the v4/v5 lines above keep their numbers
        // with a V5 prefix and are reserved; the v6 script starts at 119.
        // E0 the dock.
        Threes = 119,
        MeetPixel = 120,
        PixelBrain = 121,
        PullGo = 122,

        // E1 pixels, then draw your 3.
        PixelsSquares = 123,
        PixelsNumbers = 124,
        PixelsFortyNine = 125,
        DrawYourTurn = 126,
        DrawBigger = 127,
        DrawDoneNudge = 128,
        DrawDone = 129,
        ZapIntro = 130,

        // E2 one neuron, then spray the weights.
        NeuronHolds = 131,
        NeuronWires = 132,
        NeuronGreenRed = 133,
        NeuronAdds = 134,
        NeuronYourTurn = 135,
        NeuronLitForOther = 136,
        NeuronMissedThree = 137,
        NeuronSolved = 138,

        // E3 the network, send your 3.
        NetworkTeam = 139,
        NetworkAnswers = 140,
        NetworkWire = 141,
        NetworkSend = 142,

        // E4 the guess and the loss.
        WrongSays = 143,
        WrongWanted = 144,
        WrongLoss = 145,

        // E5 backpropagation, the hot-wire hunt.
        BackWish = 146,
        BackFlows = 147,
        BackGrab = 148,
        BackHurry = 149,
        BackDone = 150,
        StopClear = 151,

        // E6 gradient descent, the training game.
        DescentHill = 152,
        DescentStep = 153,
        DescentTooBig = 154,
        TrainYourTurn = 155,
        TrainTooBig = 156,
        TrainTooSmall = 157,
        TrainGood = 158,
        TrainDone = 159,

        // E7 Pixel reads your 3.
        RightYours = 160,
        RightYes = 161,
        RightStudy = 162,
        RightNowYes = 163,
        RightAccuracy = 164,
        RightLoop = 165,

        // Bonus: stump Pixel.
        StumpYourTurn = 166,
        StumpRight = 167,
        StumpWrong = 168,
        StumpDone = 169,

        // E8 tokens.
        TokensWords = 170,
        TokensNumbers = 171,
        TokensPredict = 172,
        TokensNext = 173,

        // Level 1 v7 (TASK_LEVEL1_V7_DEMO): the intro's roadmap, blast the 3s, what the hidden neurons learned.
        IntroStops = 174,
        ZapWrong = 175,
        LearnedPainted = 176,
        LearnedHonest = 177
    }

    /// <summary>Stable ids for the pod's control channels. Level 1 has one: GO.</summary>
    public static class RideControlIds
    {
        public const string Action = "action";
    }
}
