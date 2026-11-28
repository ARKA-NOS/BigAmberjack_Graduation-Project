using _Scripts._Core._Manager;
using _Scripts._Core._ServiceLocator;
using _Scripts.Settings;
using UnityEngine;
using UnityEngine.Audio;

namespace _Scripts.Audio
{
    [DisallowMultipleComponent]
    public sealed class SoundManager : AbstractManager, ISoundManager
    {
        private const string MusicVolumeParameter = "MusicVolume";
        private const string SfxVolumeParameter = "SfxVolume";
        private const string UiVolumeParameter = "UiVolume";
        private const float MutedDecibels = -80f;

        [Header("Mixer Outputs")]
        [SerializeField] private AudioMixerGroup _musicOutput;
        [SerializeField] private AudioMixerGroup _sfxOutput;
        [SerializeField] private AudioMixerGroup _uiOutput;
        [SerializeField] private AudioClip _initialMusic;

        [Header("One-Shot Sources")]
        [SerializeField, Min(1)] private int _sfxCount = 12;
        [SerializeField, Min(1)] private int _uiCount = 2;
        [SerializeField, Range(0f, 1f)] private float _sfxSpatialBlend = 0.7f;
        [SerializeField, Min(0.01f)] private float _sfxMinDistance = 2f;
        [SerializeField, Min(0.01f)] private float _sfxMaxDistance = 16f;

        private AudioSource _musicSource;
        private Voice[] _sfxVoices;
        private Voice[] _uiVoices;
        private ISettingsManager _settings;
        private float _sfxGain = 1f;
        private float _uiGain = 1f;
        private ulong _playOrder;
        private bool _initialized;
        private bool _started;
        private bool _restartPending;

        private bool IsRegistered => ReferenceEquals(ServiceLocator.Get<ISoundManager>(), this);

        public override void Initialize()
        {
            if (_initialized) return;

            _initialized = true;

            if (isActiveAndEnabled) Register();
        }

        private void OnEnable()
        {
            if (_initialized) Register();
        }

        private void Start()
        {
            _started = true;
            StartAudio();
        }

        private void Update()
        {
            if (!_restartPending) return;

            _restartPending = false;
            StartAudio();
        }

        private void StartAudio()
        {
            if (!IsRegistered) return;

            BindSettings();

            if (_musicSource.clip == null) PlayMusic(_initialMusic);
        }

        private void Register()
        {
            if (ServiceLocator.Get<ISoundManager>() is SoundManager current && current != null && current != this)
            {
                Debug.LogWarning("SoundManager is already registered. Keep one active GameManager.", this);

                return;
            }

            if (_musicSource == null)
            {
                _musicSource = CreateSource("Music", _musicOutput, false);
                _musicSource.loop = true;
                _sfxVoices = CreateVoices(_sfxCount, "SFX", _sfxOutput, false);
                _uiVoices = CreateVoices(_uiCount, "UI", _uiOutput, true);
            }

            ServiceLocator.Register<ISoundManager>(this);
            // AudioMixer.SetFloat은 Start 이후에 호출한다. OnEnable에서 등록한 경우 Update까지 기다린다.
            _restartPending = _started;
        }

        private void BindSettings()
        {
            _settings = ServiceLocator.Get<ISettingsManager>();

            if (_settings != null) _settings.OnSettingsChanged += ApplyVolumes;

            ApplyVolumes(_settings?.Settings ?? SettingsData.Default);
        }

        private void ApplyVolumes(SettingsData data)
        {
            _musicSource.volume = SetOutputGain(_musicOutput, MusicVolumeParameter, data.musicVolume);
            _sfxGain = SetOutputGain(_sfxOutput, SfxVolumeParameter, data.effectsVolume);
            _uiGain = SetOutputGain(_uiOutput, UiVolumeParameter, data.effectsVolume);
            ApplyVoiceGain(_sfxVoices, _sfxGain);
            ApplyVoiceGain(_uiVoices, _uiGain);
        }

        private static float SetOutputGain(AudioMixerGroup group, string parameter, int percent)
        {
            float gain = Mathf.Clamp01(percent / (float)SettingsData.MaxPercent);
            float decibels = gain > 0f ? 20f * Mathf.Log10(gain) : MutedDecibels;

            return group != null && group.audioMixer.SetFloat(parameter, decibels) ? 1f : gain;
        }

