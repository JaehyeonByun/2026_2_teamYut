using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

// Manager owns match timing; health never starts turns or resets itself in Awake.
public sealed class DuelHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int playerMaxHealth = 6;
    [SerializeField, Min(1)] private int opponentMaxHealth = 6;
    [SerializeField] private TMP_Text playerHealthText;
    [SerializeField] private TMP_Text opponentHealthText;
    [Header("Presentation only: connect the SoulScale prefab here")]
    [SerializeField] private UnityEvent onPlayerVictory = new UnityEvent();
    [SerializeField] private UnityEvent onOpponentVictory = new UnityEvent();
    [SerializeField] private UnityEvent onMatchReset = new UnityEvent();
    private readonly YutDuelRules rules = new YutDuelRules();
    private bool resultPublished;
    public int PlayerHP => rules.PlayerHP;
    public int OpponentHP => rules.OpponentHP;
    public bool IsOver => rules.IsOver;
    public bool PlayerWon => rules.PlayerWon;
    public string Summary => $"HP P:{rules.PlayerHP}/{rules.PlayerMax} E:{rules.OpponentHP}/{rules.OpponentMax}";
    public bool CanUse(bool player, int index) => rules.CanUse(player, index);
    public bool IsResting(bool player, int index) => rules.IsResting(player, index);
    public void BeginTurn(bool player) { rules.BeginTurn(player); }
    public void ResetMatch()
    {
        rules.Reset(Mathf.Max(1, playerMaxHealth), Mathf.Max(1, opponentMaxHealth));
        resultPublished = false; RefreshUI(); onMatchReset.Invoke();
    }
    public int ResolveFinish(bool player, IList<int> members)
    {
        int damage = rules.Finish(player, members); RefreshUI(); return damage;
    }
    // Called once AFTER input is locked and the result camera is ready.
    public void PublishResult()
    {
        if (!rules.IsOver || resultPublished) return;
        resultPublished = true;
        if (rules.PlayerWon) onPlayerVictory.Invoke(); else onOpponentVictory.Invoke();
    }
    private void RefreshUI()
    {
        if (playerHealthText != null) playerHealthText.text = $"PLAYER HP {rules.PlayerHP} / {rules.PlayerMax}";
        if (opponentHealthText != null) opponentHealthText.text = $"OPPONENT HP {rules.OpponentHP} / {rules.OpponentMax}";
    }
#if UNITY_EDITOR
    public void SetHealthForTesting(int playerHP, int opponentHP)
    { rules.SetHealthForTesting(playerHP, opponentHP); resultPublished = false; RefreshUI(); }
#endif
}
