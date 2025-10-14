using UnityEngine;
using System.Collections.Generic;

/// Pool global de mejoras para evitar repetidos y ofrecer candidatos de Armory.
/// Pon uno en la escena inicial (DontDestroyOnLoad) y arrástrale la lista completa de SOs.
public class UpgradePoolManager : MonoBehaviour
{
    public static UpgradePoolManager Instance { get; private set; }

    [Header("Lista completa de mejoras (ScriptableObjects)")]
    public List<WeaponUpgradeSO> allUpgrades = new();

    [Header("Debug")]
    public bool autoFillFromResources = false; // si true, carga todos los SO en Resources
    HashSet<WeaponUpgradeSO> used = new();

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (autoFillFromResources && (allUpgrades == null || allUpgrades.Count == 0))
        {
            var loaded = Resources.LoadAll<WeaponUpgradeSO>("");
            allUpgrades = new List<WeaponUpgradeSO>(loaded);
        }
    }

    public void ResetPool() => used.Clear();

    public bool DrawRandomUnique(out WeaponUpgradeSO so)
    {
        so = null;
        var candidates = GetUnusedList();
        if (candidates.Count == 0) return false;

        int i = UnityEngine.Random.Range(0, candidates.Count);
        so = candidates[i];
        used.Add(so);
        return true;
    }

    public bool SampleCandidates(int count, out List<WeaponUpgradeSO> result)
    {
        result = new List<WeaponUpgradeSO>(count);
        var candidates = GetUnusedList();
        if (candidates.Count == 0) return false;

        if (count >= candidates.Count)
        {
            result.AddRange(candidates);
            return true;
        }

        // muestreo sin reemplazo
        for (int k = 0; k < count; k++)
        {
            int idx = UnityEngine.Random.Range(0, candidates.Count);
            result.Add(candidates[idx]);
            candidates.RemoveAt(idx);
        }
        return true;
    }

    public void MarkUsed(WeaponUpgradeSO so)
    {
        if (!so) return;
        used.Add(so);
    }

    List<WeaponUpgradeSO> GetUnusedList()
    {
        var list = new List<WeaponUpgradeSO>();
        if (allUpgrades != null)
        {
            foreach (var u in allUpgrades)
                if (u && !used.Contains(u)) list.Add(u);
        }
        return list;
    }
}
