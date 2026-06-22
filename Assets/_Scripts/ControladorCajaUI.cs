using UnityEngine;
using TMPro;

public class ControladorCajaUI : MonoBehaviour
{
    [Header("Pantalla de la Computadora")]
    public TextMeshProUGUI textoTotalPantalla;

    private float totalCuenta = 0f;

    // Reinicia la cuenta a cero (ideal para cuando entra el jugador)
    private void OnEnable()
    {
        totalCuenta = 0f;
        ActualizarPantalla();
    }

    // Recibe el precio desde el PlayerController y lo suma
    public void RegistrarProductoEscaneado(float precioProducto)
    {
        totalCuenta += precioProducto;
        ActualizarPantalla();
        Debug.Log("Producto escaneado. Total actual: $" + totalCuenta);
    }

    private void ActualizarPantalla()
    {
        if (textoTotalPantalla != null)
        {
            textoTotalPantalla.text = "TOTAL: $" + totalCuenta.ToString("F2");
        }
    }
}