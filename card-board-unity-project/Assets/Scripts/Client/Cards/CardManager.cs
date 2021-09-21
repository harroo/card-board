
using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public class CardManager : MonoBehaviour {

    public static CardManager instance;
    private void Awake () { instance = this; }

    public GameObject prefab;

    public void LoadData (byte[] data) {

        int index = 0;

        while (index < data.Length) {

            int size = BitConverter.ToInt32(data, index);
            index += 4;

            byte[] buf = new byte[size];
            Buffer.BlockCopy(data, index, buf, 0, size);
            index += size;
        }
    }

    private List<Card> cards = new List<Card>();

    public void LoadCard (byte[] buf) {

        Card card = Instantiate(prefab, Vector3.zero, Quaternion.identity).GetComponent<Card>();
        card.Config(buf);
        cards.Add(card);
    }
}
