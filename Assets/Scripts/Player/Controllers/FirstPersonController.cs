﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(CapsuleCollider)), RequireComponent(typeof(Rigidbody)), AddComponentMenu("First Person Controller")]

public sealed class FirstPersonController : MonoBehaviour
{
    #region Variables    
    #region Look Settings
    private float VerticalRotationRange = 0f;
    private readonly float HeadMaxY = 90;
    private readonly float HeadMinY = -90;
    private const float Sensitivity = 2;

    public float SensivityM { get; set; } = 1;
    private readonly float CameraSmoothing = 5f;

    private Camera PlayerCamera;
    private Transform PlayerCameraTr;

    private Vector3 targetAngles;
    private Vector3 followAngles;
    private Vector3 followVelocity;

    private PlayerSoundsCalculator playerSoundsCalculator;
    #endregion

    #region Movement Settings
    private bool PlayerCanMove = true;
    private bool Sprint = false;
    private readonly float WalkSpeed = 4f;
    private float RecumbentingSpeed;

    private const KeyCode SprintKey = KeyCode.LeftShift;
    private float SprintSpeed = 8f;
    private readonly float JumpPower = 5f;
    private bool Jump;
    private bool didJump;

    private float speed;
    private float WalkSpeedInternal;
    private float SprintSpeedInternal;
    private float JumpPowerInternal;
    private float RecumbentingSpeedInternal;
    public delegate void StepHandler(int matIndex, StepPlayer.TypeOfMovement type);
    public event StepHandler PlayerStepEvent;

    [System.Serializable]
    public sealed class CrouchModifiers
    {
        public bool toggleCrouch = false;
        public KeyCode crouchKey = KeyCode.LeftControl;
        public float crouchWalkSpeedMultiplier = 0.5f;
        public float crouchJumpPowerMultiplier = 0f;
    }

    public sealed class RecumbentModifiers
    {
        public const KeyCode recumbentKey = KeyCode.Z;
        public float RecumbenthWalkSpeedMultiplier { get; set; } = 0.25f;
        public float RecumbentJumpPowerMultiplier { get; set; } = 0f;
    }
    public CrouchModifiers MCrouchModifiers { get; set; } = new CrouchModifiers();
    public RecumbentModifiers MRecumbentModifiers { get; set; } = new RecumbentModifiers();
    private StepPlayer stepPlayer;

    [System.Serializable]
    public sealed class AdvancedSettings
    {
        public float GravityMultiplier { get; set; } = 2f;

        public PhysicMaterial ZeroFrictionMaterial { get; set; }

        public PhysicMaterial HighFrictionMaterial { get; set; }

        public float MaxSlopeAngle { get; set; } = 90f;
        internal bool IsTouchingWalkable { get; set; }
        internal bool IsTouchingUpright { get; set; }
        internal bool IsTouchingFlat { get; set; }
        public float MaxWallShear { get; set; } = 89f;
        public float MaxStepHeight { get; set; } = 0.2f;
        internal bool stairMiniHop = false;
        public Vector3 CurntGroundNormal { get; set; }
        public float LastKnownSlopeAngle { get; set; }
        public float FOVKickAmount { get; set; } = 2.5f;
        public float fovRef;

        public float ColliderRadius { get; set; }
        public float ColliderHeight { get; set; }
    }

    public AdvancedSettings Advanced { get; set; } = new AdvancedSettings();
    private CapsuleCollider capsule;
    private bool IsGrounded = true;
    private Vector2 inputXY;
    private bool isCrouching = false;
    private bool isRecumbenting = false;
    private float yVelocity;

    private Rigidbody _fpsRigidbody;
    public CapsuleCollider GetCollider() => capsule;
    private Vector3 oldPos = Vector3.zero;
    private int CurrentPhysicMaterialIndex;
    private bool wasGrounded = false;
    #endregion

    #endregion

