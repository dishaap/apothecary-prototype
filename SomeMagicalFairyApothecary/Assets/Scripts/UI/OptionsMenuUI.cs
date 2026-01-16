using UnityEngine;
using UnityEngine.SceneManagement;

public class OptionsMenuUI : MonoBehaviour
{
    public GameObject optionsMenu;
    public GameObject settingsMenu;
    public GameObject sceneMenu;
    public void BackToGame()
    {
        optionsMenu.SetActive(false);
        sceneMenu.SetActive(true);
    }

    public void OpenSettingsMenu()
    {
        optionsMenu.SetActive(false);
        settingsMenu.SetActive(true);
    }

    public void SaveGame()
    {
        MainManager.Instance.SaveGame();
    }

    public void ExitToMainMenu()
    {
        SceneManager.LoadScene(2);
    }
}
