using System;
using System.Collections.Generic;
using UnityEngine;
namespace SilentHeist
{
    public enum GameState { Menu, Briefing, Playing, Paused, Caught, Complete }
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance {get;private set;}
        public GameConfig Config {get;private set;}
        public GridWorld World {get;private set;}
        public WorldView View {get;private set;}
        public GridActor Player {get;private set;}
        public InputRouter Input {get;private set;}
        public AudioManager Audio {get;private set;}
        public VFXPool Effects {get;private set;}
        public GameState State {get;private set;}
        public bool SecurityDisabled {get;private set;}
        public int LevelIndex {get;private set;}
        public int Collected {get;private set;}
        public int Alerts {get;private set;}
        public float Elapsed {get;private set;}
        public float Detection {get;private set;}
        public string Notice {get;private set;}
        public event Action Changed;
        public event Action<GameState> StateChanged;
        private readonly List<GuardController> guards=new List<GuardController>();
        private readonly List<SecurityCamera> cameras=new List<SecurityCamera>();
        private readonly HashSet<Cell> remaining=new HashSet<Cell>();
        private Transform levelRoot;
        private DigSystem digging;
        private GameUI ui;
        private Camera worldCamera;
        private float noticeTime,hudTime;
        private bool cameraAlert;
        private int bufferedDig;
        private float digBufferTime, useBufferTime;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics(){Instance=null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartGame(){if(Instance==null)new GameObject("Silent Heist / GameManager").AddComponent<GameManager>();}
        private void Awake()
        {
            if(Instance!=null){Destroy(gameObject);return;}Instance=this;
            Application.targetFrameRate=60;Screen.orientation=ScreenOrientation.LandscapeLeft;
            Config=Resources.Load<GameConfig>("GameConfig");if(Config==null)Config=ScriptableObject.CreateInstance<GameConfig>();
            Input=gameObject.AddComponent<InputRouter>();Audio=gameObject.AddComponent<AudioManager>();Effects=gameObject.AddComponent<VFXPool>();
            worldCamera=Camera.main;
            if(worldCamera==null){var c=new GameObject("Main Camera");c.tag="MainCamera";worldCamera=c.AddComponent<Camera>();c.AddComponent<AudioListener>();}
            worldCamera.orthographic=true;worldCamera.clearFlags=CameraClearFlags.SolidColor;worldCamera.backgroundColor=PixelArt.Ink;
            worldCamera.transform.position=new Vector3(13,7f,-20);
            LoadLevel(0);ui=gameObject.AddComponent<GameUI>();ui.Initialize(this);SetState(GameState.Menu);
        }
        private void LoadLevel(int index)
        {
            Time.timeScale=1;Input.Clear();Effects.Clear();
            if(levelRoot!=null){levelRoot.gameObject.SetActive(false);Destroy(levelRoot.gameObject);}
            guards.Clear();cameras.Clear();remaining.Clear();LevelIndex=index;
            Collected=Alerts=0;Elapsed=Detection=0;SecurityDisabled=false;cameraAlert=false;Notice="";
            bufferedDig=0;digBufferTime=useBufferTime=0;
            World=new GridWorld(LevelDefinition.Create(index));
            levelRoot=new GameObject("Level "+(index+1)+" / Runtime Objects").transform;
            View=levelRoot.gameObject.AddComponent<WorldView>();View.Build(World);View.ExitReady(false);
            var playerObject=new GameObject("Player");playerObject.transform.SetParent(levelRoot);Player=playerObject.AddComponent<GridActor>();Player.Initialize(World.Definition.Start,false);
            foreach(Cell c in World.Definition.Loot)remaining.Add(c);
            foreach(Cell c in World.Definition.Guards)
            {var go=new GameObject("Guard");go.transform.SetParent(levelRoot);var guard=go.AddComponent<GuardController>();guard.Initialize(this,c);guards.Add(guard);}
            foreach(Cell c in World.Definition.Cameras)
            {var go=new GameObject("Security Camera");go.transform.SetParent(levelRoot);var camera=go.AddComponent<SecurityCamera>();camera.Initialize(this,c);cameras.Add(camera);}
            digging=levelRoot.gameObject.AddComponent<DigSystem>();digging.Initialize(this);
        }
        public void SelectLevel(int index)
        {if(index<0||index>2||index>=SaveSystem.Unlocked)return;LoadLevel(index);SetState(GameState.Briefing);}
        public void Begin(){SetState(GameState.Playing);Message(LevelIndex==0?"WASD / arrows to move and climb. Q / E dig. Collect diamonds, return to EXIT.":"F / USE activates a nearby security terminal.",7);}
        public void Restart(){LoadLevel(LevelIndex);SetState(GameState.Playing);}
        public void Menu(){LoadLevel(0);SetState(GameState.Menu);}
        public void TogglePause(){if(State==GameState.Playing)SetState(GameState.Paused);else if(State==GameState.Paused)SetState(GameState.Playing);}
        public void Next(){if(LevelIndex<2)SelectLevel(LevelIndex+1);else Menu();}
        private void SetState(GameState value)
        {
            State=value;Input.Clear();Time.timeScale=value==GameState.Playing||value==GameState.Menu||value==GameState.Briefing?1:0;
            if(StateChanged!=null)StateChanged(value);Emit();
        }
        private void LateUpdate()
        {
            float aspect=Screen.width/(float)Mathf.Max(1,Screen.height);
            worldCamera.orthographicSize=Mathf.Max(12.1f,15/aspect);
            if(Input.Pause)TogglePause();
            if(State!=GameState.Playing)return;
            float dt=Time.deltaTime;Elapsed+=dt;noticeTime-=dt;if(noticeTime<=0)Notice="";
            // Brief buffering makes one-tap dig/use input reliable during a tile transition.
            digBufferTime-=dt;useBufferTime-=dt;
            if(Input.DigLeft){bufferedDig=-1;digBufferTime=.3f;}
            if(Input.DigRight){bufferedDig=1;digBufferTime=.3f;}
            if(Input.Interact)useBufferTime=.3f;
            Player.Tick(dt);
            if(!Player.Moving)
            {
                if(remaining.Remove(Player.Cell))
                {
                    Collected++;View.Collect(Player.Cell);Audio.PlayCue("loot");Effects.Burst(Player.transform.position,PixelArt.Cyan);
                    if(remaining.Count==0){View.ExitReady(true);Message("ALL DIAMONDS SECURED. Return to the green exit!",5);}Emit();
                }
                if(Player.Cell==World.Definition.Exit&&remaining.Count==0){Complete();return;}
                if(useBufferTime>0){Interact();useBufferTime=0;}
                if(digBufferTime>0){digging.TryDig(bufferedDig);digBufferTime=0;}
                Cell move=new Cell(0,0);
                if(World.Falling(Player.Cell))move=new Cell(0,-1);
                else
                {
                    Vector2Int input=Input.Move;
                    // Prefer ladder input, then allow horizontal movement if climbing is blocked.
                    if(input.y!=0&&World.CanMove(Player.Cell,new Cell(0,input.y)))move=new Cell(0,input.y);
                    else if(input.x!=0)move=new Cell(input.x,0);
                }
                if(World.CanMove(Player.Cell,move))Player.Begin(Player.Cell+move,World.Falling(Player.Cell)?9:Config.playerSpeed);
            }
            foreach(GuardController guard in guards)
            {
                guard.Tick(dt);
                if(guard.State!=GuardState.Trapped&&Vector2.Distance(Player.transform.position,guard.transform.position)<.57f)
                {Fail("A guard caught you. Dig a trap or take another route.");return;}
            }
            bool exposed=false;foreach(SecurityCamera camera in cameras)exposed|=camera.Tick(Elapsed);
            Detection=Mathf.Clamp01(Detection+(exposed?dt/Config.cameraExposureTime:-dt*Config.detectionDrain));
            if(exposed&&!cameraAlert){RegisterAlert();cameraAlert=true;Audio.PlayCue("alert");}
            if(Detection<=0)cameraAlert=false;
            if(Detection>=1){Fail("Camera exposure reached 100%. Use cover or disable security.");return;}
            if(Player.Cell.Y<1){Fail("You fell out of the building.");return;}
            hudTime-=dt;if(hudTime<=0){hudTime=.1f;Emit();}
        }
        private void Interact()
        {
            Cell c=World.Definition.Switch;
            if(World.Definition.HasSwitch&&Mathf.Abs(Player.Cell.X-c.X)+Mathf.Abs(Player.Cell.Y-c.Y)<=1)
            {
                if(SecurityDisabled){Message("Security is already offline.");return;}
                SecurityDisabled=true;World.OpenDoors();View.OpenDoors();Audio.PlayCue("switch");Effects.Burst(WorldView.Position(c),PixelArt.Cyan);Message("SECURITY OFFLINE. Vault doors open. Cameras disabled.",5);
            }
            else Message("Move next to a cyan terminal to use it. Loot is collected automatically.");
        }
        public bool Occupied(Cell c)
        {if(Player.Occupies(c))return true;foreach(var g in guards)if(g.Actor.Occupies(c))return true;return false;}
        public void RegisterAlert(){Alerts++;Emit();}
        public void Message(string text,float duration=3){Notice=text;noticeTime=duration;Emit();}
        private void Fail(string reason){Notice=reason;Audio.PlayCue("fail");Player.Tint(new Color(1,.4f,.4f));SetState(GameState.Caught);}
        private void Complete(){SaveSystem.Complete(LevelIndex,Elapsed,Alerts);Audio.PlayCue("win");SetState(GameState.Complete);}
        private void Emit(){if(Changed!=null)Changed();}
        private void OnApplicationFocus(bool focused){if(!focused&&State==GameState.Playing)SetState(GameState.Paused);}
        private void OnDestroy(){if(Instance==this){Instance=null;Time.timeScale=1;}}
    }
}
