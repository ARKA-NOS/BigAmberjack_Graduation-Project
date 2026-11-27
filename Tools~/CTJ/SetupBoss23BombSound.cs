using System;
using _Scripts.Agent.Enemies;
using _Scripts.Audio;
using UnityEditor;
using UnityEngine;

public static class SetupBoss23BombSound
{
    private const string Folder = "Assets/_Prefabs/Enemies/Bosses/Enemy23/";

    public static object Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Use Edit Mode");
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Prefabs/Enemies/Effects/Audio/Clips/PlazmaBoom_Impact.wav");
        if (clip == null) throw new Exception("PlazmaBoom clip missing");

        string path = Folder + "Boss23_BombImpactSound.asset";
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
        data.FindProperty("_volume").floatValue = 0.8f;
        data.FindProperty("_pitchRange").vector2Value = Vector2.one;
        data.FindProperty("_priority").intValue = 128;
        data.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssetIfDirty(cue);

        var root = PrefabUtility.LoadPrefabContents(Folder + "Boss23_Bomb.prefab");
        try
        {
            var projectile = root.GetComponent<EnemyLobProjectile>();
            data = new SerializedObject(projectile);
            data.FindProperty("impactCue").objectReferenceValue = cue;
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, Folder + "Boss23_Bomb.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        return new { cue = path, clip = clip.name, volume = 0.8f, pitch = 1f, length = clip.length };
    }
}
