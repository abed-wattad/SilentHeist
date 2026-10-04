using UnityEngine;
namespace SilentHeist
{
    public sealed class SecurityCamera : MonoBehaviour
    {
        private GameManager game;
        private Cell cell;
        private Mesh mesh;
        private MeshRenderer coneRenderer;
        private Material coneMaterial;
        private float phase,angle;
        private readonly Vector3[] vertices=new Vector3[26];
        private readonly int[] triangles=new int[72];
        private const float Range=6f,HalfAngle=24f;
        private SpriteRenderer body;
        public void Initialize(GameManager manager,Cell position)
        {
            game=manager;cell=position;phase=position.X*.37f+position.Y*.53f;transform.position=new Vector3(cell.X,cell.Y,0);
            body=PixelArt.Draw(transform,"Camera body","camera",Vector2.zero,new Vector2(.85f,.85f),5,Color.white);
            var cone=new GameObject("Visible, wall-clipped field of view");cone.transform.SetParent(transform,false);
            mesh=new Mesh();mesh.name="Camera vision fan";cone.AddComponent<MeshFilter>().sharedMesh=mesh;
            coneRenderer=cone.AddComponent<MeshRenderer>();coneMaterial=new Material(Shader.Find("Sprites/Default"));coneMaterial.color=new Color(1,.66f,.2f,.15f);coneRenderer.sharedMaterial=coneMaterial;coneRenderer.sortingOrder=0;
            for(int i=0;i<24;i++){triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=i+2;}
            UpdateCone();
        }
        public bool Tick(float elapsed)
        {
            if(game.SecurityDisabled){coneRenderer.enabled=false;body.color=new Color(.35f,.5f,.5f);return false;}
            angle=-90+Mathf.Sin(elapsed*.65f+phase)*52;UpdateCone();
            Vector2 d=(Vector2)game.Player.transform.position-(Vector2)transform.position;
            Vector2 direction=new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad));
            bool sees=d.magnitude<Range&&Vector2.Angle(direction,d)<HalfAngle&&game.World.ClearSight(cell,game.Player.Cell);
            coneMaterial.color=sees?new Color(1,.25f,.2f,.26f):new Color(1,.72f,.25f,.13f);
            return sees;
        }
        private void UpdateCone()
        {
            vertices[0]=Vector3.zero;
            for(int i=0;i<=24;i++)
            {
                float a=(angle-HalfAngle+i*(HalfAngle*2/24))*Mathf.Deg2Rad;
                Vector2 dir=new Vector2(Mathf.Cos(a),Mathf.Sin(a));float distance=Range;
                for(float r=.15f;r<Range;r+=.12f)
                {
                    Vector2 p=(Vector2)transform.position+dir*r;
                    if(game.World.Solid(new Cell(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.y)))){distance=r;break;}
                }
                vertices[i+1]=dir*distance;
            }
            mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateBounds();
        }
        private void OnDestroy(){if(mesh!=null)Destroy(mesh);if(coneMaterial!=null)Destroy(coneMaterial);}
    }
}
