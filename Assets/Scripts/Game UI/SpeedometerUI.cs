using UnityEngine;
using TMPro;
using Unity.Netcode;

public class SpeedometerUI : MonoBehaviour
{


    #region private


    private TextMeshProUGUI _speedometerText;
    private Rigidbody _playerRb = null;
    private float _speed;
    private Vector3 _lastPosition;

    #endregion

    #region config

    private readonly float _speedMultiplier = 3.6f;

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

    public void FixedUpdate()
    {
        if (_playerRb is null) return;

        UpdateSpeedValue();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void UpdateSpeedValue()
    {
        var vel = (_playerRb.position - _lastPosition).magnitude / Time.fixedDeltaTime;
        _speed = (int) (vel * _speedMultiplier);
        _lastPosition = _playerRb.position;

        //_speed = (int) (_playerRb.velocity.magnitude * _speedMultiplier);
    }

    private void UpdateSpeedometerString()
    {
        _speedometerText.text = _speed.ToString("000") + " KM/H";
    }

    public void SetPlayerRb(Rigidbody rb)           // Se llama desde Player para cada jugador (Owner)
    {
        _playerRb = rb;
        _lastPosition = rb.position;
    }
}