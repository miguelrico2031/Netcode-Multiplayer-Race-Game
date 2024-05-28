using TMPro;
using UnityEngine;

/// <summary>
/// Clase que gestiona la lista del orden de los jugadores en el HUD durante la carrera.
/// Actualmente se usa en la pantalla de resultados de la carrera.
/// </summary>
public class PositionIndicatorUI : MonoBehaviour
{
    // IMPORTANTE METER UN MÉTODO QUE HAGA UNA LISTA FIJA DE LOS JUGADORES QUE VAYAN LLEGANDO A LA META.
    #region private

    private TextMeshProUGUI _positionIndicatorText;                                      
    private int _totalPlayers;                                                  // SIN USAR. Número total de jugadores en la carrera.

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

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /*
     * Actualiza la lista de posiciones de los jugadores
     */
    private void UpdatePlayerPositionList()
    {
        _positionIndicatorText.text = "";
        var st = GameManager.Instance.RaceController.PlayerOrder.Value.Value;   // Toma la lista de los jugadores ordenados desde el RaceController 
        var stsplit = st.Split(";");
        foreach (var p in stsplit)
        {
            if(p == "") continue;
            _positionIndicatorText.text += $"{p}\n";                            // Muestra cada jugador como una línea separada
        }
    }
    
    /* SIN USAR
     * Obtiene el número total de jugadores en la carrera
     */
    private int GetTotalPlayers()
    {
        return GameManager.Instance.NumPlayers.Value;
    }
}
