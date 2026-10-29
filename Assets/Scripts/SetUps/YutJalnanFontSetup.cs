#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
public static class YutJalnanFontSetup
{
    [MenuItem("Tools/Yut Duel/Fonts/Connect Original Font")]
    public static void ConnectOriginal()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogWarning("Stop Play mode first.");return;}
        const string folder="Assets/Fonts/YutFont";
        Font source=AssetDatabase.LoadAssetAtPath<Font>(folder+"/Jalnan.ttf");
        if(source==null && AssetDatabase.IsValidFolder(folder))
        {
            var ids=AssetDatabase.FindAssets("t:Font",new[]{folder});
            if(ids.Length==1)source=AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(ids[0]));
        }
        if(source==null){Debug.LogError("Place Jalnan.ttf in Assets/Fonts/YutFont, or select its reference manually in YutFontSettings. Multiple font files are not chosen automatically.");return;}
        if(!AssetDatabase.IsValidFolder("Assets/Resources"))AssetDatabase.CreateFolder("Assets","Resources");
        const string path="Assets/Resources/YutFontSettings.asset";
        var settings=AssetDatabase.LoadAssetAtPath<YutFontSettings>(path);
        if(settings==null){settings=ScriptableObject.CreateInstance<YutFontSettings>();AssetDatabase.CreateAsset(settings,path);}
        Undo.RecordObject(settings,"Connect original Yut font");
        settings.sourceFont=source;
        EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();
        Selection.activeObject=settings;EditorGUIUtility.PingObject(settings);
        Debug.Log("Yut original font connected: "+AssetDatabase.GetAssetPath(source)+". No saved TMP Font Asset required.",settings);
    }
    [MenuItem("Tools/Yut Duel/Fonts/Create Jalnan TMP Font")]
    public static void Create()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogWarning("Stop Play mode first.");return;}
        const string path="Assets/Fonts/YutFont/JalnanSDF.asset";
        var existing=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if(existing!=null){Connect(existing);Selection.activeObject=existing;EditorGUIUtility.PingObject(existing);return;}
        var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/YutFont/Jalnan.ttf");
        if(source==null){Debug.LogError("Import Assets/Fonts/YutFont/Jalnan.ttf first.");return;}
        var font=TMP_FontAsset.CreateFontAsset(source);font.name="JalnanSDF";
        font.atlasPopulationMode=AtlasPopulationMode.Dynamic;font.isMultiAtlasTexturesEnabled=true;
        font.TryAddCharacters("x0123456789 /현재최대체력보드천칭정면던지기플레이어상대도개걸윷모",out string missing);
        AssetDatabase.CreateAsset(font,path);
        if(font.material!=null)AssetDatabase.AddObjectToAsset(font.material,font);
        if(font.atlasTextures!=null)foreach(var atlas in font.atlasTextures)if(atlas!=null)AssetDatabase.AddObjectToAsset(atlas,font);
        EditorUtility.SetDirty(font);Connect(font);AssetDatabase.SaveAssets();Selection.activeObject=font;EditorGUIUtility.PingObject(font);
        Debug.Log("JalnanSDF created. Assign it to a TextMeshPro Font Asset field. Original TTF and license must remain with your project.");
    }
    private static void Connect(TMP_FontAsset font)
    {
        var settings=YutFontSettings.Load();
        if(settings==null){Debug.LogError("Import Assets/Resources/YutFontSettings.asset first.");return;}
        Undo.RecordObject(settings,"Connect Yut font");
        settings.fontAsset=font;
        EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();
    }
}

#endif
