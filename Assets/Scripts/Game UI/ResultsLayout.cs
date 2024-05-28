using TMPro;
using UnityEngine;

public class ResultsLayout : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _lapTimeText;
    [SerializeField] private PositionIndicatorUI _raceEndPositionsText;

    private string[] _lapTimes = new string[3];
    private string _totalTime;

    public void Show()
    {
        LoadLapTimes();
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetPositionIndicator(PositionIndicatorUI positionIndicator)
    {
        _raceEndPositionsText = positionIndicator;
    }

    public void SetLapTimes(string[] lapTimes, string totalTime)
    {
        _lapTimes = lapTimes;
        _totalTime = totalTime;
    }

    private void LoadLapTimes()
    {
        _lapTimeText.text = "";
        foreach (string lapTime in _lapTimes)
        {
            _lapTimeText.text += lapTime + "\n";
        }
        _lapTimeText.text += "----------\n" + _totalTime;
    }
}