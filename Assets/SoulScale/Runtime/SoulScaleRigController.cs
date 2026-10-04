using UnityEngine;

namespace SoulScaleAsset
{
    // Scripted bone animation. No physics joints or Animator Controller required.
    public sealed class SoulScaleRigController : MonoBehaviour
    {
        [SerializeField] private Transform beam;
        [SerializeField] private Transform hangLeft;
        [SerializeField] private Transform hangRight;
        [SerializeField] private Transform flameAnchorLeft;
        [SerializeField] private Transform flameAnchorRight;
        [SerializeField, Range(-18f, 18f)] private float targetAngle;
        [SerializeField, Min(0.05f)] private float smoothSeconds = 0.7f;
        [SerializeField] private bool gentleSway = true;
        private Quaternion beamRest, leftRest, rightRest;
        private Quaternion beamLocalRest, leftLocalRest, rightLocalRest;
        private float angle, velocity, swayLeft, swayRight, swayVelocityLeft, swayVelocityRight;
        private bool initialized;
        public Transform FlameAnchorLeft => flameAnchorLeft;
        public Transform FlameAnchorRight => flameAnchorRight;
        public float CurrentAngle => angle;

        public void Configure(Transform beamBone, Transform leftBone, Transform rightBone,
            Transform leftAnchor, Transform rightAnchor)
        {
            beam = beamBone; hangLeft = leftBone; hangRight = rightBone;
            flameAnchorLeft = leftAnchor; flameAnchorRight = rightAnchor;
        }
        private void Awake()
        {
            if (beam == null || hangLeft == null || hangRight == null)
            {
                Debug.LogError("SoulScale: assign Beam, Hang_L and Hang_R, or use the generated prefab.", this);
                enabled = false; return;
            }
            Quaternion inv = Quaternion.Inverse(transform.rotation);
            beamRest = inv * beam.rotation;
            leftRest = inv * hangLeft.rotation; rightRest = inv * hangRight.rotation;
            beamLocalRest = beam.localRotation;
            leftLocalRest = hangLeft.localRotation; rightLocalRest = hangRight.localRotation;
            initialized = true;
        }
        private void LateUpdate()
        {
            if (!initialized) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (dt <= 0f) return;
            float previous = angle;
            angle = Mathf.SmoothDamp(angle, Mathf.Clamp(targetAngle, -18f, 18f), ref velocity,
                Mathf.Max(0.05f, smoothSeconds), Mathf.Infinity, dt);
            if (gentleSway)
            {
                float impulse = -(angle - previous) * 1.8f;
                swayVelocityLeft += impulse; swayVelocityRight += impulse * 0.85f;
                Spring(ref swayLeft, ref swayVelocityLeft, dt);
                Spring(ref swayRight, ref swayVelocityRight, dt);
            }
            else swayLeft = swayRight = swayVelocityLeft = swayVelocityRight = 0f;
            // Beam carries both the central ring and arms. Hangers follow endpoints,
            // but counter-rotate to stay upright relative to the prefab root.
            Quaternion root = transform.rotation;
            beam.rotation = root * Quaternion.AngleAxis(angle, Vector3.forward) * beamRest;
            hangLeft.rotation = root * Quaternion.AngleAxis(swayLeft, Vector3.forward) * leftRest;
            hangRight.rotation = root * Quaternion.AngleAxis(swayRight, Vector3.forward) * rightRest;
        }
        private static void Spring(ref float position, ref float speed, float dt)
        {
            speed += (-35f * position - 7f * speed) * dt;
            position = Mathf.Clamp(position + speed * dt, -4f, 4f);
        }
        public void SetAngle(float degrees) { targetAngle = Mathf.Clamp(degrees, -18f, 18f); }
        public void SetLeftWinner() { SetAngle(18f); }
        public void SetRightWinner() { SetAngle(-18f); }
        public void ResetScale()
        {
            targetAngle = angle = velocity = swayLeft = swayRight = swayVelocityLeft = swayVelocityRight = 0f;
            RestorePose();
        }
        private void RestorePose()
        {
            if (!initialized) return;
            beam.localRotation = beamLocalRest;
            hangLeft.localRotation = leftLocalRest; hangRight.localRotation = rightLocalRest;
        }
        private void OnDisable() { RestorePose(); angle = velocity = swayLeft = swayRight = swayVelocityLeft = swayVelocityRight = 0f; }
        [ContextMenu("Tests/Left Wins (Play Mode)")]
        private void TestLeft() { if (Application.isPlaying) SetLeftWinner(); }
        [ContextMenu("Tests/Right Wins (Play Mode)")]
        private void TestRight() { if (Application.isPlaying) SetRightWinner(); }
        [ContextMenu("Tests/Reset (Play Mode)")]
        private void TestReset() { if (Application.isPlaying) ResetScale(); }
    }
}
