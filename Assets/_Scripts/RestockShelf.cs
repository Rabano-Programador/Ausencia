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
            Debug.Log("<color=orange>El estante ya está lleno.</color>");
            return;
        }

        if (cajaDelJugador == null || cajaDelJugador.datosProducto == null || productoRequerido == null)
        {
            Debug.LogWarning("<color=orange>RestockShelf: Faltan datos para reponer el producto.</color>");
            return;
        }

        if (productoRequerido.prefabIndividual == null)
        {
            Debug.LogWarning($"<color=orange>RestockShelf: '{productoRequerido.nombreProducto}' no tiene prefabIndividual asignado.</color>");
            return;
        }

        if (cajaDelJugador.datosProducto == productoRequerido)
        {
            if (cajaDelJugador.PillarProducto())
            {
                Transform punto = puntosDeColocacion[stockActual];
                if (punto == null)
                {
                    Debug.LogWarning($"<color=orange>RestockShelf: El punto de colocación {stockActual} no está asignado.</color>");
                    return;
                }

                GameObject nuevoItem = Instantiate(productoRequerido.prefabIndividual, punto.position, punto.rotation);
                nuevoItem.transform.SetParent(punto, true);

                productosVisuales[stockActual] = nuevoItem;

                stockActual++;
                totalReposicionesSemanales++;
                ActualizarUITienda();

                if (GameManager.Instance != null) GameManager.Instance.RegistrarTrabajo(15f);
            }
        }
        else
        {
            Debug.Log($"<color=orange>RestockShelf: Este estante requiere '{productoRequerido.nombreProducto}' y la caja tiene '{cajaDelJugador.datosProducto.nombreProducto}'.</color>");
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
