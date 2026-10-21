using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬을 옮길때 갖고 있어야 할 정보들을 저장하는 용도입니다.
/// </summary>
public class RememberManager : MonoBehaviour
{
    [SerializeField] UIGameExit _uiGameExitWindow;
    [SerializeField] GameObject _uiBlackOut;
    [SerializeField] private float _blackOutDuration = 1f;
    public static RememberManager Instance { get; private set; }

    public string _nickname { get; set; }

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
    }
    public void GameStart(string userName)
    {
        _nickname = userName;
        _uiBlackOut.SetActive(true);
        _blackOutCanvas.alpha = 0f;
        _blackOutCanvas.DOFade(1f, _blackOutDuration)
                       .OnComplete(() => {
                           SceneManager.LoadScene("IngameScene");
                           SoundManager.Instance.CrossFadeBGM("BGM/BattleBGM", 1.5f);
                           FadeOutBlack();
                       });
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