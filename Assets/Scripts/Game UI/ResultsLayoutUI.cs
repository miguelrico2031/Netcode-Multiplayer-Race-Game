using TMPro;
using UnityEngine;

/// <summary>
/// Clase que gestiona el Overlay de los resultados de la carrera.
/// Muestra los tiempos de vuelta y el tiempo total de la carrera.
/// También muestra la posición final de los jugadores.
/// </summary>
public class ResultsLayoutUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _lapTimeText;
    [SerializeField] private PositionIndicatorUI _raceEndPositionsText;             // No se está usando pero se deja por si acaso

    private string[] _lapTimes = new string[3];                                     // Almacena como strings el tiempo de cada vuelta
    private string _totalTime;                                                      // Almacena como string el tiempo total de la carrera

    public void Show()
    {
        LoadLapTimes();                                                             // Obtiene los valores de los tiempos
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /*
     * Establece la referencia al indicador de posición. Por ahora, toma el mismo objeto usado en la UI de la carrera-
     */ 
    public void SetPositionIndicator(PositionIndicatorUI positionIndicator)
    {
        _raceEndPositionsText = positionIndicator;
    }

    /*
     * Toma los valores de tiempo de vuelta y tiempo final de la clase TimerUI y los muestra en pantalla
     */
    public void SetLapTimes(string[] lapTimes, string totalTime)
    {
        _lapTimes = lapTimes;
        _totalTime = totalTime;
    }

    /*
     * Formatea y muestra los tiempos de vuelta en pantalla
     */
    private void LoadLapTimes()
    {
        _lapTimeText.text = "";
        foreach (string lapTime in _lapTimes)
        {
            _lapTimeText.text += lapTime + "\n";
        }
        _lapTimeText.text += "----------\n" + _totalTime;
    }
}