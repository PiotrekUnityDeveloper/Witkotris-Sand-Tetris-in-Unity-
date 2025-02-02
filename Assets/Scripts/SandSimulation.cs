using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using static SandSimulation;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class SandSimulation : MonoBehaviour
{
    public static SandSimulation Instance { get; set; }
    public readonly System.Random globalRandom = new System.Random();


    public enum ChunkGenerationMode
    {
        TilemapOutline,
        TilemapRooms,
        Independent // not implemented yet
    }

    public enum ChunkUpdateMode
    {
        Sequential,
        FourSample,
    }

    [Header("Simulation Settings")]
    public bool initOnStart = false;
    public bool isSimPaused = false;
    public float updateInterval = 0.1f; //how often to update (in seconds)
    public float gravity = 0.2f;
    public int chunkSize = 16;
    public ChunkGenerationMode chunkGenerationMode = ChunkGenerationMode.TilemapOutline;
    public ChunkUpdateMode chunkUpdateMode = ChunkUpdateMode.Sequential;

    [Header("Interaction Settings")]
    public bool enableInteraction = false;
    public string selectedElement = "sand";
    [Range(0, 15)] public float drawingBrushSize = 0;
    public bool interactionUpdatesEnvironment = true;

    [Header("References")]
    public GameManager gameManager;
    public SimulationRenderer simulationRenderer;
    public Tilemap collisionTilemap;
    public Tilemap boundaryTilemap;
    [HideInInspector] public Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();
    [HideInInspector] public List<SimulationObject> simObjects = new List<SimulationObject>();

    [Header("Engine Excluded")]
    public int defaultFallSpeed = 1;
    public int fastForwardSpeed = 2;
    public int defaultFallDrag = 3;
    [HideInInspector] public bool isFastForwarding = false;
    [HideInInspector] public int fallSpeed = 1;
    public int horizontalSpeed = 2;

    [Header("Debug")]
    public bool drawDebug = false;
    public bool renderChunkBorders = true;
    public bool showActiveChunks = true;
    public bool renderElements = false;

    private void Awake()
    {
        Instance = this;
        //this.chunkSize = SettingsSaver.chunkSize;
    }

    void Start()
    {
        fallSpeed = defaultFallSpeed;
        if(initOnStart) InitSimulation();
        //if (initOnStart) CheckForClearLine();
    }

    public void ManualInit()
    {
        fallSpeed = defaultFallSpeed;
        InitSimulation();
    }

    private float timeSinceLastUpdate = 0f;
    void Update()
    {
        if (!isSimPaused)
        {
            timeSinceLastUpdate += Time.deltaTime;

            if (timeSinceLastUpdate >= updateInterval)
            {
                UpdateChunks();
                timeSinceLastUpdate = 0f;
            }
        }

        if (enableInteraction && Input.GetMouseButton(0))
        {
            CreateElementsAtMousePos();
        }

        if (enableInteraction && Input.GetMouseButton(1))
        {
            DiscardElementsAtMousePos();
        }

        if (enableInteraction && Input.GetMouseButtonDown(2))
        {
            EvacutableElementsAtMousePos();
        }

        if (enableInteraction && Input.GetMouseButtonDown(3))
        {
            //CreateSimObject();
        }

        if(enableInteraction && Input.GetKeyDown(KeyCode.R))
        {
            //RotateSimObject();
        }
    }

    public void CheckForClearLine()
    {
        StartCoroutine(CheckForClearLineLoop());
    }

    /*
    public Color[] targetColorData => new Color[]
        {
            new Color(0.94f, 0.85f, 0.53f, 1f), // Light Beige
            new Color(0.87f, 0.76f, 0.46f, 1f), // Soft Yellow Sand
            new Color(0.91f, 0.77f, 0.47f, 1f), // Light Brown Sand
            new Color(0.81f, 0.67f, 0.35f, 1f), // Desert Sand
            new Color(0.74f, 0.62f, 0.37f, 1f), // Medium Sand
            new Color(0.85f, 0.72f, 0.48f, 1f), // Warm Sand
            new Color(0.78f, 0.61f, 0.39f, 1f), // Brownish Sand
            new Color(0.92f, 0.80f, 0.47f, 1f)  // Pale Sandy Yellow
        };

    public Color[] blueColorData => new Color[]
    {
        new Color(0.1261561f, 0.391388f, 0.8627451f, 1f),
        new Color(0.1359045f, 0.1359045f, 0.9294118f, 1f)
    };
    */

    public List<Color[]> colorsToCheck = new List<Color[]>();
    public List<string> typesToCheck = new List<string>();
    public bool checkColorsOnly = true;

    public IEnumerator CheckForClearLineLoop()
    {
        ///HashSet<(Vector2Int, Vector2Int)> foundSandElements = new HashSet<(Vector2Int, Vector2Int)>();
        ///bool isConnectedSand = EdgeConnectionChecker.CheckTypeConnectsBorders<Sand>(chunks, out foundSandElements, targetColorData);
        ///if(isConnectedSand) print("connected sand!");

        ///HashSet<(Vector2Int, Vector2Int)> foundBlueElements = new HashSet<(Vector2Int, Vector2Int)>();
        ///bool isConnectedBlueSand = EdgeConnectionChecker.CheckTypeConnectsBorders<Sand>(chunks, out foundBlueElements, blueColorData);
        ///if (isConnectedBlueSand) print("connected bluesand!");

        if (!checkColorsOnly)
        {
            foreach (string etype in typesToCheck)
            {
                if (etype == "sand")
                {
                    foreach (Color[] colorsRem in colorsToCheck)
                    {
                        HashSet<(Vector2Int, Vector2Int)> foundElements = new HashSet<(Vector2Int, Vector2Int)>();
                        bool isConnectedSand = EdgeConnectionChecker.CheckTypeConnectsBorders<Sand>(chunks, out foundElements, colorsRem);
                        if (isConnectedSand)
                        {
                            Debug.Log("found a connection!");
                            ClearLine(foundElements);
                        }
                    }
                }
                else if (etype == "water")
                {
                    foreach (Color[] colorsRem in colorsToCheck)
                    {
                        HashSet<(Vector2Int, Vector2Int)> foundElements = new HashSet<(Vector2Int, Vector2Int)>();
                        bool isConnectedSand = EdgeConnectionChecker.CheckTypeConnectsBorders<Water>(chunks, out foundElements, colorsRem);
                        if (isConnectedSand)
                        {
                            Debug.Log("found a connection!");
                            ClearLine(foundElements);
                        }
                    }
                }
                else if (etype == "sawdust")
                {
                    foreach (Color[] colorsRem in colorsToCheck)
                    {
                        HashSet<(Vector2Int, Vector2Int)> foundElements = new HashSet<(Vector2Int, Vector2Int)>();
                        bool isConnectedSand = EdgeConnectionChecker.CheckTypeConnectsBorders<SawDust>(chunks, out foundElements, colorsRem);
                        if (isConnectedSand)
                        {
                            Debug.Log("found a connection!");
                            ClearLine(foundElements);
                        }
                    }
                }
                else if (etype == "flour")
                {
                    foreach (Color[] colorsRem in colorsToCheck)
                    {
                        HashSet<(Vector2Int, Vector2Int)> foundElements = new HashSet<(Vector2Int, Vector2Int)>();
                        bool isConnectedSand = EdgeConnectionChecker.CheckTypeConnectsBorders<Flour>(chunks, out foundElements, colorsRem);
                        if (isConnectedSand)
                        {
                            Debug.Log("found a connection!");
                            ClearLine(foundElements);
                        }
                    }
                }
                else if (etype == "bricks")
                {
                    foreach (Color[] colorsRem in colorsToCheck)
                    {
                        HashSet<(Vector2Int, Vector2Int)> foundElements = new HashSet<(Vector2Int, Vector2Int)>();
                        bool isConnectedSand = EdgeConnectionChecker.CheckTypeConnectsBorders<Bricks>(chunks, out foundElements, colorsRem);
                        if (isConnectedSand)
                        {
                            Debug.Log("found a connection!");
                            ClearLine(foundElements);
                        }
                    }
                }
            }
        }
        else
        {
            foreach (Color[] colorsRem in colorsToCheck)
            {
                HashSet<(Vector2Int, Vector2Int)> foundElements = new HashSet<(Vector2Int, Vector2Int)>();
                bool isConnectedSand = ColorEdgeConnectionChecker.CheckColorConnectsBorders(chunks, out foundElements, colorsRem);
                if (isConnectedSand)
                {
                    Debug.Log("found a connection!");
                    ClearLine(foundElements);
                }
            }
        }

        // HashSet<(Vector2Int, Vector2Int)> combined = new HashSet<(Vector2Int, Vector2Int)>(foundSandElements.Union(foundBlueElements));
        //ClearLine(combined);

        yield return new WaitForSecondsRealtime(2f);
        //gameManager.lineChecker = null;
        gameManager.lineChecker = StartCoroutine(CheckForClearLineLoop());
    }

    public void ClearLine(HashSet<(Vector2Int, Vector2Int)> elementsToRemove)
    {
        this.isSimPaused = true;

        /*
        foreach (var (chunkPos, localPos) in elementsToRemove)
        {
            Chunk chunk = chunks[chunkPos];
            chunk.elements[localPos.x, localPos.y].LocalColor = Color.white;
        }*/

        foreach (var (chunkPos, localPos) in elementsToRemove)
        {
            Chunk chunk = chunks[chunkPos];
            chunk.elements[localPos.x, localPos.y] = null;
            chunk.isActive = true;
            gameManager.AddScore(1);
            
        }

        gameManager.CountClear();

        this.isSimPaused = false;
    }


    // INPUT LOGIC

    public void CreateTempElement(Vector2 pos, out Element TempElem)
    {
        string elementName = "tempelement";

        TempElem = null;

        Vector2 worldPos = pos;

        int checkRadius = Mathf.CeilToInt(drawingBrushSize);
        for (int xOffset = -checkRadius; xOffset <= checkRadius; xOffset++)
        {
            for (int yOffset = -checkRadius; yOffset <= checkRadius; yOffset++)
            {
                Vector2 checkPos = new Vector2(
                    worldPos.x + xOffset / (float)chunkSize,
                    worldPos.y + yOffset / (float)chunkSize
                );

                float distance = Vector2.Distance(worldPos, checkPos);
                if (distance > drawingBrushSize / (float)chunkSize) continue;

                Vector2Int cellPos = new Vector2Int(
                    Mathf.FloorToInt(checkPos.x),
                    Mathf.FloorToInt(checkPos.y)
                );

                Vector2Int chunkPos = new Vector2Int(
                    Mathf.FloorToInt(cellPos.x),
                    Mathf.FloorToInt(cellPos.y)
                );

                if (!chunks.ContainsKey(chunkPos))
                {
                    continue;
                }

                Vector2Int localPos = new Vector2Int(
                    Mathf.RoundToInt((checkPos.x - chunkPos.x) * chunkSize),
                    Mathf.RoundToInt((checkPos.y - chunkPos.y) * chunkSize)
                );

                localPos.x = Mathf.Clamp(localPos.x, 0, chunkSize - 1);
                localPos.y = Mathf.Clamp(localPos.y, 0, chunkSize - 1);

                Vector3Int tilePosition = new Vector3Int(
                    localPos.x / chunkSize + chunkPos.x,
                    localPos.y / chunkSize + chunkPos.y,
                    0
                );

                if (collisionTilemap.HasTile(tilePosition))
                {
                    continue;
                }

                if (chunks[chunkPos].elements[localPos.x, localPos.y] == null)
                {
                    Element tempElement = null;
                    chunks[chunkPos].AddElement(localPos, out tempElement, elementName);
                    TempElem = tempElement;
                    if (tempElement != null) StartCoroutine(KillTempElement(tempElement));
                    if (interactionUpdatesEnvironment) chunks[chunkPos].WakeUpParticleNeighbors(localPos, chunkPos);
                }
            }
        }
    }

    public IEnumerator KillTempElement(Element e)
    {
        if(e != null)
        {
            if (e.GetType() == typeof(TemporalPowder))
            {
                yield return new WaitForSecondsRealtime(25f);
                (e as TemporalPowder).Die();
                //print("dead!");
            }
        }
    }

    private void CreateElementsAtMousePos()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        int checkRadius = Mathf.CeilToInt(drawingBrushSize);
        for (int xOffset = -checkRadius; xOffset <= checkRadius; xOffset++)
        {
            for (int yOffset = -checkRadius; yOffset <= checkRadius; yOffset++)
            {
                Vector2 checkPos = new Vector2(
                    mouseWorldPos.x + xOffset / (float)chunkSize,
                    mouseWorldPos.y + yOffset / (float)chunkSize
                );

                float distance = Vector2.Distance(mouseWorldPos, checkPos);
                if (distance > drawingBrushSize / (float)chunkSize) continue;

                Vector2Int cellPos = new Vector2Int(
                    Mathf.FloorToInt(checkPos.x),
                    Mathf.FloorToInt(checkPos.y)
                );

                Vector2Int chunkPos = new Vector2Int(
                    Mathf.FloorToInt(cellPos.x),
                    Mathf.FloorToInt(cellPos.y)
                );

                if (!chunks.ContainsKey(chunkPos))
                {
                    continue;
                }

                Vector2Int localPos = new Vector2Int(
                    Mathf.RoundToInt((checkPos.x - chunkPos.x) * chunkSize),
                    Mathf.RoundToInt((checkPos.y - chunkPos.y) * chunkSize)
                );

                localPos.x = Mathf.Clamp(localPos.x, 0, chunkSize - 1);
                localPos.y = Mathf.Clamp(localPos.y, 0, chunkSize - 1);

                Vector3Int tilePosition = new Vector3Int(
                    localPos.x / chunkSize + chunkPos.x,
                    localPos.y / chunkSize + chunkPos.y,
                    0
                );

                if (collisionTilemap.HasTile(tilePosition))
                {
                    continue;
                }

                if (chunks[chunkPos].elements[localPos.x, localPos.y] == null)
                {
                    chunks[chunkPos].AddElement(localPos, selectedElement);
                    if(interactionUpdatesEnvironment) chunks[chunkPos].WakeUpParticleNeighbors(localPos, chunkPos);
                }
            }
        }
    }

    private void DiscardElementsAtMousePos()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        int checkRadius = Mathf.CeilToInt(drawingBrushSize);
        for (int xOffset = -checkRadius; xOffset <= checkRadius; xOffset++)
        {
            for (int yOffset = -checkRadius; yOffset <= checkRadius; yOffset++)
            {
                Vector2 checkPos = new Vector2(
                    mouseWorldPos.x + xOffset / (float)chunkSize,
                    mouseWorldPos.y + yOffset / (float)chunkSize
                );

                float distance = Vector2.Distance(mouseWorldPos, checkPos);
                if (distance > drawingBrushSize / (float)chunkSize) continue;

                Vector2Int cellPos = new Vector2Int(
                    Mathf.FloorToInt(checkPos.x),
                    Mathf.FloorToInt(checkPos.y)
                );

                Vector2Int chunkPos = new Vector2Int(
                    Mathf.FloorToInt(cellPos.x),
                    Mathf.FloorToInt(cellPos.y)
                );

                Vector2Int localPos = new Vector2Int(
                    Mathf.RoundToInt((checkPos.x - chunkPos.x) * chunkSize),
                    Mathf.RoundToInt((checkPos.y - chunkPos.y) * chunkSize)
                );

                localPos.x = Mathf.Clamp(localPos.x, 0, chunkSize - 1);
                localPos.y = Mathf.Clamp(localPos.y, 0, chunkSize - 1);

                if (chunks.ContainsKey(chunkPos))
                {
                    Chunk chunk = chunks[chunkPos];
                    if (chunk.elements[localPos.x, localPos.y] != null)
                    {
                        chunk.WakeUpParticleNeighbors(localPos, chunkPos); //reqiured
                        if(interactionUpdatesEnvironment) chunk.WakeUpAllChunkNeighbors(true);
                        if (chunk.elements[localPos.x, localPos.y].containedInObject == false)
                        {
                            chunk.elements[localPos.x, localPos.y] = null;
                        }
                        else
                        {
                            chunk.elements[localPos.x, localPos.y].containingObject.containedElements.Remove(chunk.elements[localPos.x, localPos.y]);
                            chunk.elements[localPos.x, localPos.y].containedInObject = false;
                            chunk.elements[localPos.x, localPos.y] = null;

                            

                            //or granularize it if you want to...
                        }

                        // Check if the chunk is now empty
                        bool hasParticles = false;
                        for (int x = 0; x < chunk.chunkSize; x++)
                        {
                            for (int y = 0; y < chunk.chunkSize; y++)
                            {
                                if (chunk.elements[x, y] != null)
                                {
                                    hasParticles = true;
                                    break;
                                }
                            }
                            if (hasParticles) break;
                        }
                        chunk.isActive = hasParticles;
                    }
                }
            }
        }
    }

    private void EvacutableElementsAtMousePos()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Vector2Int chunkPos = new Vector2Int(
            Mathf.FloorToInt(mouseWorldPos.x),
            Mathf.FloorToInt(mouseWorldPos.y)
        );

        Debug.Log($"Attempting to evacuate chunk at {chunkPos}");

        EvacuateChunk(chunkPos);
    }

    [ContextMenu("Init Simulation")]
    public void InitSimulation()
    {
        if (chunkGenerationMode == ChunkGenerationMode.TilemapOutline)
        {
            BoundsInt bounds = boundaryTilemap.cellBounds;

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector2Int chunkPosition = new Vector2Int(x, y);
                    chunks[chunkPosition] = new Chunk(chunkPosition, chunkSize);
                }
            }
        }else if (chunkGenerationMode == ChunkGenerationMode.TilemapRooms)
        {
            BoundsInt bounds = boundaryTilemap.cellBounds;

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector2Int chunkPosition = new Vector2Int(x, y);
                    if(boundaryTilemap.GetTile(new Vector3Int(chunkPosition.x, chunkPosition.x, 0)) != null)
                    {
                        chunks[chunkPosition] = new Chunk(chunkPosition, chunkSize);
                    }
                }
            }
        }

        simulationRenderer.enabled = true;
        simulationRenderer.InitAll();
    }

    public void UpdateChunks()
    {
        if(chunkUpdateMode == ChunkUpdateMode.Sequential)
        {
            foreach (var chunk in chunks.Values)
            {
                if (chunk.isActive == false) continue;
                chunk.UpdateElements();
            }

            UpdateSimObjects();
        }
        else if(chunkUpdateMode == ChunkUpdateMode.FourSample)
        {
            foreach (var chunk in chunks.Values)
            {
                if (chunk.isActive == false) continue;
                if (chunk.chunkPosition.x % 2 != 0 && chunk.chunkPosition.y % 2 != 0) continue;
                chunk.UpdateElements();
            }

            foreach (var chunk in chunks.Values)
            {
                if (chunk.isActive == false) continue;
                if (chunk.chunkPosition.x % 2 == 0 && chunk.chunkPosition.y % 2 == 0) continue;
                chunk.UpdateElements();
            }

            foreach (var chunk in chunks.Values)
            {
                if (chunk.isActive == false) continue;
                if (chunk.chunkPosition.x % 2 != 0 && chunk.chunkPosition.y % 2 == 0) continue;
                chunk.UpdateElements();
            }

            foreach (var chunk in chunks.Values)
            {
                if (chunk.isActive == false) continue;
                if (chunk.chunkPosition.x % 2 == 0 && chunk.chunkPosition.y % 2 != 0) continue;
                chunk.UpdateElements();
            }

            UpdateSimObjects();
        }
    }
    public void UpdateSimObjects()
    {
        for (int i = 0; i < SandSimulation.Instance.simObjects.Count; i++)
        {
            SandSimulation.Instance.simObjects[i].Simulate();
        }
    }

    public void CreateSimObject(Vector2 objPos, Sprite objSprite, Color objColor, string elementType) // the color for blending
    {
        // Add detailed debug logging
        //Debug.Log($"Raw Mouse World Position: {mouseWorldPos}");
        //Debug.Log($"Chunk Size: {chunkSize}");

        // Calculate and log intermediate values
        float preChunkX = objPos.x / chunkSize;
        float preChunkY = objPos.y / chunkSize;
        //Debug.Log($"Pre-floor chunk calculation: ({preChunkX}, {preChunkY})");

        Vector2Int chunkPos = new Vector2Int(
            Mathf.FloorToInt(objPos.x),  // Not dividing by chunkSize
            Mathf.FloorToInt(objPos.y)
        );

        Vector2Int localPos = new Vector2Int(
            Mathf.FloorToInt(objPos.x) % chunkSize,
            Mathf.FloorToInt(objPos.y) % chunkSize
        );

        // Handle negative coordinates
        if (localPos.x < 0) localPos.x += chunkSize;
        if (localPos.y < 0) localPos.y += chunkSize;

        //Debug.Log($"Final Chunk Position: {chunkPos}");
        //Debug.Log($"Final Local Position: {localPos}");

        // Log all active chunks before adding new one
        /*
        Debug.Log("Currently Active Chunks:");
        foreach (var chunk in chunks.Where(c => c.Value.isActive))
        {
            Debug.Log($"Active chunk at: {chunk.Key}");
        }*/

        WitkotrisBlock newBlock = new WitkotrisBlock(objPos);
        currentTile = newBlock;
        newBlock._objectChunkPos = chunkPos;
        newBlock._objectLocalPos = localPos;
        newBlock.tileColor = objColor;
        newBlock.elementType = elementType;
        newBlock.tileShapeSprite = objSprite;
        newBlock.InitializeBlock();
        simObjects.Add(newBlock);

        if (chunks.ContainsKey(chunkPos))
        {
            //Debug.Log($"Activating chunk at: {chunkPos}");
            chunks[chunkPos].isActive = true;
            chunks[chunkPos].isActiveNextFrame = true;
        }
        else
        {
            //Debug.Log($"WARNING: No chunk found at position: {chunkPos}");
        }
    }

    public void CreateSimObjectAtMousePos(Sprite objSprite, Color objColor) // the color for blending
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Add detailed debug logging
        //Debug.Log($"Raw Mouse World Position: {mouseWorldPos}");
        //Debug.Log($"Chunk Size: {chunkSize}");

        // Calculate and log intermediate values
        float preChunkX = mouseWorldPos.x / chunkSize;
        float preChunkY = mouseWorldPos.y / chunkSize;
        //Debug.Log($"Pre-floor chunk calculation: ({preChunkX}, {preChunkY})");

        Vector2Int chunkPos = new Vector2Int(
            Mathf.FloorToInt(mouseWorldPos.x),  // Not dividing by chunkSize
            Mathf.FloorToInt(mouseWorldPos.y)
        );

        Vector2Int localPos = new Vector2Int(
            Mathf.FloorToInt(mouseWorldPos.x) % chunkSize,
            Mathf.FloorToInt(mouseWorldPos.y) % chunkSize
        );

        // Handle negative coordinates
        if (localPos.x < 0) localPos.x += chunkSize;
        if (localPos.y < 0) localPos.y += chunkSize;

        //Debug.Log($"Final Chunk Position: {chunkPos}");
        //Debug.Log($"Final Local Position: {localPos}");

        // Log all active chunks before adding new one
        /*
        Debug.Log("Currently Active Chunks:");
        foreach (var chunk in chunks.Where(c => c.Value.isActive))
        {
            Debug.Log($"Active chunk at: {chunk.Key}");
        }*/

        WitkotrisBlock newBlock = new WitkotrisBlock(mouseWorldPos);
        currentTile = newBlock;
        newBlock._objectChunkPos = chunkPos;
        newBlock._objectLocalPos = localPos;
        newBlock.tileColor = objColor;
        newBlock.elementType = "sand";
        newBlock.tileShapeSprite = objSprite;
        newBlock.InitializeBlock();
        simObjects.Add(newBlock);

        if (chunks.ContainsKey(chunkPos))
        {
            //Debug.Log($"Activating chunk at: {chunkPos}");
            chunks[chunkPos].isActive = true;
            chunks[chunkPos].isActiveNextFrame = true;
        }
        else
        {
            //Debug.Log($"WARNING: No chunk found at position: {chunkPos}");
        }
    }

    [HideInInspector] public SimulationObject currentTile;

    public void RotateSimObject()
    {
        if(currentTile != null && ((WitkotrisBlock)currentTile).isGranularized == false)
        {
            //rotate the tile
            ((WitkotrisBlock)currentTile).RotateClockwise();
        }
    }

    public void MoveTileRight()
    {
        if (currentTile != null && ((WitkotrisBlock)currentTile).isGranularized == false)
        {
            ((WitkotrisBlock)currentTile).moveRight = true;
        }
    }

    public void MoveTileLeft()
    {
        if (currentTile != null && ((WitkotrisBlock)currentTile).isGranularized == false)
        {
            ((WitkotrisBlock)currentTile).moveLeft = true;
        }
    }

    public void StopTileMovement()
    {
        if (currentTile != null)
        {
            ((WitkotrisBlock)currentTile).moveRight = false;
            ((WitkotrisBlock)currentTile).moveLeft = false;
        }
    }

    public void FastForwardFall()
    {
        this.fallSpeed = this.fastForwardSpeed;
        isFastForwarding = true;
    }

    public void StopFastFall()
    {
        this.fallSpeed = this.defaultFallSpeed;
        isFastForwarding = false;

        if(currentTile != null && this.fastForwardSpeed != 1 && (currentTile as WitkotrisBlock).isGranularized == false)
        {
            (currentTile as WitkotrisBlock).ProcessSpritePixels((currentTile as WitkotrisBlock).rotatedSprite);
        }
    }

    public void EvacuateChunk(Vector2Int chunkPosition)
    {
        if (!chunks.ContainsKey(chunkPosition))
        {
            Debug.LogWarning($"Attempted to evacuate non-existent chunk at {chunkPosition}");
            return;
        }

        Debug.LogWarning($"Evacuating chunk at {chunkPosition}");

        Chunk sourceChunk = chunks[chunkPosition];

        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),   // right
            new Vector2Int(1, 1),   // top-right
            new Vector2Int(0, 1),   // top
            new Vector2Int(-1, 1),  // top-left
            new Vector2Int(-1, 0),  // left
            new Vector2Int(-1, -1), // bottom-left
            new Vector2Int(0, -1),  // bottom
            new Vector2Int(1, -1)   // bottom-right
        };

        List<Element> elementsToMove = new List<Element>();
        for (int x = 0; x < sourceChunk.chunkSize; x++)
        {
            for (int y = 0; y < sourceChunk.chunkSize; y++)
            {
                if (sourceChunk.elements[x, y] != null)
                {
                    elementsToMove.Add(sourceChunk.elements[x, y]);
                    sourceChunk.elements[x, y] = null;
                }
            }
        }

        if (elementsToMove.Count == 0) return;

        List<(Vector2Int pos, Chunk chunk)> neighbors = new List<(Vector2Int, Chunk)>();
        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighborPos = chunkPosition + dir;
            if (chunks.TryGetValue(neighborPos, out Chunk neighborChunk))
            {
                neighbors.Add((neighborPos, neighborChunk));
            }
        }

        const int maxAttempts = 3;

        foreach (var element in elementsToMove)
        {
            bool placed = false;
            int attempts = 0;

            while (!placed && attempts < maxAttempts)
            {
                // Randomly select a neighbor chunk
                int neighborIndex = Random.Range(0, neighbors.Count);
                var (neighborPos, neighborChunk) = neighbors[neighborIndex];

                // Try a random position in the chunk
                int x = Random.Range(0, neighborChunk.chunkSize);
                int y = Random.Range(0, neighborChunk.chunkSize);

                // Check if position is valid
                Vector3Int tilePosition = new Vector3Int(
                    x / neighborChunk.chunkSize + neighborPos.x,
                    y / neighborChunk.chunkSize + neighborPos.y,
                    0);

                if (!collisionTilemap.HasTile(tilePosition) && neighborChunk.elements[x, y] == null)
                {
                    element.localPosition = new Vector2Int(x, y);
                    element.chunkPosition = neighborPos;
                    neighborChunk.elements[x, y] = element;
                    neighborChunk.isActive = true;
                    placed = true;
                }

                attempts++;
            }

            if (!placed)
            {
                Debug.LogWarning($"Particle destroyed during evacuation after {maxAttempts} failed placement attempts");
            }
        }
    }

    public void TriggerGameOver()
    {
        StartCoroutine(GameoverScreen());
    }

    public IEnumerator GameoverScreen()
    {
        foreach(Chunk ch in this.chunks.Values)
        {
            yield return new WaitForSecondsRealtime(0.000001f);
            EvacuateChunk(ch.chunkPosition);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (chunks == null || !drawDebug) return;
        float scale = 1f / chunkSize;

        foreach (var chunk in chunks.Values)
        {
            // Convert chunk position directly since it's already in tilemap coordinates
            Vector3 chunkCenter = new Vector3(
                chunk.chunkPosition.x + 0.5f,
                chunk.chunkPosition.y + 0.5f,
                0
            );

            // Render chunk borders
            if (SandSimulation.Instance.renderChunkBorders && !SandSimulation.Instance.showActiveChunks)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(chunkCenter, Vector3.one);
            }
            // Show active chunks
            else if (SandSimulation.Instance.showActiveChunks && !SandSimulation.Instance.renderChunkBorders)
            {
                Chunk ch;
                chunks.TryGetValue(chunk.chunkPosition, out ch);
                if (ch != null && ch.isActive)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(chunkCenter, Vector3.one);
                }
            }
            // Show both active and inactive chunks
            else if (SandSimulation.Instance.showActiveChunks && SandSimulation.Instance.renderChunkBorders)
            {
                Chunk ch;
                chunks.TryGetValue(chunk.chunkPosition, out ch);
                if (ch != null)
                {
                    Gizmos.color = ch.isActive ? Color.green : Color.red;
                    Gizmos.DrawWireCube(chunkCenter, Vector3.one);
                }
            }

            // Draw elements within chunks
            if (SandSimulation.Instance.renderElements)
            {
                for (int x = 0; x < chunk.chunkSize; x++)
                {
                    for (int y = 0; y < chunk.chunkSize; y++)
                    {
                        Element particle = chunk.elements[x, y];
                        if (particle != null)
                        {
                            float worldX = chunk.chunkPosition.x + (x * scale);
                            float worldY = chunk.chunkPosition.y + (y * scale);
                            Vector3 elementCenter = new Vector3(worldX, worldY, 0) + (Vector3.one * scale * 0.5f);

                            Gizmos.color = Color.yellow;
                            Gizmos.DrawCube(elementCenter, Vector3.one * scale * 0.8f);
                        }
                    }
                }
            }
        }
    }