        private static void ApplyVoiceGain(Voice[] voices, float gain)
        {
            foreach (Voice voice in voices)
                voice.Source.volume = voice.CueVolume * gain;
        }

        public void PlayMusic(AudioClip clip)
        {
            if (!IsRegistered || clip == null || _musicSource.clip == clip) return;

            _musicSource.clip = clip;
            _musicSource.Play();
        }

        public void StopMusic()
        {
            if (_musicSource == null) return;

            _musicSource.Stop();
            _musicSource.clip = null;
        }

        public void PlaySfx(SoundCueSO cue, Vector3 position)
        {
            if (IsRegistered && !AudioListener.pause)
                PlayOneShot(_sfxVoices, cue, position, _sfxGain);
        }

        public void PlayUi(SoundCueSO cue)
        {
            if (IsRegistered) PlayOneShot(_uiVoices, cue, transform.position, _uiGain);
        }

        private void PlayOneShot(Voice[] voices, SoundCueSO cue, Vector3 position, float gain)
        {
            if (cue == null) return;

            AudioClip clip = cue.PickClip();
            if (clip == null) return;

            int priority = cue.Priority;
            Voice voice = FindVoice(voices, priority);
            if (voice == null) return;

            voice.Source.Stop();
            voice.Source.transform.position = position;
            voice.Source.clip = clip;
            voice.Source.priority = priority;
            voice.Source.pitch = cue.PickPitch();
            voice.CueVolume = cue.Volume;
            voice.Source.volume = voice.CueVolume * gain;
            voice.PlayOrder = ++_playOrder;
            voice.Source.Play();
        }

        private static Voice FindVoice(Voice[] voices, int priority)
        {
            Voice replacement = null;

            foreach (Voice voice in voices)
            {
                if (!voice.Source.isPlaying) return voice;
                if (priority > voice.Source.priority) continue;

                if (replacement == null || voice.Source.priority > replacement.Source.priority ||
                    (voice.Source.priority == replacement.Source.priority && voice.PlayOrder < replacement.PlayOrder))
                {
                    replacement = voice;
                }
            }

            return replacement;
        }

        private Voice[] CreateVoices(int count, string label, AudioMixerGroup output, bool isUi)
        {
            var voices = new Voice[Mathf.Max(1, count)];

            for (int i = 0; i < voices.Length; i++)
            {
                AudioSource source = CreateSource($"{label} {i + 1}", output, isUi);

                if (!isUi)
                {
                    source.spatialBlend = _sfxSpatialBlend;
                    source.rolloffMode = AudioRolloffMode.Linear;
                    source.minDistance = _sfxMinDistance;
                    source.maxDistance = Mathf.Max(_sfxMinDistance, _sfxMaxDistance);
                }

                voices[i] = new Voice(source);
            }

            return voices;
        }

        private AudioSource CreateSource(string label, AudioMixerGroup output, bool ignoreListenerPause)
        {
            var child = new GameObject(label);

            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.outputAudioMixerGroup = output;
            source.ignoreListenerPause = ignoreListenerPause;
            source.dopplerLevel = 0f;

            return source;
        }

        public override void GameEnd()
        {
            _initialized = false;
            Release();
        }

        private void OnDisable() => Release();

        private void Release()
        {
            _restartPending = false;

            if (_settings != null) _settings.OnSettingsChanged -= ApplyVolumes;

            _settings = null;

            if (IsRegistered) ServiceLocator.Register<ISoundManager>(NullSoundManager.Instance);

            if (_musicSource == null) return;

            StopMusic();
            StopVoices(_sfxVoices);
            StopVoices(_uiVoices);
        }

        private static void StopVoices(Voice[] voices)
        {
            foreach (Voice voice in voices)
            {
                voice.Source.Stop();
                voice.Source.clip = null;
            }
        }

        private sealed class Voice
        {
            public readonly AudioSource Source;
            public float CueVolume = 1f;
            public ulong PlayOrder;

            public Voice(AudioSource source) => Source = source;
        }
    }
}
