// Assets/Scripts/Floors/ElevatorExit.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ElevatorExit : MonoBehaviour
{
    [Header("Trigger")]
    public string playerTag = "Player";
    public bool autoTriggerOnTouch = true;
    public float triggerDelay = 0.35f;
    public bool lockPlayerOnUse = true;

    bool used = false;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
        if (gameObject.tag == "Untagged") gameObject.tag = "Elevator";
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!autoTriggerOnTouch || used) return;
        if (!other.CompareTag(playerTag)) return;
        used = true;
        StartCoroutine(UseRoutine(other.transform));
    }

    System.Collections.IEnumerator UseRoutine(Transform player)
    {
        if (lockPlayerOnUse && player)
        {
            var locker = player.GetComponent<PlayerControlLocker>();
            if (locker) locker.HardLock();
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb) rb.linearVelocity = Vector2.zero;
        }

        yield return new WaitForSeconds(triggerDelay);

        if (FloorFlowController.Instance)
            FloorFlowController.Instance.NextFloor();
        else
            Debug.LogWarning("[ElevatorExit] No hay FloorFlowController en escena.");
    }
}
