using System;
using System.Collections;
using System.Collections.Generic;
using Interfaces;
using Resources;
using UnityEngine;

namespace IVRAR.Group02.Environment
{
    /// <summary>
    /// Controls pop-up, ducking, friendly vs hostile visual state, and hit registration
    /// for tactical shooting targets in both Free Practice and Active Drill modes.
    /// </summary>
    public class TargetPopUpController : MonoBehaviour, IDamageable
    {
        public TargetClassification Classification { get; private set; } = TargetClassification.HostileThreat;
        public bool IsPoppedUp { get; private set; } = true;

        private float _spawnTime = 0f;
        private float _exposureDuration = 2.5f;
        private Coroutine _exposureCoroutine;
        private Quaternion _uprightRotation;
        private Quaternion _duckedRotation;

        // Visual Parts for switching between Hostile Threat and Civilian Friendly
        private Renderer _torsoRenderer;
        private Renderer _headRenderer;
        private Renderer _beaconRenderer;
        private GameObject _hostileRings;
        private GameObject _civilianCross;

        private Material _hostileTorsoMat;
        private Material _civilianTorsoMat;
        private Material _hostileBeaconMat;
        private Material _civilianBeaconMat;

        private void Awake()
        {
            _uprightRotation = transform.localRotation;
            _duckedRotation = _uprightRotation * Quaternion.Euler(-80f, 0f, 0f);

            FindVisualParts();
        }

        private void Start()
        {
            if (TacticalDrillGameManager.Instance != null)
            {
                TacticalDrillGameManager.Instance.RegisterTarget(this);
            }
        }

        private void OnDestroy()
        {
            if (TacticalDrillGameManager.Instance != null)
            {
                TacticalDrillGameManager.Instance.UnregisterTarget(this);
            }
        }

        private void FindVisualParts()
        {
            Transform torsoT = transform.Find("Torso");
            if (torsoT != null)
            {
                _torsoRenderer = torsoT.GetComponent<Renderer>();
                _hostileRings = torsoT.Find("Ring_Outer_White")?.gameObject;
            }

            Transform headT = transform.Find("Head");
            if (headT != null) _headRenderer = headT.GetComponent<Renderer>();

            Transform beaconT = transform.Find("TacticalBeaconLED");
            if (beaconT != null) _beaconRenderer = beaconT.GetComponent<Renderer>();

            Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");

            _hostileTorsoMat = new Material(lit) { color = new Color(0.12f, 0.22f, 0.38f, 1f) };
            _civilianTorsoMat = new Material(lit) { color = new Color(0.15f, 0.55f, 0.25f, 1f) }; // Bright Green for friendly

            _hostileBeaconMat = new Material(unlit) { color = new Color(1.0f, 0.15f, 0.15f, 1f) }; // Red
            _civilianBeaconMat = new Material(unlit) { color = new Color(0.2f, 0.85f, 1.0f, 1f) }; // Cyan / Blue

            // Create civilian cross visual if not present
            if (torsoT != null && torsoT.Find("CivilianCross") == null)
            {
                GameObject cross = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cross.name = "CivilianCross";
                cross.transform.SetParent(torsoT, false);
                cross.transform.localPosition = new Vector3(0f, 0.05f, -0.54f);
                cross.transform.localScale = new Vector3(0.18f, 0.42f, 0.04f);
                Destroy(cross.GetComponent<Collider>());
                cross.GetComponent<Renderer>().material = new Material(unlit) { color = Color.white };

                GameObject crossH = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crossH.name = "CrossH";
                crossH.transform.SetParent(cross.transform, false);
                crossH.transform.localPosition = Vector3.zero;
                crossH.transform.localScale = new Vector3(2.3f, 0.40f, 1.1f);
                Destroy(crossH.GetComponent<Collider>());
                crossH.GetComponent<Renderer>().material = cross.GetComponent<Renderer>().material;

                _civilianCross = cross;
                _civilianCross.SetActive(false);
            }
        }

        public void PopUp(TargetClassification classification, float exposureDuration)
        {
            Classification = classification;
            _exposureDuration = exposureDuration;
            _spawnTime = Time.time;
            IsPoppedUp = true;

            ApplyVisualStyle(classification);

            StopAllCoroutines();
            StartCoroutine(RotateTowardsRoutine(_uprightRotation, 0.16f));
            _exposureCoroutine = StartCoroutine(ExposureCountdownRoutine());
        }

