using UnityEngine;

namespace IslandQuest2019
{
    [RequireComponent(typeof(CharacterController))]
    public class QuestSkeleton : MonoBehaviour
    {
        public QuestHero hero;
        public QuestGame game;
        public float health=66;
        public float speed=2.1f;
        public bool Dead {get{return health<=0;}}
        CharacterController controller;
        QuestPose pose;
        float cooldown,vertical,stagger,deathTime;
        Vector3 home,knockback;
        void Start(){controller=GetComponent<CharacterController>();pose=GetComponent<QuestPose>();home=transform.position;}
        void Update()
        {
            if(Dead)
            {
                deathTime+=Time.deltaTime;
                transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.Euler(85,transform.eulerAngles.y,0),Time.deltaTime*4);
                if(deathTime>2.5f) Destroy(gameObject);
                return;
            }
            if(!hero || hero.health<=0) {pose.Animate(0,0);return;}
            float dt=Time.deltaTime;cooldown-=dt;stagger-=dt;
            Vector3 delta=hero.transform.position-transform.position;delta.y=0;float range=delta.magnitude;
            Vector3 move=Vector3.zero;
            if(stagger>0) move=knockback;
            else if(range<11 && Vector3.Distance(transform.position,home)<15)
            {
                if(range>.05f) transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(delta),dt*6);
                if(range>1.45f) move=delta.normalized*speed;
                else if(cooldown<=0)
                { cooldown=1.4f;hero.TakeDamage(12); }
            }
            else if(Vector3.Distance(transform.position,home)>1) move=(home-transform.position).normalized*speed;
            if(controller.isGrounded && vertical<0) vertical=-2;vertical-=20*dt;
            controller.Move((move+Vector3.up*vertical)*dt);
            pose.Animate(move.magnitude/speed,cooldown>1 ? (1.4f-cooldown)/.4f : 0);
        }
        public void Hit(int damage,Vector3 direction)
        {
            if(Dead) return; health-=damage;stagger=.2f;knockback=direction*3.5f;
            if(Dead){controller.enabled=false;game.EnemyDefeated();}
        }
        void OnGUI()
        {
            if(Dead || !game || !game.showHUD || !Camera.main) return;
            Vector3 screen=Camera.main.WorldToScreenPoint(transform.position+Vector3.up*2.2f);
            if(screen.z<=0) return;
            GUI.Box(new Rect(screen.x-36,Screen.height-screen.y,72,22),Mathf.CeilToInt(health)+" HP");
        }
    }
}
