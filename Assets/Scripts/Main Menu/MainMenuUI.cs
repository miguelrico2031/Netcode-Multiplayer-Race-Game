using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public string PlayerName { get; private set; } = "Anon";

    [SerializeField] private TextMeshProUGUI _joinCodeText, _nameText, _playersLogText;
    [SerializeField] private Button _hostBtn, _joinBtn, _startBtn;
    [SerializeField] private GameObject _circuitSelect, _joinInput, _nameInput, _playersLog;
    [SerializeField] private MeshRenderer _carRenderer;
    [SerializeField] private Material[] _carColors;

    private string _joinCode = "";
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
        PlayerName = newName;
    }

    public void SetJoinCode(string code) => _joinCode = code;

    public void Host()
    {
        if (!_circuitSelect.activeSelf)
        {
            _joinBtn.gameObject.SetActive(false);
            _circuitSelect.SetActive(true);
            _hostBtn.GetComponent<RectTransform>().position = _joinBtn.GetComponent<RectTransform>().position;
        }
        else
        {
            _circuitSelect.SetActive(false);
            _hostBtn.gameObject.SetActive(false);
            SetNameText();
            GameManager.Instance.StartHost(joinCode =>
            {
                _joinCodeText.text = joinCode;
                _joinCodeText.gameObject.SetActive(true);
                _startBtn.gameObject.SetActive(true);
                _playersLog.SetActive(true);
            });
        }
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
                SetNameText();
                _playersLog.SetActive(true);
            },
            () =>
            {
                Debug.LogWarning("Ruina");
            });
        }
    }

    private void SetNameText()
    {
        _nameInput.SetActive(false);
        _nameText.gameObject.SetActive(true);
        _nameText.text = PlayerName;
    }

    public void OnPlayersUpdated(NetworkListEvent<FixedString64Bytes> e = default)
    {
        _playersLogText.text = "";
        var list = GameManager.Instance.PlayerNames;
        foreach (var playerName in list)
        {
            _playersLogText.text += $"{playerName}";
            
            if (GameManager.Instance.HostName.Value == playerName)
                _playersLogText.text += " (HOST)";
            if (PlayerName == playerName) //eres tu
                _playersLogText.text += " (YOU)";
            
            _playersLogText.text += "\n";
        }
    }

}
