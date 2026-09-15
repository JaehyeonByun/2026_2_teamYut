using System;
using System.Collections.Generic;

// 0: waiting, 1..20: outer nodes, 30: finished.
// A: 5 -> 21 -> 22 -> 25(center) -> 23 -> 24 -> 15.
// B: 10 -> 26 -> 27 -> 25(center) -> 28 -> 29 -> 20.
public static class YutRouteRules
{
    public const int Waiting = 0;
    public const int Center = 25;
    public const int Finished = 30;
    public static bool HasRouteChoice(int node) => node == 5 || node == 10;

    public static List<int> BuildMove(int startNode, int steps, bool useShortcut)
    {
        if (startNode < Waiting || startNode > Finished)
            throw new ArgumentOutOfRangeException(nameof(startNode));
        if (steps < 1 || steps > 5)
            throw new ArgumentOutOfRangeException(nameof(steps));
        var path = new List<int>();
        int current = startNode;
        int previous = -1;
        for (int i = 0; i < steps && current != Finished; i++)
        {
            int next = NextNode(current, previous, i == 0 && useShortcut);
            path.Add(next);
            previous = current;
            current = next;
        }
        return path;
    }

    private static int NextNode(int node, int previous, bool enterShortcut)
    {
        if (node == Waiting) return 1;
        if (node == 5 && enterShortcut) return 21;
        if (node == 10 && enterShortcut) return 26;
        if (node >= 1 && node < 20) return node + 1;
        if (node == 20) return Finished;
        switch (node)
        {
            case 21: return 22;
            case 22: return Center;
            // Passing from A stays on A. A new move from center heads toward home.
            case Center: return previous == 22 ? 23 : 28;
            case 23: return 24;
            case 24: return 15;
            case 26: return 27;
            case 27: return Center;
            case 28: return 29;
            case 29: return 20;
            default: throw new InvalidOperationException("Invalid route node: " + node);
        }
    }

    public static string NodeName(int node)
    {
        if (node == Waiting) return "Waiting";
        if (node == Finished) return "Finished";
        if (node == Center) return "Center";
        if (node >= 1 && node <= 20) return "Node_" + node.ToString("00");
        if (node >= 21 && node <= 24) return "A_" + (node - 20).ToString("00");
        if (node >= 26 && node <= 29) return "B_" + (node - 25).ToString("00");
        throw new ArgumentOutOfRangeException(nameof(node));
    }
}

#if UNITY_EDITOR
public static class YutRouteRuleChecks
{
    [UnityEditor.MenuItem("Tools/Yut/Run Route Checks")]
    public static void Run()
    {
        Check(0, 1, false, 1);
        Check(0, 5, false, 1, 2, 3, 4, 5);
        Check(4, 3, true, 5, 6, 7);
        Check(9, 3, true, 10, 11, 12);
        Check(5, 3, false, 6, 7, 8);
        Check(5, 3, true, 21, 22, 25);
        Check(5, 4, true, 21, 22, 25, 23);
        Check(10, 4, true, 26, 27, 25, 28);
        Check(21, 5, false, 22, 25, 23, 24, 15);
        Check(22, 4, false, 25, 23, 24, 15);
        Check(25, 1, false, 28);
        Check(25, 3, false, 28, 29, 20);
        Check(25, 4, false, 28, 29, 20, 30);
        Check(24, 2, false, 15, 16);
        Check(27, 5, false, 25, 28, 29, 20, 30);
        Check(29, 1, false, 20);
        Check(19, 1, false, 20);
        Check(19, 2, false, 20, 30);
        Check(20, 1, false, 30);
        Check(20, 5, false, 30);
        Check(30, 5, false);
        UnityEngine.Debug.Log("Yut route checks passed: 21 cases.");
    }
    private static void Check(int start, int steps, bool shortcut, params int[] expected)
    {
        List<int> actual = YutRouteRules.BuildMove(start, steps, shortcut);
        if (actual.Count != expected.Length)
            throw new Exception($"Route check failed at {start}: length {actual.Count}");
        for (int i = 0; i < expected.Length; i++)
            if (actual[i] != expected[i])
                throw new Exception($"Route check failed at {start}, step {i}: {actual[i]} != {expected[i]}");
    }
}
#endif
