using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BGM / SFX 재생을 담당하는 매니저
/// Resources/Sounds/ 폴더에서 AudioClip을 로드합니다.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("BGM")]
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField][Range(0f, 1f)] private float _bgmVolume = 0.5f;

    [Header("SFX")]
    [SerializeField] private AudioSource _sfxSource;
    [SerializeField][Range(0f, 1f)] private float _sfxVolume = 1.0f;
    [SerializeField] private int _sfxPoolSize = 5; // 동시 재생 가능한 SFX 수

    // SFX 동시 재생을 위한 풀
    private AudioSource[] _sfxPool;
    private int _sfxPoolIndex = 0;

    // 로드한 클립 캐싱 (같은 클립을 매번 Resources.Load 하지 않도록)
    private Dictionary<string, AudioClip> _clipCache = new();

    [Header("미리 불러오기 (Preload)")]
    [Tooltip("인게임 시작 시 미리 불러올 효과음 (Resources/Sounds/ 기준 경로). 처음 재생할 때의 렉을 없애기 위함.")]
    [SerializeField] private string[] _preloadSfx =
    {
        "SFX/Player_Sword", "SFX/Player_SwordHit", "SFX/Player_StrongSword", "SFX/HeavyHit",
        "SFX/Hit", "SFX/Player_Dash", "SFX/Slain", "SFX/BulletFire", "SFX/Telegraph",
        "SFX/AreaDamage", "SFX/Player_SpecialCharge", "SFX/Player_SwordOneSlash", "SFX/GlassBreaking",
        "SFX/DropCoin", "SFX/CoinDropping", "SFX/PhaseChange", "SFX/Scratch",
    };
    [Tooltip("위 목록에 없는 Resources/Sounds/SFX 효과음도 전부 미리 불러온다")]
    [SerializeField] private bool _preloadAllSfx = true;

    public float BGMVolume => _bgmVolume;
    public float SFXVolume => _sfxVolume;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitSFXPool();
    }

    // ── 미리 불러오기 (Preload) ──────────────────────────────

    /// <summary>
    /// 목록의 효과음을 미리 캐시에 넣고, 소리 데이터(압축 해제)까지 준비해 둔다.
    /// 한 프레임에 몰지 않도록 클립 하나마다 한 프레임씩 나눠서 진행한다.
    /// </summary>
    public IEnumerator PreloadRoutine()
    {
        foreach (var path in _preloadSfx)
        {
            var clip = LoadClip(path); // ① 파일 불러오기 + 캐시에 저장 (기존 PlaySFX와 같은 캐시)

            // ② Preload Audio Data가 꺼진 클립은 Resources.Load만으로는 소리 데이터가 아직 없다.
            //    여기서 미리 읽어 두면 처음 재생할 때 데이터를 읽느라 끊기지 않는다.
            if (clip != null && clip.loadState == AudioDataLoadState.Unloaded)
                clip.LoadAudioData();

            yield return null;
        }

        // 목록에 없는 효과음도 전부 (새 효과음을 추가할 때마다 목록에 넣는 걸 잊어도 괜찮게)
        if (_preloadAllSfx)
        {
            foreach (var clip in Resources.LoadAll<AudioClip>("Sounds/SFX"))
            {
                string key = $"SFX/{clip.name}";            // PlaySFX에 넘기는 경로와 같은 모양
                if (!_clipCache.ContainsKey(key)) _clipCache[key] = clip;
                if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
            }
            yield return null;
        }
    }

    // ── 초기화 ───────────────────────────────────────────────

    private void InitSFXPool()
    {
        _sfxPool = new AudioSource[_sfxPoolSize];
        for (int i = 0; i < _sfxPoolSize; i++)
        {
            var go = new GameObject($"SFXSource_{i}");
            go.transform.SetParent(transform);
            _sfxPool[i] = go.AddComponent<AudioSource>();
            _sfxPool[i].volume = _sfxVolume;
            _sfxPool[i].playOnAwake = false;
        }
    }

    // ── BGM ──────────────────────────────────────────────────

    /// <summary>
    /// BGM을 재생합니다. 같은 클립이면 재시작하지 않습니다.
    /// path 예시: "BGM/MainTheme" (Resources/Sounds/ 이후 경로)
    /// </summary>
    public void PlayBGM(string path, bool loop = true)
    {
        var clip = LoadClip(path);
        if (clip == null) return;

        // 이미 같은 BGM이 재생 중이면 무시
        if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

        _bgmSource.clip = clip;
        _bgmSource.loop = loop;
        _bgmSource.volume = _bgmVolume;
        _bgmSource.Play();
    }

    /// <summary>BGM을 서서히 전환합니다.</summary>
    public void CrossFadeBGM(string path, float fadeDuration = 1f, bool loop = true)
    {
        StartCoroutine(CrossFadeRoutine(path, fadeDuration, loop));
    }

    public void StopBGM() => _bgmSource.Stop();
    public void PauseBGM() => _bgmSource.Pause();
    public void ResumeBGM() => _bgmSource.UnPause();

    public void SetBGMVolume(float volume)
    {
        _bgmVolume = Mathf.Clamp01(volume);
        _bgmSource.volume = _bgmVolume;
    }
    public void FadeInBGM(string path, float fadeDuration = 2f, bool loop = true)
    {
        StartCoroutine(FadeInRoutine(path, fadeDuration, loop));
    }

    private IEnumerator FadeInRoutine(string path, float fadeDuration, bool loop)
    {
        var clip = LoadClip(path);
        if (clip == null) yield break;

        _bgmSource.clip = clip;
        _bgmSource.loop = loop;
        _bgmSource.volume = 0f;
        _bgmSource.Play();

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _bgmSource.volume = Mathf.Lerp(0f, _bgmVolume, elapsed / fadeDuration);
            yield return null;
        }

        _bgmSource.volume = _bgmVolume;
    }
    // ── SFX ──────────────────────────────────────────────────

    /// <summary>
    /// SFX를 재생합니다.
    /// path 예시: "SFX/Attack" (Resources/Sounds/ 이후 경로)
    /// </summary>
    public void PlaySFX(string path)
    {
        var clip = LoadClip(path);
        if (clip == null) return;

        // 풀에서 현재 재생 중이지 않은 AudioSource 찾기
        AudioSource source = GetAvailableSFXSource();
        source.volume = _sfxVolume;
        source.PlayOneShot(clip);
    }

    /// <summary>SFX를 특정 볼륨으로 재생합니다.</summary>
    public void PlaySFX(string path, float volume)
    {
        var clip = LoadClip(path);
        if (clip == null) return;

        AudioSource source = GetAvailableSFXSource();
        source.PlayOneShot(clip, volume);
    }

    /// <summary>
    /// SFX를 음높이(pitch)를 바꿔서 재생합니다. 1 = 원래 소리, 1.3 = 더 높고 짧게, 0.8 = 더 낮고 길게.
    /// 같은 소리도 음높이를 바꾸면 다른 기술처럼 들린다 (예: 찌르기 = 기본 칼 소리를 높게).
    /// </summary>
    public void PlaySFXPitch(string path, float pitch, float volumeScale = 1f)
    {
        var clip = LoadClip(path);
        if (clip == null) return;

        AudioSource source = GetAvailableSFXSource();
        source.volume = _sfxVolume;
        source.pitch = pitch;
        source.PlayOneShot(clip, volumeScale);
    }

    /// <summary>
    /// 재생할 때마다 음높이를 ±variation만큼 무작위로 흔든다. (같은 타격음이 반복돼도 덜 지루하게)
    /// </summary>
    public void PlaySFXVaried(string path, float variation = 0.06f, float basePitch = 1f)
        => PlaySFXPitch(path, basePitch + Random.Range(-variation, variation));

    public void SetSFXVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
        foreach (var src in _sfxPool)
            src.volume = _sfxVolume;
    }
    /// <summary>SFX를 지정한 지속시간만큼만 재생 후 강제 중단합니다.</summary>
    public void PlaySFX(string path, float volume, float duration)
    {
        var clip = LoadClip(path);
        if (clip == null) return;

        AudioSource source = GetAvailableSFXSource();
        source.volume = _sfxVolume;
        source.clip = clip;
        source.loop = true; // 루프로 설정해서 duration 동안 끊기지 않게
        source.Play();

        StartCoroutine(StopSFXAfter(source, duration));
    }

    private IEnumerator StopSFXAfter(AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration);
        source.Stop();
        source.loop = false; // 루프 해제
        source.clip = null;
    }
    // ── 유틸 ─────────────────────────────────────────────────

    // 소스를 꺼낼 때 음높이는 항상 원래대로 (PlaySFXPitch가 바꿔 둔 값이 다음 소리에 남지 않게)
    private AudioSource GetAvailableSFXSource()
    {
        var source = FindSFXSource();
        source.pitch = 1f;
        return source;
    }

    private AudioSource FindSFXSource()
    {
        // 재생 중이지 않은 소스 우선 선택
        foreach (var src in _sfxPool)
            if (!src.isPlaying) return src;

        // 전부 사용 중이면 라운드 로빈
        var selected = _sfxPool[_sfxPoolIndex];
        _sfxPoolIndex = (_sfxPoolIndex + 1) % _sfxPoolSize;
        return selected;
    }

    private AudioClip LoadClip(string path)
    {
        if (_clipCache.TryGetValue(path, out var cached))
            return cached;

        var clip = Resources.Load<AudioClip>($"Sounds/{path}");
        if (clip == null)
        {
            Debug.LogWarning($"[SoundManager] 클립을 찾을 수 없습니다: Resources/Sounds/{path}");
            return null;
        }

        _clipCache[path] = clip;
        return clip;
    }

    private IEnumerator CrossFadeRoutine(string path, float duration, bool loop)
    {
        // 기존 BGM 페이드 아웃
        float startVolume = _bgmSource.volume;
        float elapsed = 0f;

        while (elapsed < duration * 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            _bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (duration * 0.5f));
            yield return null;
        }

        // 새 BGM으로 교체 후 페이드 인
        var clip = LoadClip(path);
        if (clip != null)
        {
            _bgmSource.clip = clip;
            _bgmSource.loop = loop;
            _bgmSource.Play();
        }

        elapsed = 0f;
        while (elapsed < duration * 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            _bgmSource.volume = Mathf.Lerp(0f, _bgmVolume, elapsed / (duration * 0.5f));
            yield return null;
        }

        _bgmSource.volume = _bgmVolume;
    }
}