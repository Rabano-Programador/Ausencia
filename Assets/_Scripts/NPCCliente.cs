using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class NPCCliente : MonoBehaviour
{
    [HideInInspector] public Transform zonaFilaCaja, mesaDeCobro, puntoSalida;
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

    private NavMeshAgent agent;
    private List<ProductoData> productosRecogidos = new List<ProductoData>();
    private int productosObjetivo;
    private float tiempoInicio;

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
            Debug.LogError("<color=red>NPC ERROR: Falta el componente NavMeshAgent.</color>");
            Destroy(gameObject);
            return;
        }

        if (!agent.isOnNavMesh && !IntentarSnapAlNavMesh(transform.position, 3f))
        {
            Debug.LogError("<color=red>NPC ERROR: No está en el NavMesh y no se pudo corregir su posición al iniciar.</color>");
            Destroy(gameObject);
            return;
        }

        productosObjetivo = Random.Range(1, maxProductosARecoger + 1);
        tiempoInicio = Time.time;
        lastPosition = transform.position;
        Debug.Log($"<color=cyan>NPC: Voy a buscar {productosObjetivo} producto(s).</color>");

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

                if (productosRecogidos.Count == 0 && Time.time - tiempoInicio >= tiempoMaximoSinProductos)
                {
                    Debug.Log("<color=orange>NPC: Llevo demasiado tiempo sin encontrar productos. Me voy.</color>");
                    IrASalida();
                    break;
                }

                if (!agent.pathPending && HaLlegado())
                    EvaluarSiguienteAccionVagando();
                break;

            case EstadoNPC.EsperandoEnCola:
                
                break;

            case EstadoNPC.SiendoAtendido:
                
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
                Debug.LogWarning("<color=orange>NPC TRABADO: Sin movimiento por " + stuckTimeout + "s. Saltando al siguiente estante.</color>");
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
                Debug.Log("<color=orange>NPC: No encontré más estantes disponibles. Voy a la caja.</color>");
                UnirseACola();
            }
            else
            {
                Debug.Log("<color=orange>NPC: No encontré estantes disponibles. Voy a vagar por la tienda.</color>");
                ComenzarVagabundeo();
            }
            return;
        }

        estantesVisitados.Add(objetivo);
        estadoActual = EstadoNPC.BuscandoEstante;
        ResetearTraba();

        Vector3 destino = objetivo.transform.position;
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(destino, out navHit, 5f, NavMesh.AllAreas))
        {
            destino = navHit.position;
            Debug.Log($"<color=cyan>NPC: Destino sampledNavMesh={destino}. Yendo a '{objetivo.gameObject.name}'.</color>");
        }
        else
        {
            Debug.LogWarning($"<color=orange>NPC: No hay NavMesh cerca de '{objetivo.gameObject.name}'. Descartando.</color>");
            BuscarSiguienteEstante();
            return;
        }

        if (!IntentarMoverA(destino))
        {
            Debug.LogWarning($"<color=orange>NPC: No se pudo calcular un path válido a '{objetivo.gameObject.name}'. Descartando.</color>");
            BuscarSiguienteEstante();
            return;
        }
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
                Debug.Log($"<color=cyan>NPC: Tomé '{producto.nombreProducto}'. Total: {productosRecogidos.Count}/{productosObjetivo}.</color>");
            }
            else
            {
                Debug.Log("<color=orange>NPC: El estante estaba vacío.</color>");
            }
        }
        else
        {
            Debug.Log("<color=orange>NPC: Llegué pero no hay estante en el radio de 3u.</color>");
        }

        BuscarSiguienteEstante();
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

    void ComenzarVagabundeo()
    {
        estadoActual = EstadoNPC.Vagando;
        ResetearTraba();

        if (!MoverAUnPuntoAleatorio())
        {
            Debug.LogWarning("<color=orange>NPC: No encontré un punto válido para vagar. Reintentando búsqueda de estantes.</color>");
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
            BuscarSiguienteEstante();
            return;
        }

        if (!MoverAUnPuntoAleatorio())
        {
            Debug.LogWarning("<color=orange>NPC: No encontré otro punto válido para seguir vagando.</color>");
        }
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
                Debug.Log($"<color=cyan>NPC: Vagando hacia {hit.position}.</color>");
                return true;
            }
        }

        return false;
    }

  

    void UnirseACola()
    {
        if (productosRecogidos.Count == 0)
        {
            Debug.Log("<color=orange>NPC: Sin productos. Me voy.</color>");
            IrASalida();
            return;
        }

        if (QueueManager.Instance == null)
        {
            Debug.LogWarning("<color=red>NPC: No hay QueueManager. Modo legacy.</color>");
            IrACajaLegacy();
            return;
        }

        estadoActual = EstadoNPC.EsperandoEnCola;
        Vector3 posicionEspera = QueueManager.Instance.UnirseACola(this);

        if (!IntentarMoverA(posicionEspera, 5f, true))
            Debug.LogWarning("<color=orange>NPC: No pude encontrar un path válido hacia la cola.</color>");

        Debug.Log("<color=cyan>NPC: Me uno a la cola.</color>");
    }

    public void RecibirTurnoEnCaja(Vector3 posicionMesa)
    {
        estadoActual = EstadoNPC.SiendoAtendido;

        if (!IntentarMoverA(posicionMesa, 5f, true))
            Debug.LogWarning("<color=orange>NPC: No pude moverme correctamente al punto de cobro.</color>");

        if (mesaDeCobro != null)
        {
            foreach (ProductoData producto in productosRecogidos)
            {
                if (objetoCajaPrefab != null)
                {
                    Vector3 offset = new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.2f, 0.2f));
                    GameObject itemCaja = Instantiate(objetoCajaPrefab, mesaDeCobro.position + offset, Quaternion.identity);
                    ObjetoCaja scriptObjeto = itemCaja.GetComponent<ObjetoCaja>();
                    if (scriptObjeto != null) scriptObjeto.datosProducto = producto;
                }
            }
            Debug.Log($"<color=cyan>NPC: Turno en caja. {productosRecogidos.Count} producto(s) en mesa.</color>");
        }
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
            if (!IntentarMoverA(puntoSalida.position, 5f, true))
                Debug.LogWarning("<color=orange>NPC: No pude encontrar un path válido hacia la salida.</color>");
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
            IntentarMoverA(zonaFilaCaja.position, 5f, true);

            if (mesaDeCobro != null && objetoCajaPrefab != null)
            {
                foreach (ProductoData producto in productosRecogidos)
                {
                    GameObject itemCaja = Instantiate(objetoCajaPrefab, mesaDeCobro.position, Quaternion.identity);
                    ObjetoCaja script = itemCaja.GetComponent<ObjetoCaja>();
                    if (script != null) script.datosProducto = producto;
                }
            }
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
