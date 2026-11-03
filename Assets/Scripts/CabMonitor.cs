using UnityEngine;

// The little screen in the cab. With the doors open it shows the platform, so the driver can see everyone on;
// the rest of the time it shows the car behind them, which is nothing anyone needs to keep looking at.
public class CabMonitor : MonoBehaviour
{
    const int RenderEvery = 3, FeedWidth = 320, FeedHeight = 240;

    public Camera feed;
    public Transform saloonSpot, platformSpot;
    public TrainDoors doors;
    public FirstPersonController player;
    public Renderer screen;

    void Awake()
    {
        var target = new RenderTexture(FeedWidth, FeedHeight, 16) { name = "CabMonitorFeed", filterMode = FilterMode.Bilinear };
        feed.targetTexture = target;
        feed.enabled = false;
        screen.material.SetTexture("_BaseMap", target);
        screen.enabled = false;
    }

    void LateUpdate()
    {
        screen.enabled = player.Seated;
        feed.enabled = player.Seated && Time.frameCount % RenderEvery == 0;

        Transform spot = doors.IsOpen ? platformSpot : saloonSpot;
        feed.transform.SetPositionAndRotation(spot.position, spot.rotation);
    }

    void OnDestroy()
    {
        if (feed.targetTexture != null) feed.targetTexture.Release();
    }
}
