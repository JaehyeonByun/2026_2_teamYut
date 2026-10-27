using System.Collections.Generic;
using SoulFlameAsset;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
public sealed class YutFlameHealthTooltip : MonoBehaviour
{
    private static readonly List<YutFlameHealthTooltip> instances=new List<YutFlameHealthTooltip>();
    private SoulFlameVisual source;
    private Camera viewCamera;
    private GameObject ui;
    private RectTransform canvasRect,box;
    private TMP_Text text;
    private TMP_FontAsset ownedFont;
    private void OnEnable(){source=GetComponent<SoulFlameVisual>();if(!instances.Contains(this))instances.Add(this);}
    private void OnDisable(){instances.Remove(this);DestroyUI();}
    private void OnDestroy(){instances.Remove(this);DestroyUI();}
    private void LateUpdate()
    {
        if(instances.Count==0 || instances[0]!=this)return;
        if(viewCamera==null || !viewCamera.isActiveAndEnabled)viewCamera=Camera.main;
        Vector2 pointer;
        if(viewCamera==null || !TryMouse(out pointer) || !viewCamera.pixelRect.Contains(pointer)
            || (EventSystem.current!=null && EventSystem.current.IsPointerOverGameObject())){Hide();return;}
        Ray ray=viewCamera.ScreenPointToRay(pointer);
        YutFlameHealthTooltip best=null;float nearest=float.PositiveInfinity;
        foreach(var item in instances)
        {
            if(item==null || item.source==null || !item.source.isActiveAndEnabled || !item.source.HasHealth
                || (viewCamera.cullingMask & (1<<item.gameObject.layer))==0)continue;
            float entry;
            if(item.source.HealthHoverBounds.IntersectRay(ray,out entry) && entry>=0 && entry<nearest)
            {best=item;nearest=entry;}
        }
        if(best==null){Hide();return;}
        // No new Collider: querying the flame cannot block piece clicks or yut physics.
        RaycastHit obstacle;
        if(Physics.Raycast(ray,out obstacle,Mathf.Max(0,nearest-.015f),viewCamera.cullingMask & ~(1<<2),QueryTriggerInteraction.Ignore))
        {Hide();return;}
        Vector3 screen=viewCamera.WorldToScreenPoint(best.source.HealthLabelPoint);
        if(screen.z<=0){Hide();return;}
        if(ui==null)BuildUI();
        ui.SetActive(true);text.text=best.source.CurrentHP+" / "+best.source.MaximumHP;
        text.color=best.source.IsOpponentFlame?new Color(1,.76f,.45f):new Color(.65f,1,.9f);
        Canvas.ForceUpdateCanvases();
        Vector2 local;RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,screen,null,out local);
        local.x=Mathf.Clamp(local.x,canvasRect.rect.xMin+120,canvasRect.rect.xMax-120);
        local.y=Mathf.Clamp(local.y,canvasRect.rect.yMin+36,canvasRect.rect.yMax-36);
        box.anchoredPosition=local;
    }
    private static bool TryMouse(out Vector2 position)
    {
#if ENABLE_INPUT_SYSTEM
        if(Mouse.current!=null){position=Mouse.current.position.ReadValue();return true;}
#elif ENABLE_LEGACY_INPUT_MANAGER
        position=Input.mousePosition;return true;
#endif
        position=Vector2.zero;return false;
    }
    private void BuildUI()
    {
        ui=new GameObject("FlameHealthTooltip_Runtime",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(CanvasGroup));
        var canvas=ui.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=95;
        var scaler=ui.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        var group=ui.GetComponent<CanvasGroup>();group.interactable=false;group.blocksRaycasts=false;
        canvasRect=ui.GetComponent<RectTransform>();
        var panel=new GameObject("Health",typeof(RectTransform),typeof(Image));box=panel.GetComponent<RectTransform>();
        box.SetParent(canvasRect,false);box.anchorMin=box.anchorMax=box.pivot=new Vector2(.5f,.5f);box.sizeDelta=new Vector2(228,58);
        var bg=panel.GetComponent<Image>();bg.color=new Color(.025f,.035f,.045f,.94f);bg.raycastTarget=false;
        text=new GameObject("HP",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.rectTransform.SetParent(box,false);text.rectTransform.anchorMin=Vector2.zero;text.rectTransform.anchorMax=Vector2.one;
        text.rectTransform.offsetMin=new Vector2(10,4);text.rectTransform.offsetMax=new Vector2(-10,-4);
        var sdf=Resources.Load<TMP_FontAsset>("YutFonts/JalnanSDF");
        if(sdf!=null)text.font=sdf;
        else
        {
            var font=Resources.Load<Font>("YutFonts/Jalnan");
            if(font!=null){ownedFont=TMP_FontAsset.CreateFontAsset(font);text.font=ownedFont;}
        }
        text.fontSize=30;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;
        text.enableAutoSizing=true;text.fontSizeMin=16;text.fontSizeMax=30;
    }
    private void Hide(){if(ui!=null)ui.SetActive(false);}
    private void DestroyUI()
    {
        if(ui!=null)Destroy(ui);ui=null;
        if(ownedFont!=null)
        {if(ownedFont.atlasTextures!=null)foreach(var atlas in ownedFont.atlasTextures)if(atlas!=null)Destroy(atlas);
         if(ownedFont.material!=null)Destroy(ownedFont.material);Destroy(ownedFont);ownedFont=null;}
    }
}
