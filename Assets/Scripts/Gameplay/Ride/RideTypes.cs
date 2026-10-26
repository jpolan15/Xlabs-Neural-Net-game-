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
    /// reuses carry a V3 prefix) and the v4 lines are appended from 64.
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
        Threes = 64,
        MeetPixel = 65,
        PixelBrain = 66,
        PullGo = 67,

        // E1 pixels.
        PixelsSquares = 68,
        PixelsNumbers = 69,
        PixelsFortyNine = 70,
        // 71 was PixelsFly, cut in v5 (reserved).

        // E2 one neuron.
        NeuronHolds = 72,
        NeuronWires = 73,
        NeuronGreenRed = 74,
        NeuronAdds = 75,
        NeuronYourTurn = 76,
        NeuronLitForOther = 77,
        NeuronMissedThree = 78,
        NeuronSolved = 79,

        // E3 the network.
        NetworkTeam = 80,
        NetworkAnswers = 81,
        NetworkWire = 82,
        NetworkSend = 83,

        // E4 wrong, and the loss.
        WrongSays = 84,
        WrongRandom = 85,
        WrongWanted = 86,
        WrongLoss = 87,

        // E5 backpropagation.
        BackWish = 88,
        BackFlows = 89,
        BackHot = 90,
        BackGrab = 91,
        BackDone = 92,

        // E6 gradient descent.
        DescentHill = 93,
        DescentStep = 94,
        DescentTooBig = 95,
        DescentRealHill = 96,
        DescentOvershoot = 97,
        DescentSettle = 98,

        // E7 right, and training.
        RightSays = 99,
        RightTraining = 100,
        RightAccuracy = 101,

        // E8 tokens.
        TokensWords = 102,
        TokensNumbers = 103,
        TokensPredict = 104,
        TokensNext = 105,

        // Level 1 v5, the mini-games (TASK_LEVEL1_V5_GAMEPLAY). 71 (PixelsFly) is cut; its number stays reserved.
        StopClear = 106,
        DrawYourTurn = 107,
        DrawHint = 108,
        DrawDone = 109,
        ZapIntro = 110,
        PlugYourTurn = 111,
        PlugWrong = 112,
        PlugDone = 113,
        PlugTooMany = 114,
        BackHurry = 115,
        DescentYourTurn = 116,
        DescentGameOvershoot = 117,
        DescentGameSlow = 118
    }

    /// <summary>Stable ids for the pod's control channels. Level 1 has one: GO.</summary>
    public static class RideControlIds
    {
        public const string Action = "action";
    }
}
