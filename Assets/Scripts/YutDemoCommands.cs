using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Add only to the presentation demo scene. Works in a standalone player build.
public sealed class YutDemoCommands : MonoBehaviour
{
    [SerializeField] private YutTurnManager turnManager;
    [SerializeField] private bool enableDemoCommands=true;
    [SerializeField,Min(1)] private int demoDamage=20;
    private void Awake(){if(turnManager==null)turnManager=FindFirstObjectByType<YutTurnManager>();}
    private void Update()
    {
        if(!enableDemoCommands || turnManager==null)return;
#if ENABLE_INPUT_SYSTEM
        var keyboard=Keyboard.current;if(keyboard==null)return;
        if(keyboard.f6Key.wasPressedThisFrame)DamageOpponent();
        else if(keyboard.f7Key.wasPressedThisFrame)DamagePlayer();
        else if(keyboard.f8Key.wasPressedThisFrame)ShowVictory();
        else if(keyboard.f9Key.wasPressedThisFrame)ResetDemo();
#elif ENABLE_LEGACY_INPUT_MANAGER
        if(Input.GetKeyDown(KeyCode.F6))DamageOpponent();
        else if(Input.GetKeyDown(KeyCode.F7))DamagePlayer();
        else if(Input.GetKeyDown(KeyCode.F8))ShowVictory();
        else if(Input.GetKeyDown(KeyCode.F9))ResetDemo();
#endif
    }
    public void DamageOpponent(){Run(false,false);}
    public void DamagePlayer(){Run(true,false);}
    public void ShowVictory(){Run(false,true);}
    private void Run(bool targetPlayer,bool victory)
    {
        if(!enableDemoCommands || !isActiveAndEnabled)return;
        if(turnManager==null || !turnManager.TryDemoScale(targetPlayer,victory,demoDamage))
            Debug.Log("Demo command unavailable: wait for your turn, camera transition and animations to finish. After game over press F9.",this);
    }
    public void ResetDemo()
    {
        if(!enableDemoCommands || !isActiveAndEnabled)return;
        if(turnManager==null || !turnManager.TryDemoReset())
            Debug.Log("Demo reset unavailable during a throw, movement, AI turn or presentation. Wait until the command finishes.",this);
    }
}
