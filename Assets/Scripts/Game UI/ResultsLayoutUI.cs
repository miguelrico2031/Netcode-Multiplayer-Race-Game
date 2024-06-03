using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;

/// <summary>
/// Clase que gestiona el Overlay de los resultados de la carrera.
/// Muestra los tiempos de vuelta y el tiempo total de la carrera.
/// También muestra la posición final de los jugadores.
/// </summary>
public class ResultsLayoutUI : MonoBehaviour
{
    public List<string> LapTimes = new();



    [SerializeField] private TextMeshProUGUI _lapTimeText;
    [SerializeField] private TextMeshProUGUI _raceEndPositionsText;                

    private string _totalTime;                                                      // Almacena como string el tiempo total de la carrera

    public void Show()
    {
        gameObject.SetActive(true);
        LoadLapTimesValues();                                                       // Obtiene los valores de los tiempos
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /*
     * Establece la referencia al indicador de posición. Por ahora, toma el mismo objeto usado en la UI de la carrera.
     */ 
    public void SetPositionIndicator(PositionIndicatorUI racerPositions)
    {
        if (GameManager.Instance.TrainingMode) return;                              // No se muestra en el modo de entrenamiento

        _raceEndPositionsText.text = racerPositions.GetPositionsList();  // Crea una copia de la lista de los jugadores pero "inmutable". MENTIRA SE CAMBIA IGUAL
    }

    /*
     * Toma los valores de tiempo de vuelta y tiempo final de la clase TimerUI y los muestra en pantalla
     */
    public void SetLapTimes(string totalTime)
    {
        _totalTime = totalTime;
    }

    /*
     * Formatea y muestra los tiempos de vuelta en pantalla
     */
    private void LoadLapTimesValues()
    {
        StartCoroutine(Wait());
    }

    private IEnumerator Wait()
    {
        yield return null;
        _lapTimeText.text = "";
        for (int i = 0; i < 3; i++)
        {
            string lapTime = "DNF";
            if (LapTimes.Any())
            {
                lapTime = LapTimes[0];
                LapTimes.RemoveAt(0);
            }
            _lapTimeText.text += lapTime + "\n";
        }
        _lapTimeText.text += "----------\n" + _totalTime;
    }
}