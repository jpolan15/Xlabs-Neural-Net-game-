using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using Convergence.Presentation.Ride.Explainers;

namespace Convergence.Presentation.Ride.Level2
{
    /// <summary>
    /// A Level 2 explainer: the ride's explainer kit (steps keyed to narration cues, the blackboard sky, skip and jump to the end), with
    /// its pieces built in code under the stop's own region root when the scene loads. Display only.
    /// </summary>
    public abstract class Level2Explainer : Explainer
    {
        [Tooltip("The region root the explainer's pieces are built under (its space is the stop's: the rider sits at the origin, +z ahead).")]
        [SerializeField] protected Transform anchor;
        [SerializeField] protected Gameplay.Ride.Level2.Level2DataSource source;

        protected Level2Kit Kit => Level2Kit.Current;

        /// <summary>The empty object the explainer's pieces live under; the base class shows and hides it.</summary>
        protected Transform MakeContent(string name)
        {
            content = new GameObject(name);
            content.transform.SetParent(anchor != null ? anchor : transform, false);
            content.transform.localPosition = Vector3.zero;
            return content.transform;
        }
    }
}
