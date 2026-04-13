using UnityEngine;

public class EnemySpawner : MonoBehaviour {
    [Header("Prefabs (Coloque Ranger no 0 e Melee no 1)")]
    public GameObject[] enemyPrefabs; 

    [Header("Configurações de Área")]
    public Collider2D spawnArea; 
    public LayerMask groundLayer;
    public LayerMask wallLayer;

    [Header("Spawn Settings")]
    public float spawnInterval = 3f;
    public int maxEnemies = 24;      // Limite máximo
    public int resumeThreshold = 12; // Só volta a gerar quando cair para 12
    
    private int spawnCount = 0; 
    private bool isPaused = false;

    void Start() {
        InvokeRepeating("TrySpawnEnemy", 2f, spawnInterval);
    }

    void TrySpawnEnemy() {
        // Conta quantos inimigos com a Tag "Enemy" existem na cena agora
        int currentEnemies = GameObject.FindGameObjectsWithTag("Enemy").Length;

        // Lógica de Interrupção
        if (currentEnemies >= maxEnemies) {
            isPaused = true;
            Debug.Log("Limite atingido (24). Geração pausada.");
        }

        // Lógica de Retomada
        if (isPaused && currentEnemies <= resumeThreshold) {
            isPaused = false;
            Debug.Log("Inimigos reduzidos para 12. Retomando geração.");
        }

        // Se estiver pausado, interrompe a execução aqui
        if (isPaused) return;

        // Se não estiver pausado, segue com a tentativa de spawn
        Vector2 randomPos = GetRandomPointInBounds();
        RaycastHit2D hitGround = Physics2D.Raycast(randomPos, Vector2.down, 5f, groundLayer);

        if (hitGround.collider != null) {
            Collider2D overlapWall = Physics2D.OverlapCircle(hitGround.point + new Vector2(0, 1f), 0.5f, wallLayer);

            if (overlapWall == null) {
                Spawn(hitGround.point + new Vector2(0, 0.5f));
            }
        }
    }

    void Spawn(Vector2 pos) {
        int indexToSpawn;

        if ((spawnCount + 1) % 3 == 0) {
            indexToSpawn = 1; // Melee
        } else {
            indexToSpawn = 0; // Ranger
        }

        Instantiate(enemyPrefabs[indexToSpawn], pos, Quaternion.identity);
        spawnCount++; 
    }

    Vector2 GetRandomPointInBounds() {
        Bounds bounds = spawnArea.bounds;
        return new Vector2(Random.Range(bounds.min.x, bounds.max.x), Random.Range(bounds.min.y, bounds.max.y));
    }
}