using UnityEngine;
using UnityEngine.AI;

namespace Tidebound
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class TideEnemy : MonoBehaviour
    {
        public int health = 48;
        public int attackDamage = 12;
        public TideAdventure adventure;
        public int encounterId;
        public Transform body;
        public Renderer core;
        public bool Dead => health <= 0;
        NavMeshAgent agent;
        Vector3 home;
        float nextPath, nextAttack, strikeAt = -1, stunnedUntil;
        MaterialPropertyBlock color;
        void Awake() { agent = GetComponent<NavMeshAgent>(); color = new MaterialPropertyBlock(); }
        void Start() { home = transform.position; }

        void Update()
        {
            if (Dead || !agent.isOnNavMesh) return;
            agent.isStopped = !adventure.IsPlaying || Time.time < stunnedUntil;
            if (!adventure.IsPlaying) return;
            body.localPosition = Vector3.up * (.85f + Mathf.Sin(Time.time * 3 + encounterId) * .1f);
            if (Time.time < stunnedUntil) { strikeAt = -1; Telegraph(false); return; }
            float distance = Vector3.Distance(transform.position, adventure.Player.transform.position);
            bool chase = distance < 28 && Vector3.Distance(adventure.Player.transform.position, home) < 45;
            if (Time.time >= nextPath)
            {
                agent.SetDestination(chase ? adventure.Player.transform.position : home);
                nextPath = Time.time + .25f;
            }
            if (strikeAt >= 0)
            {
                agent.isStopped = true;
                if (Time.time >= strikeAt)
                {
                    var from = transform.position + Vector3.up;
                    var to = adventure.Player.transform.position + Vector3.up;
                    bool clear = !Physics.Linecast(from, to, out var hit, ~0, QueryTriggerInteraction.Ignore) ||
                        hit.transform.GetComponentInParent<TidePlayer>() == adventure.Player;
                    if (distance < 2.3f && clear) adventure.Player.Hurt(attackDamage);
                    strikeAt = -1;
                    nextAttack = Time.time + 1.3f;
                    Telegraph(false);
                }
            }
            else if (chase && distance < 2 && Time.time >= nextAttack)
            {
                strikeAt = Time.time + .7f;
                Telegraph(true);
            }
        }

        void Telegraph(bool charging)
        {
            color.SetColor("_BaseColor", charging ? new Color(1, .22f, .3f) : new Color(.08f, .6f, .7f));
            core.SetPropertyBlock(color);
        }

        public void Hit(int damage, Vector3 knockback)
        {
            if (Dead || !adventure.IsPlaying) return;
            health = Mathf.Max(0, health - damage);
            if (health == 0)
            {
                if (adventure.Quest.Defeat(encounterId)) adventure.DropFragment(encounterId, transform.position);
                Destroy(gameObject);
                return;
            }
            if (knockback.sqrMagnitude > 0 && agent.isOnNavMesh)
            {
                // Trace on the NavMesh, so knockback cannot teleport a monster through a wall or into the sea.
                var end = transform.position + Vector3.ProjectOnPlane(knockback, Vector3.up);
                if (NavMesh.Raycast(transform.position, end, out var edge, NavMesh.AllAreas)) end = edge.position;
                if (NavMesh.SamplePosition(end, out var ground, 1, NavMesh.AllAreas)) agent.Warp(ground.position);
                stunnedUntil = Time.time + .65f;
                strikeAt = -1;
                Telegraph(false);
            }
        }
    }

    internal sealed class TideFragment : MonoBehaviour
    {
        public TideAdventure Adventure;
        public int EncounterId;
        Vector3 origin;
        void Start() { origin = transform.position; }
        void Update()
        {
            if (!Adventure.IsPlaying) return;
            transform.position = origin + Vector3.up * Mathf.Sin(Time.time * 3) * .15f;
            transform.Rotate(0, 60 * Time.deltaTime, 0);
            if (Vector3.Distance(Adventure.Player.transform.position + Vector3.up, transform.position) < 1.8f &&
                Adventure.Quest.Collect(EncounterId))
            {
                Adventure.Player.Heal(8);
                Adventure.Notify("夺回一枚灯芯碎片。生命恢复 8。", 2);
                Destroy(gameObject);
            }
        }
    }
}