#endif

    public class Chunk
    {
        public Vector2Int chunkPosition;
        public Element[,] elements;
        public int chunkSize;
        public bool isActive;
        public bool isActiveNextFrame;

        public Chunk(Vector2Int position, int size)
        {
            chunkPosition = position;
            chunkSize = size;
            elements = new Element[chunkSize, chunkSize];
            isActive = false;
            isActiveNextFrame = false;
        }

        public void FillWithElement(string particleType)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    if (elements[x, y] == null)
                    {
                        AddElement(new Vector2Int(x, y), particleType);
                    }
                }
            }
            isActive = true;
        }

        
        public float GetOccupancy()
        {
            int occupiedCells = 0;
            int totalCells = chunkSize * chunkSize;

            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    if (elements[x, y] != null)
                    {
                        occupiedCells++;
                    }
                }
            }

            return (float)occupiedCells / totalCells;
        }

        
        public void AddElement(Vector2Int localPos, string type = "sand")
        {
            if (localPos.x >= 0 && localPos.x < chunkSize && localPos.y >= 0 && localPos.y < chunkSize)
            {
                Element newParticle;
                switch (type.ToLower())
                {
                    case "water":
                        newParticle = new Water(localPos, chunkPosition);
                        break;
                    case "sand":
                        newParticle = new Sand(localPos, chunkPosition);
                        break;
                    case "sawdust":
                        newParticle = new SawDust(localPos, chunkPosition);
                        break;
                    case "flour":
                        newParticle = new SawDust(localPos, chunkPosition);
                        break;
                    case "tempelement":
                        newParticle = new SawDust(localPos, chunkPosition);
                        break;
                    default:
                        newParticle = new Sand(localPos, chunkPosition);
                        break;
                }
                elements[localPos.x, localPos.y] = newParticle;
                this.isActive = true;
                this.isActiveNextFrame = true;
            }
        }

        public void AddElement(Vector2Int localPos, out Element e, string type = "sand")
        {
            e = null;

            if (localPos.x >= 0 && localPos.x < chunkSize && localPos.y >= 0 && localPos.y < chunkSize)
            {
                Element newParticle;
                switch (type.ToLower())
                {
                    case "water":
                        newParticle = new Water(localPos, chunkPosition);
                        e = newParticle;
                        break;
                    case "sand":
                        newParticle = new Sand(localPos, chunkPosition);
                        e = newParticle;
                        break;
                    case "sawdust":
                        newParticle = new SawDust(localPos, chunkPosition);
                        e = newParticle;
                        break;
                    case "flour":
                        newParticle = new Flour(localPos, chunkPosition);
                        e = newParticle;
                        break;
                    case "bricks":
                        newParticle = new Bricks(localPos, chunkPosition);
                        e = newParticle;
                        break;
                    case "tempelement":
                        newParticle = new TemporalPowder(localPos, chunkPosition);
                        e = newParticle;
                        break;
                    default:
                        newParticle = new Sand(localPos, chunkPosition);
                        e = newParticle;
                        break;
                }
                elements[localPos.x, localPos.y] = newParticle;
                this.isActive = true;
                this.isActiveNextFrame = true;
            }
        }

        public void AddCustomElement(Vector2Int localPos, Element elementToAdd)
        {
            if (localPos.x >= 0 && localPos.x < chunkSize && localPos.y >= 0 && localPos.y < chunkSize)
            {
                elements[localPos.x, localPos.y] = elementToAdd;
                this.isActive = true;
                this.isActiveNextFrame = true;
            }
        }

        // Update particle behavior in this chunk
        public void UpdateElements()
        {
            if (!isActive) return;
            //bool allNull = particles.Cast<Particle>().All(x => x == null);

            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    Element p = elements[x, y];
                    if (p != null && !p.containedInObject)
                    {
                        p.Simulate(this);
                    }
                }
            }

            isActive = isActiveNextFrame;
            isActiveNextFrame = false;
        }

        public void CheckElementContacts(Element particle)
        {
            Vector2Int[] contactPoints = new Vector2Int[]
            {
                new Vector2Int(particle.localPosition.x, particle.localPosition.y - 1), // below
                new Vector2Int(particle.localPosition.x, particle.localPosition.y + 1)  // above
            };

            particle.hasContactBelow = false;
            particle.hasContactAbove = false;

            foreach (Vector2Int contactPoint in contactPoints)
            {
                Vector2Int targetChunk = particle.chunkPosition;
                Vector2Int adjustedPos = contactPoint;

                if (adjustedPos.y < 0)
                {
                    adjustedPos.y = chunkSize - 1;
                    targetChunk.y -= 1;
                }
                else if (adjustedPos.y >= chunkSize)
                {
                    adjustedPos.y = 0;
                    targetChunk.y += 1;
                }

                if (SandSimulation.Instance.chunks.TryGetValue(targetChunk, out Chunk targetChunkObj))
                {
                    Vector3Int tilePosition = new Vector3Int(
                        adjustedPos.x / chunkSize + targetChunk.x,
                        adjustedPos.y / chunkSize + targetChunk.y,
                        0);

                    bool hasContact = SandSimulation.Instance.collisionTilemap.HasTile(tilePosition) ||
                                    targetChunkObj.elements[adjustedPos.x, adjustedPos.y] != null;

                    if (contactPoint.y < particle.localPosition.y)
                    {
                        particle.hasContactBelow = hasContact;
                    }
                    else
                    {
                        particle.hasContactAbove = hasContact;
                    }
                }
            }
        }

        
        public bool HasParticleType<T>() where T : Element
        {
            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    if (elements[x, y] is T) return true;
                }
            }
            return false;
        }

        public float GetParticleTypePercentage<T>() where T : Element
        {
            int count = 0;
            int totalCells = chunkSize * chunkSize;

            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    if (elements[x, y] is T) count++;
                }
            }

            return (float)count / totalCells;
        }

        public void WakeUpAllChunkNeighbors(bool diagonal = false)
        {
            if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x + 1, this.chunkPosition.y), out Chunk rightChunk))
            {
                rightChunk.isActive = true;
                rightChunk.isActiveNextFrame = true;
            }

            if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x - 1, this.chunkPosition.y), out Chunk leftChunk))
            {
                leftChunk.isActive = true;
                leftChunk.isActiveNextFrame = true;
            }

            if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x, this.chunkPosition.y + 1), out Chunk aboveChunk))
            {
                aboveChunk.isActive = true;
                aboveChunk.isActiveNextFrame = true;
            }

            if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x, this.chunkPosition.y - 1), out Chunk belowChunk))
            {
                belowChunk.isActive = true;
                belowChunk.isActiveNextFrame = true;
            }

            if (diagonal)
            {
                if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x + 1, this.chunkPosition.y + 1), out Chunk aboveRightChunk))
                {
                    aboveRightChunk.isActive = true;
                    aboveRightChunk.isActiveNextFrame = true;
                }

                if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x + 1, this.chunkPosition.y - 1), out Chunk belowRightChunk))
                {
                    belowRightChunk.isActive = true;
                    belowRightChunk.isActiveNextFrame = true;
                }

                if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x - 1, this.chunkPosition.y + 1), out Chunk aboveLeftChunk))
                {
                    aboveLeftChunk.isActive = true;
                    aboveLeftChunk.isActiveNextFrame = true;
                }

                if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x - 1, this.chunkPosition.y - 1), out Chunk belowLeftChunk))
                {
                    belowLeftChunk.isActive = true;
                    belowLeftChunk.isActiveNextFrame = true;
                }
            }
        }

        public void WakeUpChunkNeighbors(Vector2Int chunkPosition, bool diagonal = false)
        {
            Vector2Int chunkPos = chunkPosition;

            bool activateRight = chunkPos.x == chunkSize - 1;
            bool activateLeft = chunkPos.x == 0;
            bool activateUp = chunkPos.y == chunkSize - 1;
            bool activateDown = chunkPos.y == 0;
            //diagonal
            bool activateRightUp = chunkPos.x == chunkSize - 1 && chunkPos.y == chunkSize - 1;
            bool activateRightDown = chunkPos.x == chunkSize - 1 && chunkPos.y == 0;
            bool activateLeftUp = chunkPos.x == 0 && chunkPos.y == chunkSize - 1;
            bool activateLeftDown = chunkPos.x == 0 && chunkPos.y == 0;

            if (activateRight && SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x + 1, this.chunkPosition.y), out Chunk rightChunk))
            {
                rightChunk.isActive = true;
                rightChunk.isActiveNextFrame = true;
            }

            if (activateLeft && SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x - 1, this.chunkPosition.y), out Chunk leftChunk))
            {
                leftChunk.isActive = true;
                leftChunk.isActiveNextFrame = true;
            }

            if (activateUp && SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x, this.chunkPosition.y + 1), out Chunk aboveChunk))
            {
                aboveChunk.isActive = true;
                aboveChunk.isActiveNextFrame = true;
            }

            if (activateDown && SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x, this.chunkPosition.y - 1), out Chunk belowChunk))
            {
                belowChunk.isActive = true;
                belowChunk.isActiveNextFrame = true;
            }

            if (diagonal)
            {
                if (activateRightUp && SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x + 1, this.chunkPosition.y + 1), out Chunk aboveRightChunk))
                {
                    aboveRightChunk.isActive = true;
                    aboveRightChunk.isActiveNextFrame = true;
                }

                if (activateRightDown && SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x + 1, this.chunkPosition.y - 1), out Chunk belowRightChunk))
                {
                    belowRightChunk.isActive = true;
                    belowRightChunk.isActiveNextFrame = true;
                }

                if (activateLeftUp && SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x - 1, this.chunkPosition.y + 1), out Chunk aboveLeftChunk))
                {
                    aboveLeftChunk.isActive = true;
                    aboveLeftChunk.isActiveNextFrame = true;
                }

                if (activateLeftDown && SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x - 1, this.chunkPosition.y - 1), out Chunk belowLeftChunk))
                {
                    belowLeftChunk.isActive = true;
                    belowLeftChunk.isActiveNextFrame = true;
                }
            }
        }

        public void WakeUpParticleNeighbors(Vector2Int position, Vector2Int currentChunkPos)
        {
            Vector2Int[] neighbors = new Vector2Int[]
            {
                new Vector2Int(position.x - 1, position.y),     // left
                new Vector2Int(position.x + 1, position.y),     // right
                new Vector2Int(position.x, position.y + 1),     // above
                new Vector2Int(position.x - 1, position.y + 1), // above-left
                new Vector2Int(position.x + 1, position.y + 1), // above-right
            };

            foreach (Vector2Int neighborPos in neighbors)
            {
                Vector2Int targetChunk = currentChunkPos;
                Vector2Int adjustedPos = neighborPos;

                if (adjustedPos.x < 0)
                {
                    adjustedPos.x = chunkSize - 1;
                    targetChunk.x -= 1;
                }
                else if (adjustedPos.x >= chunkSize)
                {
                    adjustedPos.x = 0;
                    targetChunk.x += 1;
                }
                if (adjustedPos.y >= chunkSize)
                {
                    adjustedPos.y = 0;
                    targetChunk.y += 1;
                }

                if (SandSimulation.Instance.chunks.TryGetValue(targetChunk, out Chunk targetChunkObj))
                {
                    Element neighbor = targetChunkObj.elements[adjustedPos.x, adjustedPos.y];
                    if (neighbor != null && neighbor.isResting)
                    {
                        neighbor.isResting = false;
                        neighbor.fallVelocity = 0f;
                    }
                }
            }
        }

        public void ActivateAllNeighbors(bool diagonal = false)
        {
            if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x + 1, this.chunkPosition.y), out Chunk rightChunk))
            {
                rightChunk.isActive = true;
                rightChunk.isActiveNextFrame = true;
            }

            if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x - 1, this.chunkPosition.y), out Chunk leftChunk))
            {
                leftChunk.isActive = true;
                leftChunk.isActiveNextFrame = true;
            }

            if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x, this.chunkPosition.y + 1), out Chunk aboveChunk))
            {
                aboveChunk.isActive = true;
                aboveChunk.isActiveNextFrame = true;
            }

            if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x, this.chunkPosition.y - 1), out Chunk belowChunk))
            {
                belowChunk.isActive = true;
                belowChunk.isActiveNextFrame = true;
            }

            if (diagonal)
            {
                if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x + 1, this.chunkPosition.y + 1), out Chunk aboveRightChunk))
                {
                    aboveRightChunk.isActive = true;
                    aboveRightChunk.isActiveNextFrame = true;
                }

                if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x + 1, this.chunkPosition.y - 1), out Chunk belowRightChunk))
                {
                    belowRightChunk.isActive = true;
                    belowRightChunk.isActiveNextFrame = true;
                }

                if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x - 1, this.chunkPosition.y + 1), out Chunk aboveLeftChunk))
                {
                    aboveLeftChunk.isActive = true;
                    aboveLeftChunk.isActiveNextFrame = true;
                }

                if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x - 1, this.chunkPosition.y - 1), out Chunk belowLeftChunk))
                {
                    belowLeftChunk.isActive = true;
                    belowLeftChunk.isActiveNextFrame = true;
                }
            }
        }

        public void ActivateNeighbors(Vector2Int localPos)
        {
            Vector2Int currentChunkPos = this.chunkPosition;

            bool isOnLeftBorder = localPos.x == 0;
            bool isOnRightBorder = localPos.x == chunkSize;
            bool isOnTopBorder = localPos.y == chunkSize;
            bool isOnBottomBorder = localPos.y == 0;

            List<Vector2Int> chunksToWake = new List<Vector2Int>();

            if (isOnLeftBorder)
            {
                chunksToWake.Add(new Vector2Int(currentChunkPos.x - 1, currentChunkPos.y));
                if (isOnTopBorder)
                    chunksToWake.Add(new Vector2Int(currentChunkPos.x - 1, currentChunkPos.y + 1));
                if (isOnBottomBorder)
                    chunksToWake.Add(new Vector2Int(currentChunkPos.x - 1, currentChunkPos.y - 1));
            }
            if (isOnRightBorder)
            {
                chunksToWake.Add(new Vector2Int(currentChunkPos.x + 1, currentChunkPos.y));
                if (isOnTopBorder)
                    chunksToWake.Add(new Vector2Int(currentChunkPos.x + 1, currentChunkPos.y + 1));
                if (isOnBottomBorder)
                    chunksToWake.Add(new Vector2Int(currentChunkPos.x + 1, currentChunkPos.y - 1));
            }

            if (isOnTopBorder)
            {
                chunksToWake.Add(new Vector2Int(currentChunkPos.x, currentChunkPos.y + 1));
            }
            if (isOnBottomBorder)
            {
                chunksToWake.Add(new Vector2Int(currentChunkPos.x, currentChunkPos.y - 1));
            }

            foreach (Vector2Int neighborChunk in chunksToWake)
            {
                if (SandSimulation.Instance.chunks.TryGetValue(neighborChunk, out Chunk neighborChunkObj))
                {
                    neighborChunkObj.isActive = true;
                    neighborChunkObj.isActiveNextFrame = true;
                }
            }
        }
    }

    public abstract class SimulationObject
    {
        private Vector2? _objectPosition; // Backing field
        public Vector2Int _objectChunkPos;
        public Vector2Int _objectLocalPos;

        public virtual Vector2? objectPosition
        {
            get => _objectPosition;
            protected set => _objectPosition = value;
        }
        public List<Element> containedElements = new List<Element>();

        public SimulationObject(Vector2 objectPos)
        {
            _objectPosition = objectPos;
        }

        public abstract void Simulate();
        public abstract void ComputeCollisions();

        public Vector2? GetWorldPos()
        {
            if(objectPosition != null) {
                Vector2 globalPos = objectPosition.Value;
                if(globalPos != null) return globalPos;
                else return null;
            }
            else return null;
        }

        public (Vector2Int?/*chunk*/, Vector2Int?/*local*/) GetSimulationPos()
        {
            if(objectPosition == null && objectPosition.Value == null) return (null, null); 

            int chunkSize = SandSimulation.Instance.chunkSize;
            int chunkPosX = (int)(objectPosition.Value.x / chunkSize);
            int chunkPosY = (int)(objectPosition.Value.y / chunkSize);
            int localPosX = (int)(objectPosition.Value.x % chunkSize);
            int localPosY = (int)(objectPosition.Value.y % chunkSize);

            return (new Vector2Int(chunkPosX, chunkPosY), new Vector2Int(localPosX, localPosY));
        }
    }

    public class WitkotrisBlock : SimulationObject
    {
        public bool moveRight = false;
        public bool moveLeft = false;

        public override Vector2? objectPosition
        {
            get => base.objectPosition;
            protected set => base.objectPosition = value; // Allow setting the position
        }

        public string elementType = null;
        public Color tileColor = Color.white;
        public Sprite tileShapeSprite = null;
        public Sprite rotatedSprite = null;
        private bool initialized = false;
        private Stopwatch timer;

        public WitkotrisBlock(Vector2 objectPos) : base(objectPos)
        {
            objectPosition = objectPos;
            initialized = false;
            //InitializeBlock();
            timer = new Stopwatch();
            timer.Reset();
            timer.Start();
        }

        public void InitializeBlock()
        {
            if(tileShapeSprite != null && elementType != null)
            {
                //int pixelCount = GetNonTransparentPixelCount(tileShapeSprite);
                ProcessSpritePixels(tileShapeSprite);
            }
        }

        public void ProcessSpritePixels(Sprite sprite)
        {
            rotatedSprite = sprite;

            // Get the sprite's texture
            Texture2D texture = sprite.texture;

            // Get the sprite's rect in the texture
            Rect rect = sprite.textureRect;

            // Save texture's colors here
            HashSet<Color> uniqueColors = new HashSet<Color>();

            // Convert rect to pixel coordinates
            int xStart = Mathf.RoundToInt(rect.x);
            int yStart = Mathf.RoundToInt(rect.y);
            int width = Mathf.RoundToInt(rect.width);
            int height = Mathf.RoundToInt(rect.height);

            // Iterate over each pixel in the sprite
            for (int y = yStart; y < yStart + height; y++)
            {
                for (int x = xStart; x < xStart + width; x++)
                {
                    Color pixelColor = texture.GetPixel(x, y);

                    if (pixelColor.a == 1f)
                    {
                        // Calculate brightness as a simple average of RGB components
                        float brightness = (pixelColor.r + pixelColor.g + pixelColor.b) / 3f;

                        // Blend the color based on brightness and tileColor
                        Color finalColor = new Color(
                            tileColor.r * brightness,
                            tileColor.g * brightness,
                            tileColor.b * brightness,
                            1f
                        );

                        //print("adding color: " + finalColor.r + " " + finalColor.g + " " + finalColor.b);


                        uniqueColors.Add(finalColor);
                    }

                    ProcessPixel(x, y, pixelColor, uniqueColors);
                }
            }

            initialized = true;
        }

        void ProcessPixel(int x, int y, Color color, HashSet<Color> colorData)
        {
            if (!objectPosition.HasValue) return;
            if (color.a != 1) return;

            // Calculate world position for this pixel
            Vector2 worldPos = new Vector2(
                this.objectPosition.Value.x * SandSimulation.Instance.chunkSize + x,
                this.objectPosition.Value.y * SandSimulation.Instance.chunkSize + y
            );

            // Calculate correct chunk position
            Vector2Int chunkPos = new Vector2Int(
                Mathf.FloorToInt(worldPos.x / SandSimulation.Instance.chunkSize),
                Mathf.FloorToInt(worldPos.y / SandSimulation.Instance.chunkSize)
            );

            // Calculate local position within chunk
            Vector2Int localPos = new Vector2Int(
                Mathf.FloorToInt(worldPos.x) % SandSimulation.Instance.chunkSize,
                Mathf.FloorToInt(worldPos.y) % SandSimulation.Instance.chunkSize
            );

            //chunkPos = _objectChunkPos;
            //localPos = _objectLocalPos - new Vector2Int(x, y);

            // Handle negative coordinates
            if (localPos.x < 0) localPos.x += SandSimulation.Instance.chunkSize;
            if (localPos.y < 0) localPos.y += SandSimulation.Instance.chunkSize;

            //process the color
            float brightness = (color.r + color.g + color.b) / 3f;  // Simple average for brightness

            // Create a modified color based on the pixel brightness and tileColor
            Color finalColor = new Color(
                tileColor.r * brightness,
                tileColor.g * brightness,
                tileColor.b * brightness,
                tileColor.a
            );

            if (elementType == "sand")
            {
                Sand e_sand = new Sand(localPos, chunkPos);
                Vector2Int adjustedLocal = e_sand.AdjustPositionForChunk(localPos, ref chunkPos, SandSimulation.Instance.chunkSize);

                /// Debug visualization
                ///Vector3 debugWorldPos = SimulationCoordinateConverter.ChunkToWorldPosition(chunkPos, adjustedLocal,
                ///    SandSimulation.Instance.chunkSize);
                ///Debug.DrawLine(debugWorldPos, debugWorldPos + Vector3.forward * 0.01f, Color.red, 5.0f);

                e_sand.localPosition = adjustedLocal;
                e_sand.chunkPosition = chunkPos;
                e_sand.useCustomColorData = true;
                e_sand.LocalColor = finalColor;
                //add color data for the borderedge algorithm
                //e_sand.colorData = colorData.ToArray();
                e_sand.SetCustomColorData(colorData.ToArray());
                e_sand.containedInObject = true;
                e_sand.containingObject = this;

                if (SandSimulation.Instance.chunks.TryGetValue(chunkPos, out Chunk chunkToAdd) && chunkToAdd != null)
                {
                    chunkToAdd.AddCustomElement(localPos, e_sand);
                    this.containedElements.Add(e_sand);
                    // Make sure the chunk is active
                    //chunkToAdd.isActive = true;
                    //chunkToAdd.isActiveNextFrame = true;
                }
            }
            else if(elementType == "water")
            {
                Water e_water = new Water(localPos, chunkPos);
                Vector2Int adjustedLocal = e_water.AdjustPositionForChunk(localPos, ref chunkPos, SandSimulation.Instance.chunkSize);

                /// Debug visualization
                ///Vector3 debugWorldPos = SimulationCoordinateConverter.ChunkToWorldPosition(chunkPos, adjustedLocal,
                ///    SandSimulation.Instance.chunkSize);
                ///Debug.DrawLine(debugWorldPos, debugWorldPos + Vector3.forward * 0.01f, Color.red, 5.0f);

                e_water.localPosition = adjustedLocal;
                e_water.chunkPosition = chunkPos;
                e_water.useCustomColorData = true;
                e_water.LocalColor = finalColor;
                //add color data for the borderedge algorithm
                //e_sand.colorData = colorData.ToArray();
                e_water.SetCustomColorData(colorData.ToArray());
                e_water.containedInObject = true;
                e_water.containingObject = this;

                if (SandSimulation.Instance.chunks.TryGetValue(chunkPos, out Chunk chunkToAdd) && chunkToAdd != null)
                {
                    chunkToAdd.AddCustomElement(localPos, e_water);
                    this.containedElements.Add(e_water);
                    // Make sure the chunk is active
                    //chunkToAdd.isActive = true;
                    //chunkToAdd.isActiveNextFrame = true;
                }
            }
            else if (elementType == "sawdust")
            {
                SawDust e_sawdust = new SawDust(localPos, chunkPos);
                Vector2Int adjustedLocal = e_sawdust.AdjustPositionForChunk(localPos, ref chunkPos, SandSimulation.Instance.chunkSize);

                /// Debug visualization
                ///Vector3 debugWorldPos = SimulationCoordinateConverter.ChunkToWorldPosition(chunkPos, adjustedLocal,
                ///    SandSimulation.Instance.chunkSize);
                ///Debug.DrawLine(debugWorldPos, debugWorldPos + Vector3.forward * 0.01f, Color.red, 5.0f);

                e_sawdust.localPosition = adjustedLocal;
                e_sawdust.chunkPosition = chunkPos;
                e_sawdust.useCustomColorData = true;
                e_sawdust.LocalColor = finalColor;
                //add color data for the borderedge algorithm
                //e_sand.colorData = colorData.ToArray();
                e_sawdust.SetCustomColorData(colorData.ToArray());
                e_sawdust.containedInObject = true;
                e_sawdust.containingObject = this;

                if (SandSimulation.Instance.chunks.TryGetValue(chunkPos, out Chunk chunkToAdd) && chunkToAdd != null)
                {
                    chunkToAdd.AddCustomElement(localPos, e_sawdust);
                    this.containedElements.Add(e_sawdust);
                    // Make sure the chunk is active
                    //chunkToAdd.isActive = true;
                    //chunkToAdd.isActiveNextFrame = true;
                }
            }
            else if (elementType == "flour")
            {
                Flour e_flour = new Flour(localPos, chunkPos);
                Vector2Int adjustedLocal = e_flour.AdjustPositionForChunk(localPos, ref chunkPos, SandSimulation.Instance.chunkSize);

                /// Debug visualization
                ///Vector3 debugWorldPos = SimulationCoordinateConverter.ChunkToWorldPosition(chunkPos, adjustedLocal,
                ///    SandSimulation.Instance.chunkSize);
                ///Debug.DrawLine(debugWorldPos, debugWorldPos + Vector3.forward * 0.01f, Color.red, 5.0f);

                e_flour.localPosition = adjustedLocal;
                e_flour.chunkPosition = chunkPos;
                e_flour.useCustomColorData = true;
                e_flour.LocalColor = finalColor;
                //add color data for the borderedge algorithm
                //e_sand.colorData = colorData.ToArray();
                e_flour.SetCustomColorData(colorData.ToArray());
                e_flour.containedInObject = true;
                e_flour.containingObject = this;

                if (SandSimulation.Instance.chunks.TryGetValue(chunkPos, out Chunk chunkToAdd) && chunkToAdd != null)
                {
                    chunkToAdd.AddCustomElement(localPos, e_flour);
                    this.containedElements.Add(e_flour);
                    // Make sure the chunk is active
                    //chunkToAdd.isActive = true;
                    //chunkToAdd.isActiveNextFrame = true;
                }
            }
            else if (elementType == "bricks")
            {
                Bricks e_bricks = new Bricks(localPos, chunkPos);
                Vector2Int adjustedLocal = e_bricks.AdjustPositionForChunk(localPos, ref chunkPos, SandSimulation.Instance.chunkSize);

                /// Debug visualization
                ///Vector3 debugWorldPos = SimulationCoordinateConverter.ChunkToWorldPosition(chunkPos, adjustedLocal,
                ///    SandSimulation.Instance.chunkSize);
                ///Debug.DrawLine(debugWorldPos, debugWorldPos + Vector3.forward * 0.01f, Color.red, 5.0f);

                e_bricks.localPosition = adjustedLocal;
                e_bricks.chunkPosition = chunkPos;
                e_bricks.useCustomColorData = true;
                e_bricks.LocalColor = finalColor;
                //add color data for the borderedge algorithm
                //e_sand.colorData = colorData.ToArray();
                e_bricks.SetCustomColorData(colorData.ToArray());
                e_bricks.containedInObject = true;
                e_bricks.containingObject = this;

                if (SandSimulation.Instance.chunks.TryGetValue(chunkPos, out Chunk chunkToAdd) && chunkToAdd != null)
                {
                    chunkToAdd.AddCustomElement(localPos, e_bricks);
                    this.containedElements.Add(e_bricks);
                    // Make sure the chunk is active
                    //chunkToAdd.isActive = true;
                    //chunkToAdd.isActiveNextFrame = true;
                }
            }


        }

        public int GetNonTransparentPixelCount(Sprite sprite)
        {
            // Get the texture from the sprite
            Texture2D texture = sprite.texture;

            // Get the pixels from the sprite (using the UV rectangle to avoid unneeded data)
            Rect spriteRect = sprite.textureRect;
            Color[] pixels = texture.GetPixels(
                (int)spriteRect.x,
                (int)spriteRect.y,
                (int)spriteRect.width,
                (int)spriteRect.height
            );

            int count = 0;

            // Loop through the pixels and count those with non-zero alpha
            foreach (Color pixel in pixels)
            {
                if (pixel.a > 0f) // Alpha value greater than 0 means it's not transparent
                {
                    count++;
                }
            }

            return count;
        }

        public void FinishInitialization()
        {

        }

        private int fallFactorCounter = 0;

        public override void Simulate()
        {
            if (!initialized || containedElements.Count <= 0) return;
            if(isGranularized) return;

            bool outOfBoundsRight = CheckForOutOfBounds(new Vector2Int(SandSimulation.Instance.horizontalSpeed, -SandSimulation.Instance.fallSpeed));
            bool outOfBoundsLeft = CheckForOutOfBounds(new Vector2Int(-SandSimulation.Instance.horizontalSpeed, -SandSimulation.Instance.fallSpeed));

            Dictionary<Element, (Vector2Int pos, Vector2Int chunk)> moves = new Dictionary<Element, (Vector2Int, Vector2Int)>();
            Vector2 totalMovement = Vector2.zero;

            foreach (Element element in containedElements)
            {
                if(elementType == "sand")
                {
                    if (!element.containedInObject) continue;
                    if (isGranularized) return;

                    Sand sand = element as Sand;
                    Vector2Int chunkPOS = sand.chunkPosition;
                    Vector2 relativeObjPos;
                    Vector2Int targetPos;

                    if (moveRight && moveLeft)
                    {
                        relativeObjPos = new Vector2(0, -SandSimulation.Instance.fallSpeed);
                        targetPos = sand.AdjustPositionForChunk(
                            new Vector2Int(sand.localPosition.x, sand.localPosition.y - SandSimulation.Instance.fallSpeed),
                            ref chunkPOS,
                            SandSimulation.Instance.chunkSize
                        );
                    }
                    else if (moveRight && !outOfBoundsRight)
                    {
                        relativeObjPos = new Vector2(SandSimulation.Instance.horizontalSpeed, -SandSimulation.Instance.fallSpeed);
                        targetPos = sand.AdjustPositionForChunk(
                            new Vector2Int(sand.localPosition.x + SandSimulation.Instance.horizontalSpeed, sand.localPosition.y - SandSimulation.Instance.fallSpeed),
                            ref chunkPOS,
                            SandSimulation.Instance.chunkSize
                        );
                    }
                    else if (moveLeft && !outOfBoundsLeft)
                    {
                        relativeObjPos = new Vector2(-SandSimulation.Instance.horizontalSpeed, -SandSimulation.Instance.fallSpeed);
                        targetPos = sand.AdjustPositionForChunk(
                            new Vector2Int(sand.localPosition.x - SandSimulation.Instance.horizontalSpeed, sand.localPosition.y - SandSimulation.Instance.fallSpeed),
                            ref chunkPOS,
                            SandSimulation.Instance.chunkSize
                        );
                    }
                    else
                    {
                        relativeObjPos = new Vector2(0, -SandSimulation.Instance.fallSpeed);
                        targetPos = sand.AdjustPositionForChunk(
                            new Vector2Int(sand.localPosition.x, sand.localPosition.y - SandSimulation.Instance.fallSpeed),
                            ref chunkPOS,
                            SandSimulation.Instance.chunkSize
                        );
                    }

                    if (SandSimulation.Instance.chunks.TryGetValue(chunkPOS, out Chunk targetChunkObject))
                    {
                        targetChunkObject.isActive = true;
                        targetChunkObject.isActiveNextFrame = true;

                        Vector3Int tilePosition = sand.GetTilemapPosition(targetPos, chunkPOS);
                        bool isEmpty = !SandSimulation.Instance.collisionTilemap.HasTile(tilePosition) &&
                                     (targetChunkObject.elements[targetPos.x, targetPos.y] == null ||
                                      targetChunkObject.elements[targetPos.x, targetPos.y].containedInObject);

                        if (SandSimulation.Instance.chunks.TryGetValue(sand.chunkPosition, out Chunk sandChunk))
                        {
                            sandChunk.isActive = true;
                            sandChunk.isActiveNextFrame = true;

                            if (isEmpty && sandChunk != null)
                            {
                                moves[sand] = (targetPos, chunkPOS);
                                totalMovement += relativeObjPos;
                            }
                            else
                            {
                                Granularize();
                                return;
                            }
                        }
                    }
                }else if(elementType == "water")
                {
                    if (!element.containedInObject) continue;
                    if (isGranularized) return;

                    Water water = element as Water;
                    Vector2Int chunkPOS = water.chunkPosition;
                    Vector2 relativeObjPos;
                    Vector2Int targetPos;

                    if (moveRight && moveLeft)
                    {
                        relativeObjPos = new Vector2(0, -SandSimulation.Instance.fallSpeed);
                        targetPos = water.AdjustPositionForChunk(
                            new Vector2Int(water.localPosition.x, water.localPosition.y - SandSimulation.Instance.fallSpeed),
                            ref chunkPOS,
                            SandSimulation.Instance.chunkSize
                        );
                    }
                    else if (moveRight && !outOfBoundsRight)
                    {
                        relativeObjPos = new Vector2(SandSimulation.Instance.horizontalSpeed, -SandSimulation.Instance.fallSpeed);
                        targetPos = water.AdjustPositionForChunk(
                            new Vector2Int(water.localPosition.x + SandSimulation.Instance.horizontalSpeed, water.localPosition.y - SandSimulation.Instance.fallSpeed),
                            ref chunkPOS,
                            SandSimulation.Instance.chunkSize
                        );
                    }
                    else if (moveLeft && !outOfBoundsLeft)
                    {
                        relativeObjPos = new Vector2(-SandSimulation.Instance.horizontalSpeed, -SandSimulation.Instance.fallSpeed);
                        targetPos = water.AdjustPositionForChunk(
                            new Vector2Int(water.localPosition.x - SandSimulation.Instance.horizontalSpeed, water.localPosition.y - SandSimulation.Instance.fallSpeed),
                            ref chunkPOS,
                            SandSimulation.Instance.chunkSize
                        );
                    }
                    else
                    {
                        relativeObjPos = new Vector2(0, -SandSimulation.Instance.fallSpeed);
                        targetPos = water.AdjustPositionForChunk(
                            new Vector2Int(water.localPosition.x, water.localPosition.y - SandSimulation.Instance.fallSpeed),
                            ref chunkPOS,
                            SandSimulation.Instance.chunkSize
                        );
                    }

                    if (SandSimulation.Instance.chunks.TryGetValue(chunkPOS, out Chunk targetChunkObject))
                    {
                        targetChunkObject.isActive = true;
                        targetChunkObject.isActiveNextFrame = true;

                        Vector3Int tilePosition = water.GetTilemapPosition(targetPos, chunkPOS);
                        bool isEmpty = !SandSimulation.Instance.collisionTilemap.HasTile(tilePosition) &&
                                     (targetChunkObject.elements[targetPos.x, targetPos.y] == null ||
                                      targetChunkObject.elements[targetPos.x, targetPos.y].containedInObject);

                        if (SandSimulation.Instance.chunks.TryGetValue(water.chunkPosition, out Chunk sandChunk))
                        {
                            sandChunk.isActive = true;
                            sandChunk.isActiveNextFrame = true;

                            if (isEmpty && sandChunk != null)
                            {
                                moves[water] = (targetPos, chunkPOS);
                                totalMovement += relativeObjPos;
                            }
                            else
                            {
                                Granularize();
                                return;
                            }
                        }
                    }
                }
                else if (elementType == "sawdust")
                {
                    if (!element.containedInObject) continue;
                    if (isGranularized) return;

                    SawDust sawdust = element as SawDust;
                    Vector2Int chunkPOS = sawdust.chunkPosition;
                    Vector2 relativeObjPos;
                    Vector2Int targetPos;

                    if (moveRight && moveLeft)
                    {
                        relativeObjPos = new Vector2(0, -SandSimulation.Instance.fallSpeed);
                        targetPos = sawdust.AdjustPositionForChunk(
                            new Vector2Int(sawdust.localPosition.x, sawdust.localPosition.y - SandSimulation.Instance.fallSpeed),
                            ref chunkPOS,
                            SandSimulation.Instance.chunkSize
                        );
                    }
                    else if (moveRight && !outOfBoundsRight)
                    {
                        relativeObjPos = new Vector2(SandSimulation.Instance.horizontalSpeed, -SandSimulation.Instance.fallSpeed);
                        targetPos = sawdust.AdjustPositionForChunk(
                            new Vector2Int(sawdust.localPosition.x + SandSimulation.Instance.horizontalSpeed, sawdust.localPosition.y - SandSimulation.Instance.fallSpeed),
                            ref chunkPOS,
                            SandSimulation.Instance.chunkSize
                        );
                    }
                    else if (moveLeft && !outOfBoundsLeft)
                    {
                        relativeObjPos = new Vector2(-SandSimulation.Instance.horizontalSpeed, -SandSimulation.Instance.fallSpeed);
                        targetPos = sawdust.AdjustPositionForChunk(
                            new Vector2Int(sawdust.localPosition.x - SandSimulation.Instance.horizontalSpeed, sawdust.localPosition.y - SandSimulation.Instance.fallSpeed),
                            ref chunkPOS,
                            SandSimulation.Instance.chunkSize
                        );
                    }
                    else
                    {
                        relativeObjPos = new Vector2(0, -SandSimulation.Instance.fallSpeed);
                        targetPos = sawdust.AdjustPositionForChunk(
                            new Vector2Int(sawdust.localPosition.x, sawdust.localPosition.y - SandSimulation.Instance.fallSpeed),
                            ref chunkPOS,
                            SandSimulation.Instance.chunkSize
                        );
                    }

                    if (SandSimulation.Instance.chunks.TryGetValue(chunkPOS, out Chunk targetChunkObject))
                    {
                        targetChunkObject.isActive = true;
                        targetChunkObject.isActiveNextFrame = true;

                        Vector3Int tilePosition = sawdust.GetTilemapPosition(targetPos, chunkPOS);
                        bool isEmpty = !SandSimulation.Instance.collisionTilemap.HasTile(tilePosition) &&
                                     (targetChunkObject.elements[targetPos.x, targetPos.y] == null ||
                                      targetChunkObject.elements[targetPos.x, targetPos.y].containedInObject);

                        if (SandSimulation.Instance.chunks.TryGetValue(sawdust.chunkPosition, out Chunk sandChunk))
                        {
                            sandChunk.isActive = true;
                            sandChunk.isActiveNextFrame = true;

                            if (isEmpty && sandChunk != null)
                            {
                                moves[sawdust] = (targetPos, chunkPOS);
                                totalMovement += relativeObjPos;
                            }
                            else
                            {
                                Granularize();
                                return;
                            }
                        }
                    }
                }
                else if (elementType == "flour")
                {
                    if (!element.containedInObject) continue;
                    if (isGranularized) return;

                    Flour flour = element as Flour;
                    Vector2Int chunkPOS = flour.chunkPosition;
                    Vector2 relativeObjPos;
                    Vector2Int targetPos;

                    if (moveRight && moveLeft)
                    {
                        relativeObjPos = new Vector2(0, -SandSimulation.Instance.fallSpeed);
                        targetPos = flour.AdjustPositionForChunk(
                            new Vector2Int(flour.localPosition.x, flour.localPosition.y - SandSimulation.Instance.fallSpeed),
                            ref chunkPOS,
                            SandSimulation.Instance.chunkSize
                        );
                    }
                    else if (moveRight && !outOfBoundsRight)
                    {
                        relativeObjPos = new Vector2(SandSimulation.Instance.horizontalSpeed, -SandSimulation.Instance.fallSpeed);
                        targetPos = flour.AdjustPositionForChunk(
                            new Vector2Int(flour.localPosition.x + SandSimulation.Instance.horizontalSpeed, flour.localPosition.y - SandSimulation.Instance.fallSpeed),
                            ref chunkPOS,
                            SandSimulation.Instance.chunkSize
                        );
                    }
                    else if (moveLeft && !outOfBoundsLeft)
                    {
                        relativeObjPos = new Vector2(-SandSimulation.Instance.horizontalSpeed, -SandSimulation.Instance.fallSpeed);
                        targetPos = flour.AdjustPositionForChunk(
                            new Vector2Int(flour.localPosition.x - SandSimulation.Instance.horizontalSpeed, flour.localPosition.y - SandSimulation.Instance.fallSpeed),
                            ref chunkPOS,
                            SandSimulation.Instance.chunkSize
                        );
                    }
                    else
                    {
                        relativeObjPos = new Vector2(0, -SandSimulation.Instance.fallSpeed);
                        targetPos = flour.AdjustPositionForChunk(
                            new Vector2Int(flour.localPosition.x, flour.localPosition.y - SandSimulation.Instance.fallSpeed),
                            ref chunkPOS,
                            SandSimulation.Instance.chunkSize
                        );
                    }

                    if (SandSimulation.Instance.chunks.TryGetValue(chunkPOS, out Chunk targetChunkObject))
                    {
                        targetChunkObject.isActive = true;
                        targetChunkObject.isActiveNextFrame = true;

                        Vector3Int tilePosition = flour.GetTilemapPosition(targetPos, chunkPOS);
                        bool isEmpty = !SandSimulation.Instance.collisionTilemap.HasTile(tilePosition) &&
                                     (targetChunkObject.elements[targetPos.x, targetPos.y] == null ||
                                      targetChunkObject.elements[targetPos.x, targetPos.y].containedInObject);

                        if (SandSimulation.Instance.chunks.TryGetValue(flour.chunkPosition, out Chunk sandChunk))
                        {
                            sandChunk.isActive = true;
                            sandChunk.isActiveNextFrame = true;

                            if (isEmpty && sandChunk != null)
                            {
                                moves[flour] = (targetPos, chunkPOS);
                                totalMovement += relativeObjPos;
                            }
                            else
                            {
                                Granularize();
                                return;
                            }
                        }
                    }
                }
                else if (elementType == "bricks")
                {
                    if (!element.containedInObject) continue;
                    if (isGranularized) return;

                    Bricks bricks = element as Bricks;
                    Vector2Int chunkPOS = bricks.chunkPosition;
                    Vector2 relativeObjPos;
                    Vector2Int targetPos;

                    if (moveRight && moveLeft)
                    {
                        relativeObjPos = new Vector2(0, -SandSimulation.Instance.fallSpeed);
                        targetPos = bricks.AdjustPositionForChunk(
                            new Vector2Int(bricks.localPosition.x, bricks.localPosition.y - SandSimulation.Instance.fallSpeed),
                            ref chunkPOS,
                            SandSimulation.Instance.chunkSize
                        );
                    }
                    else if (moveRight && !outOfBoundsRight)
                    {
                        relativeObjPos = new Vector2(SandSimulation.Instance.horizontalSpeed, -SandSimulation.Instance.fallSpeed);
                        targetPos = bricks.AdjustPositionForChunk(
                            new Vector2Int(bricks.localPosition.x + SandSimulation.Instance.horizontalSpeed, bricks.localPosition.y - SandSimulation.Instance.fallSpeed),
                            ref chunkPOS,
                            SandSimulation.Instance.chunkSize
                        );
                    }
                    else if (moveLeft && !outOfBoundsLeft)
                    {
                        relativeObjPos = new Vector2(-SandSimulation.Instance.horizontalSpeed, -SandSimulation.Instance.fallSpeed);
                        targetPos = bricks.AdjustPositionForChunk(
                            new Vector2Int(bricks.localPosition.x - SandSimulation.Instance.horizontalSpeed, bricks.localPosition.y - SandSimulation.Instance.fallSpeed),
                            ref chunkPOS,
                            SandSimulation.Instance.chunkSize
                        );
                    }
                    else
                    {
                        relativeObjPos = new Vector2(0, -SandSimulation.Instance.fallSpeed);
                        targetPos = bricks.AdjustPositionForChunk(
                            new Vector2Int(bricks.localPosition.x, bricks.localPosition.y - SandSimulation.Instance.fallSpeed),
                            ref chunkPOS,
                            SandSimulation.Instance.chunkSize
                        );
                    }

                    if (SandSimulation.Instance.chunks.TryGetValue(chunkPOS, out Chunk targetChunkObject))
                    {
                        targetChunkObject.isActive = true;
                        targetChunkObject.isActiveNextFrame = true;

                        Vector3Int tilePosition = bricks.GetTilemapPosition(targetPos, chunkPOS);
                        bool isEmpty = !SandSimulation.Instance.collisionTilemap.HasTile(tilePosition) &&
                                     (targetChunkObject.elements[targetPos.x, targetPos.y] == null ||
                                      targetChunkObject.elements[targetPos.x, targetPos.y].containedInObject);

                        if (SandSimulation.Instance.chunks.TryGetValue(bricks.chunkPosition, out Chunk sandChunk))
                        {
                            sandChunk.isActive = true;
                            sandChunk.isActiveNextFrame = true;

                            if (isEmpty && sandChunk != null)
                            {
                                moves[bricks] = (targetPos, chunkPOS);
                                totalMovement += relativeObjPos;
                            }
                            else
                            {
                                Granularize();
                                return;
                            }
                        }
                    }
                }

            }

            // Apply all moves at once
            foreach (var move in moves)
            {
                Chunk currentChunk = SandSimulation.Instance.chunks[move.Key.chunkPosition];
                Chunk targetChunk = SandSimulation.Instance.chunks[move.Value.chunk];
                move.Key.MoveToNewPositionCST(move.Value.pos, move.Value.chunk, currentChunk, targetChunk, move.Key.localPosition);
            }

            if (moves.Count > 0)
            {
                objectPosition += (totalMovement / (SandSimulation.Instance.chunkSize * containedElements.Count));
            }

            if(SandSimulation.Instance.fastForwardSpeed != 1) ProcessSpritePixels(rotatedSprite);
        }

        public bool CheckForOutOfBounds(Vector2Int tPos) //true = out of bounds
        {
            Vector2Int targetPos = tPos;
            Vector2Int newChunkPos;

            if(elementType == "sand")
            {
                foreach (Element e in containedElements)
                {
                    newChunkPos = e.chunkPosition;
                    targetPos = (e as Sand).AdjustPositionForChunk(
                            new Vector2Int((e as Sand).localPosition.x + targetPos.x, (e as Sand).localPosition.y + targetPos.y),
                            ref newChunkPos,
                            SandSimulation.Instance.chunkSize
                        );

                    if (!SandSimulation.Instance.chunks.ContainsKey(newChunkPos))
                    {
                        return true; // out of bounds
                    }
                }
            }
            else if(elementType == "water")
            {
                foreach (Element e in containedElements)
                {
                    newChunkPos = e.chunkPosition;
                    targetPos = (e as Water).AdjustPositionForChunk(
                            new Vector2Int((e as Water).localPosition.x + targetPos.x, (e as Water).localPosition.y + targetPos.y),
                            ref newChunkPos,
                            SandSimulation.Instance.chunkSize
                        );

                    if (!SandSimulation.Instance.chunks.ContainsKey(newChunkPos))
                    {
                        return true; // out of bounds
                    }
                }
            }
            else if(elementType == "sawdust")
            {
                foreach (Element e in containedElements)
                {
                    newChunkPos = e.chunkPosition;
                    targetPos = (e as SawDust).AdjustPositionForChunk(
                            new Vector2Int((e as SawDust).localPosition.x + targetPos.x, (e as SawDust).localPosition.y + targetPos.y),
                            ref newChunkPos,
                            SandSimulation.Instance.chunkSize
                        );

                    if (!SandSimulation.Instance.chunks.ContainsKey(newChunkPos))
                    {
                        return true; // out of bounds
                    }
                }
            }
            else if (elementType == "flour")
            {
                foreach (Element e in containedElements)
                {
                    newChunkPos = e.chunkPosition;
                    targetPos = (e as Flour).AdjustPositionForChunk(
                            new Vector2Int((e as Flour).localPosition.x + targetPos.x, (e as Flour).localPosition.y + targetPos.y),
                            ref newChunkPos,
                            SandSimulation.Instance.chunkSize
                        );

                    if (!SandSimulation.Instance.chunks.ContainsKey(newChunkPos))
                    {
                        return true; // out of bounds
                    }
                }
            }
            else if (elementType == "bricks")
            {
                foreach (Element e in containedElements)
                {
                    newChunkPos = e.chunkPosition;
                    targetPos = (e as Flour).AdjustPositionForChunk(
                            new Vector2Int((e as Bricks).localPosition.x + targetPos.x, (e as Bricks).localPosition.y + targetPos.y),
                            ref newChunkPos,
                            SandSimulation.Instance.chunkSize
                        );

                    if (!SandSimulation.Instance.chunks.ContainsKey(newChunkPos))
                    {
                        return true; // out of bounds
                    }
                }
            }

            return false; // all checks passed, not out of bounds
        }

        public override void ComputeCollisions()
        {
            
        }

        public bool isGranularized = false;

        public void Granularize()
        {
            ProcessSpritePixels(rotatedSprite);

            foreach(Element e in containedElements)
            {
                e.containedInObject = false;
                e.containingObject = null;
            }

            if(SandSimulation.Instance.gameManager != null)
            {
                SandSimulation.Instance.gameManager.TriggerNextTile();
            }

            if(timer != null)
            {
                timer.Stop();
                if(timer.ElapsedMilliseconds <= 200)
                {
                    if (SandSimulation.Instance.gameManager != null)
                    {
                        SandSimulation.Instance.gameManager.Gameover();
                    }
                }
            }

            this.moveRight = false;
            this.moveLeft = false;
            isGranularized = true;
            SandSimulation.Instance.simObjects.Remove(this);
        }

        // Handling Rotation

        private Vector2 CalculateCenter()
        {
            if (containedElements.Count == 0) return objectPosition ?? Vector2.zero;

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (Element element in containedElements)
            {
                Vector2 worldPos = new Vector2(
                    element.chunkPosition.x * SandSimulation.Instance.chunkSize + element.localPosition.x,
                    element.chunkPosition.y * SandSimulation.Instance.chunkSize + element.localPosition.y
                );

                minX = Mathf.Min(minX, worldPos.x);
                minY = Mathf.Min(minY, worldPos.y);
                maxX = Mathf.Max(maxX, worldPos.x);
                maxY = Mathf.Max(maxY, worldPos.y);
            }

            return new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
        }

        private Sprite RotateSprite(Sprite originalSprite, bool clockwise)
        {
            Texture2D originalTexture = originalSprite.texture;
            Rect rect = originalSprite.textureRect;

            // Convert rect to pixel coordinates
            int xStart = Mathf.RoundToInt(rect.x);
            int yStart = Mathf.RoundToInt(rect.y);
            int width = Mathf.RoundToInt(rect.width);
            int height = Mathf.RoundToInt(rect.height);

            // Create a new texture for the rotated sprite
            Texture2D rotatedTexture = new Texture2D(height, width);

            // Rotate the pixels
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixel = originalTexture.GetPixel(xStart + x, yStart + y);

                    // For clockwise rotation: new_x = y, new_y = width - 1 - x
                    // For counterclockwise rotation: new_x = height - 1 - y, new_y = x
                    if (clockwise)
                    {
                        rotatedTexture.SetPixel(height - 1 - y, x, pixel);
                    }
                    else
                    {
                        rotatedTexture.SetPixel(y, width - 1 - x, pixel);
                    }
                }
            }

            rotatedTexture.Apply();

            // Create a new sprite from the rotated texture
            return Sprite.Create(rotatedTexture,
                new Rect(0, 0, rotatedTexture.width, rotatedTexture.height),
                new Vector2(0.5f, 0.5f));
        }

        public bool RotateClockwise()
        {
            if (!initialized || tileShapeSprite == null) return false;

            // Store the current position
            Vector2 currentPos = objectPosition ?? Vector2.zero;

            // Clear existing elements but remember their positions
            HashSet<(Vector2Int chunk, Vector2Int local)> oldPositions = new HashSet<(Vector2Int, Vector2Int)>();
            foreach (Element element in containedElements)
            {
                oldPositions.Add((element.chunkPosition, element.localPosition));
                if (SandSimulation.Instance.chunks.TryGetValue(element.chunkPosition, out Chunk chunk))
                {
                    chunk.elements[element.localPosition.x, element.localPosition.y] = null;
                }
            }

            containedElements.Clear();

            // Rotate the sprite
            Sprite rotatedSprite = RotateSprite(tileShapeSprite, true);

            // Store the original sprite
            Sprite originalSprite = tileShapeSprite;

            // Temporarily set the rotated sprite
            tileShapeSprite = rotatedSprite;

            // Try to create new elements at the rotated positions
            ProcessSpritePixels(rotatedSprite);

            // Check if any new positions are blocked
            bool positionBlocked = false;
            foreach (Element element in containedElements)
            {
                if (!IsPositionValid(element.chunkPosition, element.localPosition, oldPositions))
                {
                    //positionBlocked = true;
                    break;
                }
            }

            if (positionBlocked)
            {
                // Rotation failed - restore original state
                containedElements.Clear();
                tileShapeSprite = originalSprite;
                ProcessSpritePixels(originalSprite);
                return false;
            }

            return true;
        }

        private bool IsPositionValid(Vector2Int chunkPos, Vector2Int localPos, HashSet<(Vector2Int, Vector2Int)> excludePositions)
        {
            // Skip check for positions that were occupied by our elements before rotation
            if (excludePositions.Contains((chunkPos, localPos)))
            {
                return true;
            }

            // Check if chunk exists
            if (!SandSimulation.Instance.chunks.TryGetValue(chunkPos, out Chunk chunk))
            {
                return false;
            }

            // Check collision with tilemap
            Vector3Int tilePos = new Vector3Int(
                chunkPos.x * SandSimulation.Instance.chunkSize + localPos.x,
                chunkPos.y * SandSimulation.Instance.chunkSize + localPos.y,
                0
            );

            if (SandSimulation.Instance.collisionTilemap.HasTile(tilePos))
            {
                return false;
            }

            // Check if position is occupied by another element
            Element existingElement = chunk.elements[localPos.x, localPos.y];
            return existingElement == null || containedElements.Contains(existingElement);
        }

        

        private void MoveElement(Element element, Vector2Int newChunkPos, Vector2Int newLocalPos)
        {
            // Remove from old chunk
            if (SandSimulation.Instance.chunks.TryGetValue(element.chunkPosition, out Chunk oldChunk))
            {
                oldChunk.elements[element.localPosition.x, element.localPosition.y] = null;
            }

            // Add to new chunk
            if (SandSimulation.Instance.chunks.TryGetValue(newChunkPos, out Chunk newChunk))
            {
                element.chunkPosition = newChunkPos;
                element.localPosition = newLocalPos;
                newChunk.AddCustomElement(newLocalPos, element);

                // Keep chunks active
                newChunk.isActive = true;
                newChunk.isActiveNextFrame = true;
            }
        }
    }

    public abstract class Element
    {
        public bool containedInObject = false;
        public SimulationObject containingObject = null;

        public Vector2Int localPosition;
        public Vector2Int chunkPosition;
        public bool isResting;
        public bool isSleeping;
        public float fallVelocity;
        public float horizontalVelocity;
        public bool hasContactBelow;
        public bool hasContactAbove;
        // virtual properties
        public virtual float maxVelocity => 5f;  // Maximum fall speed
        public virtual float velocityAbsorption => 0.5f; // How much velocity is retained on impact (0-1)
        public virtual float maxHorizontalVelocity => 3f; // Maximum horizontal speed
        public virtual float horizontalDrag => 0.2f; // Base drag in air
        public virtual float friction => 0.4f; // Additional slowdown when touching surfaces

        //color data
        public virtual bool useCustomColorData { get; set; } = false;
        private Color[] _colorData = new Color[1] { new Color(1, 1, 1) };
        public virtual Color[] colorData
        {
            get => _colorData;
            set
            {
                if (value != null && value.Length > 0)
                {
                    _colorData = value;
                    Debug.Log("value was modified to " + value.Length + " length");
                }
                else
                {
                    Debug.LogWarning("Invalid color data assigned.");
                }
            }
        }
        public Color LocalColor
        {
            get => localColor;
            set => localColor = value;
        }

        protected Color localColor { get; set; } = Color.white;
        public Color GetLocalColor() => localColor;


        public Element(Vector2Int localPos, Vector2Int chunkPos)
        {
            localPosition = localPos;
            chunkPosition = chunkPos;
            isResting = false;
            fallVelocity = 0f;
            horizontalVelocity = 0f;
            hasContactBelow = false;
            hasContactAbove = false;
        }

        public void Die()
        {
            SandSimulation.Instance.chunks[this.chunkPosition].elements[this.localPosition.x, this.localPosition.y] = null;
        }

        // In your Element class:
        public virtual bool MoveToNewPositionCST(Vector2Int newPos, Vector2Int newChunkPos, Chunk currentChunk, Chunk targetChunk, Vector2Int currentPos)
        {
            if (currentChunk == targetChunk)
            {
                currentChunk.elements[currentPos.x, currentPos.y] = null;
                currentChunk.elements[newPos.x, newPos.y] = this;
                localPosition = newPos;
                return true;
            }

            currentChunk.elements[currentPos.x, currentPos.y] = null;
            targetChunk.elements[newPos.x, newPos.y] = this;
            localPosition = newPos;
            chunkPosition = newChunkPos;
            targetChunk.isActive = true;
            return true;
        }

        public Vector2Int GetGlobalPosition()
        {
            return chunkPosition * SandSimulation.Instance.chunkSize + localPosition;
        }

        public void ApplyFriction()
        {
            if (hasContactBelow || hasContactAbove)
            {
                // Apply additional slowdown when touching surfaces
                horizontalVelocity *= (1f - friction);
            }
        }

        public bool HasSupport(Chunk currentChunk)
        {
            Vector2Int below = new Vector2Int(localPosition.x, localPosition.y - 1);
            Vector2Int targetChunk = chunkPosition;

            if (below.y < 0)
            {
                below.y = currentChunk.chunkSize - 1;
                targetChunk.y -= 1;
            }

            if (SandSimulation.Instance.chunks.TryGetValue(targetChunk, out Chunk targetChunkObj))
            {
                Vector3Int tilePosition = new Vector3Int(
                    below.x / currentChunk.chunkSize + targetChunk.x,
                    below.y / currentChunk.chunkSize + targetChunk.y,
                    0);

                return SandSimulation.Instance.collisionTilemap.HasTile(tilePosition) ||
                       targetChunkObj.elements[below.x, below.y] != null;
            }

            return false;
        }

        // Abstract method that must be implemented by derived classes
        public abstract void Simulate(Chunk currentChunk);
    }

    public abstract class PowderElement : Element
    {
        // specific for this element/particle type
        public virtual float density => 2.0f; // Default density for powder particles
        public virtual float sinkSpeed => 0.3f; // How fast the particle sinks in liquid
        public virtual float diagonalSinkSpeed => 0.15f; // How likely to sink diagonally

        //COLOR DATA
        public override bool useCustomColorData => true;
        public override Color[] colorData => new Color[]
        {
            new Color(0.94f, 0.85f, 0.53f), // Light Beige
            new Color(0.87f, 0.76f, 0.46f), // Soft Yellow Sand
            new Color(0.91f, 0.77f, 0.47f), // Light Brown Sand
            new Color(0.81f, 0.67f, 0.35f), // Desert Sand
            new Color(0.74f, 0.62f, 0.37f), // Medium Sand
            new Color(0.85f, 0.72f, 0.48f), // Warm Sand
            new Color(0.78f, 0.61f, 0.39f), // Brownish Sand
            new Color(0.92f, 0.80f, 0.47f)  // Pale Sandy Yellow
        };

        public PowderElement(Vector2Int localPos, Vector2Int chunkPos) : base(localPos, chunkPos)
        {
            
        }

        // overriden from the base Particle class
        public override float maxVelocity => 5f;  // Maximum fall speed
        public override float velocityAbsorption => 0.5f; // How much velocity is retained on impact (0-1)
        public override float maxHorizontalVelocity => 3f; // Maximum horizontal speed
        public override float horizontalDrag => 0.2f; // Base drag in air
        public override float friction => 0.4f; // Additional slowdown when touching surfaces


        private Vector2Int? lastPosition = null;
        public Chunk currentChunkValue;

        public override void Simulate(Chunk currentChunk)
        {
            currentChunkValue = currentChunk;

            //epicness

            lastPosition = localPosition;

            currentChunk.CheckElementContacts(this);
            ApplyFriction();

            //print(localPosition);

            Chunk nextMoveChunk = null;

            // Handle liquid interactions...
            Vector2Int below = new Vector2Int(localPosition.x, localPosition.y - 1);
            Vector2Int targetChunk = chunkPosition;
            below = AdjustPositionForChunk(below, ref targetChunk, currentChunk.chunkSize);

            // Try to sink straight down first
            if (TrySwapWithLiquid(below, targetChunk, currentChunk, sinkSpeed))
            {
                return;
            }

            bool shouldSink = false;
            LiquidElement liquidBelow = null;

            if (SandSimulation.Instance.chunks.TryGetValue(targetChunk, out Chunk targetChunkObj))
            {
                var particleBelow = targetChunkObj.elements[below.x, below.y];
                if (particleBelow is LiquidElement liquid)
                {
                    liquidBelow = liquid;
                    float densityDiff = density - liquid.density;
                    // Add randomness to make sinking look more natural
                    shouldSink = densityDiff > 0 && Random.value < (sinkSpeed * Mathf.Min(densityDiff, 1f));
                }
            }

            if (shouldSink && liquidBelow != null)
            {
                // Swap positions with liquid below
                if (targetChunk == chunkPosition)
                {
                    // Same chunk swap
                    currentChunk.elements[localPosition.x, localPosition.y] = liquidBelow;
                    currentChunk.elements[below.x, below.y] = this;
                    liquidBelow.localPosition = localPosition;
                    localPosition = below;
                }
                else
                {
                    // Cross-chunk swap
                    currentChunk.elements[localPosition.x, localPosition.y] = liquidBelow;
                    targetChunkObj.elements[below.x, below.y] = this;
                    liquidBelow.localPosition = localPosition;
                    liquidBelow.chunkPosition = chunkPosition;
                    localPosition = below;
                    chunkPosition = targetChunk;
                    targetChunkObj.isActive = true;
                }

                // Reduce horizontal velocity when sinking
                horizontalVelocity *= 0.8f;
                return;
            }


            if (isResting && !isSleeping)
            {
                Vector2Int belowCheck = new Vector2Int(localPosition.x, localPosition.y - 1);
                Vector2Int targetChunkCheck = chunkPosition;
                belowCheck = AdjustPositionForChunk(belowCheck, ref targetChunkCheck, currentChunk.chunkSize);

                if (SandSimulation.Instance.chunks.TryGetValue(targetChunkCheck, out Chunk targetChunkObjCheck))
                {
                    Vector3Int tilePosition = GetTilemapPosition(belowCheck, targetChunkCheck);

                    bool isEmpty = !SandSimulation.Instance.collisionTilemap.HasTile(tilePosition) &&
                                 targetChunkObjCheck.elements[belowCheck.x, belowCheck.y] == null;

                    bool shouldSleep = CanSinkInLiquid(targetChunkObjCheck.elements[belowCheck.x, belowCheck.y]);

                    if (isEmpty)
                    {
                        isResting = false;
                        fallVelocity = 0f;
                    }
                    else
                    {
                        if (shouldSleep)
                        {
                            isResting = false;
                            isSleeping = true;
                        }
                        else
                        {
                            return;
                        }
                    }
                }
            }

            currentChunk.WakeUpParticleNeighbors(localPosition, chunkPosition);
            fallVelocity = Mathf.Min(fallVelocity + SandSimulation.Instance.gravity, maxVelocity);

            if (Mathf.Abs(horizontalVelocity) > 0)
            {
                horizontalVelocity *= (1f - horizontalDrag);
                if (Mathf.Abs(horizontalVelocity) < 0.1f)
                {
                    horizontalVelocity = 0f;
                }
            }

            float totalVelocity = Mathf.Sqrt(fallVelocity * fallVelocity + horizontalVelocity * horizontalVelocity);
            int steps = Mathf.CeilToInt(totalVelocity);

            Vector2Int current = localPosition;
            Vector2Int currentChunkPos = chunkPosition;

            for (int step = 0; step < steps; step++)
            {
                Vector2Int[] potentialMoves = GetPotentialMoves(current, isResting, horizontalVelocity);
                bool moved = false;
                bool hitObstacle = false;

                foreach (Vector2Int targetPos in potentialMoves)
                {
                    Vector2Int targetChunkPos = currentChunkPos;
                    Vector2Int adjustedPos = AdjustPositionForChunk(targetPos, ref targetChunkPos, currentChunk.chunkSize);

                    if (SandSimulation.Instance.chunks.TryGetValue(targetChunkPos, out Chunk targetChunkObject))
                    {
                        Vector3Int tilePosition = GetTilemapPosition(adjustedPos, targetChunkPos);
                        nextMoveChunk = targetChunkObject;

                        bool isEmpty = !SandSimulation.Instance.collisionTilemap.HasTile(tilePosition) &&
                                     targetChunkObject.elements[adjustedPos.x, adjustedPos.y] == null;

                        if (isEmpty)
                        {
                            isSleeping = false;
                            moved = MoveToNewPosition(adjustedPos, targetChunkPos, currentChunk, targetChunkObject);
                            if (targetChunkPos != currentChunkPos)
                            {
                                UpdateLastPosition(nextMoveChunk, currentChunk);
                                return;
                            }
                            current = adjustedPos;
                            break;
                        }
                        else if (targetPos.y < current.y && targetPos.x != current.x)
                        {
                            isSleeping = true;
                            if (TrySwapWithLiquid(adjustedPos, targetChunkPos, currentChunk, diagonalSinkSpeed))
                            {
                                UpdateLastPosition(nextMoveChunk, currentChunk);
                                return;
                            }
                            hitObstacle = true;
                        }
                        else if (targetPos.y < current.y)
                        {
                            hitObstacle = true;
                        }
                    }
                }

                if (!moved)
                {
                    HandleCollision(hitObstacle);
                    return;
                }
            }

            UpdateLastPosition(nextMoveChunk, currentChunk);
        }

        private Vector2Int[] GetPotentialMoves(Vector2Int current, bool isResting, float horizontalVelocity)
        {
            if (isResting)
            {
                return new Vector2Int[] { new Vector2Int(current.x, current.y - 1) };
            }

            int horizontalDir = horizontalVelocity > 0 ? 1 : -1;
            if (Mathf.Abs(horizontalVelocity) < 0.1f) horizontalDir = 0;

            if (horizontalDir == 0)
            {
                return Random.Range(0, 2) == 0 ?
                    new Vector2Int[]
                    {
                    new Vector2Int(current.x, current.y - 1),
                    new Vector2Int(current.x - 1, current.y - 1),
                    new Vector2Int(current.x + 1, current.y - 1)
                    } :
                    new Vector2Int[]
                    {
                    new Vector2Int(current.x, current.y - 1),
                    new Vector2Int(current.x + 1, current.y - 1),
                    new Vector2Int(current.x - 1, current.y - 1)
                    };
            }

            return Random.Range(0, 2) == 0 ?
                new Vector2Int[]
                {
                new Vector2Int(current.x, current.y - 1),
                new Vector2Int(current.x + horizontalDir, current.y - 1),
                new Vector2Int(current.x + horizontalDir, current.y),
                new Vector2Int(current.x - horizontalDir, current.y - 1)
                } :
                new Vector2Int[]
                {
                new Vector2Int(current.x + horizontalDir, current.y - 1),
                new Vector2Int(current.x, current.y - 1),
                new Vector2Int(current.x + horizontalDir, current.y),
                new Vector2Int(current.x - horizontalDir, current.y - 1)
                };
        }

        private void HandleCollision(bool hitObstacle)
        {
            if (hitObstacle && fallVelocity > 0.5f)
            {
                float impactForce = fallVelocity * velocityAbsorption;
                horizontalVelocity = Random.Range(-1f, 1f) * impactForce;
                horizontalVelocity = Mathf.Clamp(horizontalVelocity, -maxHorizontalVelocity, maxHorizontalVelocity);
            }

            fallVelocity = 0f;
            isResting = (Mathf.Abs(horizontalVelocity) < 0.1f);
        }

        private void UpdateLastPosition(Chunk nextMoveChunk, Chunk currentChunk)
        {
            if(isSleeping)
            {
                nextMoveChunk.isActiveNextFrame = true;
                //currentChunk.WakeUpAllChunkNeighbors(true);
                currentChunk.WakeUpChunkNeighbors(localPosition, true);
                if(lastPosition != null) currentChunk.WakeUpChunkNeighbors(lastPosition.Value, true);
            }

            if (lastPosition != null)
            {
                if (localPosition == lastPosition)
                {
                    isResting = true;
                }
                else if (nextMoveChunk != null)
                {
                    nextMoveChunk.isActiveNextFrame = true;
                    //currentChunk.WakeUpAllChunkNeighbors(true);
                    currentChunk.WakeUpChunkNeighbors(localPosition, true);
                    if (lastPosition != null) currentChunk.WakeUpChunkNeighbors(lastPosition.Value, true);
                }
            }
            else
            {
                lastPosition = localPosition;
                if (nextMoveChunk != null)
                {
                    nextMoveChunk.isActiveNextFrame = true;
                    //currentChunk.WakeUpAllChunkNeighbors(true);
                    currentChunk.WakeUpChunkNeighbors(localPosition, true);
                }
            }
        }

        private bool CanSinkInLiquid(Element particle)
        {
            if (particle == null) return false;

            if (particle.GetType() == typeof(LiquidElement))
            {
                LiquidElement liquid = particle as LiquidElement;
                float densityDiff = density - liquid.density;

                if (densityDiff > 0)
                {
                    return true;
                }
                else { return false; }
            }
            else if (particle.GetType() == typeof(PowderElement))
            {
                return false; //powders can sink in each other too, but for now its not added yet
            }

            return false;
        }

        private bool TrySwapWithLiquid(Vector2Int targetPos, Vector2Int targetChunkPos, Chunk currentChunk, float swapChance)
        {
            if (SandSimulation.Instance.chunks.TryGetValue(targetChunkPos, out Chunk targetChunkObj))
            {
                var particle = targetChunkObj.elements[targetPos.x, targetPos.y];
                if (particle is LiquidElement liquid)
                {
                    float densityDiff = density - liquid.density;
                    if (densityDiff > 0 && Random.value < (swapChance * Mathf.Min(densityDiff, 1f)))
                    {
                        // Perform the swap
                        if (targetChunkPos == chunkPosition)
                        {
                            // Same chunk swap
                            currentChunk.elements[localPosition.x, localPosition.y] = liquid;
                            currentChunk.elements[targetPos.x, targetPos.y] = this;
                            liquid.localPosition = localPosition;
                            localPosition = targetPos;
                        }
                        else
                        {
                            // Cross-chunk swap
                            currentChunk.elements[localPosition.x, localPosition.y] = liquid;
                            targetChunkObj.elements[targetPos.x, targetPos.y] = this;
                            liquid.localPosition = localPosition;
                            liquid.chunkPosition = chunkPosition;
                            localPosition = targetPos;
                            chunkPosition = targetChunkPos;
                            targetChunkObj.isActive = true;
                        }
                        // Reduce horizontal velocity when sinking
                        horizontalVelocity *= 0.8f;
                        return true;
                    }
                    else
                    {
                        currentChunk.WakeUpChunkNeighbors(localPosition, true);
                    }
                }
            }
            return false;
        }

        public Vector2Int AdjustPositionForChunk(Vector2Int position, ref Vector2Int chunkPos, int chunkSize)
        {
            Vector2Int adjustedPos = position;

            // Handle vertical boundaries
            if (adjustedPos.y < 0)
            {
                adjustedPos.y = chunkSize - 1;
                chunkPos.y -= 1;
            }
            else if (adjustedPos.y >= chunkSize)
            {
                adjustedPos.y = 0;
                chunkPos.y += 1;
            }

            // Handle horizontal boundaries
            if (adjustedPos.x < 0)
            {
                adjustedPos.x = chunkSize - 1;
                chunkPos.x -= 1;
            }
            else if (adjustedPos.x >= chunkSize)
            {
                adjustedPos.x = 0;
                chunkPos.x += 1;
            }

            return adjustedPos;
        }

        public Vector3Int GetTilemapPosition(Vector2Int localPos, Vector2Int chunkPos)
        {
            // Simply use the chunk position as the tilemap position since 1 tile = 1 chunk
            return new Vector3Int(chunkPos.x, chunkPos.y, 0);
        }

        public bool MoveToNewPosition(Vector2Int newPos, Vector2Int newChunkPos, Chunk currentChunk, Chunk targetChunk)
        {
            if (currentChunk == targetChunk)
            {
                // Same chunk movement
                currentChunk.elements[localPosition.x, localPosition.y] = null;
                currentChunk.elements[newPos.x, newPos.y] = this;
                localPosition = newPos;
                return true;
            }
            else
            {
                // Cross-chunk movement
                currentChunk.elements[localPosition.x, localPosition.y] = null;
                targetChunk.elements[newPos.x, newPos.y] = this;
                localPosition = newPos;
                chunkPosition = newChunkPos;
                targetChunk.isActive = true;
                return true;
            }
        }

        public bool MoveToNewPositionCST(Vector2Int newPos, Vector2Int newChunkPos, Chunk currentChunk, Chunk targetChunk, Vector2Int currentPos)
        {
            if (currentChunk == targetChunk)
            {
                // Same chunk movement
                currentChunk.elements[currentPos.x, currentPos.y] = null;
                currentChunk.elements[newPos.x, newPos.y] = this;
                localPosition = newPos;
                return true;
            }
            else
            {
                // Cross-chunk movement
                currentChunk.elements[currentPos.x, currentPos.y] = null;
                targetChunk.elements[newPos.x, newPos.y] = this;
                localPosition = newPos;
                chunkPosition = newChunkPos;
                targetChunk.isActive = true;
                return true;
            }
        }
    }

    public abstract class SolidElement : Element
    {
        // specific for this element/particle type
        public virtual float density => 2.0f; // Default density for powder particles
        public virtual float sinkSpeed => 0.3f; // How fast the particle sinks in liquid
        public virtual float diagonalSinkSpeed => 0.15f; // How likely to sink diagonally

        //COLOR DATA
        public override bool useCustomColorData => true;
        public override Color[] colorData => new Color[]
        {
            new Color(0.94f, 0.85f, 0.53f), // Light Beige
            new Color(0.87f, 0.76f, 0.46f), // Soft Yellow Sand
            new Color(0.91f, 0.77f, 0.47f), // Light Brown Sand
            new Color(0.81f, 0.67f, 0.35f), // Desert Sand
            new Color(0.74f, 0.62f, 0.37f), // Medium Sand
            new Color(0.85f, 0.72f, 0.48f), // Warm Sand
            new Color(0.78f, 0.61f, 0.39f), // Brownish Sand
            new Color(0.92f, 0.80f, 0.47f)  // Pale Sandy Yellow
        };

        public SolidElement(Vector2Int localPos, Vector2Int chunkPos) : base(localPos, chunkPos)
        {

        }

        // overriden from the base Particle class
        public override float maxVelocity => 5f;  // Maximum fall speed
        public override float velocityAbsorption => 0.5f; // How much velocity is retained on impact (0-1)
        public override float maxHorizontalVelocity => 3f; // Maximum horizontal speed
        public override float horizontalDrag => 0.2f; // Base drag in air
        public override float friction => 0.4f; // Additional slowdown when touching surfaces


        private Vector2Int? lastPosition = null;
        public Chunk currentChunkValue;

        public override void Simulate(Chunk currentChunk)
        {
            
        }

        private Vector2Int[] GetPotentialMoves(Vector2Int current, bool isResting, float horizontalVelocity)
        {
            if (isResting)
            {
                return new Vector2Int[] { new Vector2Int(current.x, current.y - 1) };
            }

            int horizontalDir = horizontalVelocity > 0 ? 1 : -1;
            if (Mathf.Abs(horizontalVelocity) < 0.1f) horizontalDir = 0;

            if (horizontalDir == 0)
            {
                return Random.Range(0, 2) == 0 ?
                    new Vector2Int[]
                    {
                    new Vector2Int(current.x, current.y - 1),
                    new Vector2Int(current.x - 1, current.y - 1),
                    new Vector2Int(current.x + 1, current.y - 1)
                    } :
                    new Vector2Int[]
                    {
                    new Vector2Int(current.x, current.y - 1),
                    new Vector2Int(current.x + 1, current.y - 1),
                    new Vector2Int(current.x - 1, current.y - 1)
                    };
            }

            return Random.Range(0, 2) == 0 ?
                new Vector2Int[]
                {
                new Vector2Int(current.x, current.y - 1),
                new Vector2Int(current.x + horizontalDir, current.y - 1),
                new Vector2Int(current.x + horizontalDir, current.y),
                new Vector2Int(current.x - horizontalDir, current.y - 1)
                } :
                new Vector2Int[]
                {
                new Vector2Int(current.x + horizontalDir, current.y - 1),
                new Vector2Int(current.x, current.y - 1),
                new Vector2Int(current.x + horizontalDir, current.y),
                new Vector2Int(current.x - horizontalDir, current.y - 1)
                };
        }

        private void HandleCollision(bool hitObstacle)
        {
            if (hitObstacle && fallVelocity > 0.5f)
            {
                float impactForce = fallVelocity * velocityAbsorption;
                horizontalVelocity = Random.Range(-1f, 1f) * impactForce;
                horizontalVelocity = Mathf.Clamp(horizontalVelocity, -maxHorizontalVelocity, maxHorizontalVelocity);
            }

            fallVelocity = 0f;
            isResting = (Mathf.Abs(horizontalVelocity) < 0.1f);
        }

        private void UpdateLastPosition(Chunk nextMoveChunk, Chunk currentChunk)
        {
            if (isSleeping)
            {
                nextMoveChunk.isActiveNextFrame = true;
                //currentChunk.WakeUpAllChunkNeighbors(true);
                currentChunk.WakeUpChunkNeighbors(localPosition, true);
                if (lastPosition != null) currentChunk.WakeUpChunkNeighbors(lastPosition.Value, true);
            }

            if (lastPosition != null)
            {
                if (localPosition == lastPosition)
                {
                    isResting = true;
                }
                else if (nextMoveChunk != null)
                {
                    nextMoveChunk.isActiveNextFrame = true;
                    //currentChunk.WakeUpAllChunkNeighbors(true);
                    currentChunk.WakeUpChunkNeighbors(localPosition, true);
                    if (lastPosition != null) currentChunk.WakeUpChunkNeighbors(lastPosition.Value, true);
                }
            }
            else
            {
                lastPosition = localPosition;
                if (nextMoveChunk != null)
                {
                    nextMoveChunk.isActiveNextFrame = true;
                    //currentChunk.WakeUpAllChunkNeighbors(true);
                    currentChunk.WakeUpChunkNeighbors(localPosition, true);
                }
            }
        }

        private bool CanSinkInLiquid(Element particle)
        {
            if (particle == null) return false;

            if (particle.GetType() == typeof(LiquidElement))
            {
                LiquidElement liquid = particle as LiquidElement;
                float densityDiff = density - liquid.density;

                if (densityDiff > 0)
                {
                    return true;
                }
                else { return false; }
            }
            else if (particle.GetType() == typeof(PowderElement))
            {
                return false; //powders can sink in each other too, but for now its not added yet
            }

            return false;
        }

        private bool TrySwapWithLiquid(Vector2Int targetPos, Vector2Int targetChunkPos, Chunk currentChunk, float swapChance)
        {
            if (SandSimulation.Instance.chunks.TryGetValue(targetChunkPos, out Chunk targetChunkObj))
            {
                var particle = targetChunkObj.elements[targetPos.x, targetPos.y];
                if (particle is LiquidElement liquid)
                {
                    float densityDiff = density - liquid.density;
                    if (densityDiff > 0 && Random.value < (swapChance * Mathf.Min(densityDiff, 1f)))
                    {
                        // Perform the swap
                        if (targetChunkPos == chunkPosition)
                        {
                            // Same chunk swap
                            currentChunk.elements[localPosition.x, localPosition.y] = liquid;
                            currentChunk.elements[targetPos.x, targetPos.y] = this;
                            liquid.localPosition = localPosition;
                            localPosition = targetPos;
                        }
                        else
                        {
                            // Cross-chunk swap
                            currentChunk.elements[localPosition.x, localPosition.y] = liquid;
                            targetChunkObj.elements[targetPos.x, targetPos.y] = this;
                            liquid.localPosition = localPosition;
                            liquid.chunkPosition = chunkPosition;
                            localPosition = targetPos;
                            chunkPosition = targetChunkPos;
                            targetChunkObj.isActive = true;
                        }
                        // Reduce horizontal velocity when sinking
                        horizontalVelocity *= 0.8f;
                        return true;
                    }
                    else
                    {
                        currentChunk.WakeUpChunkNeighbors(localPosition, true);
                    }
                }
            }
            return false;
        }

        public Vector2Int AdjustPositionForChunk(Vector2Int position, ref Vector2Int chunkPos, int chunkSize)
        {
            Vector2Int adjustedPos = position;

            // Handle vertical boundaries
            if (adjustedPos.y < 0)
            {
                adjustedPos.y = chunkSize - 1;
                chunkPos.y -= 1;
            }
            else if (adjustedPos.y >= chunkSize)
            {
                adjustedPos.y = 0;
                chunkPos.y += 1;
            }

            // Handle horizontal boundaries
            if (adjustedPos.x < 0)
            {
                adjustedPos.x = chunkSize - 1;
                chunkPos.x -= 1;
            }
            else if (adjustedPos.x >= chunkSize)
            {
                adjustedPos.x = 0;
                chunkPos.x += 1;
            }

            return adjustedPos;
        }

        public Vector3Int GetTilemapPosition(Vector2Int localPos, Vector2Int chunkPos)
        {
            // Simply use the chunk position as the tilemap position since 1 tile = 1 chunk
            return new Vector3Int(chunkPos.x, chunkPos.y, 0);
        }

        public bool MoveToNewPosition(Vector2Int newPos, Vector2Int newChunkPos, Chunk currentChunk, Chunk targetChunk)
        {
            if (currentChunk == targetChunk)
            {
                // Same chunk movement
                currentChunk.elements[localPosition.x, localPosition.y] = null;
                currentChunk.elements[newPos.x, newPos.y] = this;
                localPosition = newPos;
                return true;
            }
            else
            {
                // Cross-chunk movement
                currentChunk.elements[localPosition.x, localPosition.y] = null;
                targetChunk.elements[newPos.x, newPos.y] = this;
                localPosition = newPos;
                chunkPosition = newChunkPos;
                targetChunk.isActive = true;
                return true;
            }
        }

        public bool MoveToNewPositionCST(Vector2Int newPos, Vector2Int newChunkPos, Chunk currentChunk, Chunk targetChunk, Vector2Int currentPos)
        {
            if (currentChunk == targetChunk)
            {
                // Same chunk movement
                currentChunk.elements[currentPos.x, currentPos.y] = null;
                currentChunk.elements[newPos.x, newPos.y] = this;
                localPosition = newPos;
                return true;
            }
            else
            {
                // Cross-chunk movement
                currentChunk.elements[currentPos.x, currentPos.y] = null;
                targetChunk.elements[newPos.x, newPos.y] = this;
                localPosition = newPos;
                chunkPosition = newChunkPos;
                targetChunk.isActive = true;
                return true;
            }
        }
    }

    public abstract class LightPowderElement : Element
    {
        // specific for this element/particle type
        public virtual float density => 2.0f; // Default density for powder particles
        public virtual float sinkSpeed => 0.3f; // How fast the particle sinks in liquid
        public virtual float diagonalSinkSpeed => 0.15f; // How likely to sink diagonally

        //COLOR DATA
        public override bool useCustomColorData => true;
        public override Color[] colorData => new Color[]
        {
            new Color(0.94f, 0.85f, 0.53f), // Light Beige
            new Color(0.87f, 0.76f, 0.46f), // Soft Yellow Sand
            new Color(0.91f, 0.77f, 0.47f), // Light Brown Sand
            new Color(0.81f, 0.67f, 0.35f), // Desert Sand
            new Color(0.74f, 0.62f, 0.37f), // Medium Sand
            new Color(0.85f, 0.72f, 0.48f), // Warm Sand
            new Color(0.78f, 0.61f, 0.39f), // Brownish Sand
            new Color(0.92f, 0.80f, 0.47f)  // Pale Sandy Yellow
        };

        public LightPowderElement(Vector2Int localPos, Vector2Int chunkPos) : base(localPos, chunkPos)
        {

        }

        // overriden from the base Particle class
        public override float maxVelocity => 5f;  // Maximum fall speed
        public override float velocityAbsorption => 0.5f; // How much velocity is retained on impact (0-1)
        public override float maxHorizontalVelocity => 1f; // Maximum horizontal speed
        public override float horizontalDrag => 0.4f; // Base drag in air
        public override float friction => 0.7f; // Additional slowdown when touching surfaces


        private Vector2Int? lastPosition = null;
        public Chunk currentChunkValue;

        public override void Simulate(Chunk currentChunk)
        {
            currentChunkValue = currentChunk;

            //epicness

            lastPosition = localPosition;

            currentChunk.CheckElementContacts(this);
            ApplyFriction();

            //print(localPosition);

            Chunk nextMoveChunk = null;

            // Handle liquid interactions...
            Vector2Int below = new Vector2Int(localPosition.x, localPosition.y - 1);
            Vector2Int targetChunk = chunkPosition;
            below = AdjustPositionForChunk(below, ref targetChunk, currentChunk.chunkSize);

            // Try to sink straight down first
            if (TrySwapWithLiquid(below, targetChunk, currentChunk, sinkSpeed))
            {
                return;
            }

            bool shouldSink = false;
            LiquidElement liquidBelow = null;

            if (SandSimulation.Instance.chunks.TryGetValue(targetChunk, out Chunk targetChunkObj))
            {
                var particleBelow = targetChunkObj.elements[below.x, below.y];
                if (particleBelow is LiquidElement liquid)
                {
                    liquidBelow = liquid;
                    float densityDiff = density - liquid.density;
                    // Add randomness to make sinking look more natural
                    shouldSink = densityDiff > 0 && Random.value < (sinkSpeed * Mathf.Min(densityDiff, 1f));
                }
            }

            if (shouldSink && liquidBelow != null)
            {
                // Swap positions with liquid below
                if (targetChunk == chunkPosition)
                {
                    // Same chunk swap
                    currentChunk.elements[localPosition.x, localPosition.y] = liquidBelow;
                    currentChunk.elements[below.x, below.y] = this;
                    liquidBelow.localPosition = localPosition;
                    localPosition = below;
                }
                else
                {
                    // Cross-chunk swap
                    currentChunk.elements[localPosition.x, localPosition.y] = liquidBelow;
                    targetChunkObj.elements[below.x, below.y] = this;
                    liquidBelow.localPosition = localPosition;
                    liquidBelow.chunkPosition = chunkPosition;
                    localPosition = below;
                    chunkPosition = targetChunk;
                    targetChunkObj.isActive = true;
                }

                // Reduce horizontal velocity when sinking
                horizontalVelocity *= 0.8f;
                return;
            }


            if (isResting && !isSleeping)
            {
                Vector2Int belowCheck = new Vector2Int(localPosition.x, localPosition.y - 1);
                Vector2Int targetChunkCheck = chunkPosition;
                belowCheck = AdjustPositionForChunk(belowCheck, ref targetChunkCheck, currentChunk.chunkSize);

                if (SandSimulation.Instance.chunks.TryGetValue(targetChunkCheck, out Chunk targetChunkObjCheck))
                {
                    Vector3Int tilePosition = GetTilemapPosition(belowCheck, targetChunkCheck);

                    bool isEmpty = !SandSimulation.Instance.collisionTilemap.HasTile(tilePosition) &&
                                 targetChunkObjCheck.elements[belowCheck.x, belowCheck.y] == null;

                    bool shouldSleep = CanSinkInLiquid(targetChunkObjCheck.elements[belowCheck.x, belowCheck.y]);

                    if (isEmpty)
                    {
                        isResting = false;
                        fallVelocity = 0f;
                    }
                    else
                    {
                        if (shouldSleep)
                        {
                            isResting = false;
                            isSleeping = true;
                        }
                        else
                        {
                            return;
                        }
                    }
                }
            }

            currentChunk.WakeUpParticleNeighbors(localPosition, chunkPosition);
            fallVelocity = Mathf.Min(fallVelocity + SandSimulation.Instance.gravity, maxVelocity);

            if (Mathf.Abs(horizontalVelocity) > 0)
            {
                horizontalVelocity *= (1f - horizontalDrag);
                if (Mathf.Abs(horizontalVelocity) < 0.1f)
                {
                    horizontalVelocity = 0f;
                }
            }

            float totalVelocity = Mathf.Sqrt(fallVelocity * fallVelocity + horizontalVelocity * horizontalVelocity);
            int steps = Mathf.CeilToInt(totalVelocity);

            Vector2Int current = localPosition;
            Vector2Int currentChunkPos = chunkPosition;

            for (int step = 0; step < steps; step++)
            {
                Vector2Int[] potentialMoves = GetPotentialMoves(current, isResting, horizontalVelocity);
                bool moved = false;
                bool hitObstacle = false;

                foreach (Vector2Int targetPos in potentialMoves)
                {
                    Vector2Int targetChunkPos = currentChunkPos;
                    Vector2Int adjustedPos = AdjustPositionForChunk(targetPos, ref targetChunkPos, currentChunk.chunkSize);

                    if (SandSimulation.Instance.chunks.TryGetValue(targetChunkPos, out Chunk targetChunkObject))
                    {
                        Vector3Int tilePosition = GetTilemapPosition(adjustedPos, targetChunkPos);
                        nextMoveChunk = targetChunkObject;

                        bool isEmpty = !SandSimulation.Instance.collisionTilemap.HasTile(tilePosition) &&
                                     targetChunkObject.elements[adjustedPos.x, adjustedPos.y] == null;

                        if (isEmpty)
                        {
                            isSleeping = false;
                            moved = MoveToNewPosition(adjustedPos, targetChunkPos, currentChunk, targetChunkObject);
                            if (targetChunkPos != currentChunkPos)
                            {
                                UpdateLastPosition(nextMoveChunk, currentChunk);
                                return;
                            }
                            current = adjustedPos;
                            break;
                        }
                        else if (targetPos.y < current.y && targetPos.x != current.x)
                        {
                            isSleeping = true;
                            if (TrySwapWithLiquid(adjustedPos, targetChunkPos, currentChunk, diagonalSinkSpeed))
                            {
                                UpdateLastPosition(nextMoveChunk, currentChunk);
                                return;
                            }
                            hitObstacle = true;
                        }
                        else if (targetPos.y < current.y)
                        {
                            hitObstacle = true;
                        }
                    }
                }

                if (!moved)
                {
                    HandleCollision(hitObstacle);
                    return;
                }
            }

            UpdateLastPosition(nextMoveChunk, currentChunk);
        }

        private Vector2Int[] GetPotentialMoves(Vector2Int current, bool isResting, float horizontalVelocity)
        {
            if (isResting)
            {
                // Only try to move straight down when at rest
                return new Vector2Int[] { new Vector2Int(current.x, current.y - 1) };
            }

            int horizontalDir = horizontalVelocity > 0 ? 1 : -1;
            if (Mathf.Abs(horizontalVelocity) < 0.1f) horizontalDir = 0;

            // Add randomization to reduce diagonal spreading
            float spreadChance = 0.5f; // Adjust this value to control spread speed (lower = slower spread)

            if (Random.value > spreadChance)
            {
                // More often just try to move straight down
                return new Vector2Int[] { new Vector2Int(current.x, current.y - 1) };
            }

            if (horizontalDir == 0)
            {
                // Reduced probability of diagonal movement when no horizontal velocity
                if (Random.value < 0.5f) // 30% chance of trying diagonal moves
                {
                    return Random.Range(0, 2) == 0 ?
                        new Vector2Int[]
                        {
                    new Vector2Int(current.x, current.y - 1),
                    new Vector2Int(current.x - 1, current.y - 1),
                    new Vector2Int(current.x + 1, current.y - 1)
                        } :
                        new Vector2Int[]
                        {
                    new Vector2Int(current.x, current.y - 1),
                    new Vector2Int(current.x + 1, current.y - 1),
                    new Vector2Int(current.x - 1, current.y - 1)
                        };
                }
                else
                {
                    return new Vector2Int[] { new Vector2Int(current.x, current.y - 1) };
                }
            }

            // When there is horizontal velocity, still prefer vertical movement
            return Random.value < 0.5f ? // 40% chance of including horizontal movement
                new Vector2Int[]
                {
            new Vector2Int(current.x, current.y - 1),
            new Vector2Int(current.x + horizontalDir, current.y - 1),
            new Vector2Int(current.x + horizontalDir, current.y),
                } :
                new Vector2Int[]
                {
            new Vector2Int(current.x, current.y - 1)
                };
        }

        private void HandleCollision(bool hitObstacle)
        {
            if (hitObstacle && fallVelocity > 0.5f)
            {
                float impactForce = fallVelocity * velocityAbsorption * 0.5f; // Added 0.5f multiplier to reduce spread
                horizontalVelocity = Random.Range(-0.5f, 0.5f) * impactForce; // Reduced from (-1f, 1f)
                horizontalVelocity = Mathf.Clamp(horizontalVelocity, -maxHorizontalVelocity, maxHorizontalVelocity);
            }

            fallVelocity = 0f;
            isResting = (Mathf.Abs(horizontalVelocity) < 0.1f);
        }

        private void UpdateLastPosition(Chunk nextMoveChunk, Chunk currentChunk)
        {
            if (isSleeping)
            {
                nextMoveChunk.isActiveNextFrame = true;
                //currentChunk.WakeUpAllChunkNeighbors(true);
                currentChunk.WakeUpChunkNeighbors(localPosition, true);
                if (lastPosition != null) currentChunk.WakeUpChunkNeighbors(lastPosition.Value, true);
            }

            if (lastPosition != null)
            {
                if (localPosition == lastPosition)
                {
                    isResting = true;
                }
                else if (nextMoveChunk != null)
                {
                    nextMoveChunk.isActiveNextFrame = true;
                    //currentChunk.WakeUpAllChunkNeighbors(true);
                    currentChunk.WakeUpChunkNeighbors(localPosition, true);
                    if (lastPosition != null) currentChunk.WakeUpChunkNeighbors(lastPosition.Value, true);
                }
            }
            else
            {
                lastPosition = localPosition;
                if (nextMoveChunk != null)
                {
                    nextMoveChunk.isActiveNextFrame = true;
                    //currentChunk.WakeUpAllChunkNeighbors(true);
                    currentChunk.WakeUpChunkNeighbors(localPosition, true);
                }
            }
        }

        private bool CanSinkInLiquid(Element particle)
        {
            if (particle == null) return false;

            if (particle.GetType() == typeof(LiquidElement))
            {
                LiquidElement liquid = particle as LiquidElement;
                float densityDiff = density - liquid.density;

                if (densityDiff > 0)
                {
                    return true;
                }
                else { return false; }
            }
            else if (particle.GetType() == typeof(PowderElement))
            {
                return false; //powders can sink in each other too, but for now its not added yet
            }

            return false;
        }

        private bool TrySwapWithLiquid(Vector2Int targetPos, Vector2Int targetChunkPos, Chunk currentChunk, float swapChance)
        {
            if (SandSimulation.Instance.chunks.TryGetValue(targetChunkPos, out Chunk targetChunkObj))
            {
                var particle = targetChunkObj.elements[targetPos.x, targetPos.y];
                if (particle is LiquidElement liquid)
                {
                    float densityDiff = density - liquid.density;
                    if (densityDiff > 0 && Random.value < (swapChance * Mathf.Min(densityDiff, 1f)))
                    {
                        // Perform the swap
                        if (targetChunkPos == chunkPosition)
                        {
                            // Same chunk swap
                            currentChunk.elements[localPosition.x, localPosition.y] = liquid;
                            currentChunk.elements[targetPos.x, targetPos.y] = this;
                            liquid.localPosition = localPosition;
                            localPosition = targetPos;
                        }
                        else
                        {
                            // Cross-chunk swap
                            currentChunk.elements[localPosition.x, localPosition.y] = liquid;
                            targetChunkObj.elements[targetPos.x, targetPos.y] = this;
                            liquid.localPosition = localPosition;
                            liquid.chunkPosition = chunkPosition;
                            localPosition = targetPos;
                            chunkPosition = targetChunkPos;
                            targetChunkObj.isActive = true;
                        }
                        // Reduce horizontal velocity when sinking
                        horizontalVelocity *= 0.8f;
                        return true;
                    }
                    else
                    {
                        currentChunk.WakeUpChunkNeighbors(localPosition, true);
                    }
                }
            }
            return false;
        }

        public Vector2Int AdjustPositionForChunk(Vector2Int position, ref Vector2Int chunkPos, int chunkSize)
        {
            Vector2Int adjustedPos = position;

            // Handle vertical boundaries
            if (adjustedPos.y < 0)
            {
                adjustedPos.y = chunkSize - 1;
                chunkPos.y -= 1;
            }
            else if (adjustedPos.y >= chunkSize)
            {
                adjustedPos.y = 0;
                chunkPos.y += 1;
            }

            // Handle horizontal boundaries
            if (adjustedPos.x < 0)
            {
                adjustedPos.x = chunkSize - 1;
                chunkPos.x -= 1;
            }
            else if (adjustedPos.x >= chunkSize)
            {
                adjustedPos.x = 0;
                chunkPos.x += 1;
            }

            return adjustedPos;
        }

        public Vector3Int GetTilemapPosition(Vector2Int localPos, Vector2Int chunkPos)
        {
            // Simply use the chunk position as the tilemap position since 1 tile = 1 chunk
            return new Vector3Int(chunkPos.x, chunkPos.y, 0);
        }

        public bool MoveToNewPosition(Vector2Int newPos, Vector2Int newChunkPos, Chunk currentChunk, Chunk targetChunk)
        {
            if (currentChunk == targetChunk)
            {
                // Same chunk movement
                currentChunk.elements[localPosition.x, localPosition.y] = null;
                currentChunk.elements[newPos.x, newPos.y] = this;
                localPosition = newPos;
                return true;
            }
            else
            {
                // Cross-chunk movement
                currentChunk.elements[localPosition.x, localPosition.y] = null;
                targetChunk.elements[newPos.x, newPos.y] = this;
                localPosition = newPos;
                chunkPosition = newChunkPos;
                targetChunk.isActive = true;
                return true;
            }
        }

        public bool MoveToNewPositionCST(Vector2Int newPos, Vector2Int newChunkPos, Chunk currentChunk, Chunk targetChunk, Vector2Int currentPos)
        {
            if (currentChunk == targetChunk)
            {
                // Same chunk movement
                currentChunk.elements[currentPos.x, currentPos.y] = null;
                currentChunk.elements[newPos.x, newPos.y] = this;
                localPosition = newPos;
                return true;
            }
            else
            {
                // Cross-chunk movement
                currentChunk.elements[currentPos.x, currentPos.y] = null;
                targetChunk.elements[newPos.x, newPos.y] = this;
                localPosition = newPos;
                chunkPosition = newChunkPos;
                targetChunk.isActive = true;
                return true;
            }
        }
    }

    public abstract class DustElement : Element
    {
        // specific for this element/particle type
        public virtual float density => 0.1f; // Default density for powder particles
        public virtual float sinkSpeed => 0.02f; // How fast the particle sinks in liquid
        public virtual float diagonalSinkSpeed => 0.15f; // How likely to sink diagonally
        public virtual float gravityReduce => 0.15f; // How much to reduce gravity

        //COLOR DATA
        public override bool useCustomColorData => true;
        public override Color[] colorData => new Color[]
        {
            new Color(0.94f, 0.85f, 0.53f), // Light Beige
            new Color(0.87f, 0.76f, 0.46f), // Soft Yellow Sand
            new Color(0.91f, 0.77f, 0.47f), // Light Brown Sand
            new Color(0.81f, 0.67f, 0.35f), // Desert Sand
            new Color(0.74f, 0.62f, 0.37f), // Medium Sand
            new Color(0.85f, 0.72f, 0.48f), // Warm Sand
            new Color(0.78f, 0.61f, 0.39f), // Brownish Sand
            new Color(0.92f, 0.80f, 0.47f)  // Pale Sandy Yellow
        };

        public DustElement(Vector2Int localPos, Vector2Int chunkPos) : base(localPos, chunkPos)
        {

        }

        // overriden from the base Particle class
        public override float maxVelocity => 5f;  // Maximum fall speed
        public override float velocityAbsorption => 0.8f; // How much velocity is retained on impact (0-1)
        public override float maxHorizontalVelocity => 3f; // Maximum horizontal speed
        public override float horizontalDrag => 0.2f; // Base drag in air
        public override float friction => 0.4f; // Additional slowdown when touching surfaces


        private Vector2Int? lastPosition = null;
        public Chunk currentChunkValue;

        public override void Simulate(Chunk currentChunk)
        {
            currentChunkValue = currentChunk;

            //epicness

            lastPosition = localPosition;

            currentChunk.CheckElementContacts(this);
            ApplyFriction();

            //print(localPosition);

            Chunk nextMoveChunk = null;

            // Handle liquid interactions...
            Vector2Int below = new Vector2Int(localPosition.x, localPosition.y - 1);
            Vector2Int targetChunk = chunkPosition;
            below = AdjustPositionForChunk(below, ref targetChunk, currentChunk.chunkSize);

            // Try to sink straight down first
            if (TrySwapWithLiquid(below, targetChunk, currentChunk, sinkSpeed))
            {
                return;
            }

            bool shouldSink = false;
            LiquidElement liquidBelow = null;

            if (SandSimulation.Instance.chunks.TryGetValue(targetChunk, out Chunk targetChunkObj))
            {
                var particleBelow = targetChunkObj.elements[below.x, below.y];
                if (particleBelow is LiquidElement liquid)
                {
                    liquidBelow = liquid;
                    float densityDiff = density - liquid.density;
                    // Add randomness to make sinking look more natural
                    shouldSink = densityDiff > 0 && Random.value < (sinkSpeed * Mathf.Min(densityDiff, 1f));
                }
            }

            if (shouldSink && liquidBelow != null)
            {
                // Swap positions with liquid below
                if (targetChunk == chunkPosition)
                {
                    // Same chunk swap
                    currentChunk.elements[localPosition.x, localPosition.y] = liquidBelow;
                    currentChunk.elements[below.x, below.y] = this;
                    liquidBelow.localPosition = localPosition;
                    localPosition = below;
                }
                else
                {
                    // Cross-chunk swap
                    currentChunk.elements[localPosition.x, localPosition.y] = liquidBelow;
                    targetChunkObj.elements[below.x, below.y] = this;
                    liquidBelow.localPosition = localPosition;
                    liquidBelow.chunkPosition = chunkPosition;
                    localPosition = below;
                    chunkPosition = targetChunk;
                    targetChunkObj.isActive = true;
                }

                // Reduce horizontal velocity when sinking
                horizontalVelocity *= 0.8f;
                return;
            }


            if (isResting && !isSleeping)
            {
                Vector2Int belowCheck = new Vector2Int(localPosition.x, localPosition.y - 1);
                Vector2Int targetChunkCheck = chunkPosition;
                belowCheck = AdjustPositionForChunk(belowCheck, ref targetChunkCheck, currentChunk.chunkSize);

                if (SandSimulation.Instance.chunks.TryGetValue(targetChunkCheck, out Chunk targetChunkObjCheck))
                {
                    Vector3Int tilePosition = GetTilemapPosition(belowCheck, targetChunkCheck);

                    bool isEmpty = !SandSimulation.Instance.collisionTilemap.HasTile(tilePosition) &&
                                 targetChunkObjCheck.elements[belowCheck.x, belowCheck.y] == null;

                    bool shouldSleep = CanSinkInLiquid(targetChunkObjCheck.elements[belowCheck.x, belowCheck.y]);

                    if (isEmpty)
                    {
                        isResting = false;
                        fallVelocity = 0f;
                    }
                    else
                    {
                        if (shouldSleep)
                        {
                            isResting = false;
                            isSleeping = true;
                        }
                        else
                        {
                            return;
                        }
                    }
                }
            }

            currentChunk.WakeUpParticleNeighbors(localPosition, chunkPosition);
            fallVelocity = Mathf.Min(fallVelocity + (SandSimulation.Instance.gravity * gravityReduce), maxVelocity);

            if (Mathf.Abs(horizontalVelocity) > 0)
            {
                horizontalVelocity *= (1f - horizontalDrag);
                if (Mathf.Abs(horizontalVelocity) < 0.1f)
                {
                    horizontalVelocity = 0f;
                }
            }

            float totalVelocity = Mathf.Sqrt(fallVelocity * fallVelocity + horizontalVelocity * horizontalVelocity);
            int steps = Mathf.CeilToInt(totalVelocity);

            Vector2Int current = localPosition;
            Vector2Int currentChunkPos = chunkPosition;

            for (int step = 0; step < steps; step++)
            {
                Vector2Int[] potentialMoves = GetPotentialMoves(current, isResting, horizontalVelocity);
                potentialMoves = ArrayUtility.Shuffle(potentialMoves);
                bool moved = false;
                bool hitObstacle = false;

                foreach (Vector2Int targetPos in potentialMoves)
                {
                    Vector2Int targetChunkPos = currentChunkPos;
                    Vector2Int adjustedPos = AdjustPositionForChunk(targetPos, ref targetChunkPos, currentChunk.chunkSize);

                    if (SandSimulation.Instance.chunks.TryGetValue(targetChunkPos, out Chunk targetChunkObject))
                    {
                        Vector3Int tilePosition = GetTilemapPosition(adjustedPos, targetChunkPos);
                        nextMoveChunk = targetChunkObject;

                        bool isEmpty = !SandSimulation.Instance.collisionTilemap.HasTile(tilePosition) &&
                                     targetChunkObject.elements[adjustedPos.x, adjustedPos.y] == null;

                        if (isEmpty)
                        {
                            isSleeping = false;
                            moved = MoveToNewPosition(adjustedPos, targetChunkPos, currentChunk, targetChunkObject);
                            if (targetChunkPos != currentChunkPos)
                            {
                                UpdateLastPosition(nextMoveChunk, currentChunk);
                                return;
                            }
                            current = adjustedPos;
                            break;
                        }
                        else if (targetPos.y < current.y && targetPos.x != current.x)
                        {
                            isSleeping = true;
                            if (TrySwapWithLiquid(adjustedPos, targetChunkPos, currentChunk, diagonalSinkSpeed))
                            {
                                UpdateLastPosition(nextMoveChunk, currentChunk);
                                return;
                            }
                            hitObstacle = true;
                        }
                        else if (targetPos.y < current.y)
                        {
                            hitObstacle = true;
                        }
                    }
                }

                if (!moved)
                {
                    HandleCollision(hitObstacle);
                    return;
                }
            }

            UpdateLastPosition(nextMoveChunk, currentChunk);
        }

        public bool IsGrounded()
        {
            Vector2Int desiredChunk = this.chunkPosition;
            Vector2Int desiredLocal = this.localPosition;
            desiredLocal = this.AdjustPositionForChunk(desiredLocal, ref desiredChunk, SandSimulation.Instance.chunkSize);

            if (SandSimulation.Instance.chunks[desiredChunk].elements[desiredLocal.x, desiredLocal.y] == null)
            {
                return false; // not grounded
            }
            else
            {
                return true; // grounded, something is below
            }
        }

        private Vector2Int[] GetPotentialMoves(Vector2Int current, bool isResting, float horizontalVelocity)
        {
            if (isResting)
            {
                // When resting, only check straight down
                return new Vector2Int[] { new Vector2Int(current.x, current.y - 1) };
            }

            // Add random drift when in air
            float randomDrift = Random.Range(-0.2f, 0.2f);
            horizontalVelocity += randomDrift;

            int horizontalDir = horizontalVelocity > 0 ? 1 : -1;
            if (Mathf.Abs(horizontalVelocity) < 0.05f) horizontalDir = 0;

            // Prioritize diagonal movement for more natural dust behavior
            if(this.fallVelocity > 0.1f)
            {
                return new Vector2Int[]
                {
                    new Vector2Int(current.x + horizontalDir, current.y - 1),
                    new Vector2Int(current.x, current.y - 1),
                    new Vector2Int(current.x + horizontalDir, current.y),
                    new Vector2Int(current.x - horizontalDir, current.y - 1),
                    new Vector2Int(current.x - horizontalDir, current.y),
                };
            }
            else
            {
                return new Vector2Int[]
                {
                    new Vector2Int(current.x, current.y - 1),
                };
            }
        }

        private void HandleCollision(bool hitObstacle)
        {
            if (hitObstacle)
            {
                if (fallVelocity > 0.3f) // Reduced threshold for bounce
                {
                    float impactForce = fallVelocity * velocityAbsorption;
                    horizontalVelocity = Random.Range(-0.5f, 0.5f) * impactForce; // Reduced scatter
                    horizontalVelocity = Mathf.Clamp(horizontalVelocity, -maxHorizontalVelocity, maxHorizontalVelocity);
                }
                else
                {
                    // More likely to come to rest when moving slowly
                    horizontalVelocity *= 0.5f;
                }
            }

            fallVelocity = 0f;
            isResting = (Mathf.Abs(horizontalVelocity) < 0.05f); // Easier to come to rest
        }

        private void UpdateLastPosition(Chunk nextMoveChunk, Chunk currentChunk)
        {
            if (isSleeping)
            {
                nextMoveChunk.isActiveNextFrame = true;
                //currentChunk.WakeUpAllChunkNeighbors(true);
                currentChunk.WakeUpChunkNeighbors(localPosition, true);
                if (lastPosition != null) currentChunk.WakeUpChunkNeighbors(lastPosition.Value, true);
            }

            if (lastPosition != null)
            {
                if (localPosition == lastPosition)
                {
                    isResting = true;
                }
                else if (nextMoveChunk != null)
                {
                    nextMoveChunk.isActiveNextFrame = true;
                    //currentChunk.WakeUpAllChunkNeighbors(true);
                    currentChunk.WakeUpChunkNeighbors(localPosition, true);
                    if (lastPosition != null) currentChunk.WakeUpChunkNeighbors(lastPosition.Value, true);
                }
            }
            else
            {
                lastPosition = localPosition;
                if (nextMoveChunk != null)
                {
                    nextMoveChunk.isActiveNextFrame = true;
                    //currentChunk.WakeUpAllChunkNeighbors(true);
                    currentChunk.WakeUpChunkNeighbors(localPosition, true);
                }
            }
        }

        private bool CanSinkInLiquid(Element particle)
        {
            if (particle == null) return false;

            if (particle.GetType() == typeof(LiquidElement))
            {
                LiquidElement liquid = particle as LiquidElement;
                float densityDiff = density - liquid.density;

                if (densityDiff > 0)
                {
                    return true;
                }
                else { return false; }
            }
            else if (particle.GetType() == typeof(PowderElement))
            {
                return false; //powders can sink in each other too, but for now its not added yet
            }

            return false;
        }

        private bool TrySwapWithLiquid(Vector2Int targetPos, Vector2Int targetChunkPos, Chunk currentChunk, float swapChance)
        {
            if (SandSimulation.Instance.chunks.TryGetValue(targetChunkPos, out Chunk targetChunkObj))
            {
                var particle = targetChunkObj.elements[targetPos.x, targetPos.y];
                if (particle is LiquidElement liquid)
                {
                    float densityDiff = density - liquid.density;
                    if (densityDiff > 0 && Random.value < (swapChance * Mathf.Min(densityDiff, 1f)))
                    {
                        // Perform the swap
                        if (targetChunkPos == chunkPosition)
                        {
                            // Same chunk swap
                            currentChunk.elements[localPosition.x, localPosition.y] = liquid;
                            currentChunk.elements[targetPos.x, targetPos.y] = this;
                            liquid.localPosition = localPosition;
                            localPosition = targetPos;
                        }
                        else
                        {
                            // Cross-chunk swap
                            currentChunk.elements[localPosition.x, localPosition.y] = liquid;
                            targetChunkObj.elements[targetPos.x, targetPos.y] = this;
                            liquid.localPosition = localPosition;
                            liquid.chunkPosition = chunkPosition;
                            localPosition = targetPos;
                            chunkPosition = targetChunkPos;
                            targetChunkObj.isActive = true;
                        }
                        // Reduce horizontal velocity when sinking
                        horizontalVelocity *= 0.8f;
                        return true;
                    }
                    else
                    {
                        currentChunk.WakeUpChunkNeighbors(localPosition, true);
                    }
                }
            }
            return false;
        }

        public Vector2Int AdjustPositionForChunk(Vector2Int position, ref Vector2Int chunkPos, int chunkSize)
        {
            Vector2Int adjustedPos = position;

            // Handle vertical boundaries
            if (adjustedPos.y < 0)
            {
                adjustedPos.y = chunkSize - 1;
                chunkPos.y -= 1;
            }
            else if (adjustedPos.y >= chunkSize)
            {
                adjustedPos.y = 0;
                chunkPos.y += 1;
            }

            // Handle horizontal boundaries
            if (adjustedPos.x < 0)
            {
                adjustedPos.x = chunkSize - 1;
                chunkPos.x -= 1;
            }
            else if (adjustedPos.x >= chunkSize)
            {
                adjustedPos.x = 0;
                chunkPos.x += 1;
            }

            return adjustedPos;
        }

        public Vector3Int GetTilemapPosition(Vector2Int localPos, Vector2Int chunkPos)
        {
            // Simply use the chunk position as the tilemap position since 1 tile = 1 chunk
            return new Vector3Int(chunkPos.x, chunkPos.y, 0);
        }

        public bool MoveToNewPosition(Vector2Int newPos, Vector2Int newChunkPos, Chunk currentChunk, Chunk targetChunk)
        {
            if (currentChunk == targetChunk)
            {
                // Same chunk movement
                currentChunk.elements[localPosition.x, localPosition.y] = null;
                currentChunk.elements[newPos.x, newPos.y] = this;
                localPosition = newPos;
                return true;
            }
            else
            {
                // Cross-chunk movement
                currentChunk.elements[localPosition.x, localPosition.y] = null;
                targetChunk.elements[newPos.x, newPos.y] = this;
                localPosition = newPos;
                chunkPosition = newChunkPos;
                targetChunk.isActive = true;
                return true;
            }
        }

        public bool MoveToNewPositionCST(Vector2Int newPos, Vector2Int newChunkPos, Chunk currentChunk, Chunk targetChunk, Vector2Int currentPos)
        {
            if (currentChunk == targetChunk)
            {
                // Same chunk movement
                currentChunk.elements[currentPos.x, currentPos.y] = null;
                currentChunk.elements[newPos.x, newPos.y] = this;
                localPosition = newPos;
                return true;
            }
            else
            {
                // Cross-chunk movement
                currentChunk.elements[currentPos.x, currentPos.y] = null;
                targetChunk.elements[newPos.x, newPos.y] = this;
                localPosition = newPos;
                chunkPosition = newChunkPos;
                targetChunk.isActive = true;
                return true;
            }
        }
    }
    public abstract class LiquidElement : Element
    {
        public virtual float density => 1.0f;  // Default density for liquid particles
        public virtual float dispersionRate => 5f;  // How many cells to check horizontally (default 2)
        public virtual float flowSpeed => 0.8f;  // How quickly the liquid flows horizontally

        //COLOR DATA
        public override bool useCustomColorData => false;
        public override Color[] colorData => new Color[]
        {
                    new Color(0.0f, 0.6f, 0.8f), // Light Aqua
                    new Color(0.1f, 0.5f, 0.7f), // Aqua Blue
                    new Color(0.0f, 0.4f, 0.6f), // Ocean Blue
                    new Color(0.2f, 0.7f, 0.9f), // Bright Cyan
                    new Color(0.1f, 0.6f, 0.8f), // Teal
                    new Color(0.0f, 0.5f, 0.8f), // Deep Aqua
                    new Color(0.3f, 0.8f, 0.9f), // Light Sky Blue
                    new Color(0.1f, 0.4f, 0.5f)  // Deep Water Blue
        };

        public LiquidElement(Vector2Int localPos, Vector2Int chunkPos) : base(localPos, chunkPos)
        {
            
        }

        public override float maxVelocity => 4f;
        public override float velocityAbsorption => 0.3f;
        public override float maxHorizontalVelocity => 4f;
        public override float horizontalDrag => 0.1f;
        public override float friction => 0.2f;


        private Vector2Int? lastPosition = null;

        public Chunk currentChunkValue;
        public override void Simulate(Chunk currentChunk)
        {
            currentChunkValue = currentChunk;

            currentChunk.CheckElementContacts(this);
            ApplyFriction();

            lastPosition = localPosition;

            Chunk nextMoveChunk = null;

            if (isResting)
            {
                Vector2Int belowCheck = new Vector2Int(localPosition.x, localPosition.y - 1);
                Vector2Int targetChunkCheck = chunkPosition;
                belowCheck = AdjustPositionForChunk(belowCheck, ref targetChunkCheck, currentChunk.chunkSize);

                if (belowCheck.y < 0)
                {
                    belowCheck.y = currentChunk.chunkSize - 1;
                    targetChunkCheck.y -= 1;
                }

                if (SandSimulation.Instance.chunks.TryGetValue(targetChunkCheck, out Chunk targetChunkObjCheck))
                {

                    bool isEmpty = !SandSimulation.Instance.collisionTilemap.HasTile(new Vector3Int(
                        belowCheck.x / currentChunk.chunkSize + targetChunkCheck.x,
                        belowCheck.y / currentChunk.chunkSize + targetChunkCheck.y,
                        0)) &&
                        targetChunkObjCheck.elements[belowCheck.x, belowCheck.y] == null;

                    if (isEmpty)
                    {
                        isResting = false;
                        fallVelocity = 0f;
                    }
                    else
                    {
                        return;
                    }
                }
            }

            currentChunk.WakeUpParticleNeighbors(localPosition, chunkPosition);
            //chunks[chunkPos].WakeUpChunkNeighbors(localPos, true);

            fallVelocity = Mathf.Min(fallVelocity + SandSimulation.Instance.gravity, maxVelocity);

            if (Mathf.Abs(horizontalVelocity) > 0)
            {
                horizontalVelocity *= (1f - horizontalDrag);
                if (Mathf.Abs(horizontalVelocity) < 0.1f)
                {
                    horizontalVelocity = 0f;
                }
            }

            float totalVelocity = Mathf.Sqrt(fallVelocity * fallVelocity + horizontalVelocity * horizontalVelocity);
            int steps = Mathf.CeilToInt(totalVelocity);

            Vector2Int current = localPosition;
            Vector2Int currentChunkPos = chunkPosition;

            for (int step = 0; step < steps; step++)
            {
                List<Vector2Int> potentialMoves = new List<Vector2Int>();

                // always try moving down first
                potentialMoves.Add(new Vector2Int(current.x, current.y - 1));

                // Calculate horizontal spread based on dispersionRate
                int maxSpread = Mathf.CeilToInt(dispersionRate);

                //Add diagonal moves with increasing horizontal distance
                for (int spread = 1; spread <= maxSpread; spread++)
                {
                    // Calculate probability based on distance
                    float spreadProbability = flowSpeed * (1f - (spread - 1) / maxSpread);

                    int rnd0 = Random.Range(0, 2);

                    if(rnd0 == 1)
                    {
                        // Try left side
                        if (Random.value < spreadProbability)
                        {
                            int rnd = Random.Range(0, 1);

                            if (rnd == 0)
                            {
                                potentialMoves.Add(new Vector2Int(current.x - spread, current.y - 1));  // Diagonal down-left
                                potentialMoves.Add(new Vector2Int(current.x - spread, current.y));      // Straight left
                            }
                            else
                            {
                                potentialMoves.Add(new Vector2Int(current.x - spread, current.y));      // Straight left
                                potentialMoves.Add(new Vector2Int(current.x - spread, current.y - 1));  // Diagonal down-left
                            }
                        }

                        // Try right side
                        if (Random.value < spreadProbability)
                        {
                            int rnd = Random.Range(0, 1);

                            if (rnd == 0)
                            {
                                potentialMoves.Add(new Vector2Int(current.x + spread, current.y - 1));  // Diagonal down-right
                                potentialMoves.Add(new Vector2Int(current.x + spread, current.y));      // Straight right
                            }
                            else
                            {
                                potentialMoves.Add(new Vector2Int(current.x + spread, current.y));      // Straight right
                                potentialMoves.Add(new Vector2Int(current.x + spread, current.y - 1));  // Diagonal down-right
                            }
                        }
                    }
                    else
                    {
                        // Try right side
                        if (Random.value < spreadProbability)
                        {
                            int rnd = Random.Range(0, 1);

                            if (rnd == 0)
                            {
                                potentialMoves.Add(new Vector2Int(current.x + spread, current.y - 1));  // Diagonal down-right
                                potentialMoves.Add(new Vector2Int(current.x + spread, current.y));      // Straight right
                            }
                            else
                            {
                                potentialMoves.Add(new Vector2Int(current.x + spread, current.y));      // Straight right
                                potentialMoves.Add(new Vector2Int(current.x + spread, current.y - 1));  // Diagonal down-right
                            }
                        }

                        // Try left side
                        if (Random.value < spreadProbability)
                        {
                            int rnd = Random.Range(0, 1);

                            if (rnd == 0)
                            {
                                potentialMoves.Add(new Vector2Int(current.x - spread, current.y - 1));  // Diagonal down-left
                                potentialMoves.Add(new Vector2Int(current.x - spread, current.y));      // Straight left
                            }
                            else
                            {
                                potentialMoves.Add(new Vector2Int(current.x - spread, current.y));      // Straight left
                                potentialMoves.Add(new Vector2Int(current.x - spread, current.y - 1));  // Diagonal down-left
                            }
                        }
                    }

                    
                }

                bool moved = false;
                bool hitObstacle = false;

                foreach (Vector2Int targetPos in potentialMoves)
                {
                    Vector2Int targetChunkPos = currentChunkPos;
                    Vector2Int adjustedPos = targetPos;
                    adjustedPos = AdjustPositionForChunk(targetPos, ref targetChunkPos, currentChunk.chunkSize);

                    /*
                    // Adjust position and chunk if moving across chunk boundaries
                    if (adjustedPos.y < 0)
                    {
                        adjustedPos.y = currentChunk.chunkSize - 1;
                        targetChunkPos.y -= 1;
                    }
                    if (adjustedPos.x < 0)
                    {
                        adjustedPos.x = currentChunk.chunkSize - 1;
                        targetChunkPos.x -= 1;
                    }
                    else if (adjustedPos.x >= currentChunk.chunkSize)
                    {
                        adjustedPos.x = 0;
                        targetChunkPos.x += 1;
                    }*/

                    if (SandSimulation.Instance.chunks.TryGetValue(targetChunkPos, out Chunk targetChunkObject))
                    {
                        Vector3Int tilePosition = new Vector3Int(
                            adjustedPos.x / currentChunk.chunkSize + targetChunkPos.x,
                            adjustedPos.y / currentChunk.chunkSize + targetChunkPos.y,
                            0);

                        nextMoveChunk = targetChunkObject;

                        bool isEmpty = !SandSimulation.Instance.collisionTilemap.HasTile(tilePosition) &&
                                     targetChunkObject.elements[adjustedPos.x, adjustedPos.y] == null;

                        if (isEmpty)
                        {
                            // Move the particle
                            if (targetChunkPos == currentChunkPos)
                            {
                                currentChunk.elements[localPosition.x, localPosition.y] = null;
                                localPosition = adjustedPos;
                                currentChunk.elements[adjustedPos.x, adjustedPos.y] = this;
                                current = adjustedPos;
                            }
                            else
                            {
                                currentChunk.elements[localPosition.x, localPosition.y] = null;
                                localPosition = adjustedPos;
                                chunkPosition = targetChunkPos;
                                targetChunkObject.elements[adjustedPos.x, adjustedPos.y] = this;
                                targetChunkObject.isActive = true;
                                return;
                            }
                            moved = true;
                            break;
                        }
                        else if (targetPos.y < current.y)
                        {
                            hitObstacle = true;
                        }
                    }
                }

                if (!moved)
                {
                    if (hitObstacle && fallVelocity > 0.5f)
                    {
                        float impactForce = fallVelocity * velocityAbsorption;
                        horizontalVelocity = Random.Range(-1f, 1f) * impactForce;
                        horizontalVelocity = Mathf.Clamp(horizontalVelocity, -maxHorizontalVelocity, maxHorizontalVelocity);
                    }

                    fallVelocity = 0f;
                    isResting = (Mathf.Abs(horizontalVelocity) < 0.1f);
                    return;
                }
            }

            if (lastPosition != null)
            {
                if (localPosition == lastPosition)
                {
                    this.isResting = true;
                }
                else
                {
                    //wake up its chunk
                    if (nextMoveChunk != null)
                    {
                        nextMoveChunk.isActiveNextFrame = true;
                        //currentChunk.ActivateAllNeighbors(true);
                        //currentChunk.WakeUpAllChunkNeighbors(true);
                        currentChunk.WakeUpChunkNeighbors(localPosition, true);
                    }
                }
            }
            else
            {
                lastPosition = localPosition;

                //wake up its chunk
                if (nextMoveChunk != null)
                {
                    nextMoveChunk.isActiveNextFrame = true;
                    //currentChunk.ActivateAllNeighbors(true);
                    //currentChunk.WakeUpAllChunkNeighbors(true);
                    currentChunk.WakeUpChunkNeighbors(localPosition, true);
                }
            }
        }

        public Vector3Int GetTilemapPosition(Vector2Int localPos, Vector2Int chunkPos)
        {
            // Simply use the chunk position as the tilemap position since 1 tile = 1 chunk
            return new Vector3Int(chunkPos.x, chunkPos.y, 0);
        }

        public Vector2Int AdjustPositionForChunk(Vector2Int position, ref Vector2Int chunkPos, int chunkSize)
        {
            Vector2Int adjustedPos = position;

            // Handle vertical boundaries
            if (adjustedPos.y < 0)
            {
                adjustedPos.y = chunkSize - 1;
                chunkPos.y -= 1;
            }
            else if (adjustedPos.y >= chunkSize)
            {
                adjustedPos.y = 0;
                chunkPos.y += 1;
            }

            // Handle horizontal boundaries
            if (adjustedPos.x < 0)
            {
                adjustedPos.x = chunkSize - 1;
                chunkPos.x -= 1;
            }
            else if (adjustedPos.x >= chunkSize)
            {
                adjustedPos.x = 0;
                chunkPos.x += 1;
            }

            return adjustedPos;
        }

    }

    

    // ELEMENTS

    public class Sand : PowderElement
    {
        private Color[] _customColorData;

        public override Color[] colorData => _customColorData ?? new Color[]
        {
            new Color(0.94f, 0.85f, 0.53f, 1f), // Light Beige
            new Color(0.87f, 0.76f, 0.46f, 1f), // Soft Yellow Sand
            new Color(0.91f, 0.77f, 0.47f, 1f), // Light Brown Sand
            new Color(0.81f, 0.67f, 0.35f, 1f), // Desert Sand
            new Color(0.74f, 0.62f, 0.37f, 1f), // Medium Sand
            new Color(0.85f, 0.72f, 0.48f, 1f), // Warm Sand
            new Color(0.78f, 0.61f, 0.39f, 1f), // Brownish Sand
            new Color(0.92f, 0.80f, 0.47f, 1f)  // Pale Sandy Yellow
        };

        public override bool useCustomColorData => true;
        public override float density => 1.8f;

        public Sand(Vector2Int localPos, Vector2Int chunkPos) : base(localPos, chunkPos)
        {
            if (useCustomColorData && colorData != null && colorData.Length > 0)
            {
                this.localColor = colorData[Random.Range(0, colorData.Length - 1)];
            }
        }

        public void SetCustomColorData(Color[] colors)
        {
            _customColorData = colors;
        }
    }

    public class TemporalPowder : PowderElement
    {
        private Color[] _customColorData;
        //private float lifetime = 3f;

        public override Color[] colorData => _customColorData ?? new Color[]
        {
            new Color(0.078f, 0.078f, 0.078f), // #141414
            new Color(0.164f, 0.164f, 0.164f), // #2A2A2A
            new Color(0.059f, 0.059f, 0.059f), // #0F0F0F
            new Color(0.196f, 0.196f, 0.196f), // #323232
            new Color(0.149f, 0.149f, 0.149f), // #262626
            new Color(0.271f, 0.271f, 0.271f), // #383838
            new Color(0.125f, 0.125f, 0.125f), // #1F1F1F
            new Color(0.267f, 0.267f, 0.267f)  // #444444
        };


        public override bool useCustomColorData => true;
        public override float density => 1.8f;

        public TemporalPowder(Vector2Int localPos, Vector2Int chunkPos) : base(localPos, chunkPos)
        {
            if (useCustomColorData && colorData != null && colorData.Length > 0)
            {
                this.localColor = colorData[Random.Range(0, colorData.Length - 1)];
            }

            //Invoke("DieWithTime", lifetime);
        }

        public void SetCustomColorData(Color[] colors)
        {
            _customColorData = colors;
        }

        public void Die()
        {
            SandSimulation.Instance.chunks[chunkPosition].elements[this.localPosition.x, this.localPosition.y] = null;
            SandSimulation.Instance.chunks[chunkPosition].WakeUpChunkNeighbors(localPosition);
        }
    }

    public class Water : LiquidElement
    { 

        private Color[] _customColorData;
        public override Color[] colorData => _customColorData ?? new Color[]
        {
            new Color(0.0f, 0.2f, 0.3f, 1f), // Deep Aqua
            new Color(0.1f, 0.3f, 0.4f, 1f), // Dark Teal
            new Color(0.0f, 0.15f, 0.25f, 1f), // Abyss Blue
            new Color(0.1f, 0.25f, 0.35f, 1f), // Midnight Blue
            new Color(0.05f, 0.2f, 0.3f, 1f), // Deep Ocean
            new Color(0.0f, 0.1f, 0.2f, 1f), // Twilight Depth
            new Color(0.15f, 0.3f, 0.35f, 1f), // Dim Cyan
            new Color(0.1f, 0.2f, 0.25f, 1f)  // Murky Water Blue
        };

        // properties

        public override bool useCustomColorData => true;
        public override float density => 0.5f;

        public Water(Vector2Int localPos, Vector2Int chunkPos) : base(localPos, chunkPos)
        {
            if (useCustomColorData && colorData != null && colorData.Length > 0)
            {
                this.localColor = colorData[Random.Range(0, colorData.Length - 1)];
            }
        }

        public void SetCustomColorData(Color[] colors)
        {
            _customColorData = colors;
        }
    }

    public class Bricks : SolidElement
    {

        private Color[] _customColorData;
        public override Color[] colorData => _customColorData ?? new Color[]
        {
            new Color(0.35f, 0.2f, 0.3f, 1f), // grayish
        };

        // properties

        public override bool useCustomColorData => true;
        public override float density => 0.5f;

        public Bricks(Vector2Int localPos, Vector2Int chunkPos) : base(localPos, chunkPos)
        {
            if (useCustomColorData && colorData != null && colorData.Length > 0)
            {
                this.localColor = colorData[Random.Range(0, colorData.Length - 1)];
            }
        }

        public void SetCustomColorData(Color[] colors)
        {
            _customColorData = colors;
        }
    }

    public class SawDust : LightPowderElement
    {
        private Color[] _customColorData;

        public override Color[] colorData => _customColorData ?? new Color[]
        {
            new Color(0.74f, 0.65f, 0.43f, 1f), // Darker Beige
            new Color(0.67f, 0.56f, 0.36f, 1f), // Darker Yellow Sand
            new Color(0.71f, 0.57f, 0.37f, 1f), // Darker Brown Sand
            new Color(0.61f, 0.47f, 0.25f, 1f), // Darker Desert Sand
            new Color(0.54f, 0.42f, 0.27f, 1f), // Darker Medium Sand
            new Color(0.65f, 0.52f, 0.38f, 1f), // Darker Warm Sand
            new Color(0.58f, 0.41f, 0.29f, 1f), // Darker Brownish Sand
            new Color(0.72f, 0.60f, 0.37f, 1f)  // Darker Sandy Yellow
        };


        public override bool useCustomColorData => true;
        public override float density => 0.01f;

        public SawDust(Vector2Int localPos, Vector2Int chunkPos) : base(localPos, chunkPos)
        {
            if (useCustomColorData && colorData != null && colorData.Length > 0)
            {
                this.localColor = colorData[Random.Range(0, colorData.Length - 1)];
            }
        }

        public void SetCustomColorData(Color[] colors)
        {
            _customColorData = colors;
        }
    }

    public class Flour : DustElement
    {
        private Color[] _customColorData;

        public override Color[] colorData => new Color[]
        {
            new Color(0.96f, 0.93f, 0.87f), // Soft Beige
            new Color(0.98f, 0.94f, 0.86f), // Cream White
            new Color(0.97f, 0.91f, 0.85f), // Pale Ivory
            new Color(0.93f, 0.85f, 0.72f), // Warm Taupe
            new Color(0.95f, 0.88f, 0.78f), // Almond Milk
            new Color(0.92f, 0.84f, 0.69f), // Oatmeal
            new Color(0.94f, 0.87f, 0.76f), // Wheat
            new Color(0.98f, 0.95f, 0.89f)  // Vanilla Cream
        };



        public override bool useCustomColorData => true;
        public override float density => 0.8f;

        public Flour(Vector2Int localPos, Vector2Int chunkPos) : base(localPos, chunkPos)
        {
            if (useCustomColorData && colorData != null && colorData.Length > 0)
            {
                this.localColor = colorData[Random.Range(0, colorData.Length - 1)];
            }
        }

        public void SetCustomColorData(Color[] colors)
        {
            _customColorData = colors;
        }
    }
}

