using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitCameraController : MonoBehaviour
{
    private Camera cameraComponent;

    public RotationSettings rotationSettings;
    public ZoomSettings zoomSettings;
    public MovementSettings movementSettings;

    private Vector3 CurrentRotation { get { return transform.eulerAngles; } }
    private Vector3 keyboardInput { get { return new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")); } }
    private bool IsMouseDown { get { return Input.GetMouseButton((int)rotationSettings.rotationButton); } }
    private Vector2 rotationSpeed;
    private Vector2 oldMousePosition;

    private float normalizedTargetZoom = .5f;

    private float AbsoluteTargetZoom { get { return Mathf.Lerp(zoomSettings.zoomRange.x, zoomSettings.zoomRange.y, normalizedTargetZoom); } set { normalizedTargetZoom = Mathf.InverseLerp(zoomSettings.zoomRange.x, zoomSettings.zoomRange.y, value); } }



    private enum CameraControllerState
    {
        Free,
        MovRot,
        Pan,
        Zoom
    }
    private CameraControllerState state;


    public void OnEnable()
    {
        cameraComponent = GetComponentInChildren<Camera>();
        if (!cameraComponent)
        {
            enabled = false;
            Debug.LogError("no camera found!");
        }
        if (cameraComponent.transform == transform)
        {
            enabled = false;
            Debug.LogError("camera component needs to be on a child gameObject");
        }
    }

    public void Update()
    {
        UpdateState();
        switch (state)
        {
            case CameraControllerState.Free:
                DoMouseWheelZoom();
                DoKeyboardMovement();
                break;
            case CameraControllerState.MovRot:
                DoKeyboardMovement();
                DoMouseWheelZoom();
                DoMouseRotation();
                break;
            case CameraControllerState.Pan:
                if (IsMouseDown)
                    DoMousePan();
                break;
            case CameraControllerState.Zoom:
                if (IsMouseDown)
                    DoMouseZoom();
                break;
        }
        UpdateRotation();
        UpdateZoom();
        DecreaseRotationSpeed();
        oldMousePosition = Input.mousePosition;
    }

    private void UpdateState()
    {
        switch (state)
        {
            case CameraControllerState.Free:
                if (keyboardInput.sqrMagnitude > .01f || (IsMouseDown && !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftControl))))
                {
                    state = CameraControllerState.MovRot;
                    return;
                }
                break;
            case CameraControllerState.MovRot:
                if (IsMouseDown || keyboardInput.sqrMagnitude < .1f || Input.GetKey(KeyCode.LeftShift))                                    
                    return;
                break;
        }
        if (IsMouseDown && Input.GetKey(KeyCode.LeftShift))
        {
            state = CameraControllerState.Pan;
            return;
        }
        if (IsMouseDown && Input.GetKey(KeyCode.LeftControl))
        {
            state = CameraControllerState.Zoom;
            return;
        }
        state = CameraControllerState.Free;
    }

    private void DoKeyboardMovement()
    {
        float speed = movementSettings.movementSpeed * (Input.GetKey(KeyCode.LeftShift) ? movementSettings.sprintSpeedMultiplier : 1) * Time.deltaTime;
        Vector3 movementInput = Quaternion.Euler(0, CurrentRotation.y, 0) * new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        Vector3 desiredPosition = transform.position + movementInput * speed;
        Vector3 finalPosition = desiredPosition;

        //match surface height
        if (movementSettings.surfaceFollowType != MovementSettings.SurfaceFollowType.None)
        {
            if (Physics.Raycast(desiredPosition + Vector3.up * movementSettings.surfaceCheckRange, Vector3.down, out RaycastHit hit, movementSettings.surfaceCheckRange * 2f, movementSettings.groundMask))
            {
                switch (movementSettings.collisionDetection)
                {
                    case MovementSettings.CollisionDetectionMethod.None:
                        finalPosition = hit.point;
                        break;
                    case MovementSettings.CollisionDetectionMethod.SweepTest:
                        bool hitBackfaces = Physics.queriesHitBackfaces;
                        Physics.queriesHitBackfaces = true;
                        if (Physics.RaycastAll(desiredPosition, Vector3.up, movementSettings.surfaceCheckRange, movementSettings.groundMask).Length % 2 == 1)
                        {
                            Physics.queriesHitBackfaces = false;
                            float upperHitDistance = movementSettings.surfaceCheckRange;
                            if (Physics.Raycast(desiredPosition, Vector3.down, out hit, movementSettings.surfaceCheckRange, movementSettings.groundMask))
                            {
                                finalPosition = hit.point;
                                upperHitDistance = hit.distance;
                            }
                            Physics.queriesHitBackfaces = true;
                            if (Physics.Raycast(desiredPosition, Vector3.up, out hit, movementSettings.surfaceCheckRange, movementSettings.groundMask))
                                if (hit.distance < upperHitDistance)
                                    finalPosition = hit.point;
                        }
                        else if (Physics.Raycast(desiredPosition, Vector3.down, out hit, movementSettings.surfaceCheckRange, movementSettings.groundMask))
                            finalPosition = hit.point;
                        Physics.queriesHitBackfaces = hitBackfaces;
                        break;
                }
            }
            switch (movementSettings.surfaceFollowType)
            {
                case MovementSettings.SurfaceFollowType.MatchSurfaceInstant:
                    desiredPosition = finalPosition;
                    break;
                case MovementSettings.SurfaceFollowType.MatchSurfaceSmooth:
                    desiredPosition = Vector3.Lerp(desiredPosition, finalPosition, 1 - Mathf.Pow(movementSettings.smoothness * movementSettings.smoothness * .02f, Time.deltaTime));
                    break;
            }
        }
        transform.position = desiredPosition;
    }

    private void DoMousePan()
    {
        Vector2 startMousePosition = oldMousePosition;
        Vector2 endMousePosition = Input.mousePosition;
        Vector3 deltaPosition = ScreenPointToWorldXZPlane(transform.position.y, endMousePosition) - ScreenPointToWorldXZPlane(transform.position.y, startMousePosition);
        deltaPosition = Vector3.ClampMagnitude(deltaPosition, zoomSettings.zoomRange.y * zoomSettings.zoomRange.y * Time.deltaTime);
        transform.position -= deltaPosition;
    }

    private Vector3 ScreenPointToWorldXZPlane(float worldHeight, Vector3 screenPoint)
    {
        Ray ray = this.cameraComponent.ScreenPointToRay(screenPoint);
        float t = (ray.origin.y - worldHeight) / -ray.direction.y;
        return ray.origin + t * ray.direction;
    }

    private void DoMouseZoom()
    {
        float zoomInput = Input.GetAxis("Mouse Y") * zoomSettings.zoomSensitivity / Screen.height * 18f;
        AbsoluteTargetZoom = Mathf.Lerp(AbsoluteTargetZoom, AbsoluteTargetZoom - zoomInput * (zoomSettings.zoomRange.y - zoomSettings.zoomRange.x), .3f);
    }

    private void DoMouseWheelZoom()
    {
        float zoomInput = Input.GetAxis("Mouse ScrollWheel") * zoomSettings.zoomSensitivity;
        AbsoluteTargetZoom = Mathf.Lerp(AbsoluteTargetZoom, AbsoluteTargetZoom - zoomInput * (zoomSettings.zoomRange.y - zoomSettings.zoomRange.x), .3f);
    }

    private void DoMouseRotation()
    {
        if (!IsMouseDown)
            return;
        Vector2 rotationInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * rotationSettings.rotationSensitivity;
        Vector2 desiredRotationSpeed;
        desiredRotationSpeed.x = -rotationInput.y;
        desiredRotationSpeed.y = rotationInput.x;
        desiredRotationSpeed /= 10f;
        if (rotationSettings.easingBehaviour == RotationSettings.RotationEasing.Always)
            rotationSpeed = Vector2.Lerp(rotationSpeed, desiredRotationSpeed, 1 - Mathf.Pow(rotationSettings.smoothness * rotationSettings.smoothness * .2f / 100f, Time.deltaTime));
        else
            rotationSpeed = desiredRotationSpeed;
    }

    private void UpdateRotation()
    {
        float rX = CurrentRotation.x > 180 ? CurrentRotation.x - 360 : CurrentRotation.x;
        rX += rotationSpeed.x;
        if (rotationSettings.constrainX)
            rX = Mathf.Clamp(rX, rotationSettings.rotationConstraintsX.x, rotationSettings.rotationConstraintsX.y);

        float rY = CurrentRotation.y + rotationSpeed.y;
        if (rotationSettings.constrainY)
            rY = Mathf.Clamp(rY, rotationSettings.rotationConstraintsY.x, rotationSettings.rotationConstraintsY.y);
        transform.rotation = Quaternion.Euler(rX, rY, CurrentRotation.z);
    }

    private void DecreaseRotationSpeed()
    {
        if (rotationSettings.easingBehaviour != RotationSettings.RotationEasing.None)
        {
            rotationSpeed = Vector2.Lerp(rotationSpeed, Vector2.zero, 1 - Mathf.Pow(rotationSettings.smoothness * rotationSettings.smoothness * .1f / 100f, Time.deltaTime));
        }
        else rotationSpeed = Vector2.zero;
    }

    private void UpdateZoom()
    {
        AbsoluteTargetZoom = Mathf.Lerp(zoomSettings.zoomRange.x, zoomSettings.zoomRange.y, normalizedTargetZoom);
        switch (zoomSettings.collisionDetection)
        {
            case ZoomSettings.CollisionDetectionMethod.None:
                break;

            case ZoomSettings.CollisionDetectionMethod.SweepTest:
                //sweep test, if number of intersections is odd, camera is inside mesh
                bool hitBackfaces = Physics.queriesHitBackfaces;
                Physics.queriesHitBackfaces = true;
                if (Physics.RaycastAll(transform.position - AbsoluteTargetZoom * transform.forward, Vector3.up, 1000f, zoomSettings.collisionLayerMask).Length % 2 == 1)
                {
                    if (Physics.Raycast(transform.position - AbsoluteTargetZoom * transform.forward, transform.forward, out RaycastHit backfaceHit, AbsoluteTargetZoom + 0.05f, zoomSettings.collisionLayerMask))
                        AbsoluteTargetZoom = Vector3.Distance(backfaceHit.point, transform.position) - .05f;
                }
                Physics.queriesHitBackfaces = hitBackfaces;
                break;

            case ZoomSettings.CollisionDetectionMethod.RaycastFromCenter:
                if (Physics.Raycast(transform.position, -transform.forward, out RaycastHit hit, AbsoluteTargetZoom, zoomSettings.collisionLayerMask))
                {
                    AbsoluteTargetZoom = Vector3.Distance(hit.point, transform.position) - .05f;
                }
                break;
        }
        cameraComponent.transform.localPosition = Vector3.Lerp(cameraComponent.transform.localPosition, Vector3.back * AbsoluteTargetZoom, .3f);
        //normalizedTargetZoom = Mathf.InverseLerp(zoomSettings.zoomRange.x, zoomSettings.zoomRange.y, absoluteTargetZoom);
    }

}

