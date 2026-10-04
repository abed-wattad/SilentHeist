using UnityEngine;
using UnityEngine.EventSystems;
namespace SilentHeist
{
    public sealed class TouchHoldButton : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler
    {
        public InputRouter Input;
        public Vector2Int Direction;
        private bool held;
        public void OnPointerDown(PointerEventData data){held=true;Input.SetMove(Direction);}
        public void OnPointerUp(PointerEventData data){Release();}
        public void OnPointerExit(PointerEventData data){Release();}
        private void Release(){if(held&&Input!=null)Input.SetMove(Vector2Int.zero);held=false;}
        private void OnDisable(){Release();}
    }
}
