using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using IVRAR.Group02.Core;

namespace IVRAR.Group02.UI
{
    /// <summary>
    /// Master UI controller for the IVRAR Group 02 VR training simulation startup menu.
    /// Manages navigation between Main Menu, 3D Shooting Range level, Participant Setup,
    /// Research Configuration, Team Credits, and Simulation Execution.
    /// Ensures all visual components, fonts, buttons, and backgrounds are rendered.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Menu Panels")]
        [SerializeField] private GameObject _mainMenuPanel;
        [SerializeField] private GameObject _participantSetupPanel;
        [SerializeField] private GameObject _researchConfigPanel;
        [SerializeField] private GameObject _creditsPanel;
        [SerializeField] private GameObject _attributionPanel;

        [Header("Main Menu Navigation Buttons")]
        [SerializeField] private Button _btnPlayShootingRange;
        [SerializeField] private Button _btnStartSimulation;
        [SerializeField] private Button _btnParticipantSetup;
        [SerializeField] private Button _btnResearchConfig;
        [SerializeField] private Button _btnCredits;
        [SerializeField] private Button _btnAttribution;
        [SerializeField] private Button _btnExit;

        [Header("Back Buttons")]
        [SerializeField] private Button _btnBackFromParticipant;
        [SerializeField] private Button _btnBackFromConfig;
        [SerializeField] private Button _btnBackFromCredits;
        [SerializeField] private Button _btnBackFromAttribution;

        [Header("Simulation Coordinator Reference")]
        [SerializeField] private SimulationManager _simulationManager;

        [Header("Title & Branding Labels")]
        [SerializeField] private Text _lblProjectTitle;
        [SerializeField] private Text _lblProjectSubtitle;

        public const string TITLE_TEXT = "IVRAR GROUP 02";
        public const string SUBTITLE_TEXT = "Immersive VR Sensory-Stress Defensive Security Training under Low Visibility";

        private Font _uiFont;
        private Sprite _whiteSprite;

        private void Awake()
        {
            if (_simulationManager == null) _simulationManager = FindObjectOfType<SimulationManager>();

            InitializeVisuals();
            BuildAndBindNavigation();
            ShowMainMenu();
        }

        private void InitializeVisuals()
        {
            // Resolve reliable UI font safely
            try
            {
                _uiFont = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch {}

            if (_uiFont == null)
            {
                try
                {
                    _uiFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
                }
                catch {}
            }

            if (_uiFont == null)
            {
                var allFonts = UnityEngine.Resources.FindObjectsOfTypeAll<Font>();
                if (allFonts != null && allFonts.Length > 0) _uiFont = allFonts[0];
            }

            // Create 1x1 white sprite for clean UI element rendering
            Texture2D tex = Texture2D.whiteTexture;
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

            // Ensure Canvas mode is optimal for display
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                // In desktop emulation or editor, ScreenSpaceOverlay guarantees immediate high-res visibility
                if (!UnityEngine.XR.XRSettings.isDeviceActive)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                }
                else
                {
                    canvas.renderMode = RenderMode.WorldSpace;
                    transform.position = new Vector3(0f, 1.5f, 2.0f);
                    transform.rotation = Quaternion.identity;
                    transform.localScale = Vector3.one * 0.0018f;
                }
            }

            // Ensure GraphicRaycaster
            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            // Ensure EventSystem exists in the scene
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        private void BuildAndBindNavigation()
        {
            // Build or refresh the Main Menu Panel
            BuildMainMenuVisuals();

            // Build or refresh Sub-Panels
            BuildParticipantSetupVisuals();
            BuildResearchConfigVisuals();
            BuildCreditsVisuals();
            BuildAttributionVisuals();
        }

