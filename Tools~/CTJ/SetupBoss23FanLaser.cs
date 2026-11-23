using System;
using System.Linq;
using _Scripts.Agent.Enemies.Bosses;
using _Scripts.Agent.Enemies.Skills;
using _Scripts.UI.HitFeedback;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SetupBoss23FanLaser
{
    private const string Boss = "Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_ProductionControlDrone.prefab";
    private const string Warning = "Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_LaserWarning.prefab";
    private const string Material = "Assets/_Graphics/Materials/Enemy/DroneLaserFade.mat";
    private static void Reference(SerializedObject data, string field, Object value) => data.FindProperty(field).objectReferenceValue = value;

    public static object Run()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new Exception("Use Edit Mode");
        Shader shader = Shader.Find("Enemy/DroneLaserFade");
        if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new Exception("Laser shader failed to import");
        var material = AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(Material);
        if (material == null)
        {
            material = new UnityEngine.Material(shader) { name = "DroneLaserFade" };
            AssetDatabase.CreateAsset(material, Material);
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Warning) == null)
        {
            var warningRoot = PrefabUtility.LoadPrefabContents("Assets/_Prefabs/Enemies/Effects/AttackTelegraphCanvas.prefab");
            try
            {
                var view = warningRoot.GetComponent<AttackTelegraphUI>();
                var data = new SerializedObject(view);
                var oldGraphic = (UnityEngine.UI.Graphic)data.FindProperty("areaImage").objectReferenceValue;
                var area = oldGraphic.gameObject;
                Object.DestroyImmediate(oldGraphic);
                var graphic = area.AddComponent<ConvexPolygonGraphic>();
                graphic.raycastTarget = false;
                Reference(data, "areaImage", graphic);
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(warningRoot, Warning);
            }
            finally { PrefabUtility.UnloadPrefabContents(warningRoot); }
        }

        var root = PrefabUtility.LoadPrefabContents(Boss);
        try
        {
            bool active = root.activeSelf;
            Vector3 scale = root.transform.localScale;
            var boss = root.GetComponent<ProductionControlDrone>();
            var bossData = new SerializedObject(boss);
            bossData.FindProperty("laserAltitude").floatValue = 6.5f;
            bossData.ApplyModifiedPropertiesWithoutUndo();
            var laser = root.GetComponentInChildren<DroneDownwardLaserSkill>(true);
            var laserData = new SerializedObject(laser);
            laserData.FindProperty("length").floatValue = 7.5f;
            laserData.FindProperty("maxSpeed").floatValue = 140f;
            laserData.FindProperty("minSpeed").floatValue = 50f;
            laserData.FindProperty("fadeOutDuration").floatValue = 0.08f;
            laserData.FindProperty("sweepHalfAngle").floatValue = 60f;
            laserData.FindProperty("safeEdgeWidth").floatValue = 1.25f;
            laserData.FindProperty("endFadeDistance").floatValue = 1f;
            Reference(laserData, "telegraphPrefab", AssetDatabase.LoadAssetAtPath<AttackTelegraphUI>(Warning));
            laserData.ApplyModifiedPropertiesWithoutUndo();
            var beam = (SpriteRenderer)laserData.FindProperty("beam").objectReferenceValue;
            beam.sharedMaterial = material;

            var animator = root.GetComponentInChildren<Animator>(true);
            var body = animator.GetComponent<SpriteRenderer>();
            Reference(laserData, "bodyRenderer", body);
            var shake = laser.GetComponent<BossLaserShake>() ?? laser.gameObject.AddComponent<BossLaserShake>();
            var shakeData = new SerializedObject(shake);
            shakeData.FindProperty("amplitude").floatValue = 0.25f;
            shakeData.FindProperty("frequency").floatValue = 32f;
            shakeData.FindProperty("endStrength").floatValue = 1f;
            shakeData.FindProperty("fadeOutDuration").floatValue = 0.08f;
            shakeData.ApplyModifiedPropertiesWithoutUndo();
            Reference(laserData, "cameraShake", shake);
            laserData.ApplyModifiedPropertiesWithoutUndo();
            beam.sortingLayerID = body.sortingLayerID;
            beam.sortingOrder = body.sortingOrder - 1;
            var binding = EditorCurveBinding.FloatCurve("ProcessNumber", typeof(Transform), "m_LocalPosition.x");
            var clips = animator.runtimeAnimatorController.animationClips;
            var idle = clips.Single(clip => clip.name.EndsWith("_Idle", StringComparison.Ordinal));
            float targetX = -15.5f / 28f;
            float shift = targetX - AnimationUtility.GetEditorCurve(idle, binding).keys[0].value;
            int changed = 0;
            if (Mathf.Abs(shift) > 0.00001f)
                foreach (var clip in clips)
                {
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    if (curve == null) continue;
                    var keys = curve.keys;
                    for (int i = 0; i < keys.Length; i++) keys[i].value += shift;
                    curve.keys = keys;
                    AnimationUtility.SetEditorCurve(clip, binding, curve);
                    AssetDatabase.SaveAssetIfDirty(clip);
                    changed++;
                }
            var number = animator.transform.Find("ProcessNumber");
            Vector3 position = number.localPosition;
            position.x = targetX;
            number.localPosition = position;
            if (root.activeSelf != active || root.transform.localScale != scale) throw new Exception("User prefab settings changed");
            PrefabUtility.SaveAsPrefabAsset(root, Boss);
            return new { boss = Boss, numberShiftWorldLocal = shift, numberShiftPixels = shift * 28f,
                animationTracks = changed, preservedActive = active, preservedScale = scale.ToString(),
                warning = Warning, material = Material, shaderErrors = ShaderUtil.ShaderHasError(shader) };
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
