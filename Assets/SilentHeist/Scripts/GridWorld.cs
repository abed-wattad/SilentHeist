using System;
using System.Collections.Generic;

namespace SilentHeist
{
    // Pure C#: shared by movement, guard routing and the standalone rule tests.
    public struct Cell : IEquatable<Cell>
    {
        public int X, Y;
        public Cell(int x, int y) { X = x; Y = y; }
        public bool Equals(Cell other) { return X == other.X && Y == other.Y; }
        public override bool Equals(object obj) { return obj is Cell && Equals((Cell)obj); }
        public override int GetHashCode() { return X * 397 ^ Y; }
        public static bool operator ==(Cell a, Cell b) { return a.Equals(b); }
        public static bool operator !=(Cell a, Cell b) { return !a.Equals(b); }
        public static Cell operator +(Cell a, Cell b) { return new Cell(a.X + b.X, a.Y + b.Y); }
        public override string ToString() { return X + "," + Y; }
    }
    public enum Tile { Empty, Wall, Brick, Ladder, Door }
    public sealed class LevelDefinition
    {
        public string Title, Briefing;
        public Tile[,] Tiles;
        public Cell Start, Exit, Switch, Door;
        public Cell[] Loot, Guards, Cameras;
        public bool HasSwitch;
        public int Width { get { return Tiles.GetLength(0); } }
        public int Height { get { return Tiles.GetLength(1); } }
        public static LevelDefinition Create(int index)
        {
            var d = new LevelDefinition { Tiles = new Tile[27, 17], Start = new Cell(2, 1), Exit = new Cell(2, 1) };
            for (int x = 0; x < 27; x++) for (int y = 0; y < 17; y++)
            {
                if (x == 0 || x == 26 || y == 0 || y == 16) d.Tiles[x,y] = Tile.Wall;
                else if (y % 4 == 0) d.Tiles[x,y] = Tile.Brick;
            }
            int[] ladders = index == 0 ? new[] { 5, 21 } : index == 1 ? new[] { 4, 13, 23 } : new[] { 3, 12, 22 };
            foreach (int x in ladders) for (int y = 1; y <= 13; y++) d.Tiles[x,y] = Tile.Ladder;
            // Permanent pillars create occlusion and route choices, without sealing floors.
            if (index > 0) { d.Tiles[17,5] = Tile.Wall; d.Tiles[17,6] = Tile.Wall; d.Tiles[9,9] = Tile.Wall; d.Tiles[9,10] = Tile.Wall; }
            if (index == 0)
            {
                d.Title = "01 / TRAINING VAULT";
                d.Briefing = "Collect all 3 diamonds, then return to the green exit.\nClimb the gold ladders. Dig a floor tile to trap the guard.";
                d.Loot = new[] { new Cell(23,1), new Cell(8,5), new Cell(16,13) };
                d.Guards = new[] { new Cell(17,9) }; d.Cameras = new Cell[0];
            }
            else if (index == 1)
            {
                d.Title = "02 / SECURITY WING";
                d.Briefing = "Cameras raise the detection meter. Walls block sight.\nUse the cyan switch [F] to disable cameras and unlock the door.";
                d.Loot = new[] { new Cell(24,1), new Cell(7,5), new Cell(20,9), new Cell(24,13) };
                d.Guards = new[] { new Cell(19,5), new Cell(8,13) };
                d.Cameras = new[] { new Cell(18,7), new Cell(18,15) };
                d.HasSwitch = true; d.Switch = new Cell(6,9); d.Door = new Cell(21,13);
            }
            else
            {
                d.Title = "03 / MASTER VAULT";
                d.Briefing = "The final job. Five diamonds, three guards.\nReach the switch, open the vault, and bring everything home.";
                d.Loot = new[] { new Cell(24,1), new Cell(7,5), new Cell(20,5), new Cell(15,9), new Cell(24,13) };
                d.Guards = new[] { new Cell(19,1), new Cell(7,9), new Cell(17,13) };
                d.Cameras = new[] { new Cell(20,7), new Cell(18,11), new Cell(18,15) };
                d.HasSwitch = true; d.Switch = new Cell(5,13); d.Door = new Cell(21,13);
            }
            // Seal the upper-right vault behind a full-height door partition.
            if (d.HasSwitch) for (int y = 13; y < 16; y++) d.Tiles[d.Door.X,y] = Tile.Door;
            // The vault's right ladder must not bypass its door; terminate below top floor.
            if (d.HasSwitch) for (int y = 12; y <= 13; y++) d.Tiles[ladders[ladders.Length-1],y] = y == 12 ? Tile.Brick : Tile.Empty;
            return d;
        }
    }
    public sealed class GridWorld
    {
        public readonly LevelDefinition Definition;
        public readonly Tile[,] Tiles;
        public readonly HashSet<Cell> Holes = new HashSet<Cell>();
        public GridWorld(LevelDefinition definition) { Definition = definition; Tiles = (Tile[,])definition.Tiles.Clone(); }
        public bool Inside(Cell c) { return c.X >= 0 && c.Y >= 0 && c.X < Definition.Width && c.Y < Definition.Height; }
        public Tile At(Cell c) { return Inside(c) ? Tiles[c.X,c.Y] : Tile.Wall; }
        public bool Solid(Cell c) { Tile t = At(c); return t == Tile.Wall || t == Tile.Brick || t == Tile.Door; }
        public bool Ladder(Cell c) { return At(c) == Tile.Ladder; }
        public bool Falling(Cell c) { return !Ladder(c) && !Solid(c + new Cell(0,-1)); }
        public bool CanMove(Cell c, Cell delta)
        {
            Cell n = c + delta;
            if (Math.Abs(delta.X) + Math.Abs(delta.Y) != 1 || Solid(n)) return false;
            if (Falling(c)) return delta.X == 0 && delta.Y == -1;
            if (delta.Y > 0) return Ladder(c) || Ladder(n);
            if (delta.Y < 0) return Ladder(c) || Ladder(n);
            return true;
        }
        public bool CanDig(Cell c, int direction)
        {
            if (direction != -1 && direction != 1) return false;
            Cell target = c + new Cell(direction,-1);
            return !Falling(c) && !Solid(c + new Cell(direction,0)) && At(target) == Tile.Brick;
        }
        public bool Dig(Cell c, int direction)
        {
            if (!CanDig(c,direction)) return false;
            Cell t = c + new Cell(direction,-1); Tiles[t.X,t.Y] = Tile.Empty; Holes.Add(t); return true;
        }
        public void Restore(Cell c) { if (Holes.Remove(c)) Tiles[c.X,c.Y] = Tile.Brick; }
        public void OpenDoors()
        {
            for (int x = 0; x < Definition.Width; x++) for (int y = 0; y < Definition.Height; y++)
                if (Tiles[x,y] == Tile.Door) Tiles[x,y] = Tile.Empty;
        }
        public bool ClearSight(Cell from, Cell to)
        {
            int steps = Math.Max(Math.Abs(to.X-from.X),Math.Abs(to.Y-from.Y)) * 4;
            for (int i = 1; i < steps; i++)
            {
                float f = (float)i / steps;
                Cell c = new Cell((int)Math.Round(from.X+(to.X-from.X)*f),(int)Math.Round(from.Y+(to.Y-from.Y)*f));
                if (Solid(c)) return false;
            }
            return true;
        }
        public Cell NextStep(Cell from, Cell target)
        {
            var queue = new Queue<Cell>(); var previous = new Dictionary<Cell,Cell>();
            queue.Enqueue(from); previous[from] = from;
            Cell[] moves = { new Cell(-1,0), new Cell(1,0), new Cell(0,1), new Cell(0,-1) };
            while (queue.Count > 0)
            {
                Cell c = queue.Dequeue(); if (c == target) break;
                foreach (Cell m in moves) { Cell n = c+m; if (CanMove(c,m) && !previous.ContainsKey(n)) { previous[n]=c; queue.Enqueue(n); } }
            }
            if (!previous.ContainsKey(target)) return from;
            Cell result = target;
            while (result != from && previous[result] != from) result = previous[result];
            return result;
        }
    }
}
