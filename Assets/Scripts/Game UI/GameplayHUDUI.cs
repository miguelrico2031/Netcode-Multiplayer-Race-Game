using TMPro;
using UnityEngine;

// Acuerdate de que la UI se activa y desactiva con el componente Canvas del GameObject

public class GameplayHUDUI : MonoBehaviour
{

    [SerializeField] private SpeedometerUI _speedometer;
    [SerializeField] private TimerUI _timer;
    [SerializeField] private PositionIndicatorUI _positionIndicator;
    [SerializeField] private TextMeshProUGUI _backwardsText;
    [SerializeField] private GameObject _raceResultsLayout;

    private Player _localPlayer;

    // Start is called before the first frame update
    void Start()
    {
        GameManager.Instance.HUD = this;                                                            // Establece la referencia en el GameManager
        GameManager.Instance.RaceController.RaceCountdown.OnValueChanged += OnCountdown;            // Suscribirse al evento de la cuenta regresiva
        _raceResultsLayout.SetActive(false);                                                        // Desactivar la UI de resultados de carrera
    }

    private void OnCountdown(int _, int n)
    {
        if (n != 0) return;

        GameManager.Instance.RaceController.RaceCountdown.OnValueChanged -= OnCountdown;

        /* Y dirás tu,
         * por qué está esto puesto aqui en medio????
         * Pues porque si lo ponia en el start o en el awake
         * el player aún no está instanciado
         * y pues da error. Esto mejor en verdad ponerlo en el propio network spawn
         */
        SubscribeRaceEndEvent();                                                                    // Suscribirse al evento de fin de carrera

        //Activas UI
        gameObject.GetComponent<Canvas>().enabled = true;
        _timer.StartTimer();                                                                        // Interesa que el temporizador empiece despu�s de la cuenta atr�s
    }

    private void ShowRaceResults()
    {
        _timer.StopTimer();
        _timer.Hide();
        _positionIndicator.Hide();
        _speedometer.Hide();
        _raceResultsLayout.SetActive(true);
    }

    public void ShowBackwardsText() => _backwardsText.enabled = true;
    public void HideBackwardsText() => _backwardsText.enabled = false;

    private void SubscribeRaceEndEvent()
    {
        // Obtener una referencia al jugador local
        _localPlayer = GameManager.Instance.LocalPlayer;
        if (_localPlayer != null) Debug.Log("si");

        // Suscribirse al evento de fin de carrera
        _localPlayer.OnRaceFinish += ShowRaceResults;
    }
}
