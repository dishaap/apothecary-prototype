using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(1000)]
public class UIManager : MonoBehaviour
{
    
    public void GoUp()
    {
        SceneManager.LoadScene(1);
    }

    public void GoDown()
    {
        SceneManager.LoadScene(0);
    }
}
