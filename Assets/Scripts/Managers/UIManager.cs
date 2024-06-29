using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private GameObject gameUI, shiftEndUI;
    private Crosshair crosshair;
    private TextMeshProUGUI orderInfo, ordersCompleted, timerText, moneyText, scoreText;
    private int minutes;
    private float seconds;

    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }

        gameUI = GameObject.Find("GameUI");
        shiftEndUI = GameObject.Find("ShiftEndUI");
        scoreText = GameObject.Find("ScoreText").GetComponent<TextMeshProUGUI>();
        shiftEndUI.SetActive(false);
        crosshair = new Crosshair(GameObject.Find("Crosshair"));
        orderInfo = GameObject.Find("OrderInfo").GetComponent<TextMeshProUGUI>();
        timerText = GameObject.Find("Timer").GetComponent<TextMeshProUGUI>();
        ordersCompleted = GameObject.Find("OrdersCompleted").GetComponent<TextMeshProUGUI>();
        moneyText = GameObject.Find("Money").GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        // Subscribe to the state change event
        StateManager.Instance.OnStateChanged += HandleStateChange;

        // Subscribe to game paused events
        StateManager.Instance.OnGamePausedChanged += OnGamePausedChanged;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCrosshair();
    }

    private void OnDestroy()
    {
        // Unsubscribe from the state change event
        StateManager.Instance.OnStateChanged -= HandleStateChange;
    }

    private void HandleStateChange(State newState)
    {
        HideAllUIs();
        if (newState.GetType() == typeof(ShiftState))
        {
            SetOrderInfo("");
            SetOrdersCompleted(0);
            gameUI.SetActive(true);
        }
        else if (newState.GetType() == typeof(ShiftEndState))
        {
            shiftEndUI.SetActive(true);
        }
    }

    private void OnGamePausedChanged(bool gamePaused)
    {
        if (gamePaused)
        {
            // Release the player cursor
            Cursor.lockState = CursorLockMode.None;
            // Show pause UI
            if (!SceneManager.GetSceneByName("Pause").isLoaded)
            {
                SceneManager.LoadScene("Pause", LoadSceneMode.Additive);
            }
        }
        else
        {
            // Capture the player cursor
            Cursor.lockState = CursorLockMode.Locked;
            if (SceneManager.GetSceneByName("Pause").isLoaded)
            {
                SceneManager.UnloadSceneAsync("Pause");
            }
        }
    }

    private void HideAllUIs()
    {
        gameUI.SetActive(false);
        shiftEndUI.SetActive(false);
    }

    private void UpdateCrosshair()
    {
        // Update crosshair state depending on what the player is looking at
        var (type, _) = PlayerInteractions.Instance.InteractionCheck();
        if (type == InteractableType.Grabbable)
        {
            crosshair.SetGrab();
        }
        else if (type == InteractableType.Slottable && PlayerInteractions.Instance.GetGrabStatus())
        {
            crosshair.SetSlot();
        }
        else if (type == InteractableType.Usable)
        {
            crosshair.SetUse();
        }
        else
        {
            crosshair.SetNeutral();
        }
    }

    public void UpdateTimerText(float timer)
    {
        // Format the string accordingly for the UI text
        seconds = timer % 60;
        minutes = (int)timer / 60;
        timerText.text = "Time: " + string.Format("{0:00}:{1:00.0}", minutes, seconds);
    }

    public void SetOrderInfo(string order)
    {
        orderInfo.text = "Current Order: " + order;
    }

    public void SetOrdersCompleted(int completed)
    {
        ordersCompleted.text = "Orders Completed: " + completed.ToString();
        moneyText.text = SaveManager.Instance.GetPlayerMoney().ToString() + " Credits";
    }

    public void UpdateShiftEndUI(int score)
    {
        string dayCount = "Day " + SaveManager.Instance.GetDayCount().ToString();
        string ordersCompleted = "Orders Completed: " + score.ToString();
        string highScore = "High Score: " + SaveManager.Instance.GetHighScore().ToString();
        scoreText.text = dayCount + "\n" + ordersCompleted + "\n" + highScore;
    }

    // Game State Functions

    public void B_AdvanceDay()
    {
        StateManager.Instance.ChangeState(new PlanningState());
    }

    public void B_StartShift()
    {
        StateManager.Instance.ChangeState(new ShiftState());
    }

    public void B_Resume()
    {
        StateManager.Instance.SetGamePaused(false);
    }

    public void B_QuitToMenu()
    {
        StateManager.Instance.ChangeState(new MainMenuState());
    }

    public void B_QuitGame()
    {
        #if UNITY_STANDALONE
            Application.Quit();
        #endif
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    // Dialogue Functions

    public IEnumerator RevealText(TextMeshProUGUI textObj, string dialogueString, float delay = 0.05f)
    {
        // Update the text object to contain the dialogue string
        textObj.text = dialogueString;
        textObj.ForceMeshUpdate();

        int totalCharacters = dialogueString.Length;
        int currentVisibleCharacters = 0;

        while (currentVisibleCharacters <= totalCharacters)
        {
            // Update the text object's max visible characters to the current count
            textObj.maxVisibleCharacters = currentVisibleCharacters;
            currentVisibleCharacters++;
            yield return new WaitForSeconds(delay);
        }
    }
}

// Class for the Crosshair UI element, containing methods to easily swap its tooltip text and color
public class Crosshair
{
    private GameObject obj;
    private UnityEngine.UI.Image img;
    private TextMeshProUGUI tooltip;

    public Crosshair(GameObject gameObject)
    {
        obj = gameObject;
        img = obj.GetComponent<UnityEngine.UI.Image>();
        tooltip = obj.GetComponentInChildren<TextMeshProUGUI>();
    }

    public void SetNeutral()
    {
        img.color = Color.white;
        tooltip.text = "";
    }

    public void SetGrab()
    {
        img.color = Color.red;
        tooltip.text = "(E)";
    }

    public void SetSlot()
    {
        img.color = Color.green;
        tooltip.text = "(E)";
    }

    public void SetUse()
    {
        img.color = Color.blue;
        tooltip.text = "(Q)";
    }
}
