using UnityEngine;

public class FreeFlyCamera : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public float lookSpeed = 12.0f;
    public float boostMultiplier = 2.0f;
    public float gravity = 20.0f;
    public float jumpHeight = 2.0f;

    private CharacterController characterController;
    private float rotationX = 0.0f;
    private float rotationY = 0.0f;
    private Vector3 moveDirection = Vector3.zero;
    private bool isGrounded;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // 添加CharacterController组件，如果没有的话
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            // 设置适当的碰撞器参数
            characterController.height = 1.8f;
            characterController.radius = 0.3f;
            characterController.stepOffset = 0.3f;
        }
    }

    void Update()
    {
        isGrounded = characterController.isGrounded;

        // 旋转摄像机 (鼠标移动)
        rotationX += Input.GetAxis("Mouse X") * lookSpeed;
        rotationY -= Input.GetAxis("Mouse Y") * lookSpeed;
        rotationY = Mathf.Clamp(rotationY, -90, 90);
        transform.rotation = Quaternion.Euler(rotationY, rotationX, 0);

        // 移动控制
        if (isGrounded)
        {
            // 只有在地面时才能控制水平移动
            moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            moveDirection = transform.TransformDirection(moveDirection);
            float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? boostMultiplier : 1);
            moveDirection *= speed;
            
            // 跳跃
            if (Input.GetButton("Jump"))
            {
                moveDirection.y = Mathf.Sqrt(jumpHeight * 2f * gravity);
            }
        }
        
        // 应用重力
        moveDirection.y -= gravity * Time.deltaTime;
        
        // 使用CharacterController移动，这会处理碰撞
        characterController.Move(moveDirection * Time.deltaTime);

        // ESC 释放鼠标
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}