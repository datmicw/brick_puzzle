using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class LevelCreator : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField]
    private GameObject tilePrefab;

    [SerializeField]
    private GameObject goalPrefab;

    [SerializeField]
    private GameObject playerPrefab;

    [Header("Màn chơi")]
    [TextArea(5, 10)]
    public List<string> allLevels;
    public int currentLevelIndex = 0;

    [Header("Hiệu ứng Spawn")]
    [SerializeField]
    private float tileFallDistance = 10f;

    [SerializeField]
    private float tileDuration = 0.5f;

    [SerializeField]
    private float delayBetweenTiles = 0.01f;

    [Header("Hiệu ứng Explode")]
    [SerializeField]
    private float explodeDuration = 0.8f;

    [SerializeField]
    private float explodeForce = 10f;

    private void Start() => BuildLevel();

    public void BuildLevel()
    {
        DOTween.KillAll();
        StopAllCoroutines();

        ClearScene();
        StartCoroutine(BuildLevelRoutine());
    }

    public void NextLevel() => StartCoroutine(ExplodeLevelRoutine());

    private void ClearScene()
    {
        foreach (Transform child in transform)
        {
            child.DOKill();
            Destroy(child.gameObject);
        }

        GameObject oldPlayer = GameObject.Find("Player");
        if (oldPlayer != null)
        {
            oldPlayer.transform.DOKill();
            Destroy(oldPlayer);
        }
    }

    private IEnumerator BuildLevelRoutine()
    {
        if (allLevels.Count == 0 || currentLevelIndex >= allLevels.Count)
            yield break;

        string[] rows = allLevels[currentLevelIndex]
            .Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        Vector3 playerSpawnPoint = Vector3.zero;
        bool hasPlayer = false;

        for (int z = 0; z < rows.Length; z++)
        {
            for (int x = 0; x < rows[z].Length; x++)
            {
                char c = rows[z][x];
                Vector3 finalPos = new Vector3(x, 0, -z);

                if (c == 'X' || c == 'P' || c == 'G')
                {
                    SpawnTile(c, finalPos);
                }

                if (c == 'P')
                {
                    playerSpawnPoint = new Vector3(x, 1.5f, -z);
                    hasPlayer = true;
                }
            }
            yield return new WaitForSeconds(delayBetweenTiles);
        }

        if (hasPlayer)
            StartCoroutine(SpawnPlayerRoutine(playerSpawnPoint));
    }

    private void SpawnTile(char tileChar, Vector3 finalPos)
    {
        GameObject prefab = tileChar switch
        {
            'X' or 'P' => tilePrefab,
            'G' => goalPrefab,
            _ => null,
        };
        if (prefab == null)
            return;

        GameObject tile = Instantiate(
            prefab,
            finalPos + Vector3.up * tileFallDistance,
            Quaternion.identity,
            transform
        );

        tile.transform.localScale = Vector3.zero;
        tile.transform.DOKill();
        tile.transform.DOMove(finalPos, tileDuration).SetEase(Ease.OutBack);
        tile.transform.DOScale(Vector3.one, tileDuration).SetEase(Ease.OutBack);
    }

    private IEnumerator SpawnPlayerRoutine(Vector3 spawnPos)
    {
        yield return new WaitForSeconds(0.3f);
        GameObject p = Instantiate(playerPrefab, spawnPos + Vector3.up * 8f, Quaternion.identity);
        p.name = "Player";

        var pc = p.GetComponent<PlayerController>();
        var pi = p.GetComponent<PlayerInput>();
        if (pc)
            pc.enabled = false;
        if (pi)
            pi.enabled = false;

        p.transform.DOKill();
        p.transform.DOMove(spawnPos, 0.5f)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                if (pc)
                    pc.enabled = true;
                if (pi)
                    pi.enabled = true;
                Camera.main.transform.DOShakePosition(0.15f, 0.2f);
            });
    }

    private IEnumerator ExplodeLevelRoutine()
    {
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            player.transform.DOKill();
            player.transform.DOScale(Vector3.zero, 0.3f);
        }

        foreach (Transform child in transform)
        {
            child.DOKill();

            Vector3 randomDir = new Vector3(
                Random.Range(-explodeForce, explodeForce),
                Random.Range(5, explodeForce),
                Random.Range(-explodeForce, explodeForce)
            );
            child.DOMove(child.position + randomDir, explodeDuration).SetEase(Ease.InCubic);
            child.DOScale(Vector3.zero, explodeDuration).SetEase(Ease.InCubic);
        }

        yield return new WaitForSeconds(explodeDuration + 0.1f);

        if (allLevels.Count > 0)
        {
            currentLevelIndex++;
            if (currentLevelIndex >= allLevels.Count)
            {
                currentLevelIndex = 0;
            }
        }

        BuildLevel();
    }
}
