using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("Level");
        StateManager.Instance.ChangeState(new ShiftState());
    }
}