        private void BuildMainMenuVisuals()
        {
            if (_mainMenuPanel == null)
            {
                _mainMenuPanel = new GameObject("MainMenuPanel", typeof(RectTransform));
                _mainMenuPanel.transform.SetParent(transform, false);
            }

            RectTransform rt = _mainMenuPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(850f, 620f);
            rt.anchoredPosition = Vector2.zero;

            // Panel Background
            Image bg = _mainMenuPanel.GetComponent<Image>();
            if (bg == null) bg = _mainMenuPanel.AddComponent<Image>();
            bg.sprite = _whiteSprite;
            bg.color = new Color(0.07f, 0.10f, 0.15f, 0.95f); // Deep sleek navy/slate

            // Remove old children to cleanly rebuild if needed
            ClearGeneratedChildren(_mainMenuPanel.transform);

            // 1. Header Box
            GameObject header = CreateChildObject(_mainMenuPanel, "HeaderBox", new Vector2(0f, 220f), new Vector2(800f, 120f));
            Text title = CreateText(header, "Title", TITLE_TEXT, 36, FontStyle.Bold, new Color(0.22f, 0.74f, 0.98f, 1f), new Vector2(0f, 20f), new Vector2(800f, 50f));
            _lblProjectTitle = title;

            Text subtitle = CreateText(header, "Subtitle", SUBTITLE_TEXT, 17, FontStyle.Normal, new Color(0.79f, 0.85f, 0.94f, 1f), new Vector2(0f, -25f), new Vector2(800f, 40f));
            _lblProjectSubtitle = subtitle;

            // 2. Buttons Container (Vertical layout)
            float startY = 110f;
            float stepY = 62f;

            // Button 1: Launch Full 3D Shooting Range
            _btnPlayShootingRange = CreateButton(_mainMenuPanel, "BtnPlayShootingRange", "🎮  LAUNCH 3D SHOOTING RANGE (FULL LEVEL)", new Vector2(0f, startY), new Vector2(560f, 52f),
                new Color(0.02f, 0.52f, 0.78f, 1f), new Color(0.03f, 0.65f, 0.95f, 1f), new Color(0.01f, 0.40f, 0.60f, 1f));
            _btnPlayShootingRange.onClick.AddListener(OnPlayShootingRangeClicked);

            // Button 2: Start Research Experiment Simulation
            _btnStartSimulation = CreateButton(_mainMenuPanel, "BtnStartSimulation", "🔬  START EXPERIMENT SIMULATION TRIALS", new Vector2(0f, startY - stepY), new Vector2(560f, 52f),
                new Color(0.05f, 0.58f, 0.41f, 1f), new Color(0.06f, 0.70f, 0.50f, 1f), new Color(0.04f, 0.45f, 0.32f, 1f));
            _btnStartSimulation.onClick.AddListener(OnStartSimulationClicked);

            // Button 3: Participant Setup
            _btnParticipantSetup = CreateButton(_mainMenuPanel, "BtnParticipantSetup", "📋  PARTICIPANT SETUP & METADATA", new Vector2(0f, startY - stepY * 2), new Vector2(560f, 48f),
                new Color(0.18f, 0.24f, 0.33f, 1f), new Color(0.25f, 0.33f, 0.45f, 1f), new Color(0.12f, 0.16f, 0.22f, 1f));
            _btnParticipantSetup.onClick.AddListener(ShowParticipantSetup);

            // Button 4: Research Configuration
            _btnResearchConfig = CreateButton(_mainMenuPanel, "BtnResearchConfig", "⚙️  RESEARCH CONFIGURATION & PRESETS", new Vector2(0f, startY - stepY * 3), new Vector2(560f, 48f),
                new Color(0.18f, 0.24f, 0.33f, 1f), new Color(0.25f, 0.33f, 0.45f, 1f), new Color(0.12f, 0.16f, 0.22f, 1f));
            _btnResearchConfig.onClick.AddListener(ShowResearchConfiguration);

            // Button 5: Credits & Attribution
            _btnCredits = CreateButton(_mainMenuPanel, "BtnCredits", "👥  ACADEMIC TEAM CREDITS & ROSTER", new Vector2(0f, startY - stepY * 4), new Vector2(560f, 48f),
                new Color(0.18f, 0.24f, 0.33f, 1f), new Color(0.25f, 0.33f, 0.45f, 1f), new Color(0.12f, 0.16f, 0.22f, 1f));
            _btnCredits.onClick.AddListener(ShowCredits);

            // Button 6: Exit
            _btnExit = CreateButton(_mainMenuPanel, "BtnExit", "🚪  EXIT SIMULATION", new Vector2(0f, startY - stepY * 5), new Vector2(560f, 46f),
                new Color(0.65f, 0.15f, 0.15f, 1f), new Color(0.80f, 0.20f, 0.20f, 1f), new Color(0.50f, 0.10f, 0.10f, 1f));
            _btnExit.onClick.AddListener(OnExitClicked);

            // Footer info
            CreateText(_mainMenuPanel, "Footer", "SVKM's NMIMS MPSTME • Department of Computer Engineering • AY2026-27", 13, FontStyle.Italic, new Color(0.50f, 0.58f, 0.69f, 1f), new Vector2(0f, -275f), new Vector2(800f, 30f));
        }

