using UnityEngine;
using UnityEngine.UI;

namespace IVRAR.Group02.XR
{
    /// <summary>
    /// Configures world-space VR canvases for raycast collision and ergonomic viewing.
    /// Ensures buttons and sliders receive physical raycast events from controller laser pointers.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class VRWorldSpaceUIHelper : MonoBehaviour
    {
        [Header("Ergonomic Positioning")]
        [SerializeField] private float _viewingDistanceMeters = 2.0f;
        [SerializeField] private float _eyeLevelHeightMeters = 1.5f;

        private void Awake()
        {
            SetupCanvas();
        }

        public void SetupCanvas()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null) return;

            if (UnityEngine.XR.XRSettings.isDeviceActive)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                transform.position = new Vector3(0f, _eyeLevelHeightMeters, _viewingDistanceMeters);
                transform.rotation = Quaternion.identity;
                transform.localScale = Vector3.one * 0.0018f;
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            // Ensure GraphicRaycaster is present
            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            // Ensure child interactive elements have box colliders for physical pointer rays
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                if (btn.GetComponent<Collider>() == null)
                {
                    var col = btn.gameObject.AddComponent<BoxCollider>();
                    var rect = btn.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        col.size = new Vector3(rect.rect.width, rect.rect.height, 1.5f);
                    }
                }
            }

            var sliders = GetComponentsInChildren<Slider>(true);
            foreach (var s in sliders)
            {
                if (s.GetComponent<Collider>() == null)
                {
                    var col = s.gameObject.AddComponent<BoxCollider>();
                    var rect = s.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        col.size = new Vector3(rect.rect.width, rect.rect.height, 2.0f);
                    }
                }
            }
        }
    }
}
