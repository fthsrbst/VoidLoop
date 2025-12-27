using UnityEngine;

/// <summary>
/// Unity 2022 için First Person Controller
/// Kullanım: Bu scripti Player objesine ekleyin ve gerekli referansları atayın.
/// Gamepad ve Klavye/Fare desteği mevcuttur.
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

    [Header("Gamepad Ayarları")]
    [SerializeField] private float gamepadLookSensitivity = 1.2f;
    [SerializeField] private float gamepadAimAssistDeadzone = 0.15f;
    [Tooltip("Analog stick dead zone değeri")]
    [SerializeField] private float stickDeadzone = 0.1f;
    [Tooltip("Sağ analog stick için hızlanma eğrisi (1 = lineer, 2+ = üstel)")]
    [SerializeField] private float lookAccelerationCurve = 2f;
    [Tooltip("Gamepad titreşim özelliğini aktifleştir")]
    [SerializeField] private bool enableVibration = true;

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
    
    // Gamepad private değişkenler
    private bool isUsingGamepad;
    private bool crouchTogglePressed;
    private float vibrationTimer;

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
        HandleVibration();

        // ESC tuşu veya gamepad Start butonu ile cursor'u serbest bırak
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7))
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
        // Klavye input'u al
        float keyboardHorizontal = Input.GetAxisRaw("Horizontal");
        float keyboardVertical = Input.GetAxisRaw("Vertical");
        
        // Gamepad sol analog stick input'u
        float gamepadHorizontal = Input.GetAxis("Horizontal");
        float gamepadVertical = Input.GetAxis("Vertical");
        
        // Gamepad dead zone uygula
        Vector2 gamepadInput = new Vector2(gamepadHorizontal, gamepadVertical);
        if (gamepadInput.magnitude < stickDeadzone)
        {
            gamepadInput = Vector2.zero;
        }
        else
        {
            // Dead zone'u normalize et
            gamepadInput = gamepadInput.normalized * ((gamepadInput.magnitude - stickDeadzone) / (1f - stickDeadzone));
        }
        
        // Klavye veya gamepad kullanımını tespit et
        float horizontal = Mathf.Abs(keyboardHorizontal) > 0.1f ? keyboardHorizontal : gamepadInput.x;
        float vertical = Mathf.Abs(keyboardVertical) > 0.1f ? keyboardVertical : gamepadInput.y;
        
        // Gamepad kullanım tespiti
        isUsingGamepad = gamepadInput.magnitude > 0.1f || 
                         Mathf.Abs(Input.GetAxis("RightStickHorizontal")) > stickDeadzone || 
                         Mathf.Abs(Input.GetAxis("RightStickVertical")) > stickDeadzone;

        // Sprint kontrolü (Klavye: Left Shift, Gamepad: Left Stick Click veya Left Bumper)
        bool sprintKeyboard = Input.GetKey(KeyCode.LeftShift);
