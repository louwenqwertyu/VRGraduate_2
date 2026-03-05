using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CanvasPosControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public float dragSpeed = 0.5f;

    public Transform canvas;

    private bool canDrag = false;
    private Vector2? _mouseButtonDownPoint = null;
    private Vector2 _mouseButtonUpPoint;

    private Vector3 _buttonDownLocalEulerAngles;

    private void Update()
    {
        if (!canDrag) return;

        if (!canvas) return;

        MouseButtonUp();
        MouseButtonDown();
        MouseButtonMove();
    }

    private void MouseButtonDown()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _mouseButtonDownPoint = Input.mousePosition;
            _buttonDownLocalEulerAngles = canvas.localEulerAngles;
        }
    }

    private void MouseButtonUp()
    {
        if (Input.GetMouseButtonUp(0))
        {
            _mouseButtonDownPoint = null;
        }
    }

    private void MouseButtonMove()
    {
        if (_mouseButtonDownPoint == null) return;

        if (!Input.GetMouseButton(0)) return;

        //控制物体旋转

        float localEulerAnglesX = ((Vector2)Input.mousePosition - _mouseButtonDownPoint.Value).y;
        localEulerAnglesX *= dragSpeed;

        float newAngleX = localEulerAnglesX + _buttonDownLocalEulerAngles.x;
        if (newAngleX < 0 || newAngleX > 90) return;

        canvas.localEulerAngles = _buttonDownLocalEulerAngles + new Vector3(localEulerAnglesX, 0, 0);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        canDrag = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        canDrag = false;
    }
}
