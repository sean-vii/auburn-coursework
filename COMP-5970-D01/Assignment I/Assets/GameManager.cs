using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class GameManager : MonoBehaviour
{
    float score;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI gameOverText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void GameOver()
    {
        Time.timeScale = 0f;
        gameOverText.gameObject.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        score += Time.deltaTime;
        scoreText.text = "Score: " + Mathf.FloorToInt(score).ToString();

        if(Time.timeScale == 0f && Keyboard.current.spaceKey.isPressed)
        {
            RestartGame();
        }
    }

    void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}
