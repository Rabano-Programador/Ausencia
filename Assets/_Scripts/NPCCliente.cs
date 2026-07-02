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

    [Header("Debug Compra NPC")]
    public bool debugCompraNPC = true;

    [Header("Animacion")]
    public Animator animator;
    public float velocidadMinimaCaminar = 0.1f;

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
    private RestockShelf estanteObjetivoActual;


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Destroy(gameObject);
            return;
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (!agent.isOnNavMesh && !IntentarSnapAlNavMesh(transform.position, 3f))
        {
            Destroy(gameObject);
            return;
        }

        productosObjetivo = Random.Range(1, maxProductosARecoger + 1);
        tiempoInicio = Time.time;
        lastPosition = transform.position;

        LogCompra($"Spawneado. Objetivo de compra: {productosObjetivo} producto(s).");
        BuscarSiguienteEstante();
    }

    void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        ActualizarAnimacion();

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

    void ActualizarAnimacion()
    {
        if (animator == null) return;

        bool caminando = agent.velocity.sqrMagnitude > velocidadMinimaCaminar * velocidadMinimaCaminar;
        animator.SetBool("isWalking", caminando);
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
            LogCompra($"Inventario completo ({productosRecogidos.Count}/{productosObjetivo}). Voy a la caja.");
            UnirseACola();
            return;
        }

        RestockShelf objetivo = ElegirEstante();

        if (objetivo == null)
        {
            if (productosRecogidos.Count > 0)
            {
                LogCompra("No encontre mas estantes con stock. Voy a caja con lo que tengo.");
                UnirseACola();
            }
            else
            {
                esperandoPrimerProducto = true;
                LogCompra("No encontre estantes con stock. Me quedo vagando hasta que repongan productos.");
                ComenzarVagabundeo();
            }
            return;
        }

        LogCompra($"Estante elegido: '{objetivo.name}' con '{objetivo.ObtenerNombreProducto()}' y stock {objetivo.stockActual}.");
        esperandoPrimerProducto = false;
        estanteObjetivoActual = objetivo;
        estadoActual = EstadoNPC.BuscandoEstante;
        ResetearTraba();

        if (!IntentarMoverAEstante(objetivo))
        {
            LogCompra($"No pude crear ruta hacia '{objetivo.name}'. Probare otro estante.", true);

            estantesVisitados.Add(objetivo);
            estanteObjetivoActual = null;
            BuscarSiguienteEstante();
            return;
        }

        estantesVisitados.Add(objetivo);
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

    bool IntentarMoverAEstante(RestockShelf estante)
    {
        if (estante == null)
            return false;

        List<Vector3> candidatos = ObtenerPuntosAccesoEstante(estante);
        for (int i = 0; i < candidatos.Count; i++)
        {
            Vector3 candidato = candidatos[i];
            if (IntentarMoverA(candidato, 3f))
            {
                LogCompra($"Ruta creada hacia '{estante.name}' usando candidato {i}. Producto: '{estante.ObtenerNombreProducto()}'.");
                return true;
            }
        }

        return false;
    }

    RestockShelf EncontrarEstanteCercano()
    {
        if (estanteObjetivoActual != null && DistanciaAEstante(estanteObjetivoActual) <= 4f)
            return estanteObjetivoActual;

        RestockShelf[] todos = FindObjectsByType<RestockShelf>(FindObjectsSortMode.None);
        RestockShelf masCercano = null;
        float menorDistancia = 4f;

        foreach (RestockShelf estante in todos)
        {
            float dist = DistanciaAEstante(estante);
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
                LogCompra($"Tome '{producto.nombreProducto}' desde '{estanteCercano.name}'. Inventario: {productosRecogidos.Count}/{productosObjetivo}.");
            }
            else
            {
                LogCompra($"Llegue a '{estanteCercano.name}', pero no pude tomar producto. Stock actual: {estanteCercano.stockActual}.", true);
            }

            estanteObjetivoActual = null;
            BuscarSiguienteEstante();
        }
        else
        {
            LogCompra("Llegue al destino, pero no encontre estante cercano para tomar producto.", true);
            estanteObjetivoActual = null;
            BuscarSiguienteEstante();
        }
    }

    Vector3 ObtenerPuntoAccesoEstante(RestockShelf estante)
    {
        List<Vector3> candidatos = ObtenerPuntosAccesoEstante(estante);
        return candidatos.Count > 0 ? candidatos[0] : transform.position;
    }

    List<Vector3> ObtenerPuntosAccesoEstante(RestockShelf estante)
    {
        List<Vector3> candidatos = new List<Vector3>();

        if (estante == null)
            return candidatos;

        if (estante.ObtenerPuntoProductoDisponible(out Vector3 puntoProducto))
        {
            Vector3 direccionDesdeProducto = transform.position - puntoProducto;
            direccionDesdeProducto.y = 0f;

            if (direccionDesdeProducto.sqrMagnitude < 0.01f)
                direccionDesdeProducto = -estante.transform.forward;

            Vector3 haciaNPC = direccionDesdeProducto.normalized;
            Vector3 forward = estante.transform.forward;
            Vector3 right = estante.transform.right;

            candidatos.Add(puntoProducto + haciaNPC * 1.2f);
            candidatos.Add(puntoProducto + haciaNPC * 2.2f);
            candidatos.Add(puntoProducto - forward * 1.4f);
            candidatos.Add(puntoProducto + forward * 1.4f);
            candidatos.Add(puntoProducto + right * 1.4f);
            candidatos.Add(puntoProducto - right * 1.4f);

            return candidatos;
        }

        if (TryObtenerCentroPuntosEstante(estante, out Vector3 centroPuntos))
        {
            Vector3 direccionDesdePuntos = transform.position - centroPuntos;
            direccionDesdePuntos.y = 0f;

            if (direccionDesdePuntos.sqrMagnitude < 0.01f)
                direccionDesdePuntos = -estante.transform.forward;

            Vector3 haciaNPC = direccionDesdePuntos.normalized;
            Vector3 forward = estante.transform.forward;
            Vector3 right = estante.transform.right;

            candidatos.Add(centroPuntos + haciaNPC * 1.4f);
            candidatos.Add(centroPuntos + haciaNPC * 2.4f);
            candidatos.Add(centroPuntos - forward * 1.6f);
            candidatos.Add(centroPuntos + forward * 1.6f);
            candidatos.Add(centroPuntos + right * 1.6f);
            candidatos.Add(centroPuntos - right * 1.6f);

            return candidatos;
        }

        Collider[] colliders = estante.GetComponentsInChildren<Collider>();
        if (colliders.Length == 0)
        {
            candidatos.Add(estante.transform.position);
            return candidatos;
        }

        Bounds bounds = colliders[0].bounds;
        for (int i = 1; i < colliders.Length; i++)
        {
            if (colliders[i] != null && colliders[i].enabled)
                bounds.Encapsulate(colliders[i].bounds);
        }

        Vector3 desdeNPC = transform.position - bounds.center;
        desdeNPC.y = 0f;
        if (desdeNPC.sqrMagnitude < 0.01f)
            desdeNPC = -estante.transform.forward;

        candidatos.Add(bounds.center + desdeNPC.normalized * 1.4f);
        candidatos.Add(bounds.center + desdeNPC.normalized * 2.4f);
        return candidatos;
    }

    float DistanciaAEstante(RestockShelf estante)
    {
        if (estante == null)
            return float.MaxValue;

        if (estante.ObtenerPuntoProductoDisponible(out Vector3 puntoProducto))
        {
            Vector3 posicionNPC = transform.position;
            posicionNPC.y = 0f;
            puntoProducto.y = 0f;
            return Vector3.Distance(posicionNPC, puntoProducto);
        }

        if (TryObtenerDistanciaAPuntosEstante(estante, out float distanciaPuntos))
            return distanciaPuntos;

        Collider[] colliders = estante.GetComponentsInChildren<Collider>();
        if (colliders.Length == 0)
            return Vector3.Distance(transform.position, estante.transform.position);

        float menorDistancia = float.MaxValue;
        foreach (Collider col in colliders)
        {
            if (col == null || !col.enabled)
                continue;

            Vector3 puntoCercano = col.ClosestPoint(transform.position);
            float distancia = Vector3.Distance(transform.position, puntoCercano);
            if (distancia < menorDistancia)
                menorDistancia = distancia;
        }

        return menorDistancia;
    }

    bool TryObtenerCentroPuntosEstante(RestockShelf estante, out Vector3 centro)
    {
        centro = Vector3.zero;

        if (estante == null || estante.puntosDeColocacion == null || estante.puntosDeColocacion.Length == 0)
            return false;

        int cantidad = 0;
        foreach (Transform punto in estante.puntosDeColocacion)
        {
            if (punto == null)
                continue;

            centro += punto.position;
            cantidad++;
        }

        if (cantidad == 0)
            return false;

        centro /= cantidad;
        return true;
    }

    bool TryObtenerDistanciaAPuntosEstante(RestockShelf estante, out float distancia)
    {
        distancia = float.MaxValue;

        if (estante == null || estante.puntosDeColocacion == null || estante.puntosDeColocacion.Length == 0)
            return false;

        bool encontroPunto = false;
        foreach (Transform punto in estante.puntosDeColocacion)
        {
            if (punto == null)
                continue;

            Vector3 posicionNPC = transform.position;
            Vector3 posicionPunto = punto.position;
            posicionNPC.y = 0f;
            posicionPunto.y = 0f;

            float distanciaActual = Vector3.Distance(posicionNPC, posicionPunto);
            if (distanciaActual < distancia)
                distancia = distanciaActual;

            encontroPunto = true;
        }

        return encontroPunto;
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
            LogCompra("Me mandaron a cola sin productos. Me voy a salida.");
            IrASalida();
            return;
        }

        if (QueueManager.Instance == null)
        {
            LogCompra("No existe QueueManager. Uso caja legacy.");
            IrACajaLegacy();
            return;
        }

        estadoActual = EstadoNPC.EsperandoEnCola;
        Vector3 posicionEspera = QueueManager.Instance.UnirseACola(this);

        LogCompra($"Me uni a la cola con {productosRecogidos.Count} producto(s). Voy a posicion {posicionEspera}.");
        IntentarMoverA(posicionEspera, 5f, true);
    }

    public void RecibirTurnoEnCaja(Vector3 posicionMesa)
    {
        estadoActual = EstadoNPC.SiendoAtendido;
        productosEntregadosEnCaja = false;
        tiempoEsperandoEnCaja = 0f;

        LogCompra($"Es mi turno en caja. Voy al punto de cobro {posicionMesa}.");
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
            LogCompra($"Voy a la salida '{puntoSalida.name}'.");
            IntentarMoverA(puntoSalida.position, 5f, true);
        }
        else
        {
            LogCompra("No tengo puntoSalida asignado. Me destruyo en 3 segundos.", true);
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
            LogCompra($"Voy a caja legacy '{zonaFilaCaja.name}'.");
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
        if (puntoDespachoProductos == null)
        {
            GameObject puntoDespachoEncontrado = GameObject.Find("PuntoDespacho");
            if (puntoDespachoEncontrado != null)
                puntoDespachoProductos = puntoDespachoEncontrado.transform;
        }

        if (puntoDespachoProductos == null)
        {
            Debug.LogWarning("<color=orange>NPC: No encontré PuntoDespacho para dejar productos.</color>");
            return;
        }

        bool dejoAlMenosUnProducto = false;

        for (int i = 0; i < productosRecogidos.Count; i++)
        {
            ProductoData producto = productosRecogidos[i];
            GameObject prefabAInstanciar = producto != null && producto.prefabIndividual != null
                ? producto.prefabIndividual
                : objetoCajaPrefab;

            if (prefabAInstanciar == null)
            {
                LogCompra($"No puedo dejar producto {i}: prefab nulo para '{(producto != null ? producto.nombreProducto : "Producto null")}'.", true);
                continue;
            }

            Vector3 offset = new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.2f, 0.2f));
            GameObject itemCaja = Instantiate(prefabAInstanciar, puntoDespachoProductos.position + offset, Quaternion.identity);
            itemCaja.name = producto != null ? producto.nombreProducto + " Cobro" : itemCaja.name;
            itemCaja.tag = "Item";
            dejoAlMenosUnProducto = true;

            Rigidbody rbItem = itemCaja.GetComponent<Rigidbody>();
            if (rbItem == null)
                rbItem = itemCaja.AddComponent<Rigidbody>();

            rbItem.isKinematic = true;
            rbItem.useGravity = false;

            ObjetoCaja scriptObjeto = itemCaja.GetComponent<ObjetoCaja>();
            if (scriptObjeto == null)
                scriptObjeto = itemCaja.AddComponent<ObjetoCaja>();

            scriptObjeto.ConfigurarProducto(producto);
            LogCompra($"Deje '{(producto != null ? producto.nombreProducto : "Producto null")}' en PuntoDespacho.");

            if (i < productosEnInventarioVisual.Count && productosEnInventarioVisual[i] != null)
                Destroy(productosEnInventarioVisual[i]);
        }

        if (dejoAlMenosUnProducto)
        {
            productosEntregadosEnCaja = true;
            productosEnInventarioVisual.Clear();
            Debug.Log($"<color=cyan>NPC: Dejé {productosRecogidos.Count} producto(s) en PuntoDespacho.</color>");
        }
        else
        {
            Debug.LogWarning("<color=orange>NPC: Intenté dejar productos, pero no había prefabs válidos para instanciar.</color>");
        }
    }

    public void RecogerProductoPagado(ProductoData producto)
    {
        if (producto == null)
            return;

        AgregarVisualAlInventario(producto);
        LogCompra($"Recupere '{producto.nombreProducto}' despues del cobro y me voy con el.");
    }

    void LogCompra(string mensaje, bool advertencia = false)
    {
#if !UNITY_EDITOR
        if (!debugCompraNPC)
            return;
#endif

        string texto = $"NPC '{name}': {mensaje}";

        if (advertencia)
            Debug.LogWarning($"<color=orange>{texto}</color>");
        else
            Debug.Log($"<color=cyan>{texto}</color>");
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
