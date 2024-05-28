using System.Collections;
using TMPro;
using UnityEngine;

public class LapTimeUI : MonoBehaviour
{
    private TextMeshProUGUI _lapTimeText;
    [SerializeField] private float flashDuration = 1.5f;                    // Duración del texto parpadeante en pantalla
    [SerializeField] private int flashCount = 5;                            // Número de veces que parpadea el texto
    private string _lapTime = "";

    private void Start()
    {
        _lapTimeText = GetComponent<TextMeshProUGUI>();
        _lapTimeText.text = "";
    }

    public void ShowLapTime(string lapTime)
    {
        _lapTime = lapTime;
        StartCoroutine(FlashLapTime());
    }

    /*
     * Muestra el tiempo de vuelta en pantalla parpadeando
     */
    private IEnumerator FlashLapTime()
    {
        _lapTimeText.text = _lapTime;
        
        float flashingInterval = flashDuration / flashCount;

        for(int i = 0; i < flashCount; i++)
        {
            _lapTimeText.enabled = true;
            yield return new WaitForSeconds(flashingInterval);
            _lapTimeText.enabled = false;
            yield return new WaitForSeconds(flashingInterval);
        }
        
        _lapTimeText.enabled = false;
    }
}