        private void BuildParticipantSetupVisuals()
        {
            if (_participantSetupPanel == null)
            {
                _participantSetupPanel = new GameObject("ParticipantSetupPanel", typeof(RectTransform));
                _participantSetupPanel.transform.SetParent(transform, false);
            }

            SetupSubPanelContainer(_participantSetupPanel);
            ClearGeneratedChildren(_participantSetupPanel.transform);

            CreateText(_participantSetupPanel, "Title", "📋  PARTICIPANT SETUP & PROTOCOL", 28, FontStyle.Bold, new Color(0.22f, 0.74f, 0.98f, 1f), new Vector2(0f, 220f), new Vector2(750f, 50f));

            string info = "Research Protocol (Ethical Clearance ID: IVRAR-G02-2026-004)\n\n" +
                          "• Active Trainee Code: P01_DEMO\n" +
                          "• Session Block: S01_BLOCK1\n" +
                          "• Active Condition: SensoryStress_Night (Condition B/C)\n" +
                          "• Monotonic Telemetry: 90 Hz Continuous Recording\n" +
                          "• Signal Detection Metrics: d', beta criterion, latency (ms)\n\n" +
                          "All participant records are pseudonymous and compliant with zero-PII policies.";

            CreateText(_participantSetupPanel, "Body", info, 18, FontStyle.Normal, new Color(0.85f, 0.90f, 0.96f, 1f), new Vector2(0f, 40f), new Vector2(700f, 260f), TextAnchor.MiddleLeft);

            _btnBackFromParticipant = CreateButton(_participantSetupPanel, "BtnBack", "← RETURN TO MAIN MENU", new Vector2(0f, -220f), new Vector2(300f, 50f),
                new Color(0.25f, 0.32f, 0.44f, 1f), new Color(0.35f, 0.45f, 0.60f, 1f), new Color(0.18f, 0.24f, 0.32f, 1f));
            _btnBackFromParticipant.onClick.AddListener(ShowMainMenu);
        }

        private void BuildResearchConfigVisuals()
        {
            if (_researchConfigPanel == null)
            {
                _researchConfigPanel = new GameObject("ResearchConfigPanel", typeof(RectTransform));
                _researchConfigPanel.transform.SetParent(transform, false);
            }

            SetupSubPanelContainer(_researchConfigPanel);
            ClearGeneratedChildren(_researchConfigPanel.transform);

            CreateText(_researchConfigPanel, "Title", "⚙️  RESEARCH CONFIGURATION", 28, FontStyle.Bold, new Color(0.22f, 0.74f, 0.98f, 1f), new Vector2(0f, 220f), new Vector2(750f, 50f));

            string info = "Independent Research Variables:\n\n" +
                          "1. Ambient Illumination: 0.5 - 5.0 simulated lux (Mesopic Nighttime)\n" +
                          "2. Volumetric Distance Fog: alpha = 0.045 m^-1 (Exp2 Fog)\n" +
                          "3. Acoustic Startle Cues: Spatialized 3D Gunfire/Siren (<85 dB SPL)\n" +
                          "4. Peripheral Strobe Distraction: 6.0 Hz pulse bursts (<= 6 pulses)\n" +
                          "5. Poisson Inter-Arrival Times: 1.5 - 5.0s randomized intervals\n" +
                          "6. Target Distribution: 60% Hostile (Shoot) / 40% Civilian (Withhold)";

            CreateText(_researchConfigPanel, "Body", info, 18, FontStyle.Normal, new Color(0.85f, 0.90f, 0.96f, 1f), new Vector2(0f, 40f), new Vector2(700f, 260f), TextAnchor.MiddleLeft);

            _btnBackFromConfig = CreateButton(_researchConfigPanel, "BtnBack", "← RETURN TO MAIN MENU", new Vector2(0f, -220f), new Vector2(300f, 50f),
                new Color(0.25f, 0.32f, 0.44f, 1f), new Color(0.35f, 0.45f, 0.60f, 1f), new Color(0.18f, 0.24f, 0.32f, 1f));
            _btnBackFromConfig.onClick.AddListener(ShowMainMenu);
        }

