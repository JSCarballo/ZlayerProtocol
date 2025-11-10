// Scripts/System/PlayerDeathListener.cs
using UnityEngine;

public class PlayerDeathListener : MonoBehaviour
{
    public Health playerHealth; // arrástralo en el inspector; si no, lo buscamos

    void Awake()
    {
        if (!playerHealth) playerHealth = GetComponent<Health>();
        if (playerHealth) playerHealth.OnDeath += HandleDeath;
        else Debug.LogWarning("[PlayerDeathListener] No se encontró Health en el Player.");
    }

    void OnDestroy()
    {
        if (playerHealth) playerHealth.OnDeath -= HandleDeath;
    }

    void HandleDeath()
    {
        GameFlowController.Instance?.OnPlayerDied();
    }
}
