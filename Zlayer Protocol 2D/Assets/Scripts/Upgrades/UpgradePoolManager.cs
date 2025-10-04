// Assets/Scripts/Upgrades/UpgradePoolManager.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class UpgradePoolManager : MonoBehaviour
{
    public static UpgradePoolManager Instance { get; private set; }

    [Header("Pool de upgrades")]
    public List<WeaponUpgradeSO> allUpgrades = new();

    [Tooltip("Reiniciar la pool al cargar escena (nueva run).")]
    public bool resetOnSceneLoad = true;

    [Tooltip("Si no hay candidatos, permitir reusar upgrades únicos (fallback).")]
    public bool allowFallbackReuse = false;

    private readonly HashSet<string> usedIds = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (resetOnSceneLoad)
            SceneManager.activeSceneChanged += (_, __) => ResetPool();
    }

    public void ResetPool() => usedIds.Clear();

    public void MarkUsed(WeaponUpgradeSO so)
    {
        if (so != null && so.unique && !string.IsNullOrEmpty(so.id))
            usedIds.Add(so.id);
    }

    public bool IsUsed(WeaponUpgradeSO so)
    {
        return so != null && so.unique && !string.IsNullOrEmpty(so.id) && usedIds.Contains(so.id);
    }

    // === Saca 1 al azar (consume al instante) ===
    public bool DrawRandomUnique(out WeaponUpgradeSO picked)
    {
        picked = null;
        if (!BuildCandidateList(out var candidates)) return false;
        picked = WeightedPick(candidates);
        MarkUsed(picked);
        return picked != null;
    }

    // === Saca N candidatos (NO consume) para “3 opciones” ===
    public bool SampleCandidates(int count, out List<WeaponUpgradeSO> picks, bool allowReuseIfInsufficient = false)
    {
        picks = new List<WeaponUpgradeSO>();
        if (count <= 0 || allUpgrades == null || allUpgrades.Count == 0) return false;

        if (!BuildCandidateList(out var candidates))
        {
            if (!allowReuseIfInsufficient && !allowFallbackReuse) return false;
            candidates = new List<WeaponUpgradeSO>(allUpgrades); // fallback: reusar
        }

        // Muestreo ponderado SIN reemplazo
        count = Mathf.Min(count, candidates.Count);
        for (int i = 0; i < count; i++)
        {
            var pick = WeightedPick(candidates);
            if (pick == null) break;
            picks.Add(pick);
            candidates.Remove(pick);
        }
        return picks.Count > 0;
    }

    bool BuildCandidateList(out List<WeaponUpgradeSO> candidates)
    {
        candidates = new List<WeaponUpgradeSO>();
        foreach (var so in allUpgrades)
        {
            if (so == null) continue;
            if (!so.unique) { candidates.Add(so); continue; }
            if (!IsUsed(so)) candidates.Add(so);
        }
        return candidates.Count > 0;
    }

    WeaponUpgradeSO WeightedPick(List<WeaponUpgradeSO> candidates)
    {
        float total = 0f;
        foreach (var c in candidates) total += Mathf.Max(0f, c.weight);
        if (total <= 0f) return candidates[Random.Range(0, candidates.Count)];
        float r = Random.value * total, acc = 0f;
        foreach (var c in candidates)
        {
            acc += Mathf.Max(0f, c.weight);
            if (r <= acc) return c;
        }
        return candidates[candidates.Count - 1];
    }
}
