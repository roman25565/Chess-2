using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
public class EndGameProfile : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Button iconB;
    [SerializeField] private TextMeshProUGUI nameAndElo;
    [SerializeField] private TextMeshProUGUI changingElo;
    [SerializeField] private Color eloGainColor = new Color(0.2f, 0.8f, 0.2f); // Зелений
    [SerializeField] private Color eloLossColor = new Color(0.8f, 0.2f, 0.2f);  // Червоний
    [SerializeField] private float animationDuration = 0.5f;
    public void EndGame(PlayerData playerData, int newElo, MainMenu mainMenu)
    {
        icon.sprite = playerData.FirebasePlayer.Icon;
        nameAndElo.text =$"{playerData.FirebasePlayer.Name} ({newElo})";
        
        if (playerData.FirebasePlayer.Elo != newElo)
        {
            OnEloChanged(playerData.FirebasePlayer.Elo, newElo);
        }
        iconB.onClick.AddListener((() =>
        {
            mainMenu.ShowProfilePanel(playerData.FirebasePlayer.ID);
        }));
    }

    private void OnEloChanged(int oldElo, int newElo)
    {
        int difference = newElo - oldElo;
    
        if (difference > 0)
        {
            changingElo.text = $"(+{difference})";
            changingElo.color = eloGainColor;
        }
        else if (difference < 0)
        {
            changingElo.text = $"({difference})";
            changingElo.color = eloLossColor;
        }
        else
        {
            changingElo.text = "";
            return;
        }

        StartCoroutine(AnimateEloChange());
    }

    private IEnumerator AnimateEloChange()
    {
        // Спочатку робимо текст повністю прозорим і більшим
        changingElo.transform.localScale = Vector3.one * 1.3f;
        Color startColor = changingElo.color;
        startColor.a = 0;
        changingElo.color = startColor;

        // Анімація появи
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
        
            // Плавне збільшення прозорості
            Color currentColor = changingElo.color;
            currentColor.a = Mathf.Lerp(0, 1, t);
            changingElo.color = currentColor;
        
            // Плавне зменшення розміру
            changingElo.transform.localScale = Vector3.Lerp(Vector3.one * 1.3f, Vector3.one, t);
        
            yield return null;
        }

        // Завершальний стан
        changingElo.transform.localScale = Vector3.one;
    }

}
}