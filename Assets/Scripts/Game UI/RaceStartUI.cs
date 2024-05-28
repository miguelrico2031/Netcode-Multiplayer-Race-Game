using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Clase que gestiona la UI de inicio de la carrera.
/// Dispone de un Overlay a modo de pantalla de carga.
/// Tambi�n se encarga de mostrar la cuenta atr�s de la carrera.
/// </summary>
public class RaceStartUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _raceStartText;
    [SerializeField] private GameObject _coverPanel;

    private void Start()
    {
        _raceStartText.gameObject.SetActive(false);
        GameManager.Instance.RaceController.RaceCountdown.OnValueChanged += OnCountdown;
    }

    /*
     * M�todo del texto de la cuenta atr�s de la carrera.
     */
    private void OnCountdown(int _, int n)
    {
        if (n != 0)
        {
            _coverPanel.SetActive(false);                               // Desactiva el panel de carga.
            _raceStartText.gameObject.SetActive(true);
            _raceStartText.text = $"{n}";
            return;
        }

        _raceStartText.text = "GO!";
        StartCoroutine(HideUI());
    }

    /*
     * M�todo que oculta el panel de carga y el texto de la cuenta atr�s.
     * Activa el HUD de la carrera.
     * Tambi�n elimina el evento de la cuenta atr�s.
     */
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
        gameObject.SetActive(false);
    }
}
