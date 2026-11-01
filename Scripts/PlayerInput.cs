using UnityEngine;

/// <summary>
/// Ввод: стрелки/WASD — ход, пробел — нырок в портал.
/// </summary>
public class PlayerInput : MonoBehaviour
{
    [SerializeField] private GameBootstrap game;

    private void Update()
    {
        if (game == null || game.Context == null || game.Context.IsWon) return;

        // Начало хода (пропуск/отложенные эффекты) лучше вызывать из отдельной кнопки
        // или по событию. Здесь — по нажатию любой клавиши движения.
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            DoMove(Direction.Up);
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            DoMove(Direction.Down);
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            DoMove(Direction.Left);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            DoMove(Direction.Right);
        else if (Input.GetKeyDown(KeyCode.Space))
            game.Turns.DiveIntoPortal();
    }

    private void DoMove(Direction dir)
    {
        game.Turns.BeginTurn();
        game.Turns.Move(dir);
    }
}