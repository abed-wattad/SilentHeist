using System.Collections;
using UnityEngine;
namespace SilentHeist
{
    public sealed class DigSystem : MonoBehaviour
    {
        private GameManager game;
        private float nextDig;
        public void Initialize(GameManager manager){game=manager;}
        public void TryDig(int direction)
        {
            if(game.Player.Moving||Time.time<nextDig)return;
            Cell origin=game.Player.Cell;
            if(!game.World.Dig(origin,direction)){game.Message("Dig beside you into a gold-flecked floor tile.");return;}
            nextDig=Time.time+.3f;Cell hole=origin+new Cell(direction,-1);
            game.View.RefreshTile(hole);game.Effects.Burst(WorldView.Position(hole),PixelArt.Gold);game.Audio.PlayCue("dig");
            StartCoroutine(Rebuild(hole));
        }
        private IEnumerator Rebuild(Cell hole)
        {
            yield return new WaitForSeconds(game.Config.holeLifetime);
            // Never restore a collider into an actor. Wait until both origin and destination clear.
            while(game.Occupied(hole))yield return new WaitForSeconds(.1f);
            game.World.Restore(hole);game.View.RefreshTile(hole);game.Effects.Burst(WorldView.Position(hole),new Color(.55f,.7f,.7f));
        }
    }
}
