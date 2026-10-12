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
    /// new ones. Level 1's lines are appended from 32 (TASK_LEVEL1_DIGITS_RESTRUCTURE, approved script).
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
        NeuronYourTurn = 42,
        NeuronFiredForZero = 43,
        NeuronMissedOne = 44,
        NeuronSolved = 45,
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
        RightAccuracy = 61,
        RightWords = 62,
        RightNextLevel = 63
    }

    /// <summary>Stable ids for the pod's control channels. Level 1 has one: GO.</summary>
    public static class RideControlIds
    {
        public const string Action = "action";
    }
}
