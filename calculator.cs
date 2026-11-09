using System.Globalization;

namespace CalculatorApp;

/// <summary>
/// Логика калькулятора. Ничего не знает о форме — только считает
/// и хранит состояние (текущее число, отложенную операцию, историю).
/// </summary>
public class Calculator
{
    private const int HistoryLinesShown = 6;

    private double _accumulator;          // результат, накопленный к этому моменту
    private string? _pendingOp;           // операция, ждущая второго числа
    private string _input = "0";          // число, которое сейчас набирается
    private bool _startNewNumber = true;  // следующая цифра начнёт новое число

    // для повторного нажатия "=": последняя операция и её второй операнд
    private string? _lastOp;
    private double _lastOperand;

    private string _currentExpression = "";
    private readonly List<string> _history = new();

    public string Display => _input;

    public string HistoryText
    {
        get
        {
            var lines = _history.TakeLast(HistoryLinesShown).ToList();
            if (_currentExpression != "")
                lines.Add(_currentExpression);
            return string.Join(Environment.NewLine, lines);
        }
    }

    private double CurrentValue => double.Parse(_input, CultureInfo.InvariantCulture);

    public void InputDigit(string digit)
    {
        if (_startNewNumber || _input == "0")
            _input = digit;
        else
            _input += digit;
        _startNewNumber = false;
    }

    public void InputDot()
    {
        if (_startNewNumber)
        {
            _input = "0.";
            _startNewNumber = false;
        }
        else if (!_input.Contains('.'))
        {
            _input += ".";
        }
    }

    public void InputOperation(string op)
    {
        double value = CurrentValue;

        if (_pendingOp != null && _startNewNumber)
        {
            // нажали два оператора подряд — просто меняем операцию
            _currentExpression = _currentExpression[..^1] + op;
        }
        else if (_pendingOp != null)
        {
            // 2 + 3 и снова оператор: сразу считаем промежуточный результат
            _accumulator = Apply(_accumulator, _pendingOp, value);
            _currentExpression += $" {Format(value)} {op}";
        }
        else
        {
            _accumulator = value;
            _currentExpression = $"{Format(value)} {op}";
        }

        _pendingOp = op;
        _input = Format(_accumulator);
        _startNewNumber = true;
    }

    public void Calculate()
    {
        double value = CurrentValue;
        double result;

        if (_pendingOp != null)
        {
            result = Apply(_accumulator, _pendingOp, value);
            _history.Add($"{_currentExpression} {Format(value)} = {Format(result)}");
            _lastOp = _pendingOp;
            _lastOperand = value;
        }
        else if (_lastOp != null)
        {
            // повторное "=": применяем последнюю операцию ещё раз
            result = Apply(value, _lastOp, _lastOperand);
            _history.Add($"{Format(value)} {_lastOp} {Format(_lastOperand)} = {Format(result)}");
        }
        else
        {
            return;
        }

        _pendingOp = null;
        _currentExpression = "";
        _input = Format(result);
        _startNewNumber = true;
    }

    public void Backspace()
    {
        if (_startNewNumber)
            return;
        _input = _input[..^1];
        if (_input == "" || _input == "-")
            _input = "0";
    }

    /// <summary>Сбрасывает текущее вычисление. История остаётся.</summary>
    public void Clear()
    {
        _accumulator = 0;
        _pendingOp = null;
        _lastOp = null;
        _input = "0";
        _startNewNumber = true;
        _currentExpression = "";
    }

    public void ClearHistory() => _history.Clear();

    private static double Apply(double a, string op, double b)
    {
        double result = op switch
        {
            "+" => a + b,
            "-" => a - b,
            "*" => a * b,
            "/" => b == 0 ? throw new DivideByZeroException("Деление на ноль невозможно") : a / b,
            _ => throw new InvalidOperationException($"Неизвестная операция: {op}")
        };

        if (double.IsInfinity(result) || double.IsNaN(result))
            throw new OverflowException("Слишком большое число");

        return result;
    }

    private static string Format(double value) => value.ToString("G15", CultureInfo.InvariantCulture);
}
