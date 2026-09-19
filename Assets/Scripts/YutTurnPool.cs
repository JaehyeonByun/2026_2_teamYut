using System;
using System.Collections.Generic;

// A result is identified separately from its value, so two 'Yut' results stay distinct.
public sealed class YutSavedResult
{
    public int Id { get; }
    public int Steps { get; }
    public bool GrantedBonusRoll => Steps == 4 || Steps == 5;
    public YutSavedResult(int id, int steps) { Id = id; Steps = steps; }
}

public sealed class YutThrow
{
    // Bit i is 1 when stick i shows its flat face. Four independent 50/50 faces.
    public int FaceMask { get; }
    public int Steps { get; }
    private YutThrow(int mask, int steps) { FaceMask = mask; Steps = steps; }
    public static YutThrow FromMask(int mask)
    {
        if (mask < 0 || mask > 15) throw new ArgumentOutOfRangeException(nameof(mask));
        int flat = 0;
        for (int i = 0; i < 4; i++) flat += (mask >> i) & 1;
        return new YutThrow(mask, flat == 0 ? 5 : flat);
    }
    public static YutThrow Roll(Random random)
    {
        int mask = 0;
        for (int i = 0; i < 4; i++) mask |= random.Next(2) << i;
        return FromMask(mask);
    }
    public static string Name(int steps)
    {
        switch (steps)
        {
            case 1: return "Do";
            case 2: return "Gae";
            case 3: return "Geol";
            case 4: return "Yut";
            case 5: return "Mo";
            default: return "None";
        }
    }
}

// Turn resources only: no Unity scene, movement or camera dependencies.
public sealed class YutTurnPool
{
    private readonly List<YutSavedResult> results = new List<YutSavedResult>();
    private YutSavedResult inFlight;
    private int nextId;
    public int PendingRolls { get; private set; }
    public IReadOnlyList<YutSavedResult> Results => results.AsReadOnly();
    public bool CanRoll => PendingRolls > 0 && inFlight == null;
    public bool CanMove => PendingRolls == 0 && results.Count > 0 && inFlight == null;
    public bool IsComplete => PendingRolls == 0 && results.Count == 0 && inFlight == null;

    public void Clear() { results.Clear(); PendingRolls = 0; inFlight = null; }
    public void BeginTurn() { Clear(); PendingRolls = 1; }
    public bool RecordRoll(int steps)
    {
        if (!CanRoll || steps < 1 || steps > 5) return false;
        var result = new YutSavedResult(++nextId, steps);
        PendingRolls--;
        results.Add(result);
        if (result.GrantedBonusRoll) PendingRolls++;
        return true;
    }
    public int Count(int steps)
    {
        int count = 0;
        foreach (var result in results) if (result.Steps == steps) count++;
        return count;
    }
    public YutSavedResult First(int steps)
    {
        foreach (var result in results) if (result.Steps == steps) return result;
        return null;
    }
    public YutSavedResult Find(int id)
    {
        foreach (var result in results) if (result.Id == id) return result;
        return null;
    }
    public bool TryUse(int id, out YutSavedResult used)
    {
        used = null;
        if (!CanMove) return false;
        var found = Find(id);
        if (found == null) return false;
        results.Remove(found);
        inFlight = found;
        used = found;
        return true;
    }
    // Complete once after landing. Capturing several pieces awards only one roll.
    public bool CompleteMove(int capturedCount, bool allowCaptureBonusAfterYutMo)
    {
        if (inFlight == null || capturedCount < 0) return false;
        if (capturedCount > 0 && (!inFlight.GrantedBonusRoll || allowCaptureBonusAfterYutMo)) PendingRolls++;
        inFlight = null;
        return true;
    }
}

#if UNITY_EDITOR
public static class YutTurnPoolChecks
{
    [UnityEditor.MenuItem("Tools/Yut/Run Turn Pool Checks")]
    public static void Run()
    {
        int checks = 0;
        Action<bool, string> check = (condition, message) =>
        { checks++; if (!condition) throw new Exception(message); };
        var counts = new int[6];
        for (int mask = 0; mask < 16; mask++) counts[YutThrow.FromMask(mask).Steps]++;
        check(counts[1] == 4 && counts[2] == 6 && counts[3] == 4 && counts[4] == 1 && counts[5] == 1, "Face mapping");
        var pool = new YutTurnPool();
        YutSavedResult used;
        pool.BeginTurn();
        check(pool.PendingRolls == 1 && pool.Results.Count == 0, "Initial resources");
        check(pool.RecordRoll(4) && pool.PendingRolls == 1, "Yut bonus");
        int yutId = pool.First(4).Id;
        check(!pool.TryUse(yutId, out used), "Must roll before moving");
        check(pool.RecordRoll(2) && pool.PendingRolls == 0 && pool.Results.Count == 2, "Keep both results");
        check(!pool.RecordRoll(1), "Reject unearned roll");
        check(pool.TryUse(pool.First(2).Id, out used) && pool.Count(4) == 1 && pool.Count(2) == 0, "Choose order");
        check(!pool.TryUse(yutId, out used), "Reject overlapping movement");
        check(pool.CompleteMove(2, false) && pool.PendingRolls == 1, "Capture stack gives one roll");
        check(!pool.CompleteMove(2, false) && pool.PendingRolls == 1, "No duplicate reward");
        check(pool.RecordRoll(1) && pool.Count(4) == 1, "Capture does not erase saved result");
        check(pool.TryUse(yutId, out used) && pool.CompleteMove(1, false) && pool.PendingRolls == 0, "No double bonus by default");
        check(pool.TryUse(pool.First(1).Id, out used) && pool.CompleteMove(0, false) && pool.IsComplete, "Turn ends only when empty");
        pool.BeginTurn(); pool.RecordRoll(4); pool.RecordRoll(4); pool.RecordRoll(1);
        check(pool.Count(4) == 2 && pool.Results[0].Id != pool.Results[1].Id, "Duplicate values have unique IDs");
        pool.TryUse(pool.First(4).Id, out used); pool.CompleteMove(1, true);
        check(pool.PendingRolls == 1 && pool.Count(4) == 1, "Optional capture bonus");
        pool.Clear();
        check(pool.IsComplete && pool.Results.Count == 0, "Clear all state");
        pool.BeginTurn(); pool.RecordRoll(5);
        check(pool.PendingRolls == 1 && pool.Count(5) == 1, "Mo bonus");
        pool.RecordRoll(1); pool.TryUse(pool.First(1).Id, out used); pool.Clear();
        check(!pool.CompleteMove(1, false) && pool.PendingRolls == 0, "Reset cancels pending reward");
        UnityEngine.Debug.Log($"Yut turn pool checks passed: {checks} checks.");
    }
}
#endif
