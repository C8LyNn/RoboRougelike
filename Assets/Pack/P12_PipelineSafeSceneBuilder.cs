#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace RogueMachine.P12PipelineSafe.Editor
{
    public static class P12PipelineSafeSceneBuilder
    {
        private const float BlockSize = 0.55f;
        private const float WheelRadius = 0.43f;
        private const float WheelWidth = 0.38f;

        private const string MaterialFolder =
            "Assets/RogueMachineGenerated_P12_PipelineSafe";

        private static Material hullMat;
        private static Material upperMat;
        private static Material sideMat;
        private static Material turretMat;
        private static Material barrelMat;
        private static Material wheelMat;
        private static Material groundMat;
        private static Material targetMat;
        private static Material targetAltMat;
        private static Material enemyMat;
        private static Material enemyCoreMat;
        private static Material playerTracerMat;
        private static Material enemyTracerMat;

        [MenuItem("Tools/Rogue Machine/[12] Pipeline Safe/RESET SCENE")]
        public static void ResetScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning(
                    "[Pack 12] Stop Play Mode before resetting."
                );
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (!CreateMaterials())
            {
                Debug.LogError(
                    "[Pack 12] Material creation failed. Scene reset aborted to avoid magenta materials."
                );
                return;
            }

            EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single
            );

            BuildGround();
            BuildLight();

            Transform turretYaw;
            Transform cannonPitch;
            Transform playerMuzzle;
            Transform cameraPivot;

            GameObject player =
                BuildPlayer(
                    out turretYaw,
                    out cannonPitch,
                    out playerMuzzle,
                    out cameraPivot
                );

            Camera camera =
                BuildCamera(
                    player.transform,
                    cameraPivot
                );

            player.GetComponent<
                P12PipelineSafeTurret
            >().Configure(
                player.transform,
                turretYaw,
                cannonPitch,
                camera
            );

            player.GetComponent<
                P12PipelineSafePlayerGun
            >().Configure(
                camera,
                playerMuzzle,
                player.transform,
                playerTracerMat
            );

            BuildTargetWalls();

            BuildEnemyTurret(
                player.transform,
                new Vector3(
                    0f,
                    0f,
                    17f
                )
            );

            camera.gameObject.AddComponent<
                P12PipelineSafeHUD
            >();

            Selection.activeGameObject = player;

            EditorSceneManager.MarkSceneDirty(
                SceneManager.GetActiveScene()
            );

            Debug.Log(
                "[Pack 12 Pipeline Safe] Scene ready with materials cloned from the active render pipeline."
            );
        }

        private static bool CreateMaterials()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
            {
                AssetDatabase.CreateFolder(
                    "Assets",
                    "RogueMachineGenerated_P12_PipelineSafe"
                );
            }

            Material template =
                FindActivePipelineMaterial();

            if (template == null ||
                template.shader == null ||
                !template.shader.isSupported)
            {
                Debug.LogError(
                    "[Pack 12] Could not obtain a supported default material from the active render pipeline."
                );
                return false;
            }

            Debug.Log(
                "[Pack 12] Active material template: " +
                template.name +
                " | Shader: " +
                template.shader.name
            );

            hullMat = Mat(
                "P12_Hull.mat",
                template,
                new Color(0.29f, 0.34f, 0.31f, 1f)
            );

            upperMat = Mat(
                "P12_Upper.mat",
                template,
                new Color(0.36f, 0.41f, 0.37f, 1f)
            );

            sideMat = Mat(
                "P12_Side.mat",
                template,
                new Color(0.18f, 0.22f, 0.20f, 1f)
            );

            turretMat = Mat(
                "P12_Turret.mat",
                template,
                new Color(0.25f, 0.29f, 0.27f, 1f)
            );

            barrelMat = Mat(
                "P12_Barrel.mat",
                template,
                new Color(0.09f, 0.11f, 0.10f, 1f)
            );

            wheelMat = Mat(
                "P12_Wheel.mat",
                template,
                new Color(0.035f, 0.04f, 0.04f, 1f)
            );

            groundMat = Mat(
                "P12_Ground.mat",
                template,
                new Color(0.13f, 0.16f, 0.14f, 1f)
            );

            targetMat = Mat(
                "P12_Target.mat",
                template,
                new Color(0.40f, 0.28f, 0.18f, 1f)
            );

            targetAltMat = Mat(
                "P12_TargetAlt.mat",
                template,
                new Color(0.56f, 0.41f, 0.18f, 1f)
            );

            enemyMat = Mat(
                "P12_Enemy.mat",
                template,
                new Color(0.36f, 0.15f, 0.13f, 1f)
            );

            enemyCoreMat = Mat(
                "P12_EnemyCore.mat",
                template,
                new Color(0.70f, 0.16f, 0.10f, 1f)
            );

            playerTracerMat = Mat(
                "P12_PlayerTracer.mat",
                template,
                new Color(1f, 0.72f, 0.12f, 1f)
            );

            enemyTracerMat = Mat(
                "P12_EnemyTracer.mat",
                template,
                new Color(1f, 0.18f, 0.08f, 1f)
            );

            Material[] all =
            {
                hullMat,
                upperMat,
                sideMat,
                turretMat,
                barrelMat,
                wheelMat,
                groundMat,
                targetMat,
                targetAltMat,
                enemyMat,
                enemyCoreMat,
                playerTracerMat,
                enemyTracerMat
            };

            foreach (Material material in all)
            {
                if (material == null ||
                    material.shader == null ||
                    !material.shader.isSupported)
                {
                    Debug.LogError(
                        "[Pack 12] A generated material has an unsupported shader."
                    );
                    return false;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return true;
        }

        private static Material FindActivePipelineMaterial()
        {
            // Unity may expose the active RP through different properties
            // depending on project / quality level / Unity version.
            RenderPipelineAsset pipeline =
                QualitySettings.renderPipeline;

            if (pipeline == null)
                pipeline =
                    GraphicsSettings.defaultRenderPipeline;

            if (pipeline == null)
                pipeline =
                    GraphicsSettings.currentRenderPipeline;

            if (pipeline != null)
            {
                Material pipelineMaterial =
                    ReadDefaultMaterialByReflection(
                        pipeline
                    );

                if (pipelineMaterial != null &&
                    pipelineMaterial.shader != null &&
                    pipelineMaterial.shader.isSupported)
                {
                    Debug.Log(
                        "[Pack 12] Render Pipeline: " +
                        pipeline.GetType().FullName
                    );

                    return pipelineMaterial;
                }
            }

            // Built-in pipeline path. This material is owned by Unity itself,
            // so it matches Built-in without guessing a shader by name.
            Material builtin =
                AssetDatabase.GetBuiltinExtraResource<Material>(
                    "Default-Material.mat"
                );

            if (builtin != null &&
                builtin.shader != null &&
                builtin.shader.isSupported)
            {
                Debug.Log(
                    "[Pack 12] Using Unity Built-in Default-Material."
                );

                return builtin;
            }

            // Final non-destructive fallback:
            // ask a primitive renderer which material Unity itself assigns.
            GameObject probe =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            probe.hideFlags =
                HideFlags.HideAndDontSave;

            Material probeMaterial =
                probe.GetComponent<Renderer>()
                .sharedMaterial;

            Material result = null;

            if (probeMaterial != null &&
                probeMaterial.shader != null &&
                probeMaterial.shader.isSupported)
            {
                result = probeMaterial;
            }

            Object.DestroyImmediate(probe);

            if (result != null)
            {
                Debug.Log(
                    "[Pack 12] Using Unity primitive default material."
                );
            }

            return result;
        }

        private static Material ReadDefaultMaterialByReflection(
            RenderPipelineAsset pipeline)
        {
            if (pipeline == null)
                return null;

            BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            string[] propertyNames =
            {
                "defaultMaterial",
                "defaultMaterialInternal"
            };

            foreach (string propertyName in propertyNames)
            {
                PropertyInfo property =
                    pipeline.GetType().GetProperty(
                        propertyName,
                        flags
                    );

                if (property == null)
                {
                    property =
                        typeof(RenderPipelineAsset)
                        .GetProperty(
                            propertyName,
                            flags
                        );
                }

                if (property == null)
                    continue;

                try
                {
                    Material value =
                        property.GetValue(
                            pipeline,
                            null
                        ) as Material;

                    if (value != null)
                        return value;
                }
                catch
                {
                    // Try next method without breaking scene generation.
                }
            }

            return null;
        }

        private static Material Mat(
            string fileName,
            Material template,
            Color color)
        {
            string path =
                MaterialFolder +
                "/" +
                fileName;

            // Always recreate generated materials from a clean copy of
            // the pipeline-owned template. This prevents stale shaders
            // from a previous RP or previous pack from surviving.
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
                AssetDatabase.DeleteAsset(path);

            Material material =
                new Material(template);

            material.name =
                System.IO.Path.GetFileNameWithoutExtension(
                    fileName
                );

            SetMaterialColor(
                material,
                color
            );

            AssetDatabase.CreateAsset(
                material,
                path
            );

            return material;
        }

        private static void SetMaterialColor(
            Material material,
            Color color)
        {
            if (material == null)
                return;

            string[] colorProperties =
            {
                "_BaseColor",
                "_Color",
                "_UnlitColor"
            };

            foreach (string propertyName in colorProperties)
            {
                if (material.HasProperty(propertyName))
                {
                    material.SetColor(
                        propertyName,
                        color
                    );
                }
            }

            if (material.HasProperty("_Metallic"))
                material.SetFloat(
                    "_Metallic",
                    0.06f
                );

            if (material.HasProperty("_Smoothness"))
                material.SetFloat(
                    "_Smoothness",
                    0.18f
                );

            EditorUtility.SetDirty(material);
        }

        [MenuItem("Tools/Rogue Machine/[12] Pipeline Safe/DIAGNOSE MATERIAL PIPELINE")]
        public static void DiagnoseMaterialPipeline()
        {
            Material template =
                FindActivePipelineMaterial();

            RenderPipelineAsset quality =
                QualitySettings.renderPipeline;

            RenderPipelineAsset defaultRp =
                GraphicsSettings.defaultRenderPipeline;

            RenderPipelineAsset current =
                GraphicsSettings.currentRenderPipeline;

            Debug.Log(
                "[Pack 12 Diagnose]\n" +
                "Quality RP: " +
                (quality != null ? quality.GetType().FullName : "null") +
                "\nDefault RP: " +
                (defaultRp != null ? defaultRp.GetType().FullName : "null") +
                "\nCurrent RP: " +
                (current != null ? current.GetType().FullName : "null") +
                "\nTemplate: " +
                (template != null ? template.name : "null") +
                "\nShader: " +
                (
                    template != null &&
                    template.shader != null ?
                    template.shader.name :
                    "null"
                ) +
                "\nSupported: " +
                (
                    template != null &&
                    template.shader != null &&
                    template.shader.isSupported
                )
            );
        }

        private static GameObject BuildPlayer(
            out Transform turretYaw,
            out Transform cannonPitch,
            out Transform muzzle,
            out Transform cameraPivot)
        {
            GameObject root =
                new GameObject(
                    "P12_PlayerTank"
                );

            root.transform.position =
                new Vector3(
                    0f,
                    0.70f,
                    -8f
                );

            Rigidbody rb =
                root.AddComponent<Rigidbody>();

            rb.mass = 1450f;
            rb.useGravity = false;
            rb.interpolation =
                RigidbodyInterpolation.Interpolate;

            rb.collisionDetectionMode =
                CollisionDetectionMode.ContinuousDynamic;

            rb.constraints =
                RigidbodyConstraints.FreezePositionY |
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;

            // One invisible chassis collider handles environment collision.
            // It is intentionally ABOVE ground and cannot sink because Y is locked.
            BoxCollider chassisCollider =
                root.AddComponent<BoxCollider>();

            chassisCollider.center =
                new Vector3(
                    0f,
                    0.05f,
                    0f
                );

            chassisCollider.size =
                new Vector3(
                    3.15f,
                    0.48f,
                    4.65f
                );

            P12PipelineSafeTankController controller =
                root.AddComponent<
                    P12PipelineSafeTankController
                >();

            root.AddComponent<
                P12PipelineSafeTurret
            >();

            root.AddComponent<
                P12PipelineSafePlayerGun
            >();

            GameObject camPivot =
                new GameObject(
                    "Camera_Pivot"
                );

            camPivot.transform.SetParent(
                root.transform,
                false
            );

            camPivot.transform.localPosition =
                new Vector3(
                    0f,
                    1.58f,
                    -0.20f
                );

            cameraPivot =
                camPivot.transform;

            GameObject hull =
                new GameObject(
                    "Hull"
                );

            hull.transform.SetParent(
                root.transform,
                false
            );

            hull.transform.localPosition =
                Vector3.zero;

            hull.transform.localRotation =
                Quaternion.identity;

            hull.transform.localScale =
                Vector3.one;

            for (int z = -4; z <= 4; z++)
            {
                int width = z == 4 ? 1 : 2;

                for (int x = -width; x <= width; x++)
                {
                    MakePlayerBlock(
                        hull.transform,
                        new Vector3(
                            x * BlockSize,
                            0f,
                            z * BlockSize
                        ),
                        hullMat
                    );
                }
            }

            for (int z = -3; z <= 3; z++)
            {
                MakePlayerBlock(
                    hull.transform,
                    new Vector3(
                        -3f * BlockSize,
                        0f,
                        z * BlockSize
                    ),
                    sideMat
                );

                MakePlayerBlock(
                    hull.transform,
                    new Vector3(
                        3f * BlockSize,
                        0f,
                        z * BlockSize
                    ),
                    sideMat
                );
            }

            for (int z = -2; z <= 2; z++)
            {
                int width = z == 2 ? 0 : 1;

                for (int x = -width; x <= width; x++)
                {
                    MakePlayerBlock(
                        hull.transform,
                        new Vector3(
                            x * BlockSize,
                            BlockSize,
                            z * BlockSize
                        ),
                        upperMat
                    );
                }
            }

            GameObject wheels =
                new GameObject(
                    "Wheels"
                );

            wheels.transform.SetParent(
                root.transform,
                false
            );

            wheels.transform.localPosition =
                Vector3.zero;

            wheels.transform.localRotation =
                Quaternion.identity;

            wheels.transform.localScale =
                Vector3.one;

            // Pack 16: spacing is slightly larger than the visual
            // wheel diameter, so neighbouring wheels no longer intersect.
            float[] wheelZ =
            {
                -2.45f,
                -1.47f,
                -0.49f,
                 0.49f,
                 1.47f,
                 2.45f
            };

            foreach (float z in wheelZ)
            {
                MakeWheel(
                    wheels.transform,
                    controller,
                    -2.36f,
                    -0.27f,
                    z
                );

                MakeWheel(
                    wheels.transform,
                    controller,
                    2.36f,
                    -0.27f,
                    z
                );

                MakeWheelMount(
                    root.transform,
                    -1f,
                    z,
                    hullMat
                );

                MakeWheelMount(
                    root.transform,
                    1f,
                    z,
                    hullMat
                );
            }

            // ----------------------------
            // STABLE TURRET + WEAPON RIG
            // ----------------------------
            GameObject yaw =
                new GameObject(
                    "Turret_Yaw"
                );

            yaw.transform.SetParent(
                root.transform,
                false
            );

            Vector3 yawAnchor =
                new Vector3(
                    0f,
                    BlockSize * 2f,
                    0.10f
                );

            yaw.transform.localPosition =
                yawAnchor;

            yaw.transform.localRotation =
                Quaternion.identity;

            turretYaw =
                yaw.transform;

            // Compact damageable turret shell.
            for (int z = -1; z <= 0; z++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    if (x == 0 && z == 0)
                        continue;

                    MakePlayerBlock(
                        turretYaw,
                        new Vector3(
                            x * BlockSize,
                            0f,
                            z * BlockSize
                        ),
                        turretMat
                    );
                }
            }

            // Permanent breech blocks are part of the weapon rig.
            // They have no Rigidbody, no Damageable and no Collider.
            MakeWeaponBlock(
                turretYaw,
                new Vector3(
                    0f,
                    0f,
                    -BlockSize * 0.20f
                ),
                turretMat,
                "Breech_Rear"
            );

            MakeWeaponBlock(
                turretYaw,
                new Vector3(
                    0f,
                    0f,
                    BlockSize * 0.72f
                ),
                barrelMat,
                "Breech_Front"
            );

            GameObject pitch =
                new GameObject(
                    "Cannon_Pitch"
                );

            pitch.transform.SetParent(
                turretYaw,
                false
            );

            Vector3 pitchAnchor =
                new Vector3(
                    0f,
                    0f,
                    BlockSize * 0.92f
                );

            pitch.transform.localPosition =
                pitchAnchor;

            pitch.transform.localRotation =
                Quaternion.identity;

            cannonPitch =
                pitch.transform;

            GameObject last = null;

            // First barrel cube overlaps the front breech slightly.
            // That makes a visible gap impossible even at maximum pitch.
            for (int z = 0; z < 5; z++)
            {
                last =
                    MakeWeaponBlock(
                        cannonPitch,
                        new Vector3(
                            0f,
                            0f,
                            BlockSize * (0.28f + z)
                        ),
                        barrelMat,
                        "Barrel_" + z
                    );
            }

            GameObject muzzleObject =
                new GameObject(
                    "Muzzle"
                );

            muzzleObject.transform.SetParent(
                cannonPitch,
                false
            );

            muzzleObject.transform.localPosition =
                last.transform.localPosition +
                Vector3.forward *
                BlockSize * 0.62f;

            muzzle =
                muzzleObject.transform;

            root.AddComponent<
                P12PipelineSafeWeaponAnchor
            >().Configure(
                turretYaw,
                cannonPitch,
                yawAnchor,
                pitchAnchor
            );

            root.AddComponent<
                P12PipelineSafeHierarchyValidator
            >().Configure(
                hull.transform,
                wheels.transform,
                turretYaw
            );

            return root;
        }

        private static GameObject MakePlayerBlock(
            Transform parent,
            Vector3 localPosition,
            Material material)
        {
            GameObject block =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            block.name =
                "P12_PlayerBlock";

            block.transform.SetParent(
                parent,
                false
            );

            block.transform.localPosition =
                localPosition;

            block.transform.localRotation =
                Quaternion.identity;

            block.transform.localScale =
                Vector3.one *
                BlockSize * 0.965f;

            block.GetComponent<Renderer>()
                .sharedMaterial =
                material;

            block.AddComponent<
                P12_PipelineSafePlayerBlock
            >();

            block.AddComponent<
                P12PipelineSafeDamageable
            >().Configure(
                100f,
                true
            );

            return block;
        }

        private static GameObject MakeWeaponBlock(
            Transform parent,
            Vector3 localPosition,
            Material material,
            string objectName)
        {
            GameObject block =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            block.name =
                "P12_" + objectName;

            block.transform.SetParent(
                parent,
                false
            );

            block.transform.localPosition =
                localPosition;

            block.transform.localRotation =
                Quaternion.identity;

            block.transform.localScale =
                Vector3.one *
                BlockSize * 0.965f;

            block.GetComponent<Renderer>()
                .sharedMaterial =
                material;

            Collider collider =
                block.GetComponent<Collider>();

            if (collider != null)
                Object.DestroyImmediate(collider);

            return block;
        }

        private static void MakeWheelMount(
            Transform root,
            float side,
            float z,
            Material material)
        {
            GameObject block =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            block.name =
                "P17_WheelMountBlock";

            block.transform.SetParent(
                root,
                false
            );

            block.transform.localPosition =
                new Vector3(
                    side * 1.70f,
                    -0.10f,
                    z
                );

            block.transform.localScale =
                new Vector3(
                    0.48f,
                    0.48f,
                    0.48f
                );

            block.GetComponent<Renderer>()
                .sharedMaterial =
                material;

            Collider blockCollider =
                block.GetComponent<Collider>();

            if (blockCollider != null)
                Object.DestroyImmediate(
                    blockCollider
                );

            GameObject axle =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            axle.name =
                "P17_WheelAxle";

            axle.transform.SetParent(
                root,
                false
            );

            axle.transform.localPosition =
                new Vector3(
                    side * 2.07f,
                    -0.10f,
                    z
                );

            axle.transform.localScale =
                new Vector3(
                    0.34f,
                    0.16f,
                    0.16f
                );

            axle.GetComponent<Renderer>()
                .sharedMaterial =
                material;

            Collider axleCollider =
                axle.GetComponent<Collider>();

            if (axleCollider != null)
                Object.DestroyImmediate(
                    axleCollider
                );
        }

        private static void MakeWheel(
            Transform parent,
            P12PipelineSafeTankController controller,
            float x,
            float y,
            float z)
        {
            GameObject wheel =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cylinder
                );

            wheel.transform.SetParent(
                parent,
                false
            );

            wheel.transform.localPosition =
                new Vector3(x, y, z);

            wheel.transform.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    90f
                );

            // Unity Cylinder has radius 0.5, so diameter must be 2 * desired radius.
            wheel.transform.localScale =
                new Vector3(
                    WheelRadius * 2f,
                    WheelWidth * 0.5f,
                    WheelRadius * 2f
                );

            wheel.GetComponent<Renderer>()
                .sharedMaterial =
                wheelMat;

            Collider col =
                wheel.GetComponent<Collider>();

            if (col != null)
                Object.DestroyImmediate(col);

            wheel.AddComponent<
                P12PipelineSafeWheelVisual
            >().Configure(
                controller,
                WheelRadius
            );
        }

        private static Camera BuildCamera(
            Transform root,
            Transform pivot)
        {
            GameObject cameraObject =
                new GameObject(
                    "Main Camera"
                );

            cameraObject.tag =
                "MainCamera";

            Camera cam =
                cameraObject.AddComponent<
                    Camera
                >();

            cam.clearFlags =
                CameraClearFlags.SolidColor;

            cam.backgroundColor =
                new Color(
                    0.035f,
                    0.045f,
                    0.05f,
                    1f
                );

            cameraObject.AddComponent<
                AudioListener
            >();

            P12PipelineSafeCamera follow =
                cameraObject.AddComponent<
                    P12PipelineSafeCamera
                >();

            follow.Configure(root, pivot);

            return cam;
        }

        private static void BuildTargetWalls()
        {
            GameObject root =
                new GameObject(
                    "P12_TargetWalls"
                );

            MakeWall(
                root.transform,
                new Vector3(
                    -5f,
                    BlockSize * 0.5f,
                    9f
                ),
                5,
                4
            );

            MakeWall(
                root.transform,
                new Vector3(
                    5f,
                    BlockSize * 0.5f,
                    12f
                ),
                4,
                5
            );
        }

        private static void MakeWall(
            Transform parent,
            Vector3 worldOrigin,
            int width,
            int height)
        {
            GameObject wall =
                new GameObject("TargetWall");

            wall.transform.SetParent(
                parent,
                false
            );

            wall.transform.localPosition =
                worldOrigin;

            wall.transform.localRotation =
                Quaternion.identity;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    GameObject block =
                        GameObject.CreatePrimitive(
                            PrimitiveType.Cube
                        );

                    block.transform.SetParent(
                        wall.transform
                    );

                    block.transform.localPosition =
                        new Vector3(
                            (x - (width - 1) * 0.5f) *
                            BlockSize,
                            y * BlockSize,
                            0f
                        );

                    block.transform.localScale =
                        Vector3.one *
                        BlockSize * 0.965f;

                    block.GetComponent<Renderer>()
                        .sharedMaterial =
                        ((x + y) % 2 == 0) ?
                        targetMat :
                        targetAltMat;

                    block.AddComponent<
                        P12_PipelineSafeTargetBlock
                    >();

                    block.AddComponent<
                        P12PipelineSafeDamageable
                    >().Configure(
                        100f,
                        true
                    );
                }
            }
        }

        private static void BuildEnemyTurret(
            Transform player,
            Vector3 worldPosition)
        {
            GameObject enemy =
                new GameObject(
                    "P12_EnemyTurret"
                );

            enemy.transform.position =
                worldPosition;

            GameObject baseRoot =
                new GameObject(
                    "Base"
                );

            baseRoot.transform.SetParent(
                enemy.transform,
                false
            );

            baseRoot.transform.localPosition =
                Vector3.zero;

            baseRoot.transform.localRotation =
                Quaternion.identity;

            baseRoot.transform.localScale =
                Vector3.one;

            P12PipelineSafeDamageable coreDamageable = null;

            for (int y = 0; y < 2; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        GameObject block =
                            MakeEnemyBlock(
                                baseRoot.transform,
                                new Vector3(
                                    x * BlockSize,
                                    BlockSize * 0.5f +
                                    y * BlockSize,
                                    z * BlockSize
                                ),
                                enemyMat
                            );

                        if (y == 1 &&
                            x == 0 &&
                            z == 0)
                        {
                            block.GetComponent<Renderer>()
                                .sharedMaterial =
                                enemyCoreMat;

                            coreDamageable =
                                block.GetComponent<
                                    P12PipelineSafeDamageable
                                >();
                        }
                    }
                }
            }

            GameObject yaw =
                new GameObject(
                    "Enemy_Yaw"
                );

            yaw.transform.SetParent(
                enemy.transform,
                false
            );

            yaw.transform.localPosition =
                new Vector3(
                    0f,
                    BlockSize * 2.1f,
                    0f
                );

            MakeEnemyBlock(
                yaw.transform,
                Vector3.zero,
                enemyMat
            );

            GameObject barrel =
                MakeEnemyBlock(
                    yaw.transform,
                    new Vector3(
                        0f,
                        0f,
                        BlockSize * 1.1f
                    ),
                    enemyMat
                );

            GameObject muzzleObject =
                new GameObject(
                    "Enemy_Muzzle"
                );

            muzzleObject.transform.SetParent(
                yaw.transform,
                false
            );

            muzzleObject.transform.localPosition =
                new Vector3(
                    0f,
                    0f,
                    BlockSize * 1.8f
                );

            enemy.AddComponent<
                P12PipelineSafeEnemyTurret
            >().Configure(
                player,
                yaw.transform,
                muzzleObject.transform,
                enemy.transform,
                coreDamageable,
                enemyTracerMat
            );
        }

        private static GameObject MakeEnemyBlock(
            Transform parent,
            Vector3 localPosition,
            Material material)
        {
            GameObject block =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            block.name =
                "P12_EnemyBlock";

            block.transform.SetParent(
                parent,
                false
            );

            block.transform.localPosition =
                localPosition;

            block.transform.localRotation =
                Quaternion.identity;

            block.transform.localScale =
                Vector3.one *
                BlockSize * 0.965f;

            block.GetComponent<Renderer>()
                .sharedMaterial =
                material;

            block.AddComponent<
                P12_PipelineSafeTargetBlock
            >();

            block.AddComponent<
                P12PipelineSafeDamageable
            >().Configure(
                100f,
                true
            );

            return block;
        }

        private static void BuildGround()
        {
            GameObject ground =
                GameObject.CreatePrimitive(
                    PrimitiveType.Plane
                );

            ground.name =
                "P12_Ground";

            ground.transform.localScale =
                new Vector3(
                    18f,
                    1f,
                    18f
                );

            ground.GetComponent<Renderer>()
                .sharedMaterial =
                groundMat;
        }

        private static void BuildLight()
        {
            GameObject lightObject =
                new GameObject(
                    "P12_DirectionalLight"
                );

            Light light =
                lightObject.AddComponent<
                    Light
                >();

            light.type =
                LightType.Directional;

            light.intensity = 1.35f;

            lightObject.transform.rotation =
                Quaternion.Euler(
                    45f,
                    -35f,
                    0f
                );

            RenderSettings.ambientMode =
                AmbientMode.Flat;

            RenderSettings.ambientLight =
                new Color(
                    0.35f,
                    0.37f,
                    0.36f,
                    1f
                );
        }
    }
}
#endif
