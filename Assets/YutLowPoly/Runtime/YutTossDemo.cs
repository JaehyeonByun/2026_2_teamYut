using UnityEngine;
namespace YutLowPoly
{
    public sealed class YutTossDemo : MonoBehaviour
    {
        public Rigidbody[] sticks;
        public void Toss()
        {
            if (sticks == null) return;
            for (int i = 0; i < sticks.Length; i++)
            {
                var rb = sticks[i]; if (rb == null) continue;
                rb.position = new Vector3((i - 1.5f) * 0.34f, 0.45f + i * 0.035f, 0);
                rb.rotation = Random.rotationUniform;
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
#endif
                rb.angularVelocity = Vector3.zero;
                rb.maxAngularVelocity = 35f;
                rb.WakeUp();
                rb.AddForce(new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(1.4f, 2f), Random.Range(-0.2f, 0.2f)), ForceMode.VelocityChange);
                rb.AddTorque(Random.onUnitSphere * Random.Range(12f, 24f), ForceMode.VelocityChange);
            }
        }
        void OnGUI()
        {
            if (GUI.Button(new Rect(20, 20, 180, 48), "TOSS YUT")) Toss();
            GUI.Label(new Rect(20, 76, 450, 28), "3D physics demo | marked dot = Back-do stick");
        }
    }
}
