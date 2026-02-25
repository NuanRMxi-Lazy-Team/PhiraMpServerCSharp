namespace PhiraMp.Server.Console;

/// <summary>
/// 控制台命令系统 - 管理和执行控制台命令
/// 支持TAB补全、输入行固定底部、命令历史等高级特性
/// </summary>
public class ConsoleCommandSystem
{
    private readonly Dictionary<string, ConsoleCommand> _commands = new();
    private readonly List<string> _commandHistory = new();
    private readonly object _lock = new();
    private CancellationTokenSource? _cts;
    private Task? _inputTask;
    
    private const string Prompt = "> ";
    private const int MaxHistorySize = 50;
    private int _historyIndex = -1;
    private string _currentInput = "";
    
    /// <summary>
    /// 注册控制台命令
    /// </summary>
    /// <param name="command">要注册的命令</param>
    /// <exception cref="InvalidOperationException">当命令名已被注册时抛出</exception>
    public void RegisterCommand(ConsoleCommand command)
    {
        lock (_lock)
        {
            if (_commands.ContainsKey(command.Name.ToLower()))
            {
                throw new InvalidOperationException($"命令 '{command.Name}' 已被注册");
            }
            
            _commands[command.Name.ToLower()] = command;
            Logger.Info($"已注册控制台命令: {command.Name}");
        }
    }
    
    /// <summary>
    /// 取消注册控制台命令
    /// </summary>
    /// <param name="commandName">命令名称</param>
    /// <returns>如果命令存在并被移除返回 true，否则返回 false</returns>
    public bool UnregisterCommand(string commandName)
    {
        lock (_lock)
        {
            if (_commands.Remove(commandName.ToLower()))
            {
                Logger.Info($"已取消注册控制台命令: {commandName}");
                return true;
            }
            return false;
        }
    }
    
    /// <summary>
    /// 获取所有已注册的命令
    /// </summary>
    public IReadOnlyDictionary<string, ConsoleCommand> GetCommands()
    {
        lock (_lock)
        {
            return new Dictionary<string, ConsoleCommand>(_commands);
        }
    }
    
    /// <summary>
    /// 启动控制台命令监听
    /// </summary>
    public void Start(CancellationToken cancellationToken)
    {
        ConsoleUi.SetPrompt(Prompt);
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _inputTask = Task.Run(() => ListenForCommandsAsync(_cts.Token), _cts.Token);
    }
    
    /// <summary>
    /// 停止控制台命令监听
    /// </summary>
    public void Stop()
    {
        _cts?.Cancel();
        _inputTask?.Wait(TimeSpan.FromSeconds(2));
    }
    
