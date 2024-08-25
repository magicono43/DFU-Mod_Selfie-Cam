using UnityEngine;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop;
using Wenzil.Console;
using DaggerfallWorkshop.Game.Utility;

namespace SelfieCam
{
    public class SC_MousePivot : MonoBehaviour
    {
        public float XSensitivity = 2f;
        public float YSensitivity = 2f;
        public float MinimumX = -90F;
        public float MaximumX = 90F;

        public bool smooth = false;
        public float smoothTime = 5f;

        public bool clampVerticalRotation = true;

        private Transform pivot;
        private Quaternion m_CameraTargetRot;

        private bool enableMouseLook = true;
        private bool cursorActive;

        public void SetPivot(Transform pivot,Quaternion rotation)
        {
            this.pivot = pivot;
            m_CameraTargetRot = rotation;
        }

        private void Update()
        {
            if (!GameManager.IsGamePaused && InputManager.Instance.ActionComplete(InputManager.Actions.ActivateCursor))
            {
                cursorActive = !cursorActive;
            }

            if (cursorActive)
            {
                Cursor.lockState = CursorLockMode.None;
                InputManager.Instance.CursorVisible = true;
                return;
            }

            // Ensure the cursor always locked when set
            if (enableMouseLook)
            {
                Cursor.lockState = CursorLockMode.Locked;
                InputManager.Instance.CursorVisible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                InputManager.Instance.CursorVisible = true;
            }

            enableMouseLook = !GameManager.IsGamePaused;

            if (!enableMouseLook)
                return;

            // Handle mouse wheel (Zoom In & Out)
            float mouseScroll = Input.GetAxis("Mouse ScrollWheel");
            if (mouseScroll != 0)
            {
                float incs = SelfieCamMain.ZoomIncrements * 0.01f;

                if (mouseScroll > 0)
                    pivot.transform.localScale = pivot.transform.localScale + new Vector3(-incs, -incs, -incs);
                else if (mouseScroll < 0)
                    pivot.transform.localScale = pivot.transform.localScale + new Vector3(incs, incs, incs);
            }

            // Handle hotkey activated zooming (Zoom In & Out)
            if (SelfieCamMain.AllowZoomKeys)
            {
                if (InputManager.Instance.GetKey(SelfieCamMain.ZoomInKey))
                {
                    float incs = SelfieCamMain.ZoomIncrements * 0.01f;
                    pivot.transform.localScale = pivot.transform.localScale + new Vector3(-incs, -incs, -incs);
                }
                if (InputManager.Instance.GetKey(SelfieCamMain.ZoomOutKey))
                {
                    float incs = SelfieCamMain.ZoomIncrements * 0.01f;
                    pivot.transform.localScale = pivot.transform.localScale + new Vector3(incs, incs, incs);
                }
            }

            if (!SelfieCamMain.cameraLookFrozen)
                LookRotation();
        }

        public void LookRotation(float sensitivityMultiplier = 1, bool useTimescale = true)
        {
            Vector2 rawMouseDelta = new Vector2(InputManager.Instance.LookX, InputManager.Instance.LookY);

            float yRot = rawMouseDelta.x * XSensitivity * sensitivityMultiplier;
            float xRot = rawMouseDelta.y * YSensitivity * sensitivityMultiplier;

            m_CameraTargetRot *= Quaternion.Euler(-xRot, yRot, 0f);

            if (clampVerticalRotation)
                m_CameraTargetRot = ClampRotationAroundXAxis(m_CameraTargetRot);

            Vector3 v = m_CameraTargetRot.eulerAngles;

            m_CameraTargetRot = Quaternion.Euler(v.x, v.y, 0);

            if (smooth)
            {
                pivot.localRotation = Quaternion.Slerp(pivot.localRotation, m_CameraTargetRot,
                    smoothTime * (useTimescale ? Time.deltaTime : Time.unscaledDeltaTime));
            }
            else
            {
                pivot.localRotation = m_CameraTargetRot;
            }

        }

        Quaternion ClampRotationAroundXAxis(Quaternion q)
        {
            q.x /= q.w;
            q.y /= q.w;
            q.z /= q.w;
            q.w = 1.0f;

            float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);

            angleX = Mathf.Clamp(angleX, MinimumX, MaximumX);

            q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

            return q;
        }
    }
}