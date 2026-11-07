using System;
using System.Collections.Generic;
using UnityEngine;

namespace Progression
{
    /// <summary>
    /// Хранит список разблокированных инструментов.
    /// Состояние сохраняется в PlayerPrefs.
    /// </summary>
    public class ToolInventory : MonoBehaviour
    {
        private const string KeyUnlockedTools = "Progress_UnlockedTools";

        private HashSet<ToolType> unlockedTools = new HashSet<ToolType>();

        public event Action<ToolType> ToolUnlocked;

        private void Awake()
        {
            LoadUnlockedTools();
            
            // GravityGun доступен с самого начала
            if (!unlockedTools.Contains(ToolType.GravityGun))
            {
                UnlockTool(ToolType.GravityGun);
            }
        }

        /// <summary>Разблокировать инструмент.</summary>
        public void UnlockTool(ToolType toolType)
        {
            if (unlockedTools.Add(toolType))
            {
                Debug.Log($"[ToolInventory] Разблокирован инструмент: {toolType}");
                SaveUnlockedTools();
                ToolUnlocked?.Invoke(toolType);
            }
        }

        /// <summary>Проверить, разблокирован ли инструмент.</summary>
        public bool HasTool(ToolType toolType)
        {
            return unlockedTools.Contains(toolType);
        }

        /// <summary>Получить все разблокированные инструменты.</summary>
        public IEnumerable<ToolType> GetUnlockedTools()
        {
            return unlockedTools;
        }

        /// <summary>Сбросить все разблокированные инструменты (для "Новой игры").</summary>
        public void ResetTools()
        {
            unlockedTools.Clear();
            SaveUnlockedTools();
        }

        private void LoadUnlockedTools()
        {
            int mask = PlayerPrefs.GetInt(KeyUnlockedTools, 0);
            unlockedTools.Clear();

            for (int i = 0; i < 4; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    unlockedTools.Add((ToolType)i);
                }
            }
        }

        private void SaveUnlockedTools()
        {
            int mask = 0;
            foreach (var tool in unlockedTools)
            {
                mask |= (1 << (int)tool);
            }
            PlayerPrefs.SetInt(KeyUnlockedTools, mask);
            PlayerPrefs.Save();
        }
    }
}
