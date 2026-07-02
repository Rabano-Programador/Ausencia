using UnityEngine;

public class ProductBox : MonoBehaviour
{
    [Header("Datos del Producto")]
    public ProductoData datosProducto;
    public int unidadesRestantes = 6;

    [Header("Visual al cargar")]
    public bool ocultarVisualMientrasSeCarga = true;
    private readonly System.Collections.Generic.Dictionary<Renderer, bool> estadosRenderersOriginales = new System.Collections.Generic.Dictionary<Renderer, bool>();

    public bool PillarProducto()
    {
        if (unidadesRestantes > 0)
        {
            unidadesRestantes--;
            if (unidadesRestantes <= 0)
            {
                Destroy(gameObject);
            }
            return true;
        }
        return false;
    }

    public void OcultarVisualAlCargar()
    {
        if (!ocultarVisualMientrasSeCarga)
            return;

        GuardarEstadoRenderersActuales();
        SetRenderersActivos(false);
    }

    public void MostrarVisualAlSoltar()
    {
        if (!ocultarVisualMientrasSeCarga)
            return;

        GuardarEstadoRenderersActuales();
        RestaurarEstadoRenderers();
    }

    void GuardarEstadoRenderersActuales()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rendererVisual in renderers)
        {
            if (rendererVisual != null && !estadosRenderersOriginales.ContainsKey(rendererVisual))
                estadosRenderersOriginales.Add(rendererVisual, rendererVisual.enabled);
        }
    }

    void SetRenderersActivos(bool activo)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rendererVisual in renderers)
        {
            if (rendererVisual != null)
                rendererVisual.enabled = activo;
        }
    }

    void RestaurarEstadoRenderers()
    {
        foreach (System.Collections.Generic.KeyValuePair<Renderer, bool> estado in estadosRenderersOriginales)
        {
            if (estado.Key != null)
                estado.Key.enabled = estado.Value;
        }
    }
}
