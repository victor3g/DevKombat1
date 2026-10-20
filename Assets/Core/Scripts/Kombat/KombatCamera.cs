using UnityEngine;

namespace DevKombat.Kombat
{
    /// <summary>Câmera que acompanha os dois lutadores e treme nos golpes fortes.</summary>
    public class KombatCamera : MonoBehaviour
    {
        private Camera cam;
        private float shakeTime;
        private float shakeStrength;
        private float targetX;

        public Camera Camera => cam;
        public float HalfViewWidth => KombatStage.ViewHeight * 0.5f * cam.aspect;
        public float X { get; private set; }

        public static KombatCamera Create()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject go = new GameObject("Main Camera") { tag = "MainCamera" };
                camera = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }

            camera.orthographic = true;
            camera.orthographicSize = KombatStage.ViewHeight * 0.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            KombatCamera kombatCamera = camera.GetComponent<KombatCamera>();
            if (kombatCamera == null)
            {
                kombatCamera = camera.gameObject.AddComponent<KombatCamera>();
            }

            kombatCamera.cam = camera;
            kombatCamera.Snap(0f);
            return kombatCamera;
        }

        public void Follow(float x, float stageHalfWidth)
        {
            float limit = Mathf.Max(0f, stageHalfWidth + 0.6f - HalfViewWidth);
            targetX = Mathf.Clamp(x, -limit, limit);
        }

        public void Snap(float x)
        {
            targetX = x;
            X = x;
            Apply(Vector2.zero);
        }

        public void Shake(float seconds, float strength)
        {
            shakeTime = Mathf.Max(shakeTime, seconds);
            shakeStrength = Mathf.Max(shakeStrength, strength);
        }

        private void LateUpdate()
        {
            if (cam == null)
            {
                return;
            }

            X = Mathf.Lerp(X, targetX, 1f - Mathf.Exp(-8f * Time.deltaTime));
            Vector2 offset = Vector2.zero;
            if (shakeTime > 0f)
            {
                shakeTime -= Time.deltaTime;
                offset = Random.insideUnitCircle * shakeStrength;
                if (shakeTime <= 0f)
                {
                    shakeStrength = 0f;
                }
            }

            Apply(offset);
        }

        private void Apply(Vector2 offset)
        {
            float centerY = KombatStage.ViewHeight * 0.5f - KombatStage.FloorFromBottom;
            transform.position = new Vector3(X + offset.x, centerY + offset.y, -10f);
        }
    }
}
