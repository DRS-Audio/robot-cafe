using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using GameConsts;

public class StateManager : MonoBehaviour
{
    public static StateManager Instance { get; private set; }

    private State currentState;
    public delegate void StateChangeHandler(State newState);
    public event StateChangeHandler OnStateChanged;
    public delegate void PauseChangeHandler(bool gamePaused);
    public event PauseChangeHandler OnGamePausedChanged;
    private bool gamePaused = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        // Subscribe to the scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Level")
        {
            ChangeState(new ShiftState());
        }
        if (scene.name == "Start")
        {
            ChangeState(new MainMenuState());
        }
    }

    public State GetCurrentState()
    {
        return currentState;
    }

    public void ChangeState(State newState)
    {
        if (currentState != null)
        {
            currentState.Exit();
        }

        currentState = newState;
        currentState.Enter();

        // Broadcast the state change event
        OnStateChanged?.Invoke(newState);
    }

    private void Update()
    {
        if (currentState != null)
        {
            currentState.Update();
        }
    }

    public bool GetGamePaused()
    {
        return gamePaused;
    }

    public void SetGamePaused(bool _gamePaused)
    {
        // TODO: Make PlanningState pausable as well
        if (currentState.GetType() == typeof(ShiftState))
        {
            gamePaused = _gamePaused;

            // Notify subscribers of the event
            OnGamePausedChanged?.Invoke(gamePaused);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from the scene loaded event
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}

public abstract class State
{
    public abstract void Enter();
    public abstract void Update();
    public abstract void Exit();
}

public class MainMenuState : State
{
    public override void Enter()
    {
        Debug.Log("Entering MainMenu state");
    }

    public override void Update()
    {
    }

    public override void Exit()
    {
        Debug.Log("Exiting MainMenu state");
    }
}

public class PlanningState : State
{
    public override void Enter()
    {
        Debug.Log("Entering Planning state");
        // Switch to overhead camera
        CameraManager.Instance.SetActiveCamera(SceneCamera.OverheadCamera);
        // Update day count
        SaveManager.Instance.SetDayCount(SaveManager.Instance.GetDayCount() + 1);
    }

    public override void Update()
    {
    }

    public override void Exit()
    {
        // Save game
        SaveManager.Instance.Save();
        // Switch back to main camera
        CameraManager.Instance.SetActiveCamera(SceneCamera.MainCamera);
        Debug.Log("Exiting Planning state");
    }
}

public class ShiftState : State
{
    private float shiftTimer = GameConsts.GameConsts.ShiftLengthInSec;
    private bool paused = false;
    public override void Enter()
    {
        Debug.Log("Entering Shift state");
        // Subscribe to game paused events
        StateManager.Instance.OnGamePausedChanged += OnGamePausedChanged;
        // Enable player input
        GameObject.Find("PlayerCapsule").GetComponent<PlayerInput>().ActivateInput();
        // Capture the player cursor
        Cursor.lockState = CursorLockMode.Locked;
    }

    public override void Update()
    {
        if (!paused)
        {
            UpdateTimer();
        }
    }

    public override void Exit()
    {
        Debug.Log("Exiting Shift state");
        // TODO: Delete any stray objects like cups
    }

    private void UpdateTimer()
    {
        // Update the timer each frame until it reaches 0, and format the string accordingly for the UI text
        if (shiftTimer > 0f)
        {
            shiftTimer -= Time.deltaTime;
            UIManager.Instance.UpdateTimerText(shiftTimer);
        }
        else
        {
            StateManager.Instance.ChangeState(new ShiftEndState());
        }
    }

    private void OnGamePausedChanged(bool gamePaused)
    {
        paused = gamePaused;
    }
}

public class ShiftEndState : State
{
    public override void Enter()
    {
        Debug.Log("Entering ShiftEnd state");
        // Disable player input
        GameObject.Find("PlayerCapsule").GetComponent<PlayerInput>().DeactivateInput();
        // Release the player cursor
        Cursor.lockState = CursorLockMode.None;
        // Update the high score
        int score = OrderManager.Instance.completedCounter;
        if (SaveManager.Instance.GetHighScore() < score)
        {
            SaveManager.Instance.SetHighScore(score);
        }
        // Switch the UI to the game end UI
        UIManager.Instance.UpdateShiftEndUI(score);
        // Save game
        SaveManager.Instance.Save();
    }

    public override void Update()
    {
    }

    public override void Exit()
    {
        Debug.Log("Exiting ShiftEnd state");
    }
}
