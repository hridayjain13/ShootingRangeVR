using System;
using System.Collections;
using Interfaces;
using Resources;
using Targets;
using UnityEngine;
using UnityEngine.XR;

namespace IVRAR.Group02.XR
{
    /// <summary>
    /// Advanced First-Person Desktop Controller for the VR Shooting Range.
    /// Features:
    /// - Smooth WASD locomotion with sprint (LeftShift) and head-level clamping
    /// - Smooth mouse look with pitch clamping
    /// - Exquisite 100% visible First-Person Tactical Glock 17 viewmodel
    /// - True Aim-Down-Sights (ADS) centering with FOV zoom (RMB) and aligned glowing fiber-optic sights
    /// - Semi-automatic shooting (LMB) with dynamic slide blowback recoil, muzzle flash, and brass shell ejection
    /// - Tactical Laser Sight (toggle with 'L') with real-time beam and impact dot
    /// - Weapon-mounted Tactical Flashlight (toggle with 'F')
    /// - Realistic gunshot, reload, slide, and empty chamber audio
    /// - Advanced Headshot (+250) vs Body Hit (+100) detection and combo multiplier system (x1 to x5)
    /// - Esports-grade Tactical HUD with crosshair, ammo counter, combo streak, and floating combat text
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class DesktopFallbackController : MonoBehaviour
    {
        [Header("Camera & Transforms")]
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private Transform _playerOrigin;

        [Header("Locomotion Tuning")]
        [SerializeField] private float _moveSpeed = 4.0f;
        [SerializeField] private float _sprintSpeed = 6.8f;
        [SerializeField] private float _mouseSensitivity = 2.4f;
        [SerializeField] private bool _invertY = false;

        [Header("Weapon Sights & Offsets")]
        [SerializeField] private float _hipFov = 60f;
        [SerializeField] private float _aimFov = 35f;
        [SerializeField] private Vector3 _hipPosition = new Vector3(0.18f, -0.16f, 0.38f);
        [SerializeField] private Vector3 _aimPosition = new Vector3(0.0f, -0.118f, 0.33f);

        [Header("Audio Clips")]
        [SerializeField] private AudioClip _bgMusicClip;
        [SerializeField] private AudioClip _shotAudioClip;
        [SerializeField] private AudioClip _magOutClip;
        [SerializeField] private AudioClip _magInClip;
        [SerializeField] private AudioClip _slideForwardClip;

        [Header("System Links")]
        [SerializeField] private SimulatedResponseMechanism _responseMechanism;
        [SerializeField] private XRRayInteractorPointer _rayPointer;

        [Header("Mode Configuration")]
        [SerializeField] private bool _autoEnableIfNoHmd = true;
        [SerializeField] private KeyCode _toggleModeKey = KeyCode.F1;
        [SerializeField] private KeyCode _altToggleKey = KeyCode.Tab;

        public bool IsDesktopModeActive { get; private set; }

        // Camera Look
        private float _yaw;
        private float _pitch;
        private bool _cursorLocked = true;

        // Weapon State
        private bool _isAiming = false;
        private int _ammoCurrent = 17;
        private const int AMMO_MAX = 17;
        private bool _isReloading = false;
        private float _lastShootTime = 0f;
        private const float SHOOT_COOLDOWN = 0.14f;
        private Vector3 _recoilOffset = Vector3.zero;
        private Quaternion _recoilRotation = Quaternion.identity;

        // Viewmodel References
        private Transform _gunTransform;
        private Transform _slideTransform;
        private Light _muzzleFlashLight;
        private GameObject _muzzleFlashQuad;
        private float _flashTimer = 0f;
        private float _slideRecoilTimer = 0f;

        // Tactical Attachments
        private bool _laserActive = true;
        private bool _flashlightActive = false;
        private LineRenderer _laserLine;
        private GameObject _laserDot;
        private Light _tacticalFlashlight;

        // Audio Sources
        private AudioSource _musicSource;
        private AudioSource _weaponAudioSource;

        // Scoring & Advanced Combat
        private int _score = 0;
        private int _totalHits = 0;
        private int _headshots = 0;
        private int _shotsFired = 0;
        private int _comboMultiplier = 1;
        private float _comboTimer = 0f;
        private const float COMBO_DURATION = 3.5f;

        // Floating Text & Feedback
        private float _hitMarkerTimer = 0f;
        private string _lastHitText = "";
        private Color _lastHitColor = Color.green;
        private float _hitTextTimer = 0f;

        // Weapon Sway & Walking Bobbing
        private float _bobTimer = 0f;
        private bool _showHelpOverlay = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureController()
        {
            Camera cam = Camera.main ?? FindObjectOfType<Camera>();
            if (cam == null) return;

            if (cam.GetComponent<AudioListener>() == null)
            {
                cam.gameObject.AddComponent<AudioListener>();
            }

            if (FindObjectOfType<DesktopFallbackController>() == null)
            {
                cam.gameObject.AddComponent<DesktopFallbackController>();
                Debug.Log("[DesktopFallbackController] Automatically initialized First-Person Desktop Mode on Camera.");
            }
        }

        private void Awake()
        {
            EnsureCameraAndRig();
            LoadAudioClips();
            SetupAudioSources();
        }

        private void Start()
        {
            EnsureCameraAndRig();

            if (_targetCamera != null)
            {
                _targetCamera.enabled = true;
                _targetCamera.fieldOfView = _hipFov;
                _yaw = _targetCamera.transform.eulerAngles.y;
                _pitch = _targetCamera.transform.eulerAngles.x;

                if (_targetCamera.GetComponent<AudioListener>() == null)
                {
                    _targetCamera.gameObject.AddComponent<AudioListener>();
                }
            }

            BuildTacticalGlockViewmodel();

            if (_autoEnableIfNoHmd)
            {
                bool isHmdActive = XRSettings.isDeviceActive;
                SetDesktopMode(!isHmdActive);
            }
        }

        private void EnsureCameraAndRig()
        {
            if (_targetCamera == null) _targetCamera = Camera.main;
            if (_targetCamera == null) _targetCamera = FindObjectOfType<Camera>();

            if (_targetCamera != null)
            {
                _playerOrigin = _targetCamera.transform;
            }
            else
            {
                if (_playerOrigin == null) _playerOrigin = transform;
            }

            if (_responseMechanism == null) _responseMechanism = FindObjectOfType<SimulatedResponseMechanism>();
            if (_rayPointer == null) _rayPointer = FindObjectOfType<XRRayInteractorPointer>();
        }

        private void LoadAudioClips()
        {
            if (_bgMusicClip == null) _bgMusicClip = UnityEngine.Resources.Load<AudioClip>("Audio/Background");
            if (_shotAudioClip == null) _shotAudioClip = UnityEngine.Resources.Load<AudioClip>("Audio/glock_shot");
            if (_magOutClip == null) _magOutClip = UnityEngine.Resources.Load<AudioClip>("Audio/glock_mag_out");
            if (_magInClip == null) _magInClip = UnityEngine.Resources.Load<AudioClip>("Audio/glock_mag_in");
            if (_slideForwardClip == null) _slideForwardClip = UnityEngine.Resources.Load<AudioClip>("Audio/glock_slide_forward");
        }

        private void SetupAudioSources()
        {
            if (_musicSource == null)
            {
                GameObject musicObj = new GameObject("Desktop_BGM_Source");
                musicObj.transform.SetParent(transform);
                _musicSource = musicObj.AddComponent<AudioSource>();
                _musicSource.playOnAwake = false;
                _musicSource.loop = true;
                _musicSource.volume = 0.35f;
                _musicSource.spatialBlend = 0f;
            }

            if (_bgMusicClip != null && !_musicSource.isPlaying)
            {
                _musicSource.clip = _bgMusicClip;
                _musicSource.Play();
            }

            if (_weaponAudioSource == null)
            {
                GameObject sfxObj = new GameObject("Desktop_Weapon_SFX");
                sfxObj.transform.SetParent(transform);
                _weaponAudioSource = sfxObj.AddComponent<AudioSource>();
                _weaponAudioSource.playOnAwake = false;
                _weaponAudioSource.loop = false;
                _weaponAudioSource.volume = 1.0f;
                _weaponAudioSource.spatialBlend = 0f;
            }
        }

        private void BuildTacticalGlockViewmodel()
        {
            if (_targetCamera == null) return;
            if (_gunTransform != null) return;

            // Root viewmodel container attached to camera
            GameObject gunRoot = new GameObject("Tactical_Glock17_Viewmodel");
            gunRoot.transform.SetParent(_targetCamera.transform, false);
            gunRoot.transform.localPosition = _hipPosition;
            gunRoot.transform.localRotation = Quaternion.Euler(1.5f, -2.5f, 0f);
            _gunTransform = gunRoot.transform;

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");

            Material blackSteelMat = new Material(litShader) { color = new Color(0.12f, 0.13f, 0.14f, 1f) };
            Material darkPolymerMat = new Material(litShader) { color = new Color(0.08f, 0.09f, 0.10f, 1f) };
            Material silverBarrelMat = new Material(litShader) { color = new Color(0.65f, 0.68f, 0.72f, 1f) };
            Material tritiumGreenMat = new Material(unlitShader) { color = new Color(0.2f, 1.0f, 0.35f, 1f) };
            Material sightWhiteMat = new Material(unlitShader) { color = new Color(0.95f, 0.95f, 0.95f, 1f) };

            // 1. Lower Receiver Frame
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Frame";
            frame.transform.SetParent(_gunTransform, false);
            frame.transform.localPosition = new Vector3(0f, 0.005f, 0.05f);
            frame.transform.localScale = new Vector3(0.030f, 0.024f, 0.18f);
            Destroy(frame.GetComponent<Collider>());
            frame.GetComponent<Renderer>().material = darkPolymerMat;

            // 2. Ergonomic Grip with angle
            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grip.name = "Grip";
            grip.transform.SetParent(_gunTransform, false);
            grip.transform.localPosition = new Vector3(0f, -0.062f, 0.005f);
            grip.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            grip.transform.localScale = new Vector3(0.029f, 0.125f, 0.048f);
            Destroy(grip.GetComponent<Collider>());
            grip.GetComponent<Renderer>().material = darkPolymerMat;

            // 3. Magazine Baseplate
            GameObject magBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            magBase.name = "MagBase";
            magBase.transform.SetParent(grip.transform, false);
            magBase.transform.localPosition = new Vector3(0f, -0.52f, 0.02f);
            magBase.transform.localScale = new Vector3(1.1f, 0.12f, 1.15f);
            Destroy(magBase.GetComponent<Collider>());
            magBase.GetComponent<Renderer>().material = blackSteelMat;

            // 4. Trigger Guard & Trigger
            GameObject guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            guard.name = "TriggerGuard";
            guard.transform.SetParent(_gunTransform, false);
            guard.transform.localPosition = new Vector3(0f, -0.026f, 0.065f);
            guard.transform.localScale = new Vector3(0.016f, 0.042f, 0.046f);
            Destroy(guard.GetComponent<Collider>());
            guard.GetComponent<Renderer>().material = darkPolymerMat;

            GameObject trigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trigger.name = "Trigger";
            trigger.transform.SetParent(_gunTransform, false);
            trigger.transform.localPosition = new Vector3(0f, -0.022f, 0.058f);
            trigger.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            trigger.transform.localScale = new Vector3(0.006f, 0.024f, 0.010f);
            Destroy(trigger.GetComponent<Collider>());
            trigger.GetComponent<Renderer>().material = blackSteelMat;

            // 5. Polished Silver Barrel (stationary inside frame)
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Barrel";
            barrel.transform.SetParent(_gunTransform, false);
            barrel.transform.localPosition = new Vector3(0f, 0.028f, 0.09f);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            barrel.transform.localScale = new Vector3(0.014f, 0.09f, 0.014f);
            Destroy(barrel.GetComponent<Collider>());
            barrel.GetComponent<Renderer>().material = silverBarrelMat;

            // 6. Slide Container (Animates backward on recoil)
            GameObject slideRoot = new GameObject("SlideRoot");
            slideRoot.transform.SetParent(_gunTransform, false);
            slideRoot.transform.localPosition = Vector3.zero;
            _slideTransform = slideRoot.transform;

            // 6a. Main Steel Slide
            GameObject slide = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slide.name = "SlideBody";
            slide.transform.SetParent(_slideTransform, false);
            slide.transform.localPosition = new Vector3(0f, 0.028f, 0.055f);
            slide.transform.localScale = new Vector3(0.032f, 0.034f, 0.205f);
            Destroy(slide.GetComponent<Collider>());
            slide.GetComponent<Renderer>().material = blackSteelMat;

            // 6b. Ejection Port Cutout Accent
            GameObject ejectPort = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ejectPort.name = "EjectionPort";
            ejectPort.transform.SetParent(_slideTransform, false);
            ejectPort.transform.localPosition = new Vector3(0.012f, 0.032f, 0.038f);
            ejectPort.transform.localScale = new Vector3(0.012f, 0.020f, 0.038f);
            Destroy(ejectPort.GetComponent<Collider>());
            ejectPort.GetComponent<Renderer>().material = silverBarrelMat;

            // 6c. Front Sight Post with Glowing Tritium Dot
            GameObject frontPost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontPost.name = "FrontSightPost";
            frontPost.transform.SetParent(_slideTransform, false);
            frontPost.transform.localPosition = new Vector3(0f, 0.048f, 0.150f);
            frontPost.transform.localScale = new Vector3(0.005f, 0.008f, 0.006f);
            Destroy(frontPost.GetComponent<Collider>());
            frontPost.GetComponent<Renderer>().material = blackSteelMat;

            GameObject frontDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            frontDot.name = "TritiumGreenDot";
            frontDot.transform.SetParent(frontPost.transform, false);
            frontDot.transform.localPosition = new Vector3(0f, 0.2f, -0.6f);
            frontDot.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            Destroy(frontDot.GetComponent<Collider>());
            frontDot.GetComponent<Renderer>().material = tritiumGreenMat;

            // 6d. Rear Sight with Dual White Alignment Dots
            GameObject rearBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rearBase.name = "RearSightBase";
            rearBase.transform.SetParent(_slideTransform, false);
            rearBase.transform.localPosition = new Vector3(0f, 0.048f, -0.042f);
            rearBase.transform.localScale = new Vector3(0.020f, 0.008f, 0.008f);
            Destroy(rearBase.GetComponent<Collider>());
            rearBase.GetComponent<Renderer>().material = blackSteelMat;

            GameObject rearDotLeft = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rearDotLeft.name = "RearDotL";
            rearDotLeft.transform.SetParent(rearBase.transform, false);
            rearDotLeft.transform.localPosition = new Vector3(-0.35f, 0.2f, -0.6f);
            rearDotLeft.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            Destroy(rearDotLeft.GetComponent<Collider>());
            rearDotLeft.GetComponent<Renderer>().material = sightWhiteMat;

            GameObject rearDotRight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rearDotRight.name = "RearDotR";
            rearDotRight.transform.SetParent(rearBase.transform, false);
            rearDotRight.transform.localPosition = new Vector3(0.35f, 0.2f, -0.6f);
            rearDotRight.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            Destroy(rearDotRight.GetComponent<Collider>());
            rearDotRight.GetComponent<Renderer>().material = sightWhiteMat;

            // 7. Tactical Rail Module (Laser + Light)
            GameObject railUnit = GameObject.CreatePrimitive(PrimitiveType.Cube);
            railUnit.name = "UnderbarrelLaserLight";
            railUnit.transform.SetParent(_gunTransform, false);
            railUnit.transform.localPosition = new Vector3(0f, -0.010f, 0.115f);
            railUnit.transform.localScale = new Vector3(0.026f, 0.022f, 0.065f);
            Destroy(railUnit.GetComponent<Collider>());
            railUnit.GetComponent<Renderer>().material = blackSteelMat;

            // Tactical Flashlight Component
            GameObject lightObj = new GameObject("TacticalFlashlight");
            lightObj.transform.SetParent(railUnit.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0f, 0.6f);
            _tacticalFlashlight = lightObj.AddComponent<Light>();
            _tacticalFlashlight.type = LightType.Spot;
            _tacticalFlashlight.range = 35f;
            _tacticalFlashlight.spotAngle = 38f;
            _tacticalFlashlight.intensity = 2.8f;
            _tacticalFlashlight.color = new Color(0.96f, 0.98f, 1.0f);
            _tacticalFlashlight.enabled = _flashlightActive;

            // Tactical Laser Beam & Dot
            _laserLine = railUnit.AddComponent<LineRenderer>();
            _laserLine.useWorldSpace = true;
            _laserLine.startWidth = 0.003f;
            _laserLine.endWidth = 0.003f;
            _laserLine.material = new Material(unlitShader) { color = Color.red };
            _laserLine.startColor = new Color(1f, 0.1f, 0.1f, 0.8f);
            _laserLine.endColor = new Color(1f, 0.1f, 0.1f, 0.8f);
            _laserLine.positionCount = 2;
            _laserLine.enabled = _laserActive;

            _laserDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _laserDot.name = "LaserImpactDot";
            _laserDot.transform.localScale = Vector3.one * 0.035f;
            Destroy(_laserDot.GetComponent<Collider>());
            _laserDot.GetComponent<Renderer>().material = new Material(unlitShader) { color = Color.red };
            _laserDot.SetActive(_laserActive);

            // 8. Muzzle Flash Point Light & Particle Quad
            GameObject flashLightObj = new GameObject("MuzzleFlashLight");
            flashLightObj.transform.SetParent(_gunTransform, false);
            flashLightObj.transform.localPosition = new Vector3(0f, 0.028f, 0.22f);
            _muzzleFlashLight = flashLightObj.AddComponent<Light>();
            _muzzleFlashLight.type = LightType.Point;
            _muzzleFlashLight.range = 6f;
            _muzzleFlashLight.intensity = 4.5f;
            _muzzleFlashLight.color = new Color(1.0f, 0.85f, 0.4f);
            _muzzleFlashLight.enabled = false;

            _muzzleFlashQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _muzzleFlashQuad.name = "MuzzleFlashQuad";
            _muzzleFlashQuad.transform.SetParent(_gunTransform, false);
            _muzzleFlashQuad.transform.localPosition = new Vector3(0f, 0.028f, 0.24f);
            _muzzleFlashQuad.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
            Destroy(_muzzleFlashQuad.GetComponent<Collider>());
            _muzzleFlashQuad.GetComponent<Renderer>().material = new Material(unlitShader) { color = new Color(1f, 0.92f, 0.55f, 1f) };
            _muzzleFlashQuad.SetActive(false);

            Debug.Log("[DesktopFallbackController] First-Person Tactical Glock 17 Viewmodel built successfully.");
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleModeKey))
            {
                _showHelpOverlay = !_showHelpOverlay;
            }

