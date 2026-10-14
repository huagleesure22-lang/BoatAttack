using UnityEngine;
namespace IslandQuest2019
{
    public class GalleryCamera : MonoBehaviour
    {
        public Vector3 focus=new Vector3(0,1,0);
        float yaw=20,pitch=15,distance=8;
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Alpha0)){focus=new Vector3(0,1,0);distance=8;yaw=20;pitch=15;}
            if(Input.GetKeyDown(KeyCode.Alpha1)){focus=new Vector3(-2,1.15f,0);distance=3.8f;yaw=20;pitch=12;}
            if(Input.GetKeyDown(KeyCode.Alpha2)){focus=new Vector3(0,1,0);distance=3.8f;yaw=20;pitch=12;}
            if(Input.GetKeyDown(KeyCode.Alpha3)){focus=new Vector3(2.1f,.75f,1.2f);distance=3.5f;yaw=10;pitch=12;}
            if(Input.GetKeyDown(KeyCode.Alpha4)){focus=new Vector3(2.15f,1,0);distance=3.1f;yaw=25;pitch=20;}
            if(Input.GetMouseButton(0) || Input.GetMouseButton(1))
            {yaw+=Input.GetAxis("Mouse X")*2;pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*2,-5,65);}
            distance=Mathf.Clamp(distance-Input.GetAxis("Mouse ScrollWheel")*3,3,14);
            transform.position=focus+Quaternion.Euler(pitch,yaw,0)*Vector3.forward*distance;
            transform.LookAt(focus);
        }
    }
}
