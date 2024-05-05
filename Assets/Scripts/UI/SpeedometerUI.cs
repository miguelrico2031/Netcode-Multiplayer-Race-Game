using UnityEngine;
using TMPro;

public class SpeedometerUI : MonoBehaviour
{
    #region public
    
    public bool speedometerActive = false;
    
    #endregion

    #region private
    
    private TextMeshProUGUI SpeedometerText;
    private Rigidbody _playerRb = null;
    private float _speed;

    #endregion

    #region config

    private readonly float _speedMultiplier = 3.6f;
    
    #endregion

    private void Start()
    {
        SpeedometerText = GetComponent<TextMeshProUGUI>();
        SpeedometerText.text = "000 KM/H";

        StartSpeedometer();
    }

    private void Update()
    {
        if (_playerRb is null || !speedometerActive) return;

        UpdateSpeedValue();
        UpdateSpeedometerString();
    }

    public void StartSpeedometer()
    {
        speedometerActive = true;
    }

    private void UpdateSpeedValue()
    {
        _speed = (int) (_playerRb.velocity.magnitude * _speedMultiplier);
    }

    private void UpdateSpeedometerString()
    {
        SpeedometerText.text = _speed.ToString("000") + " KM/H";
    }

    public void SetPlayerRb(Rigidbody rb)           // Se llama desde Player para cada jugador (Owner)
    {
        _playerRb = rb;
    }
}