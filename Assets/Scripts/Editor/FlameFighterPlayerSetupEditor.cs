using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using FlameFighter.CameraSystem;
using FlameFighter.Environment;
using FlameFighter.Fire;
using FlameFighter.Hose;
using FlameFighter.Input;
using FlameFighter.Interaction;
using FlameFighter.Player;

namespace FlameFighter.Editor
{
    public class FlameFighterPlayerSetupEditor : EditorWindow
    {
        private const string k_SettingsFolder = "Assets/Settings";
        private const string k_MovementSettingsPath = k_SettingsFolder + "/PlayerMovementSettings.asset";
        private const string k_CameraSettingsPath = k_SettingsFolder + "/CameraSettings.asset";
        private const string k_HoseSettingsPath = k_SettingsFolder + "/HoseSettings.asset";

        [MenuItem("Flame Fighter/FPS Player Setup Wizard", priority = 0)]
        public static void OpenWizard()
        {
            var window = GetWindow<FlameFighterPlayerSetupEditor>("Player Setup");
            window.minSize = new Vector2(420, 420);
            window.Show();
        }

        [MenuItem("Flame Fighter/1-Click Setup Complete FPS Player", priority = 1)]
        public static void SetupPlayerQuick()
        {
            SetupPlayer(createGroundIfEmpty: true);
        }

        [MenuItem("Flame Fighter/Spawn Test Burning Target", priority = 10)]
        public static void SpawnTestBurningTarget()
        {
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            target.name = "Test_Burning_Tree";
            Undo.RegisterCreatedObjectUndo(target, "Create Test Burning Tree");

            // Position in front of player if possible
            GameObject player = GameObject.Find("Player_FlameFighter") ?? GameObject.FindWithTag("Player");
            if (player != null)
            {
                target.transform.position = player.transform.position + player.transform.forward * 4.5f;
            }
            else
            {
                target.transform.position = new Vector3(0f, 1f, 4.5f);
            }
            target.transform.localScale = new Vector3(0.8f, 1.8f, 0.8f);

            // Add Flammable and FireVisual
            var flammable = target.AddComponent<Flammable>();
            var fireVisual = target.AddComponent<FireVisual>();

            // Add a fire light child
            GameObject lightObj = new GameObject("FireLight");
            lightObj.transform.SetParent(target.transform);
            lightObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            Light fireLight = lightObj.AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, 0.45f, 0.1f);
            fireLight.range = 8f;
            fireLight.intensity = 3f;

            var fireVisSo = new SerializedObject(fireVisual);
            fireVisSo.FindProperty("m_FireLight").objectReferenceValue = fireLight;
            fireVisSo.ApplyModifiedProperties();

            Selection.activeGameObject = target;
            EditorGUIUtility.PingObject(target);
            Debug.Log("<color=#FF7043><b>[Flame Fighter]</b></color> Spawned test burning tree! Press <b>Play</b> and hold <b>Left Mouse Button</b> to spray and extinguish it.");
        }