        private void BuildCreditsVisuals()
        {
            if (_creditsPanel == null)
            {
                _creditsPanel = new GameObject("CreditsPanel", typeof(RectTransform));
                _creditsPanel.transform.SetParent(transform, false);
            }

            SetupSubPanelContainer(_creditsPanel);
            ClearGeneratedChildren(_creditsPanel.transform);

            CreateText(_creditsPanel, "Title", "👥  ACADEMIC TEAM ROSTER", 28, FontStyle.Bold, new Color(0.22f, 0.74f, 0.98f, 1f), new Vector2(0f, 220f), new Vector2(750f, 50f));

            string info = "IVRAR Group 02 • SVKM's NMIMS MPSTME (AY2026-27)\n\n" +
                          "• N024 - Ranish Devadiga: Lead XR Systems Architect\n" +
                          "  (OpenXR integration, mesopic shaders, distance fog, strobe cues)\n\n" +
                          "• N042 - Hriday Jain: Human Factors & Usability Engineer\n" +
                          "  (NASA-TLX workload, Kennedy SSQ delta, SAGAT freeze probes)\n\n" +
                          "• N047 - Vedika Kaki: Spatial Telemetry & Data Lead\n" +
                          "  (90 Hz kinematic logging, millisecond latency, Signal Detection Theory)\n\n" +
                          "• N062 - Medha Mishra: Techno-Managerial Product Manager\n" +
                          "  (Techno-economic model, break-even amortization, analytics dashboard)";

            CreateText(_creditsPanel, "Body", info, 16, FontStyle.Normal, new Color(0.85f, 0.90f, 0.96f, 1f), new Vector2(0f, 30f), new Vector2(720f, 280f), TextAnchor.MiddleLeft);

            _btnBackFromCredits = CreateButton(_creditsPanel, "BtnBack", "← RETURN TO MAIN MENU", new Vector2(0f, -220f), new Vector2(300f, 50f),
                new Color(0.25f, 0.32f, 0.44f, 1f), new Color(0.35f, 0.45f, 0.60f, 1f), new Color(0.18f, 0.24f, 0.32f, 1f));
            _btnBackFromCredits.onClick.AddListener(ShowMainMenu);
        }

        private void BuildAttributionVisuals()
        {
            if (_attributionPanel == null)
            {
                _attributionPanel = new GameObject("AttributionPanel", typeof(RectTransform));
                _attributionPanel.transform.SetParent(transform, false);
            }

            SetupSubPanelContainer(_attributionPanel);
            ClearGeneratedChildren(_attributionPanel.transform);

            CreateText(_attributionPanel, "Title", "ORIGINAL BASELINE ATTRIBUTION", 26, FontStyle.Bold, new Color(0.22f, 0.74f, 0.98f, 1f), new Vector2(0f, 220f), new Vector2(750f, 50f));

            string info = "Original ShootingRangeVR mechanics developed by Anton Pokidov.\n" +
                          "Released under the MIT License.\n\n" +
                          "The original baseline scene (Assets/Scenes/ShootingRange/ShootingRange.unity) " +
                          "contains the complete physical weapon rig, ballistics, ammunition mechanics, and " +
                          "firing lanes preserved intact.";

            CreateText(_attributionPanel, "Body", info, 17, FontStyle.Normal, new Color(0.85f, 0.90f, 0.96f, 1f), new Vector2(0f, 40f), new Vector2(700f, 220f), TextAnchor.MiddleCenter);

            _btnBackFromAttribution = CreateButton(_attributionPanel, "BtnBack", "← RETURN TO MAIN MENU", new Vector2(0f, -220f), new Vector2(300f, 50f),
                new Color(0.25f, 0.32f, 0.44f, 1f), new Color(0.35f, 0.45f, 0.60f, 1f), new Color(0.18f, 0.24f, 0.32f, 1f));
            _btnBackFromAttribution.onClick.AddListener(ShowMainMenu);
        }

