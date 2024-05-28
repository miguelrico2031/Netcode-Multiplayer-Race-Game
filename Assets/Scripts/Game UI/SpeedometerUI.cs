using UnityEngine;
using TMPro;

/// <summary>
/// Clase que gestiona el acelerómetro del coche de cada jugador.
/// Funciona mediante la velocidad del rigidbody del jugador.
/// </summary>
public class SpeedometerUI : MonoBehaviour
{

    #region private


    private TextMeshProUGUI _speedometerText;
    private Rigidbody _playerRb = null;
    private float _speed;
    private Vector3 _lastPosition;

    #endregion

    #region config

    private readonly float _speedMultiplier = 3.6f;                                         // Para que los valores del acelerómetro sean más "realistas"

    #endregion

    private void Start()
    {
        _speedometerText = GetComponent<TextMeshProUGUI>();
        _speedometerText.text = "000 KM/H";
    }

    private void Update()
    {
        if (_playerRb is null) return;
        UpdateSpeedometerString();
    }

    /*
     * Se actualiza el valor de la velocidad. Se llama en FixedUpdate para que el valor sea más preciso y se evite el jitter en el texto.
     */
    public void FixedUpdate()
    {
        if (_playerRb is null) return;
        UpdateSpeedValue();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /*
     * Actualiza el valor de la velocidad en función de la posición del rigidbody del jugador.
     */
    private void UpdateSpeedValue()
    {
        var vel = (_playerRb.position - _lastPosition).magnitude / Time.fixedDeltaTime;
        _speed = (int) (vel * _speedMultiplier);
        _lastPosition = _playerRb.position;
    }

    /*
     * Actualiza el texto del velocímetro.
     */
    private void UpdateSpeedometerString()
    {
        _speedometerText.text = _speed.ToString("000") + " KM/H";
    }

    /* 
     * Se llama desde Player para cada jugador (Owner)
     */
    public void SetPlayerRb(Rigidbody rb)           
    {
        _playerRb = rb;
        _lastPosition = rb.position;
    }
}