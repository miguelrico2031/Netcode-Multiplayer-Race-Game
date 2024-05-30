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

    #endregion

    private void Start()
    {

        _positionIndicatorText = GetComponent<TextMeshProUGUI>();
        _positionIndicatorText.text = "Training Mode";

    }

    private void Update()
    {
        if (GameManager.Instance.TrainingMode) return;                           // No se muestra en el modo de entrenamiento, así que no se actualiza

        GetPositionsList();
        //UpdatePlayerPositionList();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /*
     * Obtiene la lista de posiciones de los jugadores en orden
     */  
    public string GetPositionsList()
    {
        _positionIndicatorText.text = "";                                       // La vacía para actualizarla en el update
        var st = GameManager.Instance.RaceController.PlayerOrder.Value.Value;   // Toma la lista de los jugadores ordenados desde el RaceController 
        var stsplit = st.Split(";");
        foreach (var p in stsplit)
        {
            if (p == "") continue;
            _positionIndicatorText.text += $"{p}\n";                            // Muestra cada jugador como una línea separada
        }

        return _positionIndicatorText.text;
    }
}
