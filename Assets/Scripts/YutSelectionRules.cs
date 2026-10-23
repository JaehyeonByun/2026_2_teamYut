using System;

// Waiting pieces are separate; pieces sharing an on-board node are one group.
public static class YutSelectionRules
{
    public static bool SameGroup(int[] nodes, int a, int b)
    {
        if (nodes == null || a < 0 || b < 0 || a >= nodes.Length || b >= nodes.Length) return false;
        return a == b || (YutGroupRules.IsOnBoard(nodes[a]) && nodes[a] == nodes[b]);
    }
    public static int Toggle(int[] nodes, int current, int clicked)
    {
        if (nodes == null || clicked < 0 || clicked >= nodes.Length) return current;
        return SameGroup(nodes, current, clicked) ? -1 : clicked;
    }
    // A destination under another piece means capture/stack; selected group means cancel.
    public static bool PreferDestination(bool destinationHit, bool hitSelectedGroup)
    { return destinationHit && !hitSelectedGroup; }
    public static float Pulse(float elapsed, float period)
    {
        double phase = Math.Max(0f, elapsed) / Math.Max(0.1f, period);
        return (float)(0.5 - 0.5 * Math.Cos(phase * Math.PI * 2));
    }
}
