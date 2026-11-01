
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI del nivel: contador de gemas, texto de interacción,
/// notificaciones y pantalla final (victoria / derrota).
///
/// Solo escucha eventos de los demás sistemas; ninguno de ellos conoce a la UI.
/// </summary>
public class LevelUI : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private GemCollector _gemCollector;
    [SerializeField] private PlayerInteractor _interactor;
    [SerializeField] private Portal _portal;
    [SerializeField] private LevelFlow _levelFlow;

    [Header("Texts")]
    [SerializeField] private Text _gemsText;
    [SerializeField] private Text _promptText;
    [SerializeField] private Text _messageText;

    [Header("End Screen")]
    [SerializeField] private GameObject _endPanel;
    [SerializeField] private Text _endTitle;
    [SerializeField] private Button _restartButton;

    [Header("Messages")]
    [SerializeField] private float _messageDuration = 3f;
    [SerializeField] private string _startMessage = "Encontrá las gemas escondidas en los cofres";

    private Coroutine _messageRoutine;
    private WaitForSecondsRealtime _messageWait;

    private void Awake()
    {
        _messageWait = new WaitForSecondsRealtime(_messageDuration);
    }

    private void OnEnable()
    {
        if (_gemCollector != null) _gemCollector.OnGemsChanged += HandleGemsChanged;
        if (_interactor != null) _interactor.OnFocusChanged += HandleFocusChanged;
        if (_portal != null) _portal.OnActivated += HandlePortalActivated;

        if (_levelFlow != null)
        {
            _levelFlow.OnVictory += HandleVictory;
            _levelFlow.OnDefeat += HandleDefeat;
        }

        if (_restartButton != null && _levelFlow != null)
        {
            _restartButton.onClick.AddListener(_levelFlow.Restart);
        }
    }

    private void OnDisable()
    {
        if (_gemCollector != null) _gemCollector.OnGemsChanged -= HandleGemsChanged;
        if (_interactor != null) _interactor.OnFocusChanged -= HandleFocusChanged;
        if (_portal != null) _portal.OnActivated -= HandlePortalActivated;

        if (_levelFlow != null)
        {
            _levelFlow.OnVictory -= HandleVictory;
            _levelFlow.OnDefeat -= HandleDefeat;
        }

        if (_restartButton != null && _levelFlow != null)
        {
            _restartButton.onClick.RemoveListener(_levelFlow.Restart);
        }
    }

    private void Start()
    {
        _endPanel.SetActive(false);
        _promptText.text = string.Empty;

        int required = _gemCollector != null ? _gemCollector.RequiredGems : 0;
        UpdateGemsText(0, required);
        ShowMessage(_startMessage);
    }

    private void HandleGemsChanged(int current, int required)
    {
        UpdateGemsText(current, required);

        if (current < required)
        {
            ShowMessage($"¡Gema obtenida! Te faltan {required - current}");
        }
    }

    private void HandleFocusChanged(IInteractable interactable)
    {
        _promptText.text = interactable != null ? interactable.Prompt : string.Empty;
    }

    private void HandlePortalActivated()
    {
        ShowMessage("¡Tenés todas las gemas! El portal de la cima se abrió: seguí la luz");
    }

    private void HandleVictory() => ShowEndScreen("¡VICTORIA!");

    private void HandleDefeat() => ShowEndScreen("GAME OVER");

    private void ShowEndScreen(string title)
    {
        _promptText.text = string.Empty;
        _endTitle.text = title;
        _endPanel.SetActive(true);
    }

    private void UpdateGemsText(int current, int required)
    {
        _gemsText.text = $"Gemas: {current}/{required}";
    }

    private void ShowMessage(string message)
    {
        if (_messageRoutine != null)
        {
            StopCoroutine(_messageRoutine);
        }

        _messageRoutine = StartCoroutine(MessageRoutine(message));
    }

    private IEnumerator MessageRoutine(string message)
    {
        _messageText.text = message;
        yield return _messageWait;
        _messageText.text = string.Empty;
    }
}
