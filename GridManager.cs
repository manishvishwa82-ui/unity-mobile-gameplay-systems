using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager instance;

    public static Action<int> OnScoreUpdated;
    public static Action<int> OnLinesCleared;
    public static Action OnGameOver;

    [Header("Grid Dimensions")]
    public int columns = 8;
    public int rows = 8;
    public float cellSize = 0.68f;
    [Tooltip("Booster Bar se gap banane ke liye offset")]
    public float gridYOffset = 1.35f;

    [Header("Prefabs & Colors")]
    public GameObject cellPrefab;
    public Color emptyColor = new Color(0.08f, 0.12f, 0.22f, 1f);

    [Header("VFX Particle Prefab")]
    public GameObject lineClearVFXPrefab;

    [Header("Preset Puzzle Block Colors")]
    public Color[] presetColors = new Color[]
    {
        new Color(0.2f, 0.7f, 1f),    // Blue
        new Color(1f, 0.6f, 0.2f),    // Orange
        new Color(0.3f, 0.85f, 0.4f),  // Green
        new Color(0.9f, 0.3f, 0.5f),  // Pink
        new Color(0.95f, 0.8f, 0.2f)  // Yellow
    };

    [Header("Score & Streak")]
    public int currentScore = 0;
    public int currentStreak = 0;

    private GridCell[,] gridCells;
    private List<GridCell> glowingPreClearCells = new List<GridCell>();

    void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        GenerateGrid();
        SpawnPresetPuzzleLayout();
        OnScoreUpdated?.Invoke(currentScore);
    }

    public void GenerateGrid()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        gridCells = new GridCell[columns, rows];

        float startX = -((columns - 1) * cellSize) / 2f;
        float startY = -((rows - 1) * cellSize) / 2f + gridYOffset;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 pos = new Vector3(startX + (x * cellSize), startY + (y * cellSize), 0f);
                GameObject obj = Instantiate(cellPrefab, pos, Quaternion.identity, transform);
                GridCell cell = obj.GetComponent<GridCell>();

                if (cell == null)
                {
                    cell = obj.AddComponent<GridCell>();
                }

                cell.Init(x, y, emptyColor);
                gridCells[x, y] = cell;
            }
        }
    }

    // Level Mode ke liye predefined puzzle setup
    public void SpawnPresetPuzzleLayout()
    {
        bool isClassic = PlayerPrefs.GetInt("IsClassicMode", 0) == 1;
        if (isClassic) return; // Classic mode hamesha empty board se start hoga

        int level = PlayerPrefs.GetInt("CurrentPlayingLevel", 1);
        List<Vector2Int> presetCoords = GetLayoutCoordinatesForLevel(level);

        if (presetCoords == null || presetCoords.Count == 0) return;

        for (int i = 0; i < presetCoords.Count; i++)
        {
            Vector2Int coord = presetCoords[i];
            if (IsValidGridPos(coord.x, coord.y))
            {
                Color blockColor = presetColors[i % presetColors.Length];
                gridCells[coord.x, coord.y].OccupyWithShape(null, blockColor);
            }
        }
    }

    private List<Vector2Int> GetLayoutCoordinatesForLevel(int level)
    {
        List<Vector2Int> coords = new List<Vector2Int>();

        switch (level)
        {
            case 1:
                // Level 1: Bottom row almost full (7 out of 8) -> 1 piece drop karte hi instant line blast!
                for (int x = 0; x < 7; x++) coords.Add(new Vector2Int(x, 0));
                break;

            case 2:
                // Level 2: 2 corners occupied
                coords.Add(new Vector2Int(0, 0));
                coords.Add(new Vector2Int(1, 0));
                coords.Add(new Vector2Int(0, 1));
                coords.Add(new Vector2Int(7, 7));
                coords.Add(new Vector2Int(6, 7));
                coords.Add(new Vector2Int(7, 6));
                break;

            case 3:
                // Level 3: Center Cross layout
                coords.Add(new Vector2Int(3, 3));
                coords.Add(new Vector2Int(4, 3));
                coords.Add(new Vector2Int(3, 4));
                coords.Add(new Vector2Int(4, 4));
                coords.Add(new Vector2Int(3, 2));
                coords.Add(new Vector2Int(4, 5));
                break;

            case 4:
                // Level 4: Left & Right vertical walls (near complete)
                for (int y = 1; y < 6; y++)
                {
                    coords.Add(new Vector2Int(1, y));
                    coords.Add(new Vector2Int(6, y));
                }
                break;

            case 5:
                // Level 5: Diagonal Stair Pattern
                for (int i = 1; i <= 6; i++)
                {
                    coords.Add(new Vector2Int(i, i));
                }
                break;

            default:
                // Level 6 to 20: Procedural symmetrical puzzle patterns
                int seedOffset = (level % 4);
                if (seedOffset == 0)
                {
                    // Center box cluster
                    for (int x = 2; x <= 5; x++)
                    {
                        coords.Add(new Vector2Int(x, 2));
                        coords.Add(new Vector2Int(x, 5));
                    }
                }
                else if (seedOffset == 1)
                {
                    // 4-corner diamond dots
                    coords.Add(new Vector2Int(2, 2));
                    coords.Add(new Vector2Int(5, 2));
                    coords.Add(new Vector2Int(2, 5));
                    coords.Add(new Vector2Int(5, 5));
                    coords.Add(new Vector2Int(3, 3));
                    coords.Add(new Vector2Int(4, 4));
                }
                else if (seedOffset == 2)
                {
                    // Dual mini-cross
                    coords.Add(new Vector2Int(2, 4));
                    coords.Add(new Vector2Int(3, 4));
                    coords.Add(new Vector2Int(4, 3));
                    coords.Add(new Vector2Int(5, 3));
                }
                else
                {
                    // Checkerboard clusters
                    coords.Add(new Vector2Int(1, 1));
                    coords.Add(new Vector2Int(3, 1));
                    coords.Add(new Vector2Int(5, 1));
                    coords.Add(new Vector2Int(2, 6));
                    coords.Add(new Vector2Int(4, 6));
                    coords.Add(new Vector2Int(6, 6));
                }
                break;
        }

        return coords;
    }

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        float startX = -((columns - 1) * cellSize) / 2f;
        float startY = -((rows - 1) * cellSize) / 2f + gridYOffset;

        int x = Mathf.RoundToInt((worldPos.x - startX) / cellSize);
        int y = Mathf.RoundToInt((worldPos.y - startY) / cellSize);

        return new Vector2Int(x, y);
    }

    public bool IsValidGridPos(int x, int y)
    {
        return (x >= 0 && x < columns && y >= 0 && y < rows);
    }

    private Vector2 GetShapeCenterOffset(BlockShape shape)
    {
        if (shape == null || shape.relativePositions == null || shape.relativePositions.Count == 0)
            return Vector2.zero;

        float sumX = 0f;
        float sumY = 0f;

        foreach (Vector2Int pos in shape.relativePositions)
        {
            sumX += pos.x;
            sumY += pos.y;
        }

        return new Vector2(sumX / shape.relativePositions.Count, sumY / shape.relativePositions.Count);
    }

    public void ShowPreview(BlockShape shape, Vector3 worldPos)
    {
        ClearPreview();

        Vector2 centerOffset = GetShapeCenterOffset(shape);
        Vector3 adjustedWorldPos = worldPos - new Vector3(centerOffset.x * cellSize, centerOffset.y * cellSize, 0f);

        Vector2Int baseGrid = WorldToGrid(adjustedWorldPos);

        List<Vector2Int> targetCells = new List<Vector2Int>();
        foreach (Vector2Int offset in shape.relativePositions)
        {
            int checkX = baseGrid.x + offset.x;
            int checkY = baseGrid.y + offset.y;

            if (!IsValidGridPos(checkX, checkY) || gridCells[checkX, checkY].isOccupied)
            {
                return;
            }
            targetCells.Add(new Vector2Int(checkX, checkY));
        }

        Color previewCol = shape.GetCurrentColor();
        foreach (Vector2Int pos in targetCells)
        {
            gridCells[pos.x, pos.y].SetHighlight(true, previewCol);
        }

        CheckAndHighlightPotentialLines(targetCells);
    }

    private void CheckAndHighlightPotentialLines(List<Vector2Int> simulatedPositions)
    {
        HashSet<int> potentialRows = new HashSet<int>();
        HashSet<int> potentialCols = new HashSet<int>();

        for (int y = 0; y < rows; y++)
        {
            bool willComplete = true;
            for (int x = 0; x < columns; x++)
            {
                bool cellWillBeOccupied = gridCells[x, y].isOccupied || simulatedPositions.Contains(new Vector2Int(x, y));
                if (!cellWillBeOccupied)
                {
                    willComplete = false;
                    break;
                }
            }
            if (willComplete) potentialRows.Add(y);
        }

        for (int x = 0; x < columns; x++)
        {
            bool willComplete = true;
            for (int y = 0; y < rows; y++)
            {
                bool cellWillBeOccupied = gridCells[x, y].isOccupied || simulatedPositions.Contains(new Vector2Int(x, y));
                if (!cellWillBeOccupied)
                {
                    willComplete = false;
                    break;
                }
            }
            if (willComplete) potentialCols.Add(x);
        }

        foreach (int r in potentialRows)
        {
            for (int c = 0; c < columns; c++)
            {
                if (gridCells[c, r].isOccupied && !glowingPreClearCells.Contains(gridCells[c, r]))
                {
                    gridCells[c, r].SetPreClearGlow(true);
                    glowingPreClearCells.Add(gridCells[c, r]);
                }
            }
        }

        foreach (int c in potentialCols)
        {
            for (int r = 0; r < rows; r++)
            {
                if (gridCells[c, r].isOccupied && !glowingPreClearCells.Contains(gridCells[c, r]))
                {
                    gridCells[c, r].SetPreClearGlow(true);
                    glowingPreClearCells.Add(gridCells[c, r]);
                }
            }
        }
    }

    public void ClearPreview()
    {
        if (gridCells == null) return;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (gridCells[x, y] != null && !gridCells[x, y].isOccupied)
                {
                    gridCells[x, y].SetHighlight(false, Color.white);
                }
            }
        }

        foreach (GridCell cell in glowingPreClearCells)
        {
            if (cell != null) cell.SetPreClearGlow(false);
        }
        glowingPreClearCells.Clear();
    }

    public bool TryPlaceShape(BlockShape shape, Vector3 worldPos)
    {
        Vector2 centerOffset = GetShapeCenterOffset(shape);
        Vector3 adjustedWorldPos = worldPos - new Vector3(centerOffset.x * cellSize, centerOffset.y * cellSize, 0f);

        Vector2Int baseGrid = WorldToGrid(adjustedWorldPos);

        List<Vector2Int> placedPositions = new List<Vector2Int>();
        foreach (Vector2Int offset in shape.relativePositions)
        {
            int checkX = baseGrid.x + offset.x;
            int checkY = baseGrid.y + offset.y;

            if (!IsValidGridPos(checkX, checkY) || gridCells[checkX, checkY].isOccupied)
            {
                return false;
            }
            placedPositions.Add(new Vector2Int(checkX, checkY));
        }

        Material matToPass = shape.GetShapeMaterial();
        Color colToPass = shape.GetCurrentColor();

        foreach (Vector2Int pos in placedPositions)
        {
            gridCells[pos.x, pos.y].OccupyWithShape(matToPass, colToPass);
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayPlace();
        }

        TriggerPlacementWave(placedPositions);

        AddScore(shape.relativePositions.Count * 10);
        CheckAndClearLines();

        if (LevelTargetManager.instance != null)
        {
            LevelTargetManager.instance.OnPlayerMoveUsed();
        }

        if (BoosterController.instance != null)
        {
            BoosterController.instance.OnShapePlacedForDoubleScore();
        }

        return true;
    }

    private void TriggerPlacementWave(List<Vector2Int> placedPositions)
    {
        if (placedPositions == null || placedPositions.Count == 0) return;

        Vector2 center = Vector2.zero;
        foreach (var p in placedPositions) center += new Vector2(p.x, p.y);
        center /= placedPositions.Count;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (placedPositions.Contains(new Vector2Int(x, y))) continue;

                float dist = Vector2.Distance(center, new Vector2(x, y));
                if (dist <= 2.6f && gridCells[x, y] != null)
                {
                    float delay = dist * 0.022f;
                    gridCells[x, y].TriggerImpactBounce(delay);
                }
            }
        }
    }

    public void AddScore(int amount)
    {
        if (BoosterController.instance != null && BoosterController.instance.isDoubleScoreActive)
        {
            amount *= 2;
        }

        currentScore += amount;
        OnScoreUpdated?.Invoke(currentScore);

        if (LevelTargetManager.instance != null)
        {
            LevelTargetManager.instance.AddScore(amount);
        }
        else if (GameProgressManager.instance != null)
        {
            GameProgressManager.instance.AddScore(amount);
        }
    }

    void CheckAndClearLines()
    {
        List<int> fullRows = new List<int>();
        List<int> fullColumns = new List<int>();

        for (int y = 0; y < rows; y++)
        {
            bool isRowFull = true;
            for (int x = 0; x < columns; x++)
            {
                if (gridCells[x, y] == null || !gridCells[x, y].isOccupied)
                {
                    isRowFull = false;
                    break;
                }
            }
            if (isRowFull) fullRows.Add(y);
        }

        for (int x = 0; x < columns; x++)
        {
            bool isColFull = true;
            for (int y = 0; y < rows; y++)
            {
                if (gridCells[x, y] == null || !gridCells[x, y].isOccupied)
                {
                    isColFull = false;
                    break;
                }
            }
            if (isColFull) fullColumns.Add(x);
        }

        int clearedLines = fullRows.Count + fullColumns.Count;

        if (clearedLines > 0)
        {
            currentStreak++;

            HashSet<GridCell> cellsToClear = new HashSet<GridCell>();

            foreach (int y in fullRows)
            {
                for (int x = 0; x < columns; x++)
                {
                    if (gridCells[x, y] != null) cellsToClear.Add(gridCells[x, y]);
                }
            }

            foreach (int x in fullColumns)
            {
                for (int y = 0; y < rows; y++)
                {
                    if (gridCells[x, y] != null) cellsToClear.Add(gridCells[x, y]);
                }
            }

            int lineBonus = clearedLines * 120 * clearedLines;
            int streakBonus = (currentStreak > 1) ? (currentStreak * 150) : 0;
            int totalBonus = lineBonus + streakBonus;
            AddScore(totalBonus);

            Vector3 scoreSpawnPos = Vector3.zero;
            if (cellsToClear.Count > 0)
            {
                foreach (var c in cellsToClear) scoreSpawnPos += c.transform.position;
                scoreSpawnPos /= cellsToClear.Count;
                scoreSpawnPos.y -= 0.15f;
            }
            else
            {
                scoreSpawnPos = transform.position + new Vector3(0, gridYOffset, 0);
            }

            FloatingScore.Spawn(scoreSpawnPos, totalBonus, currentStreak > 1);

            if (ComboTextManager.instance != null)
            {
                ComboTextManager.instance.TriggerCombo(clearedLines, currentStreak);
            }

            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlayLineClearWithStreak(clearedLines, currentStreak);
            }

            if (CameraShake.instance != null)
            {
                float shakeDuration = Mathf.Clamp(0.12f + (clearedLines * 0.04f), 0.12f, 0.35f);
                float shakeStrength = Mathf.Clamp(0.08f + (clearedLines * 0.05f), 0.08f, 0.28f);
                CameraShake.instance.Shake(shakeDuration, shakeStrength);
            }

            OnLinesCleared?.Invoke(clearedLines);
            StartCoroutine(ClearWaveAnimation(cellsToClear));
        }
        else
        {
            currentStreak = 0;
            UpdateDangerState();
        }
    }

    IEnumerator ClearWaveAnimation(HashSet<GridCell> cells)
    {
        // 1. Pre-flash all clearing lines
        foreach (GridCell cell in cells)
        {
            if (cell != null) cell.SetHighlight(true, Color.white);
        }

        yield return new WaitForSeconds(0.035f);

        Vector3 gridCenter = transform.position + new Vector3(0, gridYOffset, 0);
        List<GridCell> sortedCells = cells.OrderBy(c => Vector3.Distance(c.transform.position, gridCenter)).ToList();

        // 2. Snappy ripple pop & debris burst
        foreach (GridCell cell in sortedCells)
        {
            if (cell != null)
            {
                Color blockColor = cell.occupiedColor;
                SpawnVFX(cell.transform.position, blockColor);
                SpawnCubeDebris(cell.transform.position, blockColor);
                cell.ClearCell();
                yield return new WaitForSeconds(0.016f);
            }
        }

        yield return new WaitForSeconds(0.09f);

        CheckPerfectClear();
        UpdateDangerState();

        if (ShapeSpawner.instance != null)
        {
            ShapeSpawner.instance.CheckGameOverState();
        }
    }

    private void SpawnCubeDebris(Vector3 pos, Color col)
    {
        int debrisCount = 4;
        for (int i = 0; i < debrisCount; i++)
        {
            GameObject debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
            debris.transform.position = pos + new Vector3(UnityEngine.Random.Range(-0.1f, 0.1f), UnityEngine.Random.Range(-0.1f, 0.1f), -0.5f);
            debris.transform.localScale = Vector3.one * (cellSize * 0.24f);

            Collider colComponent = debris.GetComponent<Collider>();
            if (colComponent != null) Destroy(colComponent);

            Renderer r = debris.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(Shader.Find("Sprites/Default"));
                r.material.color = col;
            }

            Vector2 randomDir = UnityEngine.Random.insideUnitCircle.normalized;
            StartCoroutine(AnimateDebris(debris, randomDir));
        }
    }

    private IEnumerator AnimateDebris(GameObject debris, Vector2 dir)
    {
        float duration = 0.32f;
        float elapsed = 0f;
        Vector3 startPos = debris.transform.position;
        Vector3 endPos = startPos + new Vector3(dir.x * 0.45f, dir.y * 0.45f - 0.2f, 0f);
        Vector3 startScale = debris.transform.localScale;

        while (elapsed < duration)
        {
            if (debris == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            debris.transform.position = Vector3.Lerp(startPos, endPos, t);
            debris.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t * t);
            debris.transform.Rotate(0, 0, 180f * Time.deltaTime * 3f);

            yield return null;
        }

        if (debris != null) Destroy(debris);
    }

    private void CheckPerfectClear()
    {
        if (gridCells == null) return;

        bool hasAnyOccupied = false;
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (gridCells[x, y] != null && gridCells[x, y].isOccupied)
                {
                    hasAnyOccupied = true;
                    break;
                }
            }
            if (hasAnyOccupied) break;
        }

        if (!hasAnyOccupied)
        {
            AddScore(1000);
            Vector3 centerPos = transform.position + new Vector3(0, gridYOffset - 0.5f, 0);
            FloatingScore.Spawn(centerPos, 1000, true);

            if (ComboTextManager.instance != null)
            {
                ComboTextManager.instance.TriggerPerfectClear();
            }

            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlayLineClearWithStreak(4, 5);
            }
        }
    }

    private void UpdateDangerState()
    {
        bool inDanger = IsBoardInDanger();
        if (ComboTextManager.instance != null)
        {
            ComboTextManager.instance.SetDangerState(inDanger);
        }
    }

    void SpawnVFX(Vector3 pos, Color particleColor)
    {
        if (lineClearVFXPrefab != null)
        {
            Vector3 spawnPos = new Vector3(pos.x, pos.y, -2f);
            GameObject fx = Instantiate(lineClearVFXPrefab, spawnPos, Quaternion.identity);

            ParticleSystem ps = fx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = particleColor;
            }

            Destroy(fx, 1.2f);
        }
    }

    public bool CheckIfShapeCanFitAnywhere(List<Vector2Int> relativePositions)
    {
        if (gridCells == null || relativePositions == null || relativePositions.Count == 0) return true;

        for (int startX = 0; startX < columns; startX++)
        {
            for (int startY = 0; startY < rows; startY++)
            {
                bool canFit = true;
                foreach (Vector2Int offset in relativePositions)
                {
                    int checkX = startX + offset.x;
                    int checkY = startY + offset.y;

                    if (!IsValidGridPos(checkX, checkY) || gridCells[checkX, checkY] == null || gridCells[checkX, checkY].isOccupied)
                    {
                        canFit = false;
                        break;
                    }
                }
                if (canFit) return true;
            }
        }
        return false;
    }

    public bool IsBoardInDanger()
    {
        if (gridCells == null) return false;

        int occupiedCount = 0;
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (gridCells[x, y] != null && gridCells[x, y].isOccupied)
                {
                    occupiedCount++;
                }
            }
        }
        return occupiedCount >= 48;
    }

    public void ClearAreaInstant(int startX, int startY, int width, int height)
    {
        for (int x = startX; x < startX + width; x++)
        {
            for (int y = startY; y < startY + height; y++)
            {
                if (IsValidGridPos(x, y) && gridCells[x, y] != null && gridCells[x, y].isOccupied)
                {
                    gridCells[x, y].ClearCellInstant();
                }
            }
        }

        if (CameraShake.instance != null) CameraShake.instance.Shake(0.15f, 0.1f);
        if (AudioManager.instance != null) AudioManager.instance.PlayPlace();

        UpdateDangerState();
    }

    public void ClearSingleRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= rows) return;
        HashSet<GridCell> cells = new HashSet<GridCell>();
        for (int x = 0; x < columns; x++)
        {
            if (gridCells[x, rowIndex] != null && gridCells[x, rowIndex].isOccupied)
                cells.Add(gridCells[x, rowIndex]);
        }
        if (cells.Count > 0)
        {
            int earned = cells.Count * 25;
            AddScore(earned);
            FloatingScore.Spawn(new Vector3(0, -((rows - 1) * cellSize) / 2f + gridYOffset + (rowIndex * cellSize), 0), earned);

            if (AudioManager.instance != null) AudioManager.instance.PlayLineClearWithStreak(1, 1);
            if (CameraShake.instance != null) CameraShake.instance.Shake(0.14f, 0.1f);

            StartCoroutine(ClearWaveAnimation(cells));
        }
    }

    public void ClearSingleCell(int targetX, int targetY)
    {
        if (IsValidGridPos(targetX, targetY) && gridCells[targetX, targetY] != null && gridCells[targetX, targetY].isOccupied)
        {
            HashSet<GridCell> cell = new HashSet<GridCell> { gridCells[targetX, targetY] };
            AddScore(30);
            FloatingScore.Spawn(gridCells[targetX, targetY].transform.position, 30);

            if (AudioManager.instance != null) AudioManager.instance.PlayPlace();
            if (CameraShake.instance != null) CameraShake.instance.Shake(0.1f, 0.08f);

            StartCoroutine(ClearWaveAnimation(cell));
        }
    }
}