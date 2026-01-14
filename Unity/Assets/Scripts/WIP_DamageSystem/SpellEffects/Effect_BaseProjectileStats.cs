using UnityEngine;

[CreateAssetMenu(fileName = "Effect_BaseProjectileStats", menuName = "Spells/Effects/Set Base Projectile Stats")]
public class Effect_BaseProjectileStats : SpellEffect
{
    public float speed = 50f;
    public float lifetime = 5f;
    public float size = 0.1f;
    public float tickRate = 0.5f;

    public override void Initialize(IProjectile projectile)
    {
        projectile.SetStats(speed, lifetime, size, tickRate);
    }
}