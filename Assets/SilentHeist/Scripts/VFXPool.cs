using System.Collections.Generic;
using UnityEngine;
namespace SilentHeist
{
    public sealed class VFXPool : MonoBehaviour
    {
        private sealed class Spark {public SpriteRenderer Renderer;public Vector3 Velocity;public float Remaining;}
        private readonly Stack<Spark> available=new Stack<Spark>();
        private readonly List<Spark> live=new List<Spark>();
        private void Awake()
        {
            for(int i=0;i<64;i++)
            {
                var s=new Spark{Renderer=PixelArt.Draw(transform,"Pooled spark","solid",Vector2.zero,new Vector2(.08f,.08f),20,Color.white)};
                s.Renderer.gameObject.SetActive(false);available.Push(s);
            }
        }
        public void Burst(Vector3 position,Color color)
        {
            for(int i=0;i<10&&available.Count>0;i++)
            {
                Spark s=available.Pop();s.Renderer.gameObject.SetActive(true);s.Renderer.transform.position=position;s.Renderer.color=color;
                s.Velocity=new Vector3(Random.Range(-2f,2f),Random.Range(.8f,3f),0);s.Remaining=.55f;live.Add(s);
            }
        }
        public void Clear()
        {
            foreach(Spark s in live){s.Renderer.gameObject.SetActive(false);available.Push(s);}live.Clear();
        }
        private void Update()
        {
            float dt=Time.deltaTime;
            for(int i=live.Count-1;i>=0;i--)
            {
                Spark s=live[i];s.Remaining-=dt;
                if(s.Remaining<=0){s.Renderer.gameObject.SetActive(false);available.Push(s);live.RemoveAt(i);continue;}
                s.Velocity+=Vector3.down*dt*6;s.Renderer.transform.position+=s.Velocity*dt;
                Color c=s.Renderer.color;c.a=s.Remaining/.55f;s.Renderer.color=c;
            }
        }
    }
}