    private void Awake()
    {
        #region Movement Settings - Awake

        PlayerCamera = Camera.main;
        PlayerCameraTr = PlayerCamera.transform;
        JumpPowerInternal = JumpPower;
        capsule = GetComponent<CapsuleCollider>();
        _fpsRigidbody = GetComponent<Rigidbody>();
        _fpsRigidbody.interpolation = RigidbodyInterpolation.Extrapolate;
        _fpsRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

        followAngles = targetAngles = transform.localEulerAngles;
        #endregion
    }
    private void OnEnable()
    {
        terrain = FindObjectOfType<Terrain>();
        terrainTr = terrain.transform;
        terrainDetector = new TerrainDetector(terrain);
        stepPlayer = new StepPlayer(terrainDetector);
        stepPlayer.OnInit(this);
        SprintSpeed = WalkSpeed * 1.5f;
        WalkSpeedInternal = WalkSpeed;
        SprintSpeedInternal = SprintSpeed;
        RecumbentingSpeed = WalkSpeed * 0.25f;
        RecumbentingSpeedInternal = RecumbentingSpeed;
    }

    private void Start()
    {
        #region Look Settings - Start
        VerticalRotationRange = 2 * HeadMaxY + Mathf.Clamp(0, HeadMinY, 0);
        #endregion

        #region Movement Settings - Start  
        capsule.radius = capsule.height / 4;
        Advanced.ColliderHeight = capsule.height;
        Advanced.ColliderRadius = capsule.radius;
        Advanced.ZeroFrictionMaterial = new PhysicMaterial("Zero_Friction")
        {
            dynamicFriction = 0,
            staticFriction = 0,
            frictionCombine = PhysicMaterialCombine.Minimum,
            bounceCombine = PhysicMaterialCombine.Minimum
        };

        Advanced.HighFrictionMaterial = new PhysicMaterial("Max_Friction")
        {
            dynamicFriction = 1,
            staticFriction = 1,
            frictionCombine = PhysicMaterialCombine.Maximum,
            bounceCombine = PhysicMaterialCombine.Average
        };
        playerSoundsCalculator = FindObjectOfType<PlayerSoundsCalculator>();
        #endregion
    }

    private void Update()
    {
        #region Look Settings - Update

        if (!ScreensManager.HasActiveScreen())
        {
            float mouseYInput = Input.GetAxis("Mouse Y");
            float mouseXInput = Input.GetAxis("Mouse X");

            if (targetAngles.y > 180) { targetAngles.y -= 360; followAngles.y -= 360; } else if (targetAngles.y < -180) { targetAngles.y += 360; followAngles.y += 360; }
            if (targetAngles.x > 180) { targetAngles.x -= 360; followAngles.x -= 360; } else if (targetAngles.x < -180) { targetAngles.x += 360; followAngles.x += 360; }

            targetAngles.y += mouseXInput * Sensitivity * SensivityM;//rotate camera

            targetAngles.x += mouseYInput * Sensitivity * SensivityM;

            targetAngles.x = Mathf.Clamp(targetAngles.x, -0.5f * VerticalRotationRange, 0.5f * VerticalRotationRange);
            followAngles = Vector3.SmoothDamp(followAngles, targetAngles, ref followVelocity, CameraSmoothing / 100);

            PlayerCameraTr.localRotation = Quaternion.Euler(-followAngles.x, 0, ZSlant);
            transform.localRotation = Quaternion.Euler(0, followAngles.y, 0);
        }
        #endregion

        #region Input Settings - Update
        if (Input.GetButtonDown("Jump") && !ScreensManager.HasActiveScreen())
            Jump = true;
        else if (Input.GetButtonUp("Jump"))
            Jump = false;


        if (!ScreensManager.HasActiveScreen())
        {
            isCrouching = Input.GetKey(MCrouchModifiers.crouchKey) && !isRecumbenting;

            if (Input.GetKeyDown(RecumbentModifiers.recumbentKey) && !isCrouching)
            {
                isRecumbenting = !isRecumbenting;
            }
        }

        Sprint = Input.GetKey(SprintKey) && !ScreensManager.HasActiveScreen();
        PlayerClasses.BasicNeeds.Instance.EnableFoodAndWaterMultiply(Sprint);

        #endregion
    }

