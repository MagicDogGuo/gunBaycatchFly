using System;
using System.Collections;
using UnityEngine;

namespace DreamChase
{
    [DisallowMultipleComponent]
    public sealed class GameDirector : MonoBehaviour
    {
        [SerializeField] TransformSequence transformSequence;

        bool _hasTransformed;
        bool _isBusy;

        public GamePhase CurrentPhase { get; private set; }
        public event Action<GamePhase> OnPhaseChanged;

        void Awake()
        {
            if (transformSequence == null)
                transformSequence = GetComponent<TransformSequence>();

            CurrentPhase = GamePhase.Ground;
        }

        public void BeginTransform(PlayerMotor player, ButterflyController butterfly)
        {
            if (_hasTransformed || _isBusy || player == null)
                return;

            if (player.Phase != GamePhase.Ground)
                return;

            StartCoroutine(RunTransform(player, butterfly));
        }

        IEnumerator RunTransform(PlayerMotor player, ButterflyController butterfly)
        {
            _isBusy = true;
            _hasTransformed = true;

            SetPhase(GamePhase.Transforming);
            player.EnterTransforming();

            if (butterfly != null)
                butterfly.OnCaught();

            if (transformSequence != null)
            {
                transformSequence.PlayAt(player.transform.position+Vector3.forward*-0.5f);
                if (transformSequence.Duration > 0f)
                    yield return new WaitForSeconds(transformSequence.Duration);
            }
            else
            {
                yield return new WaitForSeconds(0.8f);
            }

            player.EnterFlying();
            SetPhase(GamePhase.Flying);
            _isBusy = false;
        }

        void SetPhase(GamePhase phase)
        {
            if (CurrentPhase == phase)
                return;

            CurrentPhase = phase;
            Debug.Log("GameDirector phase=" + phase);
            Action<GamePhase> handler = OnPhaseChanged;
            if (handler != null)
                handler(phase);
        }
    }
}
