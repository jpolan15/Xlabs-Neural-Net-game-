using UnityEngine;
using Convergence.Gameplay;

namespace Convergence.Presentation
{
    /// <summary>
    /// Shows crisis damage while the puzzle is unsolved and hides it after a pass.
    /// Reads evaluation. Does not decide it.
    /// </summary>
    public sealed class ChamberDamageVisual : MonoBehaviour
    {
        [SerializeField] private ChamberController chamber;

        StateBoundDamage[] _marks;
        ParticleSystem[] _sparks;
        Renderer[] _renderers;
        bool _passed;

        void Awake()
        {
            if (chamber == null) chamber = FindAnyObjectByType<ChamberController>();
            _marks = FindObjectsByType<StateBoundDamage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _sparks = new ParticleSystem[_marks.Length];
            _renderers = new Renderer[_marks.Length];
            for (int i = 0; i < _marks.Length; i++)
            {
                _sparks[i] = _marks[i].GetComponent<ParticleSystem>();
                if (_sparks[i] == null) _sparks[i] = _marks[i].GetComponentInChildren<ParticleSystem>();
                _renderers[i] = _marks[i].GetComponent<Renderer>();
                if (_renderers[i] == null) _renderers[i] = _marks[i].GetComponentInChildren<Renderer>();
            }
        }

        void Update()
        {
            bool passed = chamber != null && chamber.LastEvaluation != null && chamber.LastEvaluation.Passed;
            if (passed == _passed) return;
            _passed = passed;
            for (int i = 0; i < _marks.Length; i++)
            {
                if (_sparks[i] != null)
                {
                    if (passed) _sparks[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    else _sparks[i].Play();
                }

                if (_marks[i].Kind == "screen" && _renderers[i] != null)
                {
                    _renderers[i].enabled = true;
                }

                if (_marks[i].Kind == "strip" && _renderers[i] != null)
                {
                    var block = new MaterialPropertyBlock();
                    _renderers[i].GetPropertyBlock(block);
                    Color color = passed ? new Color(0.20f, 0.75f, 0.98f) : new Color(0.98f, 0.60f, 0.08f);
                    block.SetColor("_BaseColor", color);
                    block.SetColor("_EmissionColor", color * (passed ? 0.4f : 0.25f));
                    _renderers[i].SetPropertyBlock(block);
                }
            }
        }
    }
}
