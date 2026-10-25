using UnityEngine;
namespace WoodenYutBoardAsset
{
    [DisallowMultipleComponent]
    public sealed class WoodenYutBoardNode : MonoBehaviour
    {
        [Range(0, 28)] public int index;
        public string role;
        public Vector3 WorldPosition { get { return transform.position; } }
    }
}
