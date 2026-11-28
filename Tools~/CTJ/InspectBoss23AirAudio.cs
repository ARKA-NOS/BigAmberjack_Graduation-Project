using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class InspectBoss23AirAudio
{
    private const string Anim = "Assets/_Graphics/Animations/Enemy/Enemy23/Boss23_ProductionControlDrone_";

    public static object CheckTrim()
    {
        const string path = "Assets/_Prefabs/Enemies/Effects/Audio/Clips/PlazmaBoom_Impact.wav";
        var source = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Sound/Enemy/PlazmaBoom.mp3");
        var trimmed = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        source.LoadAudioData();
        trimmed.LoadAudioData();

        int skip = Mathf.RoundToInt(0.3f * source.frequency);
        int fadeFrames = Mathf.RoundToInt(0.005f * source.frequency);
        if (trimmed.samples != source.samples - skip || trimmed.frequency != source.frequency || trimmed.channels != source.channels)
            throw new Exception("Trimmed clip format/duration mismatch");

        var originalSamples = new float[source.samples * source.channels];
        var trimmedSamples = new float[trimmed.samples * trimmed.channels];
        if (!source.GetData(originalSamples, 0) || !trimmed.GetData(trimmedSamples, 0))
            throw new Exception("Cannot read decoded audio samples");

        float maxError = 0f;
        for (int i = 0; i < trimmedSamples.Length; i++)
        {
            float fade = Mathf.Clamp01((i / trimmed.channels) / (float)fadeFrames);
            float expected = originalSamples[skip * source.channels + i] * fade;
            maxError = Mathf.Max(maxError, Mathf.Abs(trimmedSamples[i] - expected));
        }
        if (maxError > 0.00006f) throw new Exception("Unexpected change after the cut: " + maxError);

        var cue = new SerializedObject(AssetDatabase.LoadAssetAtPath<_Scripts.Audio.SoundCueSO>(
            "Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_BombImpactSound.asset"));
        if (cue.FindProperty("_clips").GetArrayElementAtIndex(0).objectReferenceValue != trimmed)
            throw new Exception("Bomb impact cue does not reference the trimmed clip");

        var settings = ((AudioImporter)AssetImporter.GetAtPath(path)).defaultSampleSettings;
        if (!settings.preloadAudioData || settings.loadType != AudioClipLoadType.DecompressOnLoad)
            throw new Exception("Impact audio is not preloaded");

        return new { originalSeconds = source.length, trimmedSeconds = trimmed.length, removedSeconds = 0.3f,
            fadeInSeconds = 0.005f, maxSampleError = maxError, cueReferenceValid = true, preloaded = true };
    }

    public static object Inspect()
    {
        var rows = new List<object>();
        foreach (string name in new[] { "Rise", "Descend", "AirIdle", "AirShotFront", "AirShotRear" })
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + name + ".anim");
            var frames = AnimationUtility.GetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"));
            var number = AnimationUtility.GetEditorCurve(clip, Y("ProcessNumber"));
            var lamp = AnimationUtility.GetEditorCurve(clip, Y("AlarmLamp"));
            foreach (var frame in frames)
            {
                var sprite = (Sprite)frame.value;
                int roof = Roof(sprite);
                float originTop = sprite.texture.height - sprite.rect.y - sprite.pivot.y;
                float depth = originTop - number.Evaluate(frame.time) * 28f - roof;
                rows.Add(new { state = name, sprite = sprite.name, time = frame.time, originTop, roof,
                    numberPixels = number.Evaluate(frame.time) * 28f, lampPixels = lamp.Evaluate(frame.time) * 28f,
                    depthBelowRoof = depth, targetNumberPixels = originTop - roof - 9.5f });
            }
        }
        var audio = new List<object>();
        foreach (string path in new[] { "Assets/_Sound/Enemy/PlazmaBoom.mp3", "Assets/_Prefabs/Enemies/Effects/Audio/Clips/CoinDrop.wav" })
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            clip.LoadAudioData();
            var data = new float[clip.samples * clip.channels];
            if (!clip.GetData(data, 0)) throw new Exception("Cannot read audio: " + path);
            int first = Array.FindIndex(data, sample => Mathf.Abs(sample) > 0.01f);
            audio.Add(new { path, clip.length, firstSignalSeconds = first / (float)(clip.channels * clip.frequency) });
        }
        var coin = new SerializedObject(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemies/Effects/EnemyRewardCoin.prefab")
            .GetComponent<_Scripts.Agent.Enemies.EnemyRewardCoin>());
        return new { air = rows, audio, coinCollectDuration = coin.FindProperty("collectFrames").arraySize /
            coin.FindProperty("collectFramesPerSecond").floatValue };
    }

    public static int Roof(Sprite sprite)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
        try
        {
            var pixels = texture.GetPixels32();
            int start = Mathf.FloorToInt(sprite.rect.x / 128f) * 128;
            int roof = texture.height;
            for (int y = 10; y < 70; y++)
                for (int x = 40; x < 78; x++)
                {
                    Color32 c = pixels[(texture.height - 1 - y) * texture.width + start + x];
                    if (c.a > 0 && c.r > 45 && c.r < 125 && c.g > c.r && c.g < 155 && c.b < c.g) roof = Mathf.Min(roof, y);
                }
            if (roof == texture.height) throw new Exception("Head roof not found: " + sprite.name);
            return roof;
        }
        finally { Object.DestroyImmediate(texture); }
    }

    private static EditorCurveBinding Y(string child) => EditorCurveBinding.FloatCurve(child, typeof(Transform), "m_LocalPosition.y");
}
