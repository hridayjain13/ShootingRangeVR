using System;
using System.Collections;
using System.Collections.Generic;
using Interfaces;
using Resources;
using UnityEngine;
using IVRAR.Group02.XR;

namespace IVRAR.Group02.Environment
{
    /// <summary>
    /// Runtime Tactical Firing Range Environment Spawner.
    /// Procedurally constructs a modern CQB killhouse shooting range:
    /// - 3 selectable weapons and ammo crates on the shooting bench
    /// - 22 high-contrast reactive pop-up targets across 5 firing lanes
    /// - Dynamic moving tactical enemies patrolling between cover
    /// - Highly interactive explosive red hazmat barrels
    /// - Overhead industrial steel ceiling trusses and halogen floodlights
    /// - Rear weapon armory pegboard wall with mounted firearms
    /// - Concrete Jersey barriers, VTAC barricades, sandbag bunkers, and crates
    /// - Solid physics colliders preventing clipping
    /// </summary>
    public static class RangeEnvironmentRuntimeSpawner
    {
        private const string ROOT_NAME = "Tactical_ShootingRange_Environment";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void InitializeEnvironment()
        {
            SpawnEnvironment();
        }

        public static GameObject SpawnEnvironment()
        {
            // 1. Natural Directional Lighting
            Light dirLight = FindDirectionalLight();
            if (dirLight != null)
            {
                dirLight.color = new Color(0.98f, 0.95f, 0.90f);
                dirLight.intensity = 1.15f;
            }

            // 2. Ensure Player Camera has DesktopFallbackController and AudioListener
            EnsurePlayerDesktopController();

            // Ensure TacticalDrillGameManager is present
            if (UnityEngine.Object.FindObjectOfType<TacticalDrillGameManager>() == null)
            {
                GameObject drillObj = new GameObject("TacticalDrillGameManager");
                drillObj.AddComponent<TacticalDrillGameManager>();
            }

            GameObject root = GameObject.Find(ROOT_NAME);
            if (root != null)
            {
                if (root.transform.Find("Hurdle_JerseyBarrier_01") == null || root.transform.Find("MovingEnemy_Lane1") == null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
                else
                {
                    return root;
                }
            }

            root = new GameObject(ROOT_NAME);

            // 3. Firing Range Architecture (Floors, Backstop, Armory Wall, Overhead Trusses)
            BuildRangeArchitecture(root.transform);

            // 4. Balanced Overhead Floodlighting
            CreateCeilingLight(root.transform, "CeilingLight_Booth", new Vector3(0f, 4.4f, 1.5f), 16f, 2.4f);
            CreateCeilingLight(root.transform, "CeilingLight_MidRange", new Vector3(0f, 4.4f, 9.5f), 22f, 2.4f);
            CreateCeilingLight(root.transform, "CeilingLight_DeepRange", new Vector3(0f, 4.4f, 18.0f), 24f, 2.4f);
            CreateCeilingLight(root.transform, "CeilingLight_Backstop", new Vector3(0f, 4.4f, 27.0f), 24f, 2.4f);

            // 5. Shooting Bench with Glock, M4A1, Shotgun, and Ammo Crates
            BuildShootingBench(root.transform);

            // 6. Tactical Hurdles & Barricades with Explosive Barrels
            BuildTacticalHurdles(root.transform);

            // 7. Tactical Static Pop-Up Targets & Dynamic Moving Enemies
            BuildTacticalEnemies(root.transform);

            // 8. Scoring & Diagnostic Board
            BuildAccuracyBoard(root.transform);

            Debug.Log("[RangeEnvironmentRuntimeSpawner] Successfully spawned advanced CQB shooting range with weapons arsenal, moving enemies, and explosive props!");
            return root;
        }

        private static void BuildRangeArchitecture(Transform parent)
        {
            Material floorMat = CreateMaterial(new Color(0.22f, 0.24f, 0.26f, 1f));
            Material wallMat = CreateMaterial(new Color(0.26f, 0.28f, 0.32f, 1f));
            Material metalSteel = CreateMaterial(new Color(0.18f, 0.20f, 0.22f, 1f));
            Material neonCyan = CreateUnlitMaterial(new Color(0.1f, 0.85f, 1.0f, 1f));
            Material neonAmber = CreateUnlitMaterial(new Color(1.0f, 0.65f, 0.1f, 1f));

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

            // Rear Safety Wall Behind Player Booth (prevents falling off or wandering backward)
            GameObject rearWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rearWall.name = "RangeRearSafetyWall";
            rearWall.transform.SetParent(parent, false);
            rearWall.transform.position = new Vector3(0f, 3.5f, -3.8f);
            rearWall.transform.localScale = new Vector3(30f, 7f, 1.0f);
            if (wallMat != null) rearWall.GetComponent<Renderer>().material = wallMat;

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

            // Distance Markers along both walls (5m, 10m, 15m, 20m, 25m)
            Material signMat = CreateMaterial(new Color(0.85f, 0.70f, 0.15f, 1f));
            float[] distances = { 5f, 10f, 15f, 20f, 25f };
            foreach (float d in distances)
            {
                GameObject signL = GameObject.CreatePrimitive(PrimitiveType.Cube);
                signL.name = $"MarkerSign_{d}m_L";
                signL.transform.SetParent(parent, false);
                signL.transform.position = new Vector3(-14.4f, 2.2f, d);
                signL.transform.localScale = new Vector3(0.12f, 0.65f, 0.95f);
                if (signMat != null) signL.GetComponent<Renderer>().material = signMat;

                GameObject signR = GameObject.CreatePrimitive(PrimitiveType.Cube);
                signR.name = $"MarkerSign_{d}m_R";
                signR.transform.SetParent(parent, false);
                signR.transform.position = new Vector3(14.4f, 2.2f, d);
                signR.transform.localScale = new Vector3(0.12f, 0.65f, 0.95f);
                if (signMat != null) signR.GetComponent<Renderer>().material = signMat;
            }

            // Firing Line Hazard Stripe on Floor
            GameObject firingLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            firingLine.name = "FiringLineHazardStripe";
            firingLine.transform.SetParent(parent, false);
            firingLine.transform.position = new Vector3(0f, 0.02f, 0.55f);
            firingLine.transform.localScale = new Vector3(18f, 0.02f, 0.25f);
            if (signMat != null) firingLine.GetComponent<Renderer>().material = signMat;

            // Overhead Industrial Steel Ceiling Trusses
            float[] trussZ = { 4f, 11f, 18f, 25f };
            foreach (float z in trussZ)
            {
                GameObject truss = GameObject.CreatePrimitive(PrimitiveType.Cube);
                truss.name = $"CeilingTruss_{z}m";
                truss.transform.SetParent(parent, false);
                truss.transform.position = new Vector3(0f, 5.2f, z);
                truss.transform.localScale = new Vector3(29f, 0.45f, 0.45f);
                if (metalSteel != null) truss.GetComponent<Renderer>().material = metalSteel;
            }

            // Illuminated CQB Neon Sign on Backstop
            GameObject marquee = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marquee.name = "NeonMarqueeSign";
            marquee.transform.SetParent(parent, false);
            marquee.transform.position = new Vector3(0f, 6.2f, 32.2f);
            marquee.transform.localScale = new Vector3(14f, 1.4f, 0.15f);
            if (metalSteel != null) marquee.GetComponent<Renderer>().material = metalSteel;

            // Glowing Sign Borders
            GameObject borderTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            borderTop.name = "MarqueeBorderTop";
            borderTop.transform.SetParent(marquee.transform, false);
            borderTop.transform.localPosition = new Vector3(0f, 0.52f, -0.55f);
            borderTop.transform.localScale = new Vector3(0.98f, 0.06f, 0.2f);
            UnityEngine.Object.Destroy(borderTop.GetComponent<Collider>());
            if (neonCyan != null) borderTop.GetComponent<Renderer>().material = neonCyan;

            GameObject borderBottom = GameObject.CreatePrimitive(PrimitiveType.Cube);
            borderBottom.name = "MarqueeBorderBottom";
            borderBottom.transform.SetParent(marquee.transform, false);
            borderBottom.transform.localPosition = new Vector3(0f, -0.52f, -0.55f);
            borderBottom.transform.localScale = new Vector3(0.98f, 0.06f, 0.2f);
            UnityEngine.Object.Destroy(borderBottom.GetComponent<Collider>());
            if (neonAmber != null) borderBottom.GetComponent<Renderer>().material = neonAmber;

            // Rear Weapon Pegboard & Armory Wall Behind Firing Booth
            BuildArmoryWall(parent);
        }

        private static void BuildArmoryWall(Transform parent)
        {
            Material pegboardMat = CreateMaterial(new Color(0.18f, 0.19f, 0.22f, 1f));
            Material woodStock = CreateMaterial(new Color(0.42f, 0.28f, 0.18f, 1f));
            Material blackGun = CreateMaterial(new Color(0.12f, 0.12f, 0.14f, 1f));
            Material ammoGreen = CreateMaterial(new Color(0.24f, 0.35f, 0.22f, 1f));

            // Pegboard Panel
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "ArmoryPegboard";
            board.transform.SetParent(parent, false);
            board.transform.position = new Vector3(0f, 1.8f, -3.2f);
            board.transform.localScale = new Vector3(5.5f, 2.4f, 0.10f);
            if (pegboardMat != null) board.GetComponent<Renderer>().material = pegboardMat;

            // Mounted Rifles on Pegboard
            CreateMountedRifle(board.transform, new Vector3(-1.4f, 0.45f, -0.6f), blackGun);
            CreateMountedRifle(board.transform, new Vector3(1.4f, 0.45f, -0.6f), blackGun);
            CreateMountedShotgun(board.transform, new Vector3(0f, 0.0f, -0.6f), blackGun, woodStock);

            // Ammo Supply Crates on Floor Beside Armory
            CreateAmmoCrate(parent, new Vector3(-2.2f, 0.22f, -2.6f), "5.56x45 NATO", ammoGreen);
            CreateAmmoCrate(parent, new Vector3(2.2f, 0.22f, -2.6f), "12 GA BUCK", ammoGreen);
            CreateAmmoCrate(parent, new Vector3(2.2f, 0.62f, -2.6f), "9x19 LUGER", ammoGreen);
        }

        private static void CreateMountedRifle(Transform parent, Vector3 localPos, Material mat)
        {
            GameObject rifle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rifle.name = "Mounted_M4A1";
            rifle.transform.SetParent(parent, false);
            rifle.transform.localPosition = localPos;
            rifle.transform.localScale = new Vector3(0.35f, 0.07f, 0.08f);
            UnityEngine.Object.Destroy(rifle.GetComponent<Collider>());
            if (mat != null) rifle.GetComponent<Renderer>().material = mat;
        }

        private static void CreateMountedShotgun(Transform parent, Vector3 localPos, Material metal, Material wood)
        {
            GameObject shotgun = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shotgun.name = "Mounted_Shotgun";
            shotgun.transform.SetParent(parent, false);
            shotgun.transform.localPosition = localPos;
            shotgun.transform.localScale = new Vector3(0.42f, 0.06f, 0.08f);
            UnityEngine.Object.Destroy(shotgun.GetComponent<Collider>());
            if (metal != null) shotgun.GetComponent<Renderer>().material = metal;
        }

        private static void CreateAmmoCrate(Transform parent, Vector3 pos, string label, Material mat)
        {
            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = $"AmmoCrate_{label}";
            crate.transform.SetParent(parent, false);
            crate.transform.position = pos;
            crate.transform.localScale = new Vector3(0.65f, 0.38f, 0.42f);
            if (mat != null) crate.GetComponent<Renderer>().material = mat;
        }

        private static void BuildShootingBench(Transform parent)
        {
            Material benchMat = CreateMaterial(new Color(0.20f, 0.15f, 0.12f, 1f));

            // Shooting Bench with solid BoxCollider
            GameObject bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bench.name = "ShootingBench";
            bench.transform.SetParent(parent, false);
            bench.transform.position = new Vector3(0f, 0.45f, 0.95f);
            bench.transform.localScale = new Vector3(3.2f, 0.9f, 0.85f);
            if (benchMat != null) bench.GetComponent<Renderer>().material = benchMat;

            // [1] Glock 17 Props
            CreateVisualGlockProp(parent, new Vector3(-0.75f, 0.93f, 0.95f), "Bench_Glock17_Primary");

            // [2] M4A1 Tactical Carbine Prop on Bench
            CreateVisualCarbineProp(parent, new Vector3(0.0f, 0.94f, 0.95f), "Bench_M4A1_Carbine");

            // [3] Remington 870 Shotgun Prop on Bench
            CreateVisualShotgunProp(parent, new Vector3(0.75f, 0.94f, 0.95f), "Bench_Remington870");

            // Ammo Magazines & Boxes
            CreateVisualMagProp(parent, new Vector3(-1.1f, 0.92f, 0.95f), "Bench_PistolMag_A");
            CreateVisualMagProp(parent, new Vector3(-0.4f, 0.92f, 0.95f), "Bench_RifleMag_STANAG");
            CreateAmmoBoxSmall(parent, new Vector3(1.2f, 0.93f, 0.95f), "ShotgunShellBox");
        }

        private static void BuildTacticalHurdles(Transform parent)
        {
            // 1. VTAC Wooden Shooting Barricades near firing line
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

            // 5. Interactive Explosive Hazmat Barrels (Red drums that blow up when shot)
            CreateExplosiveBarrel(parent, "Hazmat_Explosive_NearLeft", new Vector3(-4.2f, 0f, 8.2f));
            CreateExplosiveBarrel(parent, "Hazmat_Explosive_MidRight", new Vector3(4.2f, 0f, 11.5f));
            CreateExplosiveBarrel(parent, "Hazmat_Explosive_CenterCover", new Vector3(1.4f, 0f, 16.5f));
            CreateExplosiveBarrel(parent, "Hazmat_Explosive_DeepLeft", new Vector3(-2.5f, 0f, 23.2f));
            CreateExplosiveBarrel(parent, "Hazmat_Explosive_DeepRight", new Vector3(3.8f, 0f, 24.0f));

            // 6. Heavy Cargo & Weapon Crates
            CreateCrateStack(parent, "Hurdle_Crates_Left", new Vector3(-2.0f, 0f, 18.5f));
            CreateCrateStack(parent, "Hurdle_Crates_Right", new Vector3(4.2f, 0f, 20.0f));
            CreateCrateStack(parent, "Hurdle_Crates_Far", new Vector3(0f, 0f, 25.0f));
        }

        private static void BuildTacticalEnemies(Transform parent)
        {
            // 22 Static / Drill Pop-Up Targets
            var targets = new (string name, Vector3 pos, bool hasSway)[]
            {
                // Lane 1: Far Left (x = -5.0m)
                ("Enemy_L1_Near", new Vector3(-5.0f, 0f, 7.5f), false),
                ("Enemy_L1_Mid1", new Vector3(-4.8f, 0f, 13.5f), true),
                ("Enemy_L1_Mid2", new Vector3(-5.2f, 0f, 19.5f), false),
                ("Enemy_L1_Deep", new Vector3(-5.0f, 0f, 25.5f), true),

                // Lane 2: Mid Left (x = -2.5m)
                ("Enemy_L2_Near", new Vector3(-2.5f, 0f, 6.0f), false),
                ("Enemy_L2_Cover", new Vector3(-2.8f, 0f, 10.5f), true),
                ("Enemy_L2_Mid", new Vector3(-2.3f, 0f, 15.0f), false),
                ("Enemy_L2_Deep1", new Vector3(-2.6f, 0f, 20.0f), true),
                ("Enemy_L2_Deep2", new Vector3(-2.4f, 0f, 26.0f), false),

                // Lane 3: Center (x = 0.0m)
                ("Enemy_Center_NearPointBlank", new Vector3(0.0f, 0f, 5.2f), false),
                ("Enemy_Center_BunkerCover", new Vector3(0.0f, 0f, 10.0f), true),
                ("Enemy_Center_Mid", new Vector3(0.0f, 0f, 15.5f), false),
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

            foreach (var e in targets)
            {
                CreateHighVisibilityEnemy(parent, e.name, e.pos, e.hasSway);
            }

            // 3 Dynamic Moving Tactical Combat Enemies that patrol between cover
            CreateMovingCombatEnemy(parent, "MovingEnemy_Lane1", new Vector3(-3.5f, 0f, 9.0f), 2.2f);
            CreateMovingCombatEnemy(parent, "MovingEnemy_Lane3", new Vector3(0.0f, 0f, 14.5f), 2.6f);
            CreateMovingCombatEnemy(parent, "MovingEnemy_Lane5", new Vector3(3.8f, 0f, 19.0f), 2.8f);
        }

        private static void CreateMovingCombatEnemy(Transform parent, string name, Vector3 pos, float speed)
        {
            GameObject enemy = new GameObject(name);
            enemy.transform.SetParent(parent, false);
            enemy.transform.position = pos;

            Material steelMat = CreateMaterial(new Color(0.25f, 0.27f, 0.30f, 1f));
            Material camoMat = CreateMaterial(new Color(0.18f, 0.25f, 0.18f, 1f)); // tactical camo
            Material headMat = CreateMaterial(new Color(0.15f, 0.22f, 0.35f, 1f));

            // Torso
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.name = "Torso";
            torso.transform.SetParent(enemy.transform, false);
            torso.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            torso.transform.localScale = new Vector3(0.55f, 0.74f, 0.18f);
            if (camoMat != null) torso.GetComponent<Renderer>().material = camoMat;

            // Head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(enemy.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.86f, 0f);
            head.transform.localScale = new Vector3(0.32f, 0.34f, 0.28f);
            if (headMat != null) head.GetComponent<Renderer>().material = headMat;

            // Legs Stand
            GameObject legs = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            legs.name = "Legs";
            legs.transform.SetParent(enemy.transform, false);
            legs.transform.localPosition = new Vector3(0f, 0.50f, 0f);
            legs.transform.localScale = new Vector3(0.12f, 0.50f, 0.12f);
            UnityEngine.Object.Destroy(legs.GetComponent<Collider>());
            if (steelMat != null) legs.GetComponent<Renderer>().material = steelMat;

            // AI Movement Controller
            enemy.AddComponent<MovingEnemyController>();
        }

        private static void CreateExplosiveBarrel(Transform parent, string name, Vector3 pos)
        {
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = name;
            barrel.transform.SetParent(parent, false);
            barrel.transform.position = pos + Vector3.up * 0.48f;
            barrel.transform.localScale = new Vector3(0.62f, 0.48f, 0.62f);

            Material redHazmat = CreateMaterial(new Color(0.85f, 0.15f, 0.12f, 1f));
            Material yellowStripe = CreateMaterial(new Color(0.95f, 0.80f, 0.15f, 1f));

            if (redHazmat != null) barrel.GetComponent<Renderer>().material = redHazmat;

            // Hazardous Warning Band
            GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            band.name = "HazardStripeBand";
            band.transform.SetParent(barrel.transform, false);
            band.transform.localPosition = Vector3.zero;
            band.transform.localScale = new Vector3(1.02f, 0.22f, 1.02f);
            UnityEngine.Object.Destroy(band.GetComponent<Collider>());
            if (yellowStripe != null) band.GetComponent<Renderer>().material = yellowStripe;

            // Explosive Barrel Logic
            barrel.AddComponent<ExplosiveBarrel>();
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

            // Heavy Steel Base
            GameObject baseStand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseStand.name = "MountBase";
            baseStand.transform.SetParent(enemy.transform, false);
            baseStand.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            baseStand.transform.localScale = new Vector3(0.55f, 0.10f, 0.45f);
            UnityEngine.Object.Destroy(baseStand.GetComponent<Collider>());
            if (steelMat != null) baseStand.GetComponent<Renderer>().material = steelMat;

            // Mast
            GameObject mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mast.name = "Mast";
            mast.transform.SetParent(enemy.transform, false);
            mast.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            mast.transform.localScale = new Vector3(0.08f, 0.55f, 0.08f);
            UnityEngine.Object.Destroy(mast.GetComponent<Collider>());
            if (steelMat != null) mast.GetComponent<Renderer>().material = steelMat;

            // High-Contrast Torso Board
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.name = "Torso";
            torso.transform.SetParent(enemy.transform, false);
            torso.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            torso.transform.localScale = new Vector3(0.52f, 0.72f, 0.08f);
            if (torsoNavyMat != null) torso.GetComponent<Renderer>().material = torsoNavyMat;

            // Rings
            GameObject ring8 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring8.name = "Ring_Outer_White";
            ring8.transform.SetParent(torso.transform, false);
            ring8.transform.localPosition = new Vector3(0f, 0.05f, -0.52f);
            ring8.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ring8.transform.localScale = new Vector3(0.68f, 0.02f, 0.68f);
            UnityEngine.Object.Destroy(ring8.GetComponent<Collider>());
            if (ringWhiteMat != null) ring8.GetComponent<Renderer>().material = ringWhiteMat;

            GameObject ring9 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring9.name = "Ring_Inner_Amber";
            ring9.transform.SetParent(torso.transform, false);
            ring9.transform.localPosition = new Vector3(0f, 0.05f, -0.54f);
            ring9.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ring9.transform.localScale = new Vector3(0.44f, 0.025f, 0.44f);
            UnityEngine.Object.Destroy(ring9.GetComponent<Collider>());
            if (ringAmberMat != null) ring9.GetComponent<Renderer>().material = ringAmberMat;

            GameObject bullseye = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bullseye.name = "Bullseye_Center_Red";
            bullseye.transform.SetParent(torso.transform, false);
            bullseye.transform.localPosition = new Vector3(0f, 0.05f, -0.56f);
            bullseye.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            bullseye.transform.localScale = new Vector3(0.22f, 0.03f, 0.22f);
            UnityEngine.Object.Destroy(bullseye.GetComponent<Collider>());
            if (bullseyeRedMat != null) bullseye.GetComponent<Renderer>().material = bullseyeRedMat;

            // Head with dedicated headshot collider
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(enemy.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.84f, 0f);
            head.transform.localScale = new Vector3(0.28f, 0.30f, 0.24f);
            if (torsoNavyMat != null) head.GetComponent<Renderer>().material = torsoNavyMat;

            // Headshot Red Dot
            GameObject headMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            headMarker.name = "HeadshotMarker";
            headMarker.transform.SetParent(head.transform, false);
            headMarker.transform.localPosition = new Vector3(0f, 0.05f, -0.48f);
            headMarker.transform.localScale = new Vector3(0.32f, 0.32f, 0.32f);
            UnityEngine.Object.Destroy(headMarker.GetComponent<Collider>());
            if (bullseyeRedMat != null) headMarker.GetComponent<Renderer>().material = bullseyeRedMat;

            // Pop-Up Controller
            enemy.AddComponent<TargetPopUpController>();
        }

        private static void CreateJerseyBarrier(Transform parent, string name, Vector3 pos, float yaw)
        {
            GameObject barrier = new GameObject(name);
            barrier.transform.SetParent(parent, false);
            barrier.transform.position = pos;
            barrier.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            Material concrete = CreateMaterial(new Color(0.45f, 0.46f, 0.48f, 1f));
            Material hazardStripe = CreateMaterial(new Color(0.85f, 0.70f, 0.15f, 1f));

            GameObject bBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bBase.name = "Base";
            bBase.transform.SetParent(barrier.transform, false);
            bBase.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            bBase.transform.localScale = new Vector3(3.0f, 0.50f, 0.65f);
            if (concrete != null) bBase.GetComponent<Renderer>().material = concrete;

            GameObject bTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bTop.name = "Wall";
            bTop.transform.SetParent(barrier.transform, false);
            bTop.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            bTop.transform.localScale = new Vector3(3.0f, 0.60f, 0.28f);
            if (concrete != null) bTop.GetComponent<Renderer>().material = concrete;

            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = "HazardStripe";
            stripe.transform.SetParent(barrier.transform, false);
            stripe.transform.localPosition = new Vector3(0f, 0.75f, -0.15f);
            stripe.transform.localScale = new Vector3(2.6f, 0.18f, 0.02f);
            UnityEngine.Object.Destroy(stripe.GetComponent<Collider>());
            if (hazardStripe != null) stripe.GetComponent<Renderer>().material = hazardStripe;
        }

        private static void CreateVtacBarricade(Transform parent, string name, Vector3 pos, float yaw)
        {
            GameObject vtac = new GameObject(name);
            vtac.transform.SetParent(parent, false);
            vtac.transform.position = pos;
            vtac.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            Material woodMat = CreateMaterial(new Color(0.48f, 0.36f, 0.24f, 1f));

            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "VTAC_Board";
            board.transform.SetParent(vtac.transform, false);
            board.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            board.transform.localScale = new Vector3(1.8f, 2.1f, 0.06f);
            if (woodMat != null) board.GetComponent<Renderer>().material = woodMat;

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

            for (int i = -2; i <= 2; i++)
            {
                GameObject bag = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bag.name = $"Sandbag_L1_{i}";
                bag.transform.SetParent(bunker.transform, false);
                bag.transform.localPosition = new Vector3(i * 0.52f, 0.15f, 0f);
                bag.transform.localScale = new Vector3(0.50f, 0.28f, 0.38f);
                if (burlap != null) bag.GetComponent<Renderer>().material = burlap;
            }

            for (int i = -1; i <= 2; i++)
            {
                GameObject bag = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bag.name = $"Sandbag_L2_{i}";
                bag.transform.SetParent(bunker.transform, false);
                bag.transform.localPosition = new Vector3(i * 0.52f - 0.26f, 0.42f, 0f);
                bag.transform.localScale = new Vector3(0.50f, 0.28f, 0.36f);
                if (burlap != null) bag.GetComponent<Renderer>().material = burlap;
            }

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
            Material greenMetal = CreateMaterial(new Color(0.24f, 0.40f, 0.26f, 1f));

            CreateSingleDrum(cluster.transform, "Drum_1", new Vector3(-0.35f, 0.48f, 0f), blueMetal);
            CreateSingleDrum(cluster.transform, "Drum_2", new Vector3(0.35f, 0.48f, 0f), greenMetal);
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

            GameObject c1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c1.name = "Crate_Base_1";
            c1.transform.SetParent(stack.transform, false);
            c1.transform.localPosition = new Vector3(-0.45f, 0.45f, 0f);
            c1.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            if (woodCrate != null) c1.GetComponent<Renderer>().material = woodCrate;

            GameObject c2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c2.name = "Crate_Base_2";
            c2.transform.SetParent(stack.transform, false);
            c2.transform.localPosition = new Vector3(0.45f, 0.45f, 0f);
            c2.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            if (woodCrate != null) c2.GetComponent<Renderer>().material = woodCrate;

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
            foreach (var light in UnityEngine.Object.FindObjectsOfType<Light>())
            {
                if (light.type == LightType.Directional) return light;
            }
            return null;
        }

        private static void EnsurePlayerDesktopController()
        {
            Camera cam = Camera.main ?? UnityEngine.Object.FindObjectOfType<Camera>();
            if (cam == null) return;

            if (cam.GetComponent<AudioListener>() == null)
            {
                cam.gameObject.AddComponent<AudioListener>();
            }

            if (UnityEngine.Object.FindObjectOfType<DesktopFallbackController>() == null)
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

        private static Material CreateUnlitMaterial(Color col)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
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

        private static void CreateVisualCarbineProp(Transform parent, Vector3 pos, string name)
        {
            GameObject prop = new GameObject(name);
            prop.transform.SetParent(parent);
            prop.transform.position = pos;
            prop.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            Material steel = CreateMaterial(new Color(0.12f, 0.12f, 0.14f, 1f));

            // Receiver & Barrel
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(prop.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            body.transform.localScale = new Vector3(0.04f, 0.08f, 0.65f);
            if (steel != null) body.GetComponent<Renderer>().material = steel;
        }

        private static void CreateVisualShotgunProp(Transform parent, Vector3 pos, string name)
        {
            GameObject prop = new GameObject(name);
            prop.transform.SetParent(parent);
            prop.transform.position = pos;
            prop.transform.rotation = Quaternion.Euler(0f, 85f, 0f);

            Material steel = CreateMaterial(new Color(0.14f, 0.14f, 0.16f, 1f));

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(prop.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            body.transform.localScale = new Vector3(0.04f, 0.07f, 0.85f);
            if (steel != null) body.GetComponent<Renderer>().material = steel;
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

        private static void CreateAmmoBoxSmall(Transform parent, Vector3 pos, string name)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent);
            box.transform.position = pos;
            box.transform.localScale = new Vector3(0.18f, 0.08f, 0.14f);
            Material m = CreateMaterial(new Color(0.85f, 0.15f, 0.12f, 1f));
            if (m != null) box.GetComponent<Renderer>().material = m;
        }
    }
}
