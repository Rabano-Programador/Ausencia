using TMPro;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[DisallowMultipleComponent]

public class PlayerController : MonoBehaviour
{
    #region Variables

    #region Movimiento
    [Header("Camara y movimiento")]
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float crouchSpeed = 2.5f;
    public float mouseSensitivity = 200f;
    private float horizontalRotation;
    private float verticalRotation;
    private Vector3 PlayerMovement;
    private Transform camTransform;
    private float currentSpeed;

    [Header("Salto")]
    public float jumpForce = 10;
    private Rigidbody rb;
    private bool isGrounded;

    [Header("Agacharse")]
    public float crouchHeight = 1f;
    public float crouchCameraOffset = -0.5f;
    private float originalHeight;
    private Vector3 originalCenter;
    private Vector3 originalCameraPos;
    private CapsuleCollider capsule;
    private bool isCrouching;

    [Header("Grab")]
    public float RayDistance = 10;
    public float holdDistance = 2f;
    public Transform grabbedTransform;
    public Transform playerHands;
    #endregion

    [Header("Configuracion de Reposicion")]
    public float distanciaDeColocacion = 3.5f;

    [Header("UI Interaccion")]
    public TextMeshProUGUI textoInteraccion;


    [SerializeField] Collider playerDetection;

    private bool canMove = true;

    #region itemInfo
    public bool isLookingAtItem;
    #endregion

    [Header("Configuracion Caja Registradora")]
    public Transform puntoCajaTransform;
    [FormerlySerializedAs("puntoDespachoDestino")] public Transform puntoEntregaDestino;

    [Header("Configuracion Transbank")]
    public Transform puntoCamaraTransbank;

    [Header("Audio")]
    public float intervaloPasos = 0.7f;
    private float tiempoSiguientePaso;
    private bool alternarPaso;

    private bool estaEnLaCaja = false;
    private bool estaEnTransbank = false;
    private float bloqueoInteraccionCajaHasta = 0f;

    public bool EstaEnLaCaja => estaEnLaCaja;
    public bool IsRunning => canMove && !isCrouching && Input.GetKey(KeyCode.LeftShift) && (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0);

    [HideInInspector] public bool bloquearCamaraPorAtaque = false;

    #endregion

    #region Awake & Start
    private void Awake()
    {
        camTransform = GetComponentInChildren<Camera>().transform;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        capsule = GetComponent<CapsuleCollider>();
        originalHeight = capsule.height;
        originalCenter = capsule.center;
        originalCameraPos = camTransform.localPosition;
    }
    #endregion

    #region Update
    void Update()
    {
        if (PauseManager.IsPaused)
        {
            if (textoInteraccion != null)
                textoInteraccion.text = "";
            OcultarIndicadoresEstantes();
            return;
        }

        #region Camara
        if (!estaEnTransbank && !bloquearCamaraPorAtaque)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            horizontalRotation += mouseX;
            verticalRotation = Mathf.Clamp(verticalRotation - mouseY, -90f, 90f);

            transform.rotation = Quaternion.Euler(0, horizontalRotation, 0);
            camTransform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
        }
        else if (estaEnTransbank)
        {
            if (puntoCamaraTransbank != null)
            {
                camTransform.position = puntoCamaraTransbank.position;
                camTransform.rotation = puntoCamaraTransbank.rotation;
            }
        }
        #endregion

        #region Movimiento
        if (canMove)
        {
            if (isCrouching)
                currentSpeed = crouchSpeed;
            else
                currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

            float moveX = Input.GetAxis("Horizontal") * currentSpeed;
            float moveZ = Input.GetAxis("Vertical") * currentSpeed;

            Vector3 move = transform.right * moveX + transform.forward * moveZ;
            PlayerMovement = new Vector3(move.x, 0, move.z);

            transform.Translate(PlayerMovement * Time.deltaTime, Space.World);

            if (PlayerMovement.sqrMagnitude > 0.01f && Time.time >= tiempoSiguientePaso)
            {
                Debug.Log("ESTOYCAMINDANDO");
                AudioClip paso = alternarPaso ? AudioManager.instance.sonidoPasosJugador : AudioManager.instance.sonidoPasosJugador2;
                AudioManager.instance.ReproducirSonido(paso);
                alternarPaso = !alternarPaso;
                tiempoSiguientePaso = Time.time + intervaloPasos;
            }
        }
        else if (estaEnLaCaja && puntoCajaTransform != null && !estaEnTransbank)
        {
            transform.position = puntoCajaTransform.position;
        }
        #endregion

