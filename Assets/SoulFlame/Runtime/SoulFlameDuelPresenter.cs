using System.Collections;
using SoulScaleAsset;
using UnityEngine;

namespace SoulFlameAsset
{
    // Health events invoke this AFTER YutTurnManager has locked input and blended the camera.
    public sealed class SoulFlameDuelPresenter : MonoBehaviour
    {
        [SerializeField] private DuelHealth health;
        [SerializeField] private SoulScaleRigController scale;
        [SerializeField] private SoulFlameVisual playerFlame;
        [SerializeField] private SoulFlameVisual opponentFlame;
        [SerializeField] private bool playerOnLeft = true;
        [SerializeField, Min(0f)] private float silenceAfterExtinction = 0.3f;
        private bool presentingResult;

        private void Start()
        {
            if (health == null || scale == null || playerFlame == null || opponentFlame == null || playerFlame == opponentFlame)
            { Debug.LogError("SoulFlameDuelPresenter: assign Health, Scale and two different flame instances.", this); enabled = false; return; }
            ResetPresentation();
        }
        private void Update()
        {
            if (health == null) return;
            // Keep the last living appearance until the result camera is ready.
            // Numeric health and input lock are already final; only the visual is held.
            if (health.IsOver && !presentingResult) return;
            Sync(false);
        }
        private void Sync(bool instant)
        {
            if (health == null) return;
            if (playerFlame != null) playerFlame.SetHealth(health.PlayerHP, health.PlayerMaxHP, instant);
            if (opponentFlame != null) opponentFlame.SetHealth(health.OpponentHP, health.OpponentMaxHP, instant);
        }
        public void ShowPlayerVictory() { BeginResult(true); }
        public void ShowOpponentVictory() { BeginResult(false); }
        private void BeginResult(bool playerWon)
        {
            if (!isActiveAndEnabled || health == null || !health.IsOver || health.PlayerWon != playerWon || presentingResult) return;
            presentingResult = true; Sync(false);
            StartCoroutine(FinishResult(playerWon));
        }
        private IEnumerator FinishResult(bool playerWon)
        {
            var loser = playerWon ? opponentFlame : playerFlame;
            float elapsed = 0f;
            while (loser != null && loser.isActiveAndEnabled && !loser.IsExtinguished && elapsed < 3f)
            { elapsed += Time.deltaTime; yield return null; }
            // Ensure a disabled/customized effect cannot keep the result sequence waiting forever.
            if (loser != null && !loser.IsExtinguished) loser.SetHealth(0, 1, true);
            yield return new WaitForSeconds(Mathf.Max(0f, silenceAfterExtinction));
            if (!presentingResult || health == null || !health.IsOver || scale == null) yield break;
            if (playerWon == playerOnLeft) scale.SetLeftWinner(); else scale.SetRightWinner();
        }
        public void ResetPresentation()
        {
            StopAllCoroutines(); presentingResult = false;
            if (scale != null) scale.ResetScale();
            Sync(true);
        }
        private void OnDisable() { StopAllCoroutines(); presentingResult = false; }
    }
}
