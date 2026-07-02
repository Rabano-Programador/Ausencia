using UnityEngine;
using TMPro;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RestockShelf : MonoBehaviour
{
    public static readonly List<RestockShelf> Instancias = new List<RestockShelf>();

    [Header("Configuración del Estante")]
    public ProductoData productoRequerido;

    [Tooltip("Crea Emptys donde quieres que aparezcan los productos y arrástralos aquí")]
    public Transform[] puntosDeColocacion;

    [Header("Indicador de Reposición")]
    public TMP_Text textoIndicador;
    public string mensajeIndicador = "Trae este producto aqui";

    [Header("Debug Stock")]
    [Tooltip("Stock real que leen los NPCs. Si esto esta en 0 durante Play Mode, los NPCs ignoraran este estante aunque veas objetos puestos a mano.")]
    public int stockActual = 0;
    public bool debugRestockShelf = true;
    private GameObject[] productosVisuales;

    [Header("UI Cuota Global (KPI)")]
    public TextMeshProUGUI textoKPI;
    private static int totalReposicionesSemanales = 0;

    [Header("Debug Puntos de Colocacion")]
    public bool mostrarPuntosEnEditor = true;
    public bool mostrarPuntosSoloSeleccionado = false;
    public bool mostrarNumerosPuntos = true;
    public Color colorPuntos = new Color(0f, 1f, 0.35f, 0.75f);
    public Color colorPuntoSiguiente = new Color(1f, 0.75f, 0f, 0.9f);
    public Vector3 tamanoGizmoPunto = new Vector3(0.25f, 0.25f, 0.25f);

    private void OnEnable()
    {
        if (!Instancias.Contains(this))
            Instancias.Add(this);

        OcultarIndicador();
    }

    private void OnDisable()
    {
        Instancias.Remove(this);
    }

    private void Start()
    {
        InicializarArrayVisuales();
        ActualizarUITienda();
    }

    void InicializarArrayVisuales()
    {
        int cantidadPuntos = puntosDeColocacion != null ? puntosDeColocacion.Length : 0;

        if (productosVisuales == null || productosVisuales.Length != cantidadPuntos)
            productosVisuales = new GameObject[cantidadPuntos];
    }

    public bool ReponerProducto(ProductBox cajaDelJugador)
    {
        InicializarArrayVisuales();

        if (puntosDeColocacion == null || puntosDeColocacion.Length == 0)
        {
            LogDebug("No puedo reponer: no hay puntosDeColocacion asignados.");
            return false;
        }

        if (stockActual >= puntosDeColocacion.Length)
        {
            LogDebug("No puedo reponer: estante lleno.");
            return false;
        }

        if (cajaDelJugador == null || cajaDelJugador.datosProducto == null || productoRequerido == null)
        {
            LogDebug("No puedo reponer: falta caja, datosProducto o productoRequerido.");
            return false;
        }

        if (productoRequerido.prefabIndividual == null)
        {
            LogDebug($"No puedo reponer '{productoRequerido.nombreProducto}': prefabIndividual no asignado.");
            return false;
        }

        if (cajaDelJugador.datosProducto == productoRequerido)
        {
            if (cajaDelJugador.PillarProducto())
            {
                Transform punto = puntosDeColocacion[stockActual];
                if (punto == null)
                {
                    LogDebug($"No puedo reponer: puntoDeColocacion {stockActual} es null.");
                    return false;
                }

                int indiceColocado = stockActual;
                GameObject nuevoItem = Instantiate(
                    productoRequerido.prefabIndividual,
                    punto.position,
                    punto.rotation * Quaternion.Euler(productoRequerido.rotacionEnEstanteria));

                nuevoItem.transform.localScale = productoRequerido.ObtenerEscalaParaEstanteria();

                productosVisuales[indiceColocado] = nuevoItem;

                stockActual++;
                totalReposicionesSemanales++;
                ActualizarUITienda();
                LogDebug($"Repuesto '{productoRequerido.nombreProducto}' en punto {indiceColocado}. Stock actual: {stockActual}.");
                return true;
            }

            LogDebug($"La caja de '{cajaDelJugador.datosProducto.nombreProducto}' no tenia unidades para reponer.");
        }
        else
        {
            LogDebug($"Producto incorrecto. Este estante pide '{productoRequerido.nombreProducto}', pero la caja trae '{cajaDelJugador.datosProducto.nombreProducto}'.");
        }

        return false;
    }

    public ProductoData TomarProductoNPC()
    {
        InicializarArrayVisuales();

        if (puntosDeColocacion == null || puntosDeColocacion.Length == 0)
        {
            LogDebug("NPC intento tomar producto, pero no hay puntosDeColocacion asignados.");
            return null;
        }

        if (stockActual > puntosDeColocacion.Length)
        {
            LogDebug($"StockActual estaba fuera de rango ({stockActual}). Lo ajusto al maximo de puntos ({puntosDeColocacion.Length}).");
            stockActual = puntosDeColocacion.Length;
        }

        if (stockActual > 0)
        {
            stockActual--;
            GameObject visualTomado = stockActual < productosVisuales.Length ? productosVisuales[stockActual] : null;

            if (stockActual < productosVisuales.Length)
                productosVisuales[stockActual] = null;

            if (visualTomado != null)
                Destroy(visualTomado);
            else
                LogDebug($"El NPC tomo stock del punto {stockActual}, pero no habia visual guardado. Revisa si el objeto fue puesto a mano o se destruyo antes.");

            LogDebug($"NPC tomo '{productoRequerido.nombreProducto}'. Stock restante: {stockActual}.");
            return productoRequerido;
        }

        LogDebug("NPC intento tomar producto, pero el stock estaba en 0.");
        return null;
    }

    public bool TieneStockDisponible()
    {
        return stockActual > 0 && productoRequerido != null;
    }

    public bool ObtenerPuntoProductoDisponible(out Vector3 posicion)
    {
        posicion = transform.position;

        if (!TieneStockDisponible() || puntosDeColocacion == null || puntosDeColocacion.Length == 0)
            return false;

        int indiceProducto = Mathf.Clamp(stockActual - 1, 0, puntosDeColocacion.Length - 1);
        Transform punto = puntosDeColocacion[indiceProducto];
        if (punto == null)
            return false;

        posicion = punto.position;
        return true;
    }

    public string ObtenerNombreProducto()
    {
        return productoRequerido != null ? productoRequerido.nombreProducto : "Sin ProductoData";
    }

    public bool PuedeRecibirProducto(ProductoData producto)
    {
        return producto != null &&
            productoRequerido != null &&
            producto == productoRequerido &&
            puntosDeColocacion != null &&
            stockActual < puntosDeColocacion.Length;
    }

    public void MostrarIndicadorPara(ProductoData producto)
    {
        if (textoIndicador == null)
            return;

        bool mostrar = PuedeRecibirProducto(producto);
        textoIndicador.gameObject.SetActive(mostrar);

        if (mostrar)
            textoIndicador.text = string.IsNullOrWhiteSpace(mensajeIndicador)
                ? productoRequerido.nombreProducto
                : mensajeIndicador;
    }

    public void OcultarIndicador()
    {
        if (textoIndicador != null)
            textoIndicador.gameObject.SetActive(false);
    }

    private void ActualizarUITienda()
    {
        if (textoKPI != null) textoKPI.text = "Reposiciones: " + totalReposicionesSemanales;
    }

    void LogDebug(string mensaje)
    {
        if (!debugRestockShelf)
            return;

        Debug.Log($"<color=#7CFF7C>RestockShelf '{name}': {mensaje}</color>");
    }

    private void OnDrawGizmos()
    {
        if (mostrarPuntosSoloSeleccionado)
            return;

        DibujarPuntosDeColocacion();
    }

    private void OnDrawGizmosSelected()
    {
        DibujarPuntosDeColocacion();
    }

    private void DibujarPuntosDeColocacion()
    {
        if (!mostrarPuntosEnEditor || puntosDeColocacion == null)
            return;

        for (int i = 0; i < puntosDeColocacion.Length; i++)
        {
            Transform punto = puntosDeColocacion[i];
            if (punto == null)
                continue;

            bool esSiguiente = i == stockActual;
            Gizmos.color = esSiguiente ? colorPuntoSiguiente : colorPuntos;
            Gizmos.matrix = Matrix4x4.TRS(punto.position, punto.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, tamanoGizmoPunto);
            Gizmos.DrawWireCube(Vector3.zero, tamanoGizmoPunto * 1.1f);
            Gizmos.matrix = Matrix4x4.identity;

            if (i + 1 < puntosDeColocacion.Length && puntosDeColocacion[i + 1] != null)
            {
                Gizmos.color = colorPuntos;
                Gizmos.DrawLine(punto.position, puntosDeColocacion[i + 1].position);
            }

#if UNITY_EDITOR
            if (mostrarNumerosPuntos)
            {
                Handles.color = esSiguiente ? colorPuntoSiguiente : colorPuntos;
                Handles.Label(punto.position + Vector3.up * tamanoGizmoPunto.y, i.ToString());
            }
#endif
        }
    }
}
