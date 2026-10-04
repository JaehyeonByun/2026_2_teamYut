using System;
using System.Collections.Generic;

// Pure battle rules. Damage and healing values are configurable.
public sealed class YutDuelRules
{
    private readonly bool[,] resting = new bool[2, 4];
    public int PlayerMax { get; private set; }
    public int OpponentMax { get; private set; }
    public int PlayerHP { get; private set; }
    public int OpponentHP { get; private set; }
    public int DamagePerPiece { get; private set; }
    public bool IsOver => PlayerHP == 0 || OpponentHP == 0;
    public bool PlayerWon => IsOver && OpponentHP == 0;

    public YutDuelRules(int playerMax = 100, int opponentMax = 100, int damagePerPiece = 20)
    { Reset(playerMax, opponentMax, damagePerPiece); }
    public void Reset(int playerMax, int opponentMax, int damagePerPiece = 20)
    {
        if (playerMax < 1 || opponentMax < 1) throw new ArgumentOutOfRangeException("Health must be positive.");
        if (damagePerPiece < 1) throw new ArgumentOutOfRangeException("Damage must be positive.");
        DamagePerPiece = damagePerPiece;
        PlayerMax = PlayerHP = playerMax; OpponentMax = OpponentHP = opponentMax;
        Array.Clear(resting, 0, resting.Length);
    }
    public bool CanUse(bool player, int index)
    { return !IsOver && index >= 0 && index < 4 && !resting[player ? 0 : 1, index]; }
    public bool IsResting(bool player, int index)
    { return index >= 0 && index < 4 && resting[player ? 0 : 1, index]; }
    public void BeginTurn(bool player)
    {
        if (IsOver) return;
        for (int i = 0; i < 4; i++) resting[player ? 0 : 1, i] = false;
    }
    public int Finish(bool player, IList<int> indices)
    {
        if (IsOver) return 0;
        if (indices == null || indices.Count < 1 || indices.Count > 4)
            throw new ArgumentException("Provide the just-completed moving group.");
        var unique = new HashSet<int>();
        foreach (int i in indices)
            if (i < 0 || i >= 4 || !unique.Add(i) || resting[player ? 0 : 1, i])
                throw new ArgumentException("Invalid or already completed piece; damage was not applied.");
        foreach (int i in indices) resting[player ? 0 : 1, i] = true;
        int old = player ? OpponentHP : PlayerHP;
        int remaining = (int)Math.Max(0L, old - (long)indices.Count * DamagePerPiece);
        if (player) OpponentHP = remaining; else PlayerHP = remaining;
        return old - remaining;
    }
    public int Heal(bool player, int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (IsOver) return 0;
        int current = player ? PlayerHP : OpponentHP;
        int maximum = player ? PlayerMax : OpponentMax;
        int restored = (int)Math.Min((long)amount, maximum - current);
        if (player) PlayerHP += restored; else OpponentHP += restored;
        return restored;
    }
#if UNITY_EDITOR
    public void SetHealthForTesting(int playerHP, int opponentHP)
    {
        if (playerHP < 1 || playerHP > PlayerMax || opponentHP < 1 || opponentHP > OpponentMax)
            throw new ArgumentOutOfRangeException("Test health must be within max health and above zero.");
        PlayerHP = playerHP; OpponentHP = opponentHP;
        Array.Clear(resting, 0, resting.Length);
    }
#endif
}
