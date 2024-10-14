using System.Collections.Generic;
using System.Collections;
using UnityEngine;


public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;               // 移动速度
    public float jumpForce = 5f;               // 跳跃力量
    public float bigJumpMultiplier = 1.5f;     // 大跳的跳跃倍数
    public float dashSpeed = 10f;              // 冲刺速度
    public float maxDashDistance = 5f;         // 冲刺的最大距离
    public LayerMask shadowLayer;              // 阴影图层，用于限制冲刺
    public float attackRange = 1f;             // 攻击范围
    public float attackCooldown = 0.5f;        // 攻击冷却时间
    public Transform attackPoint;              // 攻击检测点
    public LayerMask enemyLayer;               // 敌人图层
    public Camera mainCamera;                  // 需要在Inspector中关联相机
    public float lookUpAngle = 3f;             // 抬头的角度
    public float lookDownAngle = -3f;          // 低头的角度
    public LayerMask groundLayer;              // 地面层
    public Transform groundCheck;              // 地面检测点
    public float groundCheckRadius = 0.2f;     // 地面检测半径

    private Rigidbody2D rb;
    private bool isJumping = false;            //标记 跳跃
    private bool isDashing = false;            //标记 冲刺
    private bool isAttacking = false;          //标记 攻击
    private bool canDashAttack = false;        //标记 冲刺攻击
    private float dashDistanceTravelled = 0f;
    private float jumpPressTime = 0f;          // 跳跃按键时间记录
    private bool isBigJump = false;            //标记 大跳
    private Animator animator;
    private bool isGrounded;                   // 是否在地面上

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void FixedUpdate()
    {
        HandleMovement();       // 移动控制 (AD左右移动，W抬头，S向下看)
        HandleInteractions();   // 交互控制 (R键交互，交互期间禁用其他操作)
        OnDrawGizmos();         // 在Scene视图中绘制地面检测的范围
    }
    private void Update()
    {
        isGrounded = IsGrounded();  //更新是否在地面上的状态
        HandleJump();               // 跳跃控制 (短按小跳，长按大跳，允许空中变向)
        HandleAttack();             // 攻击控制 (左键攻击，W+左键向上攻击)
        HandleDash();               // 冲刺控制 (E键冲刺，阴影中受限，冲刺攻击)
        HandleSkill();              // 技能控制 (C键技能)
    }

    // 移动控制 (AD左右移动，W抬头，S向下看)
    void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        Vector2 moveDirection = new Vector2(moveX * moveSpeed, rb.velocity.y);

        rb.velocity = moveDirection;

        // 调整角色朝向
        if (moveX < 0) transform.localScale = new Vector3(-1, 1, 1);
        else if (moveX > 0) transform.localScale = new Vector3(1, 1, 1);

        // 更新动画参数
        animator.SetFloat("Speed", Mathf.Abs(moveX));

        // W抬头，S向下看（这可以是相机或角色动作调整）
        if (Input.GetKey(KeyCode.W))
        {
            // W抬头逻辑，比如调整相机
            mainCamera.transform.localEulerAngles = new Vector3(lookDownAngle, 0, 0);
        }
        else if (Input.GetKey(KeyCode.S))
        {
            // S向下看逻辑
            mainCamera.transform.localEulerAngles = new Vector3(lookUpAngle, 0, 0);
        }
        else
        {
            // 恢复相机角度
            mainCamera.transform.localEulerAngles = Vector3.zero; // 或者其他默认角度
        }
        mainCamera.transform.localEulerAngles = Vector3.Lerp(mainCamera.transform.localEulerAngles, Vector3.zero, Time.deltaTime * 5f);
    }

    // 跳跃控制 (短按小跳，长按大跳，允许空中变向)
    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && IsGrounded())
        {
            isJumping = true;
            jumpPressTime = 0f;
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
        if (Input.GetButton("Jump") && isJumping)
        {
            jumpPressTime += Time.deltaTime;
            if (jumpPressTime >= 0.5f)
            {
                isBigJump = true;
            }
        }
        if (Input.GetButtonUp("Jump"))
        {
            isJumping = false;
            if (isBigJump)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpForce * bigJumpMultiplier);
                isBigJump = false;
            }
        }
    }

    // 攻击控制 (左键攻击，W+左键向上攻击)
    void HandleAttack()
    {
        if (Input.GetButtonDown("Fire1") && !isAttacking)
        {
            isAttacking = true;
            animator.SetTrigger("Attack");
            StartCoroutine(AttackCooldown());

            // 攻击方向控制
            if (Input.GetKey(KeyCode.W))
            {
                // 向上攻击
                PerformAttack(Vector2.up);
            }
            else
            {
                // 正常攻击
                PerformAttack(Vector2.right * transform.localScale.x);
            }
        }
    }

    IEnumerator AttackCooldown()
    {
        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
    }

    void PerformAttack(Vector2 attackDirection)
    {
        // 检测攻击命中
        RaycastHit2D hit = Physics2D.Raycast(attackPoint.position, attackDirection, attackRange, enemyLayer);
        if (hit.collider != null)
        {
            // 对敌人造成伤害
            Debug.Log("Hit: " + hit.collider.name);
        }
        // 检测与场景物体的交互
        // TODO: 场景物体交互处理
    }

    // 冲刺控制 (E键冲刺，阴影中受限，冲刺攻击)
    void HandleDash()
    {
        if (Input.GetKeyDown(KeyCode.E) && !isDashing && !IsInShadow())
        {
            isDashing = true;
            dashDistanceTravelled = 0f;
            StartCoroutine(DashCoroutine());
        }

        // 冲刺过程中0.5秒内按左键发动冲刺攻击
        if (canDashAttack && Input.GetButtonDown("Fire1"))
        {
            // 冲刺攻击逻辑
            Debug.Log("Dash Attack!");
        }
    }

    IEnumerator DashCoroutine()
    {
        float dashTime = maxDashDistance / dashSpeed;
        while (dashTime > 0)
        {
            rb.velocity = new Vector2(transform.localScale.x * dashSpeed, 0);
            dashDistanceTravelled += Time.deltaTime * dashSpeed;
            dashTime -= Time.deltaTime;
            yield return null;
        }
        isDashing = false;
        canDashAttack = true;
        yield return new WaitForSeconds(0.5f);
        canDashAttack = false;
    }

    bool IsInShadow()
    {
        // 检查角色是否在阴影层中
        // TODO: 实现阴影层检测逻辑
        return false;
    }

    // 交互控制 (R键交互，交互期间禁用其他操作)
    void HandleInteractions()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            // 弹出交互对话框，禁用其他控制
            Debug.Log("Interact!");
        }
    }

    // 技能控制 (C键技能)
    void HandleSkill()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            // 释放技能逻辑
            Debug.Log("Skill Activated!");
        }
    }

    bool IsGrounded()
    {
        // 检测角色是否在地面
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }
    private void OnDrawGizmos()
    {
        // 在Scene视图中绘制地面检测的范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }

}

