using System.Text.RegularExpressions;
using Spectre.Console;

var todoDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".todo");
var todoFile = Path.Combine(todoDir, "todos.md");
var today = DateOnly.FromDateTime(DateTime.Now);

var command = args.Length > 0 ? args[0].ToLower() : "list";

switch (command)
{
    case "list":
        ListTodos();
        break;
    case "add":
        var text = args.Length > 1 ? string.Join(" ", args[1..]) : null;
        if (string.IsNullOrWhiteSpace(text))
            text = AnsiConsole.Prompt(new TextPrompt<string>("What do you need to do?"));
        AddTodo(text);
        break;
    case "done":
        int? number = args.Length > 1 && int.TryParse(args[1], out var n) ? n : null;
        CompleteTodo(number);
        break;
    default:
        AnsiConsole.MarkupLine("[yellow]Usage:[/]");
        AnsiConsole.MarkupLine("  todo              List all todos");
        AnsiConsole.MarkupLine("  todo add \"text\"   Add a new todo");
        AnsiConsole.MarkupLine("  todo done <#>     Mark a todo complete");
        break;
}

List<TodoItem> LoadTodos()
{
    if (!File.Exists(todoFile))
        return [];

    var todos = new List<TodoItem>();
    var metaRegex = new Regex(@"<!-- added:(\d{4}-\d{2}-\d{2})(?:\s+done:(\d{4}-\d{2}-\d{2}))? -->$");

    foreach (var line in File.ReadAllLines(todoFile))
    {
        bool done;
        string rest;
        if (line.StartsWith("- [x] "))
            (done, rest) = (true, line[6..]);
        else if (line.StartsWith("- [ ] "))
            (done, rest) = (false, line[6..]);
        else
            continue;

        var added = today;
        DateOnly? completed = null;

        var match = metaRegex.Match(rest);
        if (match.Success)
        {
            added = DateOnly.Parse(match.Groups[1].Value);
            if (match.Groups[2].Success)
                completed = DateOnly.Parse(match.Groups[2].Value);
            rest = rest[..match.Index].TrimEnd();
        }

        todos.Add(new TodoItem(done, rest, added, completed));
    }
    return todos;
}

void SaveTodos(List<TodoItem> todos)
{
    Directory.CreateDirectory(todoDir);
    var lines = new List<string> { "# Todo", "" };
    foreach (var todo in todos)
    {
        var check = todo.Done ? "x" : " ";
        var addedFmt = todo.Added.ToString("yyyy-MM-dd");
        var meta = todo.Completed is not null
            ? $" <!-- added:{addedFmt} done:{todo.Completed.Value.ToString("yyyy-MM-dd")} -->"
            : $" <!-- added:{addedFmt} -->";
        lines.Add($"- [{check}] {todo.Text}{meta}");
    }
    File.WriteAllLines(todoFile, lines);
}

void ListTodos()
{
    var todos = LoadTodos();
    // Filter out done items older than 1 day
    var visible = todos
        .Where(t => !t.Done || (t.Completed is not null && today.DayNumber - t.Completed.Value.DayNumber <= 1))
        .ToList();

    if (visible.Count == 0)
    {
        AnsiConsole.MarkupLine("[dim]No todos yet. Add one with:[/] [blue]todo add \"something\"[/]");
        return;
    }

    var table = new Table()
        .Border(TableBorder.Rounded)
        .AddColumn(new TableColumn("[bold]#[/]").RightAligned())
        .AddColumn("[bold]Status[/]")
        .AddColumn("[bold]Description[/]")
        .AddColumn("[bold]Added[/]")
        .AddColumn("[bold]Completed[/]");

    for (var i = 0; i < todos.Count; i++)
    {
        var todo = todos[i];
        if (!visible.Contains(todo)) continue;

        var num = (i + 1).ToString();
        var addedStr = todo.Added.ToString("yyyy-MM-dd");
        if (todo.Done)
        {
            var doneStr = todo.Completed?.ToString("yyyy-MM-dd") ?? "";
            table.AddRow($"[dim]{num}[/]", "[green]done[/]",
                $"[strikethrough dim]{Markup.Escape(todo.Text)}[/]",
                $"[dim]{addedStr}[/]", $"[dim]{doneStr}[/]");
        }
        else
        {
            table.AddRow(num, "[yellow]pending[/]", Markup.Escape(todo.Text), addedStr, "");
        }
    }

    AnsiConsole.Write(table);
}

void AddTodo(string text)
{
    var todos = LoadTodos();
    todos.Add(new TodoItem(false, text, today, null));
    SaveTodos(todos);
    AnsiConsole.MarkupLine($"[green]Added:[/] {Markup.Escape(text)}");
}

void CompleteTodo(int? number)
{
    var todos = LoadTodos();
    if (todos.Count == 0)
    {
        AnsiConsole.MarkupLine("[dim]No todos to complete.[/]");
        return;
    }

    if (number is null)
    {
        var choices = todos
            .Select((t, i) => (t, i))
            .Where(x => !x.t.Done)
            .Select(x => $"{x.i + 1}. {x.t.Text}")
            .ToList();
        if (choices.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]All todos already done.[/]");
            return;
        }
        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Which todo is done?")
                .AddChoices(choices));
        number = int.Parse(selected.Split('.')[0]);
    }

    if (number < 1 || number > todos.Count)
    {
        AnsiConsole.MarkupLine($"[red]Invalid number. Pick 1-{todos.Count}.[/]");
        return;
    }

    var idx = number.Value - 1;
    var todo = todos[idx];
    todos[idx] = todo with { Done = true, Completed = today };
    SaveTodos(todos);
    AnsiConsole.MarkupLine($"[green]Done:[/] [strikethrough]{Markup.Escape(todo.Text)}[/]");
}

record TodoItem(bool Done, string Text, DateOnly Added, DateOnly? Completed);
