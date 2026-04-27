using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {
    public GameObject pauseMenuPanel;
    private bool isPaused = false;

    // Função para o botão de PAUSE (o que fica na tela do jogo)
    public void TogglePause() {
    isPaused = !isPaused;
    
    // Certifique-se de que pauseMenuPanel NÃO é o botão de pause
    if (pauseMenuPanel != null) {
        pauseMenuPanel.SetActive(isPaused);
    }

    Time.timeScale = isPaused ? 0f : 1f;
}

    // NOVA FUNÇÃO: Específica para o botão de PLAY dentro do Menu de Pausa
    public void ResumeGame() {
        isPaused = false;
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f; // Garante que o tempo volte ao normal
    }

    public void RestartGame() {
        Time.timeScale = 1f; // Sempre resetar o tempo antes de carregar a cena
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToStartMenu() {
        Time.timeScale = 1f;
        SceneManager.LoadScene("StartMenu");
    }
}