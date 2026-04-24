using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // liệt kê các trạng thái của khối: đứng, nằm ngang, nằm dọc
    public enum BlockState
    {
        Standing,
        Horizontal,
        Vertical,
    }

    // tốc độ lăn của player
    [Header("Di chuyển")]
    public float rollSpeed = 0.2f;

    // độ cao của điểm tựa trên sàn(tile sàn là 1x1x1) thì độ cao là 0.5f
    [SerializeField]
    private float floorOffset = 0.5f;

    [Header("Logic Game")]
    public LayerMask tileLayer;
    public string goalTag = "Goal";

    // amam thanh di chuyển của player
    [Header("Âm thanh")]
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip moveSFX;

    // chiều cao khi đứng và nằm
    private const float StandingY = 1.5f;
    private const float LyingY = 1.0f;
    // tốc độ rơi và độ trễ khi thắng cuộc
    private const float WinDropSpeed = 5f;
    private const float WinDropDelay = 0.7f;
    // độ dài tia raycast để kiểm tra sàn
    private const float RayLength = 3f;

    // các thành phần của khối
    private BoxCollider _collider;
    private Rigidbody _rb;
    // biến kiểm tra xem khối có đang di chuyển không
    private bool _isMoving;
    // trạng thái hiện tại của khối
    private BlockState _currentState = BlockState.Standing;

    // tham chiếu đến bộ tạo cấp độ
    private LevelCreator _levelCreator;
    private LevelCreator LevelCreator =>
        _levelCreator != null ? _levelCreator : (_levelCreator = FindObjectOfType<LevelCreator>());

    // khởi tạo các thành phần trong awake
    void Awake()
    {
        _collider = GetComponent<BoxCollider>();
        _rb = GetComponent<Rigidbody>();

        // tắt trọng lực và làm cho rigidbody được điều khiển bằng tay
        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }
    }

    // đặt vị trí ban đầu của khối ở trạng thái đứng
    void Start()
    {
        Vector3 pos = transform.position;
        pos.y = StandingY;
        transform.position = pos;
    }

    // kiểm tra đầu vào từ bàn phím mỗi khung hình
    void Update()
    {
        // nếu khối đang di chuyển thì không nhận đầu vào
        if (_isMoving)
            return;

        // kiểm tra phím mũi tên để di chuyển
        if (Input.GetKeyDown(KeyCode.UpArrow))
            Move(Vector3.forward);
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            Move(Vector3.back);
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            Move(Vector3.left);
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            Move(Vector3.right);
    }

    // xử lý di chuyển của khối
    private void Move(Vector3 dir)
    {
        // tính điểm tựa cho phép quay
        Vector3 pivot = new Vector3(transform.position.x, floorOffset, transform.position.z);
        pivot += CalculatePivotOffset(dir);

        // tính trạng thái tiếp theo và vị trí mục tiêu
        BlockState nextState = CalculateNextState(dir);
        Vector3 targetPos = CalculateTargetPosition(dir, nextState);
        // tính trục quay dựa trên hướng di chuyển
        Vector3 axis = Vector3.Cross(Vector3.up, dir);

        // bắt đầu coroutine lăn
        StartCoroutine(RollRoutine(pivot, axis, targetPos, nextState));
    }

    // tính độ lệch của điểm tựa dựa trên trạng thái hiện tại và hướng
    private Vector3 CalculatePivotOffset(Vector3 dir)
    {
        // kiểm tra xem có phải là chuyển động ngang (trái/phải) không
        bool isLateral = Mathf.Abs(dir.x) > 0.1f;

        // trả về độ lệch phù hợp cho mỗi trạng thái
        return _currentState switch
        {
            BlockState.Standing => dir * 0.5f,
            BlockState.Horizontal => dir * (isLateral ? 1.0f : 0.5f),
            BlockState.Vertical => dir * (!isLateral ? 1.0f : 0.5f),
            _ => Vector3.zero,
        };
    }

    // coroutine để thực hiện animation lăn
    private IEnumerator RollRoutine(
        Vector3 pivot,
        Vector3 axis,
        Vector3 targetPos,
        BlockState nextState
    )
    {
        _isMoving = true;
        float angle = 0f;

        // quay khối quanh điểm tựa cho đến khi quay 90 độ
        while (angle < 90f)
        {
            // tính bước quay dựa trên tốc độ lăn
            float step = Mathf.Min(90f / rollSpeed * Time.deltaTime, 90f - angle);
            transform.RotateAround(pivot, axis, step);
            angle += step;
            yield return null;
        }

        // phát âm thanh di chuyển
        audioSource?.PlayOneShot(moveSFX);

        // đặt vị trí cuối cùng và cập nhật trạng thái
        transform.position = targetPos;
        _currentState = nextState;

        // căn chỉnh xoay và kiểm tra ô sàn
        SnapRotation();
        CheckTiles();
    }

    // căn chỉnh xoay về các giá trị 90 độ
    private void SnapRotation()
    {
        Vector3 e = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(
            Mathf.Round(e.x / 90) * 90,
            Mathf.Round(e.y / 90) * 90,
            Mathf.Round(e.z / 90) * 90
        );
    }

    // kiểm tra xem khối có được hỗ trợ bởi sàn không
    private void CheckTiles()
    {
        // nếu khối đang đứng
        if (_currentState == BlockState.Standing)
        {
            // kiểm tra xem có sàn bên dưới không
            if (!IsTileUnder(transform.position))
                GameManager.Instance.LoseGame("Rơi khỏi sàn!");
            // kiểm tra xem có đạt mục tiêu không
            else if (IsGoalUnder(transform.position))
                TriggerWin();
            else
                _isMoving = false;
        }
        else
        {
            // nếu khối nằm ngang hoặc nằm dọc thì kiểm tra cả hai đầu
            Vector3 offset =
                (_currentState == BlockState.Horizontal)
                    ? Vector3.right * 0.5f
                    : Vector3.forward * 0.5f;

            // nếu một trong hai đầu không có sàn thì thua
            if (
                !IsTileUnder(transform.position + offset)
                || !IsTileUnder(transform.position - offset)
            )
                GameManager.Instance.LoseGame("Nằm hụt sàn!");
            else
                _isMoving = false;
        }
    }

    // raycast để kiểm tra có ô sàn dưới vị trí không
    private bool IsTileUnder(Vector3 pos) =>
        Physics.Raycast(pos + Vector3.up, Vector3.down, RayLength, tileLayer);

    // raycast để kiểm tra có mục tiêu dưới vị trí không
    private bool IsGoalUnder(Vector3 pos)
    {
        if (
            Physics.Raycast(
                pos + Vector3.up,
                Vector3.down,
                out RaycastHit hit,
                RayLength,
                tileLayer
            )
        )
        {
            // kiểm tra xem đối tượng được raycast có thẻ mục tiêu không
            return hit.collider.CompareTag(goalTag);
        }
        return false;
    }

    // kích hoạt trạng thái thắng cuộc
    private void TriggerWin()
    {
        _isMoving = true;

        // tắt va chạm
        if (_collider != null)
            _collider.enabled = false;

        // bật trọng lực để khối rơi
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.useGravity = true;
            _rb.velocity = Vector3.down * WinDropSpeed;
        }

        // gọi cấp độ tiếp theo sau một độ trễ
        Invoke(nameof(CallNextLevel), WinDropDelay);
    }

    // chuyển sang cấp độ tiếp theo
    private void CallNextLevel() => LevelCreator?.NextLevel();

    // tính trạng thái tiếp theo của khối dựa trên hướng di chuyển
    private BlockState CalculateNextState(Vector3 dir)
    {
        // kiểm tra xem có phải là chuyển động ngang không
        bool isLateral = Mathf.Abs(dir.x) > 0.1f;

        // trả về trạng thái tiếp theo phù hợp
        return _currentState switch
        {
            BlockState.Standing => isLateral ? BlockState.Horizontal : BlockState.Vertical,
            BlockState.Horizontal => isLateral ? BlockState.Standing : BlockState.Horizontal,
            _ => isLateral ? BlockState.Vertical : BlockState.Standing,
        };
    }

    // tính vị trí mục tiêu của khối sau khi di chuyển
    private Vector3 CalculateTargetPosition(Vector3 dir, BlockState nextState)
    {
        // kiểm tra xem có liên quan đến trạng thái đứng không
        bool involvesStanding =
            _currentState == BlockState.Standing || nextState == BlockState.Standing;

        // tính vị trí dự kiến
        Vector3 target = transform.position + dir * (involvesStanding ? 1.5f : 1.0f);
        // đặt chiều cao phù hợp
        target.y = (nextState == BlockState.Standing) ? StandingY : LyingY;

        // căn chỉnh vị trí để khớp với lưới ô sàn
        target = nextState switch
        {
            BlockState.Standing => new Vector3(
                Mathf.Round(target.x),
                target.y,
                Mathf.Round(target.z)
            ),
            BlockState.Horizontal => new Vector3(
                Mathf.Round(target.x - 0.5f) + 0.5f,
                target.y,
                Mathf.Round(target.z)
            ),
            _ => new Vector3(Mathf.Round(target.x), target.y, Mathf.Round(target.z - 0.5f) + 0.5f),
        };

        return target;
    }
}
