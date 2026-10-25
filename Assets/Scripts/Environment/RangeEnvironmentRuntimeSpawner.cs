using System;
using System.Collections;
using System.Collections.Generic;
using Interfaces;
using Resources;
using Targets;
using UnityEngine;
using IVRAR.Group02.XR;

namespace IVRAR.Group02.Environment
{
    /// <summary>
    /// Advanced Shooting Range Environment Spawner.
    /// Spawns:
    /// - 22 High-visibility tactical mannequin enemies across 5 lanes with headshot and bullseye zones
    /// - Multiple tactical hurdles: Concrete Jersey barriers, VTAC 9-hole shooting barricades,
    ///   stacked steel oil drums, sandbag parapets, and heavy wooden cargo crates
    /// - Balanced, atmospheric lighting (crisp, comfortable, natural illumination without washed-out glare)
    /// - Shooting bench with Glock 17 pistols and 9x19 magazines
    /// - Range structure (reinforced backstop, tactical sidewalls, concrete floor)
    /// - Automatically initializes on Play mode via RuntimeInitializeOnLoadMethod
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class RangeEnvironmentRuntimeSpawner : MonoBehaviour
    {
        private const string ROOT_NAME = "ShootingRangeEnvironment";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitializeOnSceneLoad()
        {
            EnsureEnvironment();
        }

        private void Awake()
        {
            EnsureEnvironment();
        }

        public static GameObject EnsureEnvironment()
        {
            // 1. Balanced Atmospheric Lighting (comfortable, clear, not overexposed)
            RenderSettings.fog = false;
            RenderSettings.ambientLight = new Color(0.68f, 0.72f, 0.78f, 1f);
            RenderSettings.ambientIntensity = 0.82f;

            Light dirLight = FindDirectionalLight();
            if (dirLight != null)
            {
                dirLight.color = new Color(0.98f, 0.95f, 0.90f, 1f);
                dirLight.intensity = 1.15f;
            }

            // 2. Ensure Player Camera has DesktopFallbackController and AudioListener
            EnsurePlayerDesktopController();

            GameObject root = GameObject.Find(ROOT_NAME);
            if (root != null)
            {
                // If existing root doesn't have hurdles or has old targets, clean and rebuild
                if (root.transform.Find("Hurdle_JerseyBarrier_01") == null)
                {
                    DestroyImmediate(root);
                }
                else
                {
                    return root;
                }
            }

            root = new GameObject(ROOT_NAME);

            // 3. Firing Range Architecture
            BuildRangeArchitecture(root.transform);

            // 4. Balanced Overhead Floodlighting (4 distinct ceiling banks down the range)
            CreateCeilingLight(root.transform, "CeilingLight_Booth", new Vector3(0f, 4.4f, 1.5f), 16f, 2.4f);
            CreateCeilingLight(root.transform, "CeilingLight_MidRange", new Vector3(0f, 4.4f, 9.5f), 22f, 2.4f);
            CreateCeilingLight(root.transform, "CeilingLight_DeepRange", new Vector3(0f, 4.4f, 18.0f), 24f, 2.4f);
            CreateCeilingLight(root.transform, "CeilingLight_Backstop", new Vector3(0f, 4.4f, 27.0f), 24f, 2.4f);

            // 5. Shooting Bench with Guns and Ammo
            BuildShootingBench(root.transform);

            // 6. Tactical Hurdles & Barricades (Concrete Jersey barriers, VTAC walls, oil drums, crates, sandbags)
            BuildTacticalHurdles(root.transform);

            // 7. 22 High-Visibility Tactical Enemy Targets across 5 Lanes
            BuildTacticalEnemies(root.transform);

            // 8. Scoring & Diagnostic Board
            BuildAccuracyBoard(root.transform);

            Debug.Log("[RangeEnvironmentRuntimeSpawner] Successfully spawned advanced tactical shooting range with 22 enemies and tactical hurdles!");
            return root;
        }

        private static void BuildRangeArchitecture(Transform parent)
        {
            Material floorMat = CreateMaterial(new Color(0.22f, 0.24f, 0.26f, 1f));
            Material wallMat = CreateMaterial(new Color(0.26f, 0.28f, 0.32f, 1f));

            // Floor (28m wide x 38m long)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "RangeFloor";
            floor.transform.SetParent(parent, false);
            floor.transform.position = new Vector3(0f, 0f, 15f);
            floor.transform.localScale = new Vector3(3.0f, 1f, 4.0f);
            if (floorMat != null) floor.GetComponent<Renderer>().material = floorMat;

            // Backstop Wall
            GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWall.name = "RangeBackstop";
            backWall.transform.SetParent(parent, false);
            backWall.transform.position = new Vector3(0f, 4.0f, 33f);
            backWall.transform.localScale = new Vector3(30f, 8f, 1.2f);
            if (wallMat != null) backWall.GetComponent<Renderer>().material = wallMat;

            // Side Walls
            GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "RangeLeftWall";
            leftWall.transform.SetParent(parent, false);
            leftWall.transform.position = new Vector3(-15f, 4.0f, 15f);
            leftWall.transform.localScale = new Vector3(1f, 8f, 40f);
            if (wallMat != null) leftWall.GetComponent<Renderer>().material = wallMat;

            GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "RangeRightWall";
            rightWall.transform.SetParent(parent, false);
            rightWall.transform.position = new Vector3(15f, 4.0f, 15f);
            rightWall.transform.localScale = new Vector3(1f, 8f, 40f);
            if (wallMat != null) rightWall.GetComponent<Renderer>().material = wallMat;
        }

        private static void BuildShootingBench(Transform parent)
        {
            Material benchMat = CreateMaterial(new Color(0.20f, 0.15f, 0.12f, 1f));

            GameObject bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bench.name = "ShootingBench";
            bench.transform.SetParent(parent, false);
            bench.transform.position = new Vector3(0f, 0.45f, 0.95f);
            bench.transform.localScale = new Vector3(2.8f, 0.9f, 0.75f);
            if (benchMat != null) bench.GetComponent<Renderer>().material = benchMat;

            // Primary and Secondary Glock 17 props on bench
            CreateVisualGlockProp(parent, new Vector3(0.35f, 0.93f, 0.95f), "Bench_Glock17_Primary");
            CreateVisualGlockProp(parent, new Vector3(0.75f, 0.93f, 0.95f), "Bench_Glock17_Secondary");

            // Magazines on bench
            CreateVisualMagProp(parent, new Vector3(-0.25f, 0.92f, 0.95f), "Bench_Mag_A");
            CreateVisualMagProp(parent, new Vector3(-0.55f, 0.92f, 0.95f), "Bench_Mag_B");
        }

        private static void BuildTacticalHurdles(Transform parent)
        {
            // 1. VTAC Wooden Shooting Barricades near firing line (with shooting ports)
            CreateVtacBarricade(parent, "Hurdle_VTAC_Left", new Vector3(-1.8f, 0f, 4.5f), 12f);
            CreateVtacBarricade(parent, "Hurdle_VTAC_Right", new Vector3(1.8f, 0f, 4.5f), -12f);

            // 2. Concrete Jersey Barriers across lanes
            CreateJerseyBarrier(parent, "Hurdle_JerseyBarrier_01", new Vector3(-2.8f, 0f, 7.5f), 15f);
            CreateJerseyBarrier(parent, "Hurdle_JerseyBarrier_02", new Vector3(2.8f, 0f, 9.8f), -10f);
            CreateJerseyBarrier(parent, "Hurdle_JerseyBarrier_03", new Vector3(-3.2f, 0f, 15.5f), 8f);
            CreateJerseyBarrier(parent, "Hurdle_JerseyBarrier_04", new Vector3(3.0f, 0f, 18.0f), -12f);

            // 3. Tactical Sandbag Bunkers
            CreateSandbagBunker(parent, "Hurdle_Sandbag_Center", new Vector3(0f, 0f, 12.0f), 0f);
            CreateSandbagBunker(parent, "Hurdle_Sandbag_Deep", new Vector3(-1.5f, 0f, 22.0f), 20f);

            // 4. Clusters of Industrial Steel Oil Drums
            CreateDrumCluster(parent, "Hurdle_Drums_Left", new Vector3(-4.8f, 0f, 10.0f));
            CreateDrumCluster(parent, "Hurdle_Drums_Right", new Vector3(4.8f, 0f, 13.0f));
            CreateDrumCluster(parent, "Hurdle_Drums_Mid", new Vector3(1.2f, 0f, 14.8f));
            CreateDrumCluster(parent, "Hurdle_Drums_Deep", new Vector3(-4.2f, 0f, 23.5f));

            // 5. Heavy Cargo & Weapon Crates
            CreateCrateStack(parent, "Hurdle_Crates_Left", new Vector3(-2.0f, 0f, 18.5f));
            CreateCrateStack(parent, "Hurdle_Crates_Right", new Vector3(4.2f, 0f, 20.0f));
            CreateCrateStack(parent, "Hurdle_Crates_Far", new Vector3(0f, 0f, 25.0f));
        }

        private static void BuildTacticalEnemies(Transform parent)
        {
            var enemies = new List<(string name, Vector3 pos, bool hasSway)>
            {
                // Lane 1: Far Left (x = -5.0m)
                ("Enemy_L1_Near", new Vector3(-5.0f, 0f, 7.0f), false),
                ("Enemy_L1_Mid1", new Vector3(-5.2f, 0f, 12.5f), true),
                ("Enemy_L1_Mid2", new Vector3(-4.8f, 0f, 18.5f), false),
                ("Enemy_L1_Deep", new Vector3(-5.0f, 0f, 25.0f), true),

                // Lane 2: Mid Left (x = -2.5m)
                ("Enemy_L2_Near", new Vector3(-2.5f, 0f, 5.8f), false),
                ("Enemy_L2_Cover", new Vector3(-2.8f, 0f, 9.2f), true),
                ("Enemy_L2_Mid", new Vector3(-2.4f, 0f, 14.0f), false),
                ("Enemy_L2_Deep1", new Vector3(-2.6f, 0f, 20.0f), true),
                ("Enemy_L2_Deep2", new Vector3(-2.5f, 0f, 27.0f), false),

                // Lane 3: Center (x = 0.0m)
                ("Enemy_Center_Close", new Vector3(0.0f, 0f, 5.0f), false),
                ("Enemy_Center_Bunker1", new Vector3(0.0f, 0f, 10.0f), true),
                ("Enemy_Center_Bunker2", new Vector3(0.0f, 0f, 15.0f), false),
                ("Enemy_Center_Deep1", new Vector3(0.0f, 0f, 21.0f), true),
                ("Enemy_Center_Backstop", new Vector3(0.0f, 0f, 28.0f), false),

                // Lane 4: Mid Right (x = +2.5m)
                ("Enemy_L4_Near", new Vector3(2.5f, 0f, 6.2f), false),
                ("Enemy_L4_Cover", new Vector3(2.8f, 0f, 11.0f), true),
                ("Enemy_L4_Mid", new Vector3(2.4f, 0f, 16.5f), false),
                ("Enemy_L4_Deep1", new Vector3(2.6f, 0f, 22.0f), true),
                ("Enemy_L4_Deep2", new Vector3(2.5f, 0f, 27.5f), false),

                // Lane 5: Far Right (x = +5.0m)
                ("Enemy_L5_Near", new Vector3(5.0f, 0f, 8.0f), false),
                ("Enemy_L5_Mid1", new Vector3(4.8f, 0f, 14.5f), true),
                ("Enemy_L5_Mid2", new Vector3(5.2f, 0f, 20.5f), false),
                ("Enemy_L5_Deep", new Vector3(5.0f, 0f, 26.5f), true),
            };

            foreach (var e in enemies)
            {
                CreateHighVisibilityEnemy(parent, e.name, e.pos, e.hasSway);
            }
        }

        private static void CreateHighVisibilityEnemy(Transform parent, string name, Vector3 pos, bool hasSway)
        {
            GameObject enemy = new GameObject(name);
            enemy.transform.SetParent(parent, false);
            enemy.transform.position = pos;

            Material steelMat = CreateMaterial(new Color(0.25f, 0.27f, 0.30f, 1f));
            Material torsoNavyMat = CreateMaterial(new Color(0.12f, 0.22f, 0.38f, 1f));
            Material ringWhiteMat = CreateMaterial(new Color(0.95f, 0.95f, 0.95f, 1f));
            Material ringAmberMat = CreateMaterial(new Color(0.95f, 0.55f, 0.10f, 1f));
            Material bullseyeRedMat = CreateMaterial(new Color(0.92f, 0.15f, 0.15f, 1f));

            // 1. Heavy Steel Mounting Base at Ground Level
            GameObject baseStand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseStand.name = "MountBase";
            baseStand.transform.SetParent(enemy.transform, false);
            baseStand.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            baseStand.transform.localScale = new Vector3(0.55f, 0.10f, 0.45f);
            Destroy(baseStand.GetComponent<Collider>());
            if (steelMat != null) baseStand.GetComponent<Renderer>().material = steelMat;

            // 2. Upright Steel Mast
            GameObject mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mast.name = "Mast";
            mast.transform.SetParent(enemy.transform, false);
            mast.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            mast.transform.localScale = new Vector3(0.08f, 0.55f, 0.08f);
            Destroy(mast.GetComponent<Collider>());
            if (steelMat != null) mast.GetComponent<Renderer>().material = steelMat;

            // 3. High-Contrast Torso Board with Scoring Rings
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.name = "Torso";
            torso.transform.SetParent(enemy.transform, false);
            torso.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            torso.transform.localScale = new Vector3(0.52f, 0.72f, 0.08f);
            if (torsoNavyMat != null) torso.GetComponent<Renderer>().material = torsoNavyMat;

            // Outer White Ring (Ring 8)
            GameObject ring8 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring8.name = "Ring_Outer_White";
            ring8.transform.SetParent(torso.transform, false);
            ring8.transform.localPosition = new Vector3(0f, 0.05f, -0.52f);
            ring8.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ring8.transform.localScale = new Vector3(0.68f, 0.02f, 0.68f);
            Destroy(ring8.GetComponent<Collider>());
            if (ringWhiteMat != null) ring8.GetComponent<Renderer>().material = ringWhiteMat;

            // Inner Amber Ring (Ring 9)
            GameObject ring9 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring9.name = "Ring_Inner_Amber";
            ring9.transform.SetParent(torso.transform, false);
            ring9.transform.localPosition = new Vector3(0f, 0.05f, -0.54f);
            ring9.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ring9.transform.localScale = new Vector3(0.44f, 0.025f, 0.44f);
            Destroy(ring9.GetComponent<Collider>());
            if (ringAmberMat != null) ring9.GetComponent<Renderer>().material = ringAmberMat;

            // Center Bullseye (10 Ring)
            GameObject bullseye = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bullseye.name = "Bullseye_Center_Red";
            bullseye.transform.SetParent(torso.transform, false);
            bullseye.transform.localPosition = new Vector3(0f, 0.05f, -0.56f);
            bullseye.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            bullseye.transform.localScale = new Vector3(0.22f, 0.03f, 0.22f);
            Destroy(bullseye.GetComponent<Collider>());
            if (bullseyeRedMat != null) bullseye.GetComponent<Renderer>().material = bullseyeRedMat;

            // 4. Silhouette Head with Dedicated Headshot Collider
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(enemy.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.84f, 0f);
            head.transform.localScale = new Vector3(0.28f, 0.30f, 0.24f);
            if (torsoNavyMat != null) head.GetComponent<Renderer>().material = torsoNavyMat;

            // Headshot Red Dot Marker on Forehead
            GameObject headMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            headMarker.name = "HeadshotMarker";
            headMarker.transform.SetParent(head.transform, false);
            headMarker.transform.localPosition = new Vector3(0f, 0.05f, -0.48f);
            headMarker.transform.localScale = new Vector3(0.32f, 0.32f, 0.32f);
            Destroy(headMarker.GetComponent<Collider>());
            if (bullseyeRedMat != null) headMarker.GetComponent<Renderer>().material = bullseyeRedMat;

            // 5. Pulsing Red Tactical Indicator LED on Stand (Ensures 100% visibility even at long distance)
            GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beacon.name = "TacticalBeaconLED";
            beacon.transform.SetParent(enemy.transform, false);
            beacon.transform.localPosition = new Vector3(0f, 0.95f, -0.06f);
            beacon.transform.localScale = Vector3.one * 0.07f;
            Destroy(beacon.GetComponent<Collider>());
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (unlit != null)
            {
                Material ledMat = new Material(unlit) { color = new Color(1f, 0.2f, 0.2f, 1f) };
                beacon.GetComponent<Renderer>().material = ledMat;
            }

            // 6. Add Hit Reaction and IDamageable
            var hitReaction = enemy.AddComponent<TargetHitReaction>();
            hitReaction.SetHasSway(hasSway);
        }

        // ==========================================
        // TACTICAL HURDLES BUILDERS
        // ==========================================

        private static void CreateJerseyBarrier(Transform parent, string name, Vector3 pos, float yaw)
        {
            GameObject barrier = new GameObject(name);
            barrier.transform.SetParent(parent, false);
            barrier.transform.position = pos;
            barrier.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            Material concrete = CreateMaterial(new Color(0.45f, 0.46f, 0.48f, 1f));
            Material hazardStripe = CreateMaterial(new Color(0.85f, 0.70f, 0.15f, 1f));

            // Concrete Base
            GameObject bBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bBase.name = "Base";
            bBase.transform.SetParent(barrier.transform, false);
            bBase.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            bBase.transform.localScale = new Vector3(3.0f, 0.50f, 0.65f);
            if (concrete != null) bBase.GetComponent<Renderer>().material = concrete;

            // Concrete Upper Wall
            GameObject bTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bTop.name = "Wall";
            bTop.transform.SetParent(barrier.transform, false);
            bTop.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            bTop.transform.localScale = new Vector3(3.0f, 0.60f, 0.28f);
            if (concrete != null) bTop.GetComponent<Renderer>().material = concrete;

            // Hazard Warning Stripe
            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = "HazardStripe";
            stripe.transform.SetParent(barrier.transform, false);
            stripe.transform.localPosition = new Vector3(0f, 0.75f, -0.15f);
            stripe.transform.localScale = new Vector3(2.6f, 0.18f, 0.02f);
            Destroy(stripe.GetComponent<Collider>());
            if (hazardStripe != null) stripe.GetComponent<Renderer>().material = hazardStripe;
        }

        private static void CreateVtacBarricade(Transform parent, string name, Vector3 pos, float yaw)
        {
            GameObject vtac = new GameObject(name);
            vtac.transform.SetParent(parent, false);
            vtac.transform.position = pos;
            vtac.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            Material woodMat = CreateMaterial(new Color(0.48f, 0.36f, 0.24f, 1f));

            // Main Barricade Board (Height 2.1m x Width 1.8m)
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "VTAC_Board";
            board.transform.SetParent(vtac.transform, false);
            board.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            board.transform.localScale = new Vector3(1.8f, 2.1f, 0.06f);
            if (woodMat != null) board.GetComponent<Renderer>().material = woodMat;

            // Standing Shooting Window (Visual border)
            GameObject portStand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            portStand.name = "Port_Standing";
            portStand.transform.SetParent(vtac.transform, false);
            portStand.transform.localPosition = new Vector3(0.35f, 1.45f, -0.04f);
            portStand.transform.localScale = new Vector3(0.40f, 0.35f, 0.04f);
            Material blackHole = CreateMaterial(new Color(0.08f, 0.08f, 0.08f, 1f));
            if (blackHole != null) portStand.GetComponent<Renderer>().material = blackHole;

            // Kneeling Shooting Window
            GameObject portKneel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            portKneel.name = "Port_Kneeling";
            portKneel.transform.SetParent(vtac.transform, false);
            portKneel.transform.localPosition = new Vector3(-0.35f, 0.85f, -0.04f);
            portKneel.transform.localScale = new Vector3(0.35f, 0.30f, 0.04f);
            if (blackHole != null) portKneel.GetComponent<Renderer>().material = blackHole;

            // Rear Braces
            GameObject brace = GameObject.CreatePrimitive(PrimitiveType.Cube);
            brace.name = "RearBrace";
            brace.transform.SetParent(vtac.transform, false);
            brace.transform.localPosition = new Vector3(0f, 0.6f, -0.5f);
            brace.transform.localRotation = Quaternion.Euler(35f, 0f, 0f);
            brace.transform.localScale = new Vector3(0.08f, 1.4f, 0.08f);
            if (woodMat != null) brace.GetComponent<Renderer>().material = woodMat;
        }

        private static void CreateSandbagBunker(Transform parent, string name, Vector3 pos, float yaw)
        {
            GameObject bunker = new GameObject(name);
            bunker.transform.SetParent(parent, false);
            bunker.transform.position = pos;
            bunker.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            Material burlap = CreateMaterial(new Color(0.55f, 0.48f, 0.36f, 1f));

            // Layer 1
            for (int i = -2; i <= 2; i++)
            {
                GameObject bag = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bag.name = $"Sandbag_L1_{i}";
                bag.transform.SetParent(bunker.transform, false);
                bag.transform.localPosition = new Vector3(i * 0.52f, 0.15f, 0f);
                bag.transform.localScale = new Vector3(0.50f, 0.28f, 0.38f);
                if (burlap != null) bag.GetComponent<Renderer>().material = burlap;
            }

            // Layer 2
            for (int i = -1; i <= 2; i++)
            {
                GameObject bag = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bag.name = $"Sandbag_L2_{i}";
                bag.transform.SetParent(bunker.transform, false);
                bag.transform.localPosition = new Vector3(i * 0.52f - 0.26f, 0.42f, 0f);
                bag.transform.localScale = new Vector3(0.50f, 0.28f, 0.36f);
                if (burlap != null) bag.GetComponent<Renderer>().material = burlap;
            }

            // Layer 3
            for (int i = -1; i <= 1; i++)
            {
                GameObject bag = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bag.name = $"Sandbag_L3_{i}";
                bag.transform.SetParent(bunker.transform, false);
                bag.transform.localPosition = new Vector3(i * 0.52f, 0.68f, 0f);
                bag.transform.localScale = new Vector3(0.50f, 0.26f, 0.34f);
                if (burlap != null) bag.GetComponent<Renderer>().material = burlap;
            }
        }

        private static void CreateDrumCluster(Transform parent, string name, Vector3 pos)
        {
            GameObject cluster = new GameObject(name);
            cluster.transform.SetParent(parent, false);
            cluster.transform.position = pos;

            Material blueMetal = CreateMaterial(new Color(0.18f, 0.32f, 0.52f, 1f));
            Material redMetal = CreateMaterial(new Color(0.58f, 0.18f, 0.18f, 1f));
            Material greenMetal = CreateMaterial(new Color(0.24f, 0.40f, 0.26f, 1f));

            CreateSingleDrum(cluster.transform, "Drum_1", new Vector3(-0.35f, 0.48f, 0f), blueMetal);
            CreateSingleDrum(cluster.transform, "Drum_2", new Vector3(0.35f, 0.48f, 0f), redMetal);
            CreateSingleDrum(cluster.transform, "Drum_3", new Vector3(0f, 0.48f, 0.55f), greenMetal);
        }

        private static void CreateSingleDrum(Transform parent, string name, Vector3 pos, Material mat)
        {
            GameObject drum = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            drum.name = name;
            drum.transform.SetParent(parent, false);
            drum.transform.localPosition = pos;
            drum.transform.localScale = new Vector3(0.60f, 0.48f, 0.60f);
            if (mat != null) drum.GetComponent<Renderer>().material = mat;
        }

        private static void CreateCrateStack(Transform parent, string name, Vector3 pos)
        {
            GameObject stack = new GameObject(name);
            stack.transform.SetParent(parent, false);
            stack.transform.position = pos;

            Material woodCrate = CreateMaterial(new Color(0.42f, 0.32f, 0.20f, 1f));

            // Base Crate 1
            GameObject c1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c1.name = "Crate_Base_1";
            c1.transform.SetParent(stack.transform, false);
            c1.transform.localPosition = new Vector3(-0.45f, 0.45f, 0f);
            c1.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            if (woodCrate != null) c1.GetComponent<Renderer>().material = woodCrate;

            // Base Crate 2
            GameObject c2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c2.name = "Crate_Base_2";
            c2.transform.SetParent(stack.transform, false);
            c2.transform.localPosition = new Vector3(0.45f, 0.45f, 0f);
            c2.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            if (woodCrate != null) c2.GetComponent<Renderer>().material = woodCrate;

            // Top Crate
            GameObject cTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cTop.name = "Crate_Top";
            cTop.transform.SetParent(stack.transform, false);
            cTop.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            cTop.transform.localRotation = Quaternion.Euler(0f, 12f, 0f);
            cTop.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            if (woodCrate != null) cTop.GetComponent<Renderer>().material = woodCrate;
        }

        private static void BuildAccuracyBoard(Transform parent)
        {
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "AccuracyBoard";
            board.transform.SetParent(parent, false);
            board.transform.position = new Vector3(-2.4f, 1.2f, 2.2f);
            board.transform.localRotation = Quaternion.Euler(0f, 32f, 0f);
            board.transform.localScale = new Vector3(1.1f, 1.5f, 0.08f);

            Material boardMat = CreateMaterial(new Color(0.15f, 0.18f, 0.22f, 1f));
            if (boardMat != null) board.GetComponent<Renderer>().material = boardMat;
        }

        private static void CreateCeilingLight(Transform parent, string name, Vector3 pos, float range, float intensity)
        {
            GameObject lightObj = new GameObject(name);
            lightObj.transform.SetParent(parent, false);
            lightObj.transform.position = pos;
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = range;
            l.intensity = intensity;
            l.color = new Color(0.96f, 0.96f, 1.0f);
        }

        private static Light FindDirectionalLight()
        {
            foreach (var light in FindObjectsOfType<Light>())
            {
                if (light.type == LightType.Directional) return light;
            }
            return null;
        }

        private static void EnsurePlayerDesktopController()
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
            }
        }

        private static Material CreateMaterial(Color col)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (s == null) return null;
            Material m = new Material(s);
            m.color = col;
            return m;
        }

        private static void CreateVisualGlockProp(Transform parent, Vector3 pos, string name)
        {
            GameObject prop = new GameObject(name);
            prop.transform.SetParent(parent);
            prop.transform.position = pos;
            prop.transform.rotation = Quaternion.Euler(0f, 95f, 0f);

            GameObject slide = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slide.name = "Slide";
            slide.transform.SetParent(prop.transform);
            slide.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            slide.transform.localScale = new Vector3(0.035f, 0.045f, 0.20f);
            Material black = CreateMaterial(new Color(0.12f, 0.12f, 0.14f, 1f));
            if (black != null) slide.GetComponent<Renderer>().material = black;

            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grip.name = "Grip";
            grip.transform.SetParent(prop.transform);
            grip.transform.localPosition = new Vector3(0f, -0.045f, -0.05f);
            grip.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            grip.transform.localScale = new Vector3(0.032f, 0.12f, 0.048f);
            Material polymer = CreateMaterial(new Color(0.18f, 0.18f, 0.20f, 1f));
            if (polymer != null) grip.GetComponent<Renderer>().material = polymer;
        }

        private static void CreateVisualMagProp(Transform parent, Vector3 pos, string name)
        {
            GameObject mag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mag.name = name;
            mag.transform.SetParent(parent);
            mag.transform.position = pos;
            mag.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            mag.transform.localScale = new Vector3(0.028f, 0.11f, 0.038f);
            Material m = CreateMaterial(new Color(0.14f, 0.14f, 0.16f, 1f));
            if (m != null) mag.GetComponent<Renderer>().material = m;
        }
    }

    /// <summary>
    /// Interactive Hit Reaction for Shooting Range Targets.
    /// Supports headshot detection, physical tilt impulse, red flash, and subtle peek/sway movement.
    /// </summary>
    public class TargetHitReaction : MonoBehaviour, IDamageable
    {
        private bool _hasSway = false;
        private float _swayTimer = 0f;
        private Vector3 _basePos;
        private Quaternion _baseRot;

        private void Start()
        {
            _basePos = transform.position;
            _baseRot = transform.localRotation;
            _swayTimer = UnityEngine.Random.Range(0f, 6.28f);
        }

        public void SetHasSway(bool hasSway)
        {
            _hasSway = hasSway;
        }

        private void Update()
        {
            if (_hasSway)
            {
                _swayTimer += Time.deltaTime * 1.5f;
                // Subtle tactical peek side to side behind hurdles
                float xOffset = Mathf.Sin(_swayTimer) * 0.25f;
                transform.position = new Vector3(_basePos.x + xOffset, _basePos.y, _basePos.z);
            }
        }

        public void Damage(DamageData damageData)
        {
            StopAllCoroutines();
            StartCoroutine(HitImpulseCoroutine());
        }

        private IEnumerator HitImpulseCoroutine()
        {
            Quaternion orig = _baseRot;
            Quaternion tilt = orig * Quaternion.Euler(-10f, 0f, UnityEngine.Random.Range(-5f, 5f));

            // Flash red on hit
            var renderers = GetComponentsInChildren<Renderer>();
            var originalColors = new Dictionary<Renderer, Color>();
            foreach (var r in renderers)
            {
                if (r.material != null && r.material.HasProperty("_Color"))
                {
                    originalColors[r] = r.material.color;
                    r.material.color = Color.red;
                }
            }

            float t = 0f;
            while (t < 0.08f)
            {
                transform.localRotation = Quaternion.Slerp(orig, tilt, t / 0.08f);
                t += Time.deltaTime;
                yield return null;
            }

            // Restore colors
            foreach (var kvp in originalColors)
            {
                if (kvp.Key != null && kvp.Key.material != null && kvp.Key.material.HasProperty("_Color"))
                {
                    kvp.Key.material.color = kvp.Value;
                }
            }

            t = 0f;
            while (t < 0.16f)
            {
                transform.localRotation = Quaternion.Slerp(tilt, orig, t / 0.16f);
                t += Time.deltaTime;
                yield return null;
            }

            transform.localRotation = orig;
        }
    }
}
