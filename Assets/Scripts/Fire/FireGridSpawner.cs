using System.Collections.Generic;
using UnityEngine;
using FlameFighter.Fire;

public class FireGridSpawner : MonoBehaviour
{
    [Header("Grid Configuration")]
    [SerializeField] private int gridWidth = 10;
    [SerializeField] private int gridHeight = 10;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float yOffset = 0f;
    [SerializeField] private bool centerGrid = true;
    [SerializeField] private string tileLayerName = "Tile";

    [Header("Visuals & VFX")]
    [SerializeField] private Material unlitMaterial;
    [SerializeField] private Material burningMaterial;
    [SerializeField] private Material extinguishedMaterial;
    [SerializeField] private Material burnedOutMaterial;
    [Tooltip("Opsional: Prefab partikel/VFX api yang akan dimunculkan di atas grid saat terbakar.")]
    [SerializeField] private GameObject fireVfxPrefab;

    [Header("Spawner & Spread Settings")]
    [SerializeField] private int initialBurnCount = 5;
    [SerializeField] private float checkInterval = 5f;
    [SerializeField] private int newFireSpawnCount = 1;

    [Tooltip("Peluang setiap petak yang terbakar menyebarkan api ke tetangganya tiap interval (0 - 1).")]
    [Range(0f, 1f)]
    [SerializeField] private float spreadChancePerTile = 0.5f;
    [Tooltip("Jika true, menyebar ke 8 arah (termasuk diagonal). Jika false, hanya 4 arah (atas, bawah, kiri, kanan).")]
    [SerializeField] private bool spreadDiagonally = false;
    [Tooltip("Maksimal tetangga baru yang terbakar per sumber api dalam 1x pengecekan.")]
    [SerializeField] private int maxSpreadNeighborsPerTile = 1;

    [Header("L4D2 Stress Director Settings")]
    [SerializeField] private float maxStressLevel = 100f;
    [SerializeField] private float resumeStressThreshold = 50f;
    [SerializeField] private float stressIncreasePerBurningTilePerSec = 1.5f;
    [SerializeField] private float stressDecreaseOnExtinguish = 10f;
    [SerializeField] private float stressDecayPerSecWhenPaused = 4f;

    [Header("Runtime Status (Read-Only)")]
    [SerializeField] private float currentStressLevel = 0f;
    [SerializeField] private bool isSpawnerActive = true;
    [SerializeField] private int activeBurningCount = 0;

    private GameObject[,] tiles;
    private MeshRenderer[,] tileRenderers;
    private Flammable[,] flammables;
    private GameObject[,] activeVfx;

    private float intervalTimer;
    private int cachedTileLayer = -1;

