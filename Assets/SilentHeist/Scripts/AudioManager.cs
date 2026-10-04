using System.Collections.Generic;
using UnityEngine;
namespace SilentHeist
{
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance {get;private set;}
        private AudioSource effects,music;
        private readonly Dictionary<string,AudioClip> cues=new Dictionary<string,AudioClip>();
        public bool Muted {get;private set;}
        private void Awake()
        {
            if(Instance!=null&&Instance!=this){Destroy(gameObject);return;}Instance=this;
            effects=gameObject.AddComponent<AudioSource>();music=gameObject.AddComponent<AudioSource>();effects.spatialBlend=0;music.spatialBlend=0;
            cues["loot"]=Tone("loot",660,1100,.16f);cues["dig"]=Tone("dig",140,55,.15f);
            cues["trap"]=Tone("trap",200,85,.23f);cues["alert"]=Tone("alert",350,500,.18f);
            cues["win"]=Tone("win",520,1040,.55f);cues["fail"]=Tone("fail",300,65,.55f);
            cues["switch"]=Tone("switch",320,720,.22f);
            // Original synthesized ambient arpeggio, six seconds, seamless envelope per note.
            int rate=22050;float[] samples=new float[rate*6];float[] notes={110,164.81f,220,164.81f,130.81f,196,261.63f,196};
            for(int i=0;i<samples.Length;i++)
            {
                float t=(float)i/rate;float beat=t/.75f;int n=(int)beat;float env=Mathf.Pow(1-(beat-n),2);
                samples[i]=Mathf.Sin(t*notes[n%8]*Mathf.PI*2)*env*.07f;
            }
            music.clip=AudioClip.Create("Original stealth ambience",samples.Length,1,rate,false);music.clip.SetData(samples,0);music.loop=true;music.volume=.65f;music.Play();
            Muted=PlayerPrefs.GetInt("SilentHeist.Muted",0)==1;ApplyMute();
        }
        private AudioClip Tone(string name,float start,float end,float seconds)
        {
            int rate=22050;float[] data=new float[(int)(rate*seconds)];float phase=0;
            for(int i=0;i<data.Length;i++){float f=(float)i/data.Length;phase+=Mathf.Lerp(start,end,f)*Mathf.PI*2/rate;data[i]=Mathf.Sin(phase)*Mathf.Sin(f*Mathf.PI)*.22f;}
            var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        public void PlayCue(string cue){AudioClip clip;if(cues.TryGetValue(cue,out clip))effects.PlayOneShot(clip);}
        public void ToggleMute(){Muted=!Muted;PlayerPrefs.SetInt("SilentHeist.Muted",Muted?1:0);PlayerPrefs.Save();ApplyMute();}
        private void ApplyMute(){music.mute=Muted;effects.mute=Muted;}
        private void OnDestroy(){if(Instance==this)Instance=null;foreach(var p in cues)Destroy(p.Value);if(music!=null&&music.clip!=null)Destroy(music.clip);}
    }
}
