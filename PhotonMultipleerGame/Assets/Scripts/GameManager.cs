using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using Photon.Realtime;
using System;
using System.Linq;
using ExitGames.Client.Photon;

public class GameManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private MapController _mapControler;

    void Start()
    {
        Vector3Int pos = new Vector3Int(UnityEngine.Random.Range(2, 18), UnityEngine.Random.Range(2, 8), 0);
        PhotonNetwork.Instantiate(_playerPrefab.name, pos, Quaternion.identity);
        PhotonPeer.RegisterType(typeof(Vector2Int), 242, SerializeVector3Int, DeserializeVector3Int);
        PhotonPeer.RegisterType(typeof(SyncData), 243, SyncData.Serialize, SyncData.Deserialize);
    }

    public void Leave()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        //????? ??????? ????? (??) ???????? ???????
        SceneManager.LoadScene(0);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if(PhotonNetwork.IsMasterClient)
        {
            _mapControler.SendSyncData(newPlayer);
        }
        Debug.LogFormat("Player {0} entered room", newPlayer.NickName);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        PlayerControls player = _mapControler.Players.First(p => p.PhotonView.CreatorActorNr == otherPlayer.ActorNumber);

        if (player != null) player.Kill();

        Debug.LogFormat("Player {0} left room", otherPlayer.NickName);
    }

    public static object DeserializeVector3Int(byte[] data) => new Vector2Int
    {
        x = BitConverter.ToInt32(data, 0),
        y = BitConverter.ToInt32(data, 4)
    };

    public static byte[] SerializeVector3Int(object obj)
    {
        Vector2Int vector = (Vector2Int)obj;
        byte[] result = new byte[8];

        BitConverter.GetBytes(vector.x).CopyTo(result, 0);
        BitConverter.GetBytes(vector.y).CopyTo(result, 4);

        return result;
    }
}
