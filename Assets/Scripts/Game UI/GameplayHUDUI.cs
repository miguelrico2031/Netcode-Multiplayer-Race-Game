using TMPro;
using UnityEngine;

// Acuerdate de que la UI se activa y desactiva con el componente Canvas del GameObject

public class GameplayHUDUI : MonoBehaviour
{

    [SerializeField] private SpeedometerUI _speedometer;
    [SerializeField] private TimerUI _timer;
    [SerializeField] private PositionIndicatorUI _positionIndicator;
    [SerializeField] private LapCounterUI _lapCounter;
    [SerializeField] private LapTimeUI _lapTime;
    [SerializeField] private TextMeshProUGUI _backwardsText;
    [SerializeField] private ResultsLayout _raceResultsLayout;

    private Player _localPlayer;

    // Start is called before the first frame update
    void Start()
    {
        GameManager.Instance.HUD = this;                                                            // Establece la referencia en el GameManager
        GameManager.Instance.RaceController.RaceCountdown.OnValueChanged += OnCountdown;            // Suscribirse al evento de la cuenta regresiva
        _raceResultsLayout.Hide();                                                                  // Desactivar la UI de resultados de carrera
    }

    private void OnCountdown(int _, int n)
    {
        if (n != 0) return;

        GameManager.Instance.RaceController.RaceCountdown.OnValueChanged -= OnCountdown;

        gameObject.GetComponent<Canvas>().enabled = true;
        _timer.StartTimer();                                                                        // Interesa que el temporizador empiece despu�s de la cuenta atr�s
    }

    public void ShowBackwardsText() => _backwardsText.enabled = true;
    public void HideBackwardsText() => _backwardsText.enabled = false;

    private void ShowRaceResults()
    {
        HideRaceUI();
        _timer.StopTimer();
        _raceResultsLayout.SetPositionIndicator(_positionIndicator);                                // CAMBIAR A UNA LISTA FIJA QUE NO PERMITA MODIFICACIONES UNA VEZ EL JUGADOR LLEGA A LA META
        _raceResultsLayout.SetLapTimes(_timer.GetLapTimes(), _timer.GetTotalTime());

        _raceResultsLayout.Show();
    }

    private void RecordLapTime()
    {
        int lapIndex = _localPlayer.CurrentLap.Value - 2;

        // CUIDADO CON EL NÚMERO HARDCODEADO. DEBERÍA SER CONSTANTE EN EL GAMEMANAGER
        if (lapIndex == -1 || lapIndex > 3) return;                                                 // Omite la vez en la que se cruza la meta nada mas salir, y la meta al finalizar la carrera

        _timer.SaveLapTime(lapIndex);

        ShowLapTime(lapIndex);
    }

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

    private void HideRaceUI()
    {
        _timer.Hide();
        _speedometer.Hide();
        _lapCounter.Hide();
        _positionIndicator.Hide();
        _backwardsText.enabled = false;
    }
}
