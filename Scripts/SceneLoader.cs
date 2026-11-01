using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{

    [Header("SceneName")]
    [Tooltip("Название сцены, на которую происходит переход")]
    [SerializeField] private string sceneName = "Menu";
    public void LoadScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
