using System.Collections;
using UnityEngine;

namespace Effects
{
    /// <summary>
    /// Эффект взрыва реактора: короткое схлопывание, затем резкое расширение
    /// с белой вспышкой света.
    /// </summary>
    public class ReactorExplosionEffect : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField, Min(0.01f)] private float collapseDuration = 0.35f;
        [SerializeField, Min(0.01f)] private float expansionDuration = 0.12f;
        [SerializeField, Min(0.01f)] private float flashDuration = 0.45f;
        [SerializeField, Range(0.01f, 1f)] private float collapsedScale = 0.08f;
        [SerializeField, Min(1f)] private float explosionScale = 2.5f;
        [SerializeField] private Color flashColor = Color.white;

        [Header("Light")]
        [SerializeField] private Light explosionLight;
        [SerializeField, Min(0f)] private float flashLightIntensity = 12f;
        [SerializeField, Min(0f)] private float flashLightRange = 12f;

        private Renderer[] renderers;
        private Material[][] materials;
        private Color[][] originalColors;
        private Color[][] originalEmissionColors;
        private float originalLightIntensity;
        private float originalLightRange;
        private Vector3 originalScale;
        private Coroutine currentAnimation;

        private void Awake()
        {
            originalScale = transform.localScale;
            CacheMaterials();

            if (explosionLight != null)
            {
                originalLightIntensity = explosionLight.intensity;
                originalLightRange = explosionLight.range;
            }
        }

        /// <summary>Запускает эффект. Повторный вызов перезапускает его с начала.</summary>
        public void Play()
        {
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);

            currentAnimation = StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            SetScale(originalScale);
            SetFlash(0f);

            yield return Animate(0f, collapseDuration, 1f, 0f, true, false);
            yield return Animate(0f, expansionDuration, 0f, 1f, false, false);
            yield return Animate(0f, flashDuration, 1f, 0f, false, true);

            SetScale(originalScale);
            RestoreMaterials();
            RestoreLight();
            currentAnimation = null;
        }

        private IEnumerator Animate(float elapsed, float duration, float from, float to, bool collapsing, bool keepExpanded)
        {
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = Mathf.SmoothStep(0f, 1f, t);
                float amount = Mathf.Lerp(from, to, t);

                if (keepExpanded)
                    SetScale(originalScale * explosionScale);
                else if (collapsing)
                    SetScale(Vector3.Lerp(originalScale, originalScale * collapsedScale, amount));
                else
                    SetScale(Vector3.Lerp(originalScale * collapsedScale, originalScale * explosionScale, amount));

                SetFlash(collapsing ? amount * 0.35f : 1f - amount * 0.15f);
                yield return null;
            }
        }

        private void CacheMaterials()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            materials = new Material[renderers.Length][];
            originalColors = new Color[renderers.Length][];
            originalEmissionColors = new Color[renderers.Length][];

            for (int i = 0; i < renderers.Length; i++)
            {
                materials[i] = renderers[i].materials;
                originalColors[i] = new Color[materials[i].Length];
                originalEmissionColors[i] = new Color[materials[i].Length];

                for (int j = 0; j < materials[i].Length; j++)
                {
                    Material material = materials[i][j];
                    originalColors[i][j] = material.HasProperty("_BaseColor")
                        ? material.GetColor("_BaseColor")
                        : material.color;
                    originalEmissionColors[i][j] = material.HasProperty("_EmissionColor")
                        ? material.GetColor("_EmissionColor")
                        : Color.black;
                }
            }
        }

        private void SetFlash(float amount)
        {
            for (int i = 0; i < materials.Length; i++)
            {
                for (int j = 0; j < materials[i].Length; j++)
                {
                    Material material = materials[i][j];
                    Color color = Color.Lerp(originalColors[i][j], flashColor, amount);
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                    if (material.HasProperty("_Color")) material.SetColor("_Color", color);
                    if (material.HasProperty("_EmissionColor"))
                        material.SetColor("_EmissionColor", flashColor * (amount * 8f));
                }
            }

            if (explosionLight != null)
            {
                explosionLight.intensity = Mathf.Lerp(originalLightIntensity, flashLightIntensity, amount);
                explosionLight.range = Mathf.Lerp(originalLightRange, flashLightRange, amount);
            }
        }

        private void RestoreMaterials()
        {
            for (int i = 0; i < materials.Length; i++)
                for (int j = 0; j < materials[i].Length; j++)
                {
                    Material material = materials[i][j];
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", originalColors[i][j]);
                    if (material.HasProperty("_Color")) material.SetColor("_Color", originalColors[i][j]);
                    if (material.HasProperty("_EmissionColor"))
                        material.SetColor("_EmissionColor", originalEmissionColors[i][j]);
                }
        }

        private void RestoreLight()
        {
            if (explosionLight == null) return;
            explosionLight.intensity = originalLightIntensity;
            explosionLight.range = originalLightRange;
        }

        private void SetScale(Vector3 scale) => transform.localScale = scale;
    }
}
