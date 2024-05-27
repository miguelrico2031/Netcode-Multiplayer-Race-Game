using TMPro;
using UnityEngine;

// Acuerdate de que la UI se activa y desactiva con el componente Canvas del GameObject

public class GameplayHUDUI : MonoBehaviour
{

    [SerializeField] private SpeedometerUI _speedometer;
    [SerializeField] private TimerUI _timer;
    [SerializeField] private PositionIndicatorUI _positionIndicator;
    [SerializeField] private TextMeshProUGUI _backwardsText;
    [SerializeField] private ResultsLayout _raceResultsLayout;

    private Player _localPlayer;
    private string[] _lapTimes = new string[3];

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

        //Activas UI
        gameObject.GetComponent<Canvas>().enabled = true;
        _timer.StartTimer();                                                                        // Interesa que el temporizador empiece despu�s de la cuenta atr�s
    }

    public void ShowBackwardsText() => _backwardsText.enabled = true;
    public void HideBackwardsText() => _backwardsText.enabled = false;

    private void ShowRaceResults()
    {
        _timer.StopTimer();
        _timer.Hide();
        _speedometer.Hide();
        _backwardsText.enabled = false;
        _raceResultsLayout.SetPositionIndicator(_positionIndicator);
        _positionIndicator.Hide();
        _raceResultsLayout.Show();
    }

    private void RecordLapTime()
    {
        int lapIndex = _localPlayer.CurrentLap.Value - 1;
        _raceResultsLayout.SaveLapTime(lapIndex, _timer.ToString());
    }

    // Llamado desde el Player para suscribirse a los eventos tras el NetworkSpawn
    public void SubscribeRaceEvents(Player player)
    {
        // Obtener una referencia al jugador local
        _localPlayer = player;

        // Suscribirse al evento de fin de carrera
        _localPlayer.OnRaceFinish += ShowRaceResults;

        // Suscribirse al evento de fin de vuelta
        _localPlayer.OnLapFinish += RecordLapTime;
    }
}
