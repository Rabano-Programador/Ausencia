using UnityEngine;
using TMPro;
using System.Collections.Generic;

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

    public void ReponerProducto(ProductBox cajaDelJugador)
    {
        if (stockActual >= puntosDeColocacion.Length)
        {
            return;
        }

        if (cajaDelJugador == null || cajaDelJugador.datosProducto == null || productoRequerido == null)
        {
            return;
        }

        if (productoRequerido.prefabIndividual == null)
        {
            return;
        }

        if (cajaDelJugador.datosProducto == productoRequerido)
        {
            if (cajaDelJugador.PillarProducto())
            {
                Transform punto = puntosDeColocacion[stockActual];
                if (punto == null)
                {
                    return;
                }

                GameObject nuevoItem = Instantiate(
                    productoRequerido.prefabIndividual,
                    punto.position,
                    punto.rotation * Quaternion.Euler(productoRequerido.rotacionEnEstanteria));

                nuevoItem.transform.localScale = Vector3.Scale(
                    nuevoItem.transform.localScale,
                    productoRequerido.escalaEnEstanteria);

                productosVisuales[stockActual] = nuevoItem;

                stockActual++;
                totalReposicionesSemanales++;
                ActualizarUITienda();
            }
        }
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
}
