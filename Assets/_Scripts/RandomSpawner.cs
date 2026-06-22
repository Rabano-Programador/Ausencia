using UnityEngine;
using System.Collections.Generic;

public class RandomSpawner : MonoBehaviour
{
    [Header("Configuración del Spawn")]
    public GameObject cajaPrefab;
    public ProductoData[] posiblesProductos;

    [Header("Tiempos")]
    public float tiempoMinimo = 5f;
    public float tiempoMaximo = 15f;

    [Header("Tope de Altura (Anti-Spam)")]
    public float radioDeComprobacion = 1f;

    private float timer = 0f;
    private float tiempoObjetivo;

    void Start()
    {
        AsignarNuevoTiempo();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= tiempoObjetivo)
        {
            if (!AreaEstaBloqueada())
            {
                SpawnearCaja();
            }
            AsignarNuevoTiempo();
        }
    }

    bool AreaEstaBloqueada()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, radioDeComprobacion);
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Item")) return true;
        }
        return false;
    }

    void SpawnearCaja()
    {
        GameObject nuevaCaja = Instantiate(cajaPrefab, transform.position, transform.rotation);
        ProductBox scriptCaja = nuevaCaja.GetComponent<ProductBox>();

        if (scriptCaja != null)
        {
            ProductoData productoElegido = ElegirProductoFaltante();

            if (productoElegido != null)
            {
                scriptCaja.datosProducto = productoElegido;
                scriptCaja.unidadesRestantes = 6;
                nuevaCaja.name = "Caja de " + productoElegido.nombreProducto;
            }
            else
            {
                Debug.Log("<color=red>No se encontró un producto válido. Destruyendo caja defectuosa...</color>");
                Destroy(nuevaCaja);
            }
        }
    }

    ProductoData ElegirProductoFaltante()
    {
        RestockShelf[] estantes = FindObjectsByType<RestockShelf>(FindObjectsSortMode.None);
        List<ProductoData> productosFaltantes = new List<ProductoData>();

        foreach (RestockShelf estante in estantes)
        {
            if (estante.productoRequerido != null && estante.puntosDeColocacion != null)
            {
                if (estante.stockActual < estante.puntosDeColocacion.Length)
                {
                    productosFaltantes.Add(estante.productoRequerido);
                }
            }
        }

        if (productosFaltantes.Count > 0)
        {
            int index = Random.Range(0, productosFaltantes.Count);
            return productosFaltantes[index];
        }

        List<ProductoData> productosValidos = new List<ProductoData>();
        foreach (ProductoData p in posiblesProductos)
        {
            if (p != null) productosValidos.Add(p);
        }

        if (productosValidos.Count > 0)
        {
            return productosValidos[Random.Range(0, productosValidos.Count)];
        }

        return null;
    }

    void AsignarNuevoTiempo()
    {
        tiempoObjetivo = Random.Range(tiempoMinimo, tiempoMaximo);
        timer = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radioDeComprobacion);
    }
}