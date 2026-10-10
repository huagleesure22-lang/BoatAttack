using UnityEngine;

namespace Tidebound
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class TidePlayer : MonoBehaviour
    {
        public float walkSpeed = 5.5f;
        public float sprintSpeed = 8;
        public float jumpHeight = 1.5f;
        public int boltDamage = 18;
        public int waveDamage = 30;
        public float waveRadius = 6;
        public float waveCooldown = 6;
        public Transform visual;
        public TideCamera view;
        public TideAdventure adventure;
        public int Health { get; private set; } = 100;
        public float WaveRemaining => Mathf.Max(0, nextWave - Time.time);
        CharacterController controller;
        Vector3 spawn;
        float fallSpeed, nextBolt, nextWave, invulnerableUntil;

        void Awake() { controller = GetComponent<CharacterController>(); }
        public void SetSpawn(Vector3 point) { spawn = point; }

        void Update()
        {
            if (adventure == null || !adventure.IsPlaying) return;
            var input = TideInput.Move;
            var forward = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(view.transform.right, Vector3.up).normalized;
            var movement = forward * input.y + right * input.x;
            if (movement.sqrMagnitude > .01f)
                visual.rotation = Quaternion.Slerp(visual.rotation, Quaternion.LookRotation(movement), Time.deltaTime * 14);
            if (controller.isGrounded && fallSpeed < 0) fallSpeed = -2;
            if (TideInput.Jump && controller.isGrounded) fallSpeed = Mathf.Sqrt(jumpHeight * 40);
            fallSpeed -= 20 * Time.deltaTime;
            controller.Move((movement * (TideInput.Sprint ? sprintSpeed : walkSpeed) + Vector3.up * fallSpeed) * Time.deltaTime);
            if (transform.position.y < .15f)
            {
                Hurt(20, true);
                controller.enabled = false;
                transform.position = spawn;
                controller.enabled = true;
                fallSpeed = 0;
                adventure.Notify("海潮将你送回岸边。落水损失 20 生命。", 3);
            }
            if (!adventure.IsPlaying) return;
            if (TideInput.Fire && Time.time >= nextBolt) Fire();
            if (TideInput.Wave && Time.time >= nextWave) CastWave();
        }

        void Fire()
        {
            nextBolt = Time.time + .28f;
            // Start inside the controller's protected volume: a muzzle offset could put the bolt beyond a nearby wall.
            var origin = transform.position + Vector3.up * 1.3f;
            var aim = view.transform.position + view.transform.forward * 60;
            var hits = Physics.RaycastAll(view.transform.position, view.transform.forward, 60, ~0, QueryTriggerInteraction.Ignore);
            float nearest = 60;
            foreach (var hit in hits)
                if (!hit.transform.IsChildOf(transform) && hit.distance < nearest)
                { nearest = hit.distance; aim = hit.point; }
            var direction = (aim - origin).normalized;
            var go = new GameObject("Water bolt");
            go.transform.position = origin;
            TideVisuals.Part(go.transform, PrimitiveType.Sphere, "Water", Vector3.zero, Vector3.one * .25f, adventure.WaterMaterial);
            var bolt = go.AddComponent<TideBolt>();
            bolt.Initialize(this, direction, boltDamage, adventure);
        }

        void CastWave()
        {
            nextWave = Time.time + waveCooldown;
            TideVisuals.Wave(transform.position, waveRadius, adventure.WaterMaterial);
            foreach (var enemy in adventure.Enemies)
            {
                if (enemy == null || enemy.Dead) continue;
                var offset = enemy.transform.position - transform.position;
                if (offset.sqrMagnitude > waveRadius * waveRadius) continue;
                var from = transform.position + Vector3.up;
                var to = enemy.transform.position + Vector3.up;
                if (!Physics.Linecast(from, to, out var hit, ~0, QueryTriggerInteraction.Ignore) ||
                    hit.transform.GetComponentInParent<TideEnemy>() == enemy)
                    enemy.Hit(waveDamage, offset.normalized * 3);
            }
        }

        public void Hurt(int amount, bool ignoreInvulnerability = false)
        {
            if (!adventure.IsPlaying || (!ignoreInvulnerability && Time.time < invulnerableUntil)) return;
            Health = Mathf.Max(0, Health - amount);
            invulnerableUntil = Time.time + .8f;
            if (Health == 0) adventure.Lose();
        }

        public void Heal(int amount) { Health = Mathf.Min(100, Health + amount); }
    }

    internal sealed class TideBolt : MonoBehaviour
    {
        TidePlayer owner;
        TideAdventure adventure;
        Vector3 direction;
        int damage;
        float life;
        public void Initialize(TidePlayer player, Vector3 aim, int power, TideAdventure game)
        { owner = player; direction = aim; damage = power; adventure = game; }
        void Update()
        {
            if (!adventure.IsPlaying) return;
            var step = 26 * Time.deltaTime;
            RaycastHit? closest = null;
            foreach (var hit in Physics.SphereCastAll(transform.position, .13f, direction, step, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(owner.transform) && (!closest.HasValue || hit.distance < closest.Value.distance))
                    closest = hit;
            if (closest.HasValue)
            {
                var hit = closest.Value;
                var enemy = hit.transform.GetComponentInParent<TideEnemy>();
                if (enemy != null) enemy.Hit(damage, Vector3.zero);
                TideVisuals.Wave(hit.point, .7f, adventure.WaterMaterial);
                Destroy(gameObject);
                return;
            }
            transform.position += direction * step;
            life += Time.deltaTime;
            if (life > 2.5f) Destroy(gameObject);
        }
    }
}
