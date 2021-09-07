using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;

public class PlayerControls : MonoBehaviour, IPunObservable
{
    [Header("SerifalField")]
    [SerializeField] private Sprite _otherSprite;
    [SerializeField] private Sprite _deadPlayerSprite;
    [SerializeField] private Transform _ladder;
    [SerializeField] private TextMeshPro _nickNameText;

    private PhotonView _photonView;
    private SpriteRenderer _spriteRenderer;


    private Vector2Int _direction;
    private Vector2Int _gamePosition;

    private bool _isDead;
    private string _nickName;
    private int _score;

    public Vector2Int Direction { get => _direction; set => _direction = value; }
    public Vector2Int GamePosition { get => _gamePosition; set => _gamePosition = value; }
    public PhotonView PhotonView { get => _photonView; set => _photonView = value; }
    public bool IsDead { get => _isDead; set => _isDead = value; }
    public string NickName { get => _nickName; set => _nickName = value; }
    public int Score { get => _score; set => _score = value; }

    void Start()
    {
        _photonView = GetComponent<PhotonView>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        GamePosition = new Vector2Int((int)transform.position.x, (int)transform.position.y);
        FindObjectOfType<MapController>().AddPlayer(this);

        _nickName = _photonView.Owner.NickName;

        _nickNameText.text = _nickName;
       

        if (!_photonView.IsMine)
            _spriteRenderer.sprite = _otherSprite;
        else
            _nickNameText.color = Color.green;
    }

    void Update()
    {
        if (PhotonView.IsMine && !IsDead)
        {
            if (Input.GetKey(KeyCode.LeftArrow)) Direction = Vector2Int.left;//transform.Translate(-Time.deltaTime * 5, 0, 0);
            if (Input.GetKey(KeyCode.RightArrow)) Direction = Vector2Int.right;
            if (Input.GetKey(KeyCode.UpArrow)) Direction = Vector2Int.up;//transform.Translate(-Time.deltaTime * 5, 0, 0);
            if (Input.GetKey(KeyCode.DownArrow)) Direction = Vector2Int.down;
        }

        if (Direction == Vector2.left) _spriteRenderer.flipX = true;
        if (Direction == Vector2.right) _spriteRenderer.flipX = false;

        transform.position = Vector3.Lerp(transform.position, (Vector2)GamePosition, Time.deltaTime * 3);
    }

    public void SetLadderLength(int length)
    {
        for (int i = 0; i < _ladder.childCount; i++)
        {
            _ladder.GetChild(i).gameObject.SetActive(i < length);
        }

        while (_ladder.childCount < length)
        {
            Transform lastTile = _ladder.GetChild(_ladder.childCount - 1);
            Instantiate(lastTile, lastTile.position + Vector3.down, Quaternion.identity, _ladder);
        }
    }

    public void Kill()
    {
        _spriteRenderer.sprite = _deadPlayerSprite;
        IsDead = true;
        SetLadderLength(0);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(Direction);
        }
        else
        {
            Direction = (Vector2Int)stream.ReceiveNext();
        }
    }
}
