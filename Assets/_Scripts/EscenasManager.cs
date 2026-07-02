using UnityEngine;
using UnityEngine.SceneManagement;

public class EscenasManager : MonoBehaviour
{
    [Header("Escenas")]
    [SerializeField] private string escenaJuego = "SampleScene";

    [Header("Paneles UI")]
    [SerializeField] private GameObject panelOpciones;
    [SerializeField] private GameObject[] objetosMenuPrincipal;

    void Start()
    {
        SetMenuPrincipalActivo(true);

        if (panelOpciones != null)
            panelOpciones.SetActive(false);
    }

    public void Jugar()
    {
        SceneManager.LoadScene(escenaJuego);
    }

    public void Opciones()
    {
        if (panelOpciones == null)
        {
            return;
        }

        SetMenuPrincipalActivo(false);
        panelOpciones.SetActive(true);
    }

    public void Volver()
    {
        SetMenuPrincipalActivo(true);

        if (panelOpciones != null)
            panelOpciones.SetActive(false);
    }

    public void Salir()
    {
        Application.Quit();
    }

    void SetMenuPrincipalActivo(bool activo)
    {
        if (objetosMenuPrincipal == null)
            return;

        foreach (GameObject objeto in objetosMenuPrincipal)
        {
            if (objeto != null)
                objeto.SetActive(activo);
        }
    }
}
