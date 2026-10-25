using UnityEngine;

namespace IVRAR.Group02.SensoryStress
{
    /// <summary>
    /// Manages spatialized audio stressors (gunfire startle cues, security sirens, alarms)
    /// with strict volume caps to protect trainee hearing.
    /// </summary>
    public class AcousticStressController : MonoBehaviour
    {
        [Header("Audio Components")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _gunshotStartleClip;
        [SerializeField] private AudioClip _sirenAlarmClip;
        [SerializeField] private AudioClip _ricochetClip;

        [Header("Gain Capping (Safety)")]
        [SerializeField, Range(0.1f, 1.0f)] private float _maxPermissibleGain = 0.80f;

        private void Awake()
        {
            if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
            if (_audioSource != null)
            {
                _audioSource.spatialBlend = 1.0f; // Full 3D spatialization
                _audioSource.playOnAwake = false;
            }
        }

        public void PlayGunshotCue(float gainMultiplier, Vector3 soundOrigin)
        {
            PlayCue(_gunshotStartleClip, gainMultiplier, soundOrigin, 0.95f, 1.05f);
        }

        public void PlayAlarmCue(float gainMultiplier, Vector3 soundOrigin)
        {
            PlayCue(_sirenAlarmClip, gainMultiplier, soundOrigin, 1.0f, 1.0f);
        }

        public void PlayRicochetCue(float gainMultiplier, Vector3 soundOrigin)
        {
            PlayCue(_ricochetClip, gainMultiplier, soundOrigin, 0.9f, 1.1f);
        }

        private void PlayCue(AudioClip clip, float gain, Vector3 origin, float minPitch, float maxPitch)
        {
            if (_audioSource == null || clip == null) return;

            transform.position = origin;
            float finalVolume = Mathf.Clamp(gain * _maxPermissibleGain, 0.05f, _maxPermissibleGain);
            _audioSource.volume = finalVolume;
            _audioSource.pitch = Random.Range(minPitch, maxPitch);
            _audioSource.PlayOneShot(clip);
        }

        public void StopAllAudio()
        {
            if (_audioSource != null && _audioSource.isPlaying)
                _audioSource.Stop();
        }
    }
}
