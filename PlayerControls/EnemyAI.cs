using System.Collections;
using UnityEngine;

public class EnemyAI : MonoBehaviour {
    public enum EnemyType { Ranger, Melee }
    [Header("Configurações de Tipo")]
    public EnemyType type;

    [Header("Status Base")]
    public float health; 
    public float speed; 
    public float detectRange; 
    
    [Header("Referências")]
    public GameObject projectilePrefab;
    public GameObject itemDrop;
    public Animator anim;
    // Opcional: Arraste o script PlayerMoviment aqui para verificação mais rápida
    public PlayerMoviment playerScript; 
    private Transform player;

    [Header("Configurações de Patrulha (Ambos)")]
    public float patrolDistance = 3f;
    private Vector2 startPos;
    private bool movingRight = true;

    [Header("Ataque Ranger (Tiro)")]
    public Transform firePoint;
    public float fireRate = 2f;
    private float nextFireTime;

    [Header("Ataque Melee (Corpo a Corpo)")]
    public float attackRange = 1.5f;
    public float attackCooldown = 1.5f;
    private float lastAttackTime;
    public int attackDamage = 1; // Dano que o monstro dá ao atacar

    [Header("Configurações de Pulo (Mario Style)")]
    public float bounceForce = 12f; // Força do pulo do player ao quicar na cabeça

    private bool isDead = false;
    private bool isFacingRight = false;

    void Start() {
        startPos = transform.position; 
        
        // AUTO-CONFIGURAÇÃO DO ANIMATOR (Para prevenir o erro de image_5.png)
        if (anim == null) anim = GetComponent<Animator>();

        if (player == null) {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) {
                player = playerObj.transform;
                // Pega a referência do script de movimento do player
                if(playerScript == null) playerScript = playerObj.GetComponent<PlayerMoviment>();
            }
        }
    }

    void Update() {
        if (isDead || player == null || anim == null) return;

        if (playerScript != null && playerScript.isDead) {
        anim.SetFloat("speed", 0);
        // Opcional: fazer o inimigo patrulhar em vez de ficar parado embaixo do corpo
        Patrol(); 
        return; 
    }

        float distToPlayer = Vector2.Distance(transform.position, player.position);

        if (distToPlayer < detectRange) {
            if (type == EnemyType.Melee) {
                HandleMeleeBehavior(distToPlayer);
            } else {
                HandleRangerBehavior();
            }
        } else {
            Patrol();
        }
    }

    // --- LÓGICA DO RANGER ---
    void HandleRangerBehavior() {
        Flip(player.position.x - transform.position.x);
        ShootPlayer();
    }

    void ShootPlayer() {
        anim.SetFloat("speed", 0);
        if (Time.time >= nextFireTime) {
            anim.SetTrigger("attack");
            nextFireTime = Time.time + fireRate;
        }
    }

    // --- LÓGICA DO MELEE ---
    void HandleMeleeBehavior(float dist) {
        Flip(player.position.x - transform.position.x);

        if (dist <= attackRange) {
            // Atacar
            anim.SetFloat("speed", 0);
            if (Time.time >= lastAttackTime + attackCooldown) {
                anim.SetTrigger("attack"); // Dispara a animação
                lastAttackTime = Time.time;
                // NOTA: O dano ao player agora é causado APENAS via Animation Event (DamagePlayer)
            }
        } else {
            // Perseguir
            anim.SetFloat("speed", 1);
            Vector2 target = new Vector2(player.position.x, transform.position.y);
            transform.position = Vector2.MoveTowards(transform.position, target, speed * Time.deltaTime);
        }
    }

    void Patrol() {
        if(anim == null) return;
        anim.SetFloat("speed", 1);
        
        float leftLimit = startPos.x - patrolDistance;
        float rightLimit = startPos.x + patrolDistance;

        if (movingRight) {
            transform.Translate(Vector2.right * speed * Time.deltaTime);
            Flip(1);
            if (transform.position.x >= rightLimit) movingRight = false;
        } else {
            transform.Translate(Vector2.left * speed * Time.deltaTime);
            Flip(-1);
            if (transform.position.x <= leftLimit) movingRight = true;
        }
    }

    // --- SISTEMA DE QUICAR (MARIO STYLE) ---
    // Removemos OnCollisionEnter e OnCollisionStay para o dano de contato.
    // Usamos OnCollisionEnter2D apenas para o quique por cima.
    private void OnCollisionEnter2D(Collision2D collision) {
        if (collision.gameObject.CompareTag("Player") && !isDead) {
            // Verifica se o contato foi por cima (cabeça do inimigo)
            foreach (ContactPoint2D point in collision.contacts) {
                if (point.normal.y < -0.5f) { // Se algo bateu no topo
                    BouncePlayer(collision.gameObject);
                    return;
                }
            }
        }
    }

    void BouncePlayer(GameObject playerObj) {
        Rigidbody2D playerRb = playerObj.GetComponent<Rigidbody2D>();
        if (playerRb != null) {
            playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, bounceForce);
            TakeDamage(0.5f); // Inimigo sofre 0.5 de dano
            Debug.Log("Player quicou no inimigo!");
        }
    }

    // Chamado pelo Animation Event do Melee (Problema 02)
    public void DamagePlayer() {
        if(player == null || playerScript == null || isDead) return;

        float dist = Vector2.Distance(transform.position, player.position);
        
        // Verifica se o player ainda está no alcance na hora do golpe
        if (dist <= attackRange && !playerScript.isDead) {
            playerScript.TakeDamage(attackDamage);
            Debug.Log("Inimigo Melee acertou o Player!");
        }
    }

    // --- STATUS ---
    public void TakeDamage(float dmg) {
        if (isDead) return;
        health -= dmg;
        if (health <= 0) Die();
        else {
            if(anim != null) anim.SetTrigger("hurt");
        }
    }

    void Die() {
    if (isDead) return;
    isDead = true;

    if(anim != null) anim.SetTrigger("death");

    // SOLUÇÃO PARA NÃO CAIR PELO CHÃO:
    Rigidbody2D rb = GetComponent<Rigidbody2D>();
    if (rb != null) {
        rb.linearVelocity = Vector2.zero; // Para o movimento na hora
        rb.bodyType = RigidbodyType2D.Static; // Faz o inimigo ficar "congelado" no lugar
    }

    // Muda a layer para o Player não ficar "trupando" no cadáver
    gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

    this.enabled = false; // Desliga a IA para ele não tentar te seguir morto
    StartCoroutine(DeathSequence());
}

IEnumerator DeathSequence() {
    // 1. Mantém o corpo na tela por 3 segundos
    yield return new WaitForSeconds(3f); 

    // 2. Instancia o cristal na posição onde o inimigo morreu
    if(itemDrop != null) {
        Instantiate(itemDrop, transform.position, Quaternion.identity);
    }

    // 3. Some com o inimigo
    Destroy(gameObject);
}

    void Flip(float direction) {
        if (direction > 0 && !isFacingRight || direction < 0 && isFacingRight) {
            isFacingRight = !isFacingRight;
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
    }

    public void LaunchProjectile() { // Chamado pelo Animation Event do Ranger
        if(projectilePrefab != null && firePoint != null) {
            GameObject projObj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            Projectile projScript = projObj.GetComponent<Projectile>();
            if (projScript != null) projScript.SetupDirection(isFacingRight ? 1 : -1);
        }
    }
}