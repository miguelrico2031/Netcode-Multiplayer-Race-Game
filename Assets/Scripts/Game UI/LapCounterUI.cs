using TMPro;
using UnityEngine;

/// <summary>
/// Clase que gestiona el contador de vueltas en el HUD durante la carrera.
/// </summary>
public class LapCounterUI : MonoBehaviour
{
    #region private

    private TextMeshProUGUI _lapCounterText;
    private int _currentLap;                                    // Vuelta actual
    private int _totalLaps;                                     // Vueltas totales
    private Player _localPlayer;                                // Referencia al jugador local

    #endregion

    public void LinkPlayer(Player player)
    {
        _localPlayer = player;
        _localPlayer.OnLapFinish += UpdateLapCounter;           // Se suscribe al evento que se dispara al completar una vuelta
    }

    public void Start()
    {
        // _totalLaps = GameManager.Instance.RaceController.CircuitController.Laps;
        // Comentado porque el numero de vueltas totales lo tienes hardcodeado en el GoalController xd
        _currentLap = 1;
        _totalLaps = 3;
        _lapCounterText = GetComponent<TextMeshProUGUI>();
        _lapCounterText.text = $"{_currentLap}/{_totalLaps}";
    }

    public void UpdateLapCounter()
    {
        _currentLap = _localPlayer.CurrentLap.Value;            // Toma el valor de la vuelta actual del jugador. Se puede hacer con contador pero esto es más seguro porque lo toma del servidor.
        _currentLap = _currentLap == 0 ? 1 : _currentLap;       // Para que no se muestre 0/3 al empezar la carrera
        _lapCounterText.text = $"{_currentLap}/{_totalLaps}";
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}