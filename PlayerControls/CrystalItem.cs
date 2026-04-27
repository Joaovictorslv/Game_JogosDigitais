using UnityEngine;

public class CrystalItem : MonoBehaviour {
    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag("Player")) {
            // Requisito: Adiciona carga à barra de carregamento
            PowerManager.instance.AddPower(); 
            Debug.Log("Cristal coletado para a barra especial!");
            Destroy(gameObject); 
        }
    }
}