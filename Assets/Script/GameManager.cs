using UnityEngine;

public class GameManager : MonoBehaviour
{
    //  singleton 
    public static GameManager Instance { get; private set; }

    void Awake()
    {
        // kiểm tra nếu singleton đã tồn tại thì hủy instance này
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    //  các trường 
    [Header("Nhạc nền")]
    [SerializeField]
    private AudioSource bgmSource;

    // lưu trữ tham chiếu để tránh gọi findObjectOfType nhiều lần
    private LevelCreator _levelCreator;
    private LevelCreator LevelCreator =>
        _levelCreator != null ? _levelCreator : (_levelCreator = FindObjectOfType<LevelCreator>());

    //  vòng đời unity 
    void Start()
    {
        // khởi tạo cache trước để lần gọi đầu tiên nhanh chóng
        _ = LevelCreator;
    }

    //  quản lý âm nhạc 
    public void StopMusic() => bgmSource?.Stop();

    public void PlayMusic()
    {
        // phát nhạc nếu nguồn âm thanh không null và chưa phát
        if (bgmSource != null && !bgmSource.isPlaying)
            bgmSource.Play();
    }

    public void WinGame()
    {
        // in thông báo thắng cuộc
        Debug.Log("<color=gold>LEVEL COMPLETE!</color>");
        Invoke(nameof(LoadNextLevel), 2.0f);
    }

    public void LoseGame(string reason)
    {
        // in thông báo thua cuộc với lý do
        Debug.Log($"<color=red>GAME OVER:</color> {reason}");
        Invoke(nameof(RestartCurrentLevel), 1.5f);
    }

    //  các hàm gọi lại riêng tư 
    private void LoadNextLevel() => LevelCreator?.NextLevel();

    private void RestartCurrentLevel() => LevelCreator?.BuildLevel();
}