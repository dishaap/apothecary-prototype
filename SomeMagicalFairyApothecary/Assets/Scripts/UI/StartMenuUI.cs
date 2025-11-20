using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuUI : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene(0);

        // TODO: add code to load current game when start button is selected.
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
        MainManager.Instance.SaveGame();
    }
}
