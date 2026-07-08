using UnityEngine;

public class MouseMovement : MonoBehaviour
{
    

    public float mouseSensitivity = 500f;

    float xRotation = 0f;
    float yRotation = 0f;

    public float topClamp = -90f;
    public float bottomClamp = 90f;

    public Vector2 LookDelta { get; private set; }
    public float Pitch => xRotation;
    public float Yaw => yRotation;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

    }

    
    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        LookDelta = new Vector2(mouseX, mouseY);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, topClamp, bottomClamp);
        yRotation += mouseX;
        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
}