bool sprintGamepad = Input.GetKey(KeyCode.JoystickButton4);
        isSprinting = (sprintKeyboard || sprintGamepad) && !isCrouching && vertical > 0;

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
        // Klavye: Space, Gamepad: A butonu (South) - Unity Input Manager "Jump" olarak tanımlı
        bool jumpPressed = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.JoystickButton0);
        
        if (jumpPressed && isGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            
            // Zıplama sesi
            if (jumpSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(jumpSound);
            }
            
            // Gamepad titreşimi
            TriggerVibration(0.2f, 0.3f, 0.1f);
        }
    }

    private void HandleCrouch()
    {
        // Klavye: Left Control veya C, Gamepad: B butonu (East) veya Right Stick Click
        bool crouchKeyboard = Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C);
        bool crouchGamepad = Input.GetKeyDown(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.JoystickButton9); // B veya RS
        
        // Toggle sistemi için gamepad kontrolü
        if (!crouchTogglePressed && (Input.GetKey(KeyCode.JoystickButton1) || Input.GetKey(KeyCode.JoystickButton9)))
        {
            crouchTogglePressed = true;
            
            if (isCrouching)
            {
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
        else if (!Input.GetKey(KeyCode.JoystickButton1) && !Input.GetKey(KeyCode.JoystickButton9))
        {
            crouchTogglePressed = false;
        }
        
        // Klavye crouch kontrolü
        if (crouchKeyboard)
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

        // Fare girişi
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Gamepad sağ analog stick girişi
        float gamepadLookX = Input.GetAxis("RightStickHorizontal");
        float gamepadLookY = Input.GetAxis("RightStickVertical");
        
        // Gamepad dead zone uygula
        Vector2 gamepadLook = new Vector2(gamepadLookX, gamepadLookY);
        if (gamepadLook.magnitude < gamepadAimAssistDeadzone)
        {
            gamepadLook = Vector2.zero;
        }
        else
        {
            // Dead zone'u normalize et ve üstel hızlanma eğrisi uygula
            float normalizedMagnitude = (gamepadLook.magnitude - gamepadAimAssistDeadzone) / (1f - gamepadAimAssistDeadzone);
            normalizedMagnitude = Mathf.Pow(normalizedMagnitude, lookAccelerationCurve);
            gamepadLook = gamepadLook.normalized * normalizedMagnitude * gamepadLookSensitivity;
        }
        
        // Her iki girişi birleştir (fare öncelikli)
        float lookX = Mathf.Abs(mouseX) > 0.01f ? mouseX : gamepadLook.x;
        float lookY = Mathf.Abs(mouseY) > 0.01f ? mouseY : gamepadLook.y;

        if (invertY)
            lookY = -lookY;

        // Yatay dönüş (Player objesini döndür)
        transform.Rotate(Vector3.up * lookX);

        // Dikey dönüş (Kamerayı döndür)
        xRotation -= lookY;
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
            
            // Sert iniş için gamepad titreşimi
            float impactIntensity = Mathf.Clamp01(Mathf.Abs(velocity.y) / 20f);
            TriggerVibration(impactIntensity * 0.5f, impactIntensity, 0.2f);
        }
    }
    
    private void HandleVibration()
    {
        if (vibrationTimer > 0)
        {
            vibrationTimer -= Time.deltaTime;
            if (vibrationTimer <= 0)
            {
                StopVibration();
            }
        }
    }
    
    /// <summary>
    /// Gamepad titreşimini tetikler
    /// </summary>
    /// <param name="lowFrequency">Düşük frekans motor gücü (0-1)</param>
    /// <param name="highFrequency">Yüksek frekans motor gücü (0-1)</param>
    /// <param name="duration">Titreşim süresi (saniye)</param>
    private void TriggerVibration(float lowFrequency, float highFrequency, float duration)
    {
        if (!enableVibration) return;
        
        #if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        // Unity Input System kullanılıyorsa
        // UnityEngine.InputSystem.Gamepad.current?.SetMotorSpeeds(lowFrequency, highFrequency);
        #endif
        
        vibrationTimer = duration;
    }
    
    /// <summary>
    /// Gamepad titreşimini durdurur
    /// </summary>
    private void StopVibration()
    {
        #if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        // Unity Input System kullanılıyorsa
        // UnityEngine.InputSystem.Gamepad.current?.SetMotorSpeeds(0, 0);
        #endif
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
    public bool IsUsingGamepad() => isUsingGamepad;
    public Vector3 GetVelocity() => currentMoveVelocity;
    public float GetCurrentSpeed() => currentMoveVelocity.magnitude;

    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;
    }
    
    public void SetGamepadLookSensitivity(float sensitivity)
    {
        gamepadLookSensitivity = sensitivity;
    }

    public void SetInvertY(bool invert)
    {
        invertY = invert;
    }
    
    public void SetVibrationEnabled(bool enabled)
    {
        enableVibration = enabled;
        if (!enabled)
        {
            StopVibration();
        }
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
