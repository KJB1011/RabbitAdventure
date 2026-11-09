using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 인게임내에서 게임의 전체적인 진행을 담당합니다.
/// 정해져있는 순서의 노드가 있고 그 노드의 순서에 맞게 게임이 진행됩니다.
/// 현재 포트폴리오 내에서는 : 전투 -> 상자 -> 전투 -> 상자 -> 상점 -> 보스  순으로 진행됩니다.
/// </summary>
public class IngameManager : MonoBehaviour
{
    public static IngameManager Instance { get; private set; }

    [SerializeField] NodeBase[] _nodes;

    [Header("노드 전환")]
    [SerializeField] float _nodeTransitionDelay = 5f;
    [SerializeField] GameObject _resultUI;
    [SerializeField] GameObject _gameoverUI;
    [SerializeField] GameObject _countdownUI;
    [SerializeField] TextMeshProUGUI _countdownText;
    [SerializeField] TextMeshProUGUI _maxDamageText;
    [SerializeField] TextMeshProUGUI _avgDPSText;
    [SerializeField] UISlidePanel _countdownPanel;
    [SerializeField] UISlidePanel _resultPanel;
    [SerializeField] UISlidePanel _gameoverPanel;

    [Header("HP Bar")]
    [SerializeField] UIHPBar _uiHpBar;

    [Header("골드")]
    [SerializeField] TextMeshProUGUI _goldText;
    public int Gold { get; set; } = 0;

    [Header("인트로 연출")]
    [SerializeField] CanvasGroup _gameUICanvas;    // 인게임 전체 UI의 CanvasGroup
    [SerializeField] float _uiFadeDuration = 2f;   // UI 페이드 인 시간
    [SerializeField] float _playerEnterTime = 1.2f; // 플레이어 등장 시간
    [SerializeField] Vector2 _playerStartPos = new Vector2(-5f, 0f); // 최종 위치
    [SerializeField] Vector2 _playerOffscreen = new Vector2(-20f, 0f); // 시작 위치 (화면 밖)
    PlayerController _player;

    int _currentIndex = 0;
    bool _isTransitioning = false;
    float _totalBattleTime = 0f;

    public bool _isBattle = false;
    public int _maxDamageInNode = 0;
    public int _totalDamage = 0;

    Vector3 _initCamPos;
    float _initOrthoSize;

