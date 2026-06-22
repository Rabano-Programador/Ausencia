using UnityEngine;
using TMPro;

public class RestockShelf : MonoBehaviour
{
    [Header("Configuración del Estante")]
    public ProductoData productoRequerido;

    [Tooltip("Crea Emptys donde quieres que aparezcan los productos y arrástralos aquí")]
    public Transform[] puntosDeColocacion;

    [HideInInspector] public int stockActual = 0;
    private GameObject[] productosVisuales;

    [Header("UI Cuota Global (KPI)")]
    public TextMeshProUGUI textoKPI;
    private static int totalReposicionesSemanales = 0;

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

        if (cajaDelJugador.datosProducto == null || productoRequerido == null) return;

        if (cajaDelJugador.datosProducto == productoRequerido)
        {
            if (cajaDelJugador.PillarProducto())
            {
                Transform punto = puntosDeColocacion[stockActual];
                GameObject nuevoItem = Instantiate(productoRequerido.prefabIndividual, punto.position, punto.rotation);

                productosVisuales[stockActual] = nuevoItem;

                stockActual++;
                totalReposicionesSemanales++;
                ActualizarUITienda();

                if (GameManager.Instance != null) GameManager.Instance.RegistrarTrabajo(15f);
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

    private void ActualizarUITienda()
    {
        if (textoKPI != null) textoKPI.text = "Reposiciones: " + totalReposicionesSemanales;
    }
}