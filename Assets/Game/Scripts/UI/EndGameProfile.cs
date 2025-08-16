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
    [SerializeField] private Color eloGainColor = new Color(0.2f, 0.8f, 0.2f); // Green
    [SerializeField] private Color eloLossColor = new Color(0.8f, 0.2f, 0.2f);  // Red
    [SerializeField] private float animationDuration = 0.5f;
    public void EndGame(PlayerData playerData, int newElo, MainMenu mainMenu)
    {
        var firebasePlayer = playerData.FirebasePlayer;
        var elo = firebasePlayer.PlayerRanking.Elo;
        
        icon.sprite = firebasePlayer.Icon;
        nameAndElo.text =$"{firebasePlayer.Name} ({newElo})";
        
        if (elo != newElo)
        {
            OnEloChanged(elo, newElo);
        }
        iconB.onClick.AddListener((() =>
        {
            mainMenu.ShowProfilePanel(firebasePlayer.ID);
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
        changingElo.transform.localScale = Vector3.one * 1.3f;
        var startColor = changingElo.color;
        startColor.a = 0;
        changingElo.color = startColor;

        var elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            var t = elapsed / animationDuration;
        
            var currentColor = changingElo.color;
            currentColor.a = Mathf.Lerp(0, 1, t);
            changingElo.color = currentColor;
        
            changingElo.transform.localScale = Vector3.Lerp(Vector3.one * 1.3f, Vector3.one, t);
        
            yield return null;
        }

        changingElo.transform.localScale = Vector3.one;
    }

}
}