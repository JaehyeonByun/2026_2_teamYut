using System;

// Also executable outside Unity: pure-rule regression checks.
public static class YutDuelRuleChecks
{
    public static int RunChecks()
    {
        int checks = 0;
        Action<bool, string> check = (ok, message) => { if (!ok) throw new Exception(message); checks++; };
        var battle = new YutDuelRules(6,6,1);
        check(battle.PlayerHP == 6 && battle.OpponentHP == 6 && !battle.IsOver, "Initial HP");
        check(battle.Finish(true, new[] {0}) == 1 && battle.OpponentHP == 5 && battle.PlayerHP == 6, "Single finish hits opponent");
        check(!battle.CanUse(true, 0) && battle.CanUse(true, 1), "Only completed piece rests");
        bool rejected = false;
        try { battle.Finish(true, new[] {0}); } catch (ArgumentException) { rejected = true; }
        check(rejected && battle.OpponentHP == 5, "Duplicate completion cannot hit twice");
        battle.BeginTurn(false);
        check(!battle.CanUse(true, 0), "Opponent turn must not release player rest");
        battle.BeginTurn(true);
        check(battle.CanUse(true, 0), "Next own turn releases rest");
        check(battle.Finish(true, new[] {0,1}) == 2 && battle.OpponentHP == 3, "Stack finish deals group damage");
        check(battle.Finish(false, new[] {2,3}) == 2 && battle.PlayerHP == 4, "Opponent attack is symmetric");
        battle.Reset(6,6,1);
        rejected = false;
        try { battle.Finish(true, new[] {1,1}); } catch (ArgumentException) { rejected = true; }
        check(rejected && battle.OpponentHP == 6 && battle.CanUse(true,1), "Invalid group is rejected atomically");
        check(battle.Finish(true, new[] {0,1,2,3}) == 4 && battle.OpponentHP == 2 && !battle.IsOver, "Four finishes are not automatic victory");
        for (int i = 0; i < 4; i++) check(!battle.CanUse(true,i), "Every completed piece rests");
        battle.BeginTurn(true);
        check(battle.Finish(true, new[] {0,1,2,3}) == 2 && battle.OpponentHP == 0 && battle.PlayerWon, "Overkill clamps to zero");
        check(!battle.CanUse(false,0) && !battle.CanUse(true,0), "Game over blocks both teams");
        check(battle.Finish(false, new[] {0}) == 0 && battle.PlayerHP == 6, "No retaliation after defeat");
        battle.Reset(1,6,1); battle.Finish(false, new[] {0});
        check(battle.IsOver && !battle.PlayerWon && battle.PlayerHP == 0, "Player defeat");
        battle.Reset(6,6,1);
        check(battle.CanUse(true,0) && battle.CanUse(false,0) && !battle.IsOver, "Reset restores health and pieces");

        // Real existing route + group + roll-pool rules, not alternate copies.
        var moving = YutGroupRules.Members(new[] {20,20,0,0},0);
        var route = YutRouteRules.BuildMove(20,1,false);
        check(moving.Count == 2 && route[route.Count-1] == YutRouteRules.Finished, "Two-piece finish path");
        check(YutGroupRules.Members(new[] {30,30,0,0},0).Count == 0, "Group must be saved before moving");
        var pool = new YutTurnPool(); pool.BeginTurn();
        check(pool.RecordRoll(4) && pool.RecordRoll(1), "Yut then Do saved");
        YutSavedResult used;
        check(pool.TryUse(pool.First(1).Id, out used) && pool.CompleteMove(0,false), "Finish consumes one result without capture bonus");
        battle.Finish(true,moving);
        check(pool.Count(4) == 1 && pool.PendingRolls == 0 && battle.OpponentHP == 4, "Saved result survives nonfatal partial finish");
        check(YutGroupRules.Captured(new[] {0,0,0,0},0).Count == 0, "Resting waiting pieces cannot be captured");
        var hp = battle.OpponentHP;
        check(YutGroupRules.Captured(new[] {5,5,0,0},5).Count == 2 && battle.OpponentHP == hp, "Capture rule itself has no health damage");
        pool.Clear();
        check(pool.IsComplete && pool.Results.Count == 0 && pool.PendingRolls == 0, "Clearing turn removes saved rolls and credits");
        var standard = new YutDuelRules();
        check(standard.PlayerHP == 100 && standard.OpponentHP == 100, "Default 100 HP");
        check(standard.Finish(true,new[]{0}) == 20 && standard.OpponentHP == 80, "Default 20 damage");
        check(standard.Heal(false,20) == 20 && standard.OpponentHP == 100, "Heal opponent symmetrically");
        check(standard.Heal(false,20) == 0, "Cannot overheal full HP");
        check(standard.Finish(false,new[]{0,1}) == 40 && standard.PlayerHP == 60, "Two piece damage 40");
        check(standard.Heal(true,30) == 30 && standard.PlayerHP == 90, "Configurable heal");
        check(standard.Heal(true,20) == 10 && standard.PlayerHP == 100, "Clamp healing to maximum");
        standard.BeginTurn(true); standard.Finish(true,new[]{0,1,2,3});
        standard.BeginTurn(true); standard.Finish(true,new[]{0});
        check(standard.IsOver && standard.Heal(false,20) == 0 && standard.OpponentHP == 0, "No resurrection after game over");
        var cards = new YutCardTurnRules(); cards.Reset(2);
        check(cards.Count(true)==2 && cards.Count(false)==2, "Equal starting card supply");
        check(cards.TryConsume(true) && !cards.TryConsume(true) && cards.Count(true)==1, "One card per own turn");
        cards.BeginTurn(false);
        check(!cards.CanUse(true) && cards.TryConsume(false), "Opponent turn does not refresh player limit");
        cards.BeginTurn(true);
        check(cards.TryConsume(true) && cards.Count(true)==0, "Next own turn resets only usage limit");
        cards.BeginTurn(true);
        check(!cards.CanUse(true) && !cards.TryConsume(true), "Exhausted cards do not regenerate");
        cards.Reset(1);
        check(cards.CanUse(true) && cards.CanUse(false) && cards.Count(true)==1, "Restart restores card supply");
        return checks;
    }
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Yut/Run Duel Health Checks")]
    public static void RunInUnity()
    { UnityEngine.Debug.Log("Duel health rule checks passed: " + RunChecks()); }
#endif
}
