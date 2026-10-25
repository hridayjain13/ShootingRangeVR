using System;
using System.Collections;
using System.Collections.Generic;
using Interfaces;
using Resources;
using Targets;
using UnityEngine;
using UnityEngine.XR;

namespace IVRAR.Group02.XR
{
    public enum WeaponType
    {
        Glock17 = 0,
        AssaultRifleM4A1 = 1,
        CombatShotgun = 2
    }

    /// <summary>
    /// Advanced First-Person Desktop & VR Hybrid Controller for the Shooting Range.
    /// Features:
    /// - Solid Physical Collision: Swept-capsule collision preventing player passing through solid objects, hurdles, benches, and walls
    /// - Multi-Weapon Arsenal:
    ///     [1] Glock 17 Tactical Pistol (17 rounds, semi-auto, laser/light, ADS fiber-optic sights)
    ///     [2] M4A1 CQB Assault Rifle (30 rounds, full-auto 630 RPM, holographic sight, foregrip)
    ///     [3] Remington 870 Combat Shotgun (8 shells, pump-action, 8-pellet spread cone)
    /// - Dynamic Weapon Switching: Keys [1], [2], [3] or Mouse Scroll Wheel with smooth draw/holster animations
    /// - Dynamic Recoil, Muzzle Flash, and Spent Brass Ejection for every weapon
    /// - Full Sound Effects: Punchy procedural audio synthesis for all weapons, hits, and reloads
    /// - Interactive Events: Reacts to explosive hazmat barrels, moving tactical enemies, and drill popups
    /// - Esports-grade Tactical HUD with weapon selector dock, ammo bar, combo streaks, and floating combat text
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

        // Weapon Arsenal State
        private WeaponType _currentWeapon = WeaponType.Glock17;
        private int[] _ammoCurrent = new int[] { 17, 30, 8 };
        private readonly int[] AMMO_MAX = new int[] { 17, 30, 8 };
        private readonly float[] FIRE_RATES = new float[] { 0.16f, 0.095f, 0.72f };
        private readonly bool[] IS_AUTO = new bool[] { false, true, false };

        private bool _isAiming = false;
        private bool _isReloading = false;
        private float _lastShootTime = 0f;
        private float _weaponSwitchTimer = 0f;
        private Vector3 _recoilOffset = Vector3.zero;
        private Quaternion _recoilRotation = Quaternion.identity;

        // Viewmodel Root & Weapon Sub-models
        private Transform _viewmodelRoot;
        private GameObject _glockModel;
        private GameObject _rifleModel;
        private GameObject _shotgunModel;
        private Transform _glockSlide;
        private Transform _shotgunPumpSlide;

        // Visual FX
        private Light _muzzleFlashLight;
        private GameObject _muzzleFlashQuad;
        private float _flashTimer = 0f;

        // Tactical Attachments
        private bool _laserActive = true;
        private bool _flashlightActive = false;
        private LineRenderer _laserLine;
        private GameObject _laserDot;
        private Light _tacticalFlashlight;

        // Audio Sources & Procedural Clips
        private AudioSource _musicSource;
        private AudioSource _weaponAudioSource;
        private AudioClip _rifleShotClip;
        private AudioClip _shotgunShotClip;
        private AudioClip _shotgunPumpClip;
        private AudioClip _dryFireClip;

        // Scoring & Combat
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
        private float _cameraShakeIntensity = 0f;

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

