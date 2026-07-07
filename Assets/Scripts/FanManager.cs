using System;
using UnityEngine;

/// <summary>
/// シーンをまたいでファン数を管理します。
/// </summary>
public class FanManager : MonoBehaviour
{
    private static FanManager instance;

    public static FanManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<FanManager>();

                if (instance == null)
                {
                    GameObject managerObject = new GameObject(nameof(FanManager));
                    instance = managerObject.AddComponent<FanManager>();
                }
            }

            return instance;
        }
    }

    public int FanCount { get; private set; }
    public event Action<int> FanCountChanged;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddFan()
    {
        FanCount++;
        FanCountChanged?.Invoke(FanCount);
    }

    public void ResetFanCount()
    {
        FanCount = 0;
        FanCountChanged?.Invoke(FanCount);
    }
}
