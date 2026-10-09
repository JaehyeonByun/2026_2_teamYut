using UnityEngine;

public class SimpleHandUI : MonoBehaviour
{
    [SerializeField] private RectTransform handRoot;
    [SerializeField] private CanvasGroup hitAreaGroup;

    [Header("Hand positions")]
    [SerializeField]
    private Vector2 closedPosition =
        new Vector2(60f, -260f);

    [SerializeField]
    private Vector2 openPosition =
        new Vector2(60f, 20f);

    [SerializeField, Min(1f)] private float moveSpeed = 12f;

    private bool isOpen;

    public bool CanInspect =>
        isOpen &&
        handRoot != null &&
        Vector2.Distance(
            handRoot.anchoredPosition,
            openPosition
        ) < 1f;

    private void Awake()
    {
        if (handRoot == null || hitAreaGroup == null)
        {
            Debug.LogError(
                "SimpleHandUI: Hand Root와 Hit Area Group을 연결하세요.",
                this
            );

            enabled = false;
            return;
        }

        isOpen = false;
        handRoot.anchoredPosition = closedPosition;

        hitAreaGroup.interactable = false;
        hitAreaGroup.blocksRaycasts = false;
    }

    private void Update()
    {
        Vector2 target = isOpen
            ? openPosition
            : closedPosition;

        float t = 1f - Mathf.Exp(
            -moveSpeed * Time.unscaledDeltaTime
        );

        handRoot.anchoredPosition = Vector2.Lerp(
            handRoot.anchoredPosition,
            target,
            t
        );

        if (Vector2.Distance(
            handRoot.anchoredPosition,
            target
        ) < 0.1f)
        {
            handRoot.anchoredPosition = target;
        }

        bool canInspect = CanInspect;

        hitAreaGroup.interactable = canInspect;
        hitAreaGroup.blocksRaycasts = canInspect;
    }

    public void ToggleHand()
    {
        if (!enabled) return;

        isOpen = !isOpen;

        // 닫는 순간부터 카드 감지를 막습니다.
        if (!isOpen)
        {
            hitAreaGroup.interactable = false;
            hitAreaGroup.blocksRaycasts = false;
        }
    }

    public void CloseHand()
    {
        isOpen = false;

        if (hitAreaGroup != null)
        {
            hitAreaGroup.interactable = false;
            hitAreaGroup.blocksRaycasts = false;
        }
    }
}