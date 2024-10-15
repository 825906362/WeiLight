using System.Collections.Generic;
using System.Collections;
using UnityEngine;


public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;               // �ƶ��ٶ�
    public float jumpForce = 5f;               // ��Ծ����
    public float bigJumpMultiplier = 1.5f;     // ��������Ծ����
    public float dashSpeed = 10f;              // ����ٶ�
    public float maxDashDistance = 5f;         // ��̵�������
    public LayerMask shadowLayer;              // ��Ӱͼ�㣬�������Ƴ��
    public float attackRange = 1f;             // ������Χ
    public float attackCooldown = 0.5f;        // ������ȴʱ��
    public Transform attackPoint;              // ��������
    public LayerMask enemyLayer;               // ����ͼ��
    public Camera mainCamera;                  // ��Ҫ��Inspector�й������
    public float lookUpAngle = 3f;             // ̧ͷ�ĽǶ�
    public float lookDownAngle = -3f;          // ��ͷ�ĽǶ�
    public LayerMask groundLayer;              // �����
    public Transform groundCheck;              // �������
    public float groundCheckRadius = 0.2f;     // ������뾶

    private Rigidbody2D rb;
    private bool isJumping = false;            //��� ��Ծ
    private bool isDashing = false;            //��� ���
    private bool isAttacking = false;          //��� ����
    private bool canDashAttack = false;        //��� ��̹���
    private float dashDistanceTravelled = 0f;
    private float jumpPressTime = 0f;          // ��Ծ����ʱ���¼
    private bool isBigJump = false;            //��� ����
    private Animator animator;
    private bool isGrounded;                   // �Ƿ��ڵ�����

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void FixedUpdate()
    {
        HandleMovement();       // �ƶ����� (AD�����ƶ���W̧ͷ��S���¿�)
        HandleInteractions();   // �������� (R�������������ڼ������������)
        OnDrawGizmos();         // ��Scene��ͼ�л��Ƶ�����ķ�Χ
    }
    private void Update()
    {
        isGrounded = IsGrounded();  //�����Ƿ��ڵ����ϵ�״̬
        HandleJump();               // ��Ծ���� (�̰�С��������������������б���)
        HandleAttack();             // �������� (���������W+������Ϲ���)
        HandleDash();               // ��̿��� (E����̣���Ӱ�����ޣ���̹���)
        HandleSkill();              // ���ܿ��� (C������)
    }

    // �ƶ����� (AD�����ƶ���W̧ͷ��S���¿�)
    void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        Vector2 moveDirection = new Vector2(moveX * moveSpeed, rb.velocity.y);

        rb.velocity = moveDirection;

        // ������ɫ����
        if (moveX < 0) transform.localScale = new Vector3(-1, 1, 1);
        else if (moveX > 0) transform.localScale = new Vector3(1, 1, 1);

        // ���¶�������
        animator.SetFloat("Speed", Mathf.Abs(moveX));

        // W̧ͷ��S���¿����������������ɫ����������
        if (Input.GetKey(KeyCode.W))
        {
            // W̧ͷ�߼�������������
            mainCamera.transform.localEulerAngles = new Vector3(lookDownAngle, 0, 0);
        }
        else if (Input.GetKey(KeyCode.S))
        {
            // S���¿��߼�
            mainCamera.transform.localEulerAngles = new Vector3(lookUpAngle, 0, 0);
        }
        else
        {
            // �ָ�����Ƕ�
            mainCamera.transform.localEulerAngles = Vector3.zero; // ��������Ĭ�ϽǶ�
        }
        mainCamera.transform.localEulerAngles = Vector3.Lerp(mainCamera.transform.localEulerAngles, Vector3.zero, Time.deltaTime * 5f);
    }

    // ��Ծ���� (�̰�С��������������������б���)
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

    // �������� (���������W+������Ϲ���)
    void HandleAttack()
    {
        if (Input.GetButtonDown("Fire1") && !isAttacking)
        {
            isAttacking = true;
            animator.SetTrigger("Attack");
            StartCoroutine(AttackCooldown());

            // �����������
            if (Input.GetKey(KeyCode.W))
            {
                // ���Ϲ���
                PerformAttack(Vector2.up);
            }
            else
            {
                // ��������
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
        // ��⹥������
        RaycastHit2D hit = Physics2D.Raycast(attackPoint.position, attackDirection, attackRange, enemyLayer);
        if (hit.collider != null)
        {
            // �Ե�������˺�
            Debug.Log("Hit: " + hit.collider.name);
        }
        // ����볡������Ľ���
        // TODO: �������彻������
    }

    // ��̿��� (E����̣���Ӱ�����ޣ���̹���)
    void HandleDash()
    {
        if (Input.GetKeyDown(KeyCode.E) && !isDashing && !IsInShadow())
        {
            isDashing = true;
            dashDistanceTravelled = 0f;
            StartCoroutine(DashCoroutine());
        }

        // ��̹�����0.5���ڰ����������̹���
        if (canDashAttack && Input.GetButtonDown("Fire1"))
        {
            // ��̹����߼�
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
        // ����ɫ�Ƿ�����Ӱ����
        // TODO: ʵ����Ӱ�����߼�
        return false;
    }

    // �������� (R�������������ڼ������������)
    void HandleInteractions()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            // ���������Ի��򣬽�����������
            Debug.Log("Interact!");
        }
    }

    // ���ܿ��� (C������)
    void HandleSkill()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            // �ͷż����߼�
            Debug.Log("Skill Activated!");
        }
    }

    bool IsGrounded()
    {
        // ����ɫ�Ƿ��ڵ���
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }
    private void OnDrawGizmos()
    {
        // ��Scene��ͼ�л��Ƶ�����ķ�Χ
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }

}

