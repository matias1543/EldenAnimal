using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menú principal: los botones llaman a estos métodos desde su OnClick (Inspector).
/// Muestra un panel por vez (principal, créditos u opciones), carga el nivel y cierra el juego.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Escena del nivel (tiene que estar en Build Profiles)")]
    [SerializeField] private string _levelSceneName = "Game";

    [Header("Paneles")]
    [SerializeField] private GameObject _mainPanel;
    [SerializeField] private GameObject _creditsPanel;
    [SerializeField] private GameObject _optionsPanel;

    private void Start()
    {
        // Si venimos de un Game Over con el juego pausado, lo despausamos.
        Time.timeScale = 1f;
        ShowPanel(_mainPanel);
    }

    public void Play()
    {
        SceneManager.LoadScene(_levelSceneName);
    }

    public void ShowCredits()
    {
        ShowPanel(_creditsPanel);
    }

    public void ShowOptions()
    {
        ShowPanel(_optionsPanel);
    }

    public void BackToMain()
    {
        ShowPanel(_mainPanel);
    }

    public void Exit()
    {
        Debug.Log("Saliendo del juego");
        Application.Quit();

#if UNITY_EDITOR
        // En el Editor Application.Quit no hace nada: cortamos el Play Mode.
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // Prende el panel pedido y apaga los otros dos.
    private void ShowPanel(GameObject panel)
    {
        _mainPanel.SetActive(panel == _mainPanel);
        _creditsPanel.SetActive(panel == _creditsPanel);
        _optionsPanel.SetActive(panel == _optionsPanel);
    }
}
