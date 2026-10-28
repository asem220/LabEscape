using UnityEngine;
using UnityEngine.SceneManagement;

// Проход через внешний шлюз завершает побег и возвращает главное меню.
public class LabExit : MonoBehaviour
{
    private bool leaving;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if(leaving||other.GetComponent<PlayerMovement>()==null)return;
        leaving=true;Time.timeScale=1f;SceneManager.LoadSceneAsync("MainMenu");
    }
}
