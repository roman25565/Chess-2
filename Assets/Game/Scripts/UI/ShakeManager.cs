using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace UI
{

public class ShakeManager : MonoBehaviour
{
    [System.Serializable]
    public class ShakeSettings
    {
        public float intensity = 5f;
        public float duration = 0.3f;
        public float speed = 0.05f;
        public bool shakeX = true;
        public bool shakeY = true;
        public bool shakeZ = false;
    }

    [SerializeField] private ShakeSettings defaultSettings = new ShakeSettings();
    
    private Dictionary<Transform, Coroutine> activeShakes = new Dictionary<Transform, Coroutine>();
    private Dictionary<Transform, Vector3> originalPositions = new Dictionary<Transform, Vector3>();

    // Публічний метод для трясіння з налаштуваннями за замовчуванням
    public void ShakeObject(Transform objectTransform)
    {
        ShakeObject(objectTransform, defaultSettings);
    }

    // Публічний метод з кастомними налаштуваннями
    public void ShakeObject(Transform objectTransform, ShakeSettings settings)
    {
        if (objectTransform == null) return;

        // Зупиняємо попереднє трясіння для цього об'єкта
        if (activeShakes.TryGetValue(objectTransform, out Coroutine existingCoroutine))
        {
            StopCoroutine(existingCoroutine);
            ResetObjectPosition(objectTransform);
        }

        // Запускаємо нове трясіння
        Coroutine newCoroutine = StartCoroutine(ShakeCoroutine(objectTransform, settings));
        activeShakes[objectTransform] = newCoroutine;
    }

    private IEnumerator ShakeCoroutine(Transform objectTransform, ShakeSettings settings)
    {
        // Зберігаємо початкову позицію
        originalPositions[objectTransform] = objectTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < settings.duration)
        {
            // Випадкове зміщення згідно налаштувань
            float offsetX = settings.shakeX ? Random.Range(-settings.intensity, settings.intensity) : 0f;
            float offsetY = settings.shakeY ? Random.Range(-settings.intensity * 0.5f, settings.intensity * 0.5f) : 0f;
            float offsetZ = settings.shakeZ ? Random.Range(-settings.intensity, settings.intensity) : 0f;

            objectTransform.localPosition = originalPositions[objectTransform] + new Vector3(offsetX, offsetY, offsetZ);

            elapsed += Time.deltaTime;
            yield return new WaitForSeconds(settings.speed);
        }

        // Повертаємо на початкову позицію
        ResetObjectPosition(objectTransform);
        activeShakes.Remove(objectTransform);
    }

    private void ResetObjectPosition(Transform objectTransform)
    {
        if (originalPositions.TryGetValue(objectTransform, out Vector3 originalPos))
        {
            objectTransform.localPosition = originalPos;
            originalPositions.Remove(objectTransform);
        }
    }

    // Метод для примусового зупинення всіх трясінь
    public void StopAllShakes()
    {
        foreach (var kvp in activeShakes)
        {
            if (kvp.Value != null)
            {
                StopCoroutine(kvp.Value);
                ResetObjectPosition(kvp.Key);
            }
        }
        activeShakes.Clear();
        originalPositions.Clear();
    }
}
}