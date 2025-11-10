using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ElevatorExit : MonoBehaviour
{
    [Tooltip("Si es true, auto-consume al jugador al entrar sin botón extra.")]
    public bool autoTriggerOnEnter = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!autoTriggerOnEnter) return;
        if (!other.CompareTag("Player")) return;

        TryLeaveFloor();
    }

    // Si prefieres usar UI/botón, llama a esto desde un botón:
    public void UI_OnEnterElevator()
    {
        TryLeaveFloor();
    }

    void TryLeaveFloor()
    {
        if (!FloorFlowController.Instance)
        {
            Debug.LogWarning("[ElevatorExit] No existe FloorFlowController en escena.");
            return;
        }
        FloorFlowController.Instance.UI_EnterElevator_AndLoadNextFloor();
    }
}
