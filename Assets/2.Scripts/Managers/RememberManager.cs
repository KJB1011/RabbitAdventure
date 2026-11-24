using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬을 옮길때 갖고 있어야 할 정보들을 저장하는 용도입니다.
/// 씬을 옮길 때의 검은 화면(+로딩 표시)도 담당합니다.
/// </summary>
public class RememberManager : MonoBehaviour
{
    [SerializeField] UIGameExit _uiGameExitWindow;
    [SerializeField] GameObject _uiBlackOut;
    [SerializeField] private float _blackOutDuration = 1f;
    [Tooltip("검은 화면 위에 띄울 로딩 글씨 (_uiBlackOut의 자식). 비워 두면 검은 화면만 보인다")]
    [SerializeField] TMPro.TMP_Text _loadingText;
    [SerializeField] string _loadingBaseText = "불러오는 중";
    public static RememberManager Instance { get; private set; }

    public string _nickname { get; set; }

    const string NicknameKey = "Nickname";
    const string DefaultNickname = "토끼";

    /// <summary>지금 닉네임. 아직 정하지 않았으면 이 컴퓨터에 저장해 둔 닉네임(없으면 "토끼")</summary>
    public string Nickname => string.IsNullOrWhiteSpace(_nickname) ? PlayerPrefs.GetString(NicknameKey, DefaultNickname) : _nickname;

    /// <summary>닉네임을 정하고 이 컴퓨터에 저장한다 (다음에 게임을 켜도 유지)</summary>
    public void SaveNickname(string nickname)
    {
        _nickname = nickname;
        PlayerPrefs.SetString(NicknameKey, nickname);
        PlayerPrefs.Save();
    }

    /// <summary>연습방에서 고른 캐릭터 (CharacterSO.characterId). 비어 있으면 기본 캐릭터</summary>
    public string SelectedCharacterId { get; set; }

    /// <summary>씬을 불러오고 인게임이 준비를 마치기 전까지 true</summary>
    public bool IsLoading { get; private set; }

    /// <summary>검은 로딩 화면이 화면을 완전히 덮고 있는지 (덮여 있을 때만 이펙트를 몰래 그려 볼 수 있다)</summary>
    public bool IsLoadingScreenVisible => IsLoading && _uiBlackOut.activeSelf && _blackOutCanvas.alpha >= 0.99f;

    private CanvasGroup _blackOutCanvas;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        _blackOutCanvas = _uiBlackOut.GetComponent<CanvasGroup>();
        if (_blackOutCanvas == null)
            _blackOutCanvas = _uiBlackOut.AddComponent<CanvasGroup>();
        _blackOutCanvas.alpha = 0f;
        _uiBlackOut.SetActive(false);
        SetLoadingText(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_uiGameExitWindow != null)
            {
                if (_uiGameExitWindow.gameObject.activeSelf)
                {
                    _uiGameExitWindow.Close();
                }
                else
                {
                    _uiGameExitWindow.Open();
                }
            }
        }

        // "불러오는 중." → ".." → "..." 처럼 점이 움직이는 표시
        if (IsLoading && _loadingText != null)
        {
            int dots = (int)(Time.unscaledTime * 3f) % 4;
            _loadingText.text = _loadingBaseText + new string('.', dots);
        }
    }

    /// <summary>
    /// 게임 시작: 화면을 검게 덮고 → 로딩 표시 → 인게임 씬을 불러온다.
    /// 검은 화면은 여기서 걷지 않는다. 인게임 쪽(IngameManager)이 이펙트·효과음 준비를 끝내고
    /// HideLoadingRoutine()을 불러야 걷힌다. (준비하는 동안의 잔렉이 화면에 보이지 않게)
    /// </summary>
    public void GameStart(string userName)
    {
        if (_isStarting) return; // 버튼을 여러 번 눌러도 한 번만
        _isStarting = true;
        _nickname = userName;
        _uiBlackOut.SetActive(true);
        _blackOutCanvas.alpha = 0f;
        _blackOutCanvas.DOFade(1f, _blackOutDuration)
                       .OnComplete(() => StartCoroutine(LoadIngameRoutine()));
    }

    IEnumerator LoadIngameRoutine()
    {
        IsLoading = true;
        SetLoadingText(true);

        var op = SceneManager.LoadSceneAsync("IngameScene");
        while (!op.isDone) yield return null;

        SoundManager.Instance.CrossFadeBGM("BGM/BattleBGM", 1.5f);
    }

    /// <summary>인게임 준비가 끝났을 때 IngameManager가 부른다: 로딩 표시를 끄고 검은 화면을 걷는다</summary>
    public IEnumerator HideLoadingRoutine()
    {
        if (!IsLoading) yield break;
        IsLoading = false;
        SetLoadingText(false);
        yield return _blackOutCanvas.DOFade(0f, _blackOutDuration).WaitForCompletion();
        _uiBlackOut.SetActive(false);
        _isStarting = false;
    }

    bool _isStarting;

    void SetLoadingText(bool on)
    {
        if (_loadingText != null) _loadingText.gameObject.SetActive(on);
    }

    public void FadeOutBlack()
    {
        _uiBlackOut.SetActive(true);
        _blackOutCanvas.alpha = 1f;
        _blackOutCanvas.DOFade(0f, _blackOutDuration)
            .OnComplete(() => _uiBlackOut.SetActive(false));
    }
    public void OpenExitWindow()
    {
        _uiGameExitWindow.Open();
    }
}
