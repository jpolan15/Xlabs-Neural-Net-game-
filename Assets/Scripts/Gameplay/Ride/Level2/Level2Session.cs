using System;
using UnityEngine;

namespace Convergence.Gameplay.Ride.Level2
{
    /// <summary>What the rider made in Level 2 that a later stop (or the end) wants to show back to them.</summary>
    public sealed class Level2Session : MonoBehaviour
    {
        /// <summary>Tokens the rider chopped.</summary>
        public int TokensCut { get; private set; }

        /// <summary>The story the rider steered, ending in a full stop; empty before the last stop.</summary>
        public string Story { get; private set; } = "";

        /// <summary>The probability GPT-2 itself gave that exact string of words, one after another.</summary>
        public double StoryProbability { get; private set; } = 1.0;

        public event Action Changed;

        public void AddTokens(int count)
        {
            TokensCut += count;
            Changed?.Invoke();
        }

        public void SetStory(string text, double probability)
        {
            Story = text;
            StoryProbability = probability;
            Changed?.Invoke();
        }
    }
}
