# Windows Form Terminal Control
Provides a terminal-styled control for windows forms.
Have a look at the Nuget Package [here](https://www.nuget.org/packages/WFTerminal)
- ![Nuget](https://img.shields.io/nuget/v/WFTerminal) ![Nuget](https://img.shields.io/nuget/dt/WFTerminal)
- ![GitHub last commit (by committer)](https://img.shields.io/github/last-commit/samstk/WFTerminal)
## Getting Started
Either install the package in visual studio (under WFTerminal)
or include the source code.
```
Install-Package WFTerminal
```

Add it to your form as normal or initialize it elsewhere.

## Features
* Buffer of set size, similar to console.
* WriteLine, Write functions to display to the console.
* Put function (use after ReadLine called) to add default inputs.
* Clear Function
* ReadKeyEvent, ReadKey, ReadChar, ReadLine functions (UserInputMode)
* SetPlaceholder function for prompts.
* Multi-color writing (output only)
* Selection (incl. Copy + Paste - Ctrl+C/V)
* RedirectStandardConsoleOut function to map System.Console output to the terminal
* Minor Stream support (InputStream single line, OutputStream write to terminal)
* ANSI colour support via WriteAnsi function
* Limited support for other ANSI codes (1.1.0+)
* Limited Support for System / Shell Streams (1.1.0+)

## Limitations
* May not contain all the QoL improvements that certain window interfaces have.

## How To Use
Here are some ways to use this package.
### Basic
This is the intended way to use this package. Since this control relies on events
instead of sequential execution, the Read functions depend on callbacks.

``` cs
terminal2.WriteLine("Hello World!");
terminal2.Write("Please write a command to send to console: ");
terminal2.ReadLine(text =>
{
    stream.Write(text + "\n");
    terminal2.Write("Press any key to continue.");
    terminal2.ReadKeyEvent(args =>
    {
        Client.Disconnect();
        terminal1.SetSystemStream(null);
        terminal1.WriteLine("SSH connection has been closed.");
    });
});
```

### Redirect Standard Output 
Use it to redirect all Console Write calls to this particular control.
``` cs
terminal1.RedirectStandardConsoleOut
```

### SSH / Shell Streams with SetSystemStream
This way allows the terminal to act similar to Putty. Note that this doesn't have full ANSI support
and program's like VIM may break this terminal.

``` cs
Client = new SshClient(sshServer, sshUser, privateKeyFile);
Client.Connect();
ExtendedStream = new MemoryStream();
ShellStream stream = Client.CreateShellStream("ssh-test", terminal1.Columns, terminal1.Rows, (uint)terminal1.Width, (uint)terminal1.Height, 4096);
terminal1.SetSystemStream(stream);
```
