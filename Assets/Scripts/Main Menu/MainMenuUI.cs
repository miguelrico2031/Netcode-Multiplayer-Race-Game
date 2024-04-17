using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using TMPro;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _joinCodeText;
    [SerializeField] private Button _hostBtn, _joinBtn, _startBtn;
    [SerializeField] private GameObject _circuitSelect, _joinInput;
    [SerializeField] private MeshRenderer _carRenderer;
    [SerializeField] private Material[] _carColors;

    private string _playerName = "Anon", _joinCode = "";
    private int _colorIdx = 0;
    private Material[] _carMaterials;

    private void Start()
    {
        _carMaterials = _carRenderer.materials;
        _carMaterials[1] = _carColors[_colorIdx];
        _carRenderer.materials = _carMaterials;
    }

    public void ChangeColor(bool right)
    {
        _colorIdx = ((_colorIdx + (right ? 1 : -1)) % _carColors.Length);
        _colorIdx = _colorIdx < 0 ? _carColors.Length-1 : _colorIdx;
        _carMaterials[1] = _carColors[_colorIdx];
        _carRenderer.materials = _carMaterials;

    }

    public void SetName(string newName)
    {
        _playerName = newName;
    }

    public void SetJoinCode(string code) => _joinCode = code;

    public void Host()
    {
        _hostBtn.gameObject.SetActive(false);
        _joinBtn.gameObject.SetActive(false);
        GameManager.Instance.StartHost(joinCode =>
        {
            _joinCodeText.text = joinCode;
            _joinCodeText.gameObject.SetActive(true);
            _circuitSelect.SetActive(true);
            _startBtn.gameObject.SetActive(true);
        });
    }

    public void Join()
    {
        if (_hostBtn.gameObject.activeSelf) // mostrar el input para poder unirse a una sala con el join code
        {
            _hostBtn.gameObject.SetActive(false);
            _joinInput.SetActive(true);
        }
        else //ahora join hace de boton de confirmacion, al pulsarlo se intentara unir a una sala con el joincode del input
        {
            GameManager.Instance.StartClient(_joinCode,
            () =>
            {
                _joinBtn.gameObject.SetActive(false);
                _joinInput.SetActive(false);
                Debug.Log("Joya");
            },
            () =>
            {
                Debug.Log("Ruina");
            });
        }


    }
}