    private async Task ListenForCommandsAsync(CancellationToken cancellationToken)
    {
        // 不在这里输出日志，避免与提示符混淆
        // 日志应该在调用 Start() 之前输出

        // 确保提示符从一个干净的新行开始，避免与上一次输出拼接导致“>”错位
        if (ConsoleUi.CanUseCursorPositioning)
        {
            ConsoleUi.WithLock(() =>
            {
                if (System.Console.CursorLeft != 0)
                    System.Console.WriteLine();
            });
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // 读取一行（该方法会在内部维护/重绘提示符和输入）
                var input = await ReadLineWithFeaturesAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                // 添加到历史记录
                AddToHistory(input);

                // 执行命令
                await ExecuteCommandAsync(input);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "处理控制台命令时发生错误:");
                // 错误也是日志，也需要等待
                await Task.Delay(50);
            }
        }

        // 清理
        System.Console.WriteLine();
    }
    
    private async Task<string> ReadLineWithFeaturesAsync(CancellationToken cancellationToken)
    {
        _currentInput = "";
        int cursorPosition = 0;
        _historyIndex = -1;

        ConsoleUi.SetInputState(true, _currentInput, cursorPosition);

        // 显示提示符（放在这里，而不是 ListenForCommandsAsync 的循环顶部，避免出现“打印了提示符但还没进入输入状态”的窗口期）
        ConsoleUi.WithLock(() =>
        {
            // 始终把提示符放到新行，确保它在最后一行
            if (System.Console.CursorLeft != 0)
                System.Console.WriteLine();

            WritePrompt();
            System.Console.Write(_currentInput);
        });

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!System.Console.KeyAvailable)
            {
                await Task.Delay(10, cancellationToken);
                continue;
            }

            var key = System.Console.ReadKey(true);

            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    System.Console.WriteLine();
                    ConsoleUi.SetInputState(false, string.Empty, 0);
                    return _currentInput;

                case ConsoleKey.Tab:
                    // TAB补全
                    var originalInput = _currentInput;
                    HandleTabCompletion(ref _currentInput, ref cursorPosition);
                    if (_currentInput != originalInput)
                    {
                        ConsoleUi.SetInputState(true, _currentInput, cursorPosition);
                        RedrawInputLine(cursorPosition);
                    }
                    break;

                case ConsoleKey.UpArrow:
                    // 历史记录 - 上一条
                    if (NavigateHistory(true, out var prevCmd))
                    {
                        _currentInput = prevCmd;
                        cursorPosition = _currentInput.Length;
                        ConsoleUi.SetInputState(true, _currentInput, cursorPosition);
                        RedrawInputLine(cursorPosition);
                    }
                    break;

                case ConsoleKey.DownArrow:
                    // 历史记录 - 下一条
                    if (NavigateHistory(false, out var nextCmd))
                    {
                        _currentInput = nextCmd;
                        cursorPosition = _currentInput.Length;
                        ConsoleUi.SetInputState(true, _currentInput, cursorPosition);
                        RedrawInputLine(cursorPosition);
                    }
                    break;

                case ConsoleKey.Backspace:
                    if (_currentInput.Length > 0 && cursorPosition > 0)
                    {
                        _currentInput = _currentInput.Remove(cursorPosition - 1, 1);
                        cursorPosition--;
                        ConsoleUi.SetInputState(true, _currentInput, cursorPosition);
                        RedrawInputLine(cursorPosition);
                    }
                    break;

                case ConsoleKey.LeftArrow:
                    if (cursorPosition > 0)
                    {
                        cursorPosition--;
                        ConsoleUi.SetInputState(true, _currentInput, cursorPosition);
                        if (ConsoleUi.CanUseCursorPositioning)
                            System.Console.SetCursorPosition(Prompt.Length + cursorPosition, System.Console.CursorTop);
                    }
                    break;

                case ConsoleKey.RightArrow:
                    if (cursorPosition < _currentInput.Length)
                    {
                        cursorPosition++;
                        ConsoleUi.SetInputState(true, _currentInput, cursorPosition);
                        if (ConsoleUi.CanUseCursorPositioning)
                            System.Console.SetCursorPosition(Prompt.Length + cursorPosition, System.Console.CursorTop);
                    }
                    break;

                case ConsoleKey.Home:
                    cursorPosition = 0;
                    ConsoleUi.SetInputState(true, _currentInput, cursorPosition);
                    if (ConsoleUi.CanUseCursorPositioning)
                        System.Console.SetCursorPosition(Prompt.Length, System.Console.CursorTop);
                    break;

                case ConsoleKey.End:
                    cursorPosition = _currentInput.Length;
                    ConsoleUi.SetInputState(true, _currentInput, cursorPosition);
                    if (ConsoleUi.CanUseCursorPositioning)
                        System.Console.SetCursorPosition(Prompt.Length + cursorPosition, System.Console.CursorTop);
                    break;

                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        _currentInput = _currentInput.Insert(cursorPosition, key.KeyChar.ToString());
                        cursorPosition++;
                        ConsoleUi.SetInputState(true, _currentInput, cursorPosition);
                        RedrawInputLine(cursorPosition);
                    }
                    break;
            }
        }

        ConsoleUi.SetInputState(false, string.Empty, 0);
        return _currentInput;
    }

    private void RedrawInputLine(int cursorPosition)
    {
        ConsoleUi.WithLock(() =>
        {
            ClearCurrentInputLine();
            WritePrompt();
            System.Console.Write(_currentInput);
            if (ConsoleUi.CanUseCursorPositioning && cursorPosition < _currentInput.Length)
            {
                System.Console.SetCursorPosition(Prompt.Length + cursorPosition, System.Console.CursorTop);
            }
        });
    }
    
    /// <summary>
    /// 导航历史记录
    /// </summary>
    private bool NavigateHistory(bool up, out string command)
    {
        command = "";
        lock (_lock)
        {
            if (_commandHistory.Count == 0)
                return false;
            
            if (up)
            {
                if (_historyIndex < _commandHistory.Count - 1)
                {
                    _historyIndex++;
                    command = _commandHistory[_commandHistory.Count - 1 - _historyIndex];
                    return true;
                }
            }
            else
            {
                if (_historyIndex > 0)
                {
                    _historyIndex--;
                    command = _commandHistory[_commandHistory.Count - 1 - _historyIndex];
                    return true;
                }
                else if (_historyIndex == 0)
                {
                    _historyIndex = -1;
                    command = "";
                    return true;
                }
            }
        }
        return false;
    }
    
    /// <summary>
    /// 清除当前输入行
    /// </summary>
    private void ClearCurrentInputLine()
    {
        var currentTop = System.Console.CursorTop;
        System.Console.SetCursorPosition(0, currentTop);

        // Use BufferWidth, not WindowWidth, to avoid clearing too little on some hosts.
        var width = 0;
        try { width = System.Console.BufferWidth; } catch { /* ignore */ }
        if (width <= 0) width = System.Console.WindowWidth;

        System.Console.Write(new string(' ', Math.Max(0, width - 1)));
        System.Console.SetCursorPosition(0, currentTop);
    }
    
    /// <summary>
    /// 处理TAB补全
    /// </summary>
    private void HandleTabCompletion(ref string input, ref int cursorPosition)
    {
        if (string.IsNullOrWhiteSpace(input))
            return;
        
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return;
        
        var commandPrefix = parts[0].ToLower();
        
        lock (_lock)
        {
            var matches = _commands.Keys
                .Where(cmd => cmd.StartsWith(commandPrefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(cmd => cmd)
                .ToList();
            
            if (matches.Count == 1)
            {
                // 唯一匹配，直接补全
                var completed = matches[0];
                if (parts.Length > 1)
                {
                    input = completed + " " + string.Join(" ", parts.Skip(1));
                }
                else
                {
                    input = completed + " ";
                }
                cursorPosition = input.Length;
            }
            else if (matches.Count > 1)
            {
                // 多个匹配，显示所有可能的命令
                System.Console.WriteLine();
                System.Console.ForegroundColor = ConsoleColor.Cyan;
                System.Console.WriteLine($"可能的命令: {string.Join(", ", matches)}");
                System.Console.ResetColor();
                
                // 找到共同前缀
                var commonPrefix = GetCommonPrefix(matches);
                if (commonPrefix.Length > commandPrefix.Length)
                {
                    if (parts.Length > 1)
                    {
                        input = commonPrefix + " " + string.Join(" ", parts.Skip(1));
                    }
                    else
                    {
                        input = commonPrefix;
                    }
                    cursorPosition = input.Length;
                }
            }
        }
    }
    
    /// <summary>
    /// 获取字符串列表的共同前缀
    /// </summary>
    private string GetCommonPrefix(List<string> strings)
    {
        if (strings.Count == 0) return "";
        if (strings.Count == 1) return strings[0];
        
        var prefix = strings[0];
        for (int i = 1; i < strings.Count; i++)
        {
            while (!strings[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                prefix = prefix[..^1];
                if (prefix.Length == 0) return "";
            }
        }
        return prefix;
    }
    
    /// <summary>
    /// 添加命令到历史记录
    /// </summary>
    private void AddToHistory(string command)
    {
        lock (_lock)
        {
            // 避免连续重复的命令
            if (_commandHistory.Count > 0 && _commandHistory[^1] == command)
                return;
            
            _commandHistory.Add(command);
            
            // 限制历史记录大小
            if (_commandHistory.Count > MaxHistorySize)
            {
                _commandHistory.RemoveAt(0);
            }
        }
    }
    
    /// <summary>
    /// 写入命令提示符
    /// </summary>
    private void WritePrompt()
    {
        System.Console.ForegroundColor = ConsoleColor.Green;
        System.Console.Write(Prompt);
        System.Console.ResetColor();

        // Mark prompt/input as active (even if input buffer is currently empty)
        ConsoleUi.SetInputState(true, _currentInput, Math.Clamp(_currentInput.Length, 0, _currentInput.Length));
    }
    
    /// <summary>
    /// 执行控制台命令
    /// </summary>
    /// <param name="input">用户输入的完整命令字符串</param>
    public async Task ExecuteCommandAsync(string input)
    {
        var parts = ParseCommandInput(input);
        if (parts.Length == 0) return;
        
        var commandName = parts[0].ToLower();
        var args = parts.Skip(1).ToArray();
        
        ConsoleCommand? command;
        lock (_lock)
        {
            if (!_commands.TryGetValue(commandName, out command))
            {
                Logger.Warning($"未知命令: {commandName}，输入 'help' 查看可用命令");
                return;
            }
        }
        
        try
        {
            await command.ExecuteAsync(args);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"执行命令 '{commandName}' 时发生错误:");
        }
    }
    
    /// <summary>
    /// 解析命令输入为命令和参数
    /// </summary>
    private string[] ParseCommandInput(string input)
    {
        var parts = new List<string>();
        var current = "";
        var inQuotes = false;
        
        for (int i = 0; i < input.Length; i++)
        {
            var c = input[i];
            
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    parts.Add(current);
                    current = "";
                }
            }
            else
            {
                current += c;
            }
        }
        
        if (current.Length > 0)
        {
            parts.Add(current);
        }
        
        return parts.ToArray();
    }
}



