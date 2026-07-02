using UnityEngine;
using System.Collections.Generic;

public class PuntoEntregaTrigger : MonoBehaviour
{
    public bool debugPuntoEntrega = true;
    private readonly List<ObjetoCaja> objetosEnEntrega = new List<ObjetoCaja>();

    private void OnTriggerEnter(Collider other)
    {
        ObjetoCaja objetoCaja = other.GetComponentInParent<ObjetoCaja>();
        if (objetoCaja == null)
            return;

        RegistrarObjeto(objetoCaja);
    }

    private void OnTriggerExit(Collider other)
    {
        ObjetoCaja objetoCaja = other.GetComponentInParent<ObjetoCaja>();
        if (objetoCaja == null)
            return;

        QuitarObjeto(objetoCaja);
    }

    public void RegistrarObjeto(ObjetoCaja objetoCaja)
    {
        if (objetoCaja == null)
            return;

        if (!objetosEnEntrega.Contains(objetoCaja))
        {
            objetosEnEntrega.Add(objetoCaja);
            LogDebug($"Objeto registrado: '{objetoCaja.name}' ({(objetoCaja.datosProducto != null ? objetoCaja.datosProducto.nombreProducto : "sin ProductoData")}). Total en entrega: {objetosEnEntrega.Count}.");
        }
    }

    public void QuitarObjeto(ObjetoCaja objetoCaja)
    {
        if (objetoCaja == null)
            return;

        if (objetosEnEntrega.Remove(objetoCaja))
            LogDebug($"Objeto salio del punto entrega: '{objetoCaja.name}'. Total en entrega: {objetosEnEntrega.Count}.");
    }

    public int EntregarObjetosAlNPC(NPCCliente npc)
    {
        if (npc == null)
            return 0;

        objetosEnEntrega.RemoveAll(obj => obj == null);

        int entregados = 0;
        for (int i = objetosEnEntrega.Count - 1; i >= 0; i--)
        {
            ObjetoCaja objeto = objetosEnEntrega[i];
            if (objeto == null)
                continue;

            npc.RecogerProductoPagado(objeto.datosProducto);
            Destroy(objeto.gameObject);
            objetosEnEntrega.RemoveAt(i);
            entregados++;
        }

        Debug.Log($"<color=cyan>PuntoEntregaTrigger: Entregué {entregados} objeto(s) al NPC '{npc.name}'.</color>");
        return entregados;
    }
    void LogDebug(string mensaje)
    {
        if (!debugPuntoEntrega)
            return;

        Debug.Log($"<color=#B8F7FF>PuntoEntregaTrigger '{name}': {mensaje}</color>");
    }
}
