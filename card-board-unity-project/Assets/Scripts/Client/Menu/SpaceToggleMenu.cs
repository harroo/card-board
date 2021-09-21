
using UnityEngine;

public class SpaceToggleMenu : MonoBehaviour {

    public GameObject go;

    private void Update () {

        if (Input.GetKeyUp(KeyCode.Space)) {

            go.SetActive(!go.activeSelf);
        }
    }
}
