using System.Collections.Generic;

// Preview and execution both use the existing route engine. No second route map.
public sealed class YutDestinationOption
{
    public int NodeId { get; }
    public bool UseShortcut { get; }
    public YutDestinationOption(int nodeId, bool useShortcut)
    { NodeId = nodeId; UseShortcut = useShortcut; }
}

public static class YutDestinationRules
{
    public static List<YutDestinationOption> Build(int startNode, int steps)
    {
        var options = new List<YutDestinationOption>();
        if (startNode < YutRouteRules.Waiting || startNode >= YutRouteRules.Finished || steps < 1 || steps > 5)
            return options;
        Add(options, startNode, steps, false);
        if (YutRouteRules.HasRouteChoice(startNode)) Add(options, startNode, steps, true);
        return options;
    }
    private static void Add(List<YutDestinationOption> options, int start, int steps, bool shortcut)
    {
        var path = YutRouteRules.BuildMove(start, steps, shortcut);
        if (path.Count == 0) return;
        int node = path[path.Count - 1];
        foreach (var option in options) if (option.NodeId == node) return;
        options.Add(new YutDestinationOption(node, shortcut));
    }
}