        #region Salto
        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
        #endregion

        #region Agacharse
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            Crouch();
        }
        else if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            StandUp();
        }
        #endregion

        #region Grab Input
        Ray ray = new Ray(camTransform.position, camTransform.forward);
        RaycastHit hit;

        isLookingAtItem = false;

        if (textoInteraccion != null) textoInteraccion.text = "";

        Color colorDebug = estaEnLaCaja ? Color.cyan : Color.red;
        Debug.DrawRay(ray.origin, ray.direction * RayDistance, colorDebug);

        if (grabbedTransform == null && !estaEnTransbank)
        {
            if (Physics.Raycast(ray, out hit, RayDistance))
            {
                if (hit.transform.CompareTag("ComputadoraCaja") && !estaEnLaCaja)
                {
                    if (textoInteraccion != null) textoInteraccion.text = "[E] Operar Caja Registradora";

                    if (Input.GetKeyDown(KeyCode.E) && Time.unscaledTime >= bloqueoInteraccionCajaHasta)
                    {
                        EntrarAModoCaja();
                    }
                }
                else if (hit.transform.CompareTag("Transbank") && estaEnLaCaja)
                {
                    if (textoInteraccion != null) textoInteraccion.text = "[LMB] Cobrar con Tarjeta";

                    if (Input.GetMouseButtonDown(0))
                    {
                        EntrarAModoTransbank();
                    }
                }
                else if (hit.transform.CompareTag("Item"))
                {
                    isLookingAtItem = true;

                    ProductBox cajaMirada = hit.transform.GetComponentInParent<ProductBox>();
                    ObjetoCaja objetoCaja = hit.transform.GetComponentInParent<ObjetoCaja>();

                    if (!estaEnLaCaja && cajaMirada != null)
                    {
                        if (cajaMirada.datosProducto != null && textoInteraccion != null)
                        {
                            textoInteraccion.text = "[LMB] Tomar:\n" + cajaMirada.datosProducto.nombreProducto + " (" + cajaMirada.unidadesRestantes + ")";
                        }

                        if (Input.GetMouseButtonDown(0))
                        {
                            GrabTransform(cajaMirada.transform);
                            AudioManager.instance.ReproducirSonido(AudioManager.instance.sonidoRecogerItem);
                        }
                    }
                    else if (objetoCaja != null && objetoCaja.disponibleParaCobro)
                    {
                        if (estaEnLaCaja)
                        {
                            if (textoInteraccion != null)
                            {
                                string nombreProducto = objetoCaja.datosProducto != null ? objetoCaja.datosProducto.nombreProducto : "Producto";
                                textoInteraccion.text = $"[LMB] Marcar y Despachar\n{nombreProducto}: ${objetoCaja.precioProducto:F2}";
                            }

                            if (Input.GetMouseButtonDown(0))
                            {
                                ControladorCajaUI cajaUI = GetComponentInChildren<ControladorCajaUI>();
                                if (cajaUI == null) cajaUI = FindFirstObjectByType<ControladorCajaUI>();

                                if (cajaUI != null)
                                {
                                    cajaUI.RegistrarProductoEscaneado(objetoCaja.precioProducto);
                                }

                                if (QTEManager.Instance != null)
                                {
                                    QTEManager.Instance.AcumularTension(QTEManager.Instance.tensionPorCobrar);
                                }

                                if (textoInteraccion != null)
                                {
                                    string nombreProducto = objetoCaja.datosProducto != null ? objetoCaja.datosProducto.nombreProducto : "Producto";
                                    textoInteraccion.text = $"{nombreProducto} cobrado por ${objetoCaja.precioProducto:F2}";
                                }

                                if (puntoEntregaDestino != null)
                                {
                                    objetoCaja.TeletransportarA(puntoEntregaDestino.position);

                                    PuntoEntregaTrigger puntoEntregaTrigger = puntoEntregaDestino.GetComponent<PuntoEntregaTrigger>();
                                    if (puntoEntregaTrigger != null)
                                    {
                                        objetoCaja.MarcarEnPuntoEntrega(true);
                                        puntoEntregaTrigger.RegistrarObjeto(objetoCaja);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (textoInteraccion != null) textoInteraccion.text = "Usa la computadora para empezar a cobrar";
                        }
                    }
                }
            }
        }
        else
        {
            if (textoInteraccion != null && grabbedTransform != null)
            {
                ProductBox cajaEnMano = grabbedTransform.GetComponent<ProductBox>();
                if (cajaEnMano != null && cajaEnMano.datosProducto != null)
                {
                    textoInteraccion.text = "Cargando: " + cajaEnMano.datosProducto.nombreProducto + " (" + cajaEnMano.unidadesRestantes + ")\n[LMB] Reponer | [E] Soltar";
                }
            }

            ActualizarIndicadoresEstantes();

            if (Input.GetKeyDown(KeyCode.E) && grabbedTransform != null)
            {
                ReleaseTransform();
            }
        }

        if (estaEnLaCaja && !estaEnTransbank && Input.GetKeyDown(KeyCode.E) && Time.unscaledTime >= bloqueoInteraccionCajaHasta)
        {
            SalirDeModoCaja();
        }

        #endregion

        #region Grab Follow Camera
        if (grabbedTransform != null)
        {
            float distanciaActual = holdDistance;
            RaycastHit wallHit;
            if (Physics.Raycast(camTransform.position, camTransform.forward, out wallHit, holdDistance))
            {
                distanciaActual = wallHit.distance - 0.2f;
            }

            grabbedTransform.position = camTransform.position + camTransform.forward * distanciaActual;
            grabbedTransform.rotation = camTransform.rotation;
        }
        #endregion

        #region Colocacion en Estanteria (LMB)
        if (grabbedTransform != null && Input.GetMouseButtonDown(0))
        {
            ProductBox caja = grabbedTransform.GetComponent<ProductBox>();
            RestockShelf estante = EncontrarEstanteParaReposicion(caja);

            if (estante != null && caja != null && estante.ReponerProducto(caja))
            {
                if (QTEManager.Instance != null)
                {
                    QTEManager.Instance.AcumularTension(QTEManager.Instance.tensionPorReponer);
                }
            }
        }
        #endregion
    }
    #endregion

    #region Agacharse
    private void Crouch()
    {
        isCrouching = true;
        capsule.height = crouchHeight;
        capsule.center = new Vector3(originalCenter.x, originalCenter.y - (originalHeight - crouchHeight) / 2f, originalCenter.z);
        camTransform.localPosition = originalCameraPos + new Vector3(0, crouchCameraOffset, 0);
        AudioManager.instance.sfxSource.PlayOneShot(AudioManager.instance.sonidoAgacharse, 3f);
    }

    private void StandUp()
    {
        isCrouching = false;
        capsule.height = originalHeight;
        capsule.center = originalCenter;
        camTransform.localPosition = originalCameraPos;
    }
    #endregion

    #region Suelo (CompareTag)
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground")) isGrounded = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground")) isGrounded = false;
    }
    #endregion

    #region Grab
    private void GrabTransform(Transform transformToGrab)
    {
        grabbedTransform = transformToGrab;
        Rigidbody rb = grabbedTransform.GetComponent<Rigidbody>();
        Collider col = grabbedTransform.GetComponent<Collider>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
        if (col != null) col.enabled = false;
    }

    private void ReleaseTransform()
    {
        Rigidbody rb = grabbedTransform.GetComponent<Rigidbody>();
        Collider col = grabbedTransform.GetComponent<Collider>();
        if (col != null) col.enabled = true;
        if (rb != null) { rb.isKinematic = false; rb.useGravity = true; rb.freezeRotation = false; }
        grabbedTransform = null;
        OcultarIndicadoresEstantes();
    }

    public void ForzarSoltarItem()
    {
        if (grabbedTransform != null)
        {
            ReleaseTransform();
        }
    }
    #endregion

    #region Gestion de Estados de Caja y Transbank
    private void EntrarAModoCaja()
    {
        estaEnLaCaja = true;
        bloqueoInteraccionCajaHasta = Time.unscaledTime + 0.2f;
        SetCanMove(false);

        if (puntoCajaTransform != null)
        {
            transform.position = puntoCajaTransform.position;
            transform.rotation = puntoCajaTransform.rotation;
            horizontalRotation = puntoCajaTransform.eulerAngles.y;
            verticalRotation = 0f;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SalirDeModoCaja()
    {
        estaEnLaCaja = false;
        bloqueoInteraccionCajaHasta = Time.unscaledTime + 0.2f;
        SetCanMove(true);
    }

    private void EntrarAModoTransbank()
    {
        estaEnTransbank = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SalirDeModoTransbank()
    {
        estaEnTransbank = false;

        camTransform.localPosition = originalCameraPos;
        AplicarEstadoCursor();
    }
    #endregion

    #region QTE Control
    public void SetCanMove(bool state)
    {
        canMove = state;
    }

    public void TeleportTo(Vector3 newPos)
    {
        transform.position = newPos;
    }

    void ActualizarIndicadoresEstantes()
    {
        ProductBox cajaEnMano = grabbedTransform != null ? grabbedTransform.GetComponent<ProductBox>() : null;
        ProductoData producto = cajaEnMano != null ? cajaEnMano.datosProducto : null;

        if (producto == null)
        {
            OcultarIndicadoresEstantes();
            return;
        }

        foreach (RestockShelf estante in RestockShelf.Instancias)
        {
            if (estante != null)
                estante.MostrarIndicadorPara(producto);
        }
    }

    void OcultarIndicadoresEstantes()
    {
        foreach (RestockShelf estante in RestockShelf.Instancias)
        {
            if (estante != null)
                estante.OcultarIndicador();
        }
    }

    RestockShelf EncontrarEstanteParaReposicion(ProductBox caja)
    {
        if (caja == null || caja.datosProducto == null)
            return null;

        Ray placementRay = new Ray(camTransform.position, camTransform.forward);
        RaycastHit[] hits = Physics.RaycastAll(placementRay, distanciaDeColocacion);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            RestockShelf estanteMirado = hit.collider.GetComponentInParent<RestockShelf>();
            if (estanteMirado != null && estanteMirado.PuedeRecibirProducto(caja.datosProducto))
                return estanteMirado;
        }

        RestockShelf estanteCercano = null;
        float menorDistancia = distanciaDeColocacion;

        foreach (RestockShelf estante in RestockShelf.Instancias)
        {
            if (estante == null || !estante.PuedeRecibirProducto(caja.datosProducto))
                continue;

            float distancia = DistanciaAlEstante(estante);
            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                estanteCercano = estante;
            }
        }

        return estanteCercano;
    }

    float DistanciaAlEstante(RestockShelf estante)
    {
        Collider[] colliders = estante.GetComponentsInChildren<Collider>();
        if (colliders.Length == 0)
            return Vector3.Distance(transform.position, estante.transform.position);

        float menorDistancia = float.MaxValue;
        foreach (Collider col in colliders)
        {
            if (col == null || !col.enabled)
                continue;

            Vector3 puntoMasCercano = col.ClosestPoint(transform.position);
            float distancia = Vector3.Distance(transform.position, puntoMasCercano);
            if (distancia < menorDistancia)
                menorDistancia = distancia;
        }

        return menorDistancia;
    }

    public void AplicarEstadoCursor()
    {
        if (PauseManager.IsPaused)
            return;

        if (estaEnTransbank)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    #endregion
}
