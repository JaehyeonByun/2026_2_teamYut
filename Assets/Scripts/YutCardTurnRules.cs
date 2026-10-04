using System;

// First card prototype: both sides own a finite supply and one card action per own turn.
public sealed class YutCardTurnRules
{
    private readonly int[] copies = new int[2];
    private readonly bool[] used = new bool[2];
    private static int Side(bool player) => player ? 0 : 1;
    public int Count(bool player) => copies[Side(player)];
    public bool CanUse(bool player) => Count(player) > 0 && !used[Side(player)];
    public void Reset(int startingCopies)
    {
        if (startingCopies < 0) throw new ArgumentOutOfRangeException(nameof(startingCopies));
        copies[0] = copies[1] = startingCopies; used[0] = used[1] = false;
    }
    public void BeginTurn(bool player) { used[Side(player)] = false; }
    public bool TryConsume(bool player)
    {
        if (!CanUse(player)) return false;
        copies[Side(player)]--; used[Side(player)] = true; return true;
    }
}