    static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
    MaterialPropertyBlock _flashBlock;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        if (_isBattle)
            _totalBattleTime += Time.deltaTime;
    }

    public UIHPBar GetHPBar() => _uiHpBar;

    void Start()
    {
        _flashBlock = new MaterialPropertyBlock();

        var cam = Camera.main;
        if (cam != null)
        {
            _initCamPos = cam.transform.position;
            _initOrthoSize = cam.orthographicSize;
        }

        foreach (var node in _nodes)
            node.gameObject.SetActive(false);

        if (_resultUI != null) _resultUI.SetActive(false);
        if (_countdownUI != null) _countdownUI.SetActive(false);
        if (_gameoverUI != null) _gameoverUI.SetActive(false);

        SoundManager.Instance?.CrossFadeBGM("BGM/BattleBGM", 1.5f);

        SpawnPlayer();
        AddGold(30);

        // 인트로 연출(UI 페이드 + 플레이어 등장)과 "동시에" 이펙트·효과음을 미리 준비한다
        StartCoroutine(PrewarmRoutine());
        StartCoroutine(IntroRoutine());
    }

    // ── 미리 준비 (Prewarm) ─────────────────────────────────
    // 전투 중 처음 나오는 이펙트·효과음에서 렉이 걸리지 않도록,
    // 플레이어가 아직 조작하지 않는 인트로 동안 미리 불러와 둔다.
    private IEnumerator PrewarmRoutine()
    {
        if (VFXManager.Instance != null)
            yield return VFXManager.Instance.PrewarmRoutine();

        if (SoundManager.Instance != null)
            yield return SoundManager.Instance.PreloadRoutine();
    }
    private void SpawnPlayer()
    {
        var prefab = Resources.Load<GameObject>("Player");
        if (prefab == null)
        {
            Debug.LogError("[IngameManager] Resources/Player 프리팹을 찾을 수 없습니다.");
            return;
        }

        var go = Instantiate(prefab, _playerOffscreen, Quaternion.identity);
        _player = go.GetComponent<PlayerController>();

        if (_player == null)
        {
            Debug.LogError("[IngameManager] Player 프리팹에 PlayerController가 없습니다.");
            return;
        }

        // ArtifactManager에도 플레이어 연결
        if (ArtifactManager.Instance != null)
            ArtifactManager.Instance.SetPlayer(_player);

        _player.SetInitChar(RememberManager.Instance._nickname);
    }

    // ── 인트로 연출 ───────────────────────────────────────────
    private IEnumerator IntroRoutine()
    {
        if (_player != null)
        {
            _player.transform.position = _playerOffscreen;
            _player.SetControllable(false);
            _player.SetBoundary(false); // ← 경계 끄기
        }

        // UI 페이드 인
        if (_gameUICanvas != null)
        {
            _gameUICanvas.alpha = 0f;
            yield return _gameUICanvas.DOFade(1f, _uiFadeDuration)
                                      .SetUpdate(true)
                                      .WaitForCompletion();
        }
        else
            yield return new WaitForSeconds(_uiFadeDuration);

        // 플레이어 등장
        if (_player != null)
            yield return StartCoroutine(
                PlayerEnterFromLeft(_playerOffscreen, _playerStartPos, _playerEnterTime));

        // 등장 완료 후 경계 다시 켜기
        if (_player != null)
        {
            _player.SetBoundary(true);  // ← 경계 켜기
            _player.SetControllable(true);
        }

        ActivateNode(0);
    }

    private IEnumerator PlayerEnterFromLeft(Vector2 from, Vector2 to, float duration)
    {
        _player.transform.position = from;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 3f); // EaseOut
            _player.transform.position = Vector2.Lerp(from, to, t);
            yield return null;
        }

        _player.transform.position = to;
    }

    // ── 노드 전환 ─────────────────────────────────────────────
    private void OnNodeComplete()
    {
        bool isBattle = _isBattle;
        _isBattle = false;
        if (_isTransitioning) return;
        StartCoroutine(TransitionRoutine(isBattle));
    }

    private IEnumerator TransitionRoutine(bool isBattle)
    {
        _isTransitioning = true;

        // 중간 결과창 표시
        _countdownPanel.Show();

        float remaining = isBattle ? _nodeTransitionDelay : 3f;

        if (_maxDamageText != null && isBattle)
        {
            _maxDamageText.enabled = true;
            _maxDamageText.text = $"최고 데미지 : {_maxDamageInNode}";
        }
        if (_avgDPSText != null && isBattle && _totalBattleTime > 0)
        {
            _avgDPSText.enabled = true;
            _avgDPSText.text = $"평균 DPS : {(_totalDamage / _totalBattleTime):F1}";
        }

        while (remaining > 0f)
        {
            if (_countdownText != null)
                _countdownText.text = $"다음 구역까지 {Mathf.CeilToInt(remaining)}초";
            remaining -= Time.deltaTime;
            yield return null;
        }

        // 플레이어 강제로 왼쪽으로 이동
        if (_player != null)
            yield return StartCoroutine(_player.MoveToPositionRoutine(_playerStartPos));

        // 중간 결과창 비활성화 및 초기화
        bool hideDone = false;
        _countdownPanel.Hide(() => hideDone = true);
        yield return new WaitUntil(() => hideDone);

        _nodes[_currentIndex].gameObject.SetActive(false);
        _currentIndex++;
        _isTransitioning = false;

        _maxDamageInNode = 0;
        _totalDamage = 0;
        _totalBattleTime = 0f;
        if (_maxDamageText != null) _maxDamageText.enabled = false;
        if (_avgDPSText != null) _avgDPSText.enabled = false;

        if (_currentIndex < _nodes.Length)
            ActivateNode(_currentIndex);
        else
        {
            _player.GameOver();
            ShowResult();
        }
    }

    // 다른 노드(스테이지)로 전환
    private void ActivateNode(int index)
    {
        _nodes[index].gameObject.SetActive(true);
        _nodes[index].StartNode(OnNodeComplete);
        Debug.Log($"[IngameManager] Node {index} 시작: {_nodes[index].GetType().Name}");
    }

    // 현재 노드 재시작
    public void RestartCurrentNode()
    {
        _gameoverPanel?.Hide();

        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.DOKill();
            cam.DOKill();

            cam.transform.position = _initCamPos;
            if (cam.orthographic)
                cam.orthographicSize = _initOrthoSize;
        }

        _nodes[_currentIndex].gameObject.SetActive(false);

        var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
            Destroy(enemy.gameObject);
        VFXManager.Instance.ClearAllBullets();

        if (_player != null)
        {
            _player.ResetState();
            _player.transform.position = _playerStartPos;
        }

        _maxDamageInNode = 0;
        _totalDamage = 0;
        _totalBattleTime = 0f;
        _isBattle = false;

        GetHPBar()?.Hide();
        _countdownPanel?.Hide();

        ActivateNode(_currentIndex);
    }
    // ── 골드 ─────────────────────────────────────────────────
    public void AddGold(int amount)
    {
        Gold += amount;
        if (_goldText != null) _goldText.text = Gold.ToString();
    }

    public bool SpendGold(int amount)
    {
        if (Gold < amount) return false;
        Gold -= amount;
        if (_goldText != null) _goldText.text = Gold.ToString();
        return true;
    }

    // ── 전투 통계 ─────────────────────────────────────────────
    public void AddDamage(int damage)
    {
        _totalDamage += damage;
        if (_maxDamageInNode < damage)
            _maxDamageInNode = damage;
    }

    // ── 적 타격 효과 ─────────────────────────────────────────────
    /// <param name="duration">빛나는 시간 (초)</param>
    /// <param name="delay">빛나기 시작할 때까지 기다리는 시간 (초)</param>
    public void Flash(SpriteRenderer sr, float duration, float delay = 0f)
    {
        StartCoroutine(CoFlash(sr, duration, delay));
    }
    IEnumerator CoFlash(SpriteRenderer sr, float duration, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);
        if (sr == null) yield break; // 기다리는 사이 적이 사라졌을 수 있다

        sr.GetPropertyBlock(_flashBlock);
        _flashBlock.SetFloat(FlashAmountId, 1f);
        sr.SetPropertyBlock(_flashBlock);

        yield return new WaitForSecondsRealtime(duration);
        if (sr == null) yield break;

        sr.GetPropertyBlock(_flashBlock);
        _flashBlock.SetFloat(FlashAmountId, 0f);
        sr.SetPropertyBlock(_flashBlock);
    }

    int _hitStopCount; // 지금 진행 중인 히트스톱 수 (여러 적이 조금씩 다른 순간에 맞아도 꼬이지 않게)

    /// <param name="duration">멈추는 시간 (초)</param>
    /// <param name="delay">멈추기 시작할 때까지 기다리는 시간 (초). 칼 이펙트가 적을 지나가는 순간에 맞추기 위함</param>
    /// <param name="sfxPath">멈추는 순간 재생할 타격음 (없으면 null)</param>
    public void HitStop(float duration, float delay = 0f, string sfxPath = null)
    {
        StartCoroutine(CoHitStop(duration, delay, sfxPath));
    }
    IEnumerator CoHitStop(float duration, float delay, string sfxPath)
    {
        // 실제 시간으로 기다린다 → 그 사이 다른 적의 히트스톱이 먼저 걸려도 예정대로 이어서 멈춘다
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        _hitStopCount++;
        if (_hitStopCount == 1)
        {
            // 소리와 화면 흔들림은 멈춤이 처음 시작될 때 한 번만 (여러 적이 맞아도 겹치지 않게)
            if (!string.IsNullOrEmpty(sfxPath))
                SoundManager.Instance.PlaySFX(sfxPath);
            ShakeCamera();
        }

        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(duration);

        // 다른 적의 히트스톱이 아직 진행 중이면 그쪽이 끝날 때 재개한다
        _hitStopCount--;
        if (_hitStopCount == 0)
            Time.timeScale = 1;
    }
    // ── 화면 흔들림 ───────────────────────────────────────────
    public const string CamShakeId = "CamShake"; // 흔들림 트윈만 골라서 찾고 정리하기 위한 이름표

    /// <param name="duration">흔들리는 시간 (초)</param>
    /// <param name="strength">흔들림 세기 (월드 단위 거리)</param>
    public void ShakeCamera(float duration = 0.15f, float strength = 0.15f)
    {
        var cam = Camera.main;
        if (cam == null) return;

        // 여러 적이 동시에 맞아도 흔들림은 한 번만 (겹치면 너무 세지고 위치가 어긋남)
        if (DOTween.IsTweening(CamShakeId)) return;

        // 끝나면 원래 위치로 자동 복귀한다
        cam.transform.DOShakePosition(duration, strength, vibrato: 25, randomness: 90f, snapping: false, fadeOut: true)
                     .SetId(CamShakeId).SetUpdate(true);
    }

    // ── 기타 ─────────────────────────────────────────────────
    public void ShowResult()
    {
        _resultPanel.Show();
    }
    public void ShowGameover()
    {
        _gameoverPanel.Show();
    }
    public void SetHPBar(float value)
    {
        _uiHpBar?.SetBarValue(value);
    }

    public void SetPlayerControl(bool isOn)
    {
        _player.SetControllable(isOn);
    }
    public void OnClickTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("LobbyScene");
    }
    public void OnClickRestart()
    {
        Time.timeScale = 1f;
        RestartCurrentNode();
    }
}