            if (Input.GetKeyDown(_altToggleKey))
            {
                SetDesktopMode(!IsDesktopModeActive);
            }

            if (!IsDesktopModeActive) return;

            HandleCursorLock();
            HandleMouseLook();
            HandleLocomotion();
            HandleAimingAndZoom();
            HandleShootingAndReload();
            HandleTacticalAttachments();
            UpdateWeaponSwayAndRecoil();
            UpdateLaserSight();
            UpdateTimers();
        }

        public void SetDesktopMode(bool active)
        {
            IsDesktopModeActive = active;
            _cursorLocked = active;
            ApplyCursorLock(_cursorLocked);

            if (_targetCamera != null)
            {
                _targetCamera.enabled = true;
            }

            if (_gunTransform != null)
            {
                _gunTransform.gameObject.SetActive(active);
            }

            if (_laserLine != null) _laserLine.enabled = active && _laserActive;
            if (_laserDot != null) _laserDot.SetActive(active && _laserActive);

            if (active && _musicSource != null && !_musicSource.isPlaying && _bgMusicClip != null)
            {
                _musicSource.Play();
            }

            Debug.Log("[DesktopFallbackController] Desktop Mode: " + (IsDesktopModeActive ? "ENABLED" : "DISABLED"));
        }

        private void HandleCursorLock()
        {
            if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt) || Input.GetKeyDown(KeyCode.Escape))
            {
                _cursorLocked = !_cursorLocked;
                ApplyCursorLock(_cursorLocked);
            }

            if (!_cursorLocked && Input.GetMouseButtonDown(0))
            {
                _cursorLocked = true;
                ApplyCursorLock(true);
            }
        }

        private void ApplyCursorLock(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void HandleMouseLook()
        {
            if (!_cursorLocked || _targetCamera == null) return;

            float currentSens = _isAiming ? _mouseSensitivity * 0.50f : _mouseSensitivity;
            float mouseX = Input.GetAxis("Mouse X") * currentSens;
            float mouseY = Input.GetAxis("Mouse Y") * currentSens * (_invertY ? 1f : -1f);

            _yaw += mouseX;
            _pitch = Mathf.Clamp(_pitch + mouseY, -82f, 82f);

            _targetCamera.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void HandleLocomotion()
        {
            if (_playerOrigin == null || _targetCamera == null) return;

            float horizontal = Input.GetAxis("Horizontal"); // A/D
            float vertical = Input.GetAxis("Vertical");     // W/S

            if (Mathf.Abs(horizontal) > 0.02f || Mathf.Abs(vertical) > 0.02f)
            {
                Vector3 forward = _targetCamera.transform.forward;
                forward.y = 0f;
                forward.Normalize();

                Vector3 right = _targetCamera.transform.right;
                right.y = 0f;
                right.Normalize();

                float speed = Input.GetKey(KeyCode.LeftShift) ? _sprintSpeed : _moveSpeed;
                Vector3 moveDir = (forward * vertical + right * horizontal).normalized;

                Vector3 targetPos = _playerOrigin.position + moveDir * (speed * Time.unscaledDeltaTime);
                targetPos.y = 1.6f; // Lock player standing eye height
                _playerOrigin.position = targetPos;

                _bobTimer += Time.deltaTime * (speed * 1.8f);
            }
            else
            {
                _bobTimer = Mathf.MoveTowards(_bobTimer, 0f, Time.deltaTime * 3.5f);
            }
        }

        private void HandleAimingAndZoom()
        {
            if (_targetCamera == null) return;

            _isAiming = Input.GetMouseButton(1);

            float targetFov = _isAiming ? _aimFov : _hipFov;
            _targetCamera.fieldOfView = Mathf.Lerp(_targetCamera.fieldOfView, targetFov, Time.deltaTime * 14f);
        }

        private void HandleShootingAndReload()
        {
            if (Input.GetKeyDown(KeyCode.R) && !_isReloading && _ammoCurrent < AMMO_MAX)
            {
                StartCoroutine(ReloadCoroutine());
            }

            if (Input.GetMouseButtonDown(0) && _cursorLocked)
            {
                if (_isReloading) return;

                if (_ammoCurrent <= 0)
                {
                    StartCoroutine(ReloadCoroutine());
                    return;
                }

                if (Time.time - _lastShootTime >= SHOOT_COOLDOWN)
                {
                    Shoot();
                }
            }
        }

        private void Shoot()
        {
            _lastShootTime = Time.time;
            _ammoCurrent--;
            _shotsFired++;

            // 1. Play Shot Sound
            if (_shotAudioClip != null && _weaponAudioSource != null)
            {
                _weaponAudioSource.pitch = UnityEngine.Random.Range(0.96f, 1.04f);
                _weaponAudioSource.PlayOneShot(_shotAudioClip, 0.95f);
            }

            // 2. Muzzle Flash
            if (_muzzleFlashLight != null) _muzzleFlashLight.enabled = true;
            if (_muzzleFlashQuad != null)
            {
                _muzzleFlashQuad.transform.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
                _muzzleFlashQuad.SetActive(true);
            }
            _flashTimer = 0.05f;

            // 3. Slide Recoil Blowback
            _slideRecoilTimer = 0.07f;
            _recoilOffset = new Vector3(0f, 0.024f, -0.065f);
            _recoilRotation = Quaternion.Euler(-4.5f, UnityEngine.Random.Range(-0.6f, 0.6f), 0f);

            // 4. Eject Brass Shell Casing
            EjectShellCasing();

            // 5. Raycast Hit Detection
            Ray ray = new Ray(_targetCamera.transform.position, _targetCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 200f))
            {
                ProcessHit(hit);
            }

            if (_responseMechanism != null) _responseMechanism.ProcessResponse(null);
            else if (_rayPointer != null) _rayPointer.TriggerClick();
        }

        private void EjectShellCasing()
        {
            if (_gunTransform == null) return;

            GameObject casing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            casing.name = "SpentBrass_Casing";
            casing.transform.position = _gunTransform.position + _gunTransform.right * 0.04f + _gunTransform.up * 0.02f;
            casing.transform.localScale = new Vector3(0.009f, 0.015f, 0.009f);
            casing.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360));
            Destroy(casing.GetComponent<Collider>());

            Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (lit != null)
            {
                Material brass = new Material(lit) { color = new Color(0.85f, 0.70f, 0.25f, 1f) };
                casing.GetComponent<Renderer>().material = brass;
            }

            Rigidbody rb = casing.AddComponent<Rigidbody>();
            rb.mass = 0.01f;
            Vector3 ejectDir = (_gunTransform.right * 2.5f + _gunTransform.up * 1.8f + _gunTransform.forward * 0.2f);
            rb.linearVelocity = ejectDir + UnityEngine.Random.insideUnitSphere * 0.5f;
            rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 25f;

            Destroy(casing, 1.2f);
        }

        private void ProcessHit(RaycastHit hit)
        {
            var hitObj = hit.transform.gameObject;
            string objName = hitObj.name.ToLower();

            bool isHeadshot = objName.Contains("head") || hitObj.CompareTag("Player") || hit.point.y > 1.65f;
            bool isEnemy = objName.Contains("target") || objName.Contains("enemy") || objName.Contains("hostile") || objName.Contains("silhouette");

            var damageData = new DamageData
            {
                contactPoint = hit.point,
                normalPoint = Quaternion.LookRotation(hit.normal),
                force = _targetCamera.transform.forward * 45f
            };

            var damageable = hitObj.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.Damage(damageData);
            }

            var humanTarget = hitObj.GetComponentInParent<HumanTarget>();
            if (humanTarget != null)
            {
                humanTarget.Damage(damageData);
            }

            if (isEnemy || damageable != null || humanTarget != null)
            {
                _totalHits++;
                _hitMarkerTimer = 0.28f;

                // Combo Multiplier
                if (_comboTimer > 0f)
                {
                    _comboMultiplier = Mathf.Min(_comboMultiplier + 1, 5);
                }
                else
                {
                    _comboMultiplier = 1;
                }
                _comboTimer = COMBO_DURATION;

                int baseScore = isHeadshot ? 250 : 100;
                int awardedScore = baseScore * _comboMultiplier;
                _score += awardedScore;

                if (isHeadshot)
                {
                    _headshots++;
                    _lastHitText = $"★ HEADSHOT! +{awardedScore} PTS (x{_comboMultiplier}) ★";
                    _lastHitColor = new Color(1f, 0.85f, 0.1f);
                }
                else
                {
                    _lastHitText = $"TARGET HIT! +{awardedScore} PTS (x{_comboMultiplier})";
                    _lastHitColor = new Color(0.2f, 1f, 0.4f);
                }
                _hitTextTimer = 1.2f;
            }

            CreateHitSparks(hit, isEnemy);
        }

        private void CreateHitSparks(RaycastHit hit, bool isEnemy)
        {
            GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spark.name = "BulletImpact";
            spark.transform.position = hit.point + hit.normal * 0.02f;
            spark.transform.localScale = Vector3.one * 0.06f;
            Destroy(spark.GetComponent<Collider>());

            Shader s = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (s != null)
            {
                Material m = new Material(s);
                m.color = isEnemy ? Color.red : Color.yellow;
                spark.GetComponent<Renderer>().material = m;
            }

            Destroy(spark, 0.35f);
        }

        private IEnumerator ReloadCoroutine()
        {
            _isReloading = true;

            if (_magOutClip != null && _weaponAudioSource != null)
            {
                _weaponAudioSource.PlayOneShot(_magOutClip, 0.85f);
            }
            yield return new WaitForSeconds(0.40f);

            if (_magInClip != null && _weaponAudioSource != null)
            {
                _weaponAudioSource.PlayOneShot(_magInClip, 0.85f);
            }
            yield return new WaitForSeconds(0.40f);

            if (_slideForwardClip != null && _weaponAudioSource != null)
            {
                _weaponAudioSource.PlayOneShot(_slideForwardClip, 0.90f);
            }
            yield return new WaitForSeconds(0.25f);

            _ammoCurrent = AMMO_MAX;
            _isReloading = false;
        }

        private void HandleTacticalAttachments()
        {
            // Toggle Laser with 'L'
            if (Input.GetKeyDown(KeyCode.L))
            {
                _laserActive = !_laserActive;
                if (_laserLine != null) _laserLine.enabled = _laserActive;
                if (_laserDot != null) _laserDot.SetActive(_laserActive);
            }

            // Toggle Flashlight with 'F'
            if (Input.GetKeyDown(KeyCode.F))
            {
                _flashlightActive = !_flashlightActive;
                if (_tacticalFlashlight != null) _tacticalFlashlight.enabled = _flashlightActive;
            }
        }

        private void UpdateLaserSight()
        {
            if (!_laserActive || _laserLine == null || _targetCamera == null) return;

            Vector3 startPos = _gunTransform != null ? _gunTransform.position + _gunTransform.forward * 0.12f : _targetCamera.transform.position;
            Ray ray = new Ray(_targetCamera.transform.position, _targetCamera.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, 150f))
            {
                _laserLine.SetPosition(0, startPos);
                _laserLine.SetPosition(1, hit.point);

                if (_laserDot != null)
                {
                    _laserDot.SetActive(true);
                    _laserDot.transform.position = hit.point + hit.normal * 0.015f;
                }
            }
            else
            {
                Vector3 endPos = startPos + _targetCamera.transform.forward * 100f;
                _laserLine.SetPosition(0, startPos);
                _laserLine.SetPosition(1, endPos);

                if (_laserDot != null) _laserDot.SetActive(false);
            }
        }

        private void UpdateWeaponSwayAndRecoil()
        {
            if (_gunTransform == null) return;

            // Recoil recovery
            _recoilOffset = Vector3.Lerp(_recoilOffset, Vector3.zero, Time.deltaTime * 18f);
            _recoilRotation = Quaternion.Slerp(_recoilRotation, Quaternion.identity, Time.deltaTime * 18f);

            // Slide Blowback Animation
            if (_slideTransform != null)
            {
                if (_slideRecoilTimer > 0f)
                {
                    _slideRecoilTimer -= Time.deltaTime;
                    _slideTransform.localPosition = new Vector3(0f, 0f, -0.045f); // Slide kicked back
                }
                else
                {
                    _slideTransform.localPosition = Vector3.Lerp(_slideTransform.localPosition, Vector3.zero, Time.deltaTime * 24f);
                }
            }

            // Weapon Bobbing
            float bobX = Mathf.Sin(_bobTimer) * 0.007f;
            float bobY = Mathf.Abs(Mathf.Cos(_bobTimer)) * 0.007f;

            Vector3 basePos = _isAiming ? _aimPosition : _hipPosition;
            Quaternion baseRot = _isAiming ? Quaternion.identity : Quaternion.Euler(1.5f, -2.5f, 0f);

            Vector3 targetPos = basePos + new Vector3(bobX, bobY, 0f) + _recoilOffset;
            Quaternion targetRot = baseRot * _recoilRotation;

            _gunTransform.localPosition = Vector3.Lerp(_gunTransform.localPosition, targetPos, Time.deltaTime * 16f);
            _gunTransform.localRotation = Quaternion.Slerp(_gunTransform.localRotation, targetRot, Time.deltaTime * 16f);
        }

        private void UpdateTimers()
        {
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                if (_flashTimer <= 0f)
                {
                    if (_muzzleFlashLight != null) _muzzleFlashLight.enabled = false;
                    if (_muzzleFlashQuad != null) _muzzleFlashQuad.SetActive(false);
                }
            }

            if (_hitMarkerTimer > 0f) _hitMarkerTimer -= Time.deltaTime;
            if (_hitTextTimer > 0f) _hitTextTimer -= Time.deltaTime;

            if (_comboTimer > 0f)
            {
                _comboTimer -= Time.deltaTime;
                if (_comboTimer <= 0f)
                {
                    _comboMultiplier = 1;
                }
            }
        }

        private void OnGUI()
        {
            if (!IsDesktopModeActive) return;

            DrawCrosshairs();
            DrawTacticalHUD();
        }

        private void DrawCrosshairs()
        {
            float cx = Screen.width / 2f;
            float cy = Screen.height / 2f;

            if (_isAiming)
            {
                // In ADS, glowing tritium iron sights line up with center; draw subtle precision point
                GUI.color = new Color(0.2f, 1f, 0.4f, 0.85f);
                GUI.DrawTexture(new Rect(cx - 2f, cy - 2f, 4f, 4f), Texture2D.whiteTexture);
            }
            else
            {
                // Tactical Hipfire Reticle
                GUI.color = new Color(1f, 1f, 1f, 0.70f);
                float gap = 8f;
                float len = 10f;
                float thick = 2f;

                GUI.DrawTexture(new Rect(cx - gap - len, cy - thick / 2f, len, thick), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(cx + gap, cy - thick / 2f, len, thick), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(cx - thick / 2f, cy - gap - len, thick, len), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(cx - thick / 2f, cy + gap, thick, len), Texture2D.whiteTexture);
            }

            // Hit Marker (Red '✕')
            if (_hitMarkerTimer > 0f)
            {
                GUI.color = new Color(1f, 0.2f, 0.2f, Mathf.Clamp01(_hitMarkerTimer / 0.28f));
                GUIStyle xStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 24,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                GUI.Label(new Rect(cx - 25, cy - 25, 50, 50), "✕", xStyle);
            }

            // Floating Combat Text
            if (_hitTextTimer > 0f)
            {
                GUI.color = new Color(_lastHitColor.r, _lastHitColor.g, _lastHitColor.b, Mathf.Clamp01(_hitTextTimer));
                GUIStyle hitStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 17,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                GUI.Label(new Rect(cx - 200, cy - 75, 400, 35), _lastHitText, hitStyle);
            }
        }

        private void DrawTacticalHUD()
        {
            // Bottom-Right Weapon Status
            GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.88f);
            GUI.Box(new Rect(Screen.width - 250, Screen.height - 110, 235, 95), GUIContent.none);

            GUI.color = Color.white;
            GUIStyle wStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(Screen.width - 235, Screen.height - 105, 210, 22), "GLOCK 17 (9x19mm TACTICAL)", wStyle);

            GUIStyle ammoStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
            ammoStyle.normal.textColor = _isReloading ? Color.yellow : (_ammoCurrent <= 3 ? Color.red : Color.cyan);
            string ammoText = _isReloading ? "RELOADING..." : $"{_ammoCurrent} / {AMMO_MAX}";
            GUI.Label(new Rect(Screen.width - 235, Screen.height - 82, 210, 32), ammoText, ammoStyle);

            GUIStyle subStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, normal = { textColor = new Color(0.7f, 0.75f, 0.8f) } };
            string attachText = $"LASER [L]: {(_laserActive ? "ON" : "OFF")}  |  LIGHT [F]: {(_flashlightActive ? "ON" : "OFF")}";
            GUI.Label(new Rect(Screen.width - 235, Screen.height - 42, 210, 20), attachText, subStyle);

            // Top-Left Esports Tactical Stats & Streak
            GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.88f);
            GUI.Box(new Rect(15, 15, 230, 95), GUIContent.none);

            GUI.color = Color.white;
            GUIStyle scoreStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(25, 20, 210, 24), $"SCORE: {_score}", scoreStyle);

            float acc = _shotsFired > 0 ? ((float)_totalHits / _shotsFired * 100f) : 0f;
            GUIStyle statStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, normal = { textColor = Color.yellow } };
            GUI.Label(new Rect(25, 45, 210, 20), $"HITS: {_totalHits} (HEADSHOTS: {_headshots})", statStyle);

            GUIStyle comboStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
            comboStyle.normal.textColor = _comboMultiplier > 1 ? new Color(1f, 0.4f, 0.1f) : Color.gray;
            string comboText = _comboMultiplier > 1 ? $"COMBO STREAK: x{_comboMultiplier} ({(int)_comboTimer}s)" : $"ACCURACY: {acc:F0}%";
            GUI.Label(new Rect(25, 68, 210, 22), comboText, comboStyle);

            // Keybindings & Help Overlay (Toggle with F1)
            if (_showHelpOverlay)
            {
                GUI.color = new Color(0.05f, 0.07f, 0.10f, 0.90f);
                GUI.Box(new Rect(Screen.width - 340, 15, 325, 150), GUIContent.none);

                GUI.color = Color.cyan;
                GUIStyle guideStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold };
                string guide =
                    "<b>TACTICAL FPS CONTROLS (F1 to toggle)</b>\n" +
                    "• <b>WASD</b>: Move | <b>Left Shift</b>: Sprint\n" +
                    "• <b>Mouse</b>: Look | <b>Alt / Esc</b>: Unlock Cursor\n" +
                    "• <b>Right Mouse (Hold)</b>: Aim Down Sights (ADS) & Zoom\n" +
                    "• <b>Left Mouse</b>: Fire Glock 17\n" +
                    "• <b>R</b>: Reload | <b>L</b>: Toggle Laser Sight\n" +
                    "• <b>F</b>: Toggle Flashlight | <b>Tab</b>: Toggle Desktop/VR";
                GUI.Label(new Rect(Screen.width - 330, 20, 310, 140), guide, guideStyle);
            }
        }

        private void OnDisable()
        {
            ApplyCursorLock(false);
            if (_musicSource != null) _musicSource.Stop();
            if (_laserLine != null) _laserLine.enabled = false;
            if (_laserDot != null) _laserDot.SetActive(false);
        }
    }
}
