using UnityEngine;
using System.Collections.Generic;

public class RandomSpawner : MonoBehaviour
{
    private class VisualProductoActivo
    {
        public Transform visual;
        public ProductoData producto;
    }

    [Header("Configuracion del Spawn")]
    public GameObject cajaPrefab;
    public ProductoData[] posiblesProductos;

    [Header("Visual de la Caja")]
    public Vector3 offsetVisualProducto = Vector3.up * 0.35f;
    public Vector3 rotacionVisualProducto;
    public Vector3 escalaVisualProducto = Vector3.one * 0.6f;
    public bool ocultarMeshCajaGenerica = true;

    [Header("Debug Spawn Pad Numerico")]
    public bool permitirSpawnConPad = true;
    public bool permitirSpawnRandomConPad0 = true;
    public bool ignorarBloqueoAlSpawnearConPad = false;

    [Header("Tiempos")]
    public float tiempoMinimo = 5f;
    public float tiempoMaximo = 15f;

    [Header("Tope de Altura (Anti-Spam)")]
    public float radioDeComprobacion = 1f;

    private float timer = 0f;
    private float tiempoObjetivo;
    private readonly List<VisualProductoActivo> visualesActivos = new List<VisualProductoActivo>();

    void Start()
    {
        AsignarNuevoTiempo();
    }

    void Update()
    {
        ProcesarSpawnConPad();

        timer += Time.deltaTime;

        if (timer >= tiempoObjetivo)
        {
            if (!AreaEstaBloqueada())
                SpawnearCaja();

            AsignarNuevoTiempo();
        }
    }

    bool AreaEstaBloqueada()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, radioDeComprobacion);
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Item"))
                return true;
        }
        return false;
    }

    void SpawnearCaja()
    {
        SpawnearCaja(ElegirProductoFaltante());
    }

    void SpawnearCaja(ProductoData productoElegido)
    {
        GameObject nuevaCaja = Instantiate(cajaPrefab, transform.position, transform.rotation);
        ProductBox scriptCaja = nuevaCaja.GetComponent<ProductBox>();

        if (scriptCaja == null)
        {
            Destroy(nuevaCaja);
            return;
        }

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

    public void ForzarSpawnCaja()
    {
        if (AreaEstaBloqueada())
            return;

        SpawnearCaja();
        AsignarNuevoTiempo();
    }

    public void ForzarSpawnCajaDebug()
    {
        if (!ignorarBloqueoAlSpawnearConPad && AreaEstaBloqueada())
            return;

        SpawnearCaja();
        AsignarNuevoTiempo();
    }

    public void ForzarSpawnCajaDebug(ProductoData productoForzado)
    {
        if (productoForzado == null)
            return;

        if (!ignorarBloqueoAlSpawnearConPad && AreaEstaBloqueada())
            return;

        SpawnearCaja(productoForzado);
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
                    productosFaltantes.Add(estante.productoRequerido);
            }
        }

        if (productosFaltantes.Count > 0)
            return productosFaltantes[Random.Range(0, productosFaltantes.Count)];

        List<ProductoData> productosValidos = new List<ProductoData>();
        foreach (ProductoData p in posiblesProductos)
        {
            if (p != null)
                productosValidos.Add(p);
        }

        if (productosValidos.Count > 0)
            return productosValidos[Random.Range(0, productosValidos.Count)];

        return null;
    }

    void ConfigurarVisualCaja(GameObject nuevaCaja, ProductoData productoElegido)
    {
        if (nuevaCaja == null || productoElegido == null || productoElegido.prefabIndividual == null)
            return;

        if (ocultarMeshCajaGenerica)
        {
            MeshRenderer[] renderersCaja = nuevaCaja.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer renderer in renderersCaja)
                renderer.enabled = false;
        }

        GameObject pivoteVisual = new GameObject(productoElegido.nombreProducto + " Pivot");
        pivoteVisual.transform.SetParent(nuevaCaja.transform, false);
        pivoteVisual.transform.localPosition = offsetVisualProducto;
        pivoteVisual.transform.localRotation = Quaternion.Euler(rotacionVisualProducto + productoElegido.rotacionEnCaja);

        GameObject visualProducto = Instantiate(productoElegido.prefabIndividual, pivoteVisual.transform);
        visualProducto.name = productoElegido.nombreProducto + " Visual";
        Vector3 escalaObjetivoProducto = productoElegido.ObtenerEscalaParaCajaYSpawn();
        visualProducto.transform.localScale = escalaObjetivoProducto;

        Renderer[] renderersVisual = visualProducto.GetComponentsInChildren<Renderer>();
        if (renderersVisual.Length > 0)
        {
            Bounds bounds = renderersVisual[0].bounds;
            for (int i = 1; i < renderersVisual.Length; i++)
                bounds.Encapsulate(renderersVisual[i].bounds);

            Vector3 centroLocal = pivoteVisual.transform.InverseTransformPoint(bounds.center);
            visualProducto.transform.localPosition -= centroLocal;
        }

        pivoteVisual.transform.localScale = escalaVisualProducto;

        visualesActivos.Add(new VisualProductoActivo
        {
            visual = pivoteVisual.transform,
            producto = productoElegido
        });

        Collider[] collidersVisual = pivoteVisual.GetComponentsInChildren<Collider>();
        foreach (Collider col in collidersVisual)
            col.enabled = false;

        Rigidbody[] rigidbodiesVisual = pivoteVisual.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rigidbodiesVisual)
            rb.isKinematic = true;
    }

    void AsignarNuevoTiempo()
    {
        tiempoObjetivo = Random.Range(tiempoMinimo, tiempoMaximo);
        timer = 0f;
    }

    void ProcesarSpawnConPad()
    {
        if (!permitirSpawnConPad)
            return;

        if (permitirSpawnRandomConPad0 && Input.GetKeyDown(KeyCode.Keypad0))
        {
            ForzarSpawnCajaDebug();
            Debug.Log("<color=cyan>RandomSpawner: Spawn random ejecutado con Keypad0.</color>");
            return;
        }

        int indiceProducto = ObtenerIndiceProductoDesdePad();
        if (indiceProducto < 0 || posiblesProductos == null || indiceProducto >= posiblesProductos.Length)
            return;

        ProductoData productoSeleccionado = posiblesProductos[indiceProducto];
        if (productoSeleccionado == null)
        {
            Debug.LogWarning($"RandomSpawner: El producto en posiblesProductos[{indiceProducto}] es null.");
            return;
        }

        ForzarSpawnCajaDebug(productoSeleccionado);
        Debug.Log($"<color=cyan>RandomSpawner: Spawn manual de '{productoSeleccionado.nombreProducto}' con Keypad{indiceProducto + 1}.</color>");
    }

    int ObtenerIndiceProductoDesdePad()
    {
        if (Input.GetKeyDown(KeyCode.Keypad1)) return 0;
        if (Input.GetKeyDown(KeyCode.Keypad2)) return 1;
        if (Input.GetKeyDown(KeyCode.Keypad3)) return 2;
        if (Input.GetKeyDown(KeyCode.Keypad4)) return 3;
        if (Input.GetKeyDown(KeyCode.Keypad5)) return 4;
        if (Input.GetKeyDown(KeyCode.Keypad6)) return 5;
        if (Input.GetKeyDown(KeyCode.Keypad7)) return 6;
        if (Input.GetKeyDown(KeyCode.Keypad8)) return 7;
        if (Input.GetKeyDown(KeyCode.Keypad9)) return 8;

        return -1;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radioDeComprobacion);
    }
}
