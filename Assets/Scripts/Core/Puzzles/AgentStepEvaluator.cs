using System;
using System.Collections.Generic;

namespace Convergence.Core.Puzzles
{
    /// <summary>The captain's three goals in the finale.</summary>
    public enum AgentGoal
    {
        /// <summary>Fly home.</summary>
        Home,

        /// <summary>Break up rocks for ore.</summary>
        MineRocks,

        /// <summary>Bring the lost drone back.</summary>
        RescueDrone
    }

    /// <summary>The tools AURA can act with.</summary>
    public enum AgentTool
    {
        /// <summary>Destroys what it hits.</summary>
        Laser,

        /// <summary>Moves the ship toward a target.</summary>
        Thrusters,

        /// <summary>Looks at a target. Always harmless.</summary>
        Scanner
    }

    /// <summary>One step AURA proposes: a tool and what it is aimed at.</summary>
    public sealed class AgentAction : IEquatable<AgentAction>
    {
        /// <summary>The tool.</summary>
        public AgentTool Tool { get; }

        /// <summary>What it is aimed at, for example "rock", "drone" or "home".</summary>
        public string Target { get; }

        /// <summary>Creates an action. Target may not be empty.</summary>
        public AgentAction(AgentTool tool, string target)
        {
            if (string.IsNullOrEmpty(target)) throw new ArgumentException("Target cannot be empty.", nameof(target));
            Tool = tool;
            Target = target;
        }

        /// <summary>The words AURA's THINK ticker shows, for example "fire laser at drone".</summary>
        public string Words
        {
            get
            {
                switch (Tool)
                {
                    case AgentTool.Laser: return "fire laser at " + Target;
                    case AgentTool.Thrusters: return "steer to " + Target;
                    default: return "scan " + Target;
                }
            }
        }

        /// <inheritdoc />
        public bool Equals(AgentAction other) => other != null && other.Tool == Tool && other.Target == Target;

        /// <inheritdoc />
        public override bool Equals(object obj) => Equals(obj as AgentAction);

        /// <inheritdoc />
        public override int GetHashCode() => ((int)Tool * 397) ^ Target.GetHashCode();

        /// <inheritdoc />
        public override string ToString() => Words;
    }

    /// <summary>Core's ruling on one proposed step.</summary>
    public sealed class AgentStepVerdict
    {
        /// <summary>False when the step would hurt a friend.</summary>
        public bool Safe { get; }

        /// <summary>True when the step is the one that completes the goal.</summary>
        public bool ReachesGoal { get; }

        /// <summary>A few words for the dash, for example "drone is a friend".</summary>
        public string Reason { get; }

        /// <summary>Creates a verdict.</summary>
        public AgentStepVerdict(bool safe, bool reachesGoal, string reason)
        {
            Safe = safe;
            ReachesGoal = reachesGoal;
            Reason = reason ?? string.Empty;
        }
    }

    /// <summary>
    /// Stop 6 (Agent loop). Rules: a laser aimed at a friend is never safe; every other step is safe.
    /// Each goal is reached by one final step. AURA's scripted plans put an unsafe step third, so the
    /// rider has to pull STOP; <see cref="Replan"/> is what she does after being stopped.
    /// </summary>
    public static class AgentStepEvaluator
    {
        private static readonly HashSet<string> Friends = new HashSet<string>(StringComparer.Ordinal)
        {
            "drone", "drones", "friend", "helper", "pilot", "captain", "ship"
        };

        /// <summary>The index in every plan of the unsafe step the rider must stop.</summary>
        public const int TwistStep = 2;

        /// <summary>True when the target is a friend.</summary>
        public static bool IsFriend(string target) => target != null && Friends.Contains(target);

        /// <summary>The step that completes each goal.</summary>
        public static AgentAction GoalStep(AgentGoal goal)
        {
            switch (goal)
            {
                case AgentGoal.Home: return new AgentAction(AgentTool.Thrusters, "home");
                case AgentGoal.MineRocks: return new AgentAction(AgentTool.Laser, "rock");
                default: return new AgentAction(AgentTool.Thrusters, "drone");
            }
        }

        /// <summary>Is this step safe, and does it finish the goal?</summary>
        public static AgentStepVerdict Evaluate(AgentGoal goal, AgentAction action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (action.Tool == AgentTool.Laser && IsFriend(action.Target))
            {
                return new AgentStepVerdict(false, false, action.Target + " is a friend");
            }

            bool reaches = action.Equals(GoalStep(goal));
            return new AgentStepVerdict(true, reaches, reaches ? "goal reached" : "ok");
        }

