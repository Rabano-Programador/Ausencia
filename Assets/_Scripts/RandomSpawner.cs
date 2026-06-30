using UnityEngine;
using System.Collections.Generic;

public class RandomSpawner : MonoBehaviour
{
    [Header("ConfiguraciÃ³n del Spawn")]
    public GameObject cajaPrefab;
    public ProductoData[] posiblesProductos;

    [Header("Visual de la Caja")]
    public Vector3 offsetVisualProducto = Vector3.up * 0.35f;
    public Vector3 rotacionVisualProducto;
    public Vector3 escalaVisualProducto = Vector3.one * 0.6f;
    public bool ocultarMeshCajaGenerica = true;

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
                ConfigurarVisualCaja(nuevaCaja, productoElegido);
            }
            else
            {
                Destroy(nuevaCaja);
            }
        }
    }

    public void ForzarSpawnCaja()
    {
        if (AreaEstaBloqueada())
        {
            return;
        }

        SpawnearCaja();
        AsignarNuevoTiempo();
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

    void ConfigurarVisualCaja(GameObject nuevaCaja, ProductoData productoElegido)
    {
        if (nuevaCaja == null || productoElegido == null)
            return;

        if (productoElegido.prefabIndividual == null)
        {
            return;
        }

        if (ocultarMeshCajaGenerica)
        {
            MeshRenderer[] renderersCaja = nuevaCaja.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer renderer in renderersCaja)
                renderer.enabled = false;
        }

        GameObject visualProducto = Instantiate(productoElegido.prefabIndividual, nuevaCaja.transform);
        visualProducto.name = productoElegido.nombreProducto + " Visual";
        visualProducto.transform.localPosition = offsetVisualProducto;
        visualProducto.transform.localRotation = Quaternion.Euler(rotacionVisualProducto + productoElegido.rotacionEnCaja);
        visualProducto.transform.localScale = Vector3.Scale(escalaVisualProducto, productoElegido.escalaEnCaja);

        Collider[] collidersVisual = visualProducto.GetComponentsInChildren<Collider>();
        foreach (Collider col in collidersVisual)
            col.enabled = false;

        Rigidbody[] rigidbodiesVisual = visualProducto.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rigidbodiesVisual)
            rb.isKinematic = true;
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
