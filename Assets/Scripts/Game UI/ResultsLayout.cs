using TMPro;
using UnityEngine;

public class ResultsLayout : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _lapTimeText;
    [SerializeField] private PositionIndicatorUI _raceResultsText;

    private readonly string[] _lapTimes = new string[4];

    public void Show()
    {
        _lapTimeText.text = "";
        foreach (string lapTime in _lapTimes)
        {
            _lapTimeText.text += lapTime + "\n";
        }
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetPositionIndicator(PositionIndicatorUI positionIndicator)
    {
        _raceResultsText = positionIndicator;
    }

    public void SaveLapTime(int index, string lapTime)
    {
        _lapTimes[index] = lapTime;
        Debug.Log("Lap time recorded: " + lapTime);
    }
}