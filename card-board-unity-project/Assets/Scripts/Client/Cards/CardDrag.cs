
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardDrag : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler {

    private bool mouseOver;

    public void OnPointerEnter (PointerEventData eventData) {

        mouseOver = true;
    }

    public void OnPointerExit (PointerEventData eventData) {

        mouseOver = false;
    }

    public void OnPointerClick (PointerEventData eventData) { }

    private bool dragging;
    private Vector3 posCache;

    private void Update () {

        if (mouseOver && Input.GetMouseButtonDown(0)) {

            dragging = true;
            posCache = Input.mousePosition;
        }

        if (dragging) {

            transform.position -= posCache - Input.mousePosition;
            posCache = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
            dragging = false;
    }
}
