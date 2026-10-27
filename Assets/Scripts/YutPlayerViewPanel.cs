using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class YutPlayerViewPanel : MonoBehaviour
{
    [SerializeField] private Vector2 bottomRightOffset=new Vector2(-28,110);
    private YutTurnManager owner;
    private GameObject canvasObject;
    private RectTransform panel;
    private readonly Button[] buttons=new Button[4];
    private readonly Image[] backgrounds=new Image[4];
    private TMP_Text hint;
    private TMP_FontAsset ownedFont;
    private bool korean;
    public void Initialize(YutTurnManager manager,TMP_FontAsset fallback)
    {
        owner=manager;if(canvasObject!=null)return;
        TMP_FontAsset font=fallback;
        var source=Resources.Load<Font>("YutSoulResultUI/GowunBatang-Bold");
        if(source!=null){ownedFont=TMP_FontAsset.CreateFontAsset(source);font=ownedFont;}
        korean=font!=null && font.HasCharacter('천',true,true);
        canvasObject=new GameObject("PlayerViewButtons_Runtime",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=85;
        var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        panel=new GameObject("ViewButtons",typeof(RectTransform)).GetComponent<RectTransform>();panel.SetParent(canvasObject.transform,false);
        panel.anchorMin=panel.anchorMax=new Vector2(1,0);panel.pivot=new Vector2(1,0);panel.sizeDelta=new Vector2(470,86);panel.anchoredPosition=bottomRightOffset;
        string[] names=korean?new[]{"보드","천칭","정면","던지기"}:new[]{"Board","Scale","Front","Throw"};
        for(int i=0;i<4;i++)
        {
            int index=i;
            var go=new GameObject(names[i],typeof(RectTransform),typeof(Image),typeof(Button));
            var rect=go.GetComponent<RectTransform>();rect.SetParent(panel,false);rect.anchorMin=rect.anchorMax=Vector2.zero;
            rect.pivot=Vector2.zero;rect.anchoredPosition=new Vector2(i*120,0);rect.sizeDelta=new Vector2(110,48);
            backgrounds[i]=go.GetComponent<Image>();buttons[i]=go.GetComponent<Button>();buttons[i].targetGraphic=backgrounds[i];
            var nav=buttons[i].navigation;nav.mode=Navigation.Mode.None;buttons[i].navigation=nav;
            buttons[i].onClick.AddListener(()=>owner.TryChoosePlayerView((CameraDirector.PlayerView)index));
            var label=Label(go.transform,font,names[i],22);label.rectTransform.offsetMin=Vector2.zero;label.rectTransform.offsetMax=Vector2.zero;
        }
        hint=Label(panel,font,"",18);hint.rectTransform.anchorMin=new Vector2(0,1);hint.rectTransform.anchorMax=Vector2.one;
        hint.rectTransform.pivot=new Vector2(.5f,1);hint.rectTransform.sizeDelta=new Vector2(0,32);hint.rectTransform.anchoredPosition=Vector2.zero;
    }
    private static TMP_Text Label(Transform parent,TMP_FontAsset font,string value,float size)
    {
        var text=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.rectTransform.SetParent(parent,false);text.rectTransform.anchorMin=Vector2.zero;text.rectTransform.anchorMax=Vector2.one;
        if(font!=null)text.font=font;text.text=value;text.fontSize=size;text.alignment=TextAlignmentOptions.Center;
        text.color=new Color(.85f,.88f,.83f);text.raycastTarget=false;return text;
    }
    private void LateUpdate()
    {
        if(canvasObject==null)return;
        if(owner==null || !owner.isActiveAndEnabled){canvasObject.SetActive(false);return;}
        canvasObject.SetActive(true);
        var director=owner.PlayerCameraDirector;
        bool allowed=owner.CanInspectPlayerView && director!=null && director.CanChoosePlayerView;
        panel.anchoredPosition=bottomRightOffset;
        for(int i=0;i<4;i++)
        {
            var view=(CameraDirector.PlayerView)i;
            buttons[i].interactable=allowed && director.IsPlayerViewAvailable(view);
            bool selected=director!=null && director.IsSelectedPlayerView(view);
            backgrounds[i].color=selected?new Color(.22f,.39f,.38f,.96f):new Color(.07f,.085f,.10f,.94f);
        }
        hint.text=korean ? (!owner.CanInspectPlayerView?"진행 중 · 시점 변경 잠김":director!=null && director.IsBlending?"시점 이동 중":owner.CanThrowFromCurrentView?"던지기 가능":owner.WaitingForPlayerRoll?"던지기는 던지기 시점에서":"상황 확인 · 이동할 말을 선택하세요")
            :(!owner.CanInspectPlayerView?"View locked":director!=null && director.IsBlending?"Changing view":owner.CanThrowFromCurrentView?"Ready to throw":owner.WaitingForPlayerRoll?"Throw only from Throw view":"Inspect the board and select a piece");
    }
    private void OnEnable(){if(canvasObject!=null)canvasObject.SetActive(true);}
    private void OnDisable(){if(canvasObject!=null)canvasObject.SetActive(false);}
    private void OnDestroy()
    {
        if(canvasObject!=null)Destroy(canvasObject);
        if(ownedFont!=null)
        {if(ownedFont.atlasTextures!=null)foreach(var t in ownedFont.atlasTextures)if(t!=null)Destroy(t);if(ownedFont.material!=null)Destroy(ownedFont.material);Destroy(ownedFont);}
    }
}
