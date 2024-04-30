using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.Windows;

public class MainMenuUI : MonoBehaviour
{
    public string PlayerName { get; private set; } = "Anon";
    public PlayerInfo.PlayerColor PlayerColor { get; private set; }
    public Circuit SelectedCircuit { get; private set; }

    [SerializeField] private TextMeshProUGUI _joinCodeText, _nameText, _playersLogText;
    [SerializeField] private Button _hostBtn, _joinBtn, _startBtn, _carSelectLeftBtn, _carSelectRightBtn, _backButton;
    [SerializeField] private GameObject _circuitSelect, _joinInput, _nameInput, _playersLog;
    [SerializeField] private MeshRenderer _carRenderer;
    
    [Serializable]
    public class CarColor
    {
        public PlayerInfo.PlayerColor Color;
        public Material Material;
    }

    [SerializeField] private CarColor[] _carColors;

    private string _joinCode = "";
    private int _colorIdx = 0;
    private Material[] _carMaterials;

    private int _circuitIdx = 0;
    private TextMeshProUGUI _circuitText;
    
    private void Start()
    {
        _carMaterials = _carRenderer.materials;
        _carMaterials[1] = _carColors[_colorIdx].Material;
        _carRenderer.materials = _carMaterials;
        PlayerColor = _carColors[_colorIdx].Color;
        SelectedCircuit = (Circuit)Enum.GetValues(typeof(Circuit)).GetValue(0);
        _circuitText = _circuitSelect.GetComponent<TextMeshProUGUI>();
        _circuitText.text = SelectedCircuit.ToString();

        GameManager.Instance.PlayerInfos.OnListChanged += OnPlayersUpdated;

    }

    private void OnDisable()
    {
        GameManager.Instance.PlayerInfos.OnListChanged -= OnPlayersUpdated;
    }

    public void ChangeColor(bool right) //metodo llamado por los botones de cambiar el color del coche
    {
        _colorIdx = ((_colorIdx + (right ? 1 : -1)) % _carColors.Length);
        _colorIdx = _colorIdx < 0 ? _carColors.Length-1 : _colorIdx;
        _carMaterials[1] = _carColors[_colorIdx].Material;
        _carRenderer.materials = _carMaterials;
        PlayerColor = _carColors[_colorIdx].Color;

    }
    
    public void ChangeCircuit(bool right) //metodo llamado por los botones de cambiar el color del coche
    {
        _circuitIdx = (_circuitIdx + (right ? 1 : -1)) % Enum.GetValues(typeof(Circuit)).Length;
        _circuitIdx = _circuitIdx < 0 ? Enum.GetValues(typeof(Circuit)).Length - 1 : _circuitIdx;
        SelectedCircuit = (Circuit)Enum.GetValues(typeof(Circuit)).GetValue(_circuitIdx);
        _circuitText.text = SelectedCircuit.ToString();
    }


    //metodo llamado por el input de elegir nombre
    public void SetName(string newName) => PlayerName = newName;
    
    
    //metodo llamado por el input de poner el codigo de la sala
    public void SetJoinCode(string code) => _joinCode = code;

    public void Host()
    {
        if (!_circuitSelect.activeSelf) //la primera vez que se le da al boton de host aparece la UI de elegir circuito
        {
            _joinBtn.gameObject.SetActive(false);
            _circuitSelect.SetActive(true);
            //se posiciona el boton de host donde estaba el de join para abrir espacio para el selector de circuito
            _hostBtn.GetComponent<RectTransform>().position = _joinBtn.GetComponent<RectTransform>().position;
        }
        else //cuando ya se ha elegido circuito, el boton de host crea la sala
        {
            SetFinalNameText();
            DisableCarButtons();
            _circuitSelect.SetActive(false);
            _hostBtn.gameObject.SetActive(false);
            GameManager.Instance.StartHost(joinCode => //callback que se ejecuta cuando termine de crearse el host
            {
                _joinCodeText.text = joinCode; //muestra el codigo de la sala por pantalla
                _joinCodeText.gameObject.SetActive(true);
                _startBtn.gameObject.SetActive(true);
                _playersLog.SetActive(true);
            },
            () => 
            {
                Debug.LogWarning("Ruina Host no creado");
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
            SetFinalNameText();
            DisableCarButtons();
            _joinBtn.gameObject.SetActive(false);
            _joinInput.SetActive(false);
            GameManager.Instance.StartClient(_joinCode,
            () =>
            { //cuando se una a la sala se muestran los jugadores que hay
                _playersLog.SetActive(true);
            },
            () =>
            {
                Debug.LogWarning("Ruina no te pudiste unir a la sala");
            });
        }
    }

    private void SetFinalNameText() //desactiva el input para no cambiarse el nombre más
    {
        _nameInput.SetActive(false);
        _nameText.gameObject.SetActive(true);
        _nameText.text = PlayerName;
    }

    private void DisableCarButtons() //desactiva los botones de cambiar el color del coche
    {
        _carSelectLeftBtn.gameObject.SetActive(false);
        _carSelectRightBtn.gameObject.SetActive(false);
    }

    private void OnPlayersUpdated(NetworkListEvent<PlayerInfo> e = default)
    { //actualiza el texto del log de jugadores, indicando quien eres tu y quien es el host
        _playersLogText.text = "";
        foreach (PlayerInfo playerInfo in GameManager.Instance.PlayerInfos)
        {
            _playersLogText.text += playerInfo.Name.Value;
            if (GameManager.Instance.HostInfo.Value.Equals(playerInfo)) _playersLogText.text += " (HOST)";
            if (PlayerName == playerInfo.Name) _playersLogText.text += " (YOU)";
            _playersLogText.text += "\n";
        }
    }

    private void HomeMenu()
    {
        
    }

}
