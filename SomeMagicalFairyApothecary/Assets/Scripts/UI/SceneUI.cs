using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(1000)]
public class UIManager : MonoBehaviour
{
    public GameObject optionsMenu;
    public GameObject sceneMenu;
    public void OpenOptions()
    {
        optionsMenu.SetActive(true);
        sceneMenu.SetActive(false);
    }
    public void GoUp()
    {
        SceneManager.LoadScene(1);
    }

    public void GoDown()
    {
        SceneManager.LoadScene(0);
    }
}
