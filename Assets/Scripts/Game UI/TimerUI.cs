using UnityEngine;
using TMPro;

/// <summary>
/// Clase que gestiona el tiempo de la carrera.
/// Se encarga de guardar el tiempo total de la carrera y los tiempos de vuelta.
/// Dispone de métodos para formatear y mostrar el tiempo en pantalla.
/// </summary>
public class TimerUI : MonoBehaviour
{
    #region public

    public bool timerActive = false;

    #endregion

    #region private

    private TextMeshProUGUI TimerText;
    private float _seconds, _minutes, _milsecs;
    private float _startTime;
    private string _totalTime;
    private float _lastLapTime = 0f;
    private readonly string[] _lapTimes = new string[3];            // ESTO ESTA HARDCODEADO CUIDADO LUEGO HAY QUE METERLO EN EL GAMEMANAGER O ALGO

    #endregion

    private void Start()
    {
        TimerText = GetComponent<TextMeshProUGUI>();
        TimerText.text = "00:00.000";
    }

    private void Update()
    {
        if (!timerActive) return;

        UpdateTimeValues();
        UpdateTimerString();
    }

    public void StartTimer()
    {
        timerActive = true;
        _startTime = Time.time;
    }

    public void StopTimer()
    {
        _totalTime = ToString();
        timerActive = false;
    }

    public void Hide() => gameObject.SetActive(false);

    public void SaveLapTime(int lapIndex) => _lapTimes[lapIndex] = GetLapTime();            // Guarda el tiempo de la vuelta como string en el array

    public string[] GetLapTimes() => _lapTimes;                                             // Devuelve el array de los tiempos de vuelta

    public string GetTotalTime() => _totalTime.ToString();                                  // Devuelve una string con el tiempo total (o actual) de la carrera

    private float GetCurrentTime() => Time.time - _startTime;                               // Obtiene el tiempo actual de la carrera. Ignora el tiempo de antes de empezar a correr (la cuenta atrás)

    private void UpdateTimeValues() => (_minutes, _seconds, _milsecs) = ConvertTime(GetCurrentTime());  // Actualiza los valores del temporizador

    private void UpdateTimerString() => TimerText.text = ToString();                        // Actualiza el temporizador en la UI

    /*
     * Obtiene el tiempo de la vuelta actual. Se llama al cruzar la meta por el evento que se triggerea en el Player
     */
    private string GetLapTime()
    {
        float currentTime = GetCurrentTime();                                               // Obtiene el tiempo actual de la carrera

        string lapTimeStr = FormatTime(currentTime - _lastLapTime);                         // Saca la string del tiempo que se ha tardado en dar la vuelta
        Debug.Log("Lap time recorded: " + lapTimeStr);

        _lastLapTime = currentTime;                                                         // Actualiza el valor para la siguiente vuelta
        return lapTimeStr;
    }

    /*
     * Convierte el tiempo en segundos a minutos, segundos y milisegundos
     * Funciona con una tupla para devolver los 3 valores a la vez
     */
    private (int minutes, int seconds, int milsecs) ConvertTime(float time)
    {
        int minutes = (int)(time / 60f);
        int seconds = (int)(time % 60f);
        int milsecs = (int)(time * 1000f) % 1000;
        return (minutes, seconds, milsecs);
    }

    /*
     * Formatea el tiempo pasado como float en el parámetro en minutos, segundos y milisegundos
     */
    private string FormatTime(float time)
    {
        var (minutes, seconds, milsecs) = ConvertTime(time);
        return minutes.ToString("00") + ":" + seconds.ToString("00") + "." + milsecs.ToString("000");
    }

    /*
     * Devuelve el tiempo actual como una string formateada
     */
    public override string ToString()
    {
        return _minutes.ToString("00") + ":" + _seconds.ToString("00") + "." + _milsecs.ToString("000");
    }
}