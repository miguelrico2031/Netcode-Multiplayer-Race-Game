using System;
using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public string PlayerName { get; private set; } = "Anon";
    public PlayerInfo.PlayerColor PlayerColor { get; private set; }
    public Circuit SelectedCircuit { get; private set; }
    [Serializable] public class CarColor
    {
        public PlayerInfo.PlayerColor Color;
        public Material Material;
    }

    [SerializeField] private TextMeshProUGUI _joinCodeText, _nameText, _playersLogText;
    [SerializeField] private Button _hostBtn, _joinBtn, _startBtn, _carSelectLeftBtn, _carSelectRightBtn, _backBtn;
    [SerializeField] private GameObject _circuitSelect, _joinInput, _nameInput, _playersLog, _errorPanel;
    [SerializeField] private MeshRenderer _carRenderer;
    [SerializeField] private CarColor[] _carColors;

    private string _joinCode = "";
    private Vector3 _hostBtnOriginalPos;
    
    private int _colorIdx = 0;
    private Material[] _carMaterials;

    private int _circuitIdx = 0;
    private Array _circuits = Enum.GetValues(typeof(Circuit));
    private TextMeshProUGUI _circuitText;
    
    private void Start()
    {
        _hostBtnOriginalPos = _hostBtn.GetComponent<RectTransform>().position;

        InitializePlayerModel();
        InitializeCircuitSelection();

        GameManager.Instance.PlayerInfos.OnListChanged += OnPlayersUpdated;
    }

    private void OnDisable()
    {
        GameManager.Instance.PlayerInfos.OnListChanged -= OnPlayersUpdated;
    }

    private void InitializePlayerModel()
    {

        _carMaterials = _carRenderer.materials;
        _carMaterials[1] = _carColors[_colorIdx].Material;
        _carRenderer.materials = _carMaterials;
        PlayerColor = _carColors[_colorIdx].Color;
    }
    private void InitializeCircuitSelection()
    {
        SelectedCircuit = (Circuit)_circuits.GetValue(0);
        _circuitText = _circuitSelect.GetComponent<TextMeshProUGUI>();
        _circuitText.text = SelectedCircuit.ToString();
    }

    public void ChangeColor(bool right)                                                     //metodo llamado por los botones de cambiar el color del coche
    {
        _colorIdx = LoopListUI(_colorIdx, _carColors.Length, right);
        _carMaterials[1] = _carColors[_colorIdx].Material;
        _carRenderer.materials = _carMaterials;
        PlayerColor = _carColors[_colorIdx].Color;
    }
    
    public void ChangeCircuit(bool right)                                                   //metodo llamado por los botones de cambiar el circuito
    {
        _circuitIdx = LoopListUI(_circuitIdx, _circuits.Length, right);
        SelectedCircuit = (Circuit)_circuits.GetValue(_circuitIdx);
        _circuitText.text = SelectedCircuit.ToString();
    }

    private int LoopListUI(int index, int length, bool right)                              //metodo para cambiar de elemento en una lista circular
    {
        index = (index + (right ? 1 : -1)) % length;
        index = index < 0 ? length - 1 : index;
        return index;
    }
    
    public void SetName(string newName) => PlayerName = newName;                            //metodo llamado por el input de elegir nombre

    //metodo llamado por el input de poner el codigo de la sala
    public void SetJoinCode(string code) => _joinCode = code;

    public void Host()
    {
        if (!_circuitSelect.activeSelf)                                                     //la primera vez que se le da al boton de host aparece la UI de elegir circuito
        {
            LoadCircuitSelectScreen();
        }
        else                                                                                //cuando ya se ha elegido circuito, el boton de host crea la sala
        {
            LoadHostLobbyScreen();
        }
    }

    public void Join()
    {
        if (_hostBtn.gameObject.activeSelf)                                                 // mostrar el input para poder unirse a una sala con el join code
        {
            LoadRoomSearchScreen();
        }
        else                                                                                //ahora join hace de boton de confirmacion, al pulsarlo se intentara unir a una sala con el joincode del input
        {
            LoadClientLobbyScreen();
        }
    }

    private void LoadCircuitSelectScreen()
    {
        _joinBtn.gameObject.SetActive(false);
        _backBtn.gameObject.SetActive(true);
        _circuitSelect.SetActive(true);
        //se posiciona el boton de host donde estaba el de join para abrir espacio para el selector de circuito
        _hostBtn.GetComponent<RectTransform>().position = _joinBtn.GetComponent<RectTransform>().position;
    }

    private void LoadHostLobbyScreen()
    {
        SetFinalNameText();
        DisableCarButtons();
        _circuitSelect.SetActive(false);
        _hostBtn.gameObject.SetActive(false);
        _backBtn.onClick.RemoveAllListeners();
        _backBtn.onClick.AddListener(DisconnectHostButton);
        GameManager.Instance.StartHost(joinCode =>                                          //callback que se ejecuta cuando termine de crearse el host
        {
            _joinCodeText.text = joinCode;                                                  //muestra el codigo de la sala por pantalla
            _joinCodeText.gameObject.SetActive(true);
            _startBtn.gameObject.SetActive(true);
            _playersLog.SetActive(true);
        },
        () =>
        {
            _errorPanel.SetActive(true);
            Debug.LogWarning("Ruina Host no creado");
        });
    }

    private void LoadRoomSearchScreen()
    {
        _hostBtn.gameObject.SetActive(false);
        _backBtn.gameObject.SetActive(true);
        _joinInput.SetActive(true);
    }

    private void LoadClientLobbyScreen()
    {
        SetFinalNameText();
        DisableCarButtons();
        _joinBtn.gameObject.SetActive(false);
        _joinInput.SetActive(false);
        _backBtn.onClick.RemoveAllListeners();
        _backBtn.onClick.AddListener(DisconnectClientButton);
        GameManager.Instance.StartClient(_joinCode,
        () =>
        {                                                                                   //cuando se una a la sala se muestran los jugadores que hay
            _playersLog.SetActive(true);
        },
        () =>
        {
            _errorPanel.SetActive(true);
            Debug.LogWarning("Ruina no te pudiste unir a la sala");
        });
    }

    private void SetFinalNameText()                                                         //desactiva el input para no cambiarse el nombre más
    {
        _nameInput.SetActive(false);
        _nameText.gameObject.SetActive(true);
        _nameText.text = PlayerName;
    }

    private void DisableCarButtons()                                                        //desactiva los botones de cambiar el color del coche
    {
        _carSelectLeftBtn.gameObject.SetActive(false);
        _carSelectRightBtn.gameObject.SetActive(false);
    }

    private void OnPlayersUpdated(NetworkListEvent<PlayerInfo> e = default)
    {                                                                                       //actualiza el texto del log de jugadores, indicando quien eres tu y quien es el host
        _playersLogText.text = "";
        foreach (PlayerInfo playerInfo in GameManager.Instance.PlayerInfos)
        {
            _playersLogText.text += playerInfo.Name.Value;
            if (GameManager.Instance.HostInfo.Value.Equals(playerInfo)) _playersLogText.color = Color.yellow;
            if (PlayerName == playerInfo.Name) _playersLogText.text += "[YOU]";
            _playersLogText.text += "\n";
        }
    }

    public void BackToHomeMenu()                                                            // Recarga la escena para volver al menú principal.
    {
        RestoreMainMenuUI();
        DisableSecondaryMenuUI();
    }

    private static void DisconnectHostButton()
    {
        GameManager.Instance.Disconnect();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private static void DisconnectClientButton()
    {
        GameManager.Instance.Disconnect();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void RestoreMainMenuUI()
    {
        _hostBtn.GetComponent<RectTransform>().position = _hostBtnOriginalPos;
        _joinBtn.gameObject.SetActive(true);
        _hostBtn.gameObject.SetActive(true);
        _nameInput.SetActive(true);
        _carSelectLeftBtn.gameObject.SetActive(true);
        _carSelectRightBtn.gameObject.SetActive(true);
    }

    private void DisableSecondaryMenuUI()
    {
        _errorPanel.SetActive(false);                                                       // Panel de error
        _nameText.gameObject.SetActive(false);                                              // Nombre del jugador
        _joinCodeText.gameObject.SetActive(false);                                          // Código de la sala
        _joinInput.SetActive(false);                                                        // Campo de entrada de código de sala
        _playersLog.SetActive(false);                                                       // Tabla de jugadores
        _circuitSelect.SetActive(false);                                                    // Selector de circuito
        _backBtn.gameObject.SetActive(false);                                               // Botón de retroceso
        _startBtn.gameObject.SetActive(false);                                              // Botón de inicio de la partida
    }
}