public static class SimulationCoordinateConverter
{
    // Convert a tilemap position to chunk position
    public static Vector2Int TilemapToChunkPosition(Vector3Int tilemapPosition)
    {
        // This is fine as is since tiles map 1:1 with chunks
        return new Vector2Int(tilemapPosition.x, tilemapPosition.y);
    }

    // Convert a world position to chunk position
    public static Vector2Int WorldToChunkPosition(Vector2 worldPosition)
    {
        // We need to divide by chunk size to get the correct chunk coordinate
        return new Vector2Int(
            Mathf.FloorToInt(worldPosition.x / SandSimulation.Instance.chunkSize),
            Mathf.FloorToInt(worldPosition.y / SandSimulation.Instance.chunkSize)
        );
    }

    // Convert a world position to local position within a chunk
    public static Vector2Int WorldToLocalPosition(Vector2 worldPosition, int chunkSize)
    {
        // Use modulo to get position within chunk
        int x = Mathf.FloorToInt(worldPosition.x) % chunkSize;
        int y = Mathf.FloorToInt(worldPosition.y) % chunkSize;

        // Handle negative coordinates
        if (x < 0) x += chunkSize;
        if (y < 0) y += chunkSize;

        return new Vector2Int(x, y);
    }

    // Convert chunk and local position to world position
    public static Vector2 ChunkToWorldPosition(Vector2Int chunkPos, Vector2Int localPos, int chunkSize)
    {
        // Multiply chunk position by chunk size and add local offset
        return new Vector2(
            chunkPos.x * chunkSize + localPos.x,
            chunkPos.y * chunkSize + localPos.y
        );
    }

