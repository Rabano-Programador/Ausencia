using UnityEngine;
using TMPro;

public class TransbankUI : MonoBehaviour
{
    [Header("Display de la Maquinita")]
    public TextMeshProUGUI textoMontoIngresado;

    private string montoActualCadena = "";
    private ControladorCajaUI cajaUI;
    private PlayerController player;

    private void OnEnable()
    {
        cajaUI = FindFirstObjectByType<ControladorCajaUI>();
        player = FindFirstObjectByType<PlayerController>();

        montoActualCadena = "";
        ActualizarDisplay();
    }

    public void EscribirDigito(string digito)
    {
        if (digito == "." && montoActualCadena.Contains(".")) return;

        if (montoActualCadena.Length >= 8) return;

        montoActualCadena += digito;
        ActualizarDisplay();
    }

    public void BorrarUltimoDigito()
    {
        if (montoActualCadena.Length > 0)
        {
            montoActualCadena = montoActualCadena.Substring(0, montoActualCadena.Length - 1);
            ActualizarDisplay();
        }
    }

    private void ActualizarDisplay()
    {
        if (textoMontoIngresado != null)
        {
            textoMontoIngresado.text = "$ " + (string.IsNullOrEmpty(montoActualCadena) ? "00.00" : montoActualCadena);
        }
    }

    public void ProcesarPagoTarjeta()
    {
        float montoIngresado = 0f;
        bool conversionExitosa = float.TryParse(
            montoActualCadena,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out montoIngresado
        );

        if (conversionExitosa)
        {
            float totalRealCaja = cajaUI.ObtenerTotalCuenta();

            Debug.Log("Transbank leyó: " + montoIngresado + " | La computadora tiene: " + totalRealCaja);

            if (Mathf.Abs(montoIngresado - totalRealCaja) < 0.02f)
            {
                Debug.Log("<color=green>¡Monto Coincide! Cobro Exitoso.</color>");

                cajaUI.LimpiarCajaFinTurno();
                player.SalirDeModoTransbank();
            }
            else
            {
                Debug.LogWarning("<color=orange>ERROR: Los montos no coinciden matemáticamente.</color>");
            }
        }
    }

    private void ResetearPantallaError()
    {
        montoActualCadena = "";
        ActualizarDisplay();
    }
}
