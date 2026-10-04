using System.Collections.Generic;
using UnityEngine;
namespace SilentHeist
{
    // Original procedural pixel assets. No downloads or third-party art required.
    public static class PixelArt
    {
        public static readonly Color Ink = new Color32(12,19,34,255);
        public static readonly Color Cyan = new Color32(78,229,219,255);
        public static readonly Color Gold = new Color32(248,193,100,255);
        private static readonly Dictionary<string,Sprite> cache = new Dictionary<string,Sprite>();
        public static Sprite Get(string key)
        {
            Sprite s; if (cache.TryGetValue(key,out s) && s != null) return s;
            const int n = 32; var t = new Texture2D(n,n,TextureFormat.RGBA32,false); t.filterMode=FilterMode.Point;
            Color[] pixels = new Color[n*n];
            System.Action<int,int,int,int,Color> box = (x,y,w,h,c) => { for(int j=y;j<y+h;j++) for(int i=x;i<x+w;i++) if(i>=0&&j>=0&&i<n&&j<n) pixels[j*n+i]=c; };
            Color dark = new Color32(26,39,57,255), stone=new Color32(60,75,92,255), light=new Color32(91,107,121,255);
            if(key=="brick" || key=="wall")
            {
                box(0,0,32,32,dark); box(1,1,30,14,stone); box(1,17,14,14,stone); box(17,17,14,14,stone);
                box(1,29,30,2,light); box(2,13,28,1,light);
                if(key=="brick") { box(3,24,3,2,Gold*.65f); box(22,6,6,1,Gold*.6f); }
                else { box(3,3,26,26,dark); box(5,5,22,22,stone); box(5,25,22,2,light); box(8,8,2,2,light); box(23,8,2,2,light); }
            }
            else if(key=="ladder")
            { box(7,0,3,32,Gold*.7f); box(22,0,3,32,Gold*.7f); for(int y=3;y<32;y+=8) {box(8,y,16,3,Gold);box(8,y-1,16,1,dark);} }
            else if(key=="loot")
            { for(int y=5;y<27;y++) { int half= y<16? (y-4)/2 : (27-y); box(16-half,y,half*2+1,1,Cyan); } box(13,15,3,9,Color.white); box(16,15,9,2,new Color32(26,133,160,255)); }
            else if(key=="door")
            { box(1,0,30,32,dark); box(3,0,26,31,stone); for(int x=7;x<29;x+=7)box(x,1,2,28,light); box(21,13,6,7,Gold); box(23,15,2,3,Ink); }
            else if(key=="exit")
            { box(2,0,28,32,dark); box(4,0,24,30,new Color32(20,74,76,255)); box(6,0,20,28,Ink); box(7,26,18,2,Cyan); box(24,12,2,3,Gold); box(12,17,8,3,Cyan); box(17,14,3,9,Cyan); }
            else if(key=="switch")
            { box(5,3,22,25,dark);box(7,5,18,21,stone);box(9,15,14,9,Cyan);box(10,8,5,4,Gold);box(19,8,4,4,light); }
            else if(key=="camera")
            { box(13,23,5,9,stone);box(11,20,9,5,light);box(4,11,22,12,stone);box(2,13,6,8,Cyan);box(8,20,18,3,light);box(23,15,6,5,dark); }
            else if(key=="player" || key=="guard")
            {
                bool g=key=="guard"; Color coat=g?new Color32(161,86,77,255):new Color32(42,128,146,255);
                box(10,1,5,9,Ink);box(18,1,5,9,Ink);box(8,0,8,3,dark);box(18,0,8,3,dark);
                box(9,9,15,12,coat);box(6,10,4,10,coat);box(24,10,3,10,coat);box(7,8,4,4,new Color32(211,165,128,255));
                box(10,21,14,10,Ink);box(12,22,12,6,new Color32(222,182,140,255));box(10,26,15,3,Ink);box(19,24,3,2,Color.white);
                box(9,17,15,3,g?Gold:Cyan);box(10,19,5,3,g?coat:Cyan);box(22,12,2,4,Gold);
                if(g) {box(8,29,18,3,stone);box(20,27,8,2,stone);box(14,30,4,2,Gold);} else {box(8,21,4,8,Ink);box(5,19,4,3,Cyan);}
            }
            else if(key=="spark") {box(13,8,6,16,Color.white);box(8,13,16,6,Color.white);}
            else box(0,0,32,32,Color.white);
            t.SetPixels(pixels); t.Apply(); t.name="Original_"+key;
            s=Sprite.Create(t,new Rect(0,0,n,n),new Vector2(.5f,.5f),32); s.name=key; cache[key]=s; return s;
        }
        public static SpriteRenderer Draw(Transform parent,string name,string art,Vector2 pos,Vector2 scale,int order,Color tint)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=new Vector3(pos.x,pos.y,0);go.transform.localScale=new Vector3(scale.x,scale.y,1);
            var r=go.AddComponent<SpriteRenderer>();r.sprite=Get(art);r.sortingOrder=order;r.color=tint;return r;
        }
    }
}
