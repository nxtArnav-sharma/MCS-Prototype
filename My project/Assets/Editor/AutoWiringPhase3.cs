using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using Cinemachine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class AutoWiringPhase3 : EditorWindow
{
    public static void RunPhase3()
    {
        BuildArenaScene();
        Debug.Log("Phase 3 Complete! Play the game!");
    }

    static void BuildArenaScene()
    {
        // Create new scene
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        // Setup Lighting
        var dl = new GameObject("Directional Light");
        var lightObj = dl.AddComponent<Light>();
        lightObj.type = LightType.Directional;
        lightObj.intensity = 1.2f;
        dl.transform.rotation = Quaternion.Euler(45, -30, 0);

        var pl = new GameObject("Arena Point Light");
        var plight = pl.AddComponent<Light>();
        plight.type = LightType.Point;
        plight.range = 20;
        plight.intensity = 2;
        pl.transform.position = new Vector3(0, 4, 0);

        // Spawn Arena FBX
        var arenaFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/Arena_Scene.fbx");
        if (arenaFbx != null)
        {
            var arenaInst = (GameObject)PrefabUtility.InstantiatePrefab(arenaFbx);
            arenaInst.transform.position = Vector3.zero;

            // Make children navigation static
            foreach (Transform t in arenaInst.GetComponentsInChildren<Transform>())
            {
                if (t.name.Contains("Floor")) {
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.NavigationStatic);
                }
            }
        }
        else
        {
            // Spawn just floor tiles if full arena doesn't exist
            var floorFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/Floor_Tile.fbx");
            if (floorFbx != null) {
                var floorInst = (GameObject)PrefabUtility.InstantiatePrefab(floorFbx);
                GameObjectUtility.SetStaticEditorFlags(floorInst, StaticEditorFlags.NavigationStatic);
            }
        }

        // Build NavMesh
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();

        // Spawn Points
        var spRoot = new GameObject("SpawnPoints");
        Vector3[] sps = { new Vector3(-8, 0, 8), new Vector3(8, 0, 8), new Vector3(-8, 0, -8), new Vector3(8, 0, -8) };
        Transform[] spawnTransforms = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            var sp = new GameObject("SpawnPoint_" + (i + 1));
            sp.transform.SetParent(spRoot.transform);
            sp.transform.position = sps[i];
            spawnTransforms[i] = sp.transform;
        }

        // Combat Manager
        var cmObj = new GameObject("CombatManager");
        var cm = cmObj.AddComponent<CombatManager>();
        cm.hitVFXPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX/HitVFX_Prefab.prefab");
        cm.poolSize = 20;
        cm.enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Enemy_Golem_Prefab.prefab");
        cm.spawnPoints = spawnTransforms;
        cm.enemiesPerWave = 3;
        cm.waveCooldown = 5;

        // Player
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player_Prefab.prefab");
        GameObject playerInst = null;
        if (playerPrefab != null)
        {
            playerInst = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerInst.transform.position = Vector3.zero;
        }

        // Cameras
        var camRig = new GameObject("CameraRig");
        var cc = camRig.AddComponent<CameraController>();
        
        var freeLookObj = new GameObject("PlayerFollowCam");
        var freeLook = freeLookObj.AddComponent<CinemachineFreeLook>();
        if (playerInst != null)
        {
            freeLook.Follow = playerInst.transform;
            freeLook.LookAt = playerInst.transform;
        }
        freeLook.m_YAxis.m_MaxSpeed = 2;
        freeLook.m_XAxis.m_MaxSpeed = 300;

        var lockOnObj = new GameObject("LockOnCam");
        var lockOn = lockOnObj.AddComponent<CinemachineVirtualCamera>();
        lockOn.Priority = 0;

        cc.freeLookCam = freeLook;
        cc.lockOnCam = lockOn;
        cc.enemyLayer = LayerMask.GetMask("Enemy"); // Ensure Enemy layer exists

        // Canvas
        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        var hud = canvasObj.AddComponent<CombatHUD>();
        
        var healthBarObj = new GameObject("PlayerHealthBar");
        healthBarObj.transform.SetParent(canvasObj.transform);
        var slider = healthBarObj.AddComponent<Slider>();
        
        var healthTextObj = new GameObject("PlayerHealthText");
        healthTextObj.transform.SetParent(canvasObj.transform);
        var hText = healthTextObj.AddComponent<TextMeshProUGUI>();
        hText.text = "100%";
        
        var weaponTextObj = new GameObject("WeaponNameText");
        weaponTextObj.transform.SetParent(canvasObj.transform);
        var wText = weaponTextObj.AddComponent<TextMeshProUGUI>();
        wText.text = "Weapon";
        
        var waveTextObj = new GameObject("WaveText");
        waveTextObj.transform.SetParent(canvasObj.transform);
        var wvText = waveTextObj.AddComponent<TextMeshProUGUI>();
        wvText.text = "Wave 1";
        
        var goPanel = new GameObject("GameOverPanel");
        goPanel.transform.SetParent(canvasObj.transform);
        var goTextObj = new GameObject("Text");
        goTextObj.transform.SetParent(goPanel.transform);
        var goText = goTextObj.AddComponent<TextMeshProUGUI>();
        goText.text = "GAME OVER";
        goPanel.SetActive(false);

        hud.playerHealthBar = slider;
        hud.playerHealthText = hText;
        hud.weaponNameText = wText;
        hud.waveText = wvText;
        hud.gameOverPanel = goPanel;

        EditorSceneManager.SaveScene(newScene, "Assets/Scenes/Arena.unity");
    }
}
