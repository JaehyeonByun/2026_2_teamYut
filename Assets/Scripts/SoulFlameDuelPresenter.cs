using SoulScaleAsset;
using UnityEngine;
namespace SoulFlameAsset
{
    public sealed class SoulFlameDuelPresenter : MonoBehaviour
    {
        [SerializeField] private DuelHealth health;
        [SerializeField] private SoulScaleRigController scale;
        [SerializeField] private SoulFlameVisual playerFlame, opponentFlame;
        [SerializeField] private bool playerOnLeft=true;
        [SerializeField, Range(0f,18f)] private float healthDifferenceAngle=16f;
        [SerializeField, Range(0f,18f)] private float victoryAngle=18f;
        [SerializeField, Min(0f)] private float silenceAfterExtinction=.3f;
        [SerializeField] private bool applyTeamColors=true;
        [Header("Wind -> snap -> rebound -> read")]
        [SerializeField, Min(.05f)] private float snapSeconds=.20f;
        [SerializeField, Min(.05f)] private float settleSeconds=.40f;
        [SerializeField, Range(0f,5f)] private float overshootDegrees=3f;
        [SerializeField, Min(0f)] private float afterImpactHoldSeconds=.80f;
        private bool ready, presenting, launched;
        private int previousPlayer,previousOpponent;
        private float clock,delay,target,readClock;
        public bool IsPresenting => isActiveAndEnabled && presenting;
        public bool UsesHealth(DuelHealth value) => health==value;
        private bool HasReferences => health!=null && scale!=null && playerFlame!=null
            && opponentFlame!=null && playerFlame!=opponentFlame;
        private void Start()
        {
            if(!HasReferences) {Debug.LogError("SoulFlameDuelPresenter: assign Health, Scale and two different Flames.",this);enabled=false;return;}
            if(applyTeamColors){playerFlame.SetTeam(false);opponentFlame.SetTeam(true);}
            ResetPresentation();
        }
        private float DesiredAngle()
        {
            if(health.IsOver) return (health.PlayerWon==playerOnLeft?1f:-1f)*victoryAngle;
            return YutScaleVisualRules.Angle(health.PlayerHP,health.PlayerMaxHP,health.OpponentHP,health.OpponentMaxHP,healthDifferenceAngle,playerOnLeft);
        }
        private void Update()
        {
            if(!HasReferences) return;
            if(!ready){ResetPresentation();return;}
            bool playerHurt=health.PlayerHP<previousPlayer,enemyHurt=health.OpponentHP<previousOpponent;
            bool changed=health.PlayerHP!=previousPlayer || health.OpponentHP!=previousOpponent;
            playerFlame.SetHealth(health.PlayerHP,health.PlayerMaxHP);
            opponentFlame.SetHealth(health.OpponentHP,health.OpponentMaxHP);
            if(playerHurt || enemyHurt)
            {
                delay=0f;
                if(playerHurt) delay=Mathf.Max(delay,playerFlame.HitSeconds+(health.PlayerHP==0?playerFlame.FadeSeconds+silenceAfterExtinction:0f));
                if(enemyHurt) delay=Mathf.Max(delay,opponentFlame.HitSeconds+(health.OpponentHP==0?opponentFlame.FadeSeconds+silenceAfterExtinction:0f));
                target=DesiredAngle();clock=readClock=0f;launched=false;presenting=true;
                scale.HoldCurrentAngle();
            }
            else if(changed && presenting)
            { presenting=false;scale.SetAngle(DesiredAngle()); }
            previousPlayer=health.PlayerHP;previousOpponent=health.OpponentHP;
            if(!presenting) {scale.SetAngle(DesiredAngle());return;}
            clock+=Time.deltaTime;
            if(!launched && clock>=delay)
            { scale.PlayImpactAngle(target,snapSeconds,settleSeconds,overshootDegrees);launched=true;return; }
            if(launched && !scale.IsImpactAnimating)
            {
                readClock+=Time.deltaTime;
                if(readClock>=afterImpactHoldSeconds) presenting=false;
            }
        }
        public void ResetPresentation()
        {
            if(!HasReferences) return;
            ready=true;presenting=launched=false;clock=readClock=0f;
            previousPlayer=health.PlayerHP;previousOpponent=health.OpponentHP;
            scale.ResetScale();scale.SetAngle(DesiredAngle());
            playerFlame.SetHealth(health.PlayerHP,health.PlayerMaxHP,true);
            opponentFlame.SetHealth(health.OpponentHP,health.OpponentMaxHP,true);
        }
        // Kept for serialized UnityEvents; Update owns the single sequence.
        public void ShowPlayerVictory() { }
        public void ShowOpponentVictory() { }
        private void OnEnable(){ready=false;}
        private void OnDisable(){presenting=false;if(scale!=null)scale.HoldCurrentAngle();}
#if UNITY_EDITOR
        [ContextMenu("Tests/Player 100 Opponent 80")]
        private void TestEnemyHit(){if(Application.isPlaying && health!=null)health.SetHealthForTesting(100,80);}
        [ContextMenu("Tests/Player 40 Opponent 100")]
        private void TestPlayerHit(){if(Application.isPlaying && health!=null)health.SetHealthForTesting(40,100);}
        [ContextMenu("Tests/Restore 100 100")]
        private void TestReset(){if(Application.isPlaying && health!=null){health.SetHealthForTesting(100,100);ResetPresentation();}}
        [ContextMenu("Tests/Player 100 Opponent 0")]
        private void TestWin(){if(Application.isPlaying && health!=null){health.SetHealthForTesting(100,1);health.ResolveFinish(true,new int[]{0});}}
#endif
    }
}
