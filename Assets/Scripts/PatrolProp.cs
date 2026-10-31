using UnityEngine;

// A prop that walks back and forth around its spot, turning at both ends.
public class PatrolProp : BuildingProp
{
    [Header("Patrol")]
    public float patrolSpeed = 40f;
    public float patrolRange = 110f;

    float x;
    int dir = 1;

    protected override void OnSetup()
    {
        x = Random.Range(-patrolRange, patrolRange);
        dir = Random.value < 0.5f ? 1 : -1;
    }

    protected override void Move(float dt, ref Vector2 shift, ref float flip)
    {
        x += dir * patrolSpeed * dt;
        if (x > patrolRange) dir = -1;
        else if (x < -patrolRange) dir = 1;
        shift.x += x;
        if (dir < 0) flip = -1f;
    }
}