    // Convert a local position within a chunk to a world position
    public static Vector2 LocalToWorldPosition(Vector2Int localPos, Vector2Int chunkPos, int chunkSize)
    {
        // Same as ChunkToWorldPosition
        return new Vector2(
            chunkPos.x * chunkSize + localPos.x,
            chunkPos.y * chunkSize + localPos.y
        );
    }
}

public static class EdgeConnectionChecker
{
    public static bool CheckTypeConnectsBorders<T>(
        Dictionary<Vector2Int, Chunk> chunks,
        out HashSet<(Vector2Int chunkPos, Vector2Int localPos)> connectedElements,
        Color[] requiredColors = null) where T : Element
    {
        connectedElements = new HashSet<(Vector2Int chunkPos, Vector2Int localPos)>();

        // Rest of the initial setup remains the same...
        var relevantChunks = new HashSet<Vector2Int>();
        var minX = int.MaxValue;
        var maxX = int.MinValue;

        foreach (var kvp in chunks)
        {
            if (kvp.Value.HasParticleType<T>())
            {
                relevantChunks.Add(kvp.Key);
                minX = Math.Min(minX, kvp.Key.x);
                maxX = Math.Max(maxX, kvp.Key.x);
            }
        }

        if (relevantChunks.Count == 0) return false;

        // Find starting points that match ANY of the required colors
        var startingPoints = new HashSet<(Vector2Int chunkPos, Vector2Int localPos)>();
        foreach (var chunkPos in relevantChunks.Where(c => c.x == minX))
        {
            var chunk = chunks[chunkPos];
            for (int y = 0; y < chunk.chunkSize; y++)
            {
                var element = chunk.elements[0, y] as T;
                if (element != null && ColorMatches(element, requiredColors))
                {
                    startingPoints.Add((chunkPos, new Vector2Int(0, y)));
                }
            }
        }

        if (startingPoints.Count == 0) return false;

        // Modified to combine results from multiple paths
        var allConnectedElements = new HashSet<(Vector2Int chunkPos, Vector2Int localPos)>();

        foreach (var start in startingPoints)
        {
            var visited = new HashSet<(Vector2Int chunkPos, Vector2Int localPos)>();
            if (PathExistsToRightEdge<T>(start, chunks, maxX, visited, requiredColors, out var initialPath))
            {
                var connectedForThisPath = FloodFillFromPath<T>(chunks, initialPath, requiredColors);
                allConnectedElements.UnionWith(connectedForThisPath);
            }
        }

        if (allConnectedElements.Count > 0)
        {
            connectedElements = allConnectedElements;
            return true;
        }

        return false;
    }

