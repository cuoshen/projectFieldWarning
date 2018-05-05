﻿using UnityEngine;

public class SlidingCameraBehaviour : MonoBehaviour {

    [Header("Translational Movement")]
    [SerializeField]
    private float panSpeed = 5f;
    [SerializeField]
    private float panLerpSpeed = 10f;

    [Header("Rotational Movement")]
    [SerializeField]
    private float horizontalRotationSpeed = 600f;
    [SerializeField]
    private float verticalRotationSpeed = 600f;
    [SerializeField]
    private float rotLerpSpeed = 10f;
    [SerializeField]
    private float maxCameraAngle = 85f;
    [SerializeField]
    private float minCameraAngle = 5f;

    [Header("Zoom Level")]
    [SerializeField]
    private float zoomSpeed = 500f;
    [SerializeField]
    private float zoomTiltSpeed = 0.3f;
    [SerializeField]
    private float minAltitude = 0.2f;
    [SerializeField]
    private float tiltThreshold = 2f;
    [SerializeField]
    private float maxAltitude = 2000;
    [SerializeField]
    private float heightSpeedScaling = 0.75f;

    // We store the camera facing and reapply it every LateUpdate() for simplicity:
    private float rotateX;
    private float rotateY;

    // Leftover translations from zoom and pan:
    private float translateX;
    private float translateZ;
    private float leftoverZoom = 0f;
    private Vector3 zoomDestination;

    // All planned transforms are actually applied to a target object, which the camera then lerps to. Maybe it is pointlessly indirect and can be refactored.
    private Vector3 targetPosition;

    private Camera cam;

    // If we allow the camera to get to height = 0 we would need special cases for the height scaling.
    private float getScaledPanSpeed() {
        return panSpeed * Time.deltaTime * Mathf.Pow(transform.position.y, heightSpeedScaling);
    }
    
    private float getScaledZoomSpeed() {
        return zoomSpeed * Time.deltaTime * Mathf.Pow(transform.position.y, heightSpeedScaling);
    }

    private void Awake() {
        cam = GetComponent<Camera>();
    }


    private void Start() {
        rotateX = transform.eulerAngles.x;
        rotateY = transform.eulerAngles.y;
        targetPosition = transform.position;
    }

    // Update() only plans movement; position/rotation are directly changed in LateUpdate().
    private void Update() {
        // Camera panning:
        translateX += Input.GetAxis("Horizontal") * getScaledPanSpeed();
        translateZ += Input.GetAxis("Vertical") * getScaledPanSpeed();

        AimedZoom();

        if (Input.GetMouseButton(2)) {
            RotateCamera();
        }
    }
    
    private void LateUpdate() {
        var dx = translateX < getScaledPanSpeed() ? translateX : getScaledPanSpeed();
        var dz = translateZ < getScaledPanSpeed() ? translateZ : getScaledPanSpeed();

        targetPosition += transform.TransformDirection(dx * Vector3.right);

        // If we move forward in local space, camera will also change altitude. To properly move forward, we have to rotate the forward vector to be horizontal in world space while keeping the magnitude:
        var worldForward = transform.TransformDirection(Vector3.forward);
        var angle = Quaternion.FromToRotation(worldForward, new Vector3(worldForward.x, 0, worldForward.z));
        targetPosition += angle * worldForward * dz;

        translateX -= dx;
        translateZ -= dz;

        // Apply zoom movement:
        var dzoom = Mathf.Abs(leftoverZoom) < getScaledZoomSpeed() ? leftoverZoom : getScaledZoomSpeed();
        var oldAltitude = targetPosition.y;

        // Zoom in:
        if (dzoom > 0) {
            ApplyZoomIn(dzoom);
        } else if (dzoom < 0) {
            ApplyZoomOut(dzoom);
        }
        
        leftoverZoom -= dzoom;
        ClampCameraAltitude();
        TiltCameraIfNearGround(oldAltitude);

        // It is mathematically incorrect to directly lerp on deltaTime like this, since we never get to the target (except by rounding I guess):
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * panLerpSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(rotateX, rotateY, 0f), Time.deltaTime * rotLerpSpeed);
    }

    /*
     * When zooming in we gradually approach whatever the cursor is pointing at.
     */
    private void AimedZoom() {
        var scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll == 0)
            return;

        // Zoom toward cursor:
        if (scroll > 0) {
            // Use a ray from the cursor to find what we'll be zooming into:
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // If the cursor is not pointing at anything, zooming in is forbidden:
            if (!Physics.Raycast(ray, out hit)) {
                return;
            }
            zoomDestination = hit.point;
        }
        
        leftoverZoom += scroll * getScaledZoomSpeed();
    }

    private void RotateCamera() {
        rotateX += -Input.GetAxis("Mouse Y") * verticalRotationSpeed * Time.deltaTime;
        rotateY += Input.GetAxis("Mouse X") * horizontalRotationSpeed * Time.deltaTime;

        // So we don't look too far up or down:
        rotateX = Mathf.Clamp(rotateX, minCameraAngle, maxCameraAngle);
    }

    private void ApplyZoomIn(float dzoom) {
        targetPosition = Vector3.MoveTowards(targetPosition, zoomDestination, dzoom);
    }

    private void ApplyZoomOut(float dzoom) {
        Vector3 diagonallyBackAndUp = Vector3.up + Vector3.back;
        Quaternion rotateToCamFacing = Quaternion.AngleAxis(rotateY, Vector3.up);
        targetPosition -= rotateToCamFacing * diagonallyBackAndUp * dzoom;
    }

    // Camera looks down when high and up when low:
    private void TiltCameraIfNearGround(float oldAltitude) {
        if (transform.position.y < tiltThreshold || targetPosition.y < tiltThreshold) {

            rotateX +=  targetPosition.y - oldAltitude * zoomTiltSpeed;
            rotateX = Mathf.Clamp(rotateX, minCameraAngle, maxCameraAngle);
        }
    }

    private void ClampCameraAltitude() {
        targetPosition = new Vector3(targetPosition.x,
                                             Mathf.Clamp(targetPosition.y, minAltitude, maxAltitude),
                                             targetPosition.z);
    }
}