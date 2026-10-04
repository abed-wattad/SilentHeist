using UnityEngine;
namespace SilentHeist
{
    public sealed class GridActor : MonoBehaviour
    {
        public Cell Cell {get;private set;}
        public Cell Target {get;private set;}
        public bool Moving {get;private set;}
        public int Facing=1;
        private SpriteRenderer sprite;
        private Vector3 origin;
        private float progress,duration;
        public void Initialize(Cell cell,bool guard)
        {
            Cell=Target=cell;transform.position=new Vector3(cell.X,cell.Y,0);
            sprite=PixelArt.Draw(transform,guard?"Guard art":"Thief art",guard?"guard":"player",new Vector2(0,.12f),new Vector2(.9f,1.18f),10,Color.white);
            PixelArt.Draw(transform,"Shadow","solid",new Vector2(0,-.39f),new Vector2(.6f,.09f),3,new Color(0,0,0,.35f));
        }
        public void Begin(Cell destination,float speed)
        {
            if(Moving||destination==Cell)return;
            Target=destination;origin=transform.position;progress=0;duration=1f/Mathf.Max(.1f,speed);Moving=true;
            if(destination.X!=Cell.X)Facing=destination.X>Cell.X?1:-1;
        }
        public void Tick(float delta)
        {
            if(Moving)
            {
                progress+=delta/duration;transform.position=Vector3.Lerp(origin,new Vector3(Target.X,Target.Y,0),Mathf.Clamp01(progress));
                if(progress>=1){Cell=Target;Moving=false;}
            }
            sprite.flipX=Facing<0;
            sprite.transform.localPosition=new Vector3(0,.12f+(Moving?Mathf.Sin(progress*Mathf.PI*2)*.035f:0),0);
            sprite.transform.localRotation=Quaternion.Euler(0,0,Moving?Mathf.Sin(progress*Mathf.PI*2)*3:0);
        }
        public void Teleport(Cell c){Cell=Target=c;Moving=false;transform.position=new Vector3(c.X,c.Y,0);}
        public bool Occupies(Cell c){return Cell==c||(Moving&&Target==c);}
        public void Tint(Color c){sprite.color=c;}
    }
}
