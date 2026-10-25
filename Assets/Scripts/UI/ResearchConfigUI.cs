using UnityEngine;
using UnityEngine.UI;
using IVRAR.Group02.Core;
using IVRAR.Group02.Environment;

namespace IVRAR.Group02.UI
{
    /// <summary>
    /// In-VR UI for researchers to adjust ScriptableObject experimental parameters
    /// such as fog extinction coefficient, strobe frequency, and trial count.
    /// </summary>
    public class ResearchConfigUI : MonoBehaviour
    {
        [Header("ScriptableObject Configuration Reference")]
        [SerializeField] private ExperimentConfig _activeConfig;

        [Header("Sliders & Controls")]
        [SerializeField] private Slider _sliderFogDensity;
        [SerializeField] private Text _lblFogDensityValue;

        [SerializeField] private Slider _sliderStrobeFrequency;
        [SerializeField] private Text _lblStrobeFreqValue;

        [SerializeField] private Slider _sliderTrialCount;
        [SerializeField] private Text _lblTrialCountValue;

        [SerializeField] private Dropdown _dropdownPreset;
        [SerializeField] private Button _btnApplyPreset;

        [Header("Environment Subsystem Link")]
        [SerializeField] private LowVisibilityEnvironmentController _envController;

        private void Awake()
        {
            if (_envController == null) _envController = FindObjectOfType<LowVisibilityEnvironmentController>();

            if (_sliderFogDensity != null) _sliderFogDensity.onValueChanged.AddListener(OnFogSliderChanged);
            if (_sliderStrobeFrequency != null) _sliderStrobeFrequency.onValueChanged.AddListener(OnStrobeSliderChanged);
            if (_sliderTrialCount != null) _sliderTrialCount.onValueChanged.AddListener(OnTrialCountChanged);
            if (_btnApplyPreset != null) _btnApplyPreset.onClick.AddListener(ApplySelectedPreset);

            InitializeUIValues();
        }

        private void InitializeUIValues()
        {
            if (_activeConfig != null)
            {
                if (_sliderFogDensity != null)
                {
                    _sliderFogDensity.minValue = 0f;
                    _sliderFogDensity.maxValue = 0.15f;
                    _sliderFogDensity.value = _activeConfig.fogExtinctionPerMeter;
                    OnFogSliderChanged(_sliderFogDensity.value);
                }

                if (_sliderStrobeFrequency != null)
                {
                    _sliderStrobeFrequency.minValue = 1f;
                    _sliderStrobeFrequency.maxValue = 6f; // Safe cap
                    _sliderStrobeFrequency.value = _activeConfig.strobeFrequencyHz;
                    OnStrobeSliderChanged(_sliderStrobeFrequency.value);
                }

                if (_sliderTrialCount != null)
                {
                    _sliderTrialCount.minValue = 5;
                    _sliderTrialCount.maxValue = 50;
                    _sliderTrialCount.wholeNumbers = true;
                    _sliderTrialCount.value = _activeConfig.totalTrialsInBlock;
                    OnTrialCountChanged(_sliderTrialCount.value);
                }
            }

            if (_dropdownPreset != null)
            {
                _dropdownPreset.options.Clear();
                _dropdownPreset.options.Add(new Dropdown.OptionData("Preset 1: Heavy Fog + High Strobe (Sensory Stress)"));
                _dropdownPreset.options.Add(new Dropdown.OptionData("Preset 2: Moderate Fog + Low Strobe"));
                _dropdownPreset.options.Add(new Dropdown.OptionData("Preset 3: Clear Baseline (No Fog/Strobe)"));
                _dropdownPreset.value = 0;
            }
        }

        private void OnFogSliderChanged(float val)
        {
            if (_lblFogDensityValue != null) _lblFogDensityValue.text = string.Format("{0:F3} /m", val);
            if (_activeConfig != null) _activeConfig.fogExtinctionPerMeter = val;
            if (_envController != null) _envController.SetFogDensity(val);
        }

        private void OnStrobeSliderChanged(float val)
        {
            if (_lblStrobeFreqValue != null) _lblStrobeFreqValue.text = string.Format("{0:F1} Hz", val);
            if (_activeConfig != null) _activeConfig.strobeFrequencyHz = val;
        }

        private void OnTrialCountChanged(float val)
        {
            int count = Mathf.RoundToInt(val);
            if (_lblTrialCountValue != null) _lblTrialCountValue.text = count.ToString();
            if (_activeConfig != null) _activeConfig.totalTrialsInBlock = count;
        }

        public void ApplySelectedPreset()
        {
            if (_dropdownPreset == null || _activeConfig == null) return;

            switch (_dropdownPreset.value)
            {
                case 0: // Heavy Fog + High Strobe
                    _activeConfig.condition = ExperimentalCondition.LowVisibility_SensoryStress;
                    _activeConfig.fogExtinctionPerMeter = 0.065f;
                    _activeConfig.strobeFrequencyHz = 6.0f;
                    _activeConfig.maxStrobePulses = 5;
                    break;
                case 1: // Moderate Fog + Low Strobe
                    _activeConfig.condition = ExperimentalCondition.LowVisibility_SensoryStress;
                    _activeConfig.fogExtinctionPerMeter = 0.035f;
                    _activeConfig.strobeFrequencyHz = 3.0f;
                    _activeConfig.maxStrobePulses = 2;
                    break;
                case 2: // Clear Baseline
                    _activeConfig.condition = ExperimentalCondition.Baseline_ClearDaylight;
                    _activeConfig.fogExtinctionPerMeter = 0.0f;
                    _activeConfig.strobeFrequencyHz = 1.0f;
                    _activeConfig.maxStrobePulses = 0;
                    break;
            }

            InitializeUIValues();
            if (_envController != null) _envController.ApplyConfiguration(_activeConfig);
        }
    }
}
