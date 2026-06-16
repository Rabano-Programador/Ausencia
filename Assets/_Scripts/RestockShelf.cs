using UnityEngine;
using TMPro;

public class RestockShelf : MonoBehaviour
{
    [Header("Configuración del Estante")]
    public ProductoData productoRequerido;
    public int stockActual = 0;
    public int stockMaximo = 25;

    [Header("UI Cuota Global (KPI)")]
    public TextMeshProUGUI textoKPI;
    private static int totalReposicionesSemanales = 0;

    private void Start()
    {
        ActualizarUITienda();
    }

    //Por mientras añadi debugs para saber si los productos estan bien o mal o si esta llena la estanteria etc, luego se pueden cambiar por animaciones o sonidos o lo que se quiera para dar feedback al jugador.
    public void ReponerProducto(ProductBox cajaDelJugador)
    {
        if (stockActual >= stockMaximo)
        {
            Debug.Log("<color=orange>El estante ya está lleno.</color>");
            return;
        }

        if (cajaDelJugador.datosProducto == null)
        {
            return;
        }
        if (productoRequerido == null)
        {
            return;
        }

        if (cajaDelJugador.datosProducto == productoRequerido)
        {
            if (cajaDelJugador.PillarProducto())
            {
                stockActual++;
                totalReposicionesSemanales++;
                ActualizarUITienda();
                Debug.Log("<color=green>¡Producto guardado exitosamente!</color>");
            }
        }
        else
        {
            Debug.Log("<color=red>RECHAZADO: Este estante pide " + productoRequerido.nombreProducto + " y tú traes " + cajaDelJugador.datosProducto.nombreProducto + "</color>");
        }
    }

    private void ActualizarUITienda()
    {
        if (textoKPI != null)
        {
            textoKPI.text = "Reposiciones: " + totalReposicionesSemanales + "/25";
        }
    }
}