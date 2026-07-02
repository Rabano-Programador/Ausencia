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

    [HideInInspector] public int stockActual = 0;
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
        productosVisuales = new GameObject[puntosDeColocacion.Length];
        ActualizarUITienda();
    }

    public bool ReponerProducto(ProductBox cajaDelJugador)
    {
        if (stockActual >= puntosDeColocacion.Length)
        {
            return false;
        }

        if (cajaDelJugador == null || cajaDelJugador.datosProducto == null || productoRequerido == null)
        {
            return false;
        }

        if (productoRequerido.prefabIndividual == null)
        {
            return false;
        }

        if (cajaDelJugador.datosProducto == productoRequerido)
        {
            if (cajaDelJugador.PillarProducto())
            {
                Transform punto = puntosDeColocacion[stockActual];
                if (punto == null)
                {
                    return false;
                }

                GameObject nuevoItem = Instantiate(
                    productoRequerido.prefabIndividual,
                    punto.position,
                    punto.rotation * Quaternion.Euler(productoRequerido.rotacionEnEstanteria));

                nuevoItem.transform.localScale = productoRequerido.ObtenerEscalaParaEstanteria();

                productosVisuales[stockActual] = nuevoItem;

                stockActual++;
                totalReposicionesSemanales++;
                ActualizarUITienda();
                return true;
            }
        }

        return false;
    }

    public ProductoData TomarProductoNPC()
    {
        if (stockActual > 0)
        {
            stockActual--;
            Destroy(productosVisuales[stockActual]);
            return productoRequerido;
        }
        return null;
    }

    public bool PuedeRecibirProducto(ProductoData producto)
    {
        return producto != null &&
            productoRequerido != null &&
            producto == productoRequerido &&
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
