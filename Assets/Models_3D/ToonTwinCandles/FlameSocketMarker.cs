using UnityEngine;
namespace ToonTwinCandles {
    // Placement helper only. Does not create particles, lights or gameplay logic.
    public sealed class FlameSocketMarker : MonoBehaviour {
        public Color gizmoColor=new Color(1f,.48f,.12f,1f);
        [Min(.001f)] public float markerRadius=.009f;
        void OnDrawGizmos() {
            Matrix4x4 old=Gizmos.matrix;Gizmos.matrix=transform.localToWorldMatrix;
            Gizmos.color=gizmoColor;Gizmos.DrawWireSphere(Vector3.zero,markerRadius);
            Gizmos.DrawLine(Vector3.zero,Vector3.up*markerRadius*3);
            Gizmos.DrawLine(Vector3.left*markerRadius*1.5f,Vector3.right*markerRadius*1.5f);
            Gizmos.matrix=old;
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position,"FlameSocket");
            #endif
        }
    }
}
