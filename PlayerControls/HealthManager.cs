using UnityEngine;
using UnityEngine.UI; // Necessário para manipular componentes de Image
using System.Collections.Generic;

public class HealthManager : MonoBehaviour
{
    public PlayerMoviment player; // Arraste o Player aqui no Inspector
    public List<Image> hearts;    // Arraste as 5 imagens de coração para esta lista
    
    public Sprite fullHeart;      // Arraste o sprite do coração cheio
    public Sprite emptyHeart;     // Arraste o sprite do coração vazio (ou quebrado)

    public void UpdateHearts()
    {
        // Percorre todos os corações da lista
        for (int i = 0; i < hearts.Count; i++)
        {
            // Se o índice for menor que a vida atual, mostra o coração cheio
            if (i < player.currentHealth)
            {
                hearts[i].sprite = fullHeart;
            }
            // Caso contrário, mostra o coração vazio
            else
            {
                hearts[i].sprite = emptyHeart;
            }
        }
    }
}