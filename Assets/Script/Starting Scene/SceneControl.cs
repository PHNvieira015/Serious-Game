using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneControl : MonoBehaviour
{
    [SerializeField] private string SceneString;
    [SerializeField] private float StartTiming = 1.5f;

    public void NextScene()
    {
        Invoke(nameof(LoadNextScene), StartTiming);
    }

    void LoadNextScene()
    {
        SceneManager.LoadScene(SceneString);
    }
}