using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Cấu hình di chuyển")]
    public float rollSpeed = 0.2f;

    [SerializeField]
    private float floorOffset = 0.5f;

    [Header("Kiểm tra sàn")]
    public LayerMask tileLayer;
    public string goalTag = "Goal";

    private BlockState _currentState = BlockState.Standing;
    private bool _isMoving;
    public bool IsMoving => _isMoving;

    private PlayerAudio _audio;
    private Rigidbody _rb;
    private BoxCollider _collider;

    void Awake()
    {
        _audio = GetComponent<PlayerAudio>();
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<BoxCollider>();

        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }
    }

    void Start()
    {
        transform.position = new Vector3(
            transform.position.x,
            BlockSettings.StandingY,
            transform.position.z
        );
    }

    public void Move(Vector3 dir)
    {
        if (_isMoving)
            return;

        Vector3 pivot = BlockMath.GetPivot(transform.position, dir, _currentState, floorOffset);
        BlockState nextState = BlockMath.GetNextState(_currentState, dir);
        Vector3 targetPos = BlockMath.GetTargetPosition(
            transform.position,
            dir,
            _currentState,
            nextState
        );
        Vector3 axis = Vector3.Cross(Vector3.up, dir);

        StartCoroutine(RollRoutine(pivot, axis, targetPos, nextState));
    }

    private IEnumerator RollRoutine(
        Vector3 pivot,
        Vector3 axis,
        Vector3 targetPos,
        BlockState nextState
    )
    {
        _isMoving = true;
        _audio?.PlayMove();

        float angle = 0f;
        while (angle < 90f)
        {
            float step = Mathf.Min(90f / rollSpeed * Time.deltaTime, 90f - angle);
            transform.RotateAround(pivot, axis, step);
            angle += step;
            yield return null;
        }

        transform.position = targetPos;
        _currentState = nextState;

        SnapRotation();
        CheckFloor();
    }

    private void SnapRotation()
    {
        Vector3 e = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(
            Mathf.Round(e.x / 90) * 90,
            Mathf.Round(e.y / 90) * 90,
            Mathf.Round(e.z / 90) * 90
        );
    }

    private void CheckFloor()
    {
        bool isSafe = false;

        if (_currentState == BlockState.Standing)
        {
            if (IsTileUnder(transform.position))
            {
                if (IsGoalUnder(transform.position))
                {
                    HandleWin();
                    return;
                }
                isSafe = true;
            }
        }
        else
        {
            Vector3 offset =
                (_currentState == BlockState.Horizontal)
                    ? Vector3.right * 0.5f
                    : Vector3.forward * 0.5f;
            if (
                IsTileUnder(transform.position + offset) && IsTileUnder(transform.position - offset)
            )
                isSafe = true;
        }

        if (!isSafe)
        {
            _isMoving = true;
            GameManager.Instance.LoseGame("Rơi khỏi sàn!");
        }
        else
        {
            _isMoving = false;
        }
    }

    private bool IsTileUnder(Vector3 pos) =>
        Physics.Raycast(pos + Vector3.up, Vector3.down, BlockSettings.RayLength, tileLayer);

    private bool IsGoalUnder(Vector3 pos)
    {
        if (
            Physics.Raycast(
                pos + Vector3.up,
                Vector3.down,
                out RaycastHit hit,
                BlockSettings.RayLength,
                tileLayer
            )
        )
            return hit.collider.CompareTag(goalTag);
        return false;
    }

    private void HandleWin()
    {
        _isMoving = true;

        if (_collider)
            _collider.enabled = false;
        if (_rb)
        {
            _rb.isKinematic = false;
            _rb.useGravity = true;
            _rb.velocity = Vector3.down * BlockSettings.WinDropSpeed;
        }

        GameManager.Instance.WinGame();
    }
}
