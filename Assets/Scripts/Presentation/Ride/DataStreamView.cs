using System.Collections.Generic;
using UnityEngine;
using Convergence.Gameplay.Ride;

namespace Convergence.Presentation.Ride
{
    /// <summary>
    /// The data stream: objects float right to left through the scanner arch. As each passes the arch the sensor
    /// lamps light and pulses race down the pipes; when the pulses reach the core it fills, and if Gameplay says it
    /// fired, a beam zaps the object. Everything shown comes from the <see cref="CaseOutcome"/> Gameplay sent.
    /// </summary>
    public sealed class DataStreamView : MonoBehaviour
    {
        private sealed class Item
        {
            public Transform root;
            public CaseOutcome outcome;
            public float age;
            public float scannedAt = -1f;
            public bool decided;
            public float zapAge = -1f;
        }

        [SerializeField] private StationController station;
        [SerializeField] private RideTheme theme;
        [SerializeField] private Transform lane;
        [SerializeField] private GameObject[] itemPrefabs = new GameObject[4];
        [SerializeField] private PipeFlowView rockPipe;
        [SerializeField] private PipeFlowView icePipe;
        [SerializeField] private CoreTankView core;
        [SerializeField] private GameObject rockLampGlow;
        [SerializeField] private GameObject iceLampGlow;
        [SerializeField] private float lampSeconds = 0.6f;
        [SerializeField] private float zapSeconds = 0.3f;

        private readonly List<Item> _items = new List<Item>();
        private float _rockLampUntil;
        private float _iceLampUntil;

        /// <summary>An object passed the scanner arch; its world position. Sound and haptics follow this, not the spawn.</summary>
        public event System.Action<CaseOutcome, Vector3> Scanned;

        /// <summary>The core decided an object (the beam fires now if it fired); its world position.</summary>
        public event System.Action<CaseOutcome, Vector3> Decided;

        private void OnEnable()
        {
            if (station != null) station.CaseFed += OnCaseFed;
            rockLampGlow.SetActive(false);
            iceLampGlow.SetActive(false);
        }

        private void OnDisable()
        {
            if (station != null) station.CaseFed -= OnCaseFed;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].root != null) Destroy(_items[i].root.gameObject);
            }

            _items.Clear();
        }

        private void OnCaseFed(CaseOutcome outcome)
        {
            GameObject prefab = itemPrefabs[(int)outcome.Kind];
            if (prefab == null) return;
            GameObject go = Instantiate(prefab, lane);
            go.SetActive(true);
            go.transform.localPosition = new Vector3(theme.spawnX, 0f, 0f);
            _items.Add(new Item { root = go.transform, outcome = outcome });
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                Item item = _items[i];
                item.age += dt;
                float x = theme.spawnX - theme.streamSpeed * item.age;
                Vector3 p = item.root.localPosition;
                p.x = x;
                p.y = Mathf.Sin(item.age * 2.2f) * 0.04f;
                item.root.localPosition = p;
                if (item.outcome.Kind != CaseKind.Drone) item.root.Rotate(0f, 35f * dt, 0f, Space.Self);

                if (item.scannedAt < 0f && x <= theme.scanX) Scan(item);
                if (item.scannedAt >= 0f && !item.decided && item.age >= item.scannedAt + theme.pulseTravelSeconds) Decide(item);

                if (item.zapAge >= 0f)
                {
                    item.zapAge += dt;
                    float k = 1f - Mathf.Clamp01(item.zapAge / zapSeconds);
                    item.root.localScale = Vector3.one * k;
                    if (k <= 0f) Remove(i);
                    continue;
                }

                if (x <= theme.despawnX) Remove(i);
            }

            rockLampGlow.SetActive(Time.time < _rockLampUntil);
            iceLampGlow.SetActive(Time.time < _iceLampUntil);
        }

        private void Scan(Item item)
        {
            item.scannedAt = item.age;
            if (item.outcome.RockSignal >= 0.5)
            {
                rockPipe.Pulse((float)item.outcome.RockSignal);
                _rockLampUntil = Time.time + lampSeconds;
            }

            if (item.outcome.IceSignal >= 0.5 && icePipe.isActiveAndEnabled)
            {
                icePipe.Pulse((float)item.outcome.IceSignal);
                _iceLampUntil = Time.time + lampSeconds;
            }

            Scanned?.Invoke(item.outcome, item.root.position);
        }

        /// <summary>The same kind of object, judged at the lever positions right now (not when it spawned).</summary>
        private CaseOutcome Live(CaseOutcome snapshot)
        {
            IReadOnlyList<CaseOutcome> board = station.Board;
            for (int i = 0; i < board.Count; i++)
            {
                if (board[i].Kind == snapshot.Kind) return board[i];
            }

            return snapshot;
        }

        private void Decide(Item item)
        {
            item.outcome = Live(item.outcome);
            item.decided = true;
            core.SetSum(item.outcome.Sum);
            core.Fire(item.outcome.Fired, item.root.position);
            if (item.outcome.Fired) item.zapAge = 0f;
            Decided?.Invoke(item.outcome, item.root.position);
        }

        private void Remove(int index)
        {
            if (_items[index].root != null) Destroy(_items[index].root.gameObject);
            _items.RemoveAt(index);
        }
    }
}
