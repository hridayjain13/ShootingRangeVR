using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using IVRAR.Group02.TargetManagement;

namespace IVRAR.Group02.XR
{
    /// <summary>
    /// Renders a visible interactive laser pointer and delivers click / selection events
    /// to both VR world-space Canvas elements and 3D physical targets.
    /// Works seamlessly with XR Controller input and Desktop mouse clicks.
    /// </summary>
    public class XRRayInteractorPointer : MonoBehaviour
    {
        [Header("Raycast Properties")]
        [SerializeField] private float _maxRayDistance = 30f;
        [SerializeField] private LayerMask _interactionLayers = ~0;
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private Transform _rayOrigin;

        [Header("Visual Feedback")]
        [SerializeField] private Color _idleColor = new Color(0.2f, 0.6f, 1f, 0.6f);
        [SerializeField] private Color _hoverColor = new Color(1f, 0.8f, 0.1f, 0.9f);
        [SerializeField] private Color _clickColor = new Color(0.1f, 1f, 0.3f, 1f);

        private RaycastHit _currentHit;
        private bool _hasHit;
        private GameObject _hoveredObject;

        private void Awake()
        {
            if (_rayOrigin == null) _rayOrigin = transform;

            if (_lineRenderer == null)
            {
                _lineRenderer = GetComponent<LineRenderer>();
                if (_lineRenderer == null)
                {
                    _lineRenderer = gameObject.AddComponent<LineRenderer>();
                    _lineRenderer.startWidth = 0.005f;
                    _lineRenderer.endWidth = 0.002f;
                    _lineRenderer.positionCount = 2;
                    _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                }
            }
        }

        private void Update()
        {
            PerformRaycast();
        }

        private void PerformRaycast()
        {
            Vector3 origin = _rayOrigin.position;
            Vector3 direction = _rayOrigin.forward;

            _hasHit = Physics.Raycast(origin, direction, out _currentHit, _maxRayDistance, _interactionLayers);
            Vector3 hitPoint = _hasHit ? _currentHit.point : (origin + direction * _maxRayDistance);

            if (_lineRenderer != null)
            {
                _lineRenderer.SetPosition(0, origin);
                _lineRenderer.SetPosition(1, hitPoint);

                Color targetCol = _hasHit ? _hoverColor : _idleColor;
                _lineRenderer.startColor = targetCol;
                _lineRenderer.endColor = targetCol;
            }

            _hoveredObject = _hasHit ? _currentHit.collider.gameObject : null;
        }

        /// <summary>
        /// Simulates a pointer click on whatever UI button or 3D target is currently targeted by the ray.
        /// </summary>
        public bool TriggerClick()
        {
            if (!_hasHit || _hoveredObject == null) return false;

            // Flash laser color
            if (_lineRenderer != null)
            {
                _lineRenderer.startColor = _clickColor;
                _lineRenderer.endColor = _clickColor;
            }

            // 1. Check for UI Button
            var button = _hoveredObject.GetComponentInParent<Button>();
            if (button != null && button.interactable)
            {
                button.onClick?.Invoke();
                return true;
            }

            // 2. Check for UI Slider
            var slider = _hoveredObject.GetComponentInParent<Slider>();
            if (slider != null)
            {
                // Adjust slider relative to ray hit
                slider.value = Mathf.Clamp(slider.value + (slider.maxValue - slider.minValue) * 0.1f, slider.minValue, slider.maxValue);
                return true;
            }

            // 3. Check for AbstractVisualStimulus
            var stimulus = _hoveredObject.GetComponentInParent<AbstractVisualStimulus>();
            if (stimulus != null)
            {
                stimulus.RegisterEngagement();
                return true;
            }

            // 4. Check for TacticalTarget
            var tacticalTarget = _hoveredObject.GetComponentInParent<TacticalTarget>();
            if (tacticalTarget != null)
            {
                tacticalTarget.OnTargetHit();
                return true;
            }

            // 4. Check for legacy TargetTrialMetadata
            var metadata = _hoveredObject.GetComponentInParent<TargetTrialMetadata>();
            if (metadata != null)
            {
                metadata.RegisterDecision(true);
                return true;
            }

            return false;
        }
    }
}