    private static HashSet<(Vector2Int chunkPos, Vector2Int localPos)> FloodFillFromPath<T>(
        Dictionary<Vector2Int, Chunk> chunks,
        HashSet<(Vector2Int chunkPos, Vector2Int localPos)> initialPath,
        Color[] requiredColors) where T : Element
    {
        var allConnected = new HashSet<(Vector2Int chunkPos, Vector2Int localPos)>();
        var queue = new Queue<(Vector2Int chunkPos, Vector2Int localPos)>();

        foreach (var point in initialPath)
        {
            if (!allConnected.Contains(point))
            {
                queue.Enqueue(point);
                allConnected.Add(point);
            }
        }

        while (queue.Count > 0)
        {
            var (currentChunkPos, currentLocalPos) = queue.Dequeue();
            var currentChunk = chunks[currentChunkPos];

            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(1, 0),   // right
                new Vector2Int(-1, 0),  // left
                new Vector2Int(0, 1),   // up
                new Vector2Int(0, -1),  // down
                new Vector2Int(1, 1),   // up-right
                new Vector2Int(1, -1),  // down-right
                new Vector2Int(-1, 1),  // up-left
                new Vector2Int(-1, -1), // down-left
            };

            foreach (var dir in directions)
            {
                var nextLocalPos = currentLocalPos + dir;
                var nextChunkPos = currentChunkPos;

                // Handle chunk boundaries
                if (nextLocalPos.x >= currentChunk.chunkSize)
                {
                    nextLocalPos.x = 0;
                    nextChunkPos.x += 1;
                }
                else if (nextLocalPos.x < 0)
                {
                    nextLocalPos.x = currentChunk.chunkSize - 1;
                    nextChunkPos.x -= 1;
                }

                if (nextLocalPos.y >= currentChunk.chunkSize)
                {
                    nextLocalPos.y = 0;
                    nextChunkPos.y += 1;
                }
                else if (nextLocalPos.y < 0)
                {
                    nextLocalPos.y = currentChunk.chunkSize - 1;
                    nextChunkPos.y -= 1;
                }

                var nextPos = (nextChunkPos, nextLocalPos);

                if (allConnected.Contains(nextPos) || !chunks.ContainsKey(nextChunkPos))
                    continue;

                var nextChunk = chunks[nextChunkPos];
                var nextElement = nextChunk.elements[nextLocalPos.x, nextLocalPos.y] as T;

                // Modified to check against all required colors
                if (nextElement != null && ColorMatches(nextElement, requiredColors, false))
                {
                    queue.Enqueue(nextPos);
                    allConnected.Add(nextPos);
                }
            }
        }

