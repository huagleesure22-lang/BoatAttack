using UnityEngine;

namespace IslandQuest2019
{
    public class QuestChest : MonoBehaviour
    {
        public Transform lid;
        public QuestGame game;
        public int requiredKills=2;
        public bool opened;
        public void Open(QuestHero hero)
        {
            if(opened){game.Notice("宝箱已开启 / Already opened");return;}
            if(!QuestRules.CanOpen(game.kills,requiredKills,opened))
            {game.Notice("需要击败 "+requiredKills+" 只骷髅 / Defeat "+requiredKills+" skeletons first");return;}
            opened=true;hero.SetWeapon(QuestRules.Upgrade(hero.weaponLevel));
            hero.health=Mathf.Min(100,hero.health+30);
            game.Notice("武器升级 Lv."+hero.weaponLevel+" · 生命恢复 +30");
        }
        void Update(){if(lid) lid.localRotation=Quaternion.Slerp(lid.localRotation,Quaternion.Euler(opened ? -105 : 0,0,0),Time.deltaTime*5);}
    }
}
