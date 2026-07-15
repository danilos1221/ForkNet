using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SudokuCell : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text text;
    public Button button;
    public Image background;

    [Header("Text Colors")]
    public Color fixedColor = Color.black;
    public Color playerColor = Color.blue;
    public Color wrongColor = Color.red;

    [Header("Background Colors")]
    public Color normalColor = Color.white;
    public Color rowColumnColor = new Color(0.92f, 0.92f, 0.92f);
    public Color boxColor = new Color(0.86f, 0.86f, 0.86f);
    public Color sameNumberColor = new Color(0.75f, 0.95f, 0.75f);
    public Color selectedColor = new Color(0.65f, 0.82f, 1f);
    public Color conflictColor = new Color(1f, 0.65f, 0.65f);

    public int row;
    public int col;

    public bool fixedCell;

    private SudokuUI sudoku;

    public int Value
    {
        get
        {
            if (string.IsNullOrEmpty(text.text))
                return 0;

            return int.Parse(text.text);
        }
    }

    public void Init(
        int r,
        int c,
        int value,
        bool fixedValue,
        SudokuUI manager)
    {
        row = r;
        col = c;
        fixedCell = fixedValue;
        sudoku = manager;

        background.color = normalColor;

        if (value == 0)
        {
            text.text = "";
            text.color = playerColor;
        }
        else
        {
            text.text = value.ToString();
            text.color = fixedColor;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(Select);
    }

    void Select()
    {
        sudoku.SelectCell(this);
    }

    public void SetValue(int value, bool correct)
    {
        if (fixedCell)
            return;

        if (value == 0)
        {
            text.text = "";
            text.color = playerColor;
            return;
        }

        text.text = value.ToString();

        text.color = correct ? playerColor : wrongColor;
    }

    //====================================================
    // Подсветка
    //====================================================

    public void ResetVisual()
    {
        background.color = normalColor;
    }

    public void HighlightRowColumn()
    {
        background.color = rowColumnColor;
    }

    public void HighlightBox()
    {
        if (background.color == normalColor)
            background.color = boxColor;
    }

    public void HighlightSame()
    {
        background.color = sameNumberColor;
    }

    public void HighlightConflict()
    {
        background.color = conflictColor;
    }

    public void SelectCellColor()
    {
        background.color = selectedColor;
    }
}