using UnityEngine;
namespace SilentHeist
{
    public enum GuardState { Patrol, Chase, Search, Trapped }
    public sealed class GuardController : MonoBehaviour
    {
        public GridActor Actor {get;private set;}
        public GuardState State {get;private set;}
        public float TrappedRemaining;
        private GameManager game;
        private float searchRemaining,decisionDelay;
        private Cell lastSeen;
        private SpriteRenderer indicator;
        private int patrolDirection=-1;
        public void Initialize(GameManager manager,Cell start)
        {
            game=manager;Actor=gameObject.AddComponent<GridActor>();Actor.Initialize(start,true);Actor.Facing=-1;
            indicator=PixelArt.Draw(transform,"Alert light","spark",new Vector2(0,1),new Vector2(.28f,.28f),12,PixelArt.Gold);
        }
        public void Trap()
        {
            if(State==GuardState.Trapped)return;
            State=GuardState.Trapped;TrappedRemaining=game.Config.guardTrappedTime;
            game.Effects.Burst(transform.position,PixelArt.Gold);game.Audio.PlayCue("trap");
        }
        public void Tick(float dt)
        {
            Actor.Tick(dt);
            if(!Actor.Moving&&game.World.Holes.Contains(Actor.Cell)&&State!=GuardState.Trapped)Trap();
            if(State==GuardState.Trapped)
            {
                TrappedRemaining-=dt;indicator.color=PixelArt.Cyan;
                if(TrappedRemaining<=0)
                {
                    Cell up=Actor.Cell+new Cell(0,1);
                    // Emerge only when safe; never materialize on top of the player.
                    if(!game.World.Solid(up)&&!game.Player.Occupies(up))
                    {
                        Cell left=up+new Cell(-1,0),right=up+new Cell(1,0);
                        Cell escape=game.World.Solid(left)?right:left;
                        if(!game.World.Solid(escape)&&!game.Player.Occupies(escape))
                        {Actor.Teleport(escape);State=GuardState.Search;searchRemaining=1.2f;lastSeen=escape;}
                    }
                }
                return;
            }
            Vector3 delta=game.Player.transform.position-Actor.transform.position;
            bool sees=Mathf.Abs(delta.y)<.7f&&Mathf.Abs(delta.x)<=game.Config.guardVision&&delta.x*Actor.Facing>=0&&game.World.ClearSight(Actor.Cell,game.Player.Cell);
            if(sees)
            {
                if(State!=GuardState.Chase){game.RegisterAlert();game.Audio.PlayCue("alert");game.Effects.Burst(transform.position+Vector3.up,Color.red);}
                State=GuardState.Chase;lastSeen=game.Player.Cell;searchRemaining=game.Config.searchTime;
            }
            else if(State==GuardState.Chase){State=GuardState.Search;searchRemaining=game.Config.searchTime;}
            if(State==GuardState.Search){searchRemaining-=dt;if(searchRemaining<=0)State=GuardState.Patrol;}
            indicator.enabled=State!=GuardState.Patrol;indicator.color=State==GuardState.Chase?new Color(1,.35f,.3f):PixelArt.Gold;
            if(Actor.Moving)return;
            if(game.World.Falling(Actor.Cell)){Actor.Begin(Actor.Cell+new Cell(0,-1),8);return;}
            decisionDelay-=dt;if(decisionDelay>0)return;
            if(State==GuardState.Chase||State==GuardState.Search)
            {
                Cell target=game.World.NextStep(Actor.Cell,lastSeen);
                if(target!=Actor.Cell)Actor.Begin(target,game.Config.guardChaseSpeed);
                else {decisionDelay=.2f;if(State==GuardState.Search)Actor.Facing=-Actor.Facing;}
            }
            else
            {
                Cell dir=new Cell(patrolDirection,0),next=Actor.Cell+dir;
                if(game.World.CanMove(Actor.Cell,dir))Actor.Begin(next,game.Config.guardPatrolSpeed);
                else{patrolDirection=-patrolDirection;Actor.Facing=patrolDirection;decisionDelay=.45f;}
            }
        }
    }
}
