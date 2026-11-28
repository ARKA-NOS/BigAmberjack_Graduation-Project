using System;
using System.Linq;
using _Scripts.Agent.Enemies;
using _Scripts.Agent.Enemies.Bosses;
using _Scripts.Agent.Enemies.Skills;
using _Scripts.Agent.Players;
using _Scripts.UI.Enemy;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SetupBoss23Rework
{
    private const string Folder = "Assets/_Prefabs/Enemies/Bosses/Enemy23/";
    private const string BossPath = Folder + "Boss23_ProductionControlDrone.prefab";
    private const string WarningPath = Folder + "Boss23_TargetBombWarning.prefab";
    private const string SkillsFolder = "Assets/_Scripts/Agent/Enemies/EnemySkills/Enemy23/";

    public static object Run()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new Exception("Use Edit Mode");
        var warning = AssetDatabase.LoadAssetAtPath<DroneBombTargetGraphic>(WarningPath);
        if (warning == null)
        {
            var temporary = new GameObject("Boss23_TargetBombWarning", typeof(RectTransform), typeof(Canvas), typeof(CanvasRenderer));
            try
            {
                var canvas = temporary.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 5;
                var graphic = temporary.AddComponent<DroneBombTargetGraphic>();
                graphic.color = new Color(1f, 0.55f, 0.55f, 0.9f);
                graphic.raycastTarget = false;
                warning = PrefabUtility.SaveAsPrefabAsset(temporary, WarningPath).GetComponent<DroneBombTargetGraphic>();
            }
            finally { Object.DestroyImmediate(temporary); }
        }

        var bombAsset = PrefabUtility.LoadPrefabContents(Folder + "Boss23_Bomb.prefab");
        try
        {
            var data = new SerializedObject(bombAsset.GetComponent<EnemyLobProjectile>());
            data.FindProperty("flightDuration").floatValue = 0.8f;
            data.FindProperty("arcHeight").floatValue = 0.25f;
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(bombAsset, Folder + "Boss23_Bomb.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(bombAsset); }

        var root = PrefabUtility.LoadPrefabContents(BossPath);
        try
        {
            bool active = root.activeSelf;
            Vector3 scale = root.transform.localScale;
            var owner = root.GetComponent<ProductionControlDrone>();
            var module = root.GetComponentInChildren<EnemySkillModule>(true);
            var visual = root.GetComponentInChildren<DroneVisualModule>(true);
            var animator = root.GetComponentInChildren<Animator>(true);
            var body = animator.GetComponent<SpriteRenderer>();
            var laser = root.GetComponentInChildren<DroneDownwardLaserSkill>(true);
            var bomb = root.GetComponentInChildren<DroneBombSkill>(true);
            laser.name = "FanLaser";
            bomb.name = "TargetBomb";
            var rapid = root.GetComponentsInChildren<DroneSalvoSkill>(true).SingleOrDefault(s => s.name != "RegularSalvo");
            if (rapid == null) throw new Exception("Barrage component missing");
            rapid.name = "RapidFire";
            var rapidData = new SerializedObject(rapid);
            var projectile = rapidData.FindProperty("projectilePrefab").objectReferenceValue;
            var muzzle = rapidData.FindProperty("muzzle").objectReferenceValue;

            var sound = root.GetComponentInChildren<EnemySoundModule>(true);
            var oldSounds = new SerializedObject(sound).FindProperty("attackSounds");
            Object rapidCue = null, laserCue = null, bombCue = null;
            for (int i = 0; i < oldSounds.arraySize; i++)
            {
                var entry = oldSounds.GetArrayElementAtIndex(i);
                var skill = entry.FindPropertyRelative("skill").objectReferenceValue;
                var cue = entry.FindPropertyRelative("cue").objectReferenceValue;
                if (skill == rapid) rapidCue = cue;
                if (skill == laser) laserCue = cue;
                if (skill == bomb) bombCue = cue;
            }

            var after = root.GetComponentInChildren<EnemyAfterImageModule>(true);
            if (after == null)
            {
                var child = new GameObject("AfterImageModule");
                child.transform.SetParent(root.transform, false);
                after = child.AddComponent<EnemyAfterImageModule>();
            }
            Set(after, "sourceRenderer", body);
            Set(after, "ghostPrefab", AssetDatabase.LoadAssetAtPath<AfterImageGhost>("Assets/_Prefabs/Player/AfterImageGhost.prefab"));

            var charge = root.GetComponentInChildren<DroneChargeSkill>(true) ?? Child<DroneChargeSkill>(module.transform, "Charge");
            var air = root.GetComponentInChildren<DroneAirTransitionSkill>(true) ?? Child<DroneAirTransitionSkill>(module.transform, "AirTransition");
            Set(charge, "visual", visual);
            Set(charge, "afterImages", after);
            Set(air, "visual", visual);
            Set(air, "muzzle", muzzle);
            Set(air, "projectilePrefab", projectile);
            Configure(charge, CopySkill("Charge"), 0.35f, 2f);
            Configure(air, CopySkill("AirTransition"), 0.15f, 2f);

            Set(rapid, "fallbackAttackDelay", 0.55f);
            Set(rapid, "projectileScale", 1f);
            Set(rapid, "range", 16f);
            Set(rapid, "firingDuration", 2.5f);
            Set(rapid, "normalVolleys", 7);
            Set(rapid, "lastStandVolleys", 12);
            Set(rapid, "verticalSpawnOffset", 0.3f);
            Set(rapid, "retreatDistance", 3f);
            Set(rapid, "retreatSpeed", 1.5f);
            Set(bomb, "range", 20f);
            Set(bomb, "warningPrefab", warning);
            Set(bomb, "telegraphPrefab", null);
            Set(bomb, "normalDrops", 3);
            Set(bomb, "lastStandDrops", 6);
            Set(bomb, "normalInterval", 1f);
            Set(bomb, "lastStandInterval", 0.5f);

            Set(owner, "charge", charge);
            Set(owner, "rapidFire", rapid);
            Set(owner, "airTransition", air);
            Set(owner, "laser", laser);
            Set(owner, "bomb", bomb);
            Set(owner, "lastStandHealthRatio", 0.35f);
            Set(owner, "airAttackAltitude", 6.5f);
            Set(owner, "lowAltitude", 0.9f);
            Set(owner, "terrainLayers", (1 << LayerMask.NameToLayer("Ground")) | (1 << LayerMask.NameToLayer("Platform")));

            var moduleData = new SerializedObject(module);
            var entries = moduleData.FindProperty("skills");
            EnemyAttackSkill[] skills = { charge, rapid, air, laser, bomb };
            entries.arraySize = skills.Length;
            for (int i = 0; i < skills.Length; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("skill").objectReferenceValue = skills[i];
                entry.FindPropertyRelative("useSkill").boolValue = true;
                entry.FindPropertyRelative("useForAutomaticSelection").boolValue = false;
                entry.FindPropertyRelative("requireGrounded").boolValue = false;
                entry.FindPropertyRelative("requireClearPath").boolValue = false;
                entry.FindPropertyRelative("maxHeightDifference").floatValue = -1f;
                entry.FindPropertyRelative("minRange").floatValue = 0f;
                entry.FindPropertyRelative("maxRange").floatValue = 20f;
            }
            moduleData.ApplyModifiedPropertiesWithoutUndo();

            var soundData = new SerializedObject(sound);
            var sounds = soundData.FindProperty("attackSounds");
            sounds.arraySize = 4;
            EnemyAttackSkill[] soundSkills = { rapid, air, laser, bomb };
            Object[] cues = { rapidCue, rapidCue, laserCue, bombCue };
            for (int i = 0; i < sounds.arraySize; i++)
            {
                sounds.GetArrayElementAtIndex(i).FindPropertyRelative("skill").objectReferenceValue = soundSkills[i];
                sounds.GetArrayElementAtIndex(i).FindPropertyRelative("cue").objectReferenceValue = cues[i];
            }
            soundData.ApplyModifiedPropertiesWithoutUndo();

            var obsolete = root.GetComponentsInChildren<DroneSalvoSkill>(true).FirstOrDefault(s => s != rapid);
            if (obsolete != null) Object.DestroyImmediate(obsolete.gameObject);
            if (root.activeSelf != active || root.transform.localScale != scale) throw new Exception("User root settings changed");
            PrefabUtility.SaveAsPrefabAsset(root, BossPath);
            return new { skills = skills.Select(s => s.name).ToArray(), active, scale = scale.ToString(),
                lastStandAt = 0.35f, airHeight = 6.5f, warning = WarningPath, afterImageReused = true };
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static T Child<T>(Transform parent, string name) where T : Component
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child.AddComponent<T>();
    }

    private static Object CopySkill(string name)
    {
        string path = SkillsFolder + "Boss23_ProductionControlDrone_" + name + ".asset";
        if (AssetDatabase.LoadMainAssetAtPath(path) == null &&
            !AssetDatabase.CopyAsset(SkillsFolder + "Boss23_ProductionControlDrone_RapidSalvo.asset", path))
            throw new Exception("Could not create skill: " + path);
        return AssetDatabase.LoadMainAssetAtPath(path);
    }

    private static void Configure(EnemyAttackSkill skill, Object data, float delay, float cooldown)
    {
        Set(skill, "<SkillSo>k__BackingField", data);
        Set(skill, "<BaseCoolTime>k__BackingField", cooldown);
        Set(skill, "fallbackAttackDelay", delay);
        Set(skill, "postBlinkDelay", 0f);
        Set(skill, "blinkBeforeAttack", false);
    }

    private static void Set(Object target, string name, object value)
    {
        var data = new SerializedObject(target);
        var field = data.FindProperty(name) ?? throw new Exception("Missing field: " + target.name + "/" + name);
        if (field.propertyType == SerializedPropertyType.ObjectReference) field.objectReferenceValue = value as Object;
        else if (field.propertyType == SerializedPropertyType.Float) field.floatValue = Convert.ToSingle(value);
        else if (field.propertyType == SerializedPropertyType.Boolean) field.boolValue = Convert.ToBoolean(value);
        else field.intValue = Convert.ToInt32(value);
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
