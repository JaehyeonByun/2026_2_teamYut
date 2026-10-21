using UnityEngine;
namespace WoodenYutBoardAsset
{
    [DisallowMultipleComponent]
    public sealed class WoodenYutBoardLayout : MonoBehaviour
    {
        [Tooltip("Outer path 00-19; diagonals 20-27; center 28. Asset IDs, not game movement rules.")]
        public Transform[] nodes = new Transform[29];
        public Transform GetNode(int index)
        {
            return nodes != null && index >= 0 && index < nodes.Length ? nodes[index] : null;
        }
        void OnDrawGizmosSelected()
        {
            if (nodes == null) return;
            Gizmos.color = new Color(.2f, 1f, .65f, 1f);
            foreach (var node in nodes)
            {
                if (node == null) continue;
                Gizmos.DrawWireSphere(node.position, .008f * Mathf.Abs(transform.lossyScale.x));
#if UNITY_EDITOR
                UnityEditor.Handles.Label(node.position + transform.up * .01f, node.name);
#endif
            }
        }
    }
}
