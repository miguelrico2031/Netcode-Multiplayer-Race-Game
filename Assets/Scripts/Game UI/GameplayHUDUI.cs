using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Acuerdate de que la UI se activa y desactiva con el componente Canvas del GameObject

/// <summary>
/// Clase que gestiona la interfaz de usuario durante la carrera y tras terminarla.
/// </summary>
public class GameplayHUDUI : MonoBehaviour
{
    /* 
     * Elementos de la interfaz
     */
    [SerializeField] private SpeedometerUI _speedometer;
    [SerializeField] private TimerUI _timer;
    [SerializeField] private PositionIndicatorUI _positionIndicator;
    [SerializeField] private LapCounterUI _lapCounter;
    [SerializeField] private LapTimeUI _lapTime;
    [SerializeField] private TextMeshProUGUI _backwardsText;
    [SerializeField] private ResultsLayoutUI _raceResultsLayout;
    [SerializeField] private Button _menuButton;

    private Player _localPlayer;                                                                    // Referencia al jugador local

    void Start()
    {
        GameManager.Instance.HUD = this;                                                            // Establece la referencia en el GameManager
        GameManager.Instance.RaceController.RaceCountdown.OnValueChanged += OnCountdown;            // Suscribirse al evento de la cuenta regresiva
        _raceResultsLayout.Hide();                                                                  // Desactivar la UI de resultados de carrera
    }

    /*
     * Evento de cuenta atrás
     */
    private void OnCountdown(int _, int n)
    {
        if (n != 0) return;

        GameManager.Instance.RaceController.RaceCountdown.OnValueChanged -= OnCountdown;

        gameObject.GetComponent<Canvas>().enabled = true;
        _timer.StartTimer();                                                                        // Interesa que el temporizador empiece despu�s de la cuenta atr�s
    }

    /*
     * Muestra el texto de aviso si el jugador va del revés.
     */
    public void ShowBackwardsText() => _backwardsText.enabled = true;
    public void HideBackwardsText() => _backwardsText.enabled = false;

    /*
     * Muestra el overlay de los resultados de la carrera
     */
    private void ShowRaceResults()
    {
        HideRaceUI();
        _timer.StopTimer();
        _raceResultsLayout.SetPositionIndicator(_positionIndicator);                                // CAMBIAR A UNA LISTA FIJA QUE NO PERMITA MODIFICACIONES UNA VEZ EL JUGADOR LLEGA A LA META
        _raceResultsLayout.SetLapTimes(_timer.GetLapTimes(), _timer.GetTotalTime());

        _raceResultsLayout.Show();
    }

    /*
     * Registra el tiempo de vuelta en el temporizador y lo muestra en pantalla
     */
    private void RecordLapTime()
    {
        int lapIndex = _localPlayer.CurrentLap.Value - 2;

        // CUIDADO CON EL NÚMERO HARDCODEADO. DEBERÍA SER CONSTANTE EN EL GAMEMANAGER
        if (lapIndex == -1 || lapIndex > 3) return;                                                 // Omite la vez en la que se cruza la meta nada mas salir, y la meta al finalizar la carrera

        _timer.SaveLapTime(lapIndex);

        ShowLapTime(lapIndex);
    }

    /*
     * Muestra el tiempo de vuelta en pantalla con unos parpadeos
     */
    private void ShowLapTime(int lapIndex) => _lapTime.ShowLapTime(_timer.GetLapTimes()[lapIndex]);

    /* 
     * Llamado desde el Player para suscribirse a los eventos tras el NetworkSpawn
     */
    public void SubscribeRaceEvents(Player player)
    { 
        _localPlayer = player;                                                                      // Obtener una referencia al jugador local

        _localPlayer.OnRaceFinish += ShowRaceResults;                                               // Suscribirse al evento de fin de carrera

        _localPlayer.OnLapFinish += RecordLapTime;                                                  // Suscribirse al evento de fin de vuelta

        _lapCounter.LinkPlayer(player);                                                             // Enlazar el contador de vueltas con el jugador al que está trackeando
    }

    /*
     * Oculta la los elementos de UI de la carrera
     */
    private void HideRaceUI()
    {
        _timer.Hide();
        _speedometer.Hide();
        _lapCounter.Hide();
        _positionIndicator.Hide();
        _backwardsText.enabled = false;
    }

    public void BackToMenu()
    {
        Debug.Log("Salir");
        GameManager.Instance.Disconnect();
        GetComponent<Canvas>().gameObject.SetActive(false);
        SceneManager.LoadScene("Main Menu", LoadSceneMode.Single);
    }
}
