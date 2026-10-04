using System;
using System.Collections.Generic;

// Pure battle rules. One fresh completion = one damage per piece.
public sealed class YutDuelRules
{
    private readonly bool[,] resting = new bool[2, 4];
    public int PlayerMax { get; private set; }
    public int OpponentMax { get; private set; }
    public int PlayerHP { get; private set; }
    public int OpponentHP { get; private set; }
    public bool IsOver => PlayerHP == 0 || OpponentHP == 0;
    public bool PlayerWon => IsOver && OpponentHP == 0;

    public YutDuelRules(int playerMax = 6, int opponentMax = 6) { Reset(playerMax, opponentMax); }
    public void Reset(int playerMax, int opponentMax)
    {
        if (playerMax < 1 || opponentMax < 1) throw new ArgumentOutOfRangeException("Health must be positive.");
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
        int remaining = Math.Max(0, old - indices.Count);
        if (player) OpponentHP = remaining; else PlayerHP = remaining;
        return old - remaining;
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
