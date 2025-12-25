using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivors.Utilities
{
    /// <summary>
    /// 게임 오디오(BGM, SFX)를 관리하는 싱글톤 매니저.
    /// BGM 페이드 인/아웃, SFX 풀링, 볼륨 조절 기능을 제공합니다.
    /// </summary>
    /// <example>
    /// // BGM 재생
    /// AudioManager.Instance.PlayBGM(bgmClip);
    /// AudioManager.Instance.PlayBGM(bgmClip, fadeTime: 1f); // 페이드 효과
    ///
    /// // SFX 재생
    /// AudioManager.Instance.PlaySFX(hitSound);
    /// AudioManager.Instance.PlaySFXAtPosition(explosionSound, transform.position);
    ///
    /// // 볼륨 조절
    /// AudioManager.Instance.MasterVolume = 0.8f;
    /// AudioManager.Instance.BGMVolume = 0.5f;
    /// </example>
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("Audio Sources")]
        /// <summary>BGM 재생용 AudioSource (인스펙터에서 설정 가능)</summary>
        [SerializeField] private AudioSource _bgmSource;

        /// <summary>단발성 SFX 재생용 AudioSource</summary>
        [SerializeField] private AudioSource _sfxSource;

        [Header("Settings")]
        /// <summary>위치 기반 SFX용 AudioSource 풀 크기</summary>
        [SerializeField] private int _sfxPoolSize = 10;

        /// <summary>위치 기반 SFX 재생을 위한 AudioSource 풀</summary>
        private List<AudioSource> _sfxPool = new List<AudioSource>();

        /// <summary>자주 사용되는 AudioClip 캐시</summary>
        private Dictionary<string, AudioClip> _audioClipCache = new Dictionary<string, AudioClip>();

        /// <summary>마스터 볼륨 (0.0 ~ 1.0)</summary>
        private float _masterVolume = 1f;

        /// <summary>BGM 볼륨 (0.0 ~ 1.0)</summary>
        private float _bgmVolume = 1f;

        /// <summary>SFX 볼륨 (0.0 ~ 1.0)</summary>
        private float _sfxVolume = 1f;

        /// <summary>
        /// 마스터 볼륨.
        /// 모든 오디오에 곱해지는 최종 볼륨입니다.
        /// </summary>
        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                _masterVolume = Mathf.Clamp01(value);
                UpdateVolumes();
            }
        }

        /// <summary>
        /// BGM 볼륨.
        /// 실제 볼륨은 MasterVolume * BGMVolume 입니다.
        /// </summary>
        public float BGMVolume
        {
            get => _bgmVolume;
            set
            {
                _bgmVolume = Mathf.Clamp01(value);
                UpdateVolumes();
            }
        }

        /// <summary>
        /// SFX 볼륨.
        /// 실제 볼륨은 MasterVolume * SFXVolume 입니다.
        /// </summary>
        public float SFXVolume
        {
            get => _sfxVolume;
            set
            {
                _sfxVolume = Mathf.Clamp01(value);
                UpdateVolumes();
            }
        }

        /// <summary>
        /// 싱글톤 초기화.
        /// AudioSource 컴포넌트와 SFX 풀을 생성합니다.
        /// </summary>
        protected override void OnSingletonAwake()
        {
            InitializeAudioSources();
            InitializeSFXPool();
        }

        /// <summary>
        /// BGM/SFX용 AudioSource를 초기화합니다.
        /// 인스펙터에서 설정하지 않은 경우 자동 생성합니다.
        /// </summary>
        private void InitializeAudioSources()
        {
            // BGM AudioSource 생성
            if (_bgmSource == null)
            {
                var bgmObj = new GameObject("BGM Source");
                bgmObj.transform.SetParent(transform);
                _bgmSource = bgmObj.AddComponent<AudioSource>();
                _bgmSource.loop = true;      // BGM은 기본적으로 반복
                _bgmSource.playOnAwake = false;
            }

            // 단발성 SFX AudioSource 생성
            if (_sfxSource == null)
            {
                var sfxObj = new GameObject("SFX Source");
                sfxObj.transform.SetParent(transform);
                _sfxSource = sfxObj.AddComponent<AudioSource>();
                _sfxSource.playOnAwake = false;
            }
        }

        /// <summary>
        /// 위치 기반 SFX 재생을 위한 AudioSource 풀을 생성합니다.
        /// 동시에 여러 위치에서 SFX를 재생할 수 있습니다.
        /// </summary>
        private void InitializeSFXPool()
        {
            var poolContainer = new GameObject("SFX Pool");
            poolContainer.transform.SetParent(transform);

            for (int i = 0; i < _sfxPoolSize; i++)
            {
                var sfxObj = new GameObject($"SFX_{i}");
                sfxObj.transform.SetParent(poolContainer.transform);
                var source = sfxObj.AddComponent<AudioSource>();
                source.playOnAwake = false;
                _sfxPool.Add(source);
            }
        }

        /// <summary>
        /// 볼륨 설정 변경 시 실제 AudioSource 볼륨을 업데이트합니다.
        /// </summary>
        private void UpdateVolumes()
        {
            if (_bgmSource != null)
                _bgmSource.volume = _masterVolume * _bgmVolume;

            if (_sfxSource != null)
                _sfxSource.volume = _masterVolume * _sfxVolume;
        }

        #region BGM

        /// <summary>
        /// BGM을 재생합니다.
        /// </summary>
        /// <param name="clip">재생할 AudioClip</param>
        /// <param name="fadeTime">페이드 인/아웃 시간 (0이면 즉시 전환)</param>
        public void PlayBGM(AudioClip clip, float fadeTime = 0f)
        {
            if (clip == null) return;

            if (fadeTime > 0f)
            {
                // 페이드 효과로 BGM 전환
                StartCoroutine(FadeBGM(clip, fadeTime));
            }
            else
            {
                // 즉시 BGM 전환
                _bgmSource.clip = clip;
                _bgmSource.volume = _masterVolume * _bgmVolume;
                _bgmSource.Play();
            }
        }

        /// <summary>
        /// BGM을 정지합니다.
        /// </summary>
        /// <param name="fadeTime">페이드 아웃 시간 (0이면 즉시 정지)</param>
        public void StopBGM(float fadeTime = 0f)
        {
            if (fadeTime > 0f)
            {
                StartCoroutine(FadeOutBGM(fadeTime));
            }
            else
            {
                _bgmSource.Stop();
            }
        }

        /// <summary>BGM을 일시정지합니다.</summary>
        public void PauseBGM() => _bgmSource.Pause();

        /// <summary>일시정지된 BGM을 재개합니다.</summary>
        public void ResumeBGM() => _bgmSource.UnPause();

        /// <summary>
        /// 현재 BGM을 페이드 아웃하고 새 BGM을 페이드 인합니다.
        /// </summary>
        /// <param name="newClip">새로 재생할 BGM</param>
        /// <param name="fadeTime">페이드 시간</param>
        private System.Collections.IEnumerator FadeBGM(AudioClip newClip, float fadeTime)
        {
            float startVolume = _bgmSource.volume;

            // 페이드 아웃
            while (_bgmSource.volume > 0)
            {
                _bgmSource.volume -= startVolume * Time.deltaTime / fadeTime;
                yield return null;
            }

            // BGM 전환
            _bgmSource.Stop();
            _bgmSource.clip = newClip;
            _bgmSource.Play();

            // 페이드 인
            while (_bgmSource.volume < _masterVolume * _bgmVolume)
            {
                _bgmSource.volume += startVolume * Time.deltaTime / fadeTime;
                yield return null;
            }

            _bgmSource.volume = _masterVolume * _bgmVolume;
        }

        /// <summary>
        /// BGM을 페이드 아웃하여 정지합니다.
        /// </summary>
        /// <param name="fadeTime">페이드 아웃 시간</param>
        private System.Collections.IEnumerator FadeOutBGM(float fadeTime)
        {
            float startVolume = _bgmSource.volume;

            while (_bgmSource.volume > 0)
            {
                _bgmSource.volume -= startVolume * Time.deltaTime / fadeTime;
                yield return null;
            }

            _bgmSource.Stop();
            _bgmSource.volume = startVolume;
        }

        #endregion

        #region SFX

        /// <summary>
        /// SFX를 재생합니다 (2D 사운드).
        /// PlayOneShot을 사용하여 동시에 여러 SFX 재생이 가능합니다.
        /// </summary>
        /// <param name="clip">재생할 AudioClip</param>
        /// <param name="volumeScale">볼륨 스케일 (0.0 ~ 1.0, 기본: 1.0)</param>
        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;

            _sfxSource.PlayOneShot(clip, _masterVolume * _sfxVolume * volumeScale);
        }

        /// <summary>
        /// 특정 위치에서 SFX를 재생합니다 (3D 사운드).
        /// SFX 풀에서 사용 가능한 AudioSource를 찾아 사용합니다.
        /// </summary>
        /// <param name="clip">재생할 AudioClip</param>
        /// <param name="position">재생 위치</param>
        /// <param name="volumeScale">볼륨 스케일 (0.0 ~ 1.0, 기본: 1.0)</param>
        public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1f)
        {
            if (clip == null) return;

            AudioSource source = GetAvailableSFXSource();
            if (source != null)
            {
                source.transform.position = position;
                source.clip = clip;
                source.volume = _masterVolume * _sfxVolume * volumeScale;
                source.Play();
            }
        }

        /// <summary>
        /// SFX 풀에서 사용 가능한(재생 중이 아닌) AudioSource를 반환합니다.
        /// 모두 사용 중이면 첫 번째 AudioSource를 반환합니다.
        /// </summary>
        /// <returns>사용 가능한 AudioSource</returns>
        private AudioSource GetAvailableSFXSource()
        {
            // 재생 중이 아닌 AudioSource 찾기
            foreach (var source in _sfxPool)
            {
                if (!source.isPlaying)
                    return source;
            }

            // 모두 사용 중이면 첫 번째 반환 (기존 사운드 덮어쓰기)
            return _sfxPool[0];
        }

        #endregion

        /// <summary>
        /// AudioClip을 캐시에 저장합니다.
        /// 자주 사용되는 클립을 미리 캐싱하여 성능을 향상시킵니다.
        /// </summary>
        /// <param name="key">캐시 키</param>
        /// <param name="clip">저장할 AudioClip</param>
        public void CacheClip(string key, AudioClip clip)
        {
            if (!_audioClipCache.ContainsKey(key))
                _audioClipCache[key] = clip;
        }

        /// <summary>
        /// 캐시에서 AudioClip을 가져옵니다.
        /// </summary>
        /// <param name="key">캐시 키</param>
        /// <returns>캐시된 AudioClip (없으면 null)</returns>
        public AudioClip GetCachedClip(string key)
        {
            _audioClipCache.TryGetValue(key, out AudioClip clip);
            return clip;
        }
    }
}