    private void FixedUpdate()
    {
        #region Movement Settings - FixedUpdate        

        Vector3 MoveDirection = Vector3.zero;
        speed = Sprint ? isCrouching ? WalkSpeedInternal : SprintSpeedInternal : WalkSpeedInternal;
        if (isRecumbenting)
            speed = RecumbentingSpeedInternal;
        speed *= additionalBraking;

        if (Advanced.MaxSlopeAngle > 0)
        {
            if (Advanced.IsTouchingUpright && Advanced.IsTouchingWalkable)
            {
                MoveDirection = transform.forward * inputXY.y * speed + transform.right * inputXY.x * WalkSpeedInternal;
                if (!didJump)
                    _fpsRigidbody.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            }
            else if (Advanced.IsTouchingUpright && !Advanced.IsTouchingWalkable)
            {
                _fpsRigidbody.constraints = RigidbodyConstraints.None | RigidbodyConstraints.FreezeRotation;
            }

            else
            {
                _fpsRigidbody.constraints = RigidbodyConstraints.None | RigidbodyConstraints.FreezeRotation;
                MoveDirection = (transform.forward * inputXY.y * speed + transform.right * inputXY.x * WalkSpeedInternal) * (_fpsRigidbody.velocity.y > 0.01f ? SlopeCheck() : 0.8f);
            }
        }
        else
            MoveDirection = transform.forward * inputXY.y * speed + transform.right * inputXY.x * WalkSpeedInternal;

        #region step logic

        if (Advanced.MaxStepHeight > 0 && Physics.Raycast(transform.position - new Vector3(0, ((capsule.height / 2) * transform.localScale.y) - 0.01f, 0), MoveDirection, out RaycastHit WT, capsule.radius - 3, Physics.AllLayers, QueryTriggerInteraction.Ignore) && Vector3.Angle(WT.normal, Vector3.up) > 88)
        {
            if (!Physics.Raycast(transform.position - new Vector3(0, (capsule.height / 2 * transform.localScale.y) - Advanced.MaxStepHeight, 0), MoveDirection, out _, capsule.radius + 0.25f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                Advanced.stairMiniHop = true;
                transform.position += new Vector3(0, Advanced.MaxStepHeight * 1.2f, 0);
            }
        }
        #endregion
        float m = ScreensManager.HasActiveScreen() ? 0 : 1;
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        inputXY = new Vector2(horizontalInput, verticalInput) * m * additionalBraking;
        if (inputXY.magnitude > 1)
            inputXY.Normalize();

        #region Jump
        yVelocity = _fpsRigidbody.velocity.y;

        if (IsGrounded && Jump && JumpPowerInternal > 0 && !didJump)
        {
            if (Advanced.MaxSlopeAngle > 0)
            {
                if (Advanced.IsTouchingFlat || Advanced.IsTouchingWalkable)
                {
                    didJump = true;
                    Jump = false;
                    yVelocity += _fpsRigidbody.velocity.y < 0.01f ? JumpPowerInternal : JumpPowerInternal / 3;
                    Advanced.IsTouchingWalkable = false;
                    Advanced.IsTouchingFlat = false;
                    Advanced.IsTouchingUpright = false;
                    _fpsRigidbody.constraints = RigidbodyConstraints.None | RigidbodyConstraints.FreezeRotation;
                }

            }
            else
            {
                didJump = true;
                Jump = false;
                yVelocity += JumpPowerInternal;
            }

        }

        if (Advanced.MaxSlopeAngle > 0)
        {
            if (!didJump && Advanced.LastKnownSlopeAngle > 5 && Advanced.IsTouchingWalkable)
            {
                yVelocity *= SlopeCheck() / 4;
            }
        }

        #endregion

        if (PlayerCanMove)
        {
            Vector3 newVel = (MoveDirection + (Vector3.up * yVelocity));
            if (newVel.y > 0)
                newVel *= additionalBraking;
            _fpsRigidbody.velocity = newVel;
        }
        else _fpsRigidbody.velocity = Vector3.zero;

        capsule.sharedMaterial = inputXY.magnitude > 0 || !IsGrounded ? Advanced.ZeroFrictionMaterial : Advanced.HighFrictionMaterial;


        _fpsRigidbody.AddForce(Physics.gravity * (Advanced.GravityMultiplier - 1));


        /* if (Advanced.FOVKickAmount > 0)
         {
             if (!isCrouching && PlayerCamera.fieldOfView != (BaseCamFOV + (Advanced.FOVKickAmount * 2) - 0.01f))
             {
                   if (Mathf.Abs(_fpsRigidbody.velocity.x) > 0.5f || Mathf.Abs(_fpsRigidbody.velocity.z) > 0.5f)// Camera animate
                    {
                        PlayerCamera.fieldOfView = Mathf.SmoothDamp(PlayerCamera.fieldOfView, baseCamFOV + (Advanced.FOVKickAmount * 2), ref Advanced.fovRef, Advanced.ChangeTime);
                    }

                    else if (PlayerCamera.fieldOfView != baseCamFOV)
                    {
                        PlayerCamera.fieldOfView = Mathf.SmoothDamp(PlayerCamera.fieldOfView, baseCamFOV, ref Advanced.fovRef, Advanced.ChangeTime * 0.5f);
                    }
             }
         }*/

        float capsuleHeightFollowing, capsuleRadiusFollowing;
        if (isCrouching)
        {
            capsuleHeightFollowing = Advanced.ColliderHeight / 1.5f;
            capsuleRadiusFollowing = Advanced.ColliderRadius;

            WalkSpeedInternal = WalkSpeed * MCrouchModifiers.crouchWalkSpeedMultiplier;
            JumpPowerInternal = JumpPower * MCrouchModifiers.crouchJumpPowerMultiplier;
        }
        else if (isRecumbenting)
        {
            capsuleHeightFollowing = Advanced.ColliderHeight / 5;
            capsuleRadiusFollowing = Advanced.ColliderRadius / 5;
            WalkSpeedInternal = WalkSpeed * MRecumbentModifiers.RecumbenthWalkSpeedMultiplier;
            JumpPowerInternal = JumpPower * MRecumbentModifiers.RecumbentJumpPowerMultiplier;
        }
        else
        {
            capsuleHeightFollowing = Advanced.ColliderHeight;
            capsuleRadiusFollowing = Advanced.ColliderRadius;

            WalkSpeedInternal = WalkSpeed;
            SprintSpeedInternal = SprintSpeed;
            JumpPowerInternal = JumpPower;
        }
        capsule.height = Mathf.MoveTowards(capsule.height, capsuleHeightFollowing, 5 * Time.deltaTime * additionalBraking);
        capsule.radius = Mathf.MoveTowards(capsule.radius, capsuleRadiusFollowing, 5 * Time.deltaTime * additionalBraking);
        #endregion
        #region  Reset Checks

        if (Advanced.MaxSlopeAngle > 0)
        {
            if (Advanced.IsTouchingFlat || Advanced.IsTouchingWalkable || Advanced.IsTouchingUpright)
                didJump = false;
            Advanced.IsTouchingWalkable = false;
            Advanced.IsTouchingUpright = false;
            Advanced.IsTouchingFlat = false;
        }
        #endregion                    
        SetPhysMaterial();
        playerSoundsCalculator.SetPlayerSpeed(Mathf.Abs(Vector3.Distance(transform.position, oldPos)));
        Vector2 to = new Vector2(transform.position.x, transform.position.z);
        Vector2 from = new Vector2(oldPos.x, oldPos.z);
        StepPlayer.TypeOfMovement type = StepPlayer.TypeOfMovement.None;
        if (!wasGrounded && IsGrounded)
            type = StepPlayer.TypeOfMovement.JumpLand;
        else if (Mathf.Abs(Vector2.Distance(to, from)) > Time.fixedDeltaTime * 2)
        {
            if (Sprint) type = StepPlayer.TypeOfMovement.Run;
            else type = StepPlayer.TypeOfMovement.Walk;
        }
        PlayerStepEvent?.Invoke(CurrentPhysicMaterialIndex, type);
        oldPos = transform.position;
        wasGrounded = IsGrounded;
        IsGrounded = false;
    }
    private Terrain terrain;
    private Transform terrainTr;
    private TerrainDetector terrainDetector;

    private void SetPhysMaterial()
    {
        if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hit))
        {
            if (hit.transform != terrainTr)
                CurrentPhysicMaterialIndex = terrainDetector.GetIndexFromMaterial(hit.collider.sharedMaterial);
            else
                CurrentPhysicMaterialIndex = terrainDetector.GetActiveTerrainTextureIdx(transform.position);
        }
        else
            CurrentPhysicMaterialIndex = 0;
    }
    float SlopeCheck()
    {
        Advanced.LastKnownSlopeAngle = Mathf.MoveTowards(Advanced.LastKnownSlopeAngle, Vector3.Angle(Advanced.CurntGroundNormal, Vector3.up), 5f);

        return new AnimationCurve(new Keyframe(-90.0f, 1.0f), new Keyframe(0.0f, 1.0f), new Keyframe(Advanced.MaxSlopeAngle + 15, 0f), new Keyframe(Advanced.MaxWallShear, 0.0f), new Keyframe(Advanced.MaxWallShear + 0.1f, 1.0f), new Keyframe(90, 1.0f)) { preWrapMode = WrapMode.Clamp, postWrapMode = WrapMode.ClampForever }.Evaluate(Advanced.LastKnownSlopeAngle);
    }

    private void OnCollisionEnter(Collision CollisionData)
    {
        for (int i = 0; i < CollisionData.contactCount; i++)
        {
            ContactPoint currentCp = CollisionData.GetContact(i);
            float a = Vector3.Angle(currentCp.normal, Vector3.up);

            if (currentCp.point.y < transform.position.y - ((capsule.height / 2) - capsule.radius * 0.95f))
            {
                if (!IsGrounded)
                {
                    IsGrounded = true;
                    Advanced.stairMiniHop = false;
                    if (didJump && a <= 70)
                        didJump = false;
                }

                if (Advanced.MaxSlopeAngle > 0)
                {
                    if (a < 5.1f)
                    {
                        Advanced.IsTouchingFlat = true;
                        Advanced.IsTouchingWalkable = true;
                    }
                    else if (a < Advanced.MaxSlopeAngle + 0.1f)
                        Advanced.IsTouchingWalkable = true;
                    else if (a < 90)
                        Advanced.IsTouchingUpright = true;

                    Advanced.CurntGroundNormal = currentCp.normal;
                }
            }
        }
    }
    private void OnCollisionStay(Collision CollisionData)
    {
        for (int i = 0; i < CollisionData.contactCount; i++)
        {
            ContactPoint currentCp = CollisionData.GetContact(i);
            float a = Vector3.Angle(currentCp.normal, Vector3.up);

            if (currentCp.point.y < transform.position.y - ((capsule.height / 2) - capsule.radius * 0.95f))
            {
                if (!IsGrounded)
                {
                    IsGrounded = true;
                    Advanced.stairMiniHop = false;
                }

                if (Advanced.MaxSlopeAngle > 0)
                {
                    if (a < 5.1f)
                    {
                        Advanced.IsTouchingFlat = true;
                        Advanced.IsTouchingWalkable = true;
                    }
                    else if (a < Advanced.MaxSlopeAngle + 0.1f)
                        Advanced.IsTouchingWalkable = true;
                    else if (a < 90)
                        Advanced.IsTouchingUpright = true;

                    Advanced.CurntGroundNormal = currentCp.normal;
                }
            }
        }
    }
    private void OnCollisionExit(Collision CollisionData)
    {
        IsGrounded = false;
        if (Advanced.MaxSlopeAngle > 0)
        {
            Advanced.CurntGroundNormal = Vector3.up;
            Advanced.LastKnownSlopeAngle = 0;
            Advanced.IsTouchingWalkable = false;
            Advanced.IsTouchingUpright = false;
        }
    }
    public void SetState(State s)
    {
        bool isLocked = (s == State.locked);
        switch (s)
        {
            case State.unlocked:
                _fpsRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
                break;

            case State.locked:
                _fpsRigidbody.constraints = RigidbodyConstraints.FreezeAll;
                _fpsRigidbody.velocity = Vector3.zero;
                break;
        }
        _fpsRigidbody.useGravity = !isLocked;
    }
    public void SetPosAndRot(Transform point)
    {
        transform.position = point.position;
        transform.rotation = point.rotation;

        followAngles = targetAngles = point.eulerAngles;
    }

    public void Rocking(Vector3 v)
    {
        followAngles += v;
        targetAngles += v;
    }
    private float ZSlant { get; set; }

    public void SetZSlant(int n)
    {
        ZSlant = n;
        float rPos = ZSlant > 0 ? -0.375f : (ZSlant < 0 ? 0.375f : 0);

        PlayerCameraTr.localPosition = Vector3.MoveTowards(PlayerCameraTr.localPosition, new Vector3(rPos, 0, 0), Time.fixedDeltaTime * 2);
    }

    private float additionalBraking = 1;//добавляемая скорость при перегузе
    public void SetBraking(float b) => additionalBraking = b;
    public class StepPlayer
    {
        public enum TypeOfMovement { None, Walk, Run, JumpLand, JumpStart }
        public enum Layers { Rock, Sand, Leaves, LeavesOld, Swamp, 
            Grass, Moss, MossRock, DirtyGround, VeryDirtyGround, Tile, VeryLeaves, VeryTile, VeryGroundTile
        }
        private FirstPersonController fpc;
        private Dictionary<(TypeOfMovement type, int matIndex), List<AudioClip>> stepSounds;
        private AudioSource stepPlayerSource;
        private readonly TerrainDetector terrainDetector;
        public StepPlayer(TerrainDetector detector) => terrainDetector = detector;
        internal void OnInit(FirstPersonController firstPersonController)
        {
            List<AudioClip> LoadAsset(Layers l, TypeOfMovement type) =>
               Resources.LoadAll<AudioClip>($"Footsteps\\{l}\\{type}\\").ToList();

            fpc = firstPersonController;
            fpc.PlayerStepEvent += PlayStepClip;

            stepSounds = new Dictionary<(TypeOfMovement type, int matIndex), List<AudioClip>>();
            for (int k = 0; k < (System.Enum.GetNames(typeof(Layers)).Length); k++)
            {
                int matIndex = terrainDetector.GetIndexFromMaterial(Resources.Load<PhysicMaterial>($"PhysicMaterials\\{(Layers)k}"));
                for (int i = 1; i < 5; i++)
                {
                    TypeOfMovement type = (TypeOfMovement)i;                    
                    stepSounds.Add((type, matIndex), LoadAsset((Layers)k, type));
                }
            }

            stepPlayerSource = fpc.gameObject.AddComponent<AudioSource>();
            stepPlayerSource.volume = 0.25f;
            stepPlayerSource.priority = 126;
        }

        private void PlayStepClip(int physicMaterialIndex, TypeOfMovement movementType)
        {
            var key = (movementType, physicMaterialIndex);
            if (stepSounds.ContainsKey(key))
            {
                var s = stepSounds[key];
                int index = Random.Range(0, s.Count);
                if (!stepPlayerSource.isPlaying || (movementType == TypeOfMovement.JumpLand))
                {
                    stepPlayerSource.clip = s[index];
                    stepPlayerSource.Play();
                }
            }
        }

        public void OnDestroy()
        {
            fpc.PlayerStepEvent -= PlayStepClip;
        }
    }
    private void OnDestroy()
    {
        stepPlayer.OnDestroy();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(FirstPersonController)), InitializeOnLoad]
