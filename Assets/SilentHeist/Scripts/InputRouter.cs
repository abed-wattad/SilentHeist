using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace SilentHeist
{
    public sealed class InputRouter : MonoBehaviour
    {
        public Vector2Int Move {get;private set;}
        public bool DigLeft {get;private set;}
        public bool DigRight {get;private set;}
        public bool Interact {get;private set;}
        public bool Pause {get;private set;}
        private Vector2Int touchMove;
        private bool touchLeft,touchRight,touchInteract;
        public void SetMove(Vector2Int value){touchMove=value;}
        public void Dig(int direction){if(direction<0)touchLeft=true;else touchRight=true;}
        public void Use(){touchInteract=true;}
        public void Clear(){touchMove=Vector2Int.zero;touchLeft=touchRight=touchInteract=false;Move=Vector2Int.zero;}
        private void Update()
        {
            Move=touchMove;DigLeft=touchLeft;DigRight=touchRight;Interact=touchInteract;Pause=false;
            touchLeft=touchRight=touchInteract=false;
#if ENABLE_INPUT_SYSTEM
            Keyboard k=Keyboard.current;
            if(k!=null)
            {
                int x=(k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0);
                int y=(k.wKey.isPressed||k.upArrowKey.isPressed?1:0)-(k.sKey.isPressed||k.downArrowKey.isPressed?1:0);
                if(x!=0||y!=0)Move=new Vector2Int(x,y);
                DigLeft|=k.qKey.wasPressedThisFrame;DigRight|=k.eKey.wasPressedThisFrame;
                Interact|=k.fKey.wasPressedThisFrame||k.spaceKey.wasPressedThisFrame;Pause=k.escapeKey.wasPressedThisFrame;
            }
#else
            int x=(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0);
            int y=(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0);
            if(x!=0||y!=0)Move=new Vector2Int(x,y);
            DigLeft|=Input.GetKeyDown(KeyCode.Q);DigRight|=Input.GetKeyDown(KeyCode.E);
            Interact|=Input.GetKeyDown(KeyCode.F)||Input.GetKeyDown(KeyCode.Space);Pause=Input.GetKeyDown(KeyCode.Escape);
#endif
        }
        private void OnApplicationFocus(bool focus){if(!focus)Clear();}
    }
}
