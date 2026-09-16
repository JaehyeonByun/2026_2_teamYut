using System;
using System.Collections.Generic;

// Pure ownership/group rules. Teams are supplied separately by the caller.
public static class YutGroupRules
{
    public static bool IsOnBoard(int node) => node > 0 && node < YutRouteRules.Finished;

    public static List<int> Members(int[] team, int selected)
    {
        if (team == null || selected < 0 || selected >= team.Length)
            throw new ArgumentOutOfRangeException(nameof(selected));
        var result = new List<int>();
        int node = team[selected];
        if (node == YutRouteRules.Finished) return result;
        if (node == YutRouteRules.Waiting) { result.Add(selected); return result; }
        for (int i = 0; i < team.Length; i++)
            if (team[i] == node) result.Add(i);
        return result;
    }

    public static List<int> Captured(int[] enemies, int destination)
    {
        var result = new List<int>();
        if (!IsOnBoard(destination)) return result;
        for (int i = 0; i < enemies.Length; i++)
            if (enemies[i] == destination) result.Add(i);
        return result;
    }

    public static bool AllFinished(int[] team)
    {
        if (team == null || team.Length == 0) return false;
        foreach (int node in team)
            if (node != YutRouteRules.Finished) return false;
        return true;
    }
}

#if UNITY_EDITOR
public static class YutGroupRuleChecks
{
    [UnityEditor.MenuItem("Tools/Yut/Run Group Checks")]
    public static void Run()
    {
        Equal(YutGroupRules.Members(new[] {0, 0, 5, 5}, 0), 0);
        Equal(YutGroupRules.Members(new[] {0, 0, 5, 5}, 2), 2, 3);
        Equal(YutGroupRules.Members(new[] {0, 0, 5, 5}, 3), 2, 3);
        Equal(YutGroupRules.Members(new[] {25, 25, 25, 25}, 0), 0, 1, 2, 3);
        Equal(YutGroupRules.Members(new[] {30, 30, 5, 0}, 0));
        Equal(YutGroupRules.Captured(new[] {25, 25, 0, 30}, 25), 0, 1);
        Equal(YutGroupRules.Captured(new[] {20, 20, 0, 30}, 20), 0, 1);
        Equal(YutGroupRules.Captured(new[] {0, 0, 0, 0}, 0));
        Equal(YutGroupRules.Captured(new[] {30, 30, 0, 0}, 30));
        Equal(YutGroupRules.Captured(new[] {3, 4, 0, 0}, 5));
        if (YutGroupRules.AllFinished(new[] {30, 30, 30, 20})) throw new Exception("Early victory");
        if (!YutGroupRules.AllFinished(new[] {30, 30, 30, 30})) throw new Exception("Missing victory");
        UnityEngine.Debug.Log("Yut group checks passed: 12 cases.");
    }
    private static void Equal(List<int> actual, params int[] expected)
    {
        if (actual.Count != expected.Length) throw new Exception("Group count mismatch");
        for (int i = 0; i < expected.Length; i++)
            if (actual[i] != expected[i]) throw new Exception("Group member mismatch");
    }
}
#endif
