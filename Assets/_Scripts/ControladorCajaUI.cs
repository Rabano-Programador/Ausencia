using UnityEngine;
using TMPro;

public class ControladorCajaUI : MonoBehaviour
{
    [Header("Pantalla de la Computadora")]
    public TextMeshProUGUI textoTotalPantalla;

    private float totalCuenta = 0f;

    private void OnEnable()
    {
        totalCuenta = 0f;
        ActualizarPantalla();
    }

    public void RegistrarProductoEscaneado(float precioProducto)
    {
        totalCuenta += precioProducto;
        ActualizarPantalla();
        GameManager.Instance.RegistrarVenta(precioProducto, true);
    }

    public void RegistrarCobroEquivocado(float precioProducto)
    {
        GameManager.Instance.RegistrarVenta(precioProducto, false);
    }

    private void ActualizarPantalla()
    {
        if (textoTotalPantalla != null)
        {
            textoTotalPantalla.text = "TOTAL: $" + totalCuenta.ToString("F2");
        }
    }

    public float ObtenerTotalCuenta()
    {
        return totalCuenta;
    }

    public void LimpiarCajaFinTurno()
    {
        totalCuenta = 0f;
        ActualizarPantalla();
    }
}