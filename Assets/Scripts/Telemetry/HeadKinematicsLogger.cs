using UnityEngine;

namespace IVRAR.Group02.Telemetry
{
    /// <summary>
    /// Samples high-rate head kinematic metrics (rotational velocity, angular acceleration, yaw tremor)
    /// to provide an exploratory behavioral stress and cognitive load index.
    /// </summary>
    public class HeadKinematicsLogger : MonoBehaviour
    {
        [SerializeField] private Transform _headTransform;
        [SerializeField, Range(60, 120)] private int _sampleRateHz = 90;

        private float _accumulator;
        private Quaternion _lastRotation;
        private Vector3 _angularVelocity;
        private float _yawJitterMetric;

        public Vector3 AngularVelocity => _angularVelocity;
        public float YawJitterMetric => _yawJitterMetric;

        private void Start()
        {
            if (_headTransform == null && Camera.main != null)
                _headTransform = Camera.main.transform;

            if (_headTransform != null)
                _lastRotation = _headTransform.rotation;
        }

        private void Update()
        {
            if (_headTransform == null) return;

            float sampleInterval = 1f / _sampleRateHz;
            _accumulator += Time.unscaledDeltaTime;

            while (_accumulator >= sampleInterval)
            {
                _accumulator -= sampleInterval;
                Quaternion currentRot = _headTransform.rotation;

                // Compute angular delta in degrees
                Quaternion delta = currentRot * Quaternion.Inverse(_lastRotation);
                delta.ToAngleAxis(out float angle, out Vector3 axis);
                if (angle > 180f) angle -= 360f;

                _angularVelocity = axis * (angle / sampleInterval);
                _yawJitterMetric = Mathf.Abs(_angularVelocity.y);

                _lastRotation = currentRot;
            }
        }
    }
}
