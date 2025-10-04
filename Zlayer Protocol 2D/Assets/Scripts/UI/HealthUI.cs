// Assets/Scripts/UI/HealthUI.cs
using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    public Text hpText;
    public Slider hpBar; // opcional

    Health target;
    float findTimer;

    void Update()
    {
        if (!target)
        {
            findTimer += Time.deltaTime;
            if (findTimer > 0.5f)
            {
                findTimer = 0f;
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p) target = p.GetComponent<Health>();
            }
            return;
        }

        float cur = Mathf.Max(0f, target.CurrentHP);
        float max = Mathf.Max(1f, target.MaxHP);

        if (hpText) hpText.text = $"HP {cur:0}/{max:0}";
        if (hpBar)
        {
            hpBar.maxValue = max;
            hpBar.value = cur;
        }
    }
}
