class Menu
{
    public List<(Func<string> name, Action<ConsoleKeyInfo> action)> Options = new();
    public bool IsOpen = false;
    private int selectedIndex = 0;
    private int maxSize = 0;
    public int SelectedIndex { get => Math.Clamp(selectedIndex, 0, Options.Count - 1); set => selectedIndex = Math.Clamp(value, 0, Options.Count - 1); }

    public virtual void Build()
    {

    }

    public void Show() => Show(new(default, ConsoleKey.Enter, false, false, false));
    public void Show(ConsoleKeyInfo info)
    {
        if (info.Key != ConsoleKey.Enter)
            return;

        Build();

        Console.Clear();
        IsOpen = true;
        do
        {
            Console.CursorTop = 0;
            maxSize = Math.Max(maxSize, Options.Count);
            for (int k = 0; k < Options.Count; k++)
            {
                Console.ForegroundColor = k == SelectedIndex ? ConsoleColor.Green : ConsoleColor.Gray;
                Console.WriteLine(Options[k].name().PadRight(Console.WindowWidth));
            }
            for (int k = Options.Count; k < maxSize; k++)
                Console.WriteLine("".PadRight(Console.WindowWidth));

            info = Console.ReadKey(true);

            int last = -1;
            if (info.Key == ConsoleKey.UpArrow)
                do
                {
                    last = SelectedIndex;
                    SelectedIndex -= 1;
                    if (SelectedIndex == last)
                    {
                        do SelectedIndex += 1; while (Options[SelectedIndex].action is null);
                        break;
                    }
                } while (Options[SelectedIndex].action is null && SelectedIndex != last);
            if (info.Key == ConsoleKey.DownArrow)
                do SelectedIndex += 1; while (Options[SelectedIndex].action is null);
            if (info.Key == ConsoleKey.Escape)
                IsOpen = false;

            Options[SelectedIndex].action(info);
        } while (IsOpen);
        Console.Clear();
    }
}
