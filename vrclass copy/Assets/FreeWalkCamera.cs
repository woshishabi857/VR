using UnityEngine;

public class FreeWalkCamera : MonoBehaviour
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

    void Awake()
    {
        // 在Awake中添加CharacterController组件，确保它在Start和Update之前初始化
        if (GetComponent<CharacterController>() == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            // 设置适当的碰撞器参数
            characterController.height = 2.0f;
            characterController.radius = 0.3f;
            characterController.stepOffset = 0.3f;
        }
        else
        {
            characterController = GetComponent<CharacterController>();
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

void Update()
{
    // 先做射线检测
    float rayDist = characterController.height * 0.5f + 0.1f; 
    bool hitGround = Physics.Raycast(transform.position, Vector3.down, rayDist);
    isGrounded = hitGround;

    // 确保在地面时给一个微小下压力，保持贴地
    if (isGrounded && moveDirection.y < 0)
        moveDirection.y = -1f;

    // 水平输入不再依赖 isGrounded
    Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
    input = transform.TransformDirection(input);
    float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? boostMultiplier : 1);
    moveDirection.x = input.x * speed;
    moveDirection.z = input.z * speed;

    // 跳跃保持不变
    if (isGrounded && Input.GetButton("Jump"))
        moveDirection.y = Mathf.Sqrt(jumpHeight * 2f * gravity);

    // 重力照常
    moveDirection.y -= gravity * Time.deltaTime;

    characterController.Move(moveDirection * Time.deltaTime);
}
}