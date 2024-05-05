using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class PositionIndicatorUI : MonoBehaviour
{

    #region public

    public bool positionIndicatorActive = false;

    #endregion

    #region private

    private TextMeshProUGUI PositionIndicatorText;
    private int _playerPosition;
    private int _totalPlayers;

    #endregion

    private void Start()
    {
        PositionIndicatorText = GetComponent<TextMeshProUGUI>();
        PositionIndicatorText.text = "0/0";

        _totalPlayers = GetTotalPlayers();

        StartPositionIndicator();
    }

    private void Update()
    {
        if (!positionIndicatorActive) return;

        UpdatePlayerPositionNumber();
        UpdatePositionIndicatorString();
    }

    public void StartPositionIndicator()
    {
        positionIndicatorActive = true;
    }

    // creo que esto así... mal.  porque usa un foreach en cada frame y well, no es lo mejor
    private void UpdatePlayerPositionNumber()
    {
        try
        {
            foreach (var p in FindObjectsOfType<Player>())
            {
                if (p.IsOwner)
                {
                    _playerPosition = GameManager.Instance.RaceController.GetPlayerPosition(p.ID);
                    if (!positionIndicatorActive) StartPositionIndicator();                                 // Si hemos llegado aquí, se tienen todas las referencias necesarias y se puede comenzar a mostrar la posición
                    break;
                }
            }
        } catch (System.Exception e)
        {
            Debug.LogError("Mecachis en la mar no se en qué posición está el jugador: " + e.Message);
        }
    }

    private void UpdatePositionIndicatorString()
    {
        PositionIndicatorText.text = _playerPosition.ToString() + "º/" + _totalPlayers.ToString();
    }
    
    private int GetTotalPlayers()
    {
        return GameManager.Instance.NumPlayers.Value;
    }
}
