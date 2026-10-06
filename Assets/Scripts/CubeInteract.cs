using System;
using Unity.VisualScripting;
using UnityEngine;

public class CubeInteract : MonoBehaviour, IInteractable
{
 [Header("Настройки цвета")]
    [SerializeField] private Color targetColor = Color.red;

    private Renderer cubeRenderer;
    private Color originalColor;

    private void Start()
    {
        cubeRenderer = GetComponent<Renderer>();
        
        if (cubeRenderer != null)
        {
            originalColor = cubeRenderer.material.color;
        }
    }

    public void Interact()
    {
        if (cubeRenderer != null)
        {
            // Меняем цвет материала на заданный
            cubeRenderer.material.color = targetColor;
            Debug.Log("Куб изменил цвет!");
            
            Color temp = targetColor;
            targetColor = originalColor;
            originalColor = temp;
        }
    }
}
