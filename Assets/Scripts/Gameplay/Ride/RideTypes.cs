using System;
using System.Collections.Generic;

namespace Convergence.Gameplay.Ride
{
    /// <summary>Which idea a stop teaches.</summary>
    public enum StationKind
    {
        /// <summary>Weight times input against a trigger. One lever.</summary>
        OneSignal,

        /// <summary>A weighted sum of two inputs. Three levers.</summary>
        TwoSignals,

        /// <summary>Loss and gradient descent. LEARN plus a learning-rate lever.</summary>
        Learns
    }

    /// <summary>What a stop is doing right now. Presentation maps this to the instruction line.</summary>
    public enum StationPhase
    {
        Idle,
        Tuning,
        Oops,
        Solved,
        Learning,
        Overshoot,
        Stalled
    }

    /// <summary>Where the pod is in the ride.</summary>
    public enum RideState
    {
        Dock,
        Traveling,
        AtStop,
        Complete
    }

    /// <summary>The fifteen narration lines, in script order. The number is the file suffix (vo_ride_NN).</summary>
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
        CourseLocked = 15
    }

    /// <summary>The four things that can float down the stream, named by which sensors they light.</summary>
    public enum CaseKind
    {
        Drone,
        Comet,
        Rock,
        Chunk
    }

    /// <summary>Stable ids for the pod's control channels.</summary>
    public static class RideControlIds
    {
        public const string Action = "action";
        public const string Eta = "eta";
        public const string Rock = "w_rock";
        public const string Ice = "w_ice";
        public const string Trigger = "trigger";
    }

    /// <summary>What one test case did at the current settings. Data only.</summary>
    public readonly struct CaseOutcome
    {
        public readonly int Index;
        public readonly CaseKind Kind;
        public readonly double RockSignal;
        public readonly double IceSignal;
        public readonly double Sum;
        public readonly bool Fired;
        public readonly bool ShouldFire;
        public readonly bool Correct;

        public CaseOutcome(int index, CaseKind kind, double rock, double ice, double sum, bool fired, bool shouldFire, bool correct)
        {
            Index = index;
            Kind = kind;
            RockSignal = rock;
            IceSignal = ice;
            Sum = sum;
            Fired = fired;
            ShouldFire = shouldFire;
            Correct = correct;
        }
    }

    /// <summary>One gradient-descent step, for the marble and the readouts.</summary>
    public readonly struct TrainingFrame
    {
        public readonly int Step;
        public readonly double W1;
        public readonly double W2;
        public readonly double Loss;
        public readonly double Gradient1;
        public readonly double Gradient2;
        public readonly double LearningRate;

        public TrainingFrame(int step, double w1, double w2, double loss, double g1, double g2, double learningRate)
        {
            Step = step;
            W1 = w1;
            W2 = w2;
            Loss = loss;
            Gradient1 = g1;
            Gradient2 = g2;
            LearningRate = learningRate;
        }
    }

    /// <summary>The numbers an equation reveal is built from.</summary>
    public readonly struct StationSummary
    {
        public readonly StationKind Kind;
        public readonly double RockWeight;
        public readonly double IceWeight;
        public readonly double Trigger;
        public readonly double LearningRate;

        public StationSummary(StationKind kind, double rock, double ice, double trigger, double learningRate)
        {
            Kind = kind;
            RockWeight = rock;
            IceWeight = ice;
            Trigger = trigger;
            LearningRate = learningRate;
        }
    }
}