[System.Serializable]
public class MovementSettings
{
    [Tooltip("Base speed of the controller. Set to '0' to disable movement")]
    public float movementSpeed = 3f;

    [Tooltip("Modifies speed when holding 'left-shift'")]
    public float sprintSpeedMultiplier = 3f;

    [Tooltip("When disabled, constraints the controller to move only on collider surfaces")]
    public bool allowFlight = false;

    public enum SurfaceFollowType { None, MatchSurfaceInstant, MatchSurfaceSmooth }
    [Tooltip("Controls how the controller follows surface heights")]
    public SurfaceFollowType surfaceFollowType = SurfaceFollowType.MatchSurfaceSmooth;

    public enum CollisionDetectionMethod { None, SweepTest }
    [Tooltip("When should the controller update the current target height?")]
    public CollisionDetectionMethod collisionDetection = CollisionDetectionMethod.SweepTest;

    [Tooltip("Maximum height difference the controller checks for new surface collisions at")]
    public float surfaceCheckRange = 50f;

    [Tooltip("Layer mask containing only the ground layer(s)")]
    public LayerMask groundMask = 1;

    [Tooltip("Delay with which the controller follows the surface if MatchSurfaceSmooth is active")]
    public float smoothness = 1f;
}

[System.Serializable]
public class RotationSettings
{
    public enum RotationEasing { None, Always, Subtle }
    [Tooltip("Controls, how the rotation input is smoothed")]
    public RotationEasing easingBehaviour = RotationEasing.Subtle;
    public enum MouseButton { Left = 0, Right = 1, Middle = 2 }
    [Tooltip("Determines, what mouse button is responsible for rotating this camera")]
    public MouseButton rotationButton = MouseButton.Middle;
    [Tooltip("Speeds up the rotation")]
    public float rotationSensitivity = 24f;
    [Tooltip("The amount of smoothing applied to the rotation input")]
    public float smoothness = 1f;
    [Tooltip("When enabled, constraints the rotation on the X axis (vertical rotation)")]
    public bool constrainX;
    [Tooltip("When enabled, constraints the rotation on the Y axis (horizontal rotation)")]
    public bool constrainY;
    [Tooltip("Lower and upper rotation angle limit")]
    public Vector2 rotationConstraintsX, rotationConstraintsY;
}



[System.Serializable]
public class ZoomSettings
{
    [Tooltip("Minimum and maximum distance of the camera to its orbit center")]
    public Vector2 zoomRange = new Vector2(1f, 15f);
    [Tooltip("Dynamicaly zooms in, to provide the camera from clipping inside of geometry")]
    public bool autoZoomIn = true;
    public enum CollisionDetectionMethod { None, RaycastFromCenter, SweepTest }
    [Tooltip("How should the controller determine, whether the camera is inside geometry or not?")]
    public CollisionDetectionMethod collisionDetection = CollisionDetectionMethod.SweepTest;
    [Tooltip("Speeds up zooming in and out")]
    public float zoomSensitivity = 4f;
    [Tooltip("Layermask used to determine, whether the camera is inside geometry or not")]
    public LayerMask collisionLayerMask = 1;

}