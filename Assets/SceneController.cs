using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance;

    [Header("Opcional")]
    [SerializeField]
    private GameObject loadingScreen;

    private bool isLoading;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Persiste entre escenas
        DontDestroyOnLoad(gameObject);
    }

    public void LoadScene(string sceneName)
    {
        if (isLoading)
            return;

        StartCoroutine(LoadSceneAsync(sceneName));
    }

    public void LoadScene(int buildIndex)
    {
        if (isLoading)
            return;

        StartCoroutine(LoadSceneAsync(buildIndex));
    }

    public void ReloadCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadNextScene()
    {
        int nextScene = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextScene >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.Log("No existe siguiente escena");
            return;
        }

        LoadScene(nextScene);
    }

    public void QuitGame()
    {
        Debug.Log("Salir");

        Application.Quit();
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        isLoading = true;

        if (loadingScreen)
            loadingScreen.SetActive(true);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        while (!operation.isDone)
        {
            yield return null;
        }

        if (loadingScreen)
            loadingScreen.SetActive(false);

        isLoading = false;
    }

    private IEnumerator LoadSceneAsync(int buildIndex)
    {
        isLoading = true;

        if (loadingScreen)
            loadingScreen.SetActive(true);

        AsyncOperation operation = SceneManager.LoadSceneAsync(buildIndex);

        while (!operation.isDone)
        {
            yield return null;
        }

        if (loadingScreen)
            loadingScreen.SetActive(false);

        isLoading = false;
    }
}
