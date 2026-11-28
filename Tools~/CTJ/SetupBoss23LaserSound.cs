using System;
using _Scripts.Agent.Enemies;
using _Scripts.Agent.Enemies.Skills;
using _Scripts.Audio;
using UnityEditor;
using UnityEngine;

public static class SetupBoss23LaserSound
{
    public static object Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Use Edit Mode");

        const string folder = "Assets/_Prefabs/Enemies/Bosses/Enemy23/";
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Sound/Enemy/yodguard-short-energy-beam-shot-4-482500.mp3");
        if (clip == null) throw new Exception("Laser clip missing");
        string path = folder + "Boss23_FanLaserSound.asset";
        var cue = AssetDatabase.LoadAssetAtPath<SoundCueSO>(path);

        if (cue == null)
        {
            cue = ScriptableObject.CreateInstance<SoundCueSO>();
            AssetDatabase.CreateAsset(cue, path);
        }

        var data = new SerializedObject(cue);
        var clips = data.FindProperty("_clips");
        clips.arraySize = 1;
        clips.GetArrayElementAtIndex(0).objectReferenceValue = clip;
        data.FindProperty("_volume").floatValue = 0.7f;
        data.FindProperty("_pitchRange").vector2Value = Vector2.one;
        data.FindProperty("_priority").intValue = 128;
        data.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssetIfDirty(cue);

        var root = PrefabUtility.LoadPrefabContents(folder + "Boss23_ProductionControlDrone.prefab");

        try
        {
            var laser = root.GetComponentInChildren<DroneDownwardLaserSkill>(true);
            var sound = new SerializedObject(root.GetComponentInChildren<EnemySoundModule>(true));
            var entries = sound.FindProperty("attackSounds");
            bool connected = false;

            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("skill").objectReferenceValue != laser) continue;

                entry.FindPropertyRelative("cue").objectReferenceValue = cue;
                connected = true;
            }

            if (!connected) throw new Exception("Laser attack sound entry missing");
            sound.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, folder + "Boss23_ProductionControlDrone.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        return new { cue = path, clip = clip.name, volume = 0.7f, pitch = 1f };
    }
}
