using UnityEngine;

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

    [SerializeField] Collider playerDetection;

    private bool canMove = true;

    #region itemInfo
    public bool isLookingAtItem;
    #endregion

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
        #region Camara
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        horizontalRotation += mouseX;
        verticalRotation = Mathf.Clamp(verticalRotation - mouseY, -90f, 90f);

        transform.rotation = Quaternion.Euler(0, horizontalRotation, 0);
        camTransform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
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

        if (grabbedTransform == null)
        {
            if (Physics.Raycast(ray, out hit, RayDistance))
            {
                if (hit.transform.CompareTag("Item"))
                {
                    isLookingAtItem = true;

                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        GrabTransform(hit.transform);
                    }
                }
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                ReleaseTransform();
            }
        }
        #endregion

        #region Grab Follow Camera
        if (grabbedTransform != null)
        {
            grabbedTransform.position = camTransform.position + camTransform.forward * holdDistance;
            grabbedTransform.rotation = camTransform.rotation;
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
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = false;
    }
    #endregion

    #region Grab
    private void GrabTransform(Transform transformToGrab)
    {
        grabbedTransform = transformToGrab;

        Rigidbody rb = grabbedTransform.GetComponent<Rigidbody>();
        Collider col = grabbedTransform.GetComponent<Collider>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (col != null)
            col.enabled = false;
    }

    private void ReleaseTransform()
    {
        Rigidbody rb = grabbedTransform.GetComponent<Rigidbody>();
        Collider col = grabbedTransform.GetComponent<Collider>();

        if (col != null)
            col.enabled = true;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.freezeRotation = false;
        }

        grabbedTransform = null;
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
    #endregion
}