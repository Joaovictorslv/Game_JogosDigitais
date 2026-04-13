using UnityEngine;

public class Projectile : MonoBehaviour {
    public float speed = 7f;
    private bool reflected = false;
    private int shootDirection = 1; // Novo: 1 para direita, -1 para esquerda

    // Função para o inimigo dizer para qual lado o tiro deve ir
    public void SetupDirection(int dir) {
        shootDirection = dir;
        
        // Opcional: Vira o sprite do projétil para o lado certo
        Vector3 scale = transform.localScale;
        scale.x = -dir;
        transform.localScale = scale;
    }

    void Update() {
        // Se refletido, ele inverte a direção original
        float moveDir = reflected ? -shootDirection : shootDirection;
        transform.Translate(Vector2.right * moveDir * speed * Time.deltaTime);
    }

    public void Ricochet() {
    reflected = true;
    
    // Muda a layer para não bater no player de novo imediatamente
    gameObject.layer = LayerMask.NameToLayer("Default"); 

    // --- ADICIONADO: Inverte o sprite visualmente ---
    Vector3 scale = transform.localScale;
    scale.x *= -1; // Multiplica a escala X por -1 para invertê-la
    transform.localScale = scale;
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag("Player") && !reflected) {
            collision.GetComponent<PlayerMoviment>()?.TakeDamage(1);
            Destroy(gameObject);
        }
        if (collision.CompareTag("Enemy") && reflected) {
            collision.GetComponent<EnemyAI>()?.TakeDamage(1);
            Destroy(gameObject);
        }
    }
}