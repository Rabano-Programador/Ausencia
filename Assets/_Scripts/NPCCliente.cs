using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class NPCCliente : MonoBehaviour
{
    [HideInInspector] public Transform zonaFilaCaja, puntoSalida;
    [HideInInspector, FormerlySerializedAs("mesaDeCobro")] public Transform puntoDespachoProductos;
    [HideInInspector] public GameObject objetoCajaPrefab;

    [Header("Comportamiento de compra")]
    [Tooltip("Cantidad máxima de productos que puede recoger. Máximo 4.")]
    [Range(1, 4)]
    public int maxProductosARecoger = 4;

    [Header("Vagabundeo")]
    [Tooltip("Tiempo máximo que el NPC puede pasar en tienda sin productos antes de irse.")]
    public float tiempoMaximoSinProductos = 60f;
    [Tooltip("Radio para elegir puntos aleatorios mientras vaga por la tienda.")]
    public float radioVagabundeo = 8f;
    [Tooltip("Intentos para encontrar un punto válido de NavMesh mientras vaga.")]
    public int intentosVagabundeo = 8;

    [Header("Inventario NPC")]
    public Transform puntoInventarioVisual;
    public Vector3 offsetInventarioInicial = new Vector3(0f, 1.2f, -0.35f);
    public Vector3 separacionInventario = new Vector3(0f, 0.18f, 0f);
    public Vector3 rotacionInventario;
    public Vector3 escalaInventario = Vector3.one * 0.4f;

    private NavMeshAgent agent;
    private List<ProductoData> productosRecogidos = new List<ProductoData>();
    private int productosObjetivo;
    private float tiempoInicio;
    private bool esperandoPrimerProducto;
    private bool productosEntregadosEnCaja;
    private float tiempoEsperandoEnCaja;
    private readonly List<GameObject> productosEnInventarioVisual = new List<GameObject>();

    private enum EstadoNPC
    {
        BuscandoEstante,
        Vagando,
        EsperandoEnCola,
        SiendoAtendido,
        Saliendo
    }
    private EstadoNPC estadoActual = EstadoNPC.BuscandoEstante;

    private HashSet<RestockShelf> estantesVisitados = new HashSet<RestockShelf>();

    [Header("Anti-traba")]
    [Tooltip("Segundos sin moverse antes de considerar al NPC trabado")]
    public float stuckTimeout = 4f;
    [Tooltip("Distancia mínima para no considerarse trabado")]
    public float stuckDistancia = 0.15f;

    private float stuckTimer = 0f;
    private Vector3 lastPosition;


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Destroy(gameObject);
            return;
        }

        if (!agent.isOnNavMesh && !IntentarSnapAlNavMesh(transform.position, 3f))
        {
            Destroy(gameObject);
            return;
        }

        productosObjetivo = Random.Range(1, maxProductosARecoger + 1);
        tiempoInicio = Time.time;
        lastPosition = transform.position;

        BuscarSiguienteEstante();
    }

    void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        switch (estadoActual)
        {
            case EstadoNPC.BuscandoEstante:
                ChequearTraba();
                if (!agent.pathPending && HaLlegado())
                    TomarProductoDelEstante();
                break;

            case EstadoNPC.Vagando:
                ChequearTraba();

                if (!esperandoPrimerProducto && productosRecogidos.Count == 0 && Time.time - tiempoInicio >= tiempoMaximoSinProductos)
                {
                    IrASalida();
                    break;
                }

                if (!agent.pathPending && HaLlegado())
                {
                    EvaluarSiguienteAccionVagando();
                }
                break;

            case EstadoNPC.EsperandoEnCola:
                break;

            case EstadoNPC.SiendoAtendido:
                if (!agent.pathPending && HaLlegado() && !productosEntregadosEnCaja)
                {
                    tiempoEsperandoEnCaja += Time.deltaTime;

                    if (tiempoEsperandoEnCaja >= 2f)
                        DejarProductosEnCaja();
                }
                else
                {
                    tiempoEsperandoEnCaja = 0f;
                }
                break;

            case EstadoNPC.Saliendo:
                break;
        }
    }


    void ChequearTraba()
    {
        float dist = Vector3.Distance(transform.position, lastPosition);

        if (dist > stuckDistancia)
        {
            stuckTimer = 0f;
            lastPosition = transform.position;
        }
        else
        {
            stuckTimer += Time.deltaTime;

            if (stuckTimer >= stuckTimeout)
            {
                stuckTimer = 0f;
                lastPosition = transform.position;
                BuscarSiguienteEstante();
            }
        }
    }

    void ResetearTraba()
    {
        stuckTimer = 0f;
        lastPosition = transform.position;
    }

    bool IntentarSnapAlNavMesh(Vector3 origen, float radio)
    {
        NavMeshHit hit;
        if (!NavMesh.SamplePosition(origen, out hit, radio, NavMesh.AllAreas))
            return false;

        bool warpExitoso = agent.Warp(hit.position);
        if (warpExitoso)
            lastPosition = hit.position;

        return warpExitoso;
    }

    bool IntentarMoverA(Vector3 destino, float radioMuestreo = 5f, bool permitirParcial = false)
    {
        if (agent == null || !agent.isOnNavMesh)
            return false;

        NavMeshHit hit;
        Vector3 destinoFinal = destino;
        if (NavMesh.SamplePosition(destino, out hit, radioMuestreo, NavMesh.AllAreas))
            destinoFinal = hit.position;

        NavMeshPath path = new NavMeshPath();
        if (!agent.CalculatePath(destinoFinal, path))
            return false;

        bool pathValido = path.status == NavMeshPathStatus.PathComplete ||
            (permitirParcial && path.status == NavMeshPathStatus.PathPartial);

        if (!pathValido)
            return false;

        return agent.SetPath(path);
    }

    void BuscarSiguienteEstante()
    {
        if (productosRecogidos.Count >= productosObjetivo)
        {
            UnirseACola();
            return;
        }

        RestockShelf objetivo = ElegirEstante();

        if (objetivo == null)
        {
            if (productosRecogidos.Count > 0)
            {
                UnirseACola();
            }
            else
            {
                esperandoPrimerProducto = true;
                ComenzarVagabundeo();
            }
            return;
        }

        esperandoPrimerProducto = false;
        estantesVisitados.Add(objetivo);
        estadoActual = EstadoNPC.BuscandoEstante;
        ResetearTraba();

        Vector3 destino = objetivo.transform.position;
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(destino, out navHit, 5f, NavMesh.AllAreas))
        {
            destino = navHit.position;
        }
        else
        {
            BuscarSiguienteEstante();
            return;
        }

        if (!IntentarMoverA(destino))
        {
            BuscarSiguienteEstante();
            return;
        }
    }

    RestockShelf ElegirEstante()
    {
        RestockShelf[] todosLosEstantes = FindObjectsByType<RestockShelf>(FindObjectsSortMode.None);
        List<RestockShelf> disponibles = new List<RestockShelf>();

        foreach (RestockShelf estante in todosLosEstantes)
        {
            if (!estantesVisitados.Contains(estante) && estante.stockActual > 0)
                disponibles.Add(estante);
        }

        if (disponibles.Count == 0) return null;
        return disponibles[Random.Range(0, disponibles.Count)];
    }

    RestockShelf EncontrarEstanteCercano()
    {
        RestockShelf[] todos = FindObjectsByType<RestockShelf>(FindObjectsSortMode.None);
        RestockShelf masCercano = null;
        float menorDistancia = 3f;

        foreach (RestockShelf estante in todos)
        {
            float dist = Vector3.Distance(transform.position, estante.transform.position);
            if (dist < menorDistancia)
            {
                menorDistancia = dist;
                masCercano = estante;
            }
        }

        return masCercano;
    }

    void TomarProductoDelEstante()
    {
        RestockShelf estanteCercano = EncontrarEstanteCercano();

        if (estanteCercano != null)
        {
            ProductoData producto = estanteCercano.TomarProductoNPC();
            if (producto != null)
            {
                productosRecogidos.Add(producto);
                AgregarVisualAlInventario(producto);
            }

            BuscarSiguienteEstante();
        }
    }

    void ComenzarVagabundeo()
    {
        estadoActual = EstadoNPC.Vagando;
        ResetearTraba();

        if (!MoverAUnPuntoAleatorio())
        {
            BuscarSiguienteEstante();
        }
    }

    void EvaluarSiguienteAccionVagando()
    {
        if (productosRecogidos.Count >= productosObjetivo)
        {
            UnirseACola();
            return;
        }

        RestockShelf objetivo = ElegirEstante();
        if (objetivo != null)
        {
            if (esperandoPrimerProducto)
            {
                productosObjetivo = 1;
            }

            BuscarSiguienteEstante();
            return;
        }

        MoverAUnPuntoAleatorio();
    }

    bool MoverAUnPuntoAleatorio()
    {
        for (int i = 0; i < intentosVagabundeo; i++)
        {
            Vector2 circulo = Random.insideUnitCircle * radioVagabundeo;
            Vector3 candidato = transform.position + new Vector3(circulo.x, 0f, circulo.y);

            NavMeshHit hit;
            if (!NavMesh.SamplePosition(candidato, out hit, radioVagabundeo, NavMesh.AllAreas))
                continue;

            if (IntentarMoverA(hit.position, 2f, true))
            {
                return true;
            }
        }

        return false;
    }

    void UnirseACola()
    {
        if (productosRecogidos.Count == 0)
        {
            IrASalida();
            return;
        }

        if (QueueManager.Instance == null)
        {
            IrACajaLegacy();
            return;
        }

        estadoActual = EstadoNPC.EsperandoEnCola;
        Vector3 posicionEspera = QueueManager.Instance.UnirseACola(this);

        IntentarMoverA(posicionEspera, 5f, true);
    }

    public void RecibirTurnoEnCaja(Vector3 posicionMesa)
    {
        estadoActual = EstadoNPC.SiendoAtendido;
        productosEntregadosEnCaja = false;
        tiempoEsperandoEnCaja = 0f;

        IntentarMoverA(posicionMesa, 5f, true);
    }

    public void RecibirPermisoDeSalir()
    {
        if (QueueManager.Instance != null)
            QueueManager.Instance.NotificarAtendido(this);
        IrASalida();
    }

    public void ActualizarPosicionEnCola(Vector3 nuevaPosicion)
    {
        if (estadoActual == EstadoNPC.EsperandoEnCola)
            IntentarMoverA(nuevaPosicion, 5f, true);
    }

    void IrASalida()
    {
        estadoActual = EstadoNPC.Saliendo;
        if (puntoSalida != null)
        {
            IntentarMoverA(puntoSalida.position, 5f, true);
        }
        else
        {
            Destroy(gameObject, 3f);
        }
    }

    void IrACajaLegacy()
    {
        if (zonaFilaCaja != null)
        {
            estadoActual = EstadoNPC.SiendoAtendido;
            productosEntregadosEnCaja = false;
            tiempoEsperandoEnCaja = 0f;
            IntentarMoverA(zonaFilaCaja.position, 5f, true);
        }
        else
        {
            IrASalida();
        }
    }

    bool HaLlegado()
    {
        if (agent.pathPending) return false;
        if (!agent.hasPath) return true;
        return agent.remainingDistance <= agent.stoppingDistance + 0.15f;
    }

    void AgregarVisualAlInventario(ProductoData producto)
    {
        if (producto == null || producto.prefabIndividual == null)
            return;

        Transform padre = puntoInventarioVisual != null ? puntoInventarioVisual : transform;
        GameObject visual = Instantiate(producto.prefabIndividual, padre);
        visual.name = producto.nombreProducto + " Inventario";
        visual.transform.localPosition = offsetInventarioInicial + separacionInventario * productosEnInventarioVisual.Count;
        visual.transform.localRotation = Quaternion.Euler(rotacionInventario);
        visual.transform.localScale = escalaInventario;

        foreach (Collider col in visual.GetComponentsInChildren<Collider>())
            col.enabled = false;

        foreach (Rigidbody rb in visual.GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = true;

        productosEnInventarioVisual.Add(visual);
    }

    void DejarProductosEnCaja()
    {
        productosEntregadosEnCaja = true;

        if (puntoDespachoProductos == null)
        {
            return;
        }

        for (int i = 0; i < productosRecogidos.Count; i++)
        {
            ProductoData producto = productosRecogidos[i];
            GameObject prefabAInstanciar = producto != null && producto.prefabIndividual != null
                ? producto.prefabIndividual
                : objetoCajaPrefab;

            if (prefabAInstanciar == null)
            {
                continue;
            }

            Vector3 offset = new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.2f, 0.2f));
            GameObject itemCaja = Instantiate(prefabAInstanciar, puntoDespachoProductos.position + offset, Quaternion.identity);
            itemCaja.name = producto != null ? producto.nombreProducto + " Cobro" : itemCaja.name;
            itemCaja.tag = "Item";

            ObjetoCaja scriptObjeto = itemCaja.GetComponent<ObjetoCaja>();
            if (scriptObjeto == null)
                scriptObjeto = itemCaja.AddComponent<ObjetoCaja>();

            scriptObjeto.ConfigurarProducto(producto);

            if (i < productosEnInventarioVisual.Count && productosEnInventarioVisual[i] != null)
                Destroy(productosEnInventarioVisual[i]);
        }

        productosEnInventarioVisual.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Salida") && estadoActual == EstadoNPC.Saliendo)
        {
            if (QueueManager.Instance != null)
                QueueManager.Instance.SalirDeCola(this);
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (QueueManager.Instance != null)
            QueueManager.Instance.SalirDeCola(this);
    }
}