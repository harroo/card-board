
using UnityEngine;
using UnityEngine.UI;

public class ClientUIMethods : MonoBehaviour {

    public Text connectionStatus;

    public void Connect (InputField field) {

        if (field.text == "") return;

        MainClient.ConnectToServer(field.text, 2486);

        if (MainClient.connected) connectionStatus.text = "<color=green>Connected!</color>";
    }

    public void Disconnect () {

        MainClient.DisconnectFromServer();

        if (!MainClient.connected) connectionStatus.text = "<color=green>Not connected.</color>";
    }
}
