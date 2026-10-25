using System;
using System.Collections;
using System.Collections.Generic;
using Interfaces;
using Resources;
using UnityEngine;

namespace IVRAR.Group02.Environment
{
    /// <summary>
    /// Interactive Red Hazmat Explosive Barrel.
    /// Detonates upon taking weapon ballistic damage or chain-explosions.
    /// Deals radius blast damage to enemies, knocks down targets, and triggers cinematic camera shake.
    /// </summary>
    public class ExplosiveBarrel : MonoBehaviour, IDamageable
    {
        [Header("Barrel Health & Blast")]
        [SerializeField] private float _health = 35f;
        [SerializeField] private float _blastRadius = 5.5f;
        [SerializeField] private float _blastDamage = 150f;
        [SerializeField] private float _blastImpulse = 800f;

        private bool _isExploded = false;
        private AudioSource _audioSource;
        private Renderer[] _renderers;

        public static event Action<Vector3, int> OnBarrelExploded;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0.85f;
            _audioSource.minDistance = 3f;
            _audioSource.maxDistance = 60f;
        }

        public void Damage(DamageData damageData)
        {
            if (_isExploded) return;

            _health -= 35f;
            StartCoroutine(FlashDamageCoroutine());

            if (_health <= 0f)
            {
                Explode();
            }
        }

        private IEnumerator FlashDamageCoroutine()
        {
            foreach (var r in _renderers)
            {
                if (r != null && r.material != null && r.material.HasProperty("_Color"))
                {
                    r.material.color = Color.white;
                }
            }
            yield return new WaitForSeconds(0.06f);
            foreach (var r in _renderers)
            {
                if (r != null && r.material != null && r.material.HasProperty("_Color"))
                {
                    r.material.color = new Color(0.85f, 0.15f, 0.12f, 1f);
                }
            }
        }

        public void Explode()
        {
            if (_isExploded) return;
            _isExploded = true;

            Vector3 blastCenter = transform.position + Vector3.up * 0.5f;

            // 1. Play Explosion Sound
            AudioClip boomClip = GenerateExplosionAudioClip();
            if (boomClip != null)
            {
                _audioSource.pitch = UnityEngine.Random.Range(0.85f, 1.05f);
                _audioSource.PlayOneShot(boomClip, 1.0f);
            }

            // 2. Spawn Visual Fireball & Flash Light
            SpawnExplosionVFX(blastCenter);

            // 3. Radius Blast Overlap Check
            Collider[] colliders = Physics.OverlapSphere(blastCenter, _blastRadius);
            int targetsNeutralized = 0;

            foreach (var hit in colliders)
            {
                if (hit.gameObject == gameObject) continue;

                // Chain-detonate other explosive barrels
                var otherBarrel = hit.GetComponentInParent<ExplosiveBarrel>();
                if (otherBarrel != null && !otherBarrel._isExploded)
                {
                    otherBarrel.Damage(new DamageData { contactPoint = blastCenter, force = Vector3.up * 50f });
                    continue;
                }

                // Blast damage to moving enemies & target popups
                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    Vector3 pushDir = (hit.transform.position - blastCenter).normalized;
                    damageable.Damage(new DamageData
                    {
                        contactPoint = hit.ClosestPoint(blastCenter),
                        normalPoint = Quaternion.LookRotation(pushDir),
                        force = pushDir * _blastImpulse
                    });
                    targetsNeutralized++;
                }

                // Push rigidbodies (casing brass, debris)
                var rb = hit.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(_blastImpulse, blastCenter, _blastRadius, 1.2f, ForceMode.Impulse);
                }
            }

            OnBarrelExploded?.Invoke(blastCenter, targetsNeutralized);

            // 4. Burn barrel mesh into charred wreck
            StartCoroutine(CharredRemainsCoroutine());
        }

        private IEnumerator CharredRemainsCoroutine()
        {
            // Shrink and char the drum
            transform.localScale = new Vector3(transform.localScale.x * 1.15f, transform.localScale.y * 0.45f, transform.localScale.z * 1.15f);
            foreach (var r in _renderers)
            {
                if (r != null && r.material != null && r.material.HasProperty("_Color"))
                {
                    r.material.color = new Color(0.12f, 0.12f, 0.12f, 1f);
                }
            }

            // Disable collider so player doesn't trip on invisible bounds
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            yield return new WaitForSeconds(8f);
            Destroy(gameObject);
        }

        private void SpawnExplosionVFX(Vector3 center)
        {
            // Expanding Fiery Sphere
            GameObject fireSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fireSphere.name = "ExplosionFireball";
            fireSphere.transform.position = center;
            fireSphere.transform.localScale = Vector3.one * 0.8f;
            Destroy(fireSphere.GetComponent<Collider>());

            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (unlit != null)
            {
                Material fireMat = new Material(unlit) { color = new Color(1.0f, 0.45f, 0.05f, 1f) };
                fireSphere.GetComponent<Renderer>().material = fireMat;
            }

            // Explosion Point Light
            GameObject lightObj = new GameObject("ExplosionFlashLight");
            lightObj.transform.position = center;
            Light blastLight = lightObj.AddComponent<Light>();
            blastLight.type = LightType.Point;
            blastLight.range = 14f;
            blastLight.intensity = 8f;
            blastLight.color = new Color(1.0f, 0.65f, 0.2f);

            // Animate Fireball Expansion and Light Fade
            StartCoroutine(AnimateFireball(fireSphere, blastLight));
        }

        private IEnumerator AnimateFireball(GameObject fireball, Light blastLight)
        {
            float elapsed = 0f;
            float duration = 0.45f;
            Vector3 startScale = Vector3.one * 0.8f;
            Vector3 endScale = Vector3.one * (_blastRadius * 1.4f);

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                if (fireball != null)
                {
                    fireball.transform.localScale = Vector3.Lerp(startScale, endScale, Mathf.Sqrt(t));
                }
                if (blastLight != null)
                {
                    blastLight.intensity = Mathf.Lerp(8f, 0f, t);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (fireball != null) Destroy(fireball);
            if (blastLight != null) Destroy(blastLight.gameObject);
        }

        public static AudioClip GenerateExplosionAudioClip()
        {
            int sampleRate = 44100;
            float duration = 0.75f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 5.5f);
                // Low punch + rumble noise
                float subBass = Mathf.Sin(2f * Mathf.PI * 65f * t * (1f - t * 0.5f));
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.65f;
                samples[i] = (subBass * 0.65f + noise) * envelope;
            }

            AudioClip clip = AudioClip.Create("ExplosionBlast", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
