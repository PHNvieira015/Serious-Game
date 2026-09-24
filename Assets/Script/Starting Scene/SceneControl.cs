using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneControl : MonoBehaviour
{
    public void NextScene()
    {
        Invoke(nameof(LoadNextScene), 1.5f);
    }

    void LoadNextScene()
    {
        SceneManager.LoadScene("2 Main Scene");
    }
}