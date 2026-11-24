using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Las referencias visuales se editan en el prefab; la lógica vive en DemoKakuroMovil.
public sealed class KakuroPanelDificultad : MonoBehaviour
{
    public TMP_Text titulo;
    public TMP_Text aviso;
    public Button facil;
    public Button medio;
    public Button dificil;
    public Button continuar;
    public Button menuPrincipal;
    public Button volverTablero;

    public bool ReferenciasValidas()
    {
        return transform is RectTransform && titulo != null && aviso != null &&
            facil != null && medio != null && dificil != null && continuar != null &&
            menuPrincipal != null && volverTablero != null;
    }
}
