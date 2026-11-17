
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Caja de diálogo en pantalla.
///
/// Muestra las líneas de un DialogueData letra por letra y avanza con
/// E, Espacio o Enter. Mientras está abierta el juego queda en pausa.
/// Al terminar ejecuta el callback recibido (por ejemplo, empezar la misión).
/// </summary>
public class DialogueUI : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private Text _speakerText;
    [SerializeField] private Text _bodyText;
    [SerializeField] private Text _hintText;
    [SerializeField] private float _charactersPerSecond = 45f;

    private DialogueData _dialogue;
    private int _lineIndex;
    private Action _onFinished;

    private Coroutine _typingRoutine;
    private bool _isTyping;

    private int _openedFrame;
    private int _closedFrame = -10;
    private float _previousTimeScale = 1f;

    public bool IsOpen { get; private set; }

    // También "ocupado" el frame en que se cerró: así la misma tecla E
    // que cierra el diálogo no lo vuelve a abrir.
    public bool IsBusy => IsOpen || Time.frameCount <= _closedFrame + 1;

    public event Action OnOpened;
    public event Action OnClosed;

    private void Awake()
    {
        _panel.SetActive(false);
    }

    public void Show(DialogueData dialogue, Action onFinished = null)
    {
        if (IsOpen || dialogue == null || dialogue.Lines.Count == 0)
            return;

        _dialogue = dialogue;
        _onFinished = onFinished;
        _lineIndex = 0;
        IsOpen = true;
        _openedFrame = Time.frameCount;

        _previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        _speakerText.text = dialogue.SpeakerName;
        _panel.SetActive(true);
        ShowLine();

        OnOpened?.Invoke();
    }

    private void Update()
    {
        // Ignoramos el frame en que se abrió: es la misma tecla E que lo abrió.
        if (!IsOpen || Time.frameCount == _openedFrame || !AdvancePressed())
            return;

        if (_isTyping)
        {
            FinishTyping();
            return;
        }

        _lineIndex++;

        if (_lineIndex >= _dialogue.Lines.Count)
        {
            Close();
        }
        else
        {
            ShowLine();
        }
    }

    private static bool AdvancePressed()
    {
        Keyboard keyboard = Keyboard.current;

        return keyboard != null &&
               (keyboard.eKey.wasPressedThisFrame ||
                keyboard.spaceKey.wasPressedThisFrame ||
                keyboard.enterKey.wasPressedThisFrame);
    }

    private void ShowLine()
    {
        bool isLast = _lineIndex == _dialogue.Lines.Count - 1;
        _hintText.text = isLast ? "E: cerrar" : "E: continuar";

        if (_typingRoutine != null)
        {
            StopCoroutine(_typingRoutine);
        }

        _typingRoutine = StartCoroutine(TypeRoutine(_dialogue.Lines[_lineIndex]));
    }

    // El juego está en pausa: usamos tiempo sin escalar.
    private IEnumerator TypeRoutine(string line)
    {
        _isTyping = true;
        float shown = 0f;

        while (shown < line.Length)
        {
            shown += _charactersPerSecond * Time.unscaledDeltaTime;
            _bodyText.text = line.Substring(0, Mathf.Min(line.Length, (int)shown));
            yield return null;
        }

        _isTyping = false;
        _typingRoutine = null;
    }

    private void FinishTyping()
    {
        if (_typingRoutine != null)
        {
            StopCoroutine(_typingRoutine);
            _typingRoutine = null;
        }

        _bodyText.text = _dialogue.Lines[_lineIndex];
        _isTyping = false;
    }

    private void Close()
    {
        IsOpen = false;
        _closedFrame = Time.frameCount;
        _panel.SetActive(false);
        Time.timeScale = _previousTimeScale;

        Action callback = _onFinished;
        _onFinished = null;

        OnClosed?.Invoke();
        callback?.Invoke();
    }
}
