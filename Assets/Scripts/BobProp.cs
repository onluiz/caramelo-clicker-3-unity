using UnityEngine;

// A hovering prop that bobs up and down.
[ExecuteAlways]
public class BobProp : BuildingProp
{
    [Header("Bob")]
    public float bobAmplitude = 9f;
    public float bobSpeed = 2.2f;

    float time;

    protected override void OnSetup()
    {
        time = Random.value * 6f;
    }

    protected override void Move(float dt, ref Vector2 shift, ref float flip)
    {
        time += dt;
        shift.y += Mathf.Sin(time * bobSpeed) * bobAmplitude;
    }
}
