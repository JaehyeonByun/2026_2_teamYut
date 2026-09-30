using System.Collections;
using TMPro;
using UnityEngine;

// Presentation only: this component never changes the turn or the roll result.
public class OpponentReactionController : MonoBehaviour
{
    public enum Reaction { Idle, Thinking, Happy, Shocked, Victory, Defeat }
    public enum VisualMode { SpriteSwap, Animator }

    [SerializeField] private VisualMode mode = VisualMode.SpriteSwap;
    [SerializeField] private SpriteRenderer characterRenderer;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private Animator characterAnimator;
    [Header("Sprites: blank entries use Idle")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite thinkingSprite;
    [SerializeField] private Sprite happySprite;
    [SerializeField] private Sprite shockedSprite;
    [SerializeField] private Sprite victorySprite;
    [SerializeField] private Sprite defeatSprite;
    [Header("Temporary reactions")]
    [SerializeField, Min(0.1f)] private float goodRollSeconds = 1.5f;
    [SerializeField, Min(0.1f)] private float captureSeconds = 2f;
    [SerializeField] private Reaction currentReaction;

    private static readonly int ReactionHash = Animator.StringToHash("Reaction");
    private Reaction baseline = Reaction.Idle;
    private Coroutine pulse;
    private int priority;
    private bool terminal;
    private bool animatorReady;
    public Reaction CurrentReaction => currentReaction;

    private void Awake()
    {
        if (idleSprite == null && characterRenderer != null) idleSprite = characterRenderer.sprite;
        if (mode == VisualMode.Animator && characterAnimator != null
            && characterAnimator.runtimeAnimatorController != null)
        {
            foreach (var parameter in characterAnimator.parameters)
                if (parameter.nameHash == ReactionHash && parameter.type == AnimatorControllerParameterType.Int)
                    animatorReady = true;
        }
        if (mode == VisualMode.Animator && !animatorReady)
            Debug.LogWarning("Opponent: assign an Animator Controller with an Int parameter named Reaction. Text still works.", this);
        if (mode == VisualMode.SpriteSwap && characterAnimator != null && characterAnimator.enabled)
            Debug.LogWarning("Opponent: disable the Animator in SpriteSwap mode so it cannot overwrite sprites.", this);
        ResetReaction();
    }

    public void SetThinking(bool thinking)
    {
        baseline = thinking ? Reaction.Thinking : Reaction.Idle;
        if (!terminal && priority == 0 && currentReaction != baseline) Apply(baseline);
    }
    public void OnGoodRoll() => PlayPulse(Reaction.Happy, goodRollSeconds, 10);
    public void OnCapture() => PlayPulse(Reaction.Happy, captureSeconds, 20);
    public void OnPiecesLost() => PlayPulse(Reaction.Shocked, captureSeconds, 20);

    private void PlayPulse(Reaction reaction, float seconds, int newPriority)
    {
        if (!isActiveAndEnabled || terminal || newPriority < priority) return;
        CancelPulse();
        priority = newPriority;
        Apply(reaction);
        pulse = StartCoroutine(ReturnToBaseline(Mathf.Max(0.1f, seconds)));
    }
    private IEnumerator ReturnToBaseline(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        pulse = null;
        priority = 0;
        if (!terminal) Apply(baseline);
    }
    public void ShowMatchResult(bool opponentWon)
    {
        CancelPulse();
        terminal = true;
        priority = 100;
        Apply(opponentWon ? Reaction.Victory : Reaction.Defeat);
    }
    public void ResetReaction()
    {
        CancelPulse();
        priority = 0;
        terminal = false;
        baseline = Reaction.Idle;
        Apply(Reaction.Idle);
    }
    private void CancelPulse()
    {
        if (pulse != null) StopCoroutine(pulse);
        pulse = null;
    }
    private void OnDisable() => ResetReaction();

    private void Apply(Reaction reaction)
    {
        currentReaction = reaction;
        if (stateText != null) stateText.text = "Opponent: " + reaction;
        if (mode == VisualMode.Animator)
        {
            if (animatorReady && characterAnimator != null)
                characterAnimator.SetInteger(ReactionHash, (int)reaction);
            return;
        }
        Sprite sprite = idleSprite;
        switch (reaction)
        {
            case Reaction.Thinking: sprite = thinkingSprite; break;
            case Reaction.Happy: sprite = happySprite; break;
            case Reaction.Shocked: sprite = shockedSprite; break;
            case Reaction.Victory: sprite = victorySprite; break;
            case Reaction.Defeat: sprite = defeatSprite; break;
        }
        if (characterRenderer != null) characterRenderer.sprite = sprite != null ? sprite : idleSprite;
    }

#if UNITY_EDITOR
    private bool CanTest()
    {
        if (Application.isPlaying && isActiveAndEnabled) return true;
        Debug.LogWarning("Enter Play mode and enable OpponentReactionController first.", this);
        return false;
    }
    [ContextMenu("Tests/Idle")]
    private void TestIdle() { if (CanTest()) ResetReaction(); }
    [ContextMenu("Tests/Thinking")]
    private void TestThinking() { if (CanTest()) { ResetReaction(); SetThinking(true); } }
    [ContextMenu("Tests/Happy")]
    private void TestHappy() { if (CanTest()) { ResetReaction(); OnCapture(); } }
    [ContextMenu("Tests/Shocked")]
    private void TestShocked() { if (CanTest()) { ResetReaction(); OnPiecesLost(); } }
    [ContextMenu("Tests/Victory")]
    private void TestVictory() { if (CanTest()) ShowMatchResult(true); }
    [ContextMenu("Tests/Defeat")]
    private void TestDefeat() { if (CanTest()) ShowMatchResult(false); }
#endif
}
