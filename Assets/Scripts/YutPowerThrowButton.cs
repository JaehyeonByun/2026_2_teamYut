using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class YutPowerThrowButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField, Min(0.1f)] private float chargeSeconds = 1.2f;
    [SerializeField] private Vector2 gaugeOffset = new Vector2(0f,8f);
    private YutTurnManager owner;
    private Button button;
    private RectTransform buttonRect;
    private readonly YutPowerCharge charge = new YutPowerCharge();
    private int activePointer;
    private GameObject gauge;
    private RectTransform fill;
    private TMP_Text label;
    public bool Initialize(YutTurnManager manager,Button rollButton,TMP_FontAsset font)
    {
        owner=manager;button=rollButton;buttonRect=GetComponent<RectTransform>();
        if(owner==null || button==null || button.gameObject!=gameObject || buttonRect==null) return false;
        if(gauge==null) BuildGauge(font);
        gauge.SetActive(false);
        return true;
    }
    private void BuildGauge(TMP_FontAsset font)
    {
        gauge=new GameObject("Throw power gauge (runtime)",typeof(RectTransform));
        var rect=gauge.GetComponent<RectTransform>();rect.SetParent(buttonRect,false);
        rect.anchorMin=new Vector2(0f,1f);rect.anchorMax=new Vector2(1f,1f);
        rect.pivot=new Vector2(0.5f,0f);rect.sizeDelta=new Vector2(0f,38f);rect.anchoredPosition=gaugeOffset;
        var bar=new GameObject("Background",typeof(RectTransform),typeof(Image));
        var barRect=bar.GetComponent<RectTransform>();barRect.SetParent(rect,false);
        barRect.anchorMin=new Vector2(0f,0f);barRect.anchorMax=new Vector2(1f,0f);
        barRect.pivot=new Vector2(0.5f,0f);barRect.sizeDelta=new Vector2(0f,10f);barRect.anchoredPosition=Vector2.zero;
        var background=bar.GetComponent<Image>();background.color=new Color(0.05f,0.04f,0.025f,0.9f);background.raycastTarget=false;
        var fillObject=new GameObject("Power",typeof(RectTransform),typeof(Image));
        fill=fillObject.GetComponent<RectTransform>();fill.SetParent(barRect,false);
        fill.anchorMin=Vector2.zero;fill.anchorMax=new Vector2(0f,1f);fill.offsetMin=Vector2.zero;fill.offsetMax=Vector2.zero;
        var image=fillObject.GetComponent<Image>();image.color=new Color(1f,0.85f,0.5f,1f);image.raycastTarget=false;
        var textObject=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));
        label=textObject.GetComponent<TextMeshProUGUI>();label.rectTransform.SetParent(rect,false);
        label.rectTransform.anchorMin=new Vector2(0f,0f);label.rectTransform.anchorMax=Vector2.one;
        label.rectTransform.offsetMin=new Vector2(0f,10f);label.rectTransform.offsetMax=Vector2.zero;
        if(font!=null) label.font=font;
        label.fontSize=18f;label.alignment=TextAlignmentOptions.Center;label.color=Color.white;label.raycastTarget=false;
    }
    public void OnPointerDown(PointerEventData data)
    {
        if(data.button!=PointerEventData.InputButton.Left || charge.IsHolding || !isActiveAndEnabled
            || owner==null || button==null || !button.interactable || !owner.TryBeginPowerCharge()) return;
        activePointer=data.pointerId;
        charge.Begin(Time.unscaledTime);
        gauge.SetActive(true);RefreshGauge();
    }
    public void OnPointerUp(PointerEventData data)
    {
        if(data.button!=PointerEventData.InputButton.Left || data.pointerId!=activePointer || !charge.IsHolding) return;
        if(owner==null || !owner.IsPowerCharging || !RectTransformUtility.RectangleContainsScreenPoint(buttonRect,data.position,data.pressEventCamera))
        { Cancel();return; }
        float power;
        if(!charge.Release(Time.unscaledTime,chargeSeconds,out power)) return;
        gauge.SetActive(false);
        owner.ReleasePowerCharge(power);
    }
    public void OnPointerExit(PointerEventData data)
    { if(charge.IsHolding && data.pointerId==activePointer) Cancel(); }
    private void Update()
    {
        if(!charge.IsHolding) return;
        if(owner==null || !owner.IsPowerCharging || button==null || !button.interactable) { Cancel();return; }
        RefreshGauge();
    }
    private void RefreshGauge()
    {
        float power=charge.Value(Time.unscaledTime,chargeSeconds);
        fill.anchorMax=new Vector2(power,1f);
        label.text="Power "+Mathf.RoundToInt(power*100f)+"%";
    }
    private void Cancel()
    {
        bool wasHolding=charge.IsHolding;
        charge.Cancel();
        if(gauge!=null) gauge.SetActive(false);
        if(wasHolding && owner!=null) owner.CancelPowerCharge();
    }
    private void OnApplicationFocus(bool focused){if(!focused) Cancel();}
    private void OnDisable(){Cancel();}
    private void OnDestroy(){Cancel();if(gauge!=null)Destroy(gauge);}
}
