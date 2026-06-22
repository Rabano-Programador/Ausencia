using UnityEngine;
using UnityEngine.AI;

public class NPCCliente : MonoBehaviour
{
    [HideInInspector] public Transform zonaFilaCaja, mesaDeCobro, puntoSalida;
    [HideInInspector] public GameObject objetoCajaPrefab;

    private NavMeshAgent agent;
    private RestockShelf estanteObjetivo;
    private ProductoData productoTomado;

    private enum EstadoNPC { YendoAlEstante, YendoACaja, Saliendo }
    private EstadoNPC estadoActual = EstadoNPC.YendoAlEstante;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (!agent.isOnNavMesh)
        {
            Debug.Log("<color=red>ERROR CRÍTICO: El NPC no está tocando el NavMesh. Baja tu NPC_Spawner para que toque el suelo.</color>");
            return;
        }

        BuscarEstanteAleatorio();
    }

    void BuscarEstanteAleatorio()
    {
        RestockShelf[] estantes = FindObjectsByType<RestockShelf>(FindObjectsSortMode.None);
        if (estantes.Length > 0)
        {
            estanteObjetivo = estantes[Random.Range(0, estantes.Length)];
            agent.SetDestination(estanteObjetivo.transform.position);
            Debug.Log("<color=cyan>NPC: Voy a ir a la estantería: " + estanteObjetivo.gameObject.name + "</color>");
        }
        else
        {
            Debug.Log("<color=orange>NPC: No encontré NINGUNA estantería. Me voy.</color>");
            IrASalida();
        }
    }

    void Update()
    {
        if (agent.pathPending || !agent.isOnNavMesh) return;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            if (estadoActual == EstadoNPC.YendoAlEstante) TomarProductoEIrACaja();
            else if (estadoActual == EstadoNPC.YendoACaja) DejarProductoEIrASalida();
        }
    }

    void TomarProductoEIrACaja()
    {
        if (estanteObjetivo != null)
        {
            productoTomado = estanteObjetivo.TomarProductoNPC();
        }

        if (productoTomado != null && zonaFilaCaja != null)
        {
            estadoActual = EstadoNPC.YendoACaja;
            agent.SetDestination(zonaFilaCaja.position);
            Debug.Log("<color=cyan>NPC: Tomé mi producto. Ahora voy a la caja registradora.</color>");
        }
        else
        {
            if (zonaFilaCaja == null) Debug.Log("<color=red>NPC: No tengo asignada la fila de la caja.</color>");
            IrASalida();
        }
    }

    void DejarProductoEIrASalida()
    {
        if (objetoCajaPrefab != null && mesaDeCobro != null && productoTomado != null)
        {
            GameObject itemCaja = Instantiate(objetoCajaPrefab, mesaDeCobro.position, Quaternion.identity);
            ObjetoCaja scriptObjeto = itemCaja.GetComponent<ObjetoCaja>();
            if (scriptObjeto != null) scriptObjeto.datosProducto = productoTomado;
            Debug.Log("<color=cyan>NPC: Dejé el producto en la mesa. ¡Adiós!</color>");
        }
        IrASalida();
    }

    void IrASalida()
    {
        estadoActual = EstadoNPC.Saliendo;
        if (puntoSalida != null)
        {
            agent.SetDestination(puntoSalida.position);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Salida") && estadoActual == EstadoNPC.Saliendo)
        {
            Destroy(gameObject);
        }
    }
}