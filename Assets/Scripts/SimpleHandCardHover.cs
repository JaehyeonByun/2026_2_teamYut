using UnityEngine;
using UnityEngine.EventSystems;

public class SimpleHandCardHover : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private SimpleHandUI hand;
    [SerializeField] private RectTransform visual;

    [Header("Hover appearance")]
    [SerializeField] private float liftHeight = 90f;
    [SerializeField] private float hoverScale = 1.12f;
    [SerializeField, Min(1f)] private float moveSpeed = 16f;

    private Vector2 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private int originalSiblingIndex;

    private bool pointerInside;
    private bool wasRaised;

    private void Awake()
    {
        if (hand == null || visual == null)
        {
            Debug.LogError(
                "SimpleHandCardHover: Hand와 Visual을 연결하세요.",
                this
            );

            enabled = false;
            return;
        }

        originalPosition = visual.anchoredPosition;
        originalRotation = visual.localRotation;
        originalScale = visual.localScale;
        originalSiblingIndex = visual.GetSiblingIndex();
    }

    private void Update()
    {
        bool raised = pointerInside && hand.CanInspect;

        if (raised != wasRaised)
        {
            if (raised)
            {
                visual.SetAsLastSibling();
            }
            else
            {
                visual.SetSiblingIndex(originalSiblingIndex);
            }

            wasRaised = raised;
        }

        Vector2 targetPosition = originalPosition;

        if (raised)
        {
            targetPosition += Vector2.up * liftHeight;
        }

        Quaternion targetRotation = raised
            ? Quaternion.identity
            : originalRotation;

        Vector3 targetScale = raised
            ? originalScale * hoverScale
            : originalScale;

        float t = 1f - Mathf.Exp(
            -moveSpeed * Time.unscaledDeltaTime
        );

        visual.anchoredPosition = Vector2.Lerp(
            visual.anchoredPosition,
            targetPosition,
            t
        );

        visual.localRotation = Quaternion.Slerp(
            visual.localRotation,
            targetRotation,
            t
        );

        visual.localScale = Vector3.Lerp(
            visual.localScale,
            targetScale,
            t
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
    }

    private void OnDisable()
    {
        pointerInside = false;
        wasRaised = false;

        if (visual == null) return;

        visual.anchoredPosition = originalPosition;
        visual.localRotation = originalRotation;
        visual.localScale = originalScale;
        visual.SetSiblingIndex(originalSiblingIndex);
    }
}