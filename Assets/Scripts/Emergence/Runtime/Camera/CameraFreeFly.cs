using UnityEngine;

namespace EmergentMechanics
{
    public sealed class CameraFreeFly : MonoBehaviour
    {
        #region Serialized
        [Tooltip("Movement speed in units per second.")]
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 6f;

        [Tooltip("Mouse look sensitivity in degrees per pixel.")]
        [Header("Look")]
        [SerializeField] private float lookSensitivity = 0.15f;

        [Tooltip("Minimum pitch angle in degrees.")]
        [SerializeField] private float minPitch = -80f;

        [Tooltip("Maximum pitch angle in degrees.")]
        [SerializeField] private float maxPitch = 80f;

        [Tooltip("Lock and hide the cursor while the camera is active.")]
        [Header("Cursor")]
        [SerializeField] private bool lockCursor = true;
        #endregion

        #region Runtime
        private PlayerControls controls;
        private PlayerControls.CameraActions cameraActions;
        private float yaw;
        private float pitch;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            controls = new PlayerControls();
            cameraActions = controls.Camera;

            Vector3 euler = transform.rotation.eulerAngles;
            yaw = euler.y;
            pitch = NormalizePitch(euler.x);
        }

        private void OnEnable()
        {
            cameraActions.Enable();
            UpdateCursorState(lockCursor);
        }

        private void OnDisable()
        {
            cameraActions.Disable();
            UpdateCursorState(false);
        }

        private void OnDestroy()
        {
            controls.Dispose();
        }

        private void Update()
        {
            UpdateLook();
            UpdateMovement();
        }
        #endregion

        #region Movement
        private void UpdateMovement()
        {
            Vector2 input = cameraActions.Move.ReadValue<Vector2>();

            if (input.sqrMagnitude <= 0f)
                return;

            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            Vector3 direction = forward * input.y + right * input.x;
            float speed = Mathf.Max(0f, moveSpeed);
            float deltaTime = Time.unscaledDeltaTime;

            if (deltaTime <= 0f)
                return;

            transform.position += direction * speed * deltaTime;
        }
        #endregion

        #region Look
        private void UpdateLook()
        {
            bool rightClick = cameraActions.RightClick.ReadValue<float>() != 0;
            if (!rightClick)
            {
                UpdateCursorState(false);
                return;
            }

            Vector2 input = cameraActions.Look.ReadValue<Vector2>();

            if (input.sqrMagnitude <= 0f)
                return;

            float sensitivity = Mathf.Max(0f, lookSensitivity);
            yaw += input.x * sensitivity;
            pitch -= input.y * sensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private static float NormalizePitch(float value)
        {
            if (value > 180f)
                return value - 360f;

            return value;
        }
        #endregion

        #region Cursor
        private static void UpdateCursorState(bool locked)
        {
            Cursor.visible = !locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        }
        #endregion
    }
}