        [MenuItem("Flame Fighter/Spawn Test Water Hydrant", priority = 11)]
        public static void SpawnTestWaterHydrant()
        {
            GameObject hydrant = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hydrant.name = "Water_Hydrant";
            Undo.RegisterCreatedObjectUndo(hydrant, "Create Water Hydrant");

            GameObject player = GameObject.Find("Player_FlameFighter") ?? GameObject.FindWithTag("Player");
            if (player != null)
            {
                hydrant.transform.position = player.transform.position + player.transform.right * 3f;
            }
            else
            {
                hydrant.transform.position = new Vector3(3f, 0.5f, 0f);
            }
            hydrant.transform.localScale = new Vector3(0.5f, 0.6f, 0.5f);

            hydrant.AddComponent<WaterSource>();

            Selection.activeGameObject = hydrant;
            EditorGUIUtility.PingObject(hydrant);
            Debug.Log("<color=#29B6F6><b>[Flame Fighter]</b></color> Spawned test water hydrant! Walk close and press <b>E</b> to refill your water tank.");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            GUILayout.Label("Flame Fighter: FPS Player & Systems Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Sets up the complete FPS player controller, standard camera, dedicated input actions, hose spraying system, and tunable settings assets.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Status checks
            bool hasMovementSettings = File.Exists(k_MovementSettingsPath);
            bool hasCameraSettings = File.Exists(k_CameraSettingsPath);
            bool hasHoseSettings = File.Exists(k_HoseSettingsPath);
            var inputAsset = FindInputAsset();
            bool hasInputAsset = inputAsset != null;
            Camera mainCam = Camera.main;
            bool hasMainCamera = mainCam != null;

            EditorGUILayout.LabelField("Status Checks:", EditorStyles.boldLabel);
            DrawStatusRow("Movement Settings Asset", hasMovementSettings);
            DrawStatusRow("Camera Settings Asset", hasCameraSettings);
            DrawStatusRow("Hose Settings Asset", hasHoseSettings);
            DrawStatusRow("FlameFighter_Actions Asset", hasInputAsset);
            DrawStatusRow("Main Camera in Scene", hasMainCamera);

            EditorGUILayout.Space(15);

            if (GUILayout.Button("⚡ 1-Click Setup Complete FPS Player + Hose", GUILayout.Height(36)))
            {
                SetupPlayer(createGroundIfEmpty: true);
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn Burning Target", GUILayout.Height(28)))
            {
                SpawnTestBurningTarget();
            }
            if (GUILayout.Button("Spawn Water Hydrant", GUILayout.Height(28)))
            {
                SpawnTestWaterHydrant();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Create Test Ground & Lighting", GUILayout.Height(26)))
            {
                CreateTestGroundAndLight();
            }
        }

        private void DrawStatusRow(string label, bool ok)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(220));
            GUI.color = ok ? Color.green : Color.yellow;
            EditorGUILayout.LabelField(ok ? "✓ Ready" : "⚠ Will be created", EditorStyles.miniBoldLabel);
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        public static void SetupPlayer(bool createGroundIfEmpty)
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Setup Flame Fighter Player");

            // 1. Ensure settings assets exist
            var (movementSettings, cameraSettings, hoseSettings) = EnsureAllSettingsAssets();

            // 2. Locate input actions asset
            var inputActions = FindInputAsset();

            // 3. Create or find Player GameObject
            GameObject player = GameObject.Find("Player_FlameFighter") ?? GameObject.FindWithTag("Player");
            if (player == null)
            {
                player = new GameObject("Player_FlameFighter");
                Undo.RegisterCreatedObjectUndo(player, "Create Player GameObject");
                player.transform.position = new Vector3(0f, 1f, 0f);
            }

            try { player.tag = "Player"; } catch { }

            // 4. Setup CharacterController
            var cc = player.GetComponent<CharacterController>();
            if (cc == null) cc = Undo.AddComponent<CharacterController>(player);
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 50f;
            cc.minMoveDistance = 0.001f;

            // 5. Setup Camera Target Child (Eye pivot)
            Transform cameraTarget = player.transform.Find("CameraTarget");
            if (cameraTarget == null)
            {
                GameObject targetObj = new GameObject("CameraTarget");
                Undo.RegisterCreatedObjectUndo(targetObj, "Create CameraTarget");
                targetObj.transform.SetParent(player.transform);
                targetObj.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                targetObj.transform.localRotation = Quaternion.identity;
                cameraTarget = targetObj.transform;
            }

            // 6. Setup PlayerInputReader
            var inputReader = player.GetComponent<PlayerInputReader>();
            if (inputReader == null) inputReader = Undo.AddComponent<PlayerInputReader>(player);
            if (inputActions != null)
            {
                var inputSo = new SerializedObject(inputReader);
                inputSo.FindProperty("m_ActionsAsset").objectReferenceValue = inputActions;
                inputSo.ApplyModifiedProperties();
            }

            // 7. Remove any obsolete Cinemachine controller if present
            var cmController = player.GetComponent<FirstPersonCinemachineController>();
            if (cmController != null) Undo.DestroyObjectImmediate(cmController);

            // 8. Setup FirstPersonCameraController (Standard Camera)
            var camController = player.GetComponent<FirstPersonCameraController>();
            if (camController == null) camController = Undo.AddComponent<FirstPersonCameraController>(player);
            var camSo = new SerializedObject(camController);
            camSo.FindProperty("m_CameraPitchRoot").objectReferenceValue = cameraTarget;
            camSo.FindProperty("m_PlayerBody").objectReferenceValue = player.transform;
            camSo.FindProperty("m_InputReader").objectReferenceValue = inputReader;
            if (cameraSettings != null) camSo.FindProperty("m_Settings").objectReferenceValue = cameraSettings;
            camSo.ApplyModifiedProperties();

            // 9. Setup Standard Camera Hierarchy
            SetupStandardCamera(cameraTarget);

            // 10. Clean up any leftover Cinemachine Virtual Camera objects
            GameObject cmCam = GameObject.Find("CM_FPS_Camera");
            if (cmCam != null) Undo.DestroyObjectImmediate(cmCam);

            // 11. Setup PlayerMovementController
            var moveController = player.GetComponent<PlayerMovementController>();
            if (moveController == null) moveController = Undo.AddComponent<PlayerMovementController>(player);
            var moveSo = new SerializedObject(moveController);
            if (movementSettings != null) moveSo.FindProperty("m_Settings").objectReferenceValue = movementSettings;
            moveSo.FindProperty("m_InputReader").objectReferenceValue = inputReader;
            moveSo.ApplyModifiedProperties();

            // 12. Setup HoseController
            var hoseController = player.GetComponent<HoseController>();
            if (hoseController == null) hoseController = Undo.AddComponent<HoseController>(player);
            var hoseSo = new SerializedObject(hoseController);
            if (hoseSettings != null) hoseSo.FindProperty("m_Settings").objectReferenceValue = hoseSettings;
            hoseSo.FindProperty("m_InputReader").objectReferenceValue = inputReader;
            hoseSo.FindProperty("m_CameraController").objectReferenceValue = camController;
            hoseSo.FindProperty("m_StreamOrigin").objectReferenceValue = cameraTarget;
            hoseSo.ApplyModifiedProperties();

            // 13. Setup PlayerInteractionHandler
            var interactionHandler = player.GetComponent<PlayerInteractionHandler>();
            if (interactionHandler == null) interactionHandler = Undo.AddComponent<PlayerInteractionHandler>(player);
            var interSo = new SerializedObject(interactionHandler);
            interSo.FindProperty("m_RaycastOrigin").objectReferenceValue = cameraTarget;
            interSo.FindProperty("m_InputReader").objectReferenceValue = inputReader;
            interSo.ApplyModifiedProperties();

            // 14. Optional Test Ground
            if (createGroundIfEmpty)
            {
                EnsureTestEnvironment();
            }

            Selection.activeGameObject = player;
            EditorGUIUtility.PingObject(player);
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log("<color=#4FC3F7><b>[Flame Fighter]</b></color> FPS Player & Hose Controller setup complete! Press <b>Play</b> to test.");
        }

        private static void SetupStandardCamera(Transform cameraTarget)
        {
            Camera mainCam = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();

            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                Undo.RegisterCreatedObjectUndo(camObj, "Create Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }

            Undo.SetTransformParent(mainCam.transform, cameraTarget, "Parent Main Camera to CameraTarget");
            mainCam.transform.localPosition = Vector3.zero;
            mainCam.transform.localRotation = Quaternion.identity;
            mainCam.transform.localScale = Vector3.one;

            if (mainCam.GetComponent<AudioListener>() == null)
            {
                Undo.AddComponent<AudioListener>(mainCam.gameObject);
            }

            var components = mainCam.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp != null && comp.GetType().Name.Contains("Cinemachine"))
                {
                    Undo.DestroyObjectImmediate(comp);
                }
            }
        }

