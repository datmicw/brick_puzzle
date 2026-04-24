using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Âm thanh hệ thống")]
    [SerializeField]
    private AudioSource bgmSource;

    [SerializeField]
    private AudioSource sfxSource;

    [SerializeField]
    private AudioClip failSFX;

    private LevelCreator _levelCreator;
    private LevelCreator LevelCreator => _levelCreator ??= FindObjectOfType<LevelCreator>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void LoseGame(string reason)
    {
        Debug.Log($"<color=red>GAME OVER:</color> {reason}");

        if (sfxSource != null && failSFX != null)
        {
            sfxSource.PlayOneShot(failSFX);
        }
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            if (player.TryGetComponent<PlayerInput>(out var pi))
                pi.enabled = false;

            if (player.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }

        Invoke(nameof(RestartCurrentLevel), 2.5f);
    }

    public void WinGame()
    {
        Debug.Log("<color=green>LEVEL COMPLETE!</color>");

        CancelInvoke(nameof(NextLevelDelayed));
        Invoke(nameof(NextLevelDelayed), 1.0f);
    }

    private void NextLevelDelayed()
    {
        if (LevelCreator != null)
        {
            LevelCreator.NextLevel();
        }
    }

    private void RestartCurrentLevel() => LevelCreator?.BuildLevel();
}
