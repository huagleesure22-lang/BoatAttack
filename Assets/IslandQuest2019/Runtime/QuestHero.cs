using UnityEngine;

namespace IslandQuest2019
{
    [RequireComponent(typeof(CharacterController))]
    public class QuestHero : MonoBehaviour
    {
        public QuestGame game;
        public QuestCamera view;
        public Transform visual, weaponMount;
        public GameObject[] swords;
        public int weaponLevel = 1;
        public float health = 100;
        public float walkSpeed = 4.5f, runSpeed = 7;
        CharacterController controller;
        QuestPose pose;
        float vertical, attackTimer, attackCooldown, invulnerable;
        bool strikeDone;
        Vector3 spawn;

        void Start()
        {
            controller = GetComponent<CharacterController>(); pose = GetComponent<QuestPose>(); spawn = transform.position;
            health=100;SetWeapon(1);
        }
        void Update()
        {
            if (!game || !game.CanSimulate) { if (pose) pose.Animate(0,0); return; }
            float dt=Time.deltaTime; invulnerable -= dt; attackCooldown -= dt;
            Vector2 input=QuestInput.Movement;
            Vector3 forward=Vector3.ProjectOnPlane(view.transform.forward,Vector3.up).normalized;
            Vector3 right=Vector3.Cross(Vector3.up,forward);
            Vector3 move=forward*input.y+right*input.x;
            bool running=QuestInput.Held(KeyCode.LeftShift);
            if (move.sqrMagnitude>.01f) transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(move),dt*14);
            if (controller.isGrounded && vertical<0) vertical=-2;
            if (controller.isGrounded && QuestInput.Pressed(KeyCode.Space)) vertical=7;
            vertical-=20*dt;
            controller.Move((move*(running ? runSpeed : walkSpeed)+Vector3.up*vertical)*dt);
            if (transform.position.y<spawn.y-20)
            { controller.enabled=false;transform.position=spawn;controller.enabled=true; vertical=0;TakeDamage(10); }
            if (QuestInput.MousePressed(0) && attackCooldown<=0)
            { attackTimer=.52f;attackCooldown=.65f;strikeDone=false; }
            float swing=0;
            if (attackTimer>0)
            {
                attackTimer-=dt; swing=1-Mathf.Clamp01(attackTimer/.52f);
                if (!strikeDone && swing>=.4f)
                {
                    strikeDone=true;
                    foreach (Collider c in Physics.OverlapSphere(transform.position+transform.forward*.9f+Vector3.up,1.35f))
                    {
                        QuestSkeleton enemy=c.GetComponentInParent<QuestSkeleton>();
                        if (enemy && !enemy.Dead && Vector3.Dot(transform.forward,(enemy.transform.position-transform.position).normalized)>-.15f)
                            enemy.Hit(QuestRules.Damage(weaponLevel),transform.forward);
                    }
                }
            }
            if (pose) pose.Animate(input.magnitude*(running ? 1.5f : 1),swing);
            if (QuestInput.Pressed(KeyCode.E))
            {
                QuestChest closest=null;float distance=2.5f;
                foreach (QuestChest chest in FindObjectsOfType<QuestChest>())
                { float d=Vector3.Distance(transform.position,chest.transform.position);if(d<distance){closest=chest;distance=d;} }
                if(closest) closest.Open(this);else game.Notice("靠近宝箱后按 E / Move closer to a chest");
            }
        }
        public void TakeDamage(float damage)
        {
            if (health<=0 || invulnerable>0) return;
            health=Mathf.Max(0,health-damage);invulnerable=.75f;
            if (game) game.Notice("受到攻击 / Hit  -"+damage);
        }
        public void SetWeapon(int level)
        {
            weaponLevel=Mathf.Clamp(level,1,3);
            for(int i=0;i<swords.Length;i++) if(swords[i]) swords[i].SetActive(i==weaponLevel-1);
        }
    }
}
