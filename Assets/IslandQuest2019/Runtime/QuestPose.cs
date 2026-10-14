using UnityEngine;

namespace IslandQuest2019
{
    // Articulated mesh limbs: no external animation or avatar package required.
    public class QuestPose : MonoBehaviour
    {
        public Transform leftArm,rightArm,leftLeg,rightLeg;
        float phase;
        public void Animate(float speed,float swing)
        {
            phase+=Time.deltaTime*(speed>.1f ? 9*speed : 1.5f);
            float step=Mathf.Sin(phase)*Mathf.Min(speed,1.4f)*28;
            if(leftLeg) leftLeg.localRotation=Quaternion.Euler(step,0,0);
            if(rightLeg) rightLeg.localRotation=Quaternion.Euler(-step,0,0);
            if(leftArm) leftArm.localRotation=Quaternion.Euler(-step*.65f,0,-7);
            if(rightArm)
            {
                float attack=swing>0 ? Mathf.Sin(swing*Mathf.PI) : 0;
                rightArm.localRotation=Quaternion.Euler(step*.5f-attack*90,attack*-60,7+attack*70);
            }
        }
    }
}
