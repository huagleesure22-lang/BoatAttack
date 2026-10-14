using UnityEngine;

namespace IslandQuest2019
{
    public class QuestCamera : MonoBehaviour
    {
        public Transform target;
        public float distance=5, yaw=180,pitch=18;
        public bool Controlling { get { return Cursor.lockState==CursorLockMode.Locked; } }
        void Start() { Cursor.lockState=CursorLockMode.None;Cursor.visible=true; }
        void Lock()
        {
            Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
            QuestHero hero=target ? target.GetComponent<QuestHero>() : null;
            if(hero && hero.game) hero.game.Begin();
        }
        void Update()
        {
            if(QuestInput.Pressed(KeyCode.Escape)){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
            else if(QuestInput.MousePressed(0) && !Controlling) Lock();
            if(!Controlling) return;
            yaw+=QuestInput.Look.x*2.3f; pitch=Mathf.Clamp(pitch-QuestInput.Look.y*2,-15,65);
            distance=Mathf.Clamp(distance-QuestInput.Scroll*3,2.5f,8);
        }
        void LateUpdate()
        {
            if(!target) return;
            Vector3 focus=target.position+Vector3.up*1.45f;
            Quaternion rot=Quaternion.Euler(pitch,yaw,0);
            Vector3 direction=rot*Vector3.back;
            RaycastHit[] hits=Physics.SphereCastAll(focus,.18f,direction,distance,~0,QueryTriggerInteraction.Ignore);
            float safe=distance;
            foreach(RaycastHit hit in hits)
                if(!hit.transform.IsChildOf(target) && hit.collider.GetComponentInParent<QuestHero>()==null && hit.collider.GetComponentInParent<QuestSkeleton>()==null)
                    safe=Mathf.Min(safe,Mathf.Max(.45f,hit.distance-.15f));
            transform.position=focus+direction*safe;transform.LookAt(focus);
        }
        void OnDisable(){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        void OnApplicationFocus(bool focused)
        {if(!focused){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}}
    }
}
