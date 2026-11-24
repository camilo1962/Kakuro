using UnityEngine;
using UnityEngine.UI;

// Se configura en el prefab del botón. No cambia la imagen de fondo ni el botón.
public sealed class KakuroIconoVoz : MonoBehaviour
{
    [Header("Iconos editables")]
    [Tooltip("Imagen hija del botón donde se muestra el icono.")]
    public Image imagen;
    [Tooltip("Sprite cuando la voz está desactivada.")]
    public Sprite vozApagada;
    [Tooltip("Sprite cuando la voz está activada.")]
    public Sprite vozActiva;

    public bool Configurado => imagen != null && vozApagada != null && vozActiva != null;
    public void Actualizar(bool activa)
    {
        if (imagen == null) return;
        if (imagen.gameObject.activeSelf != Configurado) imagen.gameObject.SetActive(Configurado);
        if (!Configurado) return;
        Sprite deseado = activa ? vozActiva : vozApagada;
        if (imagen.sprite != deseado) imagen.sprite = deseado;
    }
}
