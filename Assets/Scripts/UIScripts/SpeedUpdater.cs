using UnityEngine;
using TMPro;

public class SpeedDisplay : MonoBehaviour
{
    public Rigidbody2D target;
    public TMP_Text speedText;

    void Update()
    {
        if (target == null || speedText == null) return;

        speedText.text = "Velocidad: " + target.linearVelocity.x.ToString("F2");
    }
}