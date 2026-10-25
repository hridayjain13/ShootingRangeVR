using System;
using System.Collections;
using System.Collections.Generic;
using Interfaces;
using Resources;
using UnityEngine;

namespace IVRAR.Group02.Environment
{
    public enum EnemyState
    {
        Patrolling,
        TakingCover,
        Peeking,
        KnockedDown
    }

    /// <summary>
    /// AI Moving Tactical Enemy Controller.
    /// Operates dynamic lane patrols, cover-to-cover movement, peeking, and ragdoll-style hit reactions.
    /// Re-spawns dynamically to keep combat engaging and fast-paced.
    /// </summary>
    public class MovingEnemyController : MonoBehaviour, IDamageable
    {
        [Header("Patrol & Movement")]
        [SerializeField] private float _patrolMinX = -4.5f;
        [SerializeField] private float _patrolMaxX = 4.5f;
        [SerializeField] private float _moveSpeed = 2.4f;
        [SerializeField] private float _patrolZ = 12f;

        [Header("Combat Stats")]
        [SerializeField] private float _maxHealth = 100f;
        private float _currentHealth;

        private EnemyState _state = EnemyState.Patrolling;
        private int _moveDirection = 1;
        private float _stateTimer = 0f;
        private Vector3 _spawnBasePosition;
        private Quaternion _originalRotation;

        private Renderer[] _renderers;
        private AudioSource _audioSource;
        private GameObject _headObject;
        private GameObject _alertIndicator;

        public static event Action<MovingEnemyController, bool, int> OnEnemyKilled;

        private void Awake()
        {
            _spawnBasePosition = transform.position;
            _originalRotation = transform.rotation;
            _currentHealth = _maxHealth;
            _renderers = GetComponentsInChildren<Renderer>();

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0.85f;
            _audioSource.minDistance = 2f;
            _audioSource.maxDistance = 50f;

            Transform headTransform = transform.Find("Head");
            if (headTransform != null) _headObject = headTransform.gameObject;

            CreateTacticalHeadgear();
        }

        private void Start()
        {
            _patrolZ = transform.position.z;
            _patrolMinX = Mathf.Clamp(transform.position.x - 3.2f, -12.5f, 12.5f);
            _patrolMaxX = Mathf.Clamp(transform.position.x + 3.2f, -12.5f, 12.5f);
            _moveDirection = UnityEngine.Random.value > 0.5f ? 1 : -1;
            _stateTimer = UnityEngine.Random.Range(2f, 5f);
        }

        private void CreateTacticalHeadgear()
        {
            // Glowing Red Combat Visor
            GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "HostileVisor";
            visor.transform.SetParent(transform, false);
            visor.transform.localPosition = new Vector3(0f, 1.82f, -0.16f);
            visor.transform.localScale = new Vector3(0.24f, 0.06f, 0.08f);
            Destroy(visor.GetComponent<Collider>());

            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (unlit != null)
            {
                Material redGlow = new Material(unlit) { color = new Color(1.0f, 0.15f, 0.1f, 1f) };
                visor.GetComponent<Renderer>().material = redGlow;
            }

            // Tactical Combat Helmet
            GameObject helmet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            helmet.name = "TacticalHelmet";
            helmet.transform.SetParent(transform, false);
            helmet.transform.localPosition = new Vector3(0f, 1.90f, 0f);
            helmet.transform.localScale = new Vector3(0.42f, 0.28f, 0.44f);
            Destroy(helmet.GetComponent<Collider>());

            Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (lit != null)
            {
                Material helmetMat = new Material(lit) { color = new Color(0.18f, 0.22f, 0.18f, 1f) }; // olive drab
                helmet.GetComponent<Renderer>().material = helmetMat;
            }

            // Alert Overhead Marker
            _alertIndicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _alertIndicator.name = "AlertMarker";
            _alertIndicator.transform.SetParent(transform, false);
            _alertIndicator.transform.localPosition = new Vector3(0f, 2.3f, 0f);
            _alertIndicator.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            Destroy(_alertIndicator.GetComponent<Collider>());
            if (unlit != null)
            {
                Material alertMat = new Material(unlit) { color = Color.red };
                _alertIndicator.GetComponent<Renderer>().material = alertMat;
            }
            _alertIndicator.SetActive(false);
        }

