using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UI
{
public class OnlineStatsUI : MonoBehaviour
{
    [SerializeField] private GameObject inSearchCircle;
    [SerializeField] private TextMeshProUGUI inSearchCircleText;
    
    [SerializeField] private GameObject inSearch1Circle;
    [SerializeField] private TextMeshProUGUI inSearch1CircleText;
    
    [SerializeField] private GameObject inSearch5Circle;
    [SerializeField] private TextMeshProUGUI inSearch5CircleText;
    
    [SerializeField] private GameObject inSearch10Circle;
    [SerializeField] private TextMeshProUGUI inSearch10CircleText;

    public void UpdateUI(Dictionary<int, int> inSearchCounts)
    {
        DisableAllPanels();
        var inSearchCount = inSearchCounts[1] + inSearchCounts[5] + inSearchCounts[10];
        if (inSearchCount == 0) return;
        
        inSearchCircle.SetActive(true);
        inSearchCircleText.text = inSearchCount.ToString();

        if (inSearchCounts[1] > 0)
        {
            inSearch1Circle.SetActive(true);
            inSearch1CircleText.text = inSearchCounts[1].ToString();
        }
        
        if (inSearchCounts[5] > 0)
        {
            inSearch5Circle.SetActive(true);
            inSearch5CircleText.text = inSearchCounts[5].ToString();
        }
        
        if (inSearchCounts[10] > 0)
        {
            inSearch10Circle.SetActive(true);
            inSearch10CircleText.text = inSearchCounts[10].ToString();
        }
    }

    private void DisableAllPanels()
    {
        inSearchCircle.SetActive(false);
        inSearch1Circle.SetActive(false);
        inSearch5Circle.SetActive(false);
        inSearch10Circle.SetActive(false);
    }
}
}