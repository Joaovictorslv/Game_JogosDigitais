using NUnit.Framework;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerMoviment : MonoBehaviour
{

    public Rigidbody2D rb;
    public Animator anim;
    bool isFacingRight = true;
    [Header("Movement")]
    public float moveSpeed = 5f;

    float horizontalMoviment;

    [Header("Jumping")]
    public float jumpPower = 20f;
    public int maxJumps = 2;
    int jumpsRemaining;

    [Header("GroundCheck")]
    public Transform groundCheckPos;
    public Vector2 groundCheckSize = new Vector2(0.5f,0.05f);
    public LayerMask groundLayer;
    bool isGrounded;
    public float coyoteTime = 0.1f;
    public float coyoteTimer;
    bool wasGrounded;

    [Header("Gravity")]
    public float baseGravity = 2f;
    public float maxFallSpeed = 18f;
    public float fallSpeedMultiplier = 2f;

    [Header("WallCheck")]
    public Transform wallCheckPos;
    public Vector2 wallCheckSize = new Vector2(0.5f,0.05f);
    public LayerMask wallLayer;

    [Header("WallMoviment")]
    public float wallSlideSpeed = 5f;
    bool isWallSliding;
    bool isWallJumping;
    float wallJumpDirection;
    float wallJumpTime = 0.2f;
    float wallJumpTimer;
    public Vector2 wallJumpPower = new Vector2(5f, 5f);

    [Header("Dash")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    float dashTimer;
    float dashCooldownTimer;
    bool isDashing;
    float lastLeftPress = -1f;
    float lastRightPress = -1f;
    public float doubleTapWindow = 0.3f;
    private float lastHorizontalInput;

    [Header("Attack")]
    public Transform attackPoint;      // Arraste o objeto "AttackPoint" aqui no Unity
    public float attackRange = 0.5f;   // Tamanho do círculo de colisão do golpe
    public LayerMask enemyLayers;      // Escolha a Layer "Enemy" no Inspector
    public float attackRate = 2f;      // Quantos ataques por segundo
    float nextAttackTime = 0f;

    [Header("Health System")]
    public int maxHealth = 5;
    public int currentHealth;
    public bool isDead = false;

    [Header("UI Reference")]
    public HealthManager healthUI;

    [Header("UI Death")]
    public GameObject deathScreenPanel;

    
    [Header("Special Ability")]
    public AnimatorOverrideController specialModeController;
    private RuntimeAnimatorController normalController;
    private bool isSpecialMode = false;
    private float specialDuration = 20f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

void Start() {
    currentHealth = maxHealth;
    normalController = anim.runtimeAnimatorController; // Salva o controlador normal
}

    // Update is called once per frame
void Update()
{
    // Se o player estiver morto, para a execução do Update aqui mesmo
    // Isso elimina os erros de 'linearVelocity' e permite que a Coroutine rode
    if (isDead) return; 

    GroundCheck();
    if (nextAttackTime > 0) nextAttackTime -= Time.deltaTime;
    CheckDash();
    Gravity();  
    WallCheck();
    WallSlide();
    WallJump();

    if (isDashing) return;

    if(!isWallJumping)
    {
        rb.linearVelocity = new Vector2(horizontalMoviment * moveSpeed, rb.linearVelocity.y);
        if(!isWallSliding) Flip();
    }

    // Atualiza animações apenas se vivo
    anim.SetFloat("yVelocity", rb.linearVelocity.y);
    anim.SetFloat("magnetude", Mathf.Abs(rb.linearVelocity.x));
    anim.SetBool("isWallSliding", isWallSliding);
    anim.SetBool("isGrounded", isGrounded);
    anim.SetBool("isDashing", isDashing);
}

//Special Mode_________________________________________________________________________
public void OnSpecialAbility(UnityEngine.InputSystem.InputAction.CallbackContext context) {
    // Requisito: Apenas com 8 cristais e clicando Shift
    if (context.performed) {
        Debug.Log("Tecla Shift detectada! Tentando ativar especial...");
        
        if (PowerManager.instance.IsFull()) {
            Debug.Log("Poder cheio! Iniciando Corrotina.");
            StartCoroutine(ActivateSpecialMode());
        } else {
            Debug.Log("Poder insuficiente para ativar.");
        }
    }
}

private IEnumerator ActivateSpecialMode() 
{
    // --- INÍCIO DO MODO ESPECIAL ---
    isSpecialMode = true;
    PowerManager.instance.ResetPower();

    // Salva o controlador normal se for a primeira vez
    if (normalController == null) normalController = anim.runtimeAnimatorController;

    // Executa animação de Transformação e troca os Sprites
    anim.SetTrigger("transform");
    anim.runtimeAnimatorController = specialModeController;

    // --- AGUARDA EXATAMENTE 20 SEGUNDOS ---
    yield return new WaitForSeconds(specialDuration);

    // --- RESET DO MODO ESPECIAL ---
    isSpecialMode = false; // Volta a levar dano
    anim.runtimeAnimatorController = normalController; // Volta as animações originais
    
    // Opcional: Trigger para uma animação de "destransformação"
    anim.SetTrigger("idle");
}

//Health & Damage ____________________________________________________________________

public void TakeDamage(int damage) 
{
    if (isDead || isSpecialMode) return;
    
    currentHealth -= damage;
    //Debug.Log("Player Perdeu Vida! Restam: " + currentHealth);

    if (healthUI != null) healthUI.UpdateHearts();

    // --- IMPLEMENTAÇÃO: Knockback (Tranco) ---
    // Usamos isFacingRight para garantir que o tranco sempre empurre para trás,
    // mesmo se o jogador estiver parado (horizontalMoviment = 0).
    float knockbackDir = isFacingRight ? -1f : 1f;
    
    rb.linearVelocity = Vector2.zero; // Limpa a velocidade para o tranco não ser anulado
    rb.AddForce(new Vector2(knockbackDir * 15f, 8f), ForceMode2D.Impulse);

    if (currentHealth > 0) 
    {
        anim.SetTrigger("hurt");
    }
    
    if (currentHealth <= 0) 
    {
        currentHealth = 0;
        Die();
    }
}

public void Die() {
    if (isDead) return;
    isDead = true; // Agora o Update vai parar de rodar no próximo frame

    // 1. Inicia a animação imediatamente
    anim.SetTrigger("death"); 
    
    // 2. Trava a física para não dar erro de static body
    rb.linearVelocity = Vector2.zero;
    rb.bodyType = RigidbodyType2D.Static; 
    gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

    // 3. Inicia a sequência de tempo (A Coroutine não será interrompida agora)
    StartCoroutine(SequenciaDeMorte());
}

IEnumerator SequenciaDeMorte() {
    Debug.Log("Aguardando animação de morte...");
    yield return new WaitForSeconds(7f); // Tempo para a animação rodar

    if(deathScreenPanel != null) {
        deathScreenPanel.SetActive(true);
        Debug.Log("Tela de morte ativada!");
    }

    yield return new WaitForSeconds(3f); // Tempo com a tela visível
    SceneManager.LoadScene("StartMenu");
}

void VoltarAoMenu() {
    SceneManager.LoadScene("StartMenu");
}

// Função auxiliar que o Invoke vai chamar
void ReiniciarFase() 
{
    // Pega o nome da cena atual e carrega ela do zero
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
}

//Gravity ____________________________________________________________________
    private void Gravity()
{
    if (isWallJumping) return; // ← adicione essa linha no início

    if (rb.linearVelocity.y < 0)
    {
        rb.gravityScale = baseGravity * fallSpeedMultiplier;
        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            Mathf.Max(rb.linearVelocity.y, -maxFallSpeed)
        );
    }
    else
    {
        rb.gravityScale = baseGravity;
    }
}

//Wall ____________________________________________________________________
    private bool WallCheck()
    {
        return Physics2D.OverlapBox(wallCheckPos.position, wallCheckSize, 0, wallLayer);
    }
    private void WallSlide()
{
    if (!isGrounded && WallCheck() && horizontalMoviment != 0 && !isWallJumping) // ← adicione !isWallJumping
    {
        isWallSliding = true;
        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed)
        );
    }
    else
    {
        isWallSliding = false;
    }
}

    private void WallJump()
    {
        if (isWallSliding)
        {
            isWallJumping = false;
            wallJumpDirection = isFacingRight ? -1f : 1f;
            wallJumpTimer = wallJumpTime;
            CancelInvoke(nameof(CancelWallJump));
            // ✅ Removido o bloco de scale daqui
        }
        else if (wallJumpTimer > 0f)
        {
            wallJumpTimer -= Time.deltaTime;
            wallJumpTimer = Mathf.Max(wallJumpTimer, 0f);
        }
    }

    private void CancelWallJump()
    {
        isWallJumping = false;
    }

