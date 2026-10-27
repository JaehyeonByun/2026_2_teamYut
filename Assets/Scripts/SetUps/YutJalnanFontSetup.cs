using TMPro;
using UnityEditor;
using UnityEngine;
public static class YutJalnanFontSetup
{
    [MenuItem("Tools/Yut Duel/Fonts/Create Jalnan TMP Font")]
    public static void Create()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogWarning("Stop Play mode first.");return;}
        const string path="Assets/YutFonts/Resources/YutFonts/JalnanSDF.asset";
        var existing=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if(existing!=null){Selection.activeObject=existing;EditorGUIUtility.PingObject(existing);return;}
        var source=Resources.Load<Font>("YutFonts/Jalnan");
        if(source==null){Debug.LogError("Import Assets/YutFonts/Resources/YutFonts/Jalnan.ttf first.");return;}
        var font=TMP_FontAsset.CreateFontAsset(source);font.name="JalnanSDF";
        font.atlasPopulationMode=AtlasPopulationMode.Dynamic;font.isMultiAtlasTexturesEnabled=true;
        font.TryAddCharacters("0123456789 /현재최대체력보드천칭정면던지기플레이어상대도개걸윷모",out string missing);
        AssetDatabase.CreateAsset(font,path);
        if(font.material!=null)AssetDatabase.AddObjectToAsset(font.material,font);
        if(font.atlasTextures!=null)foreach(var atlas in font.atlasTextures)if(atlas!=null)AssetDatabase.AddObjectToAsset(atlas,font);
        EditorUtility.SetDirty(font);AssetDatabase.SaveAssets();Selection.activeObject=font;EditorGUIUtility.PingObject(font);
        Debug.Log("JalnanSDF created. Assign it to a TextMeshPro Font Asset field. Original TTF and license must remain with your project.");
    }
}
