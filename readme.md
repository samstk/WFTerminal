# Windows Form Terminal Control
Provides a terminal-styled control for windows forms.

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

## Limitations
* May not contain all the QoL improvements that certain window interfaces have.