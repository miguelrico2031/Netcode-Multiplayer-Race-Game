using UnityEngine;
using TMPro;

public class TimerUI : MonoBehaviour
{
    #region public

    public bool timerActive = false;
    
    #endregion

    #region private

    private TextMeshProUGUI TimerText;
    private float _seconds, _minutes, _milsecs;
    private float _startTime;

    #endregion

    private void Start()
    {
        TimerText = GetComponent<TextMeshProUGUI>();
        TimerText.text = "00:00.000";
    }

    private void Update()
    {
        if (!timerActive) return;

        UpdateTimeValues();
        UpdateTimerString();
    }

    public void StartTimer()
    {
        timerActive = true;
        _startTime = Time.time;
    }

    public void StopTimer()
    {
        timerActive = false;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void UpdateTimeValues()
    {
        _minutes = (int) ((Time.time - _startTime) / 60f); 
        _seconds = (int) ((Time.time - _startTime) % 60f);
        _milsecs = (int) ((Time.time - _startTime) * 1000f) % 1000;
    }

    private void UpdateTimerString()
    {
        TimerText.text = ToString();
    }

    override public string ToString()
    {
        return _minutes.ToString("00") + ":" + _seconds.ToString("00") + "." + _milsecs.ToString("000");
    }
}