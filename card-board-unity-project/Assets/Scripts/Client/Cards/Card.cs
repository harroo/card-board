
using System;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour {

    public InputField title, field;

    public void Config (byte[] buf) {

        transform.position = new Vector3(
            BitConverter.ToSingle(buf, 0),
            BitConverter.ToSingle(buf, 4),
            BitConverter.ToSingle(buf, 8)
        );

        int titleSize = BitConverter.ToInt32(buf, 12);
        int fieldSize = BitConverter.ToInt32(buf, 16) + titleSize;

        byte[] titleBuf = new byte[titleSize];
        Buffer.BlockCopy(buf, 20, titleBuf, 0, titleSize);

        byte[] fieldBuf = new byte[fieldSize];
        Buffer.BlockCopy(buf, 20 + titleSize, fieldBuf, 0, fieldSize);

        title.text = Encoding.ASCII.GetString(titleBuf);
        field.text = Encoding.ASCII.GetString(fieldBuf);
    }

    public byte[] GetData () {

        byte[] titleData = Encoding.ASCII.GetBytes(title.text);
        byte[] fieldData = Encoding.ASCII.GetBytes(field.text);

        byte[] data = new byte[titleData.Length + fieldData.Length + 20];

        Buffer.BlockCopy(BitConverter.GetBytes(transform.position.x), 0, data, 0, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(transform.position.y), 0, data, 4, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(transform.position.z), 0, data, 8, 4);

        Buffer.BlockCopy(BitConverter.GetBytes(titleData.Length), 0, data, 12, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(fieldData.Length), 0, data, 16, 4);

        Buffer.BlockCopy(titleData, 0, data, 20, titleData.Length);
        Buffer.BlockCopy(fieldData, 0, data, 20 + titleData.Length, fieldData.Length);

        return data;
    }
}
