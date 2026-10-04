using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
namespace SilentHeist
{
    public sealed class GameUI : MonoBehaviour
    {
        private GameManager game;
        private RectTransform root,overlay,panel,touchRoot;
        private Text level,loot,time,notice,alert,controls;
        private Image detectionFill;
        private Font font;
        private bool showTouch;
        private Rect lastSafe;
        private Vector2Int lastSize;
        private readonly Color panelColor=new Color32(19,31,49,255);
        public void Initialize(GameManager manager)
        {
            game=manager;showTouch=Application.isMobilePlatform;
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject=new GameObject("Silent Heist / UI");canvasObject.transform.SetParent(transform,false);
            var canvas=canvasObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
            var scaler=canvasObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            canvasObject.AddComponent<GraphicRaycaster>();root=Rect("Safe Area",canvasObject.transform);Stretch(root);
            if(EventSystem.current==null)
            {
                var events=new GameObject("EventSystem");events.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                events.AddComponent<InputSystemUIInputModule>();
#else
                events.AddComponent<StandaloneInputModule>();
#endif
            }
            RectTransform top=Box("HUD",root,new Color32(12,19,34,247));Anchor(top,0,1,1,1,0,-62,0,0);
            level=Label(top,"SILENT HEIST",19,PixelArt.Cyan,TextAnchor.MiddleLeft);Place(level.rectTransform,20,-8,340,46,0,1);
            loot=Label(top,"DIAMONDS 0/3",18,Color.white,TextAnchor.MiddleLeft);Place(loot.rectTransform,380,-8,210,46,0,1);
            time=Label(top,"00:00",19,PixelArt.Gold,TextAnchor.MiddleCenter);Place(time.rectTransform,-260,-8,100,46,1,1);
            Button(top,"SOUND",-150,-13,68,36,()=>{game.Audio.ToggleMute();game.Message(game.Audio.Muted?"Sound muted":"Sound on");},1,1);
            Button(top,"PAUSE",-72,-13,62,36,()=>game.TogglePause(),1,1);
            alert=Label(root,"CLEAR",13,PixelArt.Gold,TextAnchor.MiddleRight);Place(alert.rectTransform,-265,-69,110,20,1,1);
            var track=Box("Detection track",root,new Color32(34,49,66,255));Place(track,-145,-75,125,8,1,1);
            detectionFill=Box("Exposure",track,new Color32(255,94,84,255)).GetComponent<Image>();Anchor(detectionFill.rectTransform,0,0,0,1,0,0,0,0);
            notice=Label(root,"",17,PixelArt.Gold,TextAnchor.MiddleCenter);Anchor(notice.rectTransform,0,0,1,0,180,80,-180,115);
            controls=Label(root,"MOVE  WASD / ARROWS     DIG  Q / E     USE  F     PAUSE  ESC",15,new Color32(150,174,191,255),TextAnchor.MiddleCenter);Anchor(controls.rectTransform,0,0,1,0,100,20,-100,55);
            Button(top,"TOUCH",620,-13,75,36,()=>{showTouch=!showTouch;UpdateControls();},0,1);
            BuildTouch();
            overlay=Box("Menu scrim",root,new Color(0.015f,.035f,.065f,.87f));Stretch(overlay);overlay.GetComponent<Image>().raycastTarget=true;
            panel=Box("Menu card",overlay,panelColor);Place(panel,0,0,650,490,.5f,.5f);panel.pivot=new Vector2(.5f,.5f);panel.anchoredPosition=Vector2.zero;
            var stroke=Box("Accent",panel,PixelArt.Cyan);Anchor(stroke,0,1,1,1,0,-4,0,0);
            game.Changed+=Refresh;game.StateChanged+=ShowState;UpdateControls();UpdateSafeArea();Refresh();
        }
        private void BuildTouch()
        {
            touchRoot=Rect("Touch controls",root);Stretch(touchRoot);
            MoveButton("<",25,30,new Vector2Int(-1,0));MoveButton(">",175,30,new Vector2Int(1,0));
            MoveButton("UP",100,82,new Vector2Int(0,1));MoveButton("DN",100,5,new Vector2Int(0,-1));
            Button(touchRoot,"DIG L",-260,-35,75,56,()=>game.Input.Dig(-1),1,0);
            Button(touchRoot,"DIG R",-178,-35,75,56,()=>game.Input.Dig(1),1,0);
            Button(touchRoot,"USE",-96,-35,78,56,()=>game.Input.Use(),1,0);
        }
        private void MoveButton(string text,float x,float y,Vector2Int dir)
        {
            var b=Button(touchRoot,text,x,-y,68,54,()=>{},0,0);var hold=b.gameObject.AddComponent<TouchHoldButton>();hold.Input=game.Input;hold.Direction=dir;
        }
        private void UpdateControls(){if(touchRoot!=null)touchRoot.gameObject.SetActive(showTouch&&game.State==GameState.Playing);if(controls!=null)controls.enabled=!showTouch;}
        private void Refresh()
        {
            level.text=game.World.Definition.Title;loot.text="DIAMONDS  "+game.Collected+" / "+game.World.Definition.Loot.Length;
            time.text=FormatTime(game.Elapsed);notice.text=game.Notice;
            detectionFill.rectTransform.anchorMax=new Vector2(game.Detection,1);detectionFill.rectTransform.offsetMax=Vector2.zero;
            alert.text=game.SecurityDisabled?"OFFLINE":game.Detection>0?"DETECTED "+Mathf.RoundToInt(game.Detection*100)+"%":"CLEAR";
        }
        private void ShowState(GameState state)
        {
            UpdateControls();overlay.gameObject.SetActive(state!=GameState.Playing);
            // Keep the accent, replace only the contents of the current modal.
            for(int i=panel.childCount-1;i>=1;i--){panel.GetChild(i).gameObject.SetActive(false);Destroy(panel.GetChild(i).gameObject);}
            if(state==GameState.Playing)return;
            string eyebrow="A SMALL JOB. A CLEAN GETAWAY.",title="SILENT HEIST",body="A stealth puzzle inspired by Lode Runner + Bob the Robber.\nCollect the diamonds. Outsmart security. Get back out.";
            if(state==GameState.Briefing){eyebrow="MISSION BRIEFING";title=game.World.Definition.Title;body=game.World.Definition.Briefing;}
            if(state==GameState.Paused){eyebrow="TAKE YOUR TIME";title="JOB ON HOLD";body="The building is frozen. Your route can wait.";}
            if(state==GameState.Caught){eyebrow="NO LOOT LEFT BEHIND";title="CAUGHT";body=game.Notice;}
            if(state==GameState.Complete){eyebrow=game.Alerts==0?"SILENT RUN / NO ALERTS":"MISSION ACCOMPLISHED";title=game.LevelIndex==2?"THE PERFECT EXIT":"HEIST COMPLETE";body="All diamonds recovered.\nTime  "+FormatTime(game.Elapsed)+"      Best  "+FormatTime(SaveSystem.Best(game.LevelIndex))+"      Alerts  "+game.Alerts;}
            Text e=Label(panel,eyebrow,13,PixelArt.Cyan,TextAnchor.MiddleCenter);Place(e.rectTransform,30,-30,590,30,0,1);
            Text h=Label(panel,title,state==GameState.Briefing?31:46,state==GameState.Caught?new Color32(255,111,99,255):Color.white,TextAnchor.MiddleCenter);Place(h.rectTransform,25,-73,600,64,0,1);
            Text b=Label(panel,body,18,new Color32(172,190,205,255),TextAnchor.MiddleCenter);Place(b.rectTransform,35,-148,580,83,0,1);
            if(state==GameState.Menu)
            {
                for(int i=0;i<3;i++)
                {
                    int index=i;bool unlocked=i<SaveSystem.Unlocked;
                    string caption=(i+1)+"  "+(i==0?"TRAINING":i==1?"SECURITY":"MASTER VAULT");
                    if(!unlocked)caption+=" / LOCKED";else if(SaveSystem.Best(i)>0)caption+="  "+FormatTime(SaveSystem.Best(i));
                    var button=Button(panel,caption,70,-255-i*58,510,46,()=>game.SelectLevel(index),0,1);button.interactable=unlocked;
                }
                Text footer=Label(panel,"3 handcrafted levels   /   Original art & sound   /   Solo heist",13,new Color32(105,135,155,255),TextAnchor.MiddleCenter);Place(footer.rectTransform,20,-447,610,22,0,1);
            }
            else if(state==GameState.Briefing)
            {
                Text hint=Label(panel,showTouch?"Use the direction pad, DIG L / DIG R and USE buttons.\nNo jump. Climb ladders to change floors.":"WASD / arrows: move + climb     Q / E: dig     F: use terminal\nNo jumping. Diamonds collect on contact.",15,PixelArt.Gold,TextAnchor.MiddleCenter);Place(hint.rectTransform,30,-250,590,60,0,1);
                Button(panel,"BEGIN HEIST",100,-330,450,54,()=>game.Begin(),0,1);Button(panel,"BACK",225,-400,200,38,()=>game.Menu(),0,1);
            }
            else
            {
                string primary=state==GameState.Paused?"RESUME":state==GameState.Caught?"TRY AGAIN":game.LevelIndex==2?"BACK TO MISSIONS":"NEXT HEIST";
                Button(panel,primary,100,-275,450,55,()=>{if(state==GameState.Paused)game.TogglePause();else if(state==GameState.Caught)game.Restart();else game.Next();},0,1);
                Button(panel,state==GameState.Caught?"MISSIONS":"RESTART",100,-350,215,44,()=>{if(state==GameState.Caught)game.Menu();else game.Restart();},0,1);
                Button(panel,"MAIN MENU",335,-350,215,44,()=>game.Menu(),0,1);
            }
        }
        private void Update(){UpdateSafeArea();}
        private void UpdateSafeArea()
        {
            Rect safe=Screen.safeArea;Vector2Int size=new Vector2Int(Screen.width,Screen.height);
            if(safe==lastSafe&&size==lastSize)return;lastSafe=safe;lastSize=size;
            root.anchorMin=new Vector2(safe.xMin/Mathf.Max(1,Screen.width),safe.yMin/Mathf.Max(1,Screen.height));
            root.anchorMax=new Vector2(safe.xMax/Mathf.Max(1,Screen.width),safe.yMax/Mathf.Max(1,Screen.height));root.offsetMin=root.offsetMax=Vector2.zero;
        }
        public static string FormatTime(float seconds){int total=Mathf.FloorToInt(seconds);return (total/60).ToString("00")+":"+(total%60).ToString("00");}
        private RectTransform Rect(string name,Transform parent){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return (RectTransform)go.transform;}
        private RectTransform Box(string name,Transform parent,Color c){var rt=Rect(name,parent);var img=rt.gameObject.AddComponent<Image>();img.color=c;img.raycastTarget=false;return rt;}
        private Text Label(Transform parent,string value,int size,Color c,TextAnchor alignment)
        {var r=Rect("Text",parent);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.color=c;t.alignment=alignment;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;return t;}
        private Button Button(Transform parent,string caption,float x,float y,float w,float h,Action action,float ax,float ay)
        {
            var rt=Box(caption,parent,new Color32(34,62,78,255));Place(rt,x,y,w,h,ax,ay);rt.GetComponent<Image>().raycastTarget=true;
            var button=rt.gameObject.AddComponent<Button>();button.targetGraphic=rt.GetComponent<Image>();
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.25f,1.3f,1.3f);colors.pressedColor=PixelArt.Cyan;colors.disabledColor=new Color(.4f,.4f,.4f,.7f);button.colors=colors;
            button.onClick.AddListener(()=>action());var t=Label(rt,caption,16,Color.white,TextAnchor.MiddleCenter);Stretch(t.rectTransform);return button;
        }
        private void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        // x/y are offsets from a chosen anchor; positive width/height extend right/down.
        private void Place(RectTransform r,float x,float y,float w,float h,float ax,float ay)
        {r.anchorMin=r.anchorMax=new Vector2(ax,ay);r.pivot=new Vector2(0,1);r.sizeDelta=new Vector2(w,h);r.anchoredPosition=new Vector2(x,ay==0?-y+h:y);}
        private void Anchor(RectTransform r,float x0,float y0,float x1,float y1,float left,float bottom,float right,float top)
        {r.anchorMin=new Vector2(x0,y0);r.anchorMax=new Vector2(x1,y1);r.offsetMin=new Vector2(left,bottom);r.offsetMax=new Vector2(right,top);}
        private void OnDestroy(){if(game!=null){game.Changed-=Refresh;game.StateChanged-=ShowState;}}
    }
}
