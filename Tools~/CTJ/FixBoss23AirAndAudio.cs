using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class FixBoss23AirAndAudio
{
    public const string ImpactPath = "Assets/_Prefabs/Enemies/Effects/Audio/Clips/PlazmaBoom_Impact.wav";
    private const string Anim = "Assets/_Graphics/Animations/Enemy/Enemy23/Boss23_ProductionControlDrone_";

    public static object Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Use Edit Mode");
        foreach (string name in new[] { "Rise", "Descend", "AirIdle", "AirShot", "AirShotFront", "AirShotRear" })
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + name + ".anim");
            var frames = AnimationUtility.GetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"));
            // 머리의 실제 높이와 소스 기준점으로 계산하고, 런타임에는 위치 곡선만 사용합니다.
            var values = frames.Select(frame =>
            {
                var sprite = (Sprite)frame.value;
                float originTop = sprite.texture.height - sprite.rect.y - sprite.pivot.y;
                return originTop - Roof(sprite) - 9.5f;
            }).ToArray();
            foreach (string child in new[] { "ProcessNumber", "AlarmLamp" })
            {
                float offset = child == "AlarmLamp" ? 10f : 0f;
                var curve = new AnimationCurve(frames.Select((frame, i) => new Keyframe(frame.time, (values[i] + offset) / 28f)).ToArray());
                for (int i = 0; i < curve.length; i++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
                    AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
                }
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(child, typeof(Transform), "m_LocalPosition.y"), curve);
            }
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssetIfDirty(clip);
        }

        var source = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Sound/Enemy/PlazmaBoom.mp3");
        source.LoadAudioData();
        var samples = new float[source.samples * source.channels];
        if (!source.GetData(samples, 0)) throw new Exception("Cannot decode PlazmaBoom");
        const float trimSeconds = 0.3f;
        int skip = Mathf.RoundToInt(trimSeconds * source.frequency);
        int framesLeft = source.samples - skip;
        int fadeFrames = Mathf.RoundToInt(0.005f * source.frequency);
        using (var writer = new BinaryWriter(File.Create(ImpactPath)))
        {
            int bytes = framesLeft * source.channels * 2;
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(bytes + 36);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
            writer.Write((short)1); writer.Write((short)source.channels); writer.Write(source.frequency);
            writer.Write(source.frequency * source.channels * 2); writer.Write((short)(source.channels * 2)); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(bytes);
            for (int frame = 0; frame < framesLeft; frame++)
                for (int channel = 0; channel < source.channels; channel++)
                {
                    float fade = Mathf.Clamp01(frame / (float)Mathf.Max(1, fadeFrames));
                    float value = samples[(frame + skip) * source.channels + channel] * fade;
                    writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(value, -1f, 1f) * short.MaxValue));
                }
        }
        AssetDatabase.ImportAsset(ImpactPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (AudioImporter)AssetImporter.GetAtPath(ImpactPath);
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.PCM;
        settings.preloadAudioData = true;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();
        var trimmed = AssetDatabase.LoadAssetAtPath<AudioClip>(ImpactPath);
        var cue = AssetDatabase.LoadAssetAtPath<_Scripts.Audio.SoundCueSO>("Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_BombImpactSound.asset");
        var data = new SerializedObject(cue);
        data.FindProperty("_clips").GetArrayElementAtIndex(0).objectReferenceValue = trimmed;
        data.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssetIfDirty(cue);
        return new { airClips = 6, trimmedSeconds = trimSeconds, originalLength = source.length,
            impactLength = trimmed.length, impact = ImpactPath };
    }

    private static int Roof(Sprite sprite)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
        try
        {
            var pixels = texture.GetPixels32();
            int start = Mathf.FloorToInt(sprite.rect.x / 128f) * 128;
            int roof = texture.height;
            for (int y = 10; y < 70; y++)
                for (int x = 40; x < 78; x++)
                {
                    var c = pixels[(texture.height - 1 - y) * texture.width + start + x];
                    if (c.a > 0 && c.r > 45 && c.r < 125 && c.g > c.r && c.g < 155 && c.b < c.g) roof = Mathf.Min(roof, y);
                }
            if (roof == texture.height) throw new Exception("Head roof not found: " + sprite.name);
            return roof;
        }
        finally { UnityEngine.Object.DestroyImmediate(texture); }
    }
}
