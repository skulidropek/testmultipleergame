using System;
using System.Collections;
using UnityEngine;

public class SyncData
{
    public Vector2Int[] Positions;
    public int[] Scores;

    public BitArray MapData;

    public static object Deserialize(byte[] bytes)
    {
        SyncData data = new SyncData();

        int players = (bytes.Length - 20 * 10 / 8) / 12;

        data.Positions = new Vector2Int[players];
        data.Scores = new int[players];

        for(int i = 0; i < players; i++)
        {
            data.Positions[i].x = BitConverter.ToInt32(bytes, 8 * i);
            data.Positions[i].y = BitConverter.ToInt32(bytes, 8 * i + 4);
            data.Scores[i] = BitConverter.ToInt32(bytes, 8 * players + 4 * i);
        }

        byte[] mapBytes = new byte[20 * 10 / 8];
        Array.Copy(bytes, players * 12, mapBytes, 0, mapBytes.Length);
        data.MapData = new BitArray(mapBytes);


        return data;
    }
    public static byte[] Serialize(object obj)
    {
        SyncData data = (SyncData)obj;

        byte[] result = new byte[
            8 * data.Positions.Length + 
            4 * data.Scores.Length +
            Mathf.CeilToInt(data.MapData.Count / 8f)
            ];

        for(int i = 0; i < data.Positions.Length; i++)
        {
            BitConverter.GetBytes(data.Positions[i].x).CopyTo(result, 8 * i);
            BitConverter.GetBytes(data.Positions[i].y).CopyTo(result, 8 * i + 4);
        }

        for(int i = 0; i < data.Scores.Length; i++)
            BitConverter.GetBytes(data.Scores[i]).CopyTo(result, 8 * data.Positions.Length + 4 * i);

        data.MapData.CopyTo(result, 8 * data.Positions.Length + 4 * data.Scores.Length);

        return result;
    }

}
