using UnityEngine;
using TMPro;

public class SudokuUI : MonoBehaviour
{
    public SudokuCell cellPrefab;
    public Transform grid;

    public TMP_Text resultText;

    private SudokuCell selected;

    private SudokuCell[,] cells = new SudokuCell[9, 9];

    private int[,] board = new int[9, 9];
    private int[,] solution = new int[9, 9];

    void Start()
    {
        GenerateSudoku();
        CreateBoard();

        resultText.text = "";
    }

    void CreateBoard()
    {
        for (int y = 0; y < 9; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                SudokuCell cell = Instantiate(cellPrefab, grid);

                bool locked = board[y, x] != 0;

                cell.Init(
                    y,
                    x,
                    board[y, x],
                    locked,
                    this
                );

                cells[y, x] = cell;
            }
        }
    }

    public void SelectCell(SudokuCell cell)
    {
        selected = cell;
        UpdateHighlights();
    }

    public void InputNumber(int number)
    {
        if (selected == null)
            return;

        if (selected.fixedCell)
            return;

        board[selected.row, selected.col] = number;

        bool correct =
            number == 0 ||
            solution[selected.row, selected.col] == number;

        selected.SetValue(number, correct);

        UpdateHighlights();

        CheckWin();
    }

    //==================================================
    // ПОДСВЕТКА
    //==================================================

    void UpdateHighlights()
    {
        foreach (SudokuCell cell in cells)
            cell.ResetVisual();

        if (selected == null)
            return;

        HighlightRowColumn();

        HighlightBox();

        HighlightSameNumbers();

        HighlightConflicts();

        selected.SelectCellColor();
    }

    void HighlightRowColumn()
    {
        for (int i = 0; i < 9; i++)
        {
            cells[selected.row, i].HighlightRowColumn();
            cells[i, selected.col].HighlightRowColumn();
        }
    }

    void HighlightBox()
    {
        int startRow = selected.row / 3 * 3;
        int startCol = selected.col / 3 * 3;

        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                cells[startRow + y, startCol + x].HighlightBox();
            }
        }
    }

    void HighlightSameNumbers()
    {
        int value = selected.Value;

        if (value == 0)
            return;

        for (int y = 0; y < 9; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                if (cells[y, x].Value == value)
                    cells[y, x].HighlightSame();
            }
        }
    }

    void HighlightConflicts()
    {
        for (int y = 0; y < 9; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                if (HasConflict(y, x))
                    cells[y, x].HighlightConflict();
            }
        }
    }

    bool HasConflict(int row, int col)
    {
        int value = cells[row, col].Value;

        if (value == 0)
            return false;

        for (int x = 0; x < 9; x++)
        {
            if (x != col && cells[row, x].Value == value)
                return true;
        }

        for (int y = 0; y < 9; y++)
        {
            if (y != row && cells[y, col].Value == value)
                return true;
        }

        int startRow = row / 3 * 3;
        int startCol = col / 3 * 3;

        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                int r = startRow + y;
                int c = startCol + x;

                if ((r != row || c != col) &&
                    cells[r, c].Value == value)
                    return true;
            }
        }

        return false;
    }

    //==================================================
    // ГЕНЕРАЦИЯ
    //==================================================

    void GenerateSudoku()
    {
        int[,] solved = new int[9, 9];

        FillBoard(solved);

        solution = solved.Clone() as int[,];
        board = solved.Clone() as int[,];

        int remove = 45;

        while (remove > 0)
        {
            int x = Random.Range(0, 9);
            int y = Random.Range(0, 9);

            if (board[y, x] != 0)
            {
                board[y, x] = 0;
                remove--;
            }
        }
    }

    bool FillBoard(int[,] grid)
    {
        for (int y = 0; y < 9; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                if (grid[y, x] == 0)
                {
                    int[] nums = ShuffleNumbers();

                    foreach (int num in nums)
                    {
                        if (IsValid(grid, y, x, num))
                        {
                            grid[y, x] = num;

                            if (FillBoard(grid))
                                return true;

                            grid[y, x] = 0;
                        }
                    }

                    return false;
                }
            }
        }

        return true;
    }

    int[] ShuffleNumbers()
    {
        int[] nums = {1,2,3,4,5,6,7,8,9};

        for (int i = 0; i < nums.Length; i++)
        {
            int rnd = Random.Range(i, nums.Length);

            int temp = nums[i];
            nums[i] = nums[rnd];
            nums[rnd] = temp;
        }

        return nums;
    }

    bool IsValid(int[,] grid, int row, int col, int num)
    {
        for (int i = 0; i < 9; i++)
        {
            if (grid[row, i] == num)
                return false;

            if (grid[i, col] == num)
                return false;
        }

        int boxX = col / 3 * 3;
        int boxY = row / 3 * 3;

        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                if (grid[boxY + y, boxX + x] == num)
                    return false;
            }
        }

        return true;
    }

    //==================================================
    // ПОБЕДА
    //==================================================

    void CheckWin()
    {
        for (int y = 0; y < 9; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                if (board[y, x] == 0)
                    return;

                if (board[y, x] != solution[y, x])
                    return;
            }
        }

        resultText.text = "Победа!";
    }
}