        return allConnected;
    }

    private static bool ColorMatches(Element element, Color[] requiredColors, bool useLocal = false)
    {
        if (!element.useCustomColorData)
        {
            Debug.Log("element doesn't support color");
            return true; // Element doesn't use color data
        }

        if (useLocal)
        {
            foreach (var color in requiredColors)
            {
                if (ColorsAreEqualIgnoringAlpha(element.LocalColor, color))
                {
                    return true;
                }
            }
            return false;
        }
        else
        {
            foreach (var color in requiredColors)
            {
                if (element.colorData.Any(c => ColorsAreEqualIgnoringAlpha(c, color)))
                {
                    return true;
                }
            }
            return false;
        }
    }

    private static bool ColorsAreEqualIgnoringAlpha(Color color1, Color color2)
    {
        return Mathf.Approximately(color1.r, color2.r) &&
               Mathf.Approximately(color1.g, color2.g) &&
               Mathf.Approximately(color1.b, color2.b);
    }



    private static bool PathExistsToRightEdge<T>(
        (Vector2Int chunkPos, Vector2Int localPos) current,
        Dictionary<Vector2Int, Chunk> chunks,
        int maxX,
        HashSet<(Vector2Int chunkPos, Vector2Int localPos)> visited,
        Color[] requiredColor,
        out HashSet<(Vector2Int chunkPos, Vector2Int localPos)> initialPath) where T : Element
    {
        initialPath = new HashSet<(Vector2Int chunkPos, Vector2Int localPos)>();
        var queue = new Queue<(Vector2Int chunkPos, Vector2Int localPos)>();
        var pathParent = new Dictionary<(Vector2Int chunkPos, Vector2Int localPos), (Vector2Int chunkPos, Vector2Int localPos)>();

        queue.Enqueue(current);
        visited.Add(current);

        while (queue.Count > 0)
        {
            var (currentChunkPos, currentLocalPos) = queue.Dequeue();
            var currentChunk = chunks[currentChunkPos];

            // Check if we reached the right edge
            if (currentChunkPos.x == maxX && currentLocalPos.x == currentChunk.chunkSize - 1)
            {
                // Reconstruct the initial path
                var pathPoint = (currentChunkPos, currentLocalPos);
                while (pathParent.ContainsKey(pathPoint))
                {
                    initialPath.Add(pathPoint);
                    pathPoint = pathParent[pathPoint];
                }
                initialPath.Add(current); // Add the starting point
                return true;
            }

            // Check all adjacent positions (including diagonals)
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(1, 0),   // right
                new Vector2Int(-1, 0),  // left
                new Vector2Int(0, 1),   // up
                new Vector2Int(0, -1),  // down
                new Vector2Int(1, 1),   // up-right
                new Vector2Int(1, -1),  // down-right
                new Vector2Int(-1, 1),  // up-left
                new Vector2Int(-1, -1), // down-left
            };

            foreach (var dir in directions)
            {
                var nextLocalPos = currentLocalPos + dir;
                var nextChunkPos = currentChunkPos;

                // Handle chunk boundaries
                if (nextLocalPos.x >= currentChunk.chunkSize)
                {
                    nextLocalPos.x = 0;
                    nextChunkPos.x += 1;
                }
                else if (nextLocalPos.x < 0)
                {
                    nextLocalPos.x = currentChunk.chunkSize - 1;
                    nextChunkPos.x -= 1;
                }

                if (nextLocalPos.y >= currentChunk.chunkSize)
                {
                    nextLocalPos.y = 0;
                    nextChunkPos.y += 1;
                }
                else if (nextLocalPos.y < 0)
                {
                    nextLocalPos.y = currentChunk.chunkSize - 1;
                    nextChunkPos.y -= 1;
                }

                var nextPos = (nextChunkPos, nextLocalPos);

                // Skip if already visited or chunk doesn't exist
                if (visited.Contains(nextPos) || !chunks.ContainsKey(nextChunkPos))
                    continue;

                // Check if the next position contains the correct particle type and color
                var nextChunk = chunks[nextChunkPos];
                var nextElement = nextChunk.elements[nextLocalPos.x, nextLocalPos.y] as T;
                if (nextElement != null && ColorMatches(nextElement, requiredColor))
                {
                    queue.Enqueue(nextPos);
                    visited.Add(nextPos);
                    pathParent[nextPos] = (currentChunkPos, currentLocalPos);
                }
            }
        }

        return false;
    }

    // Helper method to check if a specific colored line exists
    public static bool CheckColoredTypeConnectsBorders<T>(
        Dictionary<Vector2Int, Chunk> chunks, 
        Color[] color,
        out HashSet<(Vector2Int chunkPos, Vector2Int localPos)> connectedElements) where T : Element
    {
        return CheckTypeConnectsBorders<T>(chunks, out connectedElements, color);
    }
}

