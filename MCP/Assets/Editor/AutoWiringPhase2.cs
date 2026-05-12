using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.AI;
using Cinemachine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class AutoWiringPhase2 : EditorWindow
{
    public static void RunPhase2()
    {
        ConfigureAnimations();
        LinkAnimatorControllers();
        BuildWeaponPrefabs();
        BuildCharacterPrefabs();
        BuildScene();
        Debug.Log("Phase 2 Complete!");
    }

    static void ConfigureAnimations()
    {
        // Player FBX
        string playerPath = "Assets/Models/Characters/Player_Warrior.fbx";
        ModelImporter playerImporter = AssetImporter.GetAtPath(playerPath) as ModelImporter;
        if (playerImporter != null)
        {
            ModelImporterClipAnimation[] playerClips = new ModelImporterClipAnimation[]
            {
                CreateClip("Idle", 0, 60, true),
                CreateClip("Walk", 70, 100, true),
                CreateClip("Attack1", 110, 130, false),
                CreateClip("Attack2", 140, 165, false),
                CreateClip("Roll", 170, 195, false)
            };
            playerImporter.clipAnimations = playerClips;
            playerImporter.SaveAndReimport();
        }

        // Enemy FBX
        string enemyPath = "Assets/Models/Characters/Enemy_Golem.fbx";
        ModelImporter enemyImporter = AssetImporter.GetAtPath(enemyPath) as ModelImporter;
        if (enemyImporter != null)
        {
            ModelImporterClipAnimation[] enemyClips = new ModelImporterClipAnimation[]
            {
                CreateClip("Idle", 0, 80, true),
                CreateClip("Walk", 90, 130, true),
                CreateClip("Attack", 140, 175, false),
                CreateClip("HitReact", 180, 195, false),
                CreateClip("Death", 200, 250, false)
            };

            // Add Animation Event to Attack clip
            AnimationEvent evt = new AnimationEvent();
            evt.functionName = "AnimEvent_AttackHit";
            // Frame 158 out of 140-175 at 30fps: (158-140)/30 = 0.6 seconds.
            // Some versions of Unity use frames if the clip specifies it, but time is seconds.
            // Let's use normalizedTime if available, or just time = 18f/30f.
            evt.time = 18f / 30f; 
            
            enemyClips[2].events = new AnimationEvent[] { evt };

            enemyImporter.clipAnimations = enemyClips;
            enemyImporter.SaveAndReimport();
        }
    }

    static ModelImporterClipAnimation CreateClip(string name, int first, int last, bool loop)
    {
        ModelImporterClipAnimation clip = new ModelImporterClipAnimation();
        clip.name = name;
        clip.firstFrame = first;
        clip.lastFrame = last;
        clip.loopTime = loop;
        clip.lockRootHeightY = true;
        clip.lockRootPositionXZ = true;
        clip.lockRootRotation = true;
        return clip;
    }

    static void LinkAnimatorControllers()
    {
        string playerAnimPath = "Assets/Animations/Player/PlayerAnimator.controller";
        var playerController = AssetDatabase.LoadAssetAtPath<AnimatorController>(playerAnimPath);
        var playerClips = AssetDatabase.LoadAllAssetsAtPath("Assets/Models/Characters/Player_Warrior.fbx");
        
        if (playerController != null)
        {
            foreach (var state in playerController.layers[0].stateMachine.states)
            {
                string clipName = state.state.name;
                // Attack0 uses Attack1, Attack1 uses Attack2, Attack2 uses Attack1
                if (clipName == "Attack0") clipName = "Attack1";
                else if (clipName == "Attack1") clipName = "Attack2";
                else if (clipName == "Attack2") clipName = "Attack1";

                foreach (var obj in playerClips)
                {
                    if (obj is AnimationClip clip && clip.name == clipName)
                    {
                        state.state.motion = clip;
                        break;
                    }
                }
            }
            EditorUtility.SetDirty(playerController);
        }

        string enemyAnimPath = "Assets/Animations/Enemy/EnemyAnimator.controller";
        var enemyController = AssetDatabase.LoadAssetAtPath<AnimatorController>(enemyAnimPath);
        var enemyClips = AssetDatabase.LoadAllAssetsAtPath("Assets/Models/Characters/Enemy_Golem.fbx");

        if (enemyController != null)
        {
            foreach (var state in enemyController.layers[0].stateMachine.states)
            {
                foreach (var obj in enemyClips)
                {
                    if (obj is AnimationClip clip && clip.name == state.state.name)
                    {
                        state.state.motion = clip;
                        break;
                    }
                }
            }
            EditorUtility.SetDirty(enemyController);
        }
        AssetDatabase.SaveAssets();
    }

    static void BuildWeaponPrefabs()
    {
        // Material
        Material swordMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        Color sc;
        if (!ColorUtility.TryParseHtmlString("#C8D4E0", out sc)) sc = Color.white;
        swordMat.color = sc;
        AssetDatabase.CreateAsset(swordMat, "Assets/Prefabs/Weapons/SwordTrail_Mat.mat");

        Material staffMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        Color stc;
        if (!ColorUtility.TryParseHtmlString("#9B4DCA", out stc)) stc = Color.white;
        staffMat.color = stc;
        AssetDatabase.CreateAsset(staffMat, "Assets/Prefabs/Weapons/StaffTrail_Mat.mat");

        // Sword
        GameObject swordFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Weapons/Weapon_Sword.fbx");
        GameObject swordInst = (GameObject)PrefabUtility.InstantiatePrefab(swordFbx);
        swordInst.name = "Sword_Prefab";
        var swordMod = swordInst.AddComponent<WeaponModule>();
        swordMod.data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/ScriptableObjects/Weapons/Sword_Data.asset");

        GameObject swordHit = new GameObject("HitCollider");
        swordHit.transform.SetParent(swordInst.transform);
        swordHit.transform.localPosition = new Vector3(0, 0.6f, 0);
        var swordBox = swordHit.AddComponent<BoxCollider>();
        swordBox.isTrigger = true;
        swordBox.size = new Vector3(0.1f, 0.8f, 0.1f);
        swordHit.AddComponent<WeaponTrigger>();
        swordMod.attackCollider = swordBox;

        var swordTrail = swordInst.AddComponent<TrailRenderer>();
        swordTrail.time = 0.12f;
        swordTrail.minVertexDistance = 0.05f;
        swordTrail.startWidth = 0.04f;
        swordTrail.endWidth = 0f;
        swordTrail.material = swordMat;
        swordTrail.emitting = false;
        swordMod.weaponTrail = swordTrail;

        PrefabUtility.SaveAsPrefabAsset(swordInst, "Assets/Prefabs/Weapons/Sword_Prefab.prefab");
        Object.DestroyImmediate(swordInst);

        // Link to Data
        var swordData = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/ScriptableObjects/Weapons/Sword_Data.asset");
        swordData.weaponPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/Sword_Prefab.prefab");
        EditorUtility.SetDirty(swordData);

        // Staff
        GameObject staffFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Weapons/Weapon_Staff.fbx");
        GameObject staffInst = (GameObject)PrefabUtility.InstantiatePrefab(staffFbx);
        staffInst.name = "Staff_Prefab";
        var staffMod = staffInst.AddComponent<WeaponModule>();
        staffMod.data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/ScriptableObjects/Weapons/Staff_Data.asset");

        GameObject staffHit = new GameObject("HitCollider");
        staffHit.transform.SetParent(staffInst.transform);
        staffHit.transform.localPosition = new Vector3(0, 0.8f, 0);
        var staffBox = staffHit.AddComponent<BoxCollider>();
        staffBox.isTrigger = true;
        staffBox.size = new Vector3(0.15f, 0.4f, 0.15f);
        staffHit.AddComponent<WeaponTrigger>();
        staffMod.attackCollider = staffBox;

        var staffTrail = staffInst.AddComponent<TrailRenderer>();
        staffTrail.time = 0.2f;
        staffTrail.minVertexDistance = 0.05f;
        staffTrail.startWidth = 0.04f;
        staffTrail.endWidth = 0f;
        staffTrail.material = staffMat;
        staffTrail.emitting = false;
        staffMod.weaponTrail = staffTrail;

        PrefabUtility.SaveAsPrefabAsset(staffInst, "Assets/Prefabs/Weapons/Staff_Prefab.prefab");
        Object.DestroyImmediate(staffInst);

        var staffData = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/ScriptableObjects/Weapons/Staff_Data.asset");
        staffData.weaponPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/Staff_Prefab.prefab");
        EditorUtility.SetDirty(staffData);

        AssetDatabase.SaveAssets();
    }

    static void BuildCharacterPrefabs()
    {
        // Player
        GameObject playerFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Player_Warrior.fbx");
        GameObject pInst = (GameObject)PrefabUtility.InstantiatePrefab(playerFbx);
        pInst.name = "Player";
        pInst.tag = "Player";

        var cc = pInst.AddComponent<CharacterController>();
        cc.slopeLimit = 45;
        cc.stepOffset = 0.3f;
        cc.skinWidth = 0.08f;
        cc.minMoveDistance = 0.001f;
        cc.center = new Vector3(0, 0.9f, 0);
        cc.radius = 0.4f;
        cc.height = 1.8f;

        var pc = pInst.AddComponent<PlayerController>();
        pc.moveSpeed = 5;
        pc.rotationSpeed = 720;
        pc.rollSpeed = 8;
        pc.rollDuration = 0.4f;
        pc.availableWeapons = new WeaponData[] {
            AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/ScriptableObjects/Weapons/Sword_Data.asset"),
            AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/ScriptableObjects/Weapons/Staff_Data.asset")
        };

        // Find Bone WeaponAttach_R
        Transform[] bones = pInst.GetComponentsInChildren<Transform>();
        foreach (var b in bones) {
            if (b.name == "WeaponAttach_R") pc.weaponSocket = b;
        }

        var hs = pInst.AddComponent<HealthSystem>();
        hs.maxHealth = 100;
        hs.hitFlashColor = new Color(1, 0.2f, 0.2f, 1);
        hs.flashDuration = 0.1f;
        hs.flashRenderers = pInst.GetComponentsInChildren<Renderer>();

        var anim = pInst.GetComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/Player/PlayerAnimator.controller");

        var pi = pInst.AddComponent<PlayerInput>();
        pi.notificationBehavior = PlayerNotifications.SendMessages;

        PrefabUtility.SaveAsPrefabAsset(pInst, "Assets/Prefabs/Player/Player_Prefab.prefab");
        Object.DestroyImmediate(pInst);

        // Enemy
        GameObject enemyFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Enemy_Golem.fbx");
        GameObject eInst = (GameObject)PrefabUtility.InstantiatePrefab(enemyFbx);
        eInst.name = "Enemy_Golem";
        eInst.tag = "Enemy";

        var cap = eInst.AddComponent<CapsuleCollider>();
        cap.center = new Vector3(0, 1.1f, 0);
        cap.radius = 0.5f;
        cap.height = 2.2f;

        var nav = eInst.AddComponent<NavMeshAgent>();
        nav.speed = 2.5f;
        nav.angularSpeed = 300;
        nav.acceleration = 10;
        nav.stoppingDistance = 1.8f;
        nav.autoBraking = true;
        nav.radius = 0.4f;
        nav.height = 2.2f;
        nav.avoidancePriority = 50;

        var eai = eInst.AddComponent<EnemyAI>();
        eai.detectionRange = 8;
        eai.attackRange = 2.2f;
        eai.loseTargetRange = 15;
        eai.attackDamage = 15;
        eai.attackCooldown = 1.8f;
        eai.attackWindup = 0.6f;

        var ehs = eInst.AddComponent<HealthSystem>();
        ehs.maxHealth = 80;
        ehs.hitFlashColor = new Color(1, 0.3f, 0f, 1);
        ehs.flashDuration = 0.12f;
        ehs.flashRenderers = eInst.GetComponentsInChildren<Renderer>();

        var eanim = eInst.GetComponent<Animator>();
        eanim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/Enemy/EnemyAnimator.controller");

        PrefabUtility.SaveAsPrefabAsset(eInst, "Assets/Prefabs/Enemies/Enemy_Golem_Prefab.prefab");
        Object.DestroyImmediate(eInst);
    }

    static void BuildScene()
    {
        // Cannot reliably build full scenes in Editor scripts without active scene management,
        // but we can create the VFX prefab easily here.
        GameObject vfxInst = new GameObject("HitVFX");
        var ps = vfxInst.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startLifetime = 0.3f;
        main.startSpeed = 4f;
        main.startSize = 0.1f;
        var em = ps.emission;
        em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0, 12) });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        PrefabUtility.SaveAsPrefabAsset(vfxInst, "Assets/Prefabs/VFX/HitVFX_Prefab.prefab");
        Object.DestroyImmediate(vfxInst);
    }
}
