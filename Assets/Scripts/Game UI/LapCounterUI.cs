using TMPro;
using UnityEngine;

public class LapCounterUI : MonoBehaviour
{
    #region private

    private TextMeshProUGUI _lapCounterText;
    private int _currentLap;
    private int _totalLaps;
    private Player _localPlayer;

    #endregion

    public void LinkPlayer(Player player)
    {
        _localPlayer = player;
        _localPlayer.OnLapFinish += UpdateLapCounter;
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
        _currentLap = _localPlayer.CurrentLap.Value;
        _currentLap = _currentLap == 0 ? 1 : _currentLap;       // Para que no se muestre 0/3 al empezar la carrera
        _lapCounterText.text = $"{_currentLap}/{_totalLaps}";
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}