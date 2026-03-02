using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Pool de TooltipUI optimizado para VR.
/// No modifica escala.
/// </summary>
public class TooltipPool : MonoBehaviour
{
    public static TooltipPool Instance { get; private set; }

    [Header("Pool Config")]
    public TooltipUI prefab;
    public int initialSize = 10;

    private readonly Queue<TooltipUI> pool = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        for (int i = 0; i < initialSize; i++)
            CreateNewInstance();
    }

    private void CreateNewInstance()
    {
        if (prefab == null) return;

        var inst = Instantiate(prefab, transform);
        inst.Clear();
        pool.Enqueue(inst);
    }

    public TooltipUI Get()
    {
        if (pool.Count == 0)
            CreateNewInstance();

        var inst = pool.Dequeue();
        inst.gameObject.SetActive(true);
        return inst;
    }

    public void ReturnToPool(TooltipUI tooltip)
    {
        if (tooltip == null) return;

        tooltip.Clear();
        tooltip.transform.SetParent(transform);
        pool.Enqueue(tooltip);
    }
}