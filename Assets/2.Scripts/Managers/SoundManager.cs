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

    private AudioSource GetAvailableSFXSource()
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