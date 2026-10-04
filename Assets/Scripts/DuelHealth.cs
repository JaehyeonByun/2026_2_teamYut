using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

// Manager owns match timing; health never starts turns or resets itself in Awake.
public sealed class DuelHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int playerMaxHealth = 100;
    [SerializeField, Min(1)] private int opponentMaxHealth = 100;
    [SerializeField, Min(1)] private int damagePerFinishedPiece = 20;
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
    public int PlayerMaxHP => rules.PlayerMax;
    public int OpponentMaxHP => rules.OpponentMax;
    public bool IsOver => rules.IsOver;
    public bool PlayerWon => rules.PlayerWon;
    public string Summary => $"HP P:{rules.PlayerHP}/{rules.PlayerMax} E:{rules.OpponentHP}/{rules.OpponentMax}";
    public bool CanUse(bool player, int index) => rules.CanUse(player, index);
    public bool IsResting(bool player, int index) => rules.IsResting(player, index);
    public void BeginTurn(bool player) { rules.BeginTurn(player); }
    public void ResetMatch()
    {
        rules.Reset(Mathf.Max(1, playerMaxHealth), Mathf.Max(1, opponentMaxHealth), Mathf.Max(1, damagePerFinishedPiece));
        resultPublished = false; RefreshUI(); onMatchReset.Invoke();
    }
    public int ResolveFinish(bool player, IList<int> members)
    {
        int damage = rules.Finish(player, members); RefreshUI(); return damage;
    }
    public int Heal(bool player, int amount)
    { int restored = rules.Heal(player, amount); RefreshUI(); return restored; }
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
    [ContextMenu("Settings/Apply 100 HP and 20 Damage")]
    private void ApplyCombatPreset()
    {
        if (Application.isPlaying) { Debug.LogWarning("Stop Play mode before applying the combat preset.", this); return; }
        UnityEditor.Undo.RecordObject(this, "Set duel health and damage");
        playerMaxHealth = opponentMaxHealth = 100; damagePerFinishedPiece = 20;
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
    }
    public void SetHealthForTesting(int playerHP, int opponentHP)
    { rules.SetHealthForTesting(playerHP, opponentHP); resultPublished = false; RefreshUI(); }
#endif
}
