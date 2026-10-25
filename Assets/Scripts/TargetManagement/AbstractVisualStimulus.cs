using System;
using UnityEngine;
using Interfaces;
using Resources;

namespace IVRAR.Group02.TargetManagement
{
    /// <summary>
    /// Categories of abstract visual stimuli used in the signal-detection and prioritization tasks.
    /// Replaces realistic human figures with standardized geometric visual stimuli.
    /// </summary>
    public enum StimulusCategory
    {
        SignalAbsent_CatchTrial = 0,   // No threat / empty noise anchor (Correct: Withhold or No-Signal button)
        HighThreat_Tier1 = 1,          // High-priority threat (Chevron / Orange-Red, Priority 1)
        MediumThreat_Tier2 = 2,        // Secondary threat (Diamond / Amber-Yellow, Priority 2)
        Neutral_Civilian = 3,          // Neutral non-threat (Circle / Soft Green, Priority 0 - DO NOT ENGAGE)
        Friendly_Protected = 4         // Friendly protected asset (Shield / Cyan-Blue, Priority 0 - DO NOT ENGAGE)
    }

    /// <summary>
    /// Abstract, clearly labeled visual stimulus for psychophysical signal detection and threat prioritization.
    /// Uses standardized 3D geometric shapes (cubes, diamonds, cylinders, spheres) and high-contrast labels
    /// to isolate visual identification from realistic human target biases.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class AbstractVisualStimulus : MonoBehaviour, IDamageable
    {
        [Header("Stimulus Identity")]
        [SerializeField] private StimulusCategory _category = StimulusCategory.HighThreat_Tier1;
        [SerializeField] private int _threatPriority = 1; // 1 = Highest, 2 = Medium, 0 = Non-combatant / Catch

        [Header("Visual Meshes & Labels")]
        [SerializeField] private MeshFilter _shapeMeshFilter;
        [SerializeField] private MeshRenderer _shapeRenderer;
        [SerializeField] private TextMesh _labelTextMesh;

        [Header("Color Coding (Psychophysical)")]
        [SerializeField] private Color _colorHighThreat = new Color(0.95f, 0.25f, 0.15f);
        [SerializeField] private Color _colorMediumThreat = new Color(0.95f, 0.70f, 0.10f);
        [SerializeField] private Color _colorCivilian = new Color(0.20f, 0.80f, 0.35f);
        [SerializeField] private Color _colorFriendly = new Color(0.15f, 0.65f, 0.95f);
        [SerializeField] private Color _colorSignalAbsent = new Color(0.40f, 0.40f, 0.45f, 0.3f);

        public event Action<AbstractVisualStimulus> OnStimulusEngaged;

        public StimulusCategory Category => _category;
        public int ThreatPriority => _threatPriority;
        public bool IsTargetThreat => (_category == StimulusCategory.HighThreat_Tier1 || _category == StimulusCategory.MediumThreat_Tier2);
        public bool IsCatchTrial => (_category == StimulusCategory.SignalAbsent_CatchTrial);
        public bool HasBeenEngaged { get; private set; }

        private void Awake()
        {
            EnsureComponents();
            ApplyStimulusAppearance();
        }

        private void EnsureComponents()
        {
            if (_shapeMeshFilter == null) _shapeMeshFilter = GetComponent<MeshFilter>();
            if (_shapeRenderer == null) _shapeRenderer = GetComponent<MeshRenderer>();

            if (_shapeMeshFilter == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _shapeMeshFilter = gameObject.AddComponent<MeshFilter>();
                _shapeMeshFilter.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
                _shapeRenderer = gameObject.AddComponent<MeshRenderer>();
                _shapeRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                DestroyImmediate(go);
            }

            if (_labelTextMesh == null)
            {
                _labelTextMesh = GetComponentInChildren<TextMesh>();
                if (_labelTextMesh == null)
                {
                    var labelObj = new GameObject("StimulusLabel");
                    labelObj.transform.SetParent(transform);
                    labelObj.transform.localPosition = new Vector3(0f, 0.75f, 0f);
                    labelObj.transform.localRotation = Quaternion.identity;
                    _labelTextMesh = labelObj.AddComponent<TextMesh>();
                    _labelTextMesh.fontSize = 24;
                    _labelTextMesh.characterSize = 0.05f;
                    _labelTextMesh.anchor = TextAnchor.MiddleCenter;
                    _labelTextMesh.alignment = TextAlignment.Center;
                    _labelTextMesh.color = Color.white;
                }
            }
        }

        /// <summary>
        /// Configures the category, contrast scaling, and threat priority of the stimulus.
        /// </summary>
        public void Configure(StimulusCategory category, float contrastMultiplier = 1.0f)
        {
            _category = category;
            switch (_category)
            {
                case StimulusCategory.HighThreat_Tier1:
                    _threatPriority = 1;
                    break;
                case StimulusCategory.MediumThreat_Tier2:
                    _threatPriority = 2;
                    break;
                default:
                    _threatPriority = 0;
                    break;
            }

            ApplyStimulusAppearance(contrastMultiplier);
        }

        private void ApplyStimulusAppearance(float contrast = 1.0f)
        {
            EnsureComponents();

            Color baseColor;
            string labelText;

            switch (_category)
            {
                case StimulusCategory.HighThreat_Tier1:
                    baseColor = _colorHighThreat;
                    labelText = "[THREAT 1]\nCHEVRON";
                    transform.localScale = new Vector3(0.6f, 0.9f, 0.2f);
                    break;
                case StimulusCategory.MediumThreat_Tier2:
                    baseColor = _colorMediumThreat;
                    labelText = "[THREAT 2]\nDIAMOND";
                    transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
                    break;
                case StimulusCategory.Neutral_Civilian:
                    baseColor = _colorCivilian;
                    labelText = "[CIVILIAN]\nCIRCLE";
                    transform.localScale = new Vector3(0.5f, 0.8f, 0.5f);
                    break;
                case StimulusCategory.Friendly_Protected:
                    baseColor = _colorFriendly;
                    labelText = "[FRIENDLY]\nSHIELD";
                    transform.localScale = new Vector3(0.6f, 0.7f, 0.3f);
                    break;
                case StimulusCategory.SignalAbsent_CatchTrial:
                default:
                    baseColor = _colorSignalAbsent;
                    labelText = "[NO SIGNAL]\nEMPTY";
                    transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    break;
            }

            // Apply contrast attenuation towards dark background
            Color modulatedColor = Color.Lerp(new Color(0.05f, 0.05f, 0.08f), baseColor, Mathf.Clamp01(contrast));

            if (_shapeRenderer != null && _shapeRenderer.material != null)
            {
                _shapeRenderer.material.color = modulatedColor;
            }

            if (_labelTextMesh != null)
            {
                _labelTextMesh.text = labelText;
                _labelTextMesh.color = Color.Lerp(new Color(0.2f, 0.2f, 0.25f), Color.white, Mathf.Clamp01(contrast));
            }
        }

        /// <summary>
        /// IDamageable implementation for physical bullet raycasts.
        /// </summary>
        public void Damage(DamageData damageData)
        {
            RegisterEngagement();
        }

        /// <summary>
        /// Called when engaged by controller trigger, VR laser pointer, or desktop click.
        /// </summary>
        public void RegisterEngagement()
        {
            if (HasBeenEngaged) return;
            HasBeenEngaged = true;

            OnStimulusEngaged?.Invoke(this);

            // Trigger TacticalTarget compatibility if present
            var tactical = GetComponent<TacticalTarget>();
            if (tactical != null)
            {
                tactical.OnTargetHit();
            }

            // Visual feedback: flash white and collapse
            if (_shapeRenderer != null)
            {
                _shapeRenderer.material.color = Color.white;
            }

            Destroy(gameObject, 0.08f);
        }
    }
}