public sealed class FirstPersonController_Editor : Editor
{
    FirstPersonController t;
    SerializedObject SerT;
    private static bool showCrouchMods = false;

    private void OnEnable()
    {
        t = (FirstPersonController)target;
        SerT = new SerializedObject(t);
    }
    public override void OnInspectorGUI()
    {
        if (t.transform.localScale != Vector3.one)
        {
            t.transform.localScale = Vector3.one;
            Debug.LogWarning("Scale needs to be (1,1,1)! \n Please scale controller via Capsule collider height/raduis.");
        }
        SerT.Update();
        EditorGUILayout.Space();


        #region Movement Setup                
        EditorGUILayout.Space();
        showCrouchMods = EditorGUILayout.BeginFoldoutHeaderGroup(showCrouchMods, new GUIContent("Crouch Modifiers", "Stat modifiers that will apply when player is crouching."));
        if (showCrouchMods)
        {
            t.MCrouchModifiers.crouchKey = (KeyCode)EditorGUILayout.EnumPopup(new GUIContent("Crouch Key", "Determines what key needs to be pressed to crouch"), t.MCrouchModifiers.crouchKey);
            t.MCrouchModifiers.toggleCrouch = EditorGUILayout.ToggleLeft(new GUIContent("Toggle Crouch?", "Determines if the crouching behaviour is on a toggle or momentary basis."), t.MCrouchModifiers.toggleCrouch);
            t.MCrouchModifiers.crouchWalkSpeedMultiplier = EditorGUILayout.Slider(new GUIContent("Crouch Movement Speed Multiplier", "Determines how fast the player can move while crouching."), t.MCrouchModifiers.crouchWalkSpeedMultiplier, 0.01f, 1.5f);
            t.MCrouchModifiers.crouchJumpPowerMultiplier = EditorGUILayout.Slider(new GUIContent("Crouching Jump Power Mult.", "Determines how much the player's jumping power is increased or reduced while crouching."), t.MCrouchModifiers.crouchJumpPowerMultiplier, 0, 1.5f);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();

        EditorGUILayout.EndFoldoutHeaderGroup();
        GUI.enabled = true;
        EditorGUILayout.Space();
        #endregion
    }
}
#endif