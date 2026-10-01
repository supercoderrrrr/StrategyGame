using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System;

public class CameraController : MonoBehaviour
{
    //Set limits to camera of where it can move to
    [SerializeField] private bool useMovementLimits = true;
    [SerializeField] private float minX = -20f;
    [SerializeField] private float maxX = 20f;
    [SerializeField] private float minZ = -20f;
    [SerializeField] private float maxZ = 20f;

    [SerializeField] private float minFollowYOffset = 2f;
    [SerializeField] private float maxFollowYOffset = 25f;
    [SerializeField] private CinemachineVirtualCamera cinemachineVirtualCamera;
    [SerializeField] private float smoothTime = 0.2f;

    [Header("Selected Unit Visibility")]
    [SerializeField] private LayerMask wallOcclusionLayerMask;
    [SerializeField] private float wallDissolveSpeed = 1.1f;
    [SerializeField] private float wallRestoreSpeed = 1.6f;
    [Range(.05f, .45f)][SerializeField] private float wallCutoutRadius = .27f;
    [Range(.002f, .05f)][SerializeField] private float wallCutoutEdgeWidth = .012f;
    [ColorUsage(true, true)][SerializeField] private Color dissolveEdgeColor = new Color(.1f, 1.5f, 2.5f, 1f);

    //Free look settings
    [SerializeField] private float mouseSensitivity = 3f;
    private float pitch;
    private float yaw;

    private CinemachineTransposer cinemachineTransposer;
    private Vector3 targetFollowOffset;
    private Camera gameplayCamera;
    private CameraOcclusionDissolver occlusionDissolver;

    private bool isGameActive = true;

    private bool isFocusing = false;
    private Vector3 focusTargetPosition;
    private Vector3 currentVelocity;

    //Camera follows enemy the whole time
    private Transform followUnitTransform;

    private void Start()
    {
        cinemachineTransposer = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineTransposer>();
        targetFollowOffset = cinemachineTransposer.m_FollowOffset;
        gameplayCamera = Camera.main;

        if (wallOcclusionLayerMask.value == 0)
        {
            wallOcclusionLayerMask = LayerMask.GetMask("Obstacles");
        }

        occlusionDissolver = new CameraOcclusionDissolver(
            wallOcclusionLayerMask,
            wallDissolveSpeed,
            wallRestoreSpeed,
            wallCutoutRadius,
            wallCutoutEdgeWidth,
            dissolveEdgeColor);

        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMissionVictory += MissionManager_OnGameEnded;
            MissionManager.Instance.OnMissionDefeat += MissionManager_OnGameEnded;
        }

        if (UnitActionSystem.Instance != null)
        {
            UnitActionSystem.Instance.OnSelectedUnitChanged += UnitActionSystem_OnSelectedUnitChanged;
        }

        BaseAction.OnAnyActionStart += BaseAction_OnAnyActionStart;
        BaseAction.OnAnyActionCompleted += BaseAction_OnAnyActionCompleted;

        Vector3 angles = transform.eulerAngles;
        pitch = angles.x;
        yaw = angles.y;

