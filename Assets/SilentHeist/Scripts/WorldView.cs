using System.Collections.Generic;
using UnityEngine;
namespace SilentHeist
{
    public sealed class WorldView : MonoBehaviour
    {
        private GridWorld world;
        private readonly Dictionary<Cell,SpriteRenderer> tiles=new Dictionary<Cell,SpriteRenderer>();
        private readonly Dictionary<Cell,SpriteRenderer> loot=new Dictionary<Cell,SpriteRenderer>();
        private SpriteRenderer exit;
        public void Build(GridWorld model)
        {
            world=model;
            PixelArt.Draw(transform,"Building silhouette","solid",new Vector2(13,8),new Vector2(27,17),-20,new Color32(17,27,44,255));
            for(int floor=0;floor<4;floor++)
            {
                int y=floor*4+1;
                PixelArt.Draw(transform,"Room wall","solid",new Vector2(13,y+1),new Vector2(25,3),-18,new Color32((byte)(25+floor*2),36,53,255));
                for(int x=2;x<26;x+=4)
                {
                    PixelArt.Draw(transform,"Recess","solid",new Vector2(x,y+1.1f),new Vector2(2.4f,1.6f),-17,new Color32(15,25,41,255));
                    PixelArt.Draw(transform,"Window glass","solid",new Vector2(x,y+1.25f),new Vector2(2.1f,1.25f),-16,new Color32(26,51,66,255));
                    PixelArt.Draw(transform,"Mullion","solid",new Vector2(x,y+1.25f),new Vector2(.09f,1.3f),-15,new Color32(49,73,86,255));
                    PixelArt.Draw(transform,"Light rail","solid",new Vector2(x,y+2.1f),new Vector2(1.5f,.07f),-14,PixelArt.Gold*.7f);
                }
            }
            for(int x=0;x<model.Definition.Width;x++)for(int y=0;y<model.Definition.Height;y++)
            {
                var c=new Cell(x,y);Tile t=model.At(c); if(t==Tile.Empty)continue;
                string art=t==Tile.Wall?"wall":t==Tile.Brick?"brick":t==Tile.Ladder?"ladder":"door";
                tiles[c]=PixelArt.Draw(transform,"Tile "+c,art,new Vector2(x,y),Vector2.one,t==Tile.Ladder?-1:2,Color.white);
            }
            exit=PixelArt.Draw(transform,"Extraction point","exit",Position(model.Definition.Exit)+new Vector2(0,.1f),new Vector2(1,1.25f),1,Color.white);
            foreach(Cell c in model.Definition.Loot)loot[c]=PixelArt.Draw(transform,"Diamond "+c,"loot",Position(c),new Vector2(.7f,.7f),4,Color.white);
            if(model.Definition.HasSwitch)PixelArt.Draw(transform,"Security terminal","switch",Position(model.Definition.Switch),Vector2.one,2,Color.white);
        }
        public static Vector2 Position(Cell c){return new Vector2(c.X,c.Y);}
        public void RefreshTile(Cell c){SpriteRenderer r;if(tiles.TryGetValue(c,out r))r.enabled=world.At(c)!=Tile.Empty;}
        public void OpenDoors(){foreach(var pair in tiles)if(world.At(pair.Key)==Tile.Empty)pair.Value.enabled=false;}
        public void Collect(Cell c){SpriteRenderer r;if(loot.TryGetValue(c,out r)){r.gameObject.SetActive(false);loot.Remove(c);}}
        public void ExitReady(bool ready){if(exit!=null)exit.color=ready?Color.white:new Color(.5f,.6f,.65f,1);}
        private void Update()
        {
            foreach(var p in loot)p.Value.transform.localPosition=new Vector3(p.Key.X,p.Key.Y+Mathf.Sin(Time.time*3+p.Key.X)*.09f,0);
        }
    }
}
