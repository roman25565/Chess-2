using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI
{
public class Notification : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textObject;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button rejectButton;

    public void Init(string text, UnityAction onAccept, UnityAction onReject)
    {
        textObject.text = text;
        acceptButton.onClick.AddListener(()=>
        {
            onAccept();
            Destroy(gameObject);
        });
        rejectButton.onClick.AddListener(()=>
        {
            onReject();
            Destroy(gameObject);
        });
    }
}
}