        public void DuckDown()
        {
            IsPoppedUp = false;
            StopAllCoroutines();
            StartCoroutine(RotateTowardsRoutine(_duckedRotation, 0.20f));
        }

        public void DuckDownImmediate()
        {
            IsPoppedUp = false;
            StopAllCoroutines();
            transform.localRotation = _duckedRotation;
        }

        private void ApplyVisualStyle(TargetClassification classification)
        {
            if (classification == TargetClassification.HostileThreat)
            {
                if (_torsoRenderer != null) _torsoRenderer.material = _hostileTorsoMat;
                if (_headRenderer != null) _headRenderer.material = _hostileTorsoMat;
                if (_beaconRenderer != null) _beaconRenderer.material = _hostileBeaconMat;
                if (_hostileRings != null) _hostileRings.SetActive(true);
                if (_civilianCross != null) _civilianCross.SetActive(false);
            }
            else
            {
                if (_torsoRenderer != null) _torsoRenderer.material = _civilianTorsoMat;
                if (_headRenderer != null) _headRenderer.material = _civilianTorsoMat;
                if (_beaconRenderer != null) _beaconRenderer.material = _civilianBeaconMat;
                if (_hostileRings != null) _hostileRings.SetActive(false);
                if (_civilianCross != null) _civilianCross.SetActive(true);
            }
        }

        private IEnumerator ExposureCountdownRoutine()
        {
            yield return new WaitForSeconds(_exposureDuration);

            // Timed out without being shot
            if (IsPoppedUp)
            {
                if (TacticalDrillGameManager.Instance != null &&
                    TacticalDrillGameManager.Instance.CurrentState == DrillState.ActiveDrill)
                {
                    TacticalDrillGameManager.Instance.RecordTargetExpired(this);
                }
                DuckDown();
            }
        }

        public void Damage(DamageData damageData)
        {
            if (!IsPoppedUp && TacticalDrillGameManager.Instance != null &&
                TacticalDrillGameManager.Instance.CurrentState == DrillState.ActiveDrill)
            {
                return; // Already ducked
            }

            float latencyMs = (Time.time - _spawnTime) * 1000f;
            bool isHeadshot = damageData.contactPoint.y > transform.position.y + 1.62f;

            if (TacticalDrillGameManager.Instance != null)
            {
                TacticalDrillGameManager.Instance.RecordTargetHit(this, isHeadshot, latencyMs);
            }

            // Hit animation
            StopAllCoroutines();
            StartCoroutine(HitImpulseAndDuckRoutine());
        }

        private IEnumerator HitImpulseAndDuckRoutine()
        {
            Quaternion orig = transform.localRotation;
            Quaternion tilt = orig * Quaternion.Euler(-14f, 0f, UnityEngine.Random.Range(-8f, 8f));

            float t = 0f;
            while (t < 0.08f)
            {
                transform.localRotation = Quaternion.Slerp(orig, tilt, t / 0.08f);
                t += Time.deltaTime;
                yield return null;
            }

            // In Active Drill, target ducks down after hit
            if (TacticalDrillGameManager.Instance != null &&
                TacticalDrillGameManager.Instance.CurrentState == DrillState.ActiveDrill)
            {
                IsPoppedUp = false;
                t = 0f;
                while (t < 0.16f)
                {
                    transform.localRotation = Quaternion.Slerp(tilt, _duckedRotation, t / 0.16f);
                    t += Time.deltaTime;
                    yield return null;
                }
                transform.localRotation = _duckedRotation;
            }
            else
            {
                // In Free Practice, spring back upright
                t = 0f;
                while (t < 0.18f)
                {
                    transform.localRotation = Quaternion.Slerp(tilt, _uprightRotation, t / 0.18f);
                    t += Time.deltaTime;
                    yield return null;
                }
                transform.localRotation = _uprightRotation;
            }
        }

        private IEnumerator RotateTowardsRoutine(Quaternion targetRot, float duration)
        {
            Quaternion startRot = transform.localRotation;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                transform.localRotation = Quaternion.Slerp(startRot, targetRot, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localRotation = targetRot;
        }
    }
}