public class ArrayUtility
{
    public static Vector2Int[] Shuffle(Vector2Int[] array)
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));

        for (int i = 0; i < array.Length - 1; ++i)
        {
            int r = Random.Range(i, array.Length);  // Unity's Random.Range for index selection
            // Swap Vector2Int elements
            Vector2Int temp = array[r];
            array[r] = array[i];
            array[i] = temp;
        }
        return array;  // Return the shuffled array
    }
}

public static class ColorEdgeConnectionChecker
{
    public static bool CheckColorConnectsBorders(
        Dictionary<Vector2Int, Chunk> chunks,
        out HashSet<(Vector2Int chunkPos, Vector2Int localPos)> connectedElements,
        Color[] requiredColors)
    {
        connectedElements = new HashSet<(Vector2Int chunkPos, Vector2Int localPos)>();

        var relevantChunks = new HashSet<Vector2Int>();
        var minX = int.MaxValue;
        var maxX = int.MinValue;

        // First, find chunks containing the required colors and determine map bounds
        foreach (var kvp in chunks)
        {
            if (HasColoredElement(kvp.Value, requiredColors))
            {
                relevantChunks.Add(kvp.Key);
                minX = Math.Min(minX, kvp.Key.x);
                maxX = Math.Max(maxX, kvp.Key.x);
            }
        }

        if (relevantChunks.Count == 0) return false;

        // Find starting points on the LEFT edge only
        var startingPoints = new HashSet<(Vector2Int chunkPos, Vector2Int localPos)>();
        foreach (var chunkPos in relevantChunks.Where(c => c.x == minX))
        {
            var chunk = chunks[chunkPos];
            for (int y = 0; y < chunk.chunkSize; y++)
            {
                var element = chunk.elements[0, y];
                if (element != null && ColorMatches(element, requiredColors))
                {
                    startingPoints.Add((chunkPos, new Vector2Int(0, y)));
                }
            }
        }

        if (startingPoints.Count == 0) return false;

        // For each starting point on the left edge, try to find a path to the right edge
        foreach (var start in startingPoints)
        {
            var visited = new HashSet<(Vector2Int chunkPos, Vector2Int localPos)>();
            if (PathExistsToRightEdge(start, chunks, maxX, visited, requiredColors, out var initialPath))
            {
                // Only flood fill if we found a complete path from left to right
                var connectedForThisPath = FloodFillFromPath(chunks, initialPath, requiredColors);
                connectedElements = connectedForThisPath;
                return true;
            }
        }

        return false;
    }

