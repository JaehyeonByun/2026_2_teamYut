using System.Collections.Generic;
using System.Text;

public sealed class YutMoveLeg
{
    public int StartNode { get; }
    public int NodeId { get; }
    public bool UseShortcut { get; }
    public int ResultId { get; }
    public int Steps { get; }
    public YutMoveLeg(int start,int node,bool shortcut,int resultId,int steps)
    { StartNode=start;NodeId=node;UseShortcut=shortcut;ResultId=resultId;Steps=steps; }
}
public sealed class YutMoveOption
{
    private readonly System.Collections.ObjectModel.ReadOnlyCollection<YutMoveLeg> legs;
    public IReadOnlyList<YutMoveLeg> Legs => legs;
    public int NodeId => legs[legs.Count-1].NodeId;
    public bool IsCombo => legs.Count>1;
    public int TotalSteps { get; }
    public YutMoveOption(params YutMoveLeg[] moves)
    {
        if(moves==null || moves.Length<1 || moves.Length>2) throw new System.ArgumentException("One or two moves required.");
        var copy=new List<YutMoveLeg>(moves);
        foreach(var leg in copy) { if(leg==null) throw new System.ArgumentException("Null move."); TotalSteps+=leg.Steps; }
        legs=copy.AsReadOnly();
    }
    public string Describe()
    {
        var text=new StringBuilder(IsCombo?"Use together: ":"Use: ");
        for(int i=0;i<legs.Count;i++)
        {
            if(i>0) text.Append(" -> ");
            text.Append(YutThrow.Name(legs[i].Steps)).Append(" (").Append(YutRouteRules.NodeName(legs[i].NodeId)).Append(")");
        }
        return text.ToString();
    }
}
public static class YutMoveOptions
{
    public static List<YutMoveOption> Build(int startNode,IReadOnlyList<YutSavedResult> results)
    {
        var destinations=new Dictionary<int,YutMoveOption>();
        if(results==null) return new List<YutMoveOption>();
        foreach(var first in results)
        {
            if(first==null) continue;
            foreach(var route in YutDestinationRules.Build(startNode,first.Steps))
            {
                var leg=new YutMoveLeg(startNode,route.NodeId,route.UseShortcut,first.Id,first.Steps);
                Add(destinations,new YutMoveOption(leg));
                // A finished piece rests; a second result cannot move that same piece again.
                if(route.NodeId==YutRouteRules.Finished) continue;
                foreach(var second in results)
                {
                    if(second==null || second.Id==first.Id) continue;
                    foreach(var next in YutDestinationRules.Build(route.NodeId,second.Steps))
                        Add(destinations,new YutMoveOption(leg,new YutMoveLeg(route.NodeId,next.NodeId,next.UseShortcut,second.Id,second.Steps)));
                }
            }
        }
        var options=new List<YutMoveOption>(destinations.Values);
        options.Sort((a,b)=>a.NodeId.CompareTo(b.NodeId));
        return options;
    }
    private static void Add(Dictionary<int,YutMoveOption> choices,YutMoveOption candidate)
    {
        YutMoveOption previous;
        if(!choices.TryGetValue(candidate.NodeId,out previous)||Prefer(candidate,previous)) choices[candidate.NodeId]=candidate;
    }
    private static bool Prefer(YutMoveOption a,YutMoveOption b)
    {
        // Avoid wasting a result; for equal costs, prefer the older result order.
        if(a.Legs.Count!=b.Legs.Count) return a.Legs.Count<b.Legs.Count;
        if(a.TotalSteps!=b.TotalSteps) return a.TotalSteps<b.TotalSteps;
        for(int i=0;i<a.Legs.Count;i++)
            if(a.Legs[i].ResultId!=b.Legs[i].ResultId) return a.Legs[i].ResultId<b.Legs[i].ResultId;
        for(int i=0;i<a.Legs.Count;i++)
            if(a.Legs[i].UseShortcut!=b.Legs[i].UseShortcut) return !a.Legs[i].UseShortcut;
        return false;
    }
    public static bool Matches(YutMoveOption a,YutMoveOption b)
    {
        if(a==null||b==null||a.Legs.Count!=b.Legs.Count) return false;
        for(int i=0;i<a.Legs.Count;i++)
        {
            var x=a.Legs[i];var y=b.Legs[i];
            if(x.StartNode!=y.StartNode||x.NodeId!=y.NodeId||x.UseShortcut!=y.UseShortcut||x.ResultId!=y.ResultId||x.Steps!=y.Steps) return false;
        }
        return true;
    }
    public static bool CanCapture(IReadOnlyList<YutMoveOption> options,int node) => CanLandOn(options,node);
    public static bool CanLandOn(IReadOnlyList<YutMoveOption> options,int node)
    {
        if(options==null||!YutGroupRules.IsOnBoard(node)) return false;
        foreach(var option in options) if(option.NodeId==node) return true;
        return false;
    }
}
