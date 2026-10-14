using UnityEngine;
namespace IslandQuest2019
{
    public class GalleryCamera : MonoBehaviour
    {
        public Vector3 focus=new Vector3(0,1,0);
        float yaw=20,pitch=15,distance=8;
        void Update()
        {
            if(QuestInput.Pressed(KeyCode.Alpha0)){focus=new Vector3(0,1,0);distance=8;yaw=20;pitch=15;}
            if(QuestInput.Pressed(KeyCode.Alpha1)){focus=new Vector3(-2,1.15f,0);distance=3.8f;yaw=20;pitch=12;}
            if(QuestInput.Pressed(KeyCode.Alpha2)){focus=new Vector3(0,1,0);distance=3.8f;yaw=20;pitch=12;}
            if(QuestInput.Pressed(KeyCode.Alpha3)){focus=new Vector3(2.1f,.75f,1.2f);distance=3.5f;yaw=10;pitch=12;}
            if(QuestInput.Pressed(KeyCode.Alpha4)){focus=new Vector3(2.15f,1,0);distance=3.1f;yaw=25;pitch=20;}
            if(QuestInput.MouseHeld(0) || QuestInput.MouseHeld(1))
            {yaw+=QuestInput.Look.x*2;pitch=Mathf.Clamp(pitch-QuestInput.Look.y*2,-5,65);}
            distance=Mathf.Clamp(distance-QuestInput.Scroll*3,3,14);
            transform.position=focus+Quaternion.Euler(pitch,yaw,0)*Vector3.forward*distance;
            transform.LookAt(focus);
        }
    }
}
