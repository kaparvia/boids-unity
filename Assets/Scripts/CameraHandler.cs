using UnityEngine;

public class CameraHandler : MonoBehaviour
{
    private const float MIN_FOV = 10f;
    private const float MAX_FOV = 80f;

    [Range(0.1f, 10f)]
    public float sensitivity = 1f;

    [Range(0.1f, 10f)]
    public float rotationSpeed = 1f;

    public Camera camera3D;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float y = Input.mouseScrollDelta.y;

        if (y != 0)
        {
            camera3D.fieldOfView = Mathf.Clamp(camera3D.fieldOfView + (y * sensitivity), MIN_FOV, MAX_FOV);
        }

        Vector3 target = Vector3.zero;
        if (Input.GetMouseButton(0))
        {
            camera3D.transform.LookAt(target);
            camera3D.transform.RotateAround(target, Vector3.up, Input.GetAxis("Mouse X") * rotationSpeed);
            camera3D.transform.RotateAround(target, Vector3.right, Input.GetAxis("Mouse Y") * rotationSpeed);
        }
    }
}
