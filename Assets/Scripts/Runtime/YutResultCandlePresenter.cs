using UnityEngine;
using SoulFlameAsset;

// One shared two-candle holder represents the current turn's unspent results.
[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
public sealed class YutResultCandlePresenter : MonoBehaviour
{
    [SerializeField] private YutTurnManager turnManager;
    [SerializeField] private Transform firstSocket;
    [SerializeField] private Transform secondSocket;
    [SerializeField,Min(.01f)] private float flameWorldHeight=.10f;
    [SerializeField,Min(0f)] private float baseOffset=0f;
    [SerializeField,Min(.05f)] private float fadeSeconds=.25f;
    private readonly SoulFlameVisual[] flames=new SoulFlameVisual[2];
    private bool built;
    private void Reset(){FindSockets();}
    [ContextMenu("Find Candle Sockets")]
    private void FindSockets()
    {
        if(firstSocket==null)firstSocket=transform.Find("Candle_Short/FlameSocket");
        if(secondSocket==null)secondSocket=transform.Find("Candle_Tall/FlameSocket");
    }
    private void Start()
    {
        FindSockets();
        if(turnManager==null)turnManager=FindFirstObjectByType<YutTurnManager>();
        if(turnManager==null || firstSocket==null || secondSocket==null || firstSocket==secondSocket)
        {Debug.LogError("YutResultCandlePresenter: assign Turn Manager and two distinct FlameSocket transforms. Attach this component to TwinCandles.",this);enabled=false;return;}
        for(int i=0;i<2;i++)
        {
            var socket=i==0?firstSocket:secondSocket;
            var host=new GameObject("ResultCandleFlame_Runtime_"+(i+1));host.layer=socket.gameObject.layer;
            host.transform.SetParent(socket,false);
            flames[i]=host.AddComponent<SoulFlameVisual>();
            flames[i].ConfigureIndicator(flameWorldHeight,baseOffset,fadeSeconds);
            flames[i].SetIndicatorLit(false,true);
        }
        built=true;
    }
    private void Update()
    {
        if(!built)return;
        int count=turnManager!=null?Mathf.Clamp(turnManager.CandleResultCount,0,2):0;
        bool opponent=turnManager!=null && !turnManager.CandleResultsArePlayer;
        for(int i=0;i<2;i++)
        {
            if(flames[i]==null)continue;
            flames[i].ConfigureIndicator(flameWorldHeight,baseOffset,fadeSeconds);
            flames[i].SetTeam(opponent);
            flames[i].SetIndicatorLit(i<count);
        }
    }
    private void OnDisable()
    {foreach(var flame in flames)if(flame!=null)flame.SetIndicatorLit(false,true);}
    private void OnDestroy()
    {foreach(var flame in flames)if(flame!=null)Destroy(flame.gameObject);}
}