//Basic_Moviments_________________________________________________________________________
        public void Move(InputAction.CallbackContext context)
    {
        float moveInput = context.ReadValue<Vector2>().x;

        // Detecta o momento exato que o jogador apertou a tecla (performed)
        if (context.performed && moveInput != 0)
        {
            if (moveInput > 0) // Direita
            {
                if (Time.time - lastRightPress < doubleTapWindow) TryDash(1f);
                lastRightPress = Time.time;
            }
            else if (moveInput < 0) // Esquerda
            {
                if (Time.time - lastLeftPress < doubleTapWindow) TryDash(-1f);
                lastLeftPress = Time.time;
            }
        }

        horizontalMoviment = moveInput;
    }

    public void Jump(InputAction.CallbackContext context)
    {
        //Debug.Log($"Jump chamado | performed:{context.performed} | wallJumpTimer:{wallJumpTimer} | jumpsRemaining:{jumpsRemaining} | isGrounded:{isGrounded}");

        // Wall Jump PRIMEIRO
        if (context.performed && wallJumpTimer > 0)
        {
            //Debug.Log(">>> WALL JUMP EXECUTADO");

            isWallJumping = true;
            isWallSliding = false;

            rb.linearVelocity = Vector2.zero;
            rb.AddForce(
                new Vector2(wallJumpDirection * wallJumpPower.x, wallJumpPower.y), ForceMode2D.Impulse);

            wallJumpTimer = 0f;
            

        if (wallJumpDirection > 0 && !isFacingRight)
        {
            isFacingRight = true;
            Vector3 ls = transform.localScale;
            ls.x = 1f; // ← força positivo
            transform.localScale = ls;
        }
        else if (wallJumpDirection < 0 && isFacingRight)
        {
            isFacingRight = false;
            Vector3 ls = transform.localScale;
            ls.x = -1f; // ← força negativo
            transform.localScale = ls;
        }

        Invoke(nameof(CancelWallJump), wallJumpTime + 0.1f);
        return;
        }

        // Pulo normal
        if (jumpsRemaining > 0)
        {
            if (context.performed)
            {
                //Debug.Log($">>> PULO NORMAL EXECUTADO | jumpsRemaining antes: {jumpsRemaining}");
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower);
                jumpsRemaining--;
                //Debug.Log($">>> jumpsRemaining depois: {jumpsRemaining}");

                anim.SetTrigger("jump");

            }
            else if (context.canceled)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);

            }
        }
        else
        {
            //Debug.Log(">>> PULO BLOQUEADO — jumpsRemaining = 0");
        }
    }

    private void GroundCheck()
    {
        bool grounded = Physics2D.OverlapBox(
            groundCheckPos.position, groundCheckSize, 0, groundLayer
        );

        // ✅ Só reseta quando ACABOU de pousar (transição false→true)
        if (grounded && !wasGrounded)
        {
            jumpsRemaining = maxJumps;
           // Debug.Log(">>> POUSOU — jumpsRemaining resetado para " + maxJumps);
        }if (isWallSliding)
    {
        jumpsRemaining = maxJumps;
    }

    isGrounded = grounded;
    wasGrounded = grounded;
    anim.SetBool("isGrounded", isGrounded);
    }

    private void CheckDash()
    {
        if (dashCooldownTimer > 0)
            dashCooldownTimer -= Time.deltaTime;

        // Em vez de Input.GetKeyDown, checamos a mudança no horizontalMoviment
        // que já é atualizado pelo seu método Move()
    }

    private void TryDash(float direction)
    {
        if (dashCooldownTimer > 0) return; // ainda em cooldown

        StartCoroutine(DashCoroutine(direction));
    }

    private IEnumerator DashCoroutine(float direction)
    {
        isDashing = true;
        dashCooldownTimer = dashCooldown;

        // Salva e desativa gravidade durante o dash
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        // Aplica velocidade do dash
        rb.linearVelocity = new Vector2(direction * dashSpeed, 0f);

        anim.SetBool("isDashing", true);

        yield return new WaitForSeconds(dashDuration);

        // Restaura estado normal
        rb.gravityScale = originalGravity;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        isDashing = false;

        anim.SetBool("isDashing", false);
    }

