using System.Collections.Generic;
using System.Linq;
using System;
using Unity.VisualScripting;
using UnityEngine;
using System.Xml.Serialization;
using UnityEngine.UIElements;
using Demo;

/// <summary>
/// A manager to handle the top level interaction with all of the levels squares.
/// </summary>
[RequireComponent(typeof(GameManager))]
public class SquareManager : MonoBehaviour
{
    /// <summary>
    /// A dictionary to find the square object at any given position
    /// </summary>
    public Dictionary<Vector2Int, Square> squares;

    private bool isPlayerTurn;

    private PlayerController player;

    private EnemyManager enemyManager;

    GameManager gameManager;

    /// <summary>
    /// The valid moves a knight can make
    /// </summary>
    Vector2Int[] KnightMoves =
    {
        new Vector2Int(1,2),
        new Vector2Int(2,1),
        new Vector2Int(1,-2),
        new Vector2Int(2,-1),
        new Vector2Int(-1,2),
        new Vector2Int(-2,1),
        new Vector2Int(-1,-2),
        new Vector2Int(-2,-1),

        // Enable for a fun time
        // new Vector2Int(0, 1),
        // new Vector2Int(0, -1),
        // new Vector2Int(1, 0),
        // new Vector2Int(-1, 0),
    };

    public void Initialise(Dictionary<Vector2Int, Square> inputSquares, PlayerController player, EnemyManager enemyManager)
    {
        squares = inputSquares;
        this.player = player;
        this.enemyManager = enemyManager;

        // Initialise the squares
        foreach (Square square in squares.Values)
        {
            square.PlayerController = player;
            square.enemyManager = enemyManager;
            square.OnLevelStart();
        }
    }

    void Start()
    {
        gameManager = GetComponent<GameManager>();
    }

    void Update()
    {
        // Handles player movement
        if (Input.GetMouseButtonDown(0) && isPlayerTurn && player.Alive && !gameManager.InputLocked)
        {
            Vector2Int mousePos = GridUtilities.GetMouseGridPos();
            if (GetValidMoves().Contains(mousePos))
            {
                // Perform actions related to leaving the current square
                squares[player.position].OnPlayerLeave();

                // Moves the player
                player.MoveTo(mousePos, AnimationController.MovementType.Jump);

                // Informs the squares that the player has moved
                gameManager.OnPlayerMove();

                // Handles player land once game-blocking animations have finished
                ActionQueue.QueueAction(gameManager.OnPlayerLand);
            }
        }
    }

    /// <summary>
    /// The actions to be performed once the player has input their move
    /// </summary>
    public void OnPlayerMove()
    {
        // Remove all square highlights
        HighlightSquares(new());

        isPlayerTurn = false;
        foreach (Square square in squares.Values)
        {
            square.OnPlayerMove();
        }
    }

    /// <summary>
    /// The actions to be performed once the player lands on its new tile.
    /// </summary>
    public void OnPlayerLand()
    {
        squares[player.position].OnPlayerLand();
    }



    /// <summary>
    /// The actions to be performed once the enemy leaves the tile.
    /// </summary>
    public void OnEnemyLeave()
    {
        foreach (Square square in squares.Values)
        {
            if (square.EnemyOnTile && square.EnemyOnTile.Position != square.Position)
            {
                square.OnEnemyLeave();
                square.EnemyOnTile = null;
            }
        }
    }

    /// <summary>
    /// The actions to be performed on the level's turn.
    /// </summary>
    public void OnLevelTurn()
    {
        foreach (Enemy enemy in enemyManager.enemies.ToList())
        {
            squares[enemy.Position].OnEnemyLand();
            squares[enemy.Position].OnEnemyLand(enemy);
        }
        // Does all of the square's turns.
        foreach (Square square in squares.Values)
        {
            square.OnLevelTurn();
        }
    }

    /// <summary>
    /// The actions to be performed at the start of the player's turn.
    /// </summary>
    public void OnPlayerTurnStart()
    {
        isPlayerTurn = true;

        foreach (Square square in squares.Values)
        {
            square.OnPlayerTurnStart();
        }

        // Add square highlights to valid moves
        HighlightSquares(GetValidMoves());

        // Add enemy capture hightlights to danger squares
        List<Vector2Int> totalCaptureList = new List<Vector2Int>();

        foreach (Enemy enemy in enemyManager.enemies)
        {
            totalCaptureList.AddRange(enemy.CaptureSquares);
        }

        HighlightEnemyCaptureSquares(totalCaptureList);

        // Show the restart prompt if there are no valid moves
        if (GetValidMoves().Count == 0 && gameManager.gameplayUIManager != null)
        {
            gameManager.gameplayUIManager.SetRestartPrompt(true);
        }
    }

    public List<Vector2Int> GetValidMoves()
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        if (player.Alive)
        {
            foreach (Vector2Int position in squares.Keys)
            {
                if (KnightMoves.Contains(position - player.position) && squares[position].IsPassable)
                {
                    moves.Add(position);
                }
            }
        }

        return moves;
    }

    /// <summary>
    /// Highlight squares at a given set of Vector2Int coordinates
    /// </summary>
    /// <param name="coordinatesToHighlight">The set of coordinates to highlight</param>
    public void HighlightSquares(List<Vector2Int> coordinatesToHighlight)
    {

        foreach (var (coordinate, square) in squares)
        {
            square.IndicateMoveValidity(coordinatesToHighlight.Contains(coordinate));
        }
    }

    /// <summary>
    /// Sets initial charge states for all conductive squares
    /// </summary>
    public void InitialiseElectricity()
    {
        foreach (var (_, square) in squares)
        {
            if (square.Type.IsConductor)
            {
                square.OnChargeChanged();
                square.UpdateOutgoingCharge();
            }
        }
    }

    /// <summary>
    /// Highlight squares at a given set of Vector2Int coordinates
    /// </summary>
    /// <param name="coordinatesToHighlight">The set of coordinates to highlight</param>
    public void HighlightEnemyCaptureSquares(List<Vector2Int> coordinatesToHighlight)
    {

        foreach (var (coordinate, square) in squares)
        {
            square.IndicateEnemyCapture(coordinatesToHighlight.Contains(coordinate));
        }
    }
}