    private static bool HasColoredElement(Chunk chunk, Color[] requiredColors)
    {
        for (int x = 0; x < chunk.chunkSize; x++)
        {
            for (int y = 0; y < chunk.chunkSize; y++)
            {
                var element = chunk.elements[x, y];
                if (element != null && ColorMatches(element, requiredColors))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static HashSet<(Vector2Int chunkPos, Vector2Int localPos)> FloodFillFromPath(
        Dictionary<Vector2Int, Chunk> chunks,
        HashSet<(Vector2Int chunkPos, Vector2Int localPos)> initialPath,
        Color[] requiredColors)
    {
        var allConnected = new HashSet<(Vector2Int chunkPos, Vector2Int localPos)>();
        var queue = new Queue<(Vector2Int chunkPos, Vector2Int localPos)>();

        foreach (var point in initialPath)
        {
            if (!allConnected.Contains(point))
            {
                queue.Enqueue(point);
                allConnected.Add(point);
            }
        }

        while (queue.Count > 0)
        {
            var (currentChunkPos, currentLocalPos) = queue.Dequeue();
            var currentChunk = chunks[currentChunkPos];

            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(1, 0),   // right
                new Vector2Int(-1, 0),  // left
                new Vector2Int(0, 1),   // up
                new Vector2Int(0, -1),  // down
                new Vector2Int(1, 1),   // up-right
                new Vector2Int(1, -1),  // down-right
                new Vector2Int(-1, 1),  // up-left
                new Vector2Int(-1, -1), // down-left
            };

            foreach (var dir in directions)
            {
                var nextLocalPos = currentLocalPos + dir;
                var nextChunkPos = currentChunkPos;

                // Handle chunk boundaries
                if (nextLocalPos.x >= currentChunk.chunkSize)
                {
                    nextLocalPos.x = 0;
                    nextChunkPos.x += 1;
                }
                else if (nextLocalPos.x < 0)
                {
                    nextLocalPos.x = currentChunk.chunkSize - 1;
                    nextChunkPos.x -= 1;
                }

                if (nextLocalPos.y >= currentChunk.chunkSize)
                {
                    nextLocalPos.y = 0;
                    nextChunkPos.y += 1;
                }
                else if (nextLocalPos.y < 0)
                {
                    nextLocalPos.y = currentChunk.chunkSize - 1;
                    nextChunkPos.y -= 1;
                }

                var nextPos = (nextChunkPos, nextLocalPos);

                if (allConnected.Contains(nextPos) || !chunks.ContainsKey(nextChunkPos))
                    continue;

                var nextChunk = chunks[nextChunkPos];
                var nextElement = nextChunk.elements[nextLocalPos.x, nextLocalPos.y];

                if (nextElement != null && ColorMatches(nextElement, requiredColors))
                {
                    queue.Enqueue(nextPos);
                    allConnected.Add(nextPos);
                }
            }
        }

        return allConnected;
    }

    private static bool ColorMatches(Element element, Color[] requiredColors)
    {
        if (!element.useCustomColorData)
        {
            Debug.Log("element doesn't support color");
            return true; // Element doesn't use color data
        }

        foreach (var color in requiredColors)
        {
            if (element.colorData.Any(c => ColorsAreEqualIgnoringAlpha(c, color)))
            {
                return true;
            }
        }
        return false;
    }

    private static bool ColorsAreEqualIgnoringAlpha(Color color1, Color color2)
    {
        return Mathf.Approximately(color1.r, color2.r) &&
               Mathf.Approximately(color1.g, color2.g) &&
               Mathf.Approximately(color1.b, color2.b);
    }

    private static bool PathExistsToRightEdge(
        (Vector2Int chunkPos, Vector2Int localPos) current,
        Dictionary<Vector2Int, Chunk> chunks,
        int maxX,
        HashSet<(Vector2Int chunkPos, Vector2Int localPos)> visited,
        Color[] requiredColor,
        out HashSet<(Vector2Int chunkPos, Vector2Int localPos)> initialPath)
    {
        initialPath = new HashSet<(Vector2Int chunkPos, Vector2Int localPos)>();
        var queue = new Queue<(Vector2Int chunkPos, Vector2Int localPos)>();
        var pathParent = new Dictionary<(Vector2Int chunkPos, Vector2Int localPos), (Vector2Int chunkPos, Vector2Int localPos)>();

        queue.Enqueue(current);
        visited.Add(current);

        while (queue.Count > 0)
        {
            var (currentChunkPos, currentLocalPos) = queue.Dequeue();
            var currentChunk = chunks[currentChunkPos];

            // Check if we've reached the right edge of the rightmost chunk
            if (currentChunkPos.x == maxX && currentLocalPos.x == currentChunk.chunkSize - 1)
            {
                // Reconstruct the path
                var pathPoint = (currentChunkPos, currentLocalPos);
                while (pathParent.ContainsKey(pathPoint))
                {
                    initialPath.Add(pathPoint);
                    pathPoint = pathParent[pathPoint];
                }
                initialPath.Add(current);
                return true;
            }

            // Prioritize horizontal movement to favor direct paths
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(1, 0),   // right (prioritized)
                new Vector2Int(1, 1),   // up-right
                new Vector2Int(1, -1),  // down-right
                new Vector2Int(0, 1),   // up
                new Vector2Int(0, -1),  // down
                new Vector2Int(-1, 0),  // left
                new Vector2Int(-1, 1),  // up-left
                new Vector2Int(-1, -1), // down-left
            };

            foreach (var dir in directions)
            {
                var nextLocalPos = currentLocalPos + dir;
                var nextChunkPos = currentChunkPos;

                // Handle chunk boundaries
                if (nextLocalPos.x >= currentChunk.chunkSize)
                {
                    nextLocalPos.x = 0;
                    nextChunkPos.x += 1;
                }
                else if (nextLocalPos.x < 0)
                {
                    nextLocalPos.x = currentChunk.chunkSize - 1;
                    nextChunkPos.x -= 1;
                }

                if (nextLocalPos.y >= currentChunk.chunkSize)
                {
                    nextLocalPos.y = 0;
                    nextChunkPos.y += 1;
                }
                else if (nextLocalPos.y < 0)
                {
                    nextLocalPos.y = currentChunk.chunkSize - 1;
                    nextChunkPos.y -= 1;
                }

                var nextPos = (nextChunkPos, nextLocalPos);

                if (visited.Contains(nextPos) || !chunks.ContainsKey(nextChunkPos))
                    continue;

                var nextChunk = chunks[nextChunkPos];
                var nextElement = nextChunk.elements[nextLocalPos.x, nextLocalPos.y];

                if (nextElement != null && ColorMatches(nextElement, requiredColor))
                {
                    queue.Enqueue(nextPos);
                    visited.Add(nextPos);
                    pathParent[nextPos] = (currentChunkPos, currentLocalPos);
                }
            }
        }

        return false;
    }
}