using UnityEngine;

[CreateAssetMenu(fileName = "Effect_Homing", menuName = "Spells/Effects/Homing")]
public class Effect_Homing : SpellEffect
{
    public float rotationSpeed = 5f;
    public float findTargetRadius = 1f;
    public LayerMask targetLayer;

    public override void OnUpdate(IProjectile projectile)
    {
        Transform target = null;

        var colliders = Physics.OverlapSphere(projectile.Transform.position, findTargetRadius, targetLayer);
        if (colliders.Length > 0)
        {
            target = colliders[0].transform; // Just grab the first one
        }

        if (target != null)
        {
            Vector3 targetDir = (target.position - projectile.Transform.position).normalized;
            Vector3 newDir = Vector3.RotateTowards(projectile.Direction, targetDir, 
                rotationSpeed * Time.deltaTime, 0f);
            
            projectile.SetDirection(newDir);
        }
    }
}