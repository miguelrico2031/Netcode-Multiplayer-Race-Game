using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class PositionIndicatorUI : MonoBehaviour
{

    #region public


    #endregion

    #region private

    private TextMeshProUGUI _positionIndicatorText;
    private int _playerPosition;
    private int _totalPlayers;

    

    #endregion

    private void Start()
    {
        _positionIndicatorText = GetComponent<TextMeshProUGUI>();
        _positionIndicatorText.text = "N/A";

        _totalPlayers = GetTotalPlayers();
    }

    private void Update()
    {
        UpdatePlayerPositionList();
    }


    private void UpdatePlayerPositionList()
    {
        _positionIndicatorText.text = "";
        var st = GameManager.Instance.RaceController.PlayerOrder.Value.Value;
        var stsplit = st.Split(";");
        foreach (var p in stsplit)
        {
            if(p == "") continue;
            _positionIndicatorText.text += $"{p}\n";
        }
    }
    
    private int GetTotalPlayers()
    {
        return GameManager.Instance.NumPlayers.Value;
    }
}
