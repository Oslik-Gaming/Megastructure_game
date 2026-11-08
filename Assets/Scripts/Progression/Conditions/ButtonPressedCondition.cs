using System;
using UnityEngine;

namespace Progression.Conditions
{
    /// <summary>
    /// Условие: кнопка-куб (CubeInteract) нажата игроком.
    /// Кнопка меняет цвет при взаимодействии, это условие срабатывает одновременно с нажатием.
    /// Подписывается на событие CubeInteract.Pressed при инициализации шага,
    /// после срабатывания отписывается. При сбросе прогрессии (Initialize заново) — сбрасывается.
    /// </summary>
    [Serializable]
    public class ButtonPressedCondition : IStepCondition
    {
        [SerializeField] private CubeInteract button;

        private bool pressed;
        private CubeInteract boundButton;

        public void Initialize(GameProgressionManager context)
        {
            pressed = false;

            // Переподписываемся заново (шаг может инициализироваться повторно при сбросе)
            Unsubscribe();

            boundButton = button;
            if (boundButton != null)
            {
                boundButton.Pressed += OnPressed;
            }
        }

        public bool IsCompleted(GameProgressionManager context)
        {
            return pressed;
        }

        private void OnPressed()
        {
            pressed = true;
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (boundButton != null)
            {
                boundButton.Pressed -= OnPressed;
            }
            boundButton = null;
        }
    }
}
