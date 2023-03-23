using UnityEngine;

public class FPSCheck : MonoBehaviour
{
    public int TargetFrameRate = 75;

    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = TargetFrameRate;
    }
}