        /// <summary>AURA's first plan for a goal, one step per loop. Step <see cref="TwistStep"/> is unsafe.</summary>
        public static IReadOnlyList<AgentAction> Plan(AgentGoal goal)
        {
            switch (goal)
            {
                case AgentGoal.Home:
                    return new[]
                    {
                        new AgentAction(AgentTool.Scanner, "stars"),
                        new AgentAction(AgentTool.Laser, "rock"),
                        new AgentAction(AgentTool.Laser, "drone"),
                        new AgentAction(AgentTool.Thrusters, "home")
                    };
                case AgentGoal.MineRocks:
                    return new[]
                    {
                        new AgentAction(AgentTool.Scanner, "rocks"),
                        new AgentAction(AgentTool.Thrusters, "rocks"),
                        new AgentAction(AgentTool.Laser, "drone"),
                        new AgentAction(AgentTool.Laser, "rock")
                    };
                default:
                    return new[]
                    {
                        new AgentAction(AgentTool.Scanner, "drone"),
                        new AgentAction(AgentTool.Thrusters, "rocks"),
                        new AgentAction(AgentTool.Laser, "drone"),
                        new AgentAction(AgentTool.Thrusters, "drone")
                    };
            }
        }

        /// <summary>The safe step AURA takes instead of the one the rider stopped.</summary>
        public static AgentAction Replan(AgentGoal goal, AgentAction stopped)
        {
            if (stopped == null) throw new ArgumentNullException(nameof(stopped));
            return new AgentAction(AgentTool.Scanner, stopped.Target);
        }
    }

    /// <summary>
    /// One run of the finale. AURA proposes the next step of her plan each loop; the rider either lets it run
    /// or pulls STOP. Won when the goal step has run and no unsafe step ever ran.
    /// </summary>
    public sealed class AgentEpisode
    {
        private readonly List<AgentAction> _plan;
        private int _next;

        /// <summary>The goal the captain picked.</summary>
        public AgentGoal Goal { get; }

        /// <summary>Loops run so far (executed or stopped).</summary>
        public int Loops { get; private set; }

        /// <summary>Unsafe steps that actually ran.</summary>
        public int UnsafeExecuted { get; private set; }

        /// <summary>Unsafe steps the rider stopped.</summary>
        public int UnsafeStopped { get; private set; }

        /// <summary>Safe steps the rider stopped for no reason.</summary>
        public int SafeStopped { get; private set; }

        /// <summary>True once the goal step has run.</summary>
        public bool GoalReached { get; private set; }

        /// <summary>True when the goal was reached with no unsafe step executed.</summary>
        public bool Won => GoalReached && UnsafeExecuted == 0;

        /// <summary>True when the plan has run out or the goal is reached.</summary>
        public bool Finished => GoalReached || _next >= _plan.Count;

        /// <summary>The step AURA proposes now, or null when finished.</summary>
        public AgentAction Proposed => Finished ? null : _plan[_next];

        /// <summary>Core's ruling on the proposed step, or null when finished.</summary>
        public AgentStepVerdict ProposedVerdict => Finished ? null : AgentStepEvaluator.Evaluate(Goal, _plan[_next]);

        /// <summary>Starts an episode on AURA's plan for the goal.</summary>
        public AgentEpisode(AgentGoal goal)
        {
            Goal = goal;
            _plan = new List<AgentAction>(AgentStepEvaluator.Plan(goal));
        }

        /// <summary>The rider lets the proposed step run. Returns its verdict.</summary>
        public AgentStepVerdict Execute()
        {
            AgentAction step = RequireProposed();
            AgentStepVerdict verdict = AgentStepEvaluator.Evaluate(Goal, step);
            if (!verdict.Safe) UnsafeExecuted++;
            if (verdict.ReachesGoal) GoalReached = true;
            _next++;
            Loops++;
            return verdict;
        }

        /// <summary>The rider pulls STOP. AURA replaces the step with a safe one, which becomes the next proposal.</summary>
        public AgentStepVerdict Stop()
        {
            AgentAction step = RequireProposed();
            AgentStepVerdict verdict = AgentStepEvaluator.Evaluate(Goal, step);
            if (verdict.Safe) SafeStopped++;
            else UnsafeStopped++;
            _plan[_next] = AgentStepEvaluator.Replan(Goal, step);
            Loops++;
            return verdict;
        }

        /// <summary>
        /// Second chance after an unsafe step ran: the plan restarts at the step that went wrong, which AURA
        /// proposes again, and the count of unsafe steps clears.
        /// </summary>
        public void RetryLastStep()
        {
            if (_next == 0) return;
            _next--;
            GoalReached = false;
            UnsafeExecuted = 0;
        }

        private AgentAction RequireProposed()
        {
            if (Finished) throw new InvalidOperationException("The episode is finished.");
            return _plan[_next];
        }
    }
}
