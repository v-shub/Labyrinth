using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class StartGameWithParameters : MonoBehaviour
{
    [Header("Выбор стартовой клетки")]
    [SerializeField] private TMP_Dropdown columnDropdown; // А, Б, В, ...
    [SerializeField] private TMP_Dropdown rowDropdown;    // 1, 2, 3, ...

    [Header("Сцена игры")]
    [SerializeField] private string gameSceneName = "Game";

    // Пока номер игрока фиксирован
    private const int PlayerNum = 1;

    public void OnStartClicked()
    {
        // Индекс выбранного варианта = индекс буквы/цифры в списке,
        // т.к. варианты уже упорядочены и начинаются с (0,0) → "А1".
        int col = columnDropdown.value; // 0 = А, 1 = Б, ...
        int row = rowDropdown.value;    // 0 = 1, 1 = 2, ...

        GameLaunchArgs.Set(PlayerNum, row, col);
        SceneManager.LoadScene(gameSceneName);
    }
}