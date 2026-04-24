using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class LevelCreator : MonoBehaviour
{
    //  các trường trong inspector 
    [Header("Prefabs")]
    [SerializeField]
    private GameObject tilePrefab;

    [SerializeField]
    private GameObject goalPrefab;

    [SerializeField]
    private GameObject playerPrefab;

    //  danh sách các màn chơi 
    [Header("Màn chơi")]
    public List<string> allLevels;
    public int currentLevelIndex = 0;

    //  hiệu ứng xuất hiện của ô sàn 
    [Header("Hiệu ứng Spawn")]
    [SerializeField]
    private float tileFallDistance = 10f;

    [SerializeField]
    private float tileDuration = 0.5f;

    [SerializeField]
    private float delayBetweenTiles = 0.01f;

    //  hiệu ứng nổ tung khi thắng cuộc 
    [Header("Hiệu ứng Explode")]
    [SerializeField]
    private float explodeDuration = 0.8f;

    [SerializeField]
    private float explodeForce = 10f;

    //  các hằng số 
    private const float PlayerSpawnHeight = 1.1f;
    private const float PlayerDropHeight = 8f;
    private const float PlayerDropDuration = 0.5f;
    private const float PlayerSpawnDelay = 0.3f;

    //  vòng đời unity 
    void Start() => BuildLevel();

    //  các hàm công khai 
    public void BuildLevel()
    {
        StopAllCoroutines();
        ClearScene();
        StartCoroutine(BuildLevelRoutine());
    }

    // chuyển sang màn chơi tiếp theo
    public void NextLevel() => StartCoroutine(ExplodeLevelRoutine());

    //  quản lý cảnh 
    private void ClearScene()
    {
        // xóa tất cả các ô sàn và mục tiêu
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        // xóa người chơi cũ
        GameObject oldPlayer = GameObject.Find("Player");
        if (oldPlayer != null)
            Destroy(oldPlayer);
    }

    //  các coroutine xây dựng màn chơi 
    private IEnumerator BuildLevelRoutine()
    {
        // phân tích dữ liệu màn chơi
        string[] rows = ParseLevel(allLevels[currentLevelIndex]);

        Vector3 playerSpawnPoint = Vector3.zero;
        bool hasPlayer = false;

        // duyệt qua từng ô trong lưới
        for (int z = 0; z < rows.Length; z++)
        {
            for (int x = 0; x < rows[z].Length; x++)
            {
                char c = rows[z][x];
                Vector3 finalPos = new Vector3(x, 0, -z);

                // tạo ô sàn hoặc mục tiêu
                SpawnTile(c, finalPos);

                // tìm vị trí xuất hiện của người chơi
                if (c == 'P')
                {
                    playerSpawnPoint = new Vector3(x, PlayerSpawnHeight, -z);
                    hasPlayer = true;
                }
                yield return new WaitForSeconds(delayBetweenTiles);
            }
        }

        // tạo người chơi nếu có
        if (hasPlayer)
            StartCoroutine(SpawnPlayerRoutine(playerSpawnPoint));
    }

    // tạo ô sàn hoặc mục tiêu với hiệu ứng rơi
    private void SpawnTile(char tileChar, Vector3 finalPos)
    {
        // xác định prefab dựa trên ký tự
        GameObject prefab = tileChar switch
        {
            'X' or 'P' => tilePrefab,
            'G' => goalPrefab,
            _ => null,
        };

        if (prefab == null)
            return;

        // tạo ô sàn từ vị trí cao hơn
        GameObject tile = Instantiate(
            prefab,
            finalPos + Vector3.up * tileFallDistance,
            Quaternion.identity,
            transform
        );
        tile.transform.localScale = Vector3.zero;
        
        // di chuyển ô sàn xuống vị trí cuối cùng với hiệu ứng
        tile.transform.DOMove(finalPos, tileDuration).SetEase(Ease.OutBack);
        tile.transform.DOScale(Vector3.one, tileDuration).SetEase(Ease.OutBack);
    }

    // tạo người chơi với hiệu ứng rơi từ trên trời
    private IEnumerator SpawnPlayerRoutine(Vector3 spawnPos)
    {
        yield return new WaitForSeconds(PlayerSpawnDelay);

        // tạo người chơi từ vị trí cao
        GameObject p = Instantiate(
            playerPrefab,
            spawnPos + Vector3.up * PlayerDropHeight,
            Quaternion.identity
        );
        p.name = "Player";

        PlayerController pc = p.GetComponent<PlayerController>();
        if (pc != null)
            pc.enabled = false;

        // di chuyển người chơi xuống vị trí xuất hiện
        p.transform.DOMove(spawnPos, PlayerDropDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                // bật điều khiển người chơi và tạo hiệu ứng camera khi cập bộ
                if (pc != null)
                    pc.enabled = true;
                Camera.main.transform.DOShakePosition(0.15f, 0.2f);
            });
    }

    //  coroutine nổ tung khi thắng cuộc 
    private IEnumerator ExplodeLevelRoutine()
    {
        // thu nhỏ người chơi trước tiên
        GameObject player = GameObject.Find("Player");
        if (player != null)
            player.transform.DOScale(Vector3.zero, 0.3f);

        // tán xạ tất cả các ô sàn
        foreach (Transform child in transform)
        {
            // tạo hướng ngẫu nhiên
            Vector3 randomDir = new Vector3(
                Random.Range(-explodeForce, explodeForce),
                Random.Range(5f, explodeForce),
                Random.Range(-explodeForce, explodeForce)
            );

            // di chuyển ô sàn ra ngoài
            child.DOMove(child.position + randomDir, explodeDuration).SetEase(Ease.InCubic);
            
            // quay ô sàn ngẫu nhiên
            child.DORotate(
                new Vector3(
                    Random.Range(-360, 360),
                    Random.Range(-360, 360),
                    Random.Range(-360, 360)
                ),
                explodeDuration,
                RotateMode.FastBeyond360
            );
            
            // thu nhỏ ô sàn
            child.DOScale(Vector3.zero, explodeDuration).SetEase(Ease.InCubic);
        }

        yield return new WaitForSeconds(explodeDuration + 0.1f);

        // chuyển sang màn chơi tiếp theo
        AdvanceLevel();
        BuildLevel();
    }

    //   các hàm trợ giúp 
    private void AdvanceLevel()
    {
        // tăng chỉ số màn chơi và quay lại đầu nếu hết
        currentLevelIndex = (currentLevelIndex + 1) % allLevels.Count;
    }

    // phân tích dữ liệu màn chơi thành mảng các hàng
    private static string[] ParseLevel(string data) =>
        data.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
}
