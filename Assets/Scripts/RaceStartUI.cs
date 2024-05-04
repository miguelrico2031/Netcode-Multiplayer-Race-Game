using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class RaceStartUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _raceStartText;
    [SerializeField] private GameObject _coverPanel;

    private void Start()
    {
        _raceStartText.gameObject.SetActive(false);
        GameManager.Instance.RaceController.RaceCountdown.OnValueChanged += OnCountdown;
    }

    private void OnCountdown(int _, int n)
    {
        if (n != 0)
        {
            _coverPanel.SetActive(false);
            _raceStartText.gameObject.SetActive(true);
            _raceStartText.text = $"{n}";
            return;
        }

        _raceStartText.text = "GO!";
        StartCoroutine(HideUI());
    }

    private IEnumerator HideUI()
    {
        GameManager.Instance.RaceController.RaceCountdown.OnValueChanged -= OnCountdown;

        float t = 0;
        while (_raceStartText.color.a > 0)
        {
            var c = _raceStartText.color;
            c.a = Mathf.Lerp(1, 0, t);
            _raceStartText.color = c;
            yield return new WaitForSeconds(.05f);
            t += .1f;
        }
        _raceStartText.gameObject.SetActive(false);
    }
}