//Flip_Player_________________________________________________________________________
    private void Flip()
    {
        // Durante wall slide — vira para longe da parede
        if (isWallSliding)
        {
            Vector3 ls = transform.localScale;
            ls.x = isFacingRight ? -1f : 1f;
            transform.localScale = ls;
            return;
        }

        // Movimento normal
        if (isFacingRight && horizontalMoviment < 0)
        {
            isFacingRight = false;
            Vector3 ls = transform.localScale;
            ls.x = -1f;
            transform.localScale = ls;
        }
        else if (!isFacingRight && horizontalMoviment > 0)
        {
            isFacingRight = true;
            Vector3 ls = transform.localScale;
            ls.x = 1f;
            transform.localScale = ls;
        }
    }

//Attack_________________________________________________________________________

// --- NOVO MÉTODO DE ATAQUE ---
    public void Attack(InputAction.CallbackContext context)
    {
        // Só executa se o botão for pressionado e o cooldown for zero
        if (context.performed && nextAttackTime <= 0)
        {
            PerformAttack();
            nextAttackTime = 1f / attackRate; // Reinicia o cooldown
        }
    }

    private void PerformAttack()
{
    anim.SetTrigger("attack");

    // 1. Detectar Inimigos
    Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
    foreach (Collider2D enemy in hitEnemies) {
        // Tenta dar dano se for um inimigo
        if(enemy.CompareTag("Enemy")) 
            enemy.GetComponent<EnemyAI>()?.TakeDamage(1);
    }

    // 2. RICOCHETE: Detectar Projéteis
    // Crie uma Layer chamada "Projectile" e adicione aqui
    LayerMask projectileLayer = LayerMask.GetMask("Projectile");
    Collider2D[] projectiles = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, projectileLayer);
    
    foreach (Collider2D proj in projectiles) {
        proj.GetComponent<Projectile>()?.Ricochet();
    }
}

//Gizmos_________________________________________________________________________
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(groundCheckPos.position, groundCheckSize);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(wallCheckPos.position, wallCheckSize);
        
        // Desenha o raio do ataque no editor para facilitar o ajuste
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}
