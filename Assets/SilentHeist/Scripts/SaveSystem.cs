using UnityEngine;
namespace SilentHeist
{
    public static class SaveSystem
    {
        public static int Unlocked {get{return Mathf.Clamp(PlayerPrefs.GetInt("SilentHeist.Unlocked",1),1,3);}}
        public static float Best(int level){return PlayerPrefs.GetFloat("SilentHeist.Best."+level,0);}
        public static bool Silent(int level){return PlayerPrefs.GetInt("SilentHeist.Silent."+level,0)==1;}
        public static void Complete(int level,float time,int alerts)
        {
            float best=Best(level);if(best==0||time<best)PlayerPrefs.SetFloat("SilentHeist.Best."+level,time);
            if(alerts==0)PlayerPrefs.SetInt("SilentHeist.Silent."+level,1);
            PlayerPrefs.SetInt("SilentHeist.Unlocked",Mathf.Max(Unlocked,Mathf.Min(3,level+2)));PlayerPrefs.Save();
        }
    }
}