            IVRAR.Group02.Environment.ExplosiveBarrel.OnBarrelExploded += HandleBarrelExploded;
            IVRAR.Group02.Environment.MovingEnemyController.OnEnemyKilled += HandleEnemyKilled;
        }

        private void OnDestroy()
        {
            IVRAR.Group02.Environment.ExplosiveBarrel.OnBarrelExploded -= HandleBarrelExploded;
            IVRAR.Group02.Environment.MovingEnemyController.OnEnemyKilled -= HandleEnemyKilled;
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

            BuildArsenalViewmodels();

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

            // Generate high-impact procedural clips for rifle, shotgun, and pump
            _rifleShotClip = GenerateRifleShotClip();
            _shotgunShotClip = GenerateShotgunShotClip();
            _shotgunPumpClip = GeneratePumpActionClip();
            _dryFireClip = GenerateDryFireClip();
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

        #region Weapon Viewmodel Procedural Builders

        private void BuildArsenalViewmodels()
        {
            if (_targetCamera == null) return;
            if (_viewmodelRoot != null) return;

            GameObject root = new GameObject("Tactical_Arsenal_ViewmodelRoot");
            root.transform.SetParent(_targetCamera.transform, false);
            root.transform.localPosition = new Vector3(0.18f, -0.16f, 0.42f);
            root.transform.localRotation = Quaternion.identity;
            _viewmodelRoot = root.transform;

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");

            Material blackSteel = new Material(litShader) { color = new Color(0.12f, 0.13f, 0.14f, 1f) };
            Material darkPolymer = new Material(litShader) { color = new Color(0.08f, 0.09f, 0.10f, 1f) };
            Material silverMetal = new Material(litShader) { color = new Color(0.65f, 0.68f, 0.72f, 1f) };
            Material oliveDrab = new Material(litShader) { color = new Color(0.18f, 0.22f, 0.18f, 1f) };
            Material tritiumGreen = new Material(unlitShader) { color = new Color(0.2f, 1.0f, 0.35f, 1f) };
            Material holoRed = new Material(unlitShader) { color = new Color(1.0f, 0.15f, 0.15f, 1f) };

            // 1. GLOCK 17 MODEL
            _glockModel = new GameObject("Glock17_Model");
            _glockModel.transform.SetParent(_viewmodelRoot, false);
            BuildGlockComponents(_glockModel.transform, blackSteel, darkPolymer, silverMetal, tritiumGreen, unlitShader);

            // 2. M4A1 CQB ASSAULT RIFLE MODEL
            _rifleModel = new GameObject("M4A1_Model");
            _rifleModel.transform.SetParent(_viewmodelRoot, false);
            BuildM4A1Components(_rifleModel.transform, blackSteel, darkPolymer, oliveDrab, holoRed);

            // 3. REMINGTON 870 COMBAT SHOTGUN MODEL
            _shotgunModel = new GameObject("Shotgun_Model");
            _shotgunModel.transform.SetParent(_viewmodelRoot, false);
            BuildShotgunComponents(_shotgunModel.transform, blackSteel, darkPolymer, silverMetal, tritiumGreen);

            // 4. Shared Muzzle Flash Light & Quad
            GameObject flashLightObj = new GameObject("MuzzleFlashLight");
            flashLightObj.transform.SetParent(_viewmodelRoot, false);
            flashLightObj.transform.localPosition = new Vector3(0f, 0.035f, 0.35f);
            _muzzleFlashLight = flashLightObj.AddComponent<Light>();
            _muzzleFlashLight.type = LightType.Point;
            _muzzleFlashLight.range = 7f;
            _muzzleFlashLight.intensity = 5.0f;
            _muzzleFlashLight.color = new Color(1.0f, 0.85f, 0.45f);
            _muzzleFlashLight.enabled = false;

            _muzzleFlashQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _muzzleFlashQuad.name = "MuzzleFlashQuad";
            _muzzleFlashQuad.transform.SetParent(_viewmodelRoot, false);
            _muzzleFlashQuad.transform.localPosition = new Vector3(0f, 0.035f, 0.38f);
            _muzzleFlashQuad.transform.localScale = new Vector3(0.16f, 0.16f, 0.16f);
            Destroy(_muzzleFlashQuad.GetComponent<Collider>());
            _muzzleFlashQuad.GetComponent<Renderer>().material = new Material(unlitShader) { color = new Color(1f, 0.95f, 0.65f, 1f) };
            _muzzleFlashQuad.SetActive(false);

            UpdateWeaponModelVisibility();
        }

        private void BuildGlockComponents(Transform parent, Material blackSteel, Material darkPolymer, Material silverMetal, Material tritiumGreen, Shader unlitShader)
        {
            // Frame
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Frame";
            frame.transform.SetParent(parent, false);
            frame.transform.localPosition = new Vector3(0f, 0.005f, 0.05f);
            frame.transform.localScale = new Vector3(0.030f, 0.024f, 0.18f);
            Destroy(frame.GetComponent<Collider>());
            frame.GetComponent<Renderer>().material = darkPolymer;

            // Grip
            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grip.name = "Grip";
            grip.transform.SetParent(parent, false);
            grip.transform.localPosition = new Vector3(0f, -0.065f, -0.015f);
            grip.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
            grip.transform.localScale = new Vector3(0.028f, 0.125f, 0.046f);
            Destroy(grip.GetComponent<Collider>());
            grip.GetComponent<Renderer>().material = darkPolymer;

            // Slide
            GameObject slide = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slide.name = "Slide";
            slide.transform.SetParent(parent, false);
            slide.transform.localPosition = new Vector3(0f, 0.028f, 0.05f);
            slide.transform.localScale = new Vector3(0.032f, 0.030f, 0.19f);
            Destroy(slide.GetComponent<Collider>());
            slide.GetComponent<Renderer>().material = blackSteel;
            _glockSlide = slide.transform;

            // Barrel & Sights
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Barrel";
            barrel.transform.SetParent(slide.transform, false);
            barrel.transform.localPosition = new Vector3(0f, -0.05f, 0.52f);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            barrel.transform.localScale = new Vector3(0.38f, 0.06f, 0.38f);
            Destroy(barrel.GetComponent<Collider>());
            barrel.GetComponent<Renderer>().material = silverMetal;

            // Tritium Front Sight
            GameObject frontSight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontSight.name = "FrontSight";
            frontSight.transform.SetParent(slide.transform, false);
            frontSight.transform.localPosition = new Vector3(0f, 0.58f, 0.44f);
            frontSight.transform.localScale = new Vector3(0.12f, 0.22f, 0.08f);
            Destroy(frontSight.GetComponent<Collider>());
            frontSight.GetComponent<Renderer>().material = tritiumGreen;

            // Tactical Rail & Laser
            GameObject railUnit = GameObject.CreatePrimitive(PrimitiveType.Cube);
            railUnit.name = "LaserLightUnit";
            railUnit.transform.SetParent(parent, false);
            railUnit.transform.localPosition = new Vector3(0f, -0.010f, 0.115f);
            railUnit.transform.localScale = new Vector3(0.026f, 0.022f, 0.065f);
            Destroy(railUnit.GetComponent<Collider>());
            railUnit.GetComponent<Renderer>().material = blackSteel;

            // Flashlight
            GameObject lightObj = new GameObject("TacticalFlashlight");
            lightObj.transform.SetParent(railUnit.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0f, 0.6f);
            _tacticalFlashlight = lightObj.AddComponent<Light>();
            _tacticalFlashlight.type = LightType.Spot;
            _tacticalFlashlight.range = 40f;
            _tacticalFlashlight.spotAngle = 36f;
            _tacticalFlashlight.intensity = 2.8f;
            _tacticalFlashlight.color = new Color(0.96f, 0.98f, 1.0f);
            _tacticalFlashlight.enabled = _flashlightActive;

            // Laser Beam & Dot
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
            _laserDot.name = "LaserDot";
            _laserDot.transform.localScale = Vector3.one * 0.035f;
            Destroy(_laserDot.GetComponent<Collider>());
            _laserDot.GetComponent<Renderer>().material = new Material(unlitShader) { color = Color.red };
            _laserDot.SetActive(_laserActive);
        }

        private void BuildM4A1Components(Transform parent, Material blackSteel, Material darkPolymer, Material oliveDrab, Material holoRed)
        {
            // Receiver
            GameObject receiver = GameObject.CreatePrimitive(PrimitiveType.Cube);
            receiver.name = "UpperLowerReceiver";
            receiver.transform.SetParent(parent, false);
            receiver.transform.localPosition = new Vector3(0f, 0.01f, 0.02f);
            receiver.transform.localScale = new Vector3(0.038f, 0.065f, 0.24f);
            Destroy(receiver.GetComponent<Collider>());
            receiver.GetComponent<Renderer>().material = blackSteel;

            // Buffer Tube & Tactical Stock
            GameObject stock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stock.name = "CraneStock";
            stock.transform.SetParent(parent, false);
            stock.transform.localPosition = new Vector3(0f, 0.01f, -0.18f);
            stock.transform.localScale = new Vector3(0.034f, 0.085f, 0.18f);
            Destroy(stock.GetComponent<Collider>());
            stock.GetComponent<Renderer>().material = darkPolymer;

            // Pistol Grip
            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grip.name = "PistolGrip";
            grip.transform.SetParent(parent, false);
            grip.transform.localPosition = new Vector3(0f, -0.075f, -0.04f);
            grip.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
            grip.transform.localScale = new Vector3(0.028f, 0.11f, 0.045f);
            Destroy(grip.GetComponent<Collider>());
            grip.GetComponent<Renderer>().material = darkPolymer;

            // Curved 30-round STANAG Magazine
            GameObject mag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mag.name = "Magazine";
            mag.transform.SetParent(parent, false);
            mag.transform.localPosition = new Vector3(0f, -0.09f, 0.06f);
            mag.transform.localRotation = Quaternion.Euler(14f, 0f, 0f);
            mag.transform.localScale = new Vector3(0.026f, 0.15f, 0.055f);
            Destroy(mag.GetComponent<Collider>());
            mag.GetComponent<Renderer>().material = oliveDrab;

            // Handguard Quad-Rail
            GameObject handguard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handguard.name = "QuadRailHandguard";
            handguard.transform.SetParent(parent, false);
            handguard.transform.localPosition = new Vector3(0f, 0.015f, 0.22f);
            handguard.transform.localScale = new Vector3(0.040f, 0.045f, 0.20f);
            Destroy(handguard.GetComponent<Collider>());
            handguard.GetComponent<Renderer>().material = darkPolymer;

            // Vertical Foregrip
            GameObject vgrip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            vgrip.name = "VerticalForegrip";
            vgrip.transform.SetParent(parent, false);
            vgrip.transform.localPosition = new Vector3(0f, -0.055f, 0.22f);
            vgrip.transform.localScale = new Vector3(0.025f, 0.045f, 0.025f);
            Destroy(vgrip.GetComponent<Collider>());
            vgrip.GetComponent<Renderer>().material = blackSteel;

            // Barrel & Birdcage Flash Hider
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "CarbineBarrel";
            barrel.transform.SetParent(parent, false);
            barrel.transform.localPosition = new Vector3(0f, 0.015f, 0.36f);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            barrel.transform.localScale = new Vector3(0.018f, 0.12f, 0.018f);
            Destroy(barrel.GetComponent<Collider>());
            barrel.GetComponent<Renderer>().material = blackSteel;

            // EOTech Holographic Optic Sight Housing
            GameObject optic = GameObject.CreatePrimitive(PrimitiveType.Cube);
            optic.name = "HoloSightHousing";
            optic.transform.SetParent(parent, false);
            optic.transform.localPosition = new Vector3(0f, 0.068f, 0.02f);
            optic.transform.localScale = new Vector3(0.034f, 0.038f, 0.09f);
            Destroy(optic.GetComponent<Collider>());
            optic.GetComponent<Renderer>().material = blackSteel;

            // Glowing Holographic Red Circle Reticle
            GameObject reticle = GameObject.CreatePrimitive(PrimitiveType.Quad);
            reticle.name = "HoloReticle";
            reticle.transform.SetParent(optic.transform, false);
            reticle.transform.localPosition = new Vector3(0f, 0.1f, -0.51f);
            reticle.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            Destroy(reticle.GetComponent<Collider>());
            reticle.GetComponent<Renderer>().material = holoRed;
        }

        private void BuildShotgunComponents(Transform parent, Material blackSteel, Material darkPolymer, Material silverMetal, Material tritiumGreen)
        {
            // Receiver
            GameObject receiver = GameObject.CreatePrimitive(PrimitiveType.Cube);
            receiver.name = "ShotgunReceiver";
            receiver.transform.SetParent(parent, false);
            receiver.transform.localPosition = new Vector3(0f, 0.01f, 0.02f);
            receiver.transform.localScale = new Vector3(0.040f, 0.055f, 0.22f);
            Destroy(receiver.GetComponent<Collider>());
            receiver.GetComponent<Renderer>().material = blackSteel;

            // Polymer Stock
            GameObject stock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stock.name = "SyntheticStock";
            stock.transform.SetParent(parent, false);
            stock.transform.localPosition = new Vector3(0f, -0.02f, -0.19f);
            stock.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
            stock.transform.localScale = new Vector3(0.034f, 0.085f, 0.22f);
            Destroy(stock.GetComponent<Collider>());
            stock.GetComponent<Renderer>().material = darkPolymer;

            // Main Shotgun Heavy Barrel (12 Gauge)
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "ShotgunBarrel";
            barrel.transform.SetParent(parent, false);
            barrel.transform.localPosition = new Vector3(0f, 0.025f, 0.32f);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            barrel.transform.localScale = new Vector3(0.024f, 0.24f, 0.024f);
            Destroy(barrel.GetComponent<Collider>());
            barrel.GetComponent<Renderer>().material = silverMetal;

            // Underbarrel Magazine Tube
            GameObject magTube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            magTube.name = "MagazineTube";
            magTube.transform.SetParent(parent, false);
            magTube.transform.localPosition = new Vector3(0f, -0.005f, 0.28f);
            magTube.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            magTube.transform.localScale = new Vector3(0.022f, 0.20f, 0.022f);
            Destroy(magTube.GetComponent<Collider>());
            magTube.GetComponent<Renderer>().material = blackSteel;

            // Ribbed Pump Slide Forend
            GameObject pump = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pump.name = "PumpSlide";
            pump.transform.SetParent(parent, false);
            pump.transform.localPosition = new Vector3(0f, -0.005f, 0.24f);
            pump.transform.localScale = new Vector3(0.042f, 0.040f, 0.12f);
            Destroy(pump.GetComponent<Collider>());
            pump.GetComponent<Renderer>().material = darkPolymer;
            _shotgunPumpSlide = pump.transform;

            // High-Visibility Bead Sight
            GameObject bead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bead.name = "FrontBeadSight";
            bead.transform.SetParent(barrel.transform, false);
            bead.transform.localPosition = new Vector3(0f, 0.98f, 0.6f);
            bead.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            Destroy(bead.GetComponent<Collider>());
            bead.GetComponent<Renderer>().material = tritiumGreen;
        }

        private void UpdateWeaponModelVisibility()
        {
            if (_glockModel != null) _glockModel.SetActive(_currentWeapon == WeaponType.Glock17);
            if (_rifleModel != null) _rifleModel.SetActive(_currentWeapon == WeaponType.AssaultRifleM4A1);
            if (_shotgunModel != null) _shotgunModel.SetActive(_currentWeapon == WeaponType.CombatShotgun);

            if (_laserLine != null) _laserLine.enabled = _laserActive && _currentWeapon == WeaponType.Glock17;
            if (_laserDot != null) _laserDot.SetActive(_laserActive && _currentWeapon == WeaponType.Glock17);
        }

        #endregion

        #region Input & Updates

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
            HandleWeaponSwitch();
            HandleMouseLook();
            HandleLocomotion();
            HandleAimingAndZoom();
            HandleShootingAndReload();
            HandleTacticalAttachments();
            UpdateWeaponSwayAndRecoil();
            UpdateLaserSight();
            UpdateCombosAndEffects();
        }

        private void HandleWeaponSwitch()
        {
            if (_isReloading) return;

            WeaponType targetWeapon = _currentWeapon;

            if (Input.GetKeyDown(KeyCode.Alpha1)) targetWeapon = WeaponType.Glock17;
            else if (Input.GetKeyDown(KeyCode.Alpha2)) targetWeapon = WeaponType.AssaultRifleM4A1;
            else if (Input.GetKeyDown(KeyCode.Alpha3)) targetWeapon = WeaponType.CombatShotgun;

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0.05f)
            {
                targetWeapon = (WeaponType)(((int)_currentWeapon + 1) % 3);
            }
            else if (scroll < -0.05f)
            {
                targetWeapon = (WeaponType)(((int)_currentWeapon + 2) % 3);
            }

            if (targetWeapon != _currentWeapon)
            {
                StartCoroutine(SwitchWeaponCoroutine(targetWeapon));
            }
        }

        private IEnumerator SwitchWeaponCoroutine(WeaponType newWeapon)
        {
            _weaponSwitchTimer = 0.25f;

            // Lower weapon
            Vector3 origPos = _viewmodelRoot != null ? _viewmodelRoot.localPosition : Vector3.zero;
            Vector3 lowerPos = origPos + Vector3.down * 0.22f;

            float t = 0f;
            while (t < 0.12f)
            {
                if (_viewmodelRoot != null) _viewmodelRoot.localPosition = Vector3.Lerp(origPos, lowerPos, t / 0.12f);
                t += Time.deltaTime;
                yield return null;
            }

            _currentWeapon = newWeapon;
            UpdateWeaponModelVisibility();

            // Play equip click
            if (_slideForwardClip != null && _weaponAudioSource != null)
            {
                _weaponAudioSource.PlayOneShot(_slideForwardClip, 0.75f);
            }

            // Raise new weapon
            t = 0f;
            while (t < 0.14f)
            {
                if (_viewmodelRoot != null) _viewmodelRoot.localPosition = Vector3.Lerp(lowerPos, origPos, t / 0.14f);
                t += Time.deltaTime;
                yield return null;
            }

            _weaponSwitchTimer = 0f;
        }

        private void HandleCursorLock()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ApplyCursorLock(false);
            }
            else if (Input.GetMouseButtonDown(0) && !_cursorLocked)
            {
                ApplyCursorLock(true);
            }
        }

        public void ApplyCursorLock(bool locked)
        {
            _cursorLocked = locked;
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

        /// <summary>
        /// Solid Locomotion with Swept-Capsule Collision Detection.
        /// Prevents passing through solid objects, hurdles, benches, and walls.
        /// Slides smoothly along obstacle surfaces.
        /// </summary>
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
                float moveDist = speed * Time.unscaledDeltaTime;

                // Swept Capsule Collision against solid environment
                Vector3 currentPos = _playerOrigin.position;
                Vector3 bottom = currentPos + Vector3.down * 1.35f;
                Vector3 top = currentPos + Vector3.up * 0.15f;
                float radius = 0.36f;
                int layerMask = ~0;

                if (Physics.CapsuleCast(bottom, top, radius, moveDir, out RaycastHit hit, moveDist + 0.04f, layerMask, QueryTriggerInteraction.Ignore))
                {
                    // Blocked by solid object: Move up to contact and slide along normal
                    float allowedDist = Mathf.Max(0f, hit.distance - 0.04f);
                    currentPos += moveDir * allowedDist;

                    Vector3 slideDir = Vector3.ProjectOnPlane(moveDir, hit.normal).normalized;
                    float remDist = moveDist - allowedDist;
                    if (slideDir.sqrMagnitude > 0.001f && remDist > 0.001f)
                    {
                        if (!Physics.CapsuleCast(currentPos + Vector3.down * 1.35f, currentPos + Vector3.up * 0.15f, radius, slideDir, out RaycastHit slideHit, remDist + 0.04f, layerMask, QueryTriggerInteraction.Ignore))
                        {
                            currentPos += slideDir * remDist;
                        }
                        else
                        {
                            currentPos += slideDir * Mathf.Max(0f, slideHit.distance - 0.04f);
                        }
                    }
                }
                else
                {
                    currentPos += moveDir * moveDist;
                }

                // Range Bounds Clamping
                currentPos.x = Mathf.Clamp(currentPos.x, -14.2f, 14.2f);
                currentPos.z = Mathf.Clamp(currentPos.z, -3.5f, 31.0f);
                currentPos.y = 1.6f;

                _playerOrigin.position = currentPos;
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

            float targetFov;
            switch (_currentWeapon)
            {
                case WeaponType.AssaultRifleM4A1: targetFov = _isAiming ? 46f : _hipFov; break;
                case WeaponType.CombatShotgun: targetFov = _isAiming ? 54f : _hipFov; break;
                default: targetFov = _isAiming ? _aimFov : _hipFov; break;
            }

            _targetCamera.fieldOfView = Mathf.Lerp(_targetCamera.fieldOfView, targetFov, Time.deltaTime * 14f);
        }

        private void HandleShootingAndReload()
        {
            int widx = (int)_currentWeapon;

            if (Input.GetKeyDown(KeyCode.R) && !_isReloading && _ammoCurrent[widx] < AMMO_MAX[widx])
            {
                StartCoroutine(ReloadCoroutine());
            }

            bool fireInput = IS_AUTO[widx] ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);

            if (fireInput && _cursorLocked && _weaponSwitchTimer <= 0f)
            {
                if (_isReloading) return;

                if (_ammoCurrent[widx] <= 0)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        if (_dryFireClip != null && _weaponAudioSource != null)
                            _weaponAudioSource.PlayOneShot(_dryFireClip, 0.7f);
                    }
                    StartCoroutine(ReloadCoroutine());
                    return;
                }

                if (Time.time - _lastShootTime >= FIRE_RATES[widx])
                {
                    ExecuteWeaponShot();
                }
            }
        }

        private void ExecuteWeaponShot()
        {
            int widx = (int)_currentWeapon;
            _lastShootTime = Time.time;
            _ammoCurrent[widx]--;
            _shotsFired++;

            // 1. Play Weapon Audio
            PlayWeaponShotAudio();

            // 2. Muzzle Flash
            if (_muzzleFlashLight != null) _muzzleFlashLight.enabled = true;
            if (_muzzleFlashQuad != null)
            {
                _muzzleFlashQuad.transform.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
                _muzzleFlashQuad.SetActive(true);
            }
            _flashTimer = 0.05f;

            // 3. Dynamic Weapon Recoil Kick
            ApplyWeaponRecoil();

            // 4. Spent Casing Ejection
            EjectShellCasing();

            // 5. Weapon Ballistic Raycasts
            if (_currentWeapon == WeaponType.CombatShotgun)
            {
                // Shotgun fires 8 buckshot pellets in spread cone
                for (int i = 0; i < 8; i++)
                {
                    Vector2 spread = UnityEngine.Random.insideUnitCircle * 0.042f;
                    Vector3 pelletDir = (_targetCamera.transform.forward + _targetCamera.transform.right * spread.x + _targetCamera.transform.up * spread.y).normalized;
                    Ray ray = new Ray(_targetCamera.transform.position, pelletDir);
                    if (Physics.Raycast(ray, out RaycastHit hit, 150f))
                    {
                        ProcessHit(hit, 25f);
                    }
                }
                // Racking pump action
                StartCoroutine(ShotgunPumpCoroutine());
            }
            else
            {
                // Single bullet (Pistol or Rifle)
                float spreadAmount = _currentWeapon == WeaponType.AssaultRifleM4A1 ? (_isAiming ? 0.006f : 0.016f) : (_isAiming ? 0.001f : 0.005f);
                Vector2 spread = UnityEngine.Random.insideUnitCircle * spreadAmount;
                Vector3 shootDir = (_targetCamera.transform.forward + _targetCamera.transform.right * spread.x + _targetCamera.transform.up * spread.y).normalized;
                Ray ray = new Ray(_targetCamera.transform.position, shootDir);
                if (Physics.Raycast(ray, out RaycastHit hit, 200f))
                {
                    float dmg = _currentWeapon == WeaponType.AssaultRifleM4A1 ? 40f : 45f;
                    ProcessHit(hit, dmg);
                }
            }

            if (_responseMechanism != null) _responseMechanism.ProcessResponse(null);
            else if (_rayPointer != null) _rayPointer.TriggerClick();
        }

        private void PlayWeaponShotAudio()
        {
            if (_weaponAudioSource == null) return;

            switch (_currentWeapon)
            {
                case WeaponType.AssaultRifleM4A1:
                    _weaponAudioSource.pitch = UnityEngine.Random.Range(0.97f, 1.03f);
                    _weaponAudioSource.PlayOneShot(_rifleShotClip, 0.95f);
                    break;

                case WeaponType.CombatShotgun:
                    _weaponAudioSource.pitch = UnityEngine.Random.Range(0.95f, 1.02f);
                    _weaponAudioSource.PlayOneShot(_shotgunShotClip, 1.0f);
                    break;

                default: // Glock 17
                    if (_shotAudioClip != null)
                    {
                        _weaponAudioSource.pitch = UnityEngine.Random.Range(0.96f, 1.04f);
                        _weaponAudioSource.PlayOneShot(_shotAudioClip, 0.95f);
                    }
                    break;
            }
        }

        private void ApplyWeaponRecoil()
        {
            switch (_currentWeapon)
            {
                case WeaponType.AssaultRifleM4A1:
                    _recoilOffset = new Vector3(0f, 0.018f, -0.048f);
                    _recoilRotation = Quaternion.Euler(-3.5f, UnityEngine.Random.Range(-0.8f, 0.8f), 0f);
                    break;

                case WeaponType.CombatShotgun:
                    _recoilOffset = new Vector3(0f, 0.055f, -0.11f);
                    _recoilRotation = Quaternion.Euler(-8.0f, UnityEngine.Random.Range(-1.2f, 1.2f), 0f);
                    _cameraShakeIntensity = 0.22f;
                    break;

                default: // Glock 17
                    _recoilOffset = new Vector3(0f, 0.024f, -0.065f);
                    _recoilRotation = Quaternion.Euler(-4.5f, UnityEngine.Random.Range(-0.6f, 0.6f), 0f);
                    break;
            }
        }

        private IEnumerator ShotgunPumpCoroutine()
        {
            yield return new WaitForSeconds(0.24f);

            // Slide back
            if (_shotgunPumpSlide != null)
                _shotgunPumpSlide.localPosition = new Vector3(0f, -0.005f, 0.18f);

            if (_shotgunPumpClip != null && _weaponAudioSource != null)
                _weaponAudioSource.PlayOneShot(_shotgunPumpClip, 0.85f);

            yield return new WaitForSeconds(0.22f);

            // Slide forward
            if (_shotgunPumpSlide != null)
                _shotgunPumpSlide.localPosition = new Vector3(0f, -0.005f, 0.24f);
        }

        private void EjectShellCasing()
        {
            if (_viewmodelRoot == null) return;

            GameObject casing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            casing.name = "SpentBrass_Casing";
            casing.transform.position = _viewmodelRoot.position + _viewmodelRoot.right * 0.04f + _viewmodelRoot.up * 0.02f;
            float scaleY = _currentWeapon == WeaponType.CombatShotgun ? 0.028f : 0.016f;
            float scaleR = _currentWeapon == WeaponType.CombatShotgun ? 0.014f : 0.009f;
            casing.transform.localScale = new Vector3(scaleR, scaleY, scaleR);
            casing.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360));
            Destroy(casing.GetComponent<Collider>());

            Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (lit != null)
            {
                Color casingColor = _currentWeapon == WeaponType.CombatShotgun ? new Color(0.85f, 0.15f, 0.12f, 1f) : new Color(0.85f, 0.70f, 0.25f, 1f);
                casing.GetComponent<Renderer>().material = new Material(lit) { color = casingColor };
            }

            Rigidbody rb = casing.AddComponent<Rigidbody>();
            rb.mass = 0.01f;
            Vector3 ejectDir = (_viewmodelRoot.right * 2.8f + _viewmodelRoot.up * 1.6f + _viewmodelRoot.forward * 0.1f);
            rb.linearVelocity = ejectDir + UnityEngine.Random.insideUnitSphere * 0.4f;
            rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 25f;

            Destroy(casing, 1.4f);
        }

        private void ProcessHit(RaycastHit hit, float damage)
        {
            var hitObj = hit.transform.gameObject;
            string objName = hitObj.name.ToLower();

            var popUpCtrl = hitObj.GetComponentInParent<IVRAR.Group02.Environment.TargetPopUpController>();
            bool isCivilian = (popUpCtrl != null && popUpCtrl.Classification == IVRAR.Group02.Environment.TargetClassification.CivilianFriendly) ||
                              objName.Contains("civilian") || objName.Contains("friendly");

            bool isHeadshot = objName.Contains("head") || hit.point.y > 1.65f;
            bool isEnemy = !isCivilian && (objName.Contains("target") || objName.Contains("enemy") || objName.Contains("hostile") || objName.Contains("silhouette") || popUpCtrl != null);

            var damageData = new DamageData
            {
                contactPoint = hit.point,
                normalPoint = Quaternion.LookRotation(hit.normal),
                force = _targetCamera.transform.forward * (damage * 1.2f)
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

            if (isCivilian)
            {
                _score = Mathf.Max(0, _score - 150);
                _comboMultiplier = 1;
                _hitMarkerTimer = 0.35f;
                _lastHitText = "⚠ CEASE FIRE! CIVILIAN CASUALTY (-150 PTS) ⚠";
                _lastHitColor = Color.red;
                _hitTextTimer = 1.6f;
            }
            else if (isEnemy || damageable != null || humanTarget != null)
            {
                _totalHits++;
                _hitMarkerTimer = 0.25f;

                if (_comboTimer > 0f) _comboMultiplier = Mathf.Min(_comboMultiplier + 1, 5);
                else _comboMultiplier = 1;
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
            spark.transform.localScale = Vector3.one * 0.07f;
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

        private void HandleBarrelExploded(Vector3 blastPos, int targetsHit)
        {
            _cameraShakeIntensity = 0.45f;
            int bonus = 500 + targetsHit * 150;
            _score += bonus;
            _lastHitText = $"💥 HAZMAT BARREL DETONATION! +{bonus} PTS ({targetsHit} KILLS) 💥";
            _lastHitColor = new Color(1f, 0.55f, 0.1f);
            _hitTextTimer = 2.0f;
        }

        private void HandleEnemyKilled(IVRAR.Group02.Environment.MovingEnemyController enemy, bool headshot, int pts)
        {
            _score += pts * _comboMultiplier;
            _lastHitText = headshot ? $"★ TACTICAL HEADSHOT ELIMINATION! +{pts * _comboMultiplier} ★" : $"ENEMY NEUTRALIZED! +{pts * _comboMultiplier}";
            _lastHitColor = headshot ? Color.yellow : Color.green;
            _hitTextTimer = 1.4f;
        }

        private IEnumerator ReloadCoroutine()
        {
            _isReloading = true;
            int widx = (int)_currentWeapon;

            if (_magOutClip != null && _weaponAudioSource != null)
            {
                _weaponAudioSource.PlayOneShot(_magOutClip, 0.85f);
            }

            float reloadDuration = _currentWeapon == WeaponType.AssaultRifleM4A1 ? 1.6f : (_currentWeapon == WeaponType.CombatShotgun ? 2.0f : 1.2f);
            yield return new WaitForSeconds(reloadDuration * 0.5f);

            if (_magInClip != null && _weaponAudioSource != null)
            {
                _weaponAudioSource.PlayOneShot(_magInClip, 0.85f);
            }
            yield return new WaitForSeconds(reloadDuration * 0.35f);

            if (_slideForwardClip != null && _weaponAudioSource != null)
            {
                _weaponAudioSource.PlayOneShot(_slideForwardClip, 0.85f);
            }
            yield return new WaitForSeconds(reloadDuration * 0.15f);

            _ammoCurrent[widx] = AMMO_MAX[widx];
            _isReloading = false;
        }

        private void HandleTacticalAttachments()
        {
            if (Input.GetKeyDown(KeyCode.L))
            {
                _laserActive = !_laserActive;
                if (_laserLine != null) _laserLine.enabled = _laserActive && _currentWeapon == WeaponType.Glock17;
                if (_laserDot != null) _laserDot.SetActive(_laserActive && _currentWeapon == WeaponType.Glock17);
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                _flashlightActive = !_flashlightActive;
                if (_tacticalFlashlight != null) _tacticalFlashlight.enabled = _flashlightActive;
            }
        }

        private void UpdateWeaponSwayAndRecoil()
        {
            if (_viewmodelRoot == null) return;

            // Target positions depending on weapon and ADS
            Vector3 targetHipPos;
            Vector3 targetAimPos;

            switch (_currentWeapon)
            {
                case WeaponType.AssaultRifleM4A1:
                    targetHipPos = new Vector3(0.18f, -0.16f, 0.42f);
                    targetAimPos = new Vector3(0.0f, -0.118f, 0.35f);
                    break;
                case WeaponType.CombatShotgun:
                    targetHipPos = new Vector3(0.19f, -0.17f, 0.44f);
                    targetAimPos = new Vector3(0.0f, -0.115f, 0.37f);
                    break;
                default: // Glock 17
                    targetHipPos = new Vector3(0.18f, -0.16f, 0.38f);
                    targetAimPos = new Vector3(0.0f, -0.118f, 0.33f);
                    break;
            }

            Vector3 basePos = _isAiming ? targetAimPos : targetHipPos;

            // Weapon bobbing
            float bobX = Mathf.Cos(_bobTimer) * 0.008f;
            float bobY = Mathf.Sin(_bobTimer * 2f) * 0.007f;
            if (_isAiming) { bobX *= 0.15f; bobY *= 0.15f; }

            Vector3 targetPos = basePos + new Vector3(bobX, bobY, 0f) + _recoilOffset;
            _viewmodelRoot.localPosition = Vector3.Lerp(_viewmodelRoot.localPosition, targetPos, Time.deltaTime * 18f);

            Quaternion targetRot = _isAiming ? Quaternion.identity : Quaternion.Euler(1.5f, -2.5f, 0f);
            targetRot *= _recoilRotation;
            _viewmodelRoot.localRotation = Quaternion.Slerp(_viewmodelRoot.localRotation, targetRot, Time.deltaTime * 20f);

            // Recover recoil
            _recoilOffset = Vector3.Lerp(_recoilOffset, Vector3.zero, Time.deltaTime * 14f);
            _recoilRotation = Quaternion.Slerp(_recoilRotation, Quaternion.identity, Time.deltaTime * 14f);

            // Flash timeout
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                if (_flashTimer <= 0f)
                {
                    if (_muzzleFlashLight != null) _muzzleFlashLight.enabled = false;
                    if (_muzzleFlashQuad != null) _muzzleFlashQuad.SetActive(false);
                }
            }

            // Camera shake recovery
            if (_cameraShakeIntensity > 0f && _targetCamera != null)
            {
                _targetCamera.transform.position += UnityEngine.Random.insideUnitSphere * (_cameraShakeIntensity * 0.08f);
                _cameraShakeIntensity = Mathf.MoveTowards(_cameraShakeIntensity, 0f, Time.deltaTime * 1.8f);
            }
        }

        private void UpdateLaserSight()
        {
            if (!_laserActive || _laserLine == null || _targetCamera == null || _currentWeapon != WeaponType.Glock17) return;

            Vector3 origin = _laserLine.transform.position;
            Vector3 forward = _targetCamera.transform.forward;

            _laserLine.SetPosition(0, origin);

            if (Physics.Raycast(origin, forward, out RaycastHit hit, 80f))
            {
                _laserLine.SetPosition(1, hit.point);
                if (_laserDot != null)
                {
                    _laserDot.SetActive(true);
                    _laserDot.transform.position = hit.point - forward * 0.02f;
                    _laserDot.transform.rotation = Quaternion.LookRotation(hit.normal);
                }
            }
            else
            {
                _laserLine.SetPosition(1, origin + forward * 80f);
                if (_laserDot != null) _laserDot.SetActive(false);
            }
        }

        private void UpdateCombosAndEffects()
        {
            if (_comboTimer > 0f)
            {
                _comboTimer -= Time.deltaTime;
                if (_comboTimer <= 0f) _comboMultiplier = 1;
            }

            if (_hitMarkerTimer > 0f) _hitMarkerTimer -= Time.deltaTime;
            if (_hitTextTimer > 0f) _hitTextTimer -= Time.deltaTime;
        }

        #endregion

        #region Procedural Audio Generators

        private AudioClip GenerateRifleShotClip()
        {
            int sampleRate = 44100;
            float duration = 0.18f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 22f);
                float crack = Mathf.Sin(2f * Mathf.PI * 340f * t * (1f - t * 0.4f));
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.75f;
                samples[i] = (crack * 0.55f + noise * 0.45f) * env;
            }

            AudioClip clip = AudioClip.Create("RifleShot", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateShotgunShotClip()
        {
            int sampleRate = 44100;
            float duration = 0.48f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 11f);
                float punch = Mathf.Sin(2f * Mathf.PI * 95f * t * (1f - t * 0.6f));
                float blastNoise = (UnityEngine.Random.value * 2f - 1f) * 0.85f;
                samples[i] = (punch * 0.65f + blastNoise * 0.55f) * env;
            }

            AudioClip clip = AudioClip.Create("ShotgunShot", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GeneratePumpActionClip()
        {
            int sampleRate = 44100;
            float duration = 0.16f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 26f);
                float metallic = Mathf.Sin(2f * Mathf.PI * 880f * t);
                samples[i] = metallic * env * 0.7f;
            }

            AudioClip clip = AudioClip.Create("ShotgunPump", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateDryFireClip()
        {
            int sampleRate = 44100;
            float duration = 0.08f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 60f);
                float click = Mathf.Sin(2f * Mathf.PI * 1800f * t);
                samples[i] = click * env * 0.85f;
            }

            AudioClip clip = AudioClip.Create("DryFire", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        #endregion

        #region Public API

        public void SetDesktopMode(bool active)
        {
            IsDesktopModeActive = active;

            if (_viewmodelRoot != null)
            {
                _viewmodelRoot.gameObject.SetActive(active);
            }

            ApplyCursorLock(active);
            Debug.Log($"[DesktopFallbackController] Desktop Mode set to: {active}");
        }

        #endregion

        #region HUD & Esports Overlay

        private void OnGUI()
        {
            if (!IsDesktopModeActive) return;

            DrawWeaponDock();
            DrawReticleAndHitMarker();
            DrawCombatScore();
            DrawFloatingHitText();
            DrawTacticalDrillHUD();

            if (_showHelpOverlay)
            {
                DrawKeybindHelp();
            }
        }

        private void DrawWeaponDock()
        {
            float w = 420;
            float h = 64;
            float x = (Screen.width - w) / 2f;
            float y = Screen.height - h - 16;

            GUI.color = new Color(0.05f, 0.07f, 0.10f, 0.88f);
            GUI.Box(new Rect(x, y, w, h), GUIContent.none);

            string[] weaponNames = { "[1] GLOCK 17", "[2] M4A1 CARBINE", "[3] REMINGTON 870" };
            float colW = w / 3f;

            GUIStyle dockStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            for (int i = 0; i < 3; i++)
            {
                bool isSelected = (int)_currentWeapon == i;
                GUI.color = isSelected ? new Color(1.0f, 0.85f, 0.2f, 1f) : new Color(0.6f, 0.65f, 0.7f, 0.8f);

                Rect slotRect = new Rect(x + i * colW, y + 4, colW, 26);
                GUI.Label(slotRect, weaponNames[i], dockStyle);

                // Ammo subtext
                string ammoStr = $"{_ammoCurrent[i]} / {AMMO_MAX[i]}";
                if (_isReloading && isSelected) ammoStr = "RELOADING...";
                GUI.color = isSelected ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.7f);
                GUI.Label(new Rect(x + i * colW, y + 30, colW, 24), ammoStr, dockStyle);
            }
        }

        private void DrawReticleAndHitMarker()
        {
            float cx = Screen.width / 2f;
            float cy = Screen.height / 2f;

            // Reticle
            if (!_isAiming)
            {
                GUI.color = new Color(0.2f, 1.0f, 0.4f, 0.85f);
                if (_currentWeapon == WeaponType.CombatShotgun)
                {
                    // Circle reticle for shotgun
                    GUI.Box(new Rect(cx - 16, cy - 16, 32, 32), GUIContent.none);
                    GUI.DrawTexture(new Rect(cx - 2, cy - 2, 4, 4), Texture2D.whiteTexture);
                }
                else
                {
                    // Precision Crosshair
                    float spread = _currentWeapon == WeaponType.AssaultRifleM4A1 ? 14f : 8f;
                    GUI.DrawTexture(new Rect(cx - spread - 12, cy - 1.5f, 10, 3), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(cx + spread + 2, cy - 1.5f, 10, 3), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(cx - 1.5f, cy - spread - 12, 3, 10), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(cx - 1.5f, cy + spread + 2, 3, 10), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(cx - 2, cy - 2, 4, 4), Texture2D.whiteTexture);
                }
            }

            // Hit Marker (X indicator)
            if (_hitMarkerTimer > 0f)
            {
                GUI.color = Color.red;
                GUI.DrawTexture(new Rect(cx - 10, cy - 10, 5, 5), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(cx + 6, cy - 10, 5, 5), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(cx - 10, cy + 6, 5, 5), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(cx + 6, cy + 6, 5, 5), Texture2D.whiteTexture);
            }
        }

        private void DrawCombatScore()
        {
            float x = 20;
            float y = 20;
            float w = 260;
            float h = 100;

            GUI.color = new Color(0.05f, 0.07f, 0.10f, 0.85f);
            GUI.Box(new Rect(x, y, w, h), GUIContent.none);

            GUI.color = Color.white;
            GUIStyle scoreStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            GUI.Label(new Rect(x + 15, y + 8, w - 30, 26), $"SCORE: {_score:N0}", scoreStyle);

            GUIStyle subStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleLeft };
            GUI.color = new Color(0.8f, 0.85f, 0.9f);
            GUI.Label(new Rect(x + 15, y + 36, w - 30, 20), $"HITS: {_totalHits}  |  HEADSHOTS: {_headshots}", subStyle);

            // Combo Gauge
            if (_comboMultiplier > 1)
            {
                GUI.color = new Color(1f, 0.75f, 0.1f);
                GUIStyle comboStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
                GUI.Label(new Rect(x + 15, y + 62, w - 30, 22), $"STREAK: x{_comboMultiplier} COMBO! ({_comboTimer:F1}s)", comboStyle);
            }
            else
            {
                GUI.color = new Color(0.5f, 0.55f, 0.6f);
                GUI.Label(new Rect(x + 15, y + 62, w - 30, 20), $"SHOTS FIRED: {_shotsFired}", subStyle);
            }
        }

        private void DrawFloatingHitText()
        {
            if (_hitTextTimer <= 0f) return;

            float cx = Screen.width / 2f;
            float cy = Screen.height * 0.40f;

            GUI.color = _lastHitColor;
            GUIStyle hitStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            GUI.Label(new Rect(cx - 250, cy, 500, 35), _lastHitText, hitStyle);
        }

        private void DrawKeybindHelp()
        {
            float w = 240;
            float h = 175;
            float x = Screen.width - w - 20;
            float y = 20;

            GUI.color = new Color(0.04f, 0.06f, 0.08f, 0.85f);
            GUI.Box(new Rect(x, y, w, h), GUIContent.none);

            GUI.color = Color.yellow;
            GUIStyle hStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(x + 12, y + 8, w - 24, 20), "TACTICAL CONTROLS", hStyle);

            GUI.color = Color.white;
            GUIStyle bStyle = new GUIStyle(GUI.skin.label) { fontSize = 11 };
            float lineY = y + 30;
            GUI.Label(new Rect(x + 12, lineY, w - 24, 18), "• WASD + Shift: Move & Sprint", bStyle); lineY += 19;
            GUI.Label(new Rect(x + 12, lineY, w - 24, 18), "• [1] [2] [3] / Wheel: Switch Guns", bStyle); lineY += 19;
            GUI.Label(new Rect(x + 12, lineY, w - 24, 18), "• LMB: Shoot (M4A1 Auto Spray)", bStyle); lineY += 19;
            GUI.Label(new Rect(x + 12, lineY, w - 24, 18), "• RMB: Aim Down Sights (ADS)", bStyle); lineY += 19;
            GUI.Label(new Rect(x + 12, lineY, w - 24, 18), "• R: Tactical Reload", bStyle); lineY += 19;
            GUI.Label(new Rect(x + 12, lineY, w - 24, 18), "• L: Laser | F: Flashlight", bStyle); lineY += 19;
            GUI.Label(new Rect(x + 12, lineY, w - 24, 18), "• G: 45s Drill | N: Night-Ops", bStyle);
        }

        private void DrawTacticalDrillHUD()
        {
            var drillMgr = IVRAR.Group02.Environment.TacticalDrillGameManager.Instance;
            if (drillMgr == null) return;

            float cx = Screen.width / 2f;

            if (drillMgr.CurrentState == IVRAR.Group02.Environment.DrillState.Countdown)
            {
                GUI.color = Color.yellow;
                GUIStyle cdStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 42,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                int cdNum = Mathf.CeilToInt(drillMgr.CountdownTimer);
                string cdMsg = cdNum > 0 ? $"READY... {cdNum}" : "ENGAGE!";
                GUI.Label(new Rect(cx - 200, Screen.height * 0.35f, 400, 60), cdMsg, cdStyle);
            }
            else if (drillMgr.CurrentState == IVRAR.Group02.Environment.DrillState.ActiveDrill)
            {
                GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.90f);
                GUI.Box(new Rect(cx - 260, 15, 520, 48), GUIContent.none);

                GUI.color = Color.white;
                GUIStyle drillStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                string statLine = $"TIME: {drillMgr.TimeRemaining:F1}s  |  SCORE: {drillMgr.DrillScore}  |  HOSTILES: {drillMgr.HostilesNeutralized}  |  CIVILIANS SPARED: {drillMgr.CiviliansSpared}  |  d': {drillMgr.DPrimeScore:F2}";
                GUI.Label(new Rect(cx - 250, 25, 500, 28), statLine, drillStyle);
            }
            else if (drillMgr.CurrentState == IVRAR.Group02.Environment.DrillState.FreePractice)
            {
                GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.75f);
                GUI.Box(new Rect(cx - 210, 15, 420, 36), GUIContent.none);

                GUI.color = Color.cyan;
                GUIStyle fpStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                GUI.Label(new Rect(cx - 200, 20, 400, 24), "FREE PRACTICE: PRESS [G] TO START 45s DRILL  |  [N] NIGHT-OPS", fpStyle);
            }
            else if (drillMgr.CurrentState == IVRAR.Group02.Environment.DrillState.AfterActionReport)
            {
                float w = 480;
                float h = 330;
                float left = cx - w / 2f;
                float top = Screen.height / 2f - h / 2f;

                GUI.color = new Color(0.04f, 0.06f, 0.09f, 0.96f);
                GUI.Box(new Rect(left, top, w, h), GUIContent.none);

                GUI.color = Color.yellow;
                GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                GUI.Label(new Rect(left, top + 15, w, 28), "★ TACTICAL DRILL AFTER-ACTION REPORT ★", headerStyle);

                string rank = drillMgr.GetDrillRank();
                Color rankCol = rank == "S" ? new Color(1f, 0.85f, 0.1f) : (rank == "A" ? Color.green : (rank == "B" ? Color.cyan : Color.white));
                GUI.color = rankCol;
                GUIStyle rankStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 44,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                GUI.Label(new Rect(left, top + 45, w, 55), $"GRADE: {rank}", rankStyle);

                GUI.color = Color.white;
                GUIStyle repStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleLeft };
                float y = top + 105;
                GUI.Label(new Rect(left + 50, y, 380, 22), $"• Final Combat Score: {drillMgr.DrillScore} PTS", repStyle); y += 24;
                GUI.Label(new Rect(left + 50, y, 380, 22), $"• Hostiles Neutralized: {drillMgr.HostilesNeutralized}  |  Missed: {drillMgr.HostilesMissed}", repStyle); y += 24;
                GUI.Label(new Rect(left + 50, y, 380, 22), $"• Civilian Casualties: {drillMgr.CivilianCasualties}  |  Spared: {drillMgr.CiviliansSpared}", repStyle); y += 24;
                GUI.Label(new Rect(left + 50, y, 380, 22), $"• Average Reaction Latency: {drillMgr.AverageReactionLatencyMs:F1} ms", repStyle); y += 24;
                GUI.Label(new Rect(left + 50, y, 380, 22), $"• Signal Detection (d-prime): {drillMgr.DPrimeScore:F2}", repStyle); y += 24;
                GUI.Label(new Rect(left + 50, y, 380, 22), $"• Headshots: {drillMgr.HeadshotCount}", repStyle); y += 30;

                GUI.color = Color.cyan;
                GUIStyle promptStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                GUI.Label(new Rect(left, y, w, 26), "PRESS [G] TO START NEW DRILL  |  PRESS [N] TO TOGGLE NIGHT-OPS", promptStyle);
            }
        }

        private void OnDisable()
        {
            ApplyCursorLock(false);
            if (_musicSource != null) _musicSource.Stop();
            if (_laserLine != null) _laserLine.enabled = false;
            if (_laserDot != null) _laserDot.SetActive(false);
        }

        #endregion
    }
}
