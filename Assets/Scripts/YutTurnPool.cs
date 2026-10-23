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

// One initial throw and at most ONE bonus throw per turn, for either team.
public sealed class YutTurnPool
{
    public const int MaxRollsPerTurn = 2;
    private readonly List<YutSavedResult> results = new List<YutSavedResult>();
    private YutSavedResult inFlight;
    private int nextId;
    public int PendingRolls { get; private set; }
    public int RollsTaken { get; private set; }
    public bool BonusAwarded { get; private set; }
    public IReadOnlyList<YutSavedResult> Results => results.AsReadOnly();
    public bool CanRoll => PendingRolls > 0 && RollsTaken < MaxRollsPerTurn && inFlight == null;
    public bool CanMove => PendingRolls == 0 && results.Count > 0 && inFlight == null;
    public bool IsComplete => PendingRolls == 0 && results.Count == 0 && inFlight == null;

    public void Clear()
    {
        DiscardRemainingActions();
        RollsTaken = 0; BonusAwarded = false;
    }
    public void DiscardRemainingActions()
    { results.Clear(); PendingRolls = 0; inFlight = null; }
    public void BeginTurn() { Clear(); PendingRolls = 1; }
    private void GrantBonusOnce()
    {
        if (BonusAwarded || RollsTaken < 1 || RollsTaken >= MaxRollsPerTurn) return;
        BonusAwarded = true;
        PendingRolls = 1;
    }
    public bool RecordRoll(int steps)
    {
        if (!CanRoll || steps < 1 || steps > 5) return false;
        var result = new YutSavedResult(++nextId, steps);
        PendingRolls--; RollsTaken++;
        results.Add(result);
        if (result.GrantedBonusRoll) GrantBonusOnce();
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
        results.Remove(found); inFlight = found; used = found;
        return true;
    }
    public bool CompleteMove(int capturedCount)
    {
        if (inFlight == null || capturedCount < 0) return false;
        if (capturedCount > 0) GrantBonusOnce();
        inFlight = null;
        return true;
    }
    // Retain old callers, but the old option can no longer bypass the turn cap.
    public bool CompleteMove(int capturedCount, bool allowCaptureBonusAfterYutMo)
    { return CompleteMove(capturedCount); }
}

// Tests do not depend on Unity, so they can also be run by a command-line harness.
public static class YutTurnPoolChecks
{
    public static int RunChecks()
    {
        int checks = 0;
        Action<bool, string> check = (ok, msg) => { if (!ok) throw new Exception(msg); checks++; };
        var counts = new int[6];
        for (int mask = 0; mask < 16; mask++) counts[YutThrow.FromMask(mask).Steps]++;
        check(counts[1]==4 && counts[2]==6 && counts[3]==4 && counts[4]==1 && counts[5]==1, "Face mapping preserved");
        var pool = new YutTurnPool(); YutSavedResult used;
        check(!pool.CanRoll && pool.IsComplete, "Inactive initial pool");
        pool.BeginTurn();
        check(pool.RollsTaken==0 && pool.PendingRolls==1 && !pool.BonusAwarded, "Initial credit");
        check(!pool.RecordRoll(0) && !pool.RecordRoll(6) && pool.RollsTaken==0 && pool.PendingRolls==1, "Invalid roll is atomic");
        check(pool.RecordRoll(2) && pool.RollsTaken==1 && !pool.CanRoll && pool.CanMove, "Normal roll gives no free second roll");
        check(!pool.TryUse(-1,out used) && pool.Count(2)==1, "Invalid selection preserves result");
        check(pool.TryUse(pool.First(2).Id,out used) && !pool.CanMove, "Move locks pool");
        check(!pool.RecordRoll(1) && !pool.CompleteMove(-1), "In-flight validation");
        check(pool.CompleteMove(0) && pool.IsComplete && pool.RollsTaken==1, "Ordinary turn ends after one roll");
        check(!pool.CompleteMove(1) && !pool.CanRoll, "Duplicate landing rejected");
        foreach (int first in new[]{4,5})
        {
            pool.BeginTurn(); pool.RecordRoll(first);
            check(pool.CanRoll && pool.BonusAwarded && pool.RollsTaken==1, "Yut/Mo earns one bonus");
            int id=pool.First(first).Id;
            check(!pool.TryUse(id,out used), "Resolve bonus roll before movement");
            pool.RecordRoll(4);
            check(pool.RollsTaken==2 && pool.PendingRolls==0 && pool.Results.Count==2, "Second Yut cannot earn third throw");
            check(!pool.RecordRoll(1), "Reject third roll");
            pool.TryUse(id,out used); pool.CompleteMove(4,true);
            check(!pool.CanRoll && pool.Results.Count==1, "Capture and legacy option cannot bypass cap");
            pool.TryUse(pool.Results[0].Id,out used); pool.CompleteMove(1);
            check(pool.IsComplete && pool.RollsTaken==2, "Use both results without bonus");
        }
        pool.BeginTurn();pool.RecordRoll(1);pool.TryUse(pool.First(1).Id,out used);pool.CompleteMove(4);
        check(pool.CanRoll && pool.PendingRolls==1 && pool.BonusAwarded, "First capture of stack grants one credit");
        pool.RecordRoll(5);
        check(!pool.CanRoll && pool.RollsTaken==2 && pool.Count(5)==1, "Bonus Mo after capture gives no third throw");
        pool.TryUse(pool.First(5).Id,out used);pool.CompleteMove(1);
        check(pool.IsComplete && !pool.CanRoll, "Second capture gives no third throw");
        pool.BeginTurn();pool.RecordRoll(4);pool.RecordRoll(4);
        int a=pool.Results[0].Id,b=pool.Results[1].Id;
        check(a!=b && pool.Count(4)==2, "Duplicate values retain unique IDs");
        pool.TryUse(b,out used);
        check(used.Id==b && pool.Find(a)!=null && pool.Find(b)==null, "Only selected copy is consumed");
        check(!pool.TryUse(a,out used), "Cannot start a second movement concurrently");
        pool.CompleteMove(0);
        check(pool.CanMove && pool.Count(4)==1, "Other saved copy remains usable");
        pool.TryUse(a,out used);pool.Clear();
        check(!pool.CompleteMove(4) && pool.IsComplete && pool.RollsTaken==0, "Reset cancels in-flight reward");
        pool.BeginTurn();
        check(pool.CanRoll && !pool.BonusAwarded && pool.RollsTaken==0, "Next turn fully resets allowance");
        pool.RecordRoll(4);pool.DiscardRemainingActions();
        check(pool.IsComplete && pool.RollsTaken==1 && pool.BonusAwarded && !pool.RecordRoll(2), "Discard preserves count but cancels earned credit");
        // Exhaustive two-roll/value/capture combinations through the public API.
        for(int first=1;first<=5;first++) for(int second=1;second<=5;second++)
        for(int capture=0;capture<=4;capture++)
        {
            pool.BeginTurn();pool.RecordRoll(first);
            if(pool.CanMove){pool.TryUse(pool.Results[0].Id,out used);pool.CompleteMove(capture);}
            if(pool.CanRoll)pool.RecordRoll(second);
            while(pool.CanMove){pool.TryUse(pool.Results[0].Id,out used);pool.CompleteMove(capture,true);}
            check(pool.IsComplete && pool.RollsTaken<=2 && !pool.CanRoll && !pool.RecordRoll(3), "Exhaustive cap / termination");
        }
        return checks;
    }
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Yut/Run Turn Pool Checks")]
    public static void Run()
    { UnityEngine.Debug.Log("Two-roll turn checks passed: " + RunChecks()); }
#endif
}
