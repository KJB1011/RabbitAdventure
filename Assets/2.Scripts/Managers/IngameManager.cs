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
    [SerializeField] ExitPortal _exitPortal;            // 클리어 후 오른쪽에 나타나는 출구 포털
    [SerializeField] BackgroundScroller _bgScroller;    // 이동하는 동안 배경을 빠르게 흘려보냄
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

        // 카운트다운 대신 출구 포털을 열고, 플레이어가 포털을 다 채울 때까지 기다린다
        // (안내 문구와 진행 게이지는 포털 위에 직접 표시된다 → 결과창 문구 자리는 비워 둔다)
        if (_countdownText != null)
            _countdownText.text = string.Empty;

        if (_exitPortal != null && _player != null)
        {
            bool entered = false;
            _exitPortal.Open(_player.transform, () => entered = true);
            yield return new WaitUntil(() => entered);
        }
        else
        {
            // 포털이 연결되지 않았을 때를 대비한 기존 방식 (3초 후 자동 이동)
            yield return new WaitForSeconds(3f);
        }

        // 중간 결과창은 플레이어가 이동하는 동안 함께 닫는다
        bool hideDone = false;
        _countdownPanel.Hide(() => hideDone = true);

        // 플레이어: 오른쪽 화면 밖으로 퇴장 → 왼쪽 화면 밖에서 다음 구역으로 등장
        if (_player != null)
        {
            _player.SetBoundary(false);   // 맵 경계 밖으로 나갈 수 있게
            _player.LockFacing(faceRight: true); // 이동하는 동안은 마우스와 상관없이 오른쪽을 본다

            // 배경이 빠르게 흘러가기 시작 → "앞으로 달려 나가는" 느낌
            if (_bgScroller != null) _bgScroller.StartBoost();

            // 퇴장 (진행 중인 스킬 정리·무적 초기화도 이 함수 안에서 처리됨)
            Vector2 exitPos = new Vector2(-_playerOffscreen.x, _player.transform.position.y);
            yield return StartCoroutine(_player.MoveToPositionRoutine(exitPos));

            // 등장 (게임 시작 인트로와 같은 방식)
            _player.SetControllable(false);

            // 플레이어가 자리에 도착하는 시간에 맞춰 배경도 서서히 원래 속도로
            if (_bgScroller != null) _bgScroller.StopBoost(_playerEnterTime);
            yield return StartCoroutine(PlayerEnterFromLeft(_playerOffscreen, _playerStartPos, _playerEnterTime));

            _player.SetBoundary(true);
            _player.SetControllable(true);
            _player.UnlockFacing();
        }

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
        if (_exitPortal != null) _exitPortal.Close(immediate: true);
        if (_bgScroller != null) _bgScroller.StopBoost(0f);
        if (_player != null) _player.UnlockFacing();

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

    // ── 디버그 (Inspector 버튼에서 호출: Editor/IngameManagerEditor.cs) ──────
#if UNITY_EDITOR
    public void DebugDoubleAtk()
    {
        if (_player == null) { Debug.LogWarning("[Debug] 플레이어가 아직 없습니다"); return; }
        _player.AddAtk(_player.Atk); // 지금 공격력만큼 더하기 = 2배
        Debug.Log($"[Debug] 공격력 2배 → {_player.Atk}");
    }

    public void DebugAddGold(int amount)
    {
        AddGold(amount);
        Debug.Log($"[Debug] 골드 +{amount} → {Gold}");
    }

    // 후보 중 아직 없는 아티팩트를 하나 골라 얻는다 (후보 목록은 에디터 스크립트가 프로젝트에서 찾아서 넘겨준다)
    public void DebugGiveRandomArtifact(System.Collections.Generic.IList<ArtifactSO> pool)
    {
        var manager = ArtifactManager.Instance;
        if (manager == null) { Debug.LogWarning("[Debug] ArtifactManager가 없습니다"); return; }
        if (manager.IsFull) { Debug.LogWarning($"[Debug] 아티팩트가 가득 찼습니다 (최대 {manager.MaxArtifacts}개)"); return; }

        var candidates = new System.Collections.Generic.List<ArtifactSO>();
        foreach (var artifact in pool)
            if (artifact != null && !manager.HasArtifact(artifact)) candidates.Add(artifact);

        if (candidates.Count == 0) { Debug.LogWarning("[Debug] 얻을 수 있는 아티팩트가 없습니다 (전부 보유 중)"); return; }
        manager.Acquire(candidates[Random.Range(0, candidates.Count)]);
    }
#endif

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