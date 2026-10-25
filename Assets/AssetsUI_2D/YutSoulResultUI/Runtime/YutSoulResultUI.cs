using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class YutSoulResultUI : MonoBehaviour
{
    [Header("Replaceable artwork and font (bundled defaults if empty)")]
    [SerializeField] private Texture2D flameTexture;
    [SerializeField] private TMP_FontAsset koreanFont;
    [Header("Optional board marker; otherwise the board centre is used")]
    [SerializeField] private Transform resultAnchor;
    [Header("World-space reveal")]
    [SerializeField] private bool revealInWorldSpace = true;
    [SerializeField, Min(.01f)] private float worldWidth = .24f;
    [SerializeField] private Vector3 worldOffset = new Vector3(0,.12f,0);
    [SerializeField, Range(-35,35)] private float worldTiltDegrees = 10;
    [SerializeField, Min(0)] private float floatAmplitude = .006f;
    [SerializeField] private Vector2 popupOffset = new Vector2(0, 80);
    [SerializeField] private Vector2 bankOffset = new Vector2(28, -28);
    [SerializeField, Min(50)] private float slotSpacing = 90;
    [SerializeField, Min(.1f)] private float popupScale = 1.35f;
    [SerializeField, Min(.1f)] private float dockScale = .95f;
    [Header("Timing")]
    [SerializeField, Min(.01f)] private float appearSeconds = .15f;
    [SerializeField, Min(0)] private float readSeconds = 1.8f;
    [SerializeField, Min(.01f)] private float travelSeconds = .60f;
    [SerializeField, Min(.01f)] private float settleSeconds = .15f;
    [SerializeField, Min(.01f)] private float consumeSeconds = .28f;
    [SerializeField, Min(0)] private float arcHeight = 100;
    [SerializeField] private bool showTrail = true;
    public bool IsReady { get; private set; }
    public float PresentationSeconds => Mathf.Max(.01f,appearSeconds)+Mathf.Max(0,readSeconds)+Mathf.Max(.01f,travelSeconds)+Mathf.Max(.01f,settleSeconds);
    class Token
    {
        public int id, steps, slot;
        public RectTransform root, flame;
        public CanvasGroup group;
        public TMP_Text number;
        public bool removing, travelling;
    }
    readonly List<Token> tokens = new List<Token>();
    readonly List<RectTransform> trails = new List<RectTransform>();
    readonly List<Vector2> trailPositions = new List<Vector2>();
    Camera worldCamera;
    Canvas canvas, worldCanvas;
    RectTransform worldRect;
    RectTransform canvasRect, safeRoot, bank;
    TMP_Text title, emptyLabel;
    TMP_FontAsset font, ownedFont;
    bool playerBank = true;
    int generation;
    Rect lastSafeArea;
    Vector2Int lastScreen;

    public bool Initialize(Camera camera, TMP_FontAsset fallbackFont)
    {
        if (IsReady) return true;
        worldCamera=camera;
        if (!flameTexture) flameTexture=Resources.Load<Texture2D>("YutSoulResultUI/SoulFlame");
        font=koreanFont;
        if(!font)
        {
            var source=Resources.Load<Font>("YutSoulResultUI/GowunBatang-Bold");
            if(source) { ownedFont=TMP_FontAsset.CreateFontAsset(source); font=ownedFont; }
        }
        if(!font) font=fallbackFont;
        if(!font || !flameTexture || !worldCamera)
        { Debug.LogWarning("YutSoulResultUI: missing Camera, font or SoulFlame.png; the original results UI remains active.",this); return false; }
        string missing;
        font.TryAddCharacters("도개걸윷모내상대결과보유칸없음012345 ·",out missing);
        if(!font.HasCharacter('윷')) Debug.LogWarning("YutSoulResultUI: font has no Korean Yut glyph. Assign a Korean TMP Font Asset.",this);
        var go=new GameObject("Soul result canvas (runtime)",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(CanvasGroup));
        go.transform.SetParent(transform,false);
        canvas=go.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=80;
        canvasRect=go.GetComponent<RectTransform>();
        var scaler=go.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight=.5f;
        var group=go.GetComponent<CanvasGroup>(); group.interactable=false; group.blocksRaycasts=false;
        // No GraphicRaycaster: the result UI cannot intercept board/piece clicks.
        safeRoot=Rect("Safe area",canvasRect,Vector2.zero); safeRoot.anchorMin=Vector2.zero; safeRoot.anchorMax=Vector2.one; safeRoot.offsetMin=Vector2.zero; safeRoot.offsetMax=Vector2.zero;
        bank=Rect("Saved results",safeRoot,new Vector2(270,205)); bank.anchorMin=bank.anchorMax=new Vector2(0,1); bank.pivot=new Vector2(0,1); bank.anchoredPosition=bankOffset;
        title=Text("Owner",bank,new Vector2(240,40),26); title.rectTransform.anchoredPosition=new Vector2(-15,75);
        emptyLabel=Text("Empty",bank,new Vector2(240,30),18); emptyLabel.rectTransform.anchoredPosition=new Vector2(-15,26); emptyLabel.text="보유 결과 없음"; emptyLabel.color=new Color(.63f,.76f,.75f,.70f);
        for(int i=0;i<6;i++)
        {
            var tr=Rect("Spirit trail "+i,canvasRect,new Vector2(160,160));
            var img=tr.gameObject.AddComponent<RawImage>(); img.texture=flameTexture; img.raycastTarget=false; img.color=new Color(.65f,1,1,.15f);
            tr.gameObject.SetActive(false); trails.Add(tr);
        }
        var worldGO=new GameObject("Soul result world canvas (runtime)",typeof(RectTransform),typeof(Canvas),typeof(CanvasGroup));
        worldGO.layer=gameObject.layer;
        worldCanvas=worldGO.GetComponent<Canvas>();worldCanvas.renderMode=RenderMode.WorldSpace;worldCanvas.worldCamera=worldCamera;
        worldRect=worldGO.GetComponent<RectTransform>();worldRect.sizeDelta=new Vector2(160,160);
        var worldGroup=worldGO.GetComponent<CanvasGroup>();worldGroup.interactable=false;worldGroup.blocksRaycasts=false;
        UpdateSafeArea(); IsReady=true; UpdateHeader(); return true;
    }
    static RectTransform Rect(string name,Transform parent,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform)); var r=go.GetComponent<RectTransform>(); r.SetParent(parent,false);
        r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f); r.sizeDelta=size; return r;
    }
    TMP_Text Text(string name,Transform parent,Vector2 size,float fontSize)
    {
        var r=Rect(name,parent,size); var t=r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font=font; t.fontSize=fontSize; t.alignment=TextAlignmentOptions.Center; t.raycastTarget=false;
        t.color=new Color(.92f,1f,.98f,1); t.text=""; return t;
    }
    Token Create(int id,int steps)
    {
        var t=new Token {id=id,steps=steps,slot=tokens.Count};
        t.root=Rect("Result "+id+" "+Name(steps),canvasRect,new Vector2(160,160)); t.group=t.root.gameObject.AddComponent<CanvasGroup>(); t.group.blocksRaycasts=false; t.group.interactable=false;
        t.flame=Rect("Flame artwork",t.root,new Vector2(160,160));
        var img=t.flame.gameObject.AddComponent<RawImage>(); img.texture=flameTexture; img.raycastTarget=false;
        t.number=Text("Result character",t.root,new Vector2(120,75),54); t.number.rectTransform.anchoredPosition=new Vector2(0,-25); t.number.text=Name(steps);
        var count=Text("Steps",t.root,new Vector2(110,25),17); count.rectTransform.anchoredPosition=new Vector2(0,-62); count.text=steps+"칸";
        tokens.Add(t); t.root.anchoredPosition=SlotPoint(t.slot); t.root.localScale=Vector3.one*dockScale; return t;
    }
    public static string Name(int steps)
    {
        switch(steps) {case 1:return "도";case 2:return "개";case 3:return "걸";case 4:return "윷";case 5:return "모";default:return "?";}
    }
    void UpdateHeader()
    {
        if(!title) return;
        title.text=playerBank?"내 결과":"상대 결과";
        title.color=playerBank?new Color(.56f,1,.91f,1):new Color(1,.78f,.56f,1);
        bool any=false; foreach(var t in tokens) if(!t.removing) {any=true;break;}
        emptyLabel.gameObject.SetActive(!any);
    }
    Token Find(int id) { foreach(var t in tokens) if(t.id==id && !t.removing) return t; return null; }
    public void SyncResults(bool player,IReadOnlyList<YutSavedResult> results)
    {
        if(!IsReady || !isActiveAndEnabled) return;
        if(playerBank!=player) { ClearAll(); playerBank=player; }
        var keep=new HashSet<int>();
        for(int i=0;i<results.Count;i++)
        {
            var saved=results[i]; keep.Add(saved.Id); var t=Find(saved.Id) ?? Create(saved.Id,saved.Steps); t.slot=i;
        }
        foreach(var t in tokens.ToArray()) if(!t.removing && !keep.Contains(t.id)) {t.removing=true; StartCoroutine(Consume(t,generation));}
        UpdateHeader();
    }
    Vector2 SlotPoint(int slot)
    {
        return canvasRect.InverseTransformPoint(bank.TransformPoint(new Vector3(78+slot*slotSpacing,-130,0)));
    }
    Vector2 PopupPoint(Vector3 fallback)
    {
        Vector3 projected=worldCamera.WorldToScreenPoint(resultAnchor ? resultAnchor.position : fallback);
        Vector2 screen=projected.z>0 ? new Vector2(projected.x,projected.y) : new Vector2(Screen.width*.5f,Screen.height*.5f);
        Vector2 local; RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,screen,null,out local); local+=popupOffset;
        Rect r=canvasRect.rect; float margin=Mathf.Min(140,Mathf.Min(r.width,r.height)*.20f);
        local.x=Mathf.Clamp(local.x,r.xMin+margin,r.xMax-margin);local.y=Mathf.Clamp(local.y,r.yMin+margin,r.yMax-margin);return local;
    }
    [ContextMenu("Apply World UI UX Defaults")]
    public void ApplyWorldUIDefaults()
    {
#if UNITY_EDITOR
        if(!Application.isPlaying) UnityEditor.Undo.RecordObject(this,"Apply Soul Result World UI Defaults");
#endif
        revealInWorldSpace=true;worldWidth=.24f;worldOffset=new Vector3(0,.12f,0);worldTiltDegrees=10;
        readSeconds=1.8f;travelSeconds=.60f;dockScale=.95f;slotSpacing=90;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        if(!Application.isPlaying && gameObject.scene.IsValid()) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }
    void PlaceWorldPopup(Token t,Vector3 fallback)
    {
        Vector3 point=(resultAnchor ? resultAnchor.position : fallback)+worldOffset;
        point.y+=Mathf.Sin(Time.time*2.2f)*Mathf.Max(0,floatAmplitude);
        worldRect.SetPositionAndRotation(point,worldCamera.transform.rotation*Quaternion.Euler(worldTiltDegrees,0,0));
        worldRect.localScale=Vector3.one*(Mathf.Max(.01f,worldWidth)/160f);
        t.root.anchoredPosition3D=Vector3.zero;
    }
    void PlacePopup(Token t,Vector3 fallback)
    {
        if(revealInWorldSpace) PlaceWorldPopup(t,fallback);
        else t.root.anchoredPosition=PopupPoint(fallback);
    }
    Vector2 CanvasPoint(Vector3 world)
    {
        Vector3 pixel=worldCamera.WorldToScreenPoint(world);Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,new Vector2(pixel.x,pixel.y),null,out local);
        return local;
    }
    Vector3 TransferToOverlay(Token t)
    {
        if(!revealInWorldSpace) return t.root.localScale;
        Canvas.ForceUpdateCanvases();
        var corners=new Vector3[4];t.root.GetWorldCorners(corners);
        Vector2 bottomLeft=CanvasPoint(corners[0]),topLeft=CanvasPoint(corners[1]),bottomRight=CanvasPoint(corners[3]);
        Vector2 centre=CanvasPoint(t.root.position);
        Vector3 scale=new Vector3(Mathf.Max(.01f,Vector2.Distance(bottomLeft,bottomRight)/160f),Mathf.Max(.01f,Vector2.Distance(bottomLeft,topLeft)/160f),1);
        float angle=Mathf.Atan2(bottomRight.y-bottomLeft.y,bottomRight.x-bottomLeft.x)*Mathf.Rad2Deg;
        t.root.SetParent(canvasRect,false);t.root.anchorMin=t.root.anchorMax=t.root.pivot=new Vector2(.5f,.5f);
        t.root.anchoredPosition3D=new Vector3(centre.x,centre.y,0);t.root.localScale=scale;t.root.localRotation=Quaternion.Euler(0,0,angle);
        t.root.SetAsLastSibling();return scale;
    }
    bool Valid(Token t,int ticket) {return ticket==generation && IsReady && isActiveAndEnabled && t.root && !t.removing;}
    public IEnumerator Present(int id,int steps,bool player,Vector3 boardPoint)
    {
        if(!IsReady || !isActiveAndEnabled) yield break;
        if(playerBank!=player) {ClearAll();playerBank=player;}
        var t=Find(id) ?? Create(id,steps); int ticket=generation; t.travelling=true;
        if(revealInWorldSpace) {t.root.SetParent(worldRect,false);t.root.localRotation=Quaternion.identity;t.root.anchoredPosition3D=Vector3.zero;}
        t.root.SetAsLastSibling(); UpdateHeader(); Canvas.ForceUpdateCanvases();
        float d=Mathf.Max(.01f,appearSeconds);
        for(float elapsed=0;elapsed<d;elapsed+=Time.deltaTime)
        {
            if(!Valid(t,ticket)) yield break;
            float u=Mathf.Clamp01(elapsed/d);PlacePopup(t,boardPoint);t.group.alpha=u;
            t.root.localScale=Vector3.one*Mathf.Lerp(popupScale*.75f,popupScale,1-Mathf.Pow(1-u,3));yield return null;
        }
        t.group.alpha=1;t.root.localScale=Vector3.one*popupScale;
        for(float elapsed=0;elapsed<Mathf.Max(0,readSeconds);elapsed+=Time.deltaTime)
        {if(!Valid(t,ticket)) yield break;PlacePopup(t,boardPoint);yield return null;}
        Vector3 transferScale=TransferToOverlay(t);Quaternion transferRotation=t.root.localRotation;
        Vector2 start=t.root.anchoredPosition; trailPositions.Clear();d=Mathf.Max(.01f,travelSeconds);
        for(float elapsed=0;elapsed<d;elapsed+=Time.deltaTime)
        {
            if(!Valid(t,ticket)) yield break;
            float u=Mathf.Clamp01(elapsed/d), eased=u*u*(3-2*u);Vector2 end=SlotPoint(t.slot);
            Vector2 control=(start+end)*.5f+Vector2.up*arcHeight;
            Vector2 pos=(1-eased)*(1-eased)*start+2*(1-eased)*eased*control+eased*eased*end;
            t.root.anchoredPosition=pos;t.root.localScale=Vector3.Lerp(transferScale,Vector3.one*dockScale,eased);t.root.localRotation=Quaternion.Slerp(transferRotation,Quaternion.identity,eased);
            UpdateTrail(pos,Mathf.Lerp(transferScale.y,dockScale,eased),u);yield return null;
        }
        HideTrail();t.root.localRotation=Quaternion.identity; d=Mathf.Max(.01f,settleSeconds);
        for(float elapsed=0;elapsed<d;elapsed+=Time.deltaTime)
        {
            if(!Valid(t,ticket)) yield break;
            float u=Mathf.Clamp01(elapsed/d);t.root.anchoredPosition=SlotPoint(t.slot);t.root.localScale=Vector3.one*dockScale*(1+.12f*Mathf.Sin(u*Mathf.PI));yield return null;
        }
        if(Valid(t,ticket)) {t.root.localScale=Vector3.one*dockScale;t.root.anchoredPosition=SlotPoint(t.slot);t.travelling=false;}
    }
    void UpdateTrail(Vector2 pos,float scale,float progress)
    {
        if(!showTrail) return;
        trailPositions.Insert(0,pos);if(trailPositions.Count>24) trailPositions.RemoveAt(trailPositions.Count-1);
        for(int i=0;i<trails.Count;i++)
        {
            int j=Mathf.Min(trailPositions.Count-1,(i+1)*3);var tr=trails[i];tr.gameObject.SetActive(true);tr.anchoredPosition=trailPositions[j];
            tr.localScale=Vector3.one*scale*(.75f-i*.07f);
            tr.GetComponent<RawImage>().color=new Color(.65f,1,1,(.14f-i*.018f)*Mathf.Sin(progress*Mathf.PI));
        }
    }
    void HideTrail(){foreach(var tr in trails) if(tr) tr.gameObject.SetActive(false);trailPositions.Clear();}
    IEnumerator Consume(Token t,int ticket)
    {
        Vector2 start=t.root.anchoredPosition; Vector3 scale=t.root.localScale;float d=Mathf.Max(.01f,consumeSeconds);
        for(float elapsed=0;elapsed<d;elapsed+=Time.deltaTime)
        {
            if(ticket!=generation || !t.root) yield break;
            float u=Mathf.Clamp01(elapsed/d);t.root.anchoredPosition=start+Vector2.up*(22*u);t.group.alpha=1-u;t.root.localScale=scale*(1+.22f*u);yield return null;
        }
        if(t.root) Destroy(t.root.gameObject);tokens.Remove(t);UpdateHeader();
    }
    public void ClearAll()
    {
        generation++;StopAllCoroutines();HideTrail();foreach(var t in tokens) if(t.root) Destroy(t.root.gameObject);tokens.Clear();UpdateHeader();
    }
    void UpdateSafeArea()
    {
        if(!safeRoot || Screen.width<=0 || Screen.height<=0) return;
        Rect a=Screen.safeArea;lastSafeArea=a;lastScreen=new Vector2Int(Screen.width,Screen.height);
        safeRoot.anchorMin=new Vector2(a.xMin/Screen.width,a.yMin/Screen.height);safeRoot.anchorMax=new Vector2(a.xMax/Screen.width,a.yMax/Screen.height);
        safeRoot.offsetMin=safeRoot.offsetMax=Vector2.zero;bank.anchoredPosition=bankOffset;
    }
    void LateUpdate()
    {
        if(!IsReady) return;
        if(Screen.safeArea!=lastSafeArea || lastScreen.x!=Screen.width || lastScreen.y!=Screen.height) UpdateSafeArea();
        foreach(var t in tokens)
        {
            if(!t.root || t.removing) continue;
            if(!t.travelling) t.root.anchoredPosition=Vector2.Lerp(t.root.anchoredPosition,SlotPoint(t.slot),1-Mathf.Exp(-16*Time.deltaTime));
            float wave=Mathf.Sin(Time.time*3+t.id)* (t.travelling?.025f:.009f);t.flame.localScale=new Vector3(1+wave,1-wave,1);
        }
    }
    void OnDisable(){ClearAll();if(canvas) canvas.gameObject.SetActive(false);if(worldCanvas) worldCanvas.gameObject.SetActive(false);}
    void OnEnable(){if(canvas) canvas.gameObject.SetActive(true);if(worldCanvas) worldCanvas.gameObject.SetActive(true);}
    void OnDestroy()
    {
        ClearAll();if(canvas) Destroy(canvas.gameObject);if(worldCanvas) Destroy(worldCanvas.gameObject);
        if(ownedFont)
        {
            if(ownedFont.atlasTextures!=null) foreach(var texture in ownedFont.atlasTextures) if(texture) Destroy(texture);
            if(ownedFont.material) Destroy(ownedFont.material);Destroy(ownedFont);
        }
    }
}
