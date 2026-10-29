using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(1100)]
[DisallowMultipleComponent]
public sealed class YutStackCountLabel : MonoBehaviour
{
    [SerializeField,Range(0f,1f)] private float horizontalFraction=.5f;
    [SerializeField] private float topGap=.025f;
    [SerializeField] private Color playerColor=new Color(.40f,1f,.84f,1f);
    [SerializeField] private Color opponentColor=new Color(1f,.60f,.27f,1f);
    private bool opponent;
    public void SetOpponent(bool value){opponent=value;}

    [SerializeField,Min(.02f)] private float worldHeight=.16f;
    [SerializeField,Range(0f,.4f)] private float outlineWidth=.24f;
    private int count=1;
    private GameObject root;
    private RectTransform rect;
    private TMP_Text label;
    private TMP_FontAsset ownedFont;
    private Material outlinedMaterial;
    private Camera cameraView;
    private bool warned;
    public void SetCount(int value){count=Mathf.Max(1,value);if(root!=null)root.SetActive(count>1 && isActiveAndEnabled);}
    private void LateUpdate()
    {
        if(count<=1){if(root!=null)root.SetActive(false);return;}
        if(cameraView==null || !cameraView.isActiveAndEnabled)cameraView=Camera.main;
        if(cameraView==null){if(root!=null)root.SetActive(false);return;}
        if(root==null && !Build())return;
        root.SetActive(true);label.text="x"+count;
        label.color=opponent?opponentColor:playerColor;
        Vector3 right=cameraView.transform.right,up=cameraView.transform.up;
        Bounds combined=new Bounds(transform.position,Vector3.zero);
        bool found=false;
        foreach(var renderer in GetComponentsInChildren<Renderer>())
        {
            if(!renderer.enabled || renderer.forceRenderingOff)continue;
            if(!found){combined=renderer.bounds;found=true;}else combined.Encapsulate(renderer.bounds);
        }
        var e=combined.extents;
        float radiusRight=Mathf.Abs(right.x)*e.x+Mathf.Abs(right.y)*e.y+Mathf.Abs(right.z)*e.z;
        float radiusUp=Mathf.Abs(up.x)*e.x+Mathf.Abs(up.y)*e.y+Mathf.Abs(up.z)*e.z;
        float radiusDepth=Mathf.Abs(cameraView.transform.forward.x)*e.x+Mathf.Abs(cameraView.transform.forward.y)*e.y+Mathf.Abs(cameraView.transform.forward.z)*e.z;
        // Center the text halfway between the projected center and right edge, above the piece.
        rect.position=combined.center+right*(radiusRight*horizontalFraction)
            +up*((radiusUp+topGap+worldHeight*.18f)*(2f/3f))
            -cameraView.transform.forward*(radiusDepth+.01f);
        rect.rotation=cameraView.transform.rotation;rect.localScale=Vector3.one*(worldHeight/100f);
        outlinedMaterial.SetFloat("_OutlineWidth",outlineWidth);
        label.UpdateMeshPadding();
    }
    private bool Build()
    {
        var source=YutFontSettings.ResolveSource();
        if(source==null)return false;
        ownedFont=TMP_FontAsset.CreateFontAsset(source);
        var font=ownedFont;
        if(font==null || font.material==null)
        {
            if(!warned){Debug.LogError("YutStackCountLabel: failed to create runtime font. Check TMP Essential Resources and font import settings.",this);warned=true;}
            if(ownedFont!=null)Destroy(ownedFont);ownedFont=null;return false;
        }
        font.atlasPopulationMode=AtlasPopulationMode.Dynamic;
        font.TryAddCharacters("x234",out string missing);
        // Detached world canvas prevents the label entering burn/outline renderer discovery.
        root=new GameObject("StackCount_WorldSpace",typeof(RectTransform),typeof(Canvas),typeof(CanvasGroup));root.layer=gameObject.layer;
        rect=root.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(240,100);rect.pivot=new Vector2(.5f,.5f);
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=cameraView;
        var group=root.GetComponent<CanvasGroup>();group.interactable=false;group.blocksRaycasts=false;
        var go=new GameObject("Count",typeof(RectTransform),typeof(TextMeshProUGUI));go.layer=gameObject.layer;
        label=go.GetComponent<TextMeshProUGUI>();label.rectTransform.SetParent(rect,false);
        label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;
        label.rectTransform.offsetMin=Vector2.zero;label.rectTransform.offsetMax=Vector2.zero;
        label.font=font;label.fontSize=36;label.alignment=TextAlignmentOptions.Center;label.color=new Color(1f,.94f,.76f);
        label.overflowMode=TextOverflowModes.Overflow;label.raycastTarget=false;label.extraPadding=true;
        outlinedMaterial=new Material(font.material){name="StackCount_Outlined"};
        outlinedMaterial.EnableKeyword("OUTLINE_ON");outlinedMaterial.SetFloat("_OutlineWidth",outlineWidth);
        outlinedMaterial.SetColor("_OutlineColor",new Color(.015f,.022f,.03f,1f));label.fontSharedMaterial=outlinedMaterial;
        label.UpdateMeshPadding();return true;
    }
    private void OnDisable(){if(root!=null)root.SetActive(false);}
    private void OnDestroy()
    {
        if(root!=null)Destroy(root);if(outlinedMaterial!=null)Destroy(outlinedMaterial);
        if(ownedFont!=null)
        {if(ownedFont.atlasTextures!=null)foreach(var t in ownedFont.atlasTextures)if(t!=null)Destroy(t);
         if(ownedFont.material!=null)Destroy(ownedFont.material);Destroy(ownedFont);}
    }
}
