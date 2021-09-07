using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapController : MonoBehaviour, IOnEventCallback
{
    //[SerializeField] private GameObject _cellPrefab;
    [SerializeField] private PlayerTop _top;
    [SerializeField] private RuleTile _cellTile;
    [SerializeField] private Tilemap _tilemap;

    private List<PlayerControls> _players = new List<PlayerControls>();
    private bool[,] _cells;
    private double lastTickTime;

    public List<PlayerControls> Players { get => _players; set => _players = value; }

    void Start()
    {
        _cells = new bool[20, 10];

        for(int x = 0; x < _cells.GetLength(0); x++)
        {
            for(int y = 0; y < _cells.GetLength(1); y++)
            {
                _tilemap.SetTile(new Vector3Int(x, y, 0), _cellTile);
                Debug.Log(new Vector3Int(x, y, 0));
                _cells[x, y] = true;
               // _cells[x, y] = Instantiate(_cellPrefab, new Vector3(x, y), Quaternion.identity, transform);
            }
        }
    }

    void Update()
    {
        if(PhotonNetwork.Time > lastTickTime + 1 &&
           PhotonNetwork.IsMasterClient &&
           PhotonNetwork.CurrentRoom.PlayerCount > 1)
        {
            Vector2Int[] directions = _players
            .Where(p=> !p.IsDead)
            .OrderBy(p => p.PhotonView.Owner.ActorNumber)
            .Select(p => p.Direction)
            .ToArray();

            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            SendOptions sendOptions = new SendOptions { Reliability = true };
            PhotonNetwork.RaiseEvent(42, directions, options, sendOptions);

            PerformTick(directions);
        }
    }

    public void SendSyncData(Player player)
    {
        SyncData data = new SyncData();

        data.Positions = new Vector2Int[_players.Count];
        data.Scores = new int[_players.Count];

        PlayerControls[] sortedPlayers = _players
            .Where(p => !p.IsDead)
            .OrderBy(p => p.PhotonView.Owner.ActorNumber)
            .ToArray();

        for(int i = 0; i < sortedPlayers.Length; i++)
        {
            data.Positions[i] = sortedPlayers[i].GamePosition;
            data.Scores[i] = sortedPlayers[i].Score;
        }

        data.MapData = new BitArray(20 * 10);
        for (int x = 0; x < _cells.GetLength(0); x++)
        {
            for(int y = 0; y < _cells.GetLength(1); y++)
            {
                data.MapData.Set(x + y * _cells.GetLength(0), _cells[x, y]);
            }
        }
        RaiseEventOptions options = new RaiseEventOptions { TargetActors = new[] { player.ActorNumber } };
        SendOptions sendOptions = new SendOptions { Reliability = true };
        PhotonNetwork.RaiseEvent(43, data, options, sendOptions);
    }

    public void AddPlayer(PlayerControls player)
    {
        _players.Add(player);

        SetCell(player.GamePosition, false);
    }

    public void SetCell(Vector2Int pos, bool set)
    {
        _cells[pos.x, pos.y] = set;
        _tilemap.SetTile((Vector3Int)pos, set ? _cellTile : null);

    }

    public void OnEvent(EventData photonEvent)
    {
        switch (photonEvent.Code)
        {
            case 42:
                Vector2Int[] directions = (Vector2Int[])photonEvent.CustomData;
                PerformTick(directions);
            break;
            case 43:
                var data = (SyncData)photonEvent.CustomData;
                StartCoroutine(OnSyncDataReceived(data));
                break;
        }
    }

    private IEnumerator OnSyncDataReceived(SyncData data)
    {
        PlayerControls[] sortedPlayers;

        do
        {
            yield return null;

            sortedPlayers = _players
            .Where(p => !p.IsDead)
            .Where(p => !p.PhotonView.IsMine)
            .OrderBy(p => p.PhotonView.Owner.ActorNumber)
            .ToArray();
        } while (sortedPlayers.Length != data.Positions.Length);


        for(int i = 0; i < sortedPlayers.Length; i++)
        {
            sortedPlayers[i].GamePosition = data.Positions[i];
            sortedPlayers[i].Score = data.Scores[i];

            sortedPlayers[i].transform.position = (Vector2)sortedPlayers[i].GamePosition;
        }

        for (int x = 0; x < _cells.GetLength(0); x++)
        {
            for (int y = 0; y < _cells.GetLength(1); y++)
            {
                bool cellActive = data.MapData.Get(x + y * _cells.GetLength(0));
                if(!cellActive) SetCell(new Vector2Int(x, y), false);
            }
        }
    }

    private void PerformTick(Vector2Int[] directions)
    {
        //if (_players.Count != directions.Length) return;

        PlayerControls[] sortedPlayers = _players
            .Where(p => !p.IsDead)
            .OrderBy(p => p.PhotonView.Owner.ActorNumber)
            .ToArray();

        int i = 0;

        foreach(var player in sortedPlayers)
        {
            player.Direction = directions[i++];

            MinePlayerBlock(player);
        }

        foreach (var player in sortedPlayers)
        {
            //Debug.Log(player.NickName);

            MovePlayer(player);
        }

        foreach (var player in _players.Where(p => p.IsDead))
        {
            Vector2Int pos = player.GamePosition;
            while (pos.y > 0 && !_cells[pos.x, pos.y - 1])
            {
                pos.y--;
            }
            player.GamePosition = pos;
        }

        _top.SetTexts(_players);
        lastTickTime = PhotonNetwork.Time;
    }

    private void MinePlayerBlock(PlayerControls player)
    {
        if (player.Direction == Vector2Int.zero) return;

        Vector2Int targetPosition = player.GamePosition + player.Direction;

        //Копаем блок:
        if (targetPosition.x < 0) return;
        if (targetPosition.y < 0) return;
        if (targetPosition.x >= _cells.GetLength(0)) return;
        if (targetPosition.y >= _cells.GetLength(1)) return;

        if(_cells[targetPosition.x, targetPosition.y])
        {
            SetCell(targetPosition, false);
            player.Score++;
        }

        // Проверяем не убило ли нас компанием

        Vector2Int pos = targetPosition;
        PlayerControls minePlayer = _players.First(p => p.PhotonView.IsMine);
        if(minePlayer != player)
        {
            while (pos.y < _cells.GetLength(1) && !_cells[pos.x, pos.y])
            {
                if (pos == minePlayer.GamePosition)
                {
                    player.Score += minePlayer.Score;
                    minePlayer.Score = 0;
                    PhotonNetwork.LeaveRoom();
                    break;
                }
                pos.y++;
            }
        }

    }

    private void MovePlayer(PlayerControls player)
    {
        player.GamePosition += player.Direction;

        if (player.GamePosition.x < 0) player.GamePosition = new Vector2Int(0, player.GamePosition.y);
        if (player.GamePosition.y < 0) player.GamePosition = new Vector2Int(player.GamePosition.x, 0);
        if (player.GamePosition.x >= _cells.GetLength(0)) player.GamePosition = new Vector2Int(_cells.GetLength(0) - 1, player.GamePosition.y);
        if (player.GamePosition.y >= _cells.GetLength(1)) player.GamePosition = new Vector2Int(player.GamePosition.x, _cells.GetLength(1) - 1);

        SetCell(player.GamePosition, false);

        int ladderLength = 0;
        Vector2Int pos = player.GamePosition;
        while (pos.y > 0 && !_cells[pos.x, pos.y - 1])
        {
            ladderLength++;
            pos.y--;
        }
        player.SetLadderLength(ladderLength);
    }

    public void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }
}
