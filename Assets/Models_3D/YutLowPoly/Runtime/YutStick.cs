using UnityEngine;
namespace YutLowPoly
{
    [DisallowMultipleComponent]
    public sealed class YutStick : MonoBehaviour
    {
        [Tooltip("True only on the stick with a dark dot on its flat face.")]
        public bool isBackDo;
        public bool FlatSideUp { get { return Vector3.Dot(transform.up, Vector3.up) > 0.7f; } }
        public bool RoundedSideUp { get { return Vector3.Dot(transform.up, Vector3.up) < -0.7f; } }
        // Query after the rigidbodies settle. Neither side true means edge/ambiguous.
        public bool IsSettled { get { var rb = GetComponent<Rigidbody>(); return rb == null || rb.IsSleeping(); } }
    }
}
