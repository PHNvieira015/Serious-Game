using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("Target Scene")]
    [SerializeField] private string sceneName;

    public void LoadScene()
    {
        LoadSceneByName(sceneName);
    }

    public void LoadSceneByName(string targetScene)
    {
        if (string.IsNullOrWhiteSpace(targetScene))
        {
            Debug.LogWarning("Assign a scene name.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(targetScene))
        {
            Debug.LogError(
                "Scene is unavailable. Add it to the build scene list: " +
                targetScene,
                this
            );
            return;
        }

        SceneManager.LoadScene(targetScene);
    }

    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }
}