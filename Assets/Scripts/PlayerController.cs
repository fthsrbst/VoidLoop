using UnityEngine;

/// <summary>
/// Unity 2022 için First Person Controller
/// Kullanım: Bu scripti Player objesine ekleyin ve gerekli referansları atayın.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 10f;

    [Header("Zıplama Ayarları")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundMask;

    [Header("Kamera Ayarları")]
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 85f;
    [SerializeField] private bool invertY = false;

    [Header("Eğilme (Crouch) Ayarları")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 10f;

    [Header("Headbob Ayarları")]
    [SerializeField] private bool enableHeadbob = true;
    [SerializeField] private float headbobFrequency = 1.5f;
    [SerializeField] private float headbobAmplitude = 0.1f;

    [Header("Ses Ayarları")]
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landSound;
    [SerializeField] private float footstepInterval = 0.5f;

    // Private değişkenler
    private CharacterController characterController;
    private AudioSource audioSource;
    private Vector3 velocity;
    private Vector3 currentMoveVelocity;
    private float xRotation;
    private float currentSpeed;
    private float targetHeight;
    private float defaultCameraY;
    private float headbobTimer;
    private float footstepTimer;
    private bool isGrounded;
    private bool wasPreviouslyGrounded;
    private bool isCrouching;
    private bool isSprinting;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
        }
    }

    private void Start()
    {
        // Cursor'u kilitle ve gizle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Varsayılan değerleri ayarla
        targetHeight = standingHeight;
        
        if (cameraHolder != null)
        {
            defaultCameraY = cameraHolder.localPosition.y;
        }

        // Eğer cameraHolder atanmadıysa, Main Camera'yı bul
        if (cameraHolder == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                cameraHolder = mainCam.transform;
                defaultCameraY = cameraHolder.localPosition.y;
            }
            else
            {
                Debug.LogError("FirstPersonController: Kamera bulunamadı! Lütfen cameraHolder'ı atayın.");
            }
        }
    }

    private void Update()
    {
        HandleGroundCheck();
        HandleMovementInput();
        HandleJump();
        HandleCrouch();
        HandleMouseLook();
        HandleHeadbob();
        ApplyGravity();
        ApplyMovement();
        HandleFootsteps();
        HandleLanding();

        // ESC tuşu ile cursor'u serbest bırak
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursor();
        }
    }

    private void HandleGroundCheck()
    {
        wasPreviouslyGrounded = isGrounded;
        
        // Zemin kontrolü için sphere cast kullan
        Vector3 spherePosition = transform.position + Vector3.down * (characterController.height / 2f - characterController.radius);
        isGrounded = Physics.CheckSphere(spherePosition, characterController.radius + groundCheckDistance, groundMask);

        // Alternatif olarak CharacterController'ın kendi ground check'ini de kontrol et
        if (!isGrounded)
        {
            isGrounded = characterController.isGrounded;
        }
    }

    private void HandleMovementInput()
    {
        // Input al
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // Sprint kontrolü
        isSprinting = Input.GetKey(KeyCode.LeftShift) && !isCrouching && vertical > 0;

        // Hedef hızı belirle
        float targetSpeed;
        if (isCrouching)
        {
            targetSpeed = crouchSpeed;
        }
        else if (isSprinting)
        {
            targetSpeed = sprintSpeed;
        }
        else
        {
            targetSpeed = walkSpeed;
        }

        // Hareket yönünü hesapla
        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
        moveDirection = moveDirection.normalized;

        // Smoothly hızı ayarla
        Vector3 targetVelocity = moveDirection * targetSpeed;

        if (moveDirection.magnitude > 0.1f)
        {
            currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, targetVelocity, acceleration * Time.deltaTime);
        }
        else
        {
            currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, Vector3.zero, deceleration * Time.deltaTime);
        }
    }

    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            
            // Zıplama sesi
            if (jumpSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(jumpSound);
            }
        }
    }

    private void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C))
        {
            if (isCrouching)
            {
                // Kalkmadan önce üstte engel var mı kontrol et
                if (!Physics.Raycast(transform.position, Vector3.up, standingHeight - crouchHeight + 0.1f))
                {
                    isCrouching = false;
                    targetHeight = standingHeight;
                }
            }
            else
            {
                isCrouching = true;
                targetHeight = crouchHeight;
            }
        }

        // Smooth geçiş
        if (Mathf.Abs(characterController.height - targetHeight) > 0.01f)
        {
            float previousHeight = characterController.height;
            characterController.height = Mathf.Lerp(characterController.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
            
            // Karakteri aşağı/yukarı hareket ettir
            float heightDifference = characterController.height - previousHeight;
            transform.position += Vector3.up * (heightDifference / 2f);

            // Kamera pozisyonunu ayarla
            if (cameraHolder != null)
            {
                float cameraTargetY = isCrouching ? defaultCameraY - (standingHeight - crouchHeight) / 2f : defaultCameraY;
                Vector3 camPos = cameraHolder.localPosition;
                camPos.y = Mathf.Lerp(camPos.y, cameraTargetY, crouchTransitionSpeed * Time.deltaTime);
                cameraHolder.localPosition = camPos;
            }
        }
    }

    private void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        if (invertY)
            mouseY = -mouseY;

        // Yatay dönüş (Player objesini döndür)
        transform.Rotate(Vector3.up * mouseX);

        // Dikey dönüş (Kamerayı döndür)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        if (cameraHolder != null)
        {
            cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    private void HandleHeadbob()
    {
        if (!enableHeadbob || cameraHolder == null)
            return;

        if (isGrounded && currentMoveVelocity.magnitude > 0.1f)
        {
            headbobTimer += Time.deltaTime * headbobFrequency * (isSprinting ? 1.5f : 1f);
            
            float bobAmount = Mathf.Sin(headbobTimer * Mathf.PI * 2f) * headbobAmplitude;
            
            Vector3 camPos = cameraHolder.localPosition;
            float targetY = isCrouching ? defaultCameraY - (standingHeight - crouchHeight) / 2f : defaultCameraY;
            camPos.y = targetY + bobAmount;
            cameraHolder.localPosition = camPos;
        }
        else
        {
            headbobTimer = 0f;
        }
    }

    private void ApplyGravity()
    {
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Küçük bir aşağı kuvvet uygula
        }

        velocity.y += gravity * Time.deltaTime;
    }

    private void ApplyMovement()
    {
        Vector3 finalMovement = currentMoveVelocity + Vector3.up * velocity.y;
        characterController.Move(finalMovement * Time.deltaTime);
    }

    private void HandleFootsteps()
    {
        if (footstepSounds == null || footstepSounds.Length == 0 || audioSource == null)
            return;

        if (isGrounded && currentMoveVelocity.magnitude > 0.5f)
        {
            footstepTimer += Time.deltaTime;
            float interval = isSprinting ? footstepInterval * 0.6f : footstepInterval;

            if (footstepTimer >= interval)
            {
                footstepTimer = 0f;
                AudioClip footstep = footstepSounds[Random.Range(0, footstepSounds.Length)];
                audioSource.PlayOneShot(footstep, 0.5f);
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    private void HandleLanding()
    {
        // Yere iniş kontrolü
        if (isGrounded && !wasPreviouslyGrounded && velocity.y < -5f)
        {
            if (landSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(landSound);
            }
        }
    }

    private void ToggleCursor()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
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

    // Public metodlar
    public bool IsGrounded() => isGrounded;
    public bool IsSprinting() => isSprinting;
    public bool IsCrouching() => isCrouching;
    public Vector3 GetVelocity() => currentMoveVelocity;
    public float GetCurrentSpeed() => currentMoveVelocity.magnitude;

    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;
    }

    public void SetInvertY(bool invert)
    {
        invertY = invert;
    }

    // Gizmos ile debug görselleştirme
    private void OnDrawGizmosSelected()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (characterController != null)
        {
            // Zemin kontrolü sphere'ini göster
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Vector3 spherePosition = transform.position + Vector3.down * (characterController.height / 2f - characterController.radius);
            Gizmos.DrawWireSphere(spherePosition, characterController.radius + groundCheckDistance);
        }
    }
}
