using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class LobyManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private Text _logText;
    [SerializeField] private InputField _nickNameInput;

    private void Start()
    {
        string nickName = PlayerPrefs.GetString("NickName", $"Player{Random.Range(1000, 9999)}");

        PhotonNetwork.NickName = nickName;

        _nickNameInput.text = nickName;

        Log("Player's name is set to " + nickName);

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = "1";
        PhotonNetwork.ConnectUsingSettings();
    }
    public override void OnConnectedToMaster()
    {
        Log("Connected to master");
    }
    public void CreateRoom()
    {
        PhotonNetwork.NickName = _nickNameInput.text;
        PlayerPrefs.SetString("NickName", _nickNameInput.text);
        PhotonNetwork.CreateRoom(null, new Photon.Realtime.RoomOptions { MaxPlayers = 20, CleanupCacheOnLeave = false});
    }
    public void JoinRoom()
    {
        PhotonNetwork.NickName = _nickNameInput.text;
        PlayerPrefs.SetString("NickName", _nickNameInput.text);
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinedRoom()
    {
        Log("Joined the room");
        PhotonNetwork.LoadLevel("Game");
    }

    private void Log(string message)
    {
        Debug.Log(message);
        _logText.text += "\n";
        _logText.text += message;
    }
}
