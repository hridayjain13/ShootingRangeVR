using UnityEngine;
using Interfaces;
using Resources;

namespace IVRAR.Group02.TargetManagement
{
    /// <summary>
    /// Tactical target representation distinguishing between Hostile (shoot) and Civilian (don't-shoot).
    /// Integrates with the existing weapon ballistics pipeline via IDamageable
    /// as well as direct raycast / interaction events.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TacticalTarget : MonoBehaviour, IDamageable
    {
        [Header("Target Identity")]
        [SerializeField] private bool _isHostile;
        [SerializeField] private GameObject _weaponProp;
        [SerializeField] private GameObject _neutralProp;
        [SerializeField] private Renderer _targetRenderer;

        [Header("Visual Feedback")]
        [SerializeField] private Color _hostileOutlineColor = new Color(0.9f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color _civilianOutlineColor = new Color(0.2f, 0.8f, 0.3f, 1f);

        private TargetTrialMetadata _metadata;
        private bool _hasBeenHit;

        public bool IsHostile => _isHostile;

        private void Awake()
        {
            _metadata = GetComponent<TargetTrialMetadata>();
            ApplyVisualIndicators();
        }

        public void SetTargetType(bool isHostile)
        {
            _isHostile = isHostile;
            ApplyVisualIndicators();
        }

        private void ApplyVisualIndicators()
        {
            if (_weaponProp != null) _weaponProp.SetActive(_isHostile);
            if (_neutralProp != null) _neutralProp.SetActive(!_isHostile);

            if (_targetRenderer != null)
            {
                // Subtle hue tint for distinction under low light
                _targetRenderer.material.color = _isHostile ? new Color(0.85f, 0.35f, 0.35f) : new Color(0.4f, 0.75f, 0.45f);
            }
        }

        /// <summary>
        /// IDamageable implementation: called automatically by existing CartridgeBulletImpact when shot.
        /// </summary>
        public void Damage(DamageData damageData)
        {
            OnTargetHit();
        }

        /// <summary>
        /// Can also be called directly by VR laser pointer, interaction events, or trigger input.
        /// </summary>
        public void OnTargetHit()
        {
            if (_hasBeenHit) return;
            _hasBeenHit = true;

            if (_metadata != null)
            {
                _metadata.RegisterDecision(true);
            }
            else
            {
                Destroy(gameObject, 0.05f);
            }
        }
    }
}
