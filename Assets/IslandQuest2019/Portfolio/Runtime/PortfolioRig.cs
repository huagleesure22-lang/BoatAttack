using UnityEngine;
namespace IslandQuest2019.Portfolio
{
    // Real weighted skin: bones drive SkinnedMeshRenderer through mesh.bindposes and boneWeights.
    public class PortfolioRig : MonoBehaviour
    {
        public Transform[] bones;
        public Vector3[] tails;
        public string assetName;
        public Transform[] boneMarkers;
        public void Pose(float time)
        {
            if(bones==null)return;
            foreach(Transform bone in bones)bone.localRotation=Quaternion.identity;
            if(time==0)return;
            float swing=Mathf.Sin(time*2.3f);
            for(int i=0;i<bones.Length;i++)
            {
                string n=bones[i].name;float sign=n.StartsWith("Left")?1:-1;
                if(n.EndsWith("Arm"))bones[i].localRotation=Quaternion.Euler(swing*18*sign,0,sign*5);
                if(n.EndsWith("Forearm"))bones[i].localRotation=Quaternion.Euler(-12-Mathf.Max(0,swing*sign)*18,0,0);
                if(n.EndsWith("Leg"))bones[i].localRotation=Quaternion.Euler(-swing*16*sign,0,0);
                if(n.EndsWith("Shin"))bones[i].localRotation=Quaternion.Euler(Mathf.Max(0,swing*sign)*20,0,0);
                if(n=="Head")bones[i].localRotation=Quaternion.Euler(0,Mathf.Sin(time)*9,0);
                if(n=="Lid")bones[i].localRotation=Quaternion.Euler(-100*(.5f+.5f*Mathf.Sin(time)),0,0);
            }
        }
        public void DrawBones(bool visible,Camera camera)
        {
            if(boneMarkers==null)return;
            for(int i=0;i<boneMarkers.Length;i++)
            {
                Transform m=boneMarkers[i];if(!m)continue;m.gameObject.SetActive(visible);
                if(!visible)continue;
                // An inspection overlay projects the actual bone axis in front of the skin.
                Vector3 offset=(camera.transform.position-transform.position).normalized*.46f;
                Vector3 a=bones[i].position+offset,b=bones[i].TransformPoint(tails[i])+offset;
                m.position=(a+b)*.5f;m.rotation=Quaternion.FromToRotation(Vector3.up,b-a);m.localScale=new Vector3(.022f,(b-a).magnitude*.5f,.022f);
            }
        }
    }
}
