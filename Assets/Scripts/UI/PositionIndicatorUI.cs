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
        foreach (var p in GameManager.Instance.RaceController.PlayerOrder.Value.Value.Split(" "))
        {
            var id = ulong.Parse(p);
            PositionIndicatorText.text += $"{id}\n";
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
