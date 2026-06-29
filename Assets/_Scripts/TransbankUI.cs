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
        AudioManager.instance.ReproducirSonido(AudioManager.instance.sonidoBotonTransbank);

        if (textoMontoIngresado != null)
        {
            textoMontoIngresado.text = "$ " + (string.IsNullOrEmpty(montoActualCadena) ? "00.00" : montoActualCadena);
        }
    }

    public void ProcesarPagoTarjeta()
    {
        float montoIngresado = 0f;
        AudioManager.instance.ReproducirSonido(AudioManager.instance.sonidoCobroCaja);
        bool conversionExitosa = float.TryParse(
            montoActualCadena,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out montoIngresado

        );

        if (conversionExitosa)
        {
            float totalRealCaja = cajaUI.ObtenerTotalCuenta();


            if (Mathf.Abs(montoIngresado - totalRealCaja) < 0.02f)
            {
                NPCCliente npcActual = QueueManager.Instance != null ? QueueManager.Instance.NPCActualEnCaja : null;
                PuntoEntregaTrigger puntoEntrega = FindFirstObjectByType<PuntoEntregaTrigger>();
                int objetosEntregados = 0;

                if (npcActual != null && puntoEntrega != null)
                    objetosEntregados = puntoEntrega.EntregarObjetosAlNPC(npcActual);

                cajaUI.LimpiarCajaFinTurno();
                player.SalirDeModoTransbank();

                if (npcActual != null)
                    npcActual.RecibirPermisoDeSalir();

                Debug.Log($"<color=green>Pago exitoso. NPC recupera {objetosEntregados} objeto(s) de PuntoEntrega y se va.</color>");
            }
            else
            {
            }
        }
    }

    private void ResetearPantallaError()
    {
        montoActualCadena = "";
        ActualizarDisplay();
    }
}