    private static readonly Vector2Int[] CardinalDirections =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    private static readonly Vector2Int[] AllDirections =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right,
        new Vector2Int(1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, 1),
        new Vector2Int(-1, -1)
    };

    // Public Properties untuk integrasi UI / sistem lain
    public float CurrentStressLevel => currentStressLevel;
    public float MaxStressLevel => maxStressLevel;
    public bool IsSpawnerActive => isSpawnerActive;
    public int ActiveBurningCount => activeBurningCount;

    private void Start()
    {
        GenerateGrid();
        SpawnInitialFires();
    }

    private void Update()
    {
        UpdateStressDirector();

        if (!isSpawnerActive) return;

        intervalTimer += Time.deltaTime;
        if (intervalTimer >= checkInterval)
        {
            intervalTimer = 0f;
            ExecuteFiveSecondTick();
        }
    }

    #region Procedural Grid Generation

    public void GenerateGrid()
    {
        ClearExistingGrid();

        tiles = new GameObject[gridWidth, gridHeight];
        tileRenderers = new MeshRenderer[gridWidth, gridHeight];
        flammables = new Flammable[gridWidth, gridHeight];
        activeVfx = new GameObject[gridWidth, gridHeight];

        cachedTileLayer = LayerMask.NameToLayer(tileLayerName);
        Vector3 boundsOffset = centerGrid
            ? new Vector3((gridWidth * tileSize) * 0.5f, 0f, (gridHeight * tileSize) * 0.5f)
            : Vector3.zero;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                tiles[x, y] = GenerateSingleTile(x, y, boundsOffset);
            }
        }
    }

    private GameObject GenerateSingleTile(int x, int y, Vector3 boundsOffset)
    {
        GameObject tileObject = new GameObject($"Tile_X:{x}_Y:{y}");
        tileObject.transform.SetParent(transform, false);

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        MeshRenderer meshRenderer = tileObject.AddComponent<MeshRenderer>();

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - boundsOffset;
        vertices[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - boundsOffset;
        vertices[2] = new Vector3((x + 1) * tileSize, yOffset, y * tileSize) - boundsOffset;
        vertices[3] = new Vector3((x + 1) * tileSize, yOffset, (y + 1) * tileSize) - boundsOffset;

        int[] tris = new int[6] { 0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (unlitMaterial != null)
        {
            meshRenderer.material = unlitMaterial;
        }

        if (cachedTileLayer >= 0)
        {
            tileObject.layer = cachedTileLayer;
        }

        tileObject.AddComponent<BoxCollider>();
        tileRenderers[x, y] = meshRenderer;

        return tileObject;
    }

    private void ClearExistingGrid()
    {
        if (tiles == null) return;

        for (int x = 0; x < tiles.GetLength(0); x++)
        {
            for (int y = 0; y < tiles.GetLength(1); y++)
            {
                if (tiles[x, y] != null)
                {
                    Destroy(tiles[x, y]);
                }
            }
        }

        activeBurningCount = 0;
    }

    #endregion

    #region L4D2 Stress Director Logic

    private void UpdateStressDirector()
    {
        // Hitung ulang jumlah petak terbakar secara akurat
        activeBurningCount = CountBurningTiles();

        if (isSpawnerActive)
        {
            // Stress naik per detik: jumlah grid terbakar * 1.5
            if (activeBurningCount > 0)
            {
                currentStressLevel += activeBurningCount * stressIncreasePerBurningTilePerSec * Time.deltaTime;
                currentStressLevel = Mathf.Clamp(currentStressLevel, 0f, maxStressLevel);
            }

            // Jika mencapai 100, hentikan spawner & masuk fase pemulihan stress
            if (currentStressLevel >= maxStressLevel)
            {
                isSpawnerActive = false;
                intervalTimer = 0f;
            }
        }
        else
        {
            // Fase istirahat: stress berkurang bertahap per detik
            currentStressLevel -= stressDecayPerSecWhenPaused * Time.deltaTime;
            currentStressLevel = Mathf.Clamp(currentStressLevel, 0f, maxStressLevel);

            // Aktif kembali saat menyentuh atau di bawah 50
            if (currentStressLevel <= resumeStressThreshold)
            {
                isSpawnerActive = true;
                intervalTimer = 0f;
            }
        }
    }

    #endregion

    #region Fire Spawning & Spreading

    private void SpawnInitialFires()
    {
        List<Vector2Int> validTiles = GetAllValidTiles();
        int countToSpawn = Mathf.Min(initialBurnCount, validTiles.Count);

        for (int i = 0; i < countToSpawn; i++)
        {
            int randomIndex = Random.Range(0, validTiles.Count);
            Vector2Int chosen = validTiles[randomIndex];
            validTiles.RemoveAt(randomIndex);

            IgniteTile(chosen.x, chosen.y);
        }
    }

    private void ExecuteFiveSecondTick()
    {
        // Snapshot semua petak yang sedang terbakar saat tick dimulai
        // agar api yang baru menyebar tidak langsung menyebar lagi di tick yang sama
        List<Vector2Int> currentBurningTiles = GetCurrentlyBurningTiles();

        // 1. Jika ada api yang menyala, sebarkan api ke petak sekitar
        if (currentBurningTiles.Count > 0)
        {
            SpreadFireFromTiles(currentBurningTiles);
        }

        // 2. Syarat utama memunculkan api baru: TIDAK ada api yang menyala di grid manapun saat itu
        if (CountBurningTiles() == 0)
        {
            SpawnRandomNewFires(newFireSpawnCount);
        }
    }

    private void SpreadFireFromTiles(List<Vector2Int> burningSources)
    {
        Vector2Int[] directions = spreadDiagonally ? AllDirections : CardinalDirections;

        foreach (Vector2Int source in burningSources)
        {
            // Pastikan sumber masih menyala
            if (flammables[source.x, source.y] == null || !flammables[source.x, source.y].IsBurning)
                continue;

            if (Random.value > spreadChancePerTile)
                continue;

            // Kumpulkan tetangga yang valid di sekitar sumber api
            List<Vector2Int> validNeighbors = new List<Vector2Int>();
            foreach (Vector2Int dir in directions)
            {
                int nx = source.x + dir.x;
                int ny = source.y + dir.y;

                if (IsInsideGrid(nx, ny) && IsTileValidToIgnite(nx, ny))
                {
                    validNeighbors.Add(new Vector2Int(nx, ny));
                }
            }

            if (validNeighbors.Count == 0) continue;

            int spreadCount = Mathf.Min(maxSpreadNeighborsPerTile, validNeighbors.Count);
            for (int i = 0; i < spreadCount; i++)
            {
                int randIdx = Random.Range(0, validNeighbors.Count);
                Vector2Int target = validNeighbors[randIdx];
                validNeighbors.RemoveAt(randIdx);

                IgniteTile(target.x, target.y);
            }
        }
    }

    private void SpawnRandomNewFires(int count)
    {
        List<Vector2Int> validTiles = GetAllValidTiles();
        int spawnAmount = Mathf.Min(count, validTiles.Count);

        for (int i = 0; i < spawnAmount; i++)
        {
            int randIdx = Random.Range(0, validTiles.Count);
            Vector2Int chosen = validTiles[randIdx];
            validTiles.RemoveAt(randIdx);

            IgniteTile(chosen.x, chosen.y);
        }
    }

    /// <summary>
    /// Menambahkan komponen Flammable secara otomatis jika belum ada, lalu menyalakan api.
    /// </summary>
    public bool IgniteTile(int x, int y)
    {
        if (!IsInsideGrid(x, y) || !IsTileValidToIgnite(x, y))
            return false;

        Flammable flammable = flammables[x, y];

        // Otomatis tambahkan komponen Flammable saat grid pertama kali mendapat state burn
        if (flammable == null)
        {
            flammable = tiles[x, y].AddComponent<Flammable>();
            flammable.Initialize(BurnState.Unlit);
            flammables[x, y] = flammable;

            // Subscribe event perubahan state
            int tileX = x;
            int tileY = y;
            flammable.OnIgnited += _ => HandleTileIgnited(tileX, tileY);
            flammable.OnExtinguished += _ => HandleTileExtinguished(tileX, tileY);
            flammable.OnBurnedOut += _ => HandleTileBurnedOut(tileX, tileY);
        }

        return flammable.Ignite();
    }

    #endregion

    #region Event Handlers & Helpers

    private void HandleTileIgnited(int x, int y)
    {
        if (burningMaterial != null)
            tileRenderers[x, y].material = burningMaterial;

        if (fireVfxPrefab != null && activeVfx[x, y] == null)
        {
            Vector3 spawnPos = tileRenderers[x, y].bounds.center;
            activeVfx[x, y] = Instantiate(fireVfxPrefab, spawnPos, Quaternion.identity, tiles[x, y].transform);
        }

        activeBurningCount = CountBurningTiles();
    }

    private void HandleTileExtinguished(int x, int y)
    {
        // Memadamkan api menurunkan stress level sebesar 10 per grid
        currentStressLevel = Mathf.Max(0f, currentStressLevel - stressDecreaseOnExtinguish);

        if (extinguishedMaterial != null)
            tileRenderers[x, y].material = extinguishedMaterial;

        RemoveTileVfx(x, y);
        activeBurningCount = CountBurningTiles();
    }

    private void HandleTileBurnedOut(int x, int y)
    {
        if (burnedOutMaterial != null)
            tileRenderers[x, y].material = burnedOutMaterial;

        RemoveTileVfx(x, y);
        activeBurningCount = CountBurningTiles();
    }

    private void RemoveTileVfx(int x, int y)
    {
        if (activeVfx[x, y] != null)
        {
            Destroy(activeVfx[x, y]);
            activeVfx[x, y] = null;
        }
    }

    public bool IsTileValidToIgnite(int x, int y)
    {
        if (!IsInsideGrid(x, y)) return false;

        Flammable flammable = flammables[x, y];
        // Jika belum punya komponen Flammable, berarti masih Unlit (valid)
        if (flammable == null) return true;

        // Jika sudah ada, cek apakah tidak sedang terbakar, tidak BurnedOut, dan cooldown sudah habis
        return flammable.CanIgnite;
    }

    private List<Vector2Int> GetAllValidTiles()
    {
        List<Vector2Int> valid = new List<Vector2Int>();
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (IsTileValidToIgnite(x, y))
                {
                    valid.Add(new Vector2Int(x, y));
                }
            }
        }
        return valid;
    }

    private List<Vector2Int> GetCurrentlyBurningTiles()
    {
        List<Vector2Int> burning = new List<Vector2Int>();
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (flammables[x, y] != null && flammables[x, y].IsBurning)
                {
                    burning.Add(new Vector2Int(x, y));
                }
            }
        }
        return burning;
    }

    private int CountBurningTiles()
    {
        int count = 0;
        if (flammables == null) return 0;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (flammables[x, y] != null && flammables[x, y].IsBurning)
                {
                    count++;
                }
            }
        }
        return count;
    }

    private bool IsInsideGrid(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }

    #endregion
}