        private void Update()
        {
            if (_state == EnemyState.KnockedDown) return;

            _stateTimer -= Time.deltaTime;

            switch (_state)
            {
                case EnemyState.Patrolling:
                    ExecutePatrol();
                    if (_stateTimer <= 0f)
                    {
                        // Take cover
                        _state = EnemyState.TakingCover;
                        _stateTimer = UnityEngine.Random.Range(1.8f, 3.2f);
                    }
                    break;

                case EnemyState.TakingCover:
                    // Duck slightly behind nearest cover
                    transform.position = Vector3.Lerp(transform.position, new Vector3(transform.position.x, _spawnBasePosition.y - 0.45f, transform.position.z), Time.deltaTime * 4f);
                    if (_stateTimer <= 0f)
                    {
                        _state = EnemyState.Peeking;
                        _stateTimer = UnityEngine.Random.Range(1.5f, 2.5f);
                        if (_alertIndicator != null) _alertIndicator.SetActive(true);
                    }
                    break;

                case EnemyState.Peeking:
                    // Pop up and track player direction
                    transform.position = Vector3.Lerp(transform.position, new Vector3(transform.position.x, _spawnBasePosition.y, transform.position.z), Time.deltaTime * 6f);
                    if (_stateTimer <= 0f)
                    {
                        if (_alertIndicator != null) _alertIndicator.SetActive(false);
                        _state = EnemyState.Patrolling;
                        _moveDirection = -_moveDirection; // Reverse patrol direction
                        _stateTimer = UnityEngine.Random.Range(2.5f, 5.0f);
                    }
                    break;
            }
        }

        private void ExecutePatrol()
        {
            Vector3 pos = transform.position;
            pos.x += _moveDirection * _moveSpeed * Time.deltaTime;

            if (pos.x >= _patrolMaxX)
            {
                pos.x = _patrolMaxX;
                _moveDirection = -1;
            }
            else if (pos.x <= _patrolMinX)
            {
                pos.x = _patrolMinX;
                _moveDirection = 1;
            }

            transform.position = pos;
        }

        public void Damage(DamageData damageData)
        {
            if (_state == EnemyState.KnockedDown) return;

            bool isHeadshot = damageData.contactPoint.y > transform.position.y + 1.62f;
            float damageAmount = isHeadshot ? 150f : 45f;
            _currentHealth -= damageAmount;

            StartCoroutine(FlashRedCoroutine());

            if (_currentHealth <= 0f)
            {
                KnockDown(isHeadshot);
            }
            else
            {
                // Stumble flinch
                StartCoroutine(FlinchCoroutine(damageData.force));
            }
        }

        private IEnumerator FlashRedCoroutine()
        {
            foreach (var r in _renderers)
            {
                if (r != null && r.material != null && r.material.HasProperty("_Color"))
                {
                    r.material.color = Color.red;
                }
            }
            yield return new WaitForSeconds(0.08f);
            foreach (var r in _renderers)
            {
                if (r != null && r.material != null && r.material.HasProperty("_Color"))
                {
                    // Restore original theme
                    r.material.color = new Color(0.15f, 0.22f, 0.35f, 1f);
                }
            }
        }

        private IEnumerator FlinchCoroutine(Vector3 force)
        {
            Quaternion tilt = _originalRotation * Quaternion.Euler(UnityEngine.Random.Range(-12f, -5f), UnityEngine.Random.Range(-10f, 10f), 0f);
            float t = 0f;
            while (t < 0.12f)
            {
                transform.rotation = Quaternion.Slerp(_originalRotation, tilt, t / 0.12f);
                t += Time.deltaTime;
                yield return null;
            }
            t = 0f;
            while (t < 0.18f)
            {
                transform.rotation = Quaternion.Slerp(tilt, _originalRotation, t / 0.18f);
                t += Time.deltaTime;
                yield return null;
            }
            transform.rotation = _originalRotation;
        }

        private void KnockDown(bool isHeadshot)
        {
            _state = EnemyState.KnockedDown;
            if (_alertIndicator != null) _alertIndicator.SetActive(false);

            int scorePoints = isHeadshot ? 250 : 125;
            OnEnemyKilled?.Invoke(this, isHeadshot, scorePoints);

            StartCoroutine(KnockDownFallAndRespawnCoroutine(isHeadshot));
        }

        private IEnumerator KnockDownFallAndRespawnCoroutine(bool isHeadshot)
        {
            Quaternion fallRot = _originalRotation * Quaternion.Euler(-85f, 0f, 0f);
            Vector3 startPos = transform.position;
            Vector3 endPos = new Vector3(startPos.x, _spawnBasePosition.y - 0.2f, startPos.z + 0.3f);

            float t = 0f;
            float duration = 0.35f;
            while (t < duration)
            {
                float frac = t / duration;
                transform.rotation = Quaternion.Slerp(_originalRotation, fallRot, frac * frac);
                transform.position = Vector3.Lerp(startPos, endPos, frac);
                t += Time.deltaTime;
                yield return null;
            }

            // Wait on ground
            yield return new WaitForSeconds(3.5f);

            // Respawn with full health at fresh position
            _currentHealth = _maxHealth;
            transform.position = _spawnBasePosition;
            transform.rotation = _originalRotation;
            _moveDirection = UnityEngine.Random.value > 0.5f ? 1 : -1;
            _stateTimer = UnityEngine.Random.Range(2f, 4f);
            _state = EnemyState.Patrolling;
        }
    }
}