        FocusSelectedUnit(true);
    }

    private void Update()
    {
        if(!isGameActive) return;

        HandleMovement();
        HandleRotation();
        HandleZoom();
    }

    private void LateUpdate()
    {
        if (gameplayCamera == null) gameplayCamera = Camera.main;
        Unit selectedUnit = UnitActionSystem.Instance != null
            ? UnitActionSystem.Instance.GetSelectedUnit()
            : null;
        occlusionDissolver?.Update(gameplayCamera, selectedUnit, Time.deltaTime);
    }

    private void MissionManager_OnGameEnded(object sender, EventArgs e)
    {
        isGameActive = false;
    }

    private void UnitActionSystem_OnSelectedUnitChanged(object sender, EventArgs e)
    {
        FocusSelectedUnit(false);
    }

    private void FocusSelectedUnit(bool instant)
    {
        Unit selectedUnit = UnitActionSystem.Instance != null
            ? UnitActionSystem.Instance.GetSelectedUnit()
            : null;
        if (selectedUnit != null)
        {
            followUnitTransform = null;
            FocusOnPosition(selectedUnit.GetWorldPosition(), instant);
        }
    }

    private void BaseAction_OnAnyActionStart(object sender, EventArgs e)
    {
        if (sender is BaseAction action)
        {
            Unit unit = action.GetUnit();

            if (unit.IsEnemy())
            {
                followUnitTransform = unit.transform;
                FocusOnPosition(unit.GetWorldPosition());
            }
            else
            {
                followUnitTransform = null;
            }
        }
    }

    private void BaseAction_OnAnyActionCompleted(object sender, EventArgs e)
    {
        if (sender is BaseAction action)
        {
            if (action.GetUnit().transform == followUnitTransform)
            {
                followUnitTransform = null;
            }
        }
    }

    private void FocusOnPosition(Vector3 targetPosition, bool instant = false)
    {
        /*Vector3 newPosition = targetPosition;
        newPosition.y = transform.position.y;

        transform.position = newPosition;*/
        focusTargetPosition = targetPosition;

        if (useMovementLimits)
        {
            focusTargetPosition.x = Mathf.Clamp(focusTargetPosition.x, minX, maxX);
            focusTargetPosition.z = Mathf.Clamp(focusTargetPosition.z, minZ, maxZ);
        }

        isFocusing = true;

        if (instant)
        {
            transform.position = focusTargetPosition;
            currentVelocity = Vector3.zero;
            isFocusing = false;
        }

    }

    private void HandleMovement()
    {
        Vector2 inputMoveDir = InputManager.Instance.GetCameraMoveVector();

        if (inputMoveDir != Vector2.zero)
        {
            isFocusing = false;

            float moveSpeed = 10f;

            Vector3 forward = transform.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 right = transform.right;
            right.y = 0;
            right.Normalize();

            Vector3 moveVector = forward * inputMoveDir.y + right * inputMoveDir.x;

            Vector3 newPosition = transform.position + moveVector * moveSpeed * Time.deltaTime;

            //Vector3 moveVector = transform.forward * inputMoveDir.y + transform.right * inputMoveDir.x;

            //Vector3 newPosition = transform.position + moveVector * moveSpeed * Time.deltaTime;

            if (useMovementLimits)
            {
                newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
                newPosition.z = Mathf.Clamp(newPosition.z, minZ, maxZ);
            }

            transform.position =newPosition;
        }
        else {
            if (followUnitTransform != null)
            {
                focusTargetPosition = followUnitTransform.position;

                if (useMovementLimits)
                {
                    focusTargetPosition.x = Mathf.Clamp(focusTargetPosition.x, minX, maxX);
                    focusTargetPosition.z = Mathf.Clamp(focusTargetPosition.z, minZ, maxZ);
                }

                isFocusing = true;
            }

            if (isFocusing)
            {
                transform.position = Vector3.SmoothDamp(transform.position, focusTargetPosition, ref currentVelocity, smoothTime);

                if (followUnitTransform == null && Vector3.Distance(transform.position, focusTargetPosition) < 0.05f)
                {
                    transform.position = focusTargetPosition;
                    isFocusing = false;
                }
            }
        }
    }

    private void HandleRotation()
    {
        /*Vector3 rotationVector = new Vector3(0, 0, 0);

        rotationVector.y = InputManager.Instance.GetCameraRotateAmount();

        float rotationSpeed = 100f;
        transform.eulerAngles += rotationVector * rotationSpeed * Time.deltaTime;*/
        if (Input.GetMouseButton(2)) // 2 代表鼠标中键 (滚轮按下去)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            yaw += mouseX * mouseSensitivity;
            pitch -= mouseY * mouseSensitivity;
           
            pitch = Mathf.Clamp(pitch, -89f, 89f);
        }
        else
        {
            float rotationAmount = InputManager.Instance.GetCameraRotateAmount();
            float rotationSpeed = 100f;
            yaw += rotationAmount * rotationSpeed * Time.deltaTime;
        }
        transform.eulerAngles = new Vector3(pitch, yaw, 0f);
    }

    private void HandleZoom()
    {
        float zoomIncreaseAmount = 1f;
        targetFollowOffset.y += InputManager.Instance.GetCameraZoomAmount() * zoomIncreaseAmount;

        targetFollowOffset.y = Mathf.Clamp(targetFollowOffset.y, minFollowYOffset, maxFollowYOffset);

        float zoomSpeed = 5f;
        cinemachineTransposer.m_FollowOffset = Vector3.Lerp(cinemachineTransposer.m_FollowOffset, targetFollowOffset, Time.deltaTime * zoomSpeed);
    }

    private void OnDrawGizmos()
    {
        if (!useMovementLimits) return;

        Gizmos.color = Color.red;

        float centerX = (minX + maxX) / 2f;
        float centerZ = (minZ + maxZ) / 2f;
        float sizeX = maxX - minX;
        float sizeZ = maxZ - minZ;

        Vector3 center = new Vector3(centerX, transform.position.y, centerZ);
        Vector3 size = new Vector3(sizeX, 0.1f, sizeZ);

        Gizmos.DrawWireCube(center, size);
    }

    private void OnDestroy()
    {
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMissionVictory -= MissionManager_OnGameEnded;
            MissionManager.Instance.OnMissionDefeat -= MissionManager_OnGameEnded;
        }

        if (UnitActionSystem.Instance != null)
        {
            UnitActionSystem.Instance.OnSelectedUnitChanged -= UnitActionSystem_OnSelectedUnitChanged;
        }

        BaseAction.OnAnyActionStart -= BaseAction_OnAnyActionStart;
        BaseAction.OnAnyActionCompleted -= BaseAction_OnAnyActionCompleted;

        occlusionDissolver?.Dispose();
        occlusionDissolver = null;
    }
}
