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
    private Vector2 rotationSpeed;

    private float normalizedTargetZoom = .5f;


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
        DoRotation();
        DoMovement();
        DoZoom();
    }


    private void DoMovement()
    {
        float speed = movementSettings.movementSpeed * (Input.GetKey(KeyCode.LeftShift) ? movementSettings.sprintSpeedMultiplier : 1) * Time.deltaTime;
        Vector3 movementInput = Quaternion.Euler(0, CurrentRotation.y, 0) * new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        Vector3 desiredPosition = transform.position + movementInput * speed;

        if (movementSettings.surfaceFollowType != MovementSettings.SurfaceFollowType.None)
        {
            if (Physics.Raycast(desiredPosition + Vector3.up * movementSettings.surfaceCheckRange, Vector3.down, out RaycastHit hit, movementSettings.surfaceCheckRange * 2f, movementSettings.groundMask))
            {
                switch (movementSettings.surfaceFollowType)
                {
                    case MovementSettings.SurfaceFollowType.MatchSurfaceInstant:
                        transform.position = hit.point;
                        break;
                    case MovementSettings.SurfaceFollowType.MatchSurfaceSmooth:

                        transform.position = Vector3.Lerp(desiredPosition, hit.point, 1 - Mathf.Pow(movementSettings.smoothness * movementSettings.smoothness * .02f, Time.deltaTime));
                        break;
                }
            }
            else if (movementSettings.allowFlight)
                transform.position = desiredPosition;
        }
        else
            transform.position = desiredPosition;
    }

    private void DoRotation()
    {
        if (Input.GetMouseButton((int)rotationSettings.rotationButton))
        {
            Vector2 rotationInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * rotationSettings.rotationSensitivity;
            Vector2 desiredRotationSpeed;
            desiredRotationSpeed.x = -rotationInput.y;
            desiredRotationSpeed.y = rotationInput.x;
            desiredRotationSpeed /= 10f;
            if (rotationSettings.easingBehaviour == RotationSettings.RotationEasing.Always)
                rotationSpeed = Vector2.Lerp(rotationSpeed, desiredRotationSpeed, 1 - Mathf.Pow(rotationSettings.smoothness * rotationSettings.smoothness * .2f, Time.deltaTime));
            else
                rotationSpeed = desiredRotationSpeed;
        }
        else if (rotationSettings.easingBehaviour != RotationSettings.RotationEasing.None)
        {
            rotationSpeed = Vector2.Lerp(rotationSpeed, Vector2.zero, 1 - Mathf.Pow(rotationSettings.smoothness * rotationSettings.smoothness * .1f, Time.deltaTime));
        }
        else rotationSpeed = Vector2.zero;

        float rX = CurrentRotation.x > 180 ? CurrentRotation.x - 360 : CurrentRotation.x;
        rX += rotationSpeed.x;
        if (rotationSettings.constrainX)
            rX = Mathf.Clamp(rX, rotationSettings.rotationConstraintsX.x, rotationSettings.rotationConstraintsX.y);

        float rY = CurrentRotation.y + rotationSpeed.y;
        if (rotationSettings.constrainY)
            rY = Mathf.Clamp(rY, rotationSettings.rotationConstraintsY.x, rotationSettings.rotationConstraintsY.y);

        transform.rotation = Quaternion.Euler(rX, rY, CurrentRotation.z);

    }


    private void DoZoom()
    {
        float absoluteTargetZoom = Mathf.Lerp(zoomSettings.zoomRange.x, zoomSettings.zoomRange.y, normalizedTargetZoom);

        absoluteTargetZoom = Mathf.Lerp(absoluteTargetZoom, absoluteTargetZoom - Input.GetAxis("Mouse ScrollWheel") * zoomSettings.zoomSensitivity * (zoomSettings.zoomRange.y - zoomSettings.zoomRange.x), .3f);

        switch (zoomSettings.collisionDetection)
        {
            case ZoomSettings.CollisionDetectionMethod.None:
                break;

            case ZoomSettings.CollisionDetectionMethod.SweepTest:
                //sweep test, if number of intersections is odd, camera is inside mesh
                bool hitBackfaces = Physics.queriesHitBackfaces;
                Physics.queriesHitBackfaces = true;
                if (Physics.RaycastAll(transform.position - absoluteTargetZoom * transform.forward, Vector3.up, 1000f, zoomSettings.collisionLayerMask).Length % 2 == 1)
                {

                    if (Physics.Raycast(transform.position - absoluteTargetZoom * transform.forward, transform.forward, out RaycastHit backfaceHit, absoluteTargetZoom, zoomSettings.collisionLayerMask))
                        absoluteTargetZoom = Vector3.Distance(backfaceHit.point, transform.position) - .05f;
                }
                Physics.queriesHitBackfaces = hitBackfaces;
                break;

            case ZoomSettings.CollisionDetectionMethod.RaycastFromCenter:
                if (Physics.Raycast(transform.position, -transform.forward, out RaycastHit hit, absoluteTargetZoom, zoomSettings.collisionLayerMask))
                {

                }
                break;
        }
        cameraComponent.transform.localPosition = Vector3.Lerp(cameraComponent.transform.localPosition, Vector3.back * absoluteTargetZoom, .3f);
        normalizedTargetZoom = Mathf.InverseLerp(zoomSettings.zoomRange.x, zoomSettings.zoomRange.y, absoluteTargetZoom);
    }
}

[System.Serializable]
public class MovementSettings
{
    public float movementSpeed = 3f;
    [Tooltip("modifies speed when holding 'left-shift'")]
    public float sprintSpeedMultiplier = 2f;

    [Tooltip("if false, constraints the controller to surfaces")]
    public bool allowFlight = false;
    public enum SurfaceFollowType { None, MatchSurfaceInstant, MatchSurfaceSmooth }
    [Tooltip("controls how the controller follows surface heights")]
    public SurfaceFollowType surfaceFollowType;
    [Tooltip("maximum height difference")]
    public float surfaceCheckRange = 50f;

    [Tooltip("Layer mask containing the ground layer")]
    public LayerMask groundMask;
    [Tooltip("speed at which the controller follows the surface if MatchSurfaceSmooth is active")]
    public float smoothness = 1f;
}

[System.Serializable]
public class RotationSettings
{
    public enum RotationEasing { None, Always, Subtle }
    public RotationEasing easingBehaviour;
    public enum MouseButton { Left = 0, Right = 1, Middle = 2 }
    public MouseButton rotationButton;
    public float rotationSensitivity = 24f;
    public float smoothness = 1f;
    public bool constrainX, constrainY;
    public Vector2 rotationConstraintsX, rotationConstraintsY;
}



[System.Serializable]
public class ZoomSettings
{
    public Vector2 zoomRange = new Vector2(1f, 15f);
    public bool autoZoomIn = true;
    public enum CollisionDetectionMethod { None, RaycastFromCenter, SweepTest }
    public CollisionDetectionMethod collisionDetection = CollisionDetectionMethod.SweepTest;
    public float zoomSensitivity = 4f;
    public LayerMask collisionLayerMask;

}