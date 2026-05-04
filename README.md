# todo

A simple CLI todo manager built with C# and [Spectre.Console](https://spectreconsole.net/).

Todos stored as human-readable markdown at `~/.todo/todos.md`.

## Usage

```
todo                    # list todos
todo add "buy milk"     # add a todo
todo done 2             # mark #2 complete
```

Running `add` without text or `done` without a number will prompt interactively.

Completed todos disappear from the list after 1 day.

## Build

Requires [.NET 10](https://dotnet.microsoft.com/download) SDK.

```bash
dotnet publish -c Release -o out
```

This produces a self-contained binary at `out/todo`.

## Install

Copy the binary somewhere on your `$PATH`:

```bash
cp out/todo /usr/local/bin/
```

Or add an alias to your shell config (`~/.zshrc` / `~/.bashrc`):

```bash
alias todo='/path/to/todo/out/todo'
```

Then reload your shell:

```bash
source ~/.zshrc
```

## Storage

```markdown
# Todo

- [ ] Buy milk <!-- added:2026-05-04 -->
- [x] Walk the dog <!-- added:2026-05-03 done:2026-05-04 -->
```
