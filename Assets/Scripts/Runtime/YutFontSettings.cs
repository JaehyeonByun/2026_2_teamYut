using TMPro;
using UnityEngine;

public sealed class YutFontSettings : ScriptableObject
{
    public Font sourceFont;
    public TMP_FontAsset fontAsset; // Kept for compatibility; source font takes priority.
    private static bool warned;
    public static YutFontSettings Load() => Resources.Load<YutFontSettings>("YutFontSettings");
    public static Font ResolveSource()
    {
        var settings=Load();
        if(settings!=null && settings.sourceFont!=null)return settings.sourceFont;
#if UNITY_EDITOR
        // Editor-only recovery. The setup menu persists this reference for builds.
        var font=UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/YutFont/Jalnan.ttf");
        if(font!=null)return font;
#endif
        if(!warned){Debug.LogError("Yut font is not connected. Stop Play and run Tools > Yut Duel > Fonts > Connect Original Font. No TMP Font Asset is required.");warned=true;}
        return null;
    }
}
