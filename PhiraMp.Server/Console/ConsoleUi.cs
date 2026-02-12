namespace PhiraMp.Server.Console;

/// <summary>
/// Coordinates console output with the interactive command prompt.
/// Ensures logs don't overwrite the current input line and the prompt stays aligned.
/// </summary>
public static class ConsoleUi
{
    private static readonly object LockObj = new();

    private static bool _inputActive;
    private static string _prompt = "> ";
    private static string _currentInput = string.Empty;
    private static int _cursorPosition;

    /// <summary>
    /// Whether we can safely use cursor positioning APIs.
    /// When output is redirected, Console APIs like CursorTop/SetCursorPosition may throw.
    /// </summary>
    public static bool CanUseCursorPositioning
    {
        get
        {
            try
            {
                return !System.Console.IsOutputRedirected && !System.Console.IsErrorRedirected;
            }
            catch
            {
                return false;
            }
        }
    }

    public static void SetPrompt(string prompt)
    {
        lock (LockObj)
        {
            _prompt = prompt;
        }
    }

    public static void SetInputState(bool active, string currentInput, int cursorPosition)
    {
        lock (LockObj)
        {
            _inputActive = active;
            _currentInput = currentInput.Length == 0 ? string.Empty : currentInput;
            _cursorPosition = Math.Clamp(cursorPosition, 0, _currentInput.Length);
        }
    }

    public static void WithLock(Action action)
    {
        lock (LockObj)
        {
            action();
        }
    }

    /// <summary>
    /// Write a line to the console without corrupting the current prompt/input.
    /// </summary>
    public static void WriteLinePreservingInput(Action writeLine)
    {
        lock (LockObj)
        {
            if (!_inputActive || !CanUseCursorPositioning)
            {
                writeLine();
                return;
            }

            try
            {
                var top = System.Console.CursorTop;

                // Clear current input line
                System.Console.SetCursorPosition(0, top);
                ClearLineUnsafe();
                System.Console.SetCursorPosition(0, top);

                // Print the log line
                writeLine();

                // Re-render prompt + input
                RenderPromptAndInputUnsafe();

                // Restore cursor to the previous logical position
                var desiredLeft = _prompt.Length + _cursorPosition;
                desiredLeft = Math.Clamp(desiredLeft, 0, Math.Max(0, System.Console.BufferWidth - 1));
                var desiredTop = System.Console.CursorTop;
                System.Console.SetCursorPosition(desiredLeft, desiredTop);
            }
            catch
            {
                // If cursor APIs fail (resizing, redirected output, etc.), just print.
                writeLine();
            }
        }
    }

    private static void RenderPromptAndInputUnsafe()
    {
        // Put us at a fresh line to avoid appending to log text.
        if (System.Console.CursorLeft != 0)
            System.Console.WriteLine();

        System.Console.ForegroundColor = ConsoleColor.Green;
        System.Console.Write(_prompt);
        System.Console.ResetColor();

        System.Console.Write(_currentInput);
    }

    private static void ClearLineUnsafe()
    {
        var width = 0;
        try
        {
            width = System.Console.BufferWidth;
        }
        catch
        {
            // ignore
        }

        if (width <= 0)
        {
            // Fallback: overwrite a reasonable amount
            System.Console.Write(new string(' ', 200));
        }
        else
        {
            System.Console.Write(new string(' ', Math.Max(0, width - 1)));
        }
    }
}
