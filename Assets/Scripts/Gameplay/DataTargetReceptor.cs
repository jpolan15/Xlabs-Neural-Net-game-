using System;
using UnityEngine;
using Convergence.Core.Puzzles;

namespace Convergence.Gameplay
{
    /// <summary>
    /// Physical 3D world representation of an individual dataset test case (e.g. Case 1: [0,0] -> 0).
    /// Tracks physical approach along defense corridors toward the station perimeter,
    /// diagnostic states, and breach/vaporization outcomes.
    /// Strictly Gameplay layer: does not execute neural math itself.
    /// </summary>
    public class DataTargetReceptor : MonoBehaviour
    {
        [Header("Case Definition")]
        [SerializeField] private int caseIndex = 0;
        [SerializeField] private string caseLabel = "[0, 0]";
        [SerializeField] private string targetTitle = "Friendly Supply Drone";
        [SerializeField] private string threatRole = "SAFE (DO NOT SHOOT)";
        [SerializeField] private double inputX1 = 0.0;
        [SerializeField] private double inputX2 = 0.0;
        [SerializeField] private double expectedOutput = 0.0;

        [Header("Live Status")]
        [SerializeField] private bool isHarmonized = false;
        [SerializeField] private double lastCalculatedZ = 0.0;
        [SerializeField] private double lastActualOutput = 0.0;

        [Header("Approach & Wave Tracking")]
        [Range(0f, 1f)]
        [SerializeField] private float approachProgress = 0.0f; // 0.0 = Far Spawn, 1.0 = Perimeter Line
        [SerializeField] private bool isBreached = false;
        [SerializeField] private bool isVaporized = false;
        [SerializeField] private bool isDocked = false;

        public int CaseIndex => caseIndex;
        public string CaseLabel => caseLabel;
        public string TargetTitle => string.IsNullOrEmpty(targetTitle) ? GetDefaultTargetTitle(caseIndex) : targetTitle;
        public string ThreatRole => string.IsNullOrEmpty(threatRole) ? GetDefaultThreatRole(caseIndex) : threatRole;
        public double InputX1 => inputX1;
        public double InputX2 => inputX2;
        public double ExpectedOutput => expectedOutput;
        public bool IsHarmonized => isHarmonized;
        public double LastCalculatedZ => lastCalculatedZ;
        public double LastActualOutput => lastActualOutput;
        public CaseDiagnostic LastDiagnostic { get; private set; }

        public float ApproachProgress => approachProgress;
        public bool IsBreached => isBreached;
        public bool IsVaporized => isVaporized;
        public bool IsDocked => isDocked;

        public static string GetDefaultTargetTitle(int idx) => idx switch
        {
            0 => "Friendly Maintenance Drone",
            1 => "Biohazard Toxin Canister",
            2 => "Rogue Radiation Drone",
            3 => "Overloaded Dual-Breach Core",
            _ => $"Target Pod {idx + 1}"
        };

        public static string GetDefaultThreatRole(int idx) => idx switch
        {
            0 => "SAFE ALLY [TARGET Y=0]",
            1 => "LETHAL HAZARD [INTERCEPT Y=1]",
            2 => "LETHAL HAZARD [INTERCEPT Y=1]",
            3 => "CRITICAL BREACH [INTERCEPT Y=1]",
            _ => "UNCLASSIFIED"
        };

        public event Action<DataTargetReceptor> OnStateUpdated;
        public event Action<DataTargetReceptor> OnPulseImpacted;
        public event Action<DataTargetReceptor> OnApproachUpdated;
        public event Action<DataTargetReceptor> OnBreached;
        public event Action<DataTargetReceptor> OnVaporized;
        public event Action<DataTargetReceptor> OnDocked;

        public void Initialize(int index, string label, double x1, double x2, double expected)
        {
            caseIndex = index;
            caseLabel = label;
            inputX1 = x1;
            inputX2 = x2;
            expectedOutput = expected;
            isHarmonized = false;
            approachProgress = 0.0f;
            isBreached = false;
            isVaporized = false;
            isDocked = false;
            LastDiagnostic = null;
        }

        public void SetApproachProgress(float progress)
        {
            approachProgress = Mathf.Clamp01(progress);
            OnApproachUpdated?.Invoke(this);
        }

        public void ApplyDiagnostic(CaseDiagnostic diagnostic, bool activationMatches)
        {
            LastDiagnostic = diagnostic;
            if (diagnostic != null)
            {
                lastCalculatedZ = diagnostic.CalculatedZ;
                lastActualOutput = diagnostic.ActualOutput;
                isHarmonized = diagnostic.IsCorrect && activationMatches;
            }
            else
            {
                isHarmonized = false;
            }

            OnStateUpdated?.Invoke(this);
        }

        public void NotifyPulseHit()
        {
            OnPulseImpacted?.Invoke(this);
        }

        public void NotifyVaporized()
        {
            isVaporized = true;
            isBreached = false;
            OnVaporized?.Invoke(this);
        }

        public void NotifyDocked()
        {
            isDocked = true;
            isBreached = false;
            OnDocked?.Invoke(this);
        }

        public void NotifyBreached()
        {
            isBreached = true;
            OnBreached?.Invoke(this);
        }

        public void ResetReceptor()
        {
            isHarmonized = false;
            approachProgress = 0.0f;
            isBreached = false;
            isVaporized = false;
            isDocked = false;
            lastCalculatedZ = 0.0;
            lastActualOutput = 0.0;
            LastDiagnostic = null;
            OnStateUpdated?.Invoke(this);
            OnApproachUpdated?.Invoke(this);
        }
    }
}
