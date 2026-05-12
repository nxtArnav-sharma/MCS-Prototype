using UnityEngine;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    [Header("Cinemachine")]
    public CinemachineFreeLook freeLookCam;
    public CinemachineVirtualCamera lockOnCam;

    [Header("Lock-On")]
    public float lockOnRange = 12f;
    public LayerMask enemyLayer;

    private Transform lockedTarget;
    private bool isLockedOn;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) ToggleLockOn();
        if (isLockedOn && lockedTarget == null) ExitLockOn();
    }

    void ToggleLockOn()
    {
        if (isLockedOn) { ExitLockOn(); return; }

        // Find nearest enemy
        Collider[] enemies = Physics.OverlapSphere(transform.position, lockOnRange, enemyLayer);
        if (enemies.Length == 0) return;

        float minDist = Mathf.Infinity;
        Transform nearest = null;
        foreach (var e in enemies)
        {
            float d = Vector3.Distance(transform.position, e.transform.position);
            if (d < minDist) { minDist = d; nearest = e.transform; }
        }

        if (nearest == null) return;
        lockedTarget = nearest;
        isLockedOn = true;
        lockOnCam.LookAt = lockedTarget;
        freeLookCam.Priority = 0;
        lockOnCam.Priority  = 20;
    }

    void ExitLockOn()
    {
        isLockedOn = false;
        lockedTarget = null;
        freeLookCam.Priority = 10;
        lockOnCam.Priority   = 0;
    }
}