        private void SetupSubPanelContainer(GameObject panel)
        {
            RectTransform rt = panel.GetComponent<RectTransform>();
            if (rt == null) rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(850f, 620f);
            rt.anchoredPosition = Vector2.zero;

            Image bg = panel.GetComponent<Image>();
            if (bg == null) bg = panel.AddComponent<Image>();
            bg.sprite = _whiteSprite;
            bg.color = new Color(0.07f, 0.10f, 0.15f, 0.96f);
        }

        private void ClearGeneratedChildren(Transform parent)
        {
            int count = parent.childCount;
            for (int i = count - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }

        private GameObject CreateChildObject(GameObject parent, string name, Vector2 pos, Vector2 size)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent.transform, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return obj;
        }

        private Text CreateText(GameObject parent, string name, string content, int fontSize, FontStyle style, Color color, Vector2 pos, Vector2 size, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            GameObject obj = CreateChildObject(parent, name, pos, size);
            Text txt = obj.AddComponent<Text>();
            txt.text = content;
            txt.font = _uiFont;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = alignment;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Truncate;
            return txt;
        }

        private Button CreateButton(GameObject parent, string name, string labelText, Vector2 pos, Vector2 size, Color normalCol, Color hoverCol, Color pressCol)
        {
            GameObject obj = CreateChildObject(parent, name, pos, size);

            Image img = obj.AddComponent<Image>();
            img.sprite = _whiteSprite;
            img.color = normalCol;

            Button btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;

            ColorBlock cb = btn.colors;
            cb.normalColor = normalCol;
            cb.highlightedColor = hoverCol;
            cb.pressedColor = pressCol;
            cb.selectedColor = hoverCol;
            btn.colors = cb;

            // Label child
            CreateText(obj, "Label", labelText, 17, FontStyle.Bold, Color.white, Vector2.zero, size);

            return btn;
        }

        public void ShowMainMenu()
        {
            SetPanelState(main: true, participant: false, config: false, credits: false, attribution: false);
        }

        public void ShowParticipantSetup()
        {
            SetPanelState(main: false, participant: true, config: false, credits: false, attribution: false);
        }

        public void ShowResearchConfiguration()
        {
            SetPanelState(main: false, participant: false, config: true, credits: false, attribution: false);
        }

        public void ShowCredits()
        {
            SetPanelState(main: false, participant: false, config: false, credits: true, attribution: false);
        }

        public void ShowAttribution()
        {
            SetPanelState(main: false, participant: false, config: false, credits: false, attribution: true);
        }

        private void SetPanelState(bool main, bool participant, bool config, bool credits, bool attribution)
        {
            if (_mainMenuPanel != null) _mainMenuPanel.SetActive(main);
            if (_participantSetupPanel != null) _participantSetupPanel.SetActive(participant);
            if (_researchConfigPanel != null) _researchConfigPanel.SetActive(config);
            if (_creditsPanel != null) _creditsPanel.SetActive(credits);
            if (_attributionPanel != null) _attributionPanel.SetActive(attribution);
        }

        public void OnPlayShootingRangeClicked()
        {
            Debug.Log("[MainMenuController] Loading full 3D interactive ShootingRange scene...");
            if (SceneManager.sceneCountInBuildSettings > 1)
            {
                SceneManager.LoadScene(1);
            }
            else
            {
                SceneManager.LoadScene("ShootingRange");
            }
        }

        public void OnStartSimulationClicked()
        {
            if (_simulationManager != null)
            {
                SetPanelState(false, false, false, false, false);
                _simulationManager.StartSimulation();
            }
            else
            {
                OnPlayShootingRangeClicked();
            }
        }

        public void OnExitClicked()
        {
            Debug.Log("[MainMenuController] Exiting IVRAR Group 02 simulation.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