        private static (PlayerMovementSettings, CameraSettings, HoseSettings) EnsureAllSettingsAssets()
        {
            if (!Directory.Exists(k_SettingsFolder))
            {
                Directory.CreateDirectory(k_SettingsFolder);
                AssetDatabase.Refresh();
            }

            PlayerMovementSettings movementSettings = AssetDatabase.LoadAssetAtPath<PlayerMovementSettings>(k_MovementSettingsPath);
            if (movementSettings == null)
            {
                movementSettings = ScriptableObject.CreateInstance<PlayerMovementSettings>();
                AssetDatabase.CreateAsset(movementSettings, k_MovementSettingsPath);
            }

            CameraSettings cameraSettings = AssetDatabase.LoadAssetAtPath<CameraSettings>(k_CameraSettingsPath);
            if (cameraSettings == null)
            {
                cameraSettings = ScriptableObject.CreateInstance<CameraSettings>();
                AssetDatabase.CreateAsset(cameraSettings, k_CameraSettingsPath);
            }

            HoseSettings hoseSettings = AssetDatabase.LoadAssetAtPath<HoseSettings>(k_HoseSettingsPath);
            if (hoseSettings == null)
            {
                hoseSettings = ScriptableObject.CreateInstance<HoseSettings>();
                AssetDatabase.CreateAsset(hoseSettings, k_HoseSettingsPath);
            }

            AssetDatabase.SaveAssets();
            return (movementSettings, cameraSettings, hoseSettings);
        }

        private static InputActionAsset FindInputAsset()
        {
            string[] guids = AssetDatabase.FindAssets("FlameFighter_Actions t:InputActionAsset");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            }
            return null;
        }

        private static void EnsureTestEnvironment()
        {
            if (UnityEngine.Object.FindFirstObjectByType<Collider>() == null)
            {
                CreateTestGroundAndLight();
            }
        }

        private static void CreateTestGroundAndLight()
        {
            if (GameObject.Find("Test_Ground") == null)
            {
                GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Test_Ground";
                Undo.RegisterCreatedObjectUndo(ground, "Create Test Ground");
                ground.transform.position = Vector3.zero;
                ground.transform.localScale = new Vector3(5f, 1f, 5f);
            }

            if (UnityEngine.Object.FindFirstObjectByType<Light>() == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                Undo.RegisterCreatedObjectUndo(lightObj, "Create Directional Light");
                Light light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = new Color(1f, 0.95f, 0.85f);
                light.intensity = 1.0f;
                lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
        }
    }
}
