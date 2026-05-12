using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class AutoWiring : EditorWindow
{
    [MenuItem("Combat System/Run Auto Wiring")]
    public static void RunAutoWiring()
    {
        Debug.Log("Starting Auto Wiring...");
        
        EnsureFolders();
        ConfigureFBXImports();
        CreateScriptableObjects();
        CreateAnimatorControllers();
        
        Debug.Log("Auto Wiring Phase 1 Complete! Please see instructions for the rest.");
    }

    static void EnsureFolders()
    {
        string[] folders = {
            "Assets/ScriptableObjects/Weapons",
            "Assets/Prefabs/Player",
            "Assets/Prefabs/Enemies",
            "Assets/Prefabs/Weapons",
            "Assets/Prefabs/VFX",
            "Assets/Animations/Player",
            "Assets/Animations/Enemy",
            "Assets/Scenes"
        };

        bool createdAny = false;
        foreach (var folder in folders)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                createdAny = true;
            }
        }
        
        if (createdAny)
        {
            AssetDatabase.Refresh();
        }
    }

    static void ConfigureFBXImports()
    {
        string[] fbxPaths = {
            "Assets/Models/Characters/Player_Warrior.fbx",
            "Assets/Models/Characters/Enemy_Golem.fbx",
            "Assets/Models/Weapons/Weapon_Sword.fbx",
            "Assets/Models/Weapons/Weapon_Staff.fbx"
        };

        foreach (var path in fbxPaths)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null)
            {
                importer.globalScale = 1f;
                importer.useFileScale = true;
                
                if (path.Contains("Player_Warrior") || path.Contains("Enemy_Golem"))
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    importer.optimizeGameObjects = false; // Important for WeaponAttach_R
                }
                else
                {
                    importer.animationType = ModelImporterAnimationType.None;
                }
                
                importer.SaveAndReimport();
                Debug.Log("Configured " + path);
            }
            else
            {
                Debug.LogWarning("Could not find FBX at " + path);
            }
        }
    }

    static void CreateScriptableObjects()
    {
        // Sword
        WeaponData sword = ScriptableObject.CreateInstance<WeaponData>();
        sword.weaponName = "Warrior Sword";
        sword.weaponType = WeaponType.Melee;
        sword.damage = 30f;
        sword.attackRange = 1.8f;
        sword.attackCooldown = 0.45f;
        sword.comboWindow = 0.8f;
        sword.maxComboHits = 3;
        sword.comboDamageMultipliers = new float[] { 1.0f, 1.3f, 2.0f };
        sword.specialDamage = 60f;
        sword.specialCooldown = 5.0f;
        sword.attackAnimNames = new string[] { "Attack0", "Attack1", "Attack2" };
        sword.specialAnimName = "Attack2";
        
        AssetDatabase.CreateAsset(sword, "Assets/ScriptableObjects/Weapons/Sword_Data.asset");

        // Staff
        WeaponData staff = ScriptableObject.CreateInstance<WeaponData>();
        staff.weaponName = "Magic Staff";
        staff.weaponType = WeaponType.Magic;
        staff.damage = 20f;
        staff.attackRange = 2.5f;
        staff.attackCooldown = 0.40f;
        staff.comboWindow = 0.7f;
        staff.maxComboHits = 3;
        staff.comboDamageMultipliers = new float[] { 1.0f, 1.2f, 1.5f };
        staff.specialDamage = 90f;
        staff.specialCooldown = 5.0f;
        staff.attackAnimNames = new string[] { "Attack0", "Attack1", "Attack0" };
        staff.specialAnimName = "Attack1";

        AssetDatabase.CreateAsset(staff, "Assets/ScriptableObjects/Weapons/Staff_Data.asset");
        
        AssetDatabase.SaveAssets();
        Debug.Log("Created ScriptableObjects.");
    }

    static void CreateAnimatorControllers()
    {
        // Player Animator
        string playerAnimPath = "Assets/Animations/Player/PlayerAnimator.controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(playerAnimPath) == null)
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(playerAnimPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("AttackIndex", AnimatorControllerParameterType.Int);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Roll", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);
            
            var rootStateMachine = controller.layers[0].stateMachine;
            rootStateMachine.AddState("Idle");
            rootStateMachine.AddState("Walk");
            rootStateMachine.AddState("Attack0");
            rootStateMachine.AddState("Attack1");
            rootStateMachine.AddState("Attack2");
            rootStateMachine.AddState("Roll");
            rootStateMachine.AddState("Death");
            Debug.Log("Created Player Animator Controller.");
        }

        // Enemy Animator
        string enemyAnimPath = "Assets/Animations/Enemy/EnemyAnimator.controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(enemyAnimPath) == null)
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(enemyAnimPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("HitReact", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);
            
            var rootStateMachine = controller.layers[0].stateMachine;
            rootStateMachine.AddState("Idle");
            rootStateMachine.AddState("Walk");
            rootStateMachine.AddState("Attack");
            rootStateMachine.AddState("HitReact");
            rootStateMachine.AddState("Death");
            Debug.Log("Created Enemy Animator Controller.");
        }
    }
}
