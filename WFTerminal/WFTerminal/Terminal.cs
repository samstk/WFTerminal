/**
 * By Samuel Sticklen (46962487)
 * - 2023.
 * 
 * Provides a simple aesthetic terminal control for use under MIT license.
 * 
 * Github: www.github.com/samstk
 */

using System.ComponentModel;
using System.Diagnostics;
using System.Security.Policy;
using System.Text;

namespace WFTerminal
{
    /// <summary>
    /// Represents a callback from allowing the user to type a single key,
    /// returning the immediate key event args the control received.
    /// </summary>
    /// <param name="e">the key events received</param>
    public delegate void TerminalKeyEventCallback(KeyEventArgs e);

    /// <summary>
    /// Represents a callback from allowing the user to type a single key,
    /// returning the key the control received.
    /// </summary>
    /// <param name="key">the key pressed</param>
    public delegate void TerminalKeyCallback(Keys key);

    /// <summary>
    /// Represents a callback from allowing the user to type a single key.
    /// </summary>
    /// <param name="c">the char returned (converted from key callback)</param>
    public delegate void TerminalCharCallback(char c);

    /// <summary>
    /// Represents a callback from allowing the user to input a line.
    /// </summary>
    /// <param name="line">the line that the user typed</param>
    public delegate void TerminalLineCallback(string line);

    /// <summary>
    /// A user control that contains a terminal-like interface.
    /// </summary>
    public partial class Terminal : UserControl
    {
        public Terminal()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
        }

        #region Data Input
        /// <summary>
        /// Gets whether the terminal is currently in user input mode.
        /// If so, then nothing can be written to the terminal.
        /// </summary>
        public bool UserInputMode { get; private set; } = false;

        /// <summary>
        /// Gets whether the current input is visible.
        /// </summary>
        public bool UserTypingVisible { get; private set; } = false;
        
        /// <summary>
        /// Gets the stored invisible input when UserTypingVisible is false,
        /// and the user has typed.
        /// </summary>
        private string _StoredInvisibleUserInput = null;

        /// <summary>
        /// Gets the index where the user input started.
        /// </summary>
        private int _UserInputStartIndex = 0;

        /// <summary>
        /// Gets the current user input (or null if not in input mode)
        /// </summary>
        public string CurrentUserInput
        {
            get
            {
                if (!UserTypingVisible)
                {
                    return _StoredInvisibleUserInput;
                }
                else
                {
                    string line = "";
                    int readPos = _UserInputStartIndex;
                    while(true)
                    {
                        char c = _Buffer[readPos++];

                        if (c == '\0')
                            break;

                        line += c;

                        if (readPos >= MAX_BUFFER_SIZE)
                            readPos = 0;
                    }

                    return line;
                }
            }
        }

        // Callbacks for read functions
        private TerminalKeyEventCallback? _CurrentKeyEventCallback;
        private TerminalKeyCallback? _CurrentKeyCallback;
        private TerminalCharCallback? _CurrentCharCallback;
        private TerminalLineCallback? _CurrentLineCallback;
        

        /// <summary>
        /// Initiates the input mode, and reads a single key press in the terminal.
        /// </summary>
        /// <param name="callback">the callback handling the key press</param>
        public void ReadKeyEvent(TerminalKeyEventCallback callback)
        {
            _CurrentKeyEventCallback = callback;
            _CurrentKeyCallback = null;
            _CurrentCharCallback = null;
            _CurrentLineCallback = null;
            
            UserInputMode = true;
            _UserInputStartIndex = _BufferCurrentPosition;
            UserTypingVisible = true;
        }

        /// <summary>
        /// Initiates the input mode, and reads a single key press in the terminal.
        /// </summary>
        /// <param name="callback">the callback handling the key press</param>
        public void ReadKey(TerminalKeyCallback callback)
        {
            _CurrentKeyEventCallback = null;
            _CurrentKeyCallback = callback;
            _CurrentCharCallback = null;
            _CurrentLineCallback = null;

            UserInputMode = true;
            _UserInputStartIndex = _BufferCurrentPosition;
            UserTypingVisible = true;
        }


        /// <summary>
        /// Initiates the input mode, and reads a single key press in the terminal, resulting
        /// in a single char output (conversion of what the key represents)
        /// </summary>
        /// <remarks>
        /// If the key pressed is enter, then it will simply return '\n'
        /// </remarks>
        /// <param name="callback">the callback handling the key press</param>
        public void ReadChar(TerminalCharCallback callback)
        {
            _CurrentKeyEventCallback = null;
            _CurrentCharCallback = callback;
            _CurrentLineCallback = null;
            UserInputMode = true;
            _UserInputStartIndex = _BufferCurrentPosition;
            UserTypingVisible = true;
        }

        /// <summary>
        /// Initiates the input mode, and reads a line from the terminal (i.e.
        /// the string from the current position of where the input began, and
        /// the user input.
        /// </summary>
        /// <param name="callback">the callback handling the input</param>
        /// <param name="visibleTyping">
        /// if set to false, then the output won't be sent to the console buffer,
        /// but instead stored elsewhere.
        /// </param>
        public void ReadLine(TerminalLineCallback callback, bool visibleTyping=true)
        {
            _CurrentKeyEventCallback = null;
            _CurrentCharCallback = null;
            _CurrentLineCallback = callback;
            UserInputMode = true;
            UserTypingVisible = visibleTyping;
            _StoredInvisibleUserInput = "";
            _UserInputStartIndex = _BufferCurrentPosition;
        }

        /// <summary>
        /// Cancels the current input if applicable.
        /// </summary>
        /// <remarks>
        /// It is upto the software to handle closing the line (e.g. add a line break after an interrupted input)
        /// </remarks>
        public void EndInput()
        {
            UserInputMode = false;
            _CurrentKeyEventCallback = null;
            _CurrentCharCallback = null;
            _CurrentLineCallback = null;
            _StoredInvisibleUserInput = null;
        }
        #endregion

        #region Appearance
        /// <summary>
        /// Sets the width of a single cell in the terminal
        /// </summary>
        [Description("Sets the width of a single cell in the terminal"), Category("Appearance")]
        public int CellWidth { get; set; } = 9;

        /// <summary>
        /// Sets the height of a single cell in the terminal
        /// </summary>
        [Description("Sets the height of a single cell in the terminal"), Category("Appearance")]
        public int CellHeight { get; set; } = 13;

        /// <summary>
        /// If true, shows the _ character on top of the cursor position
        /// </summary>
        [Description("If true, shows the _ character on top of the cursor position"), Category("Appearance")]
        public bool ShowCursorPosition { get; set; } = true;

        /// <summary>
        /// Gets or sets the font used for the terminal.
        /// </summary>
        [Description("Gets or sets the font used for the terminal"), Category("Appearance")]
        public Font TerminalFont { get; set; } = new Font("Lucida Console", 12f);

        /// <summary>
        /// Gets or sets the default placeholder color for the terminal
        /// </summary>
        [Description("Gets or sets the default placeholder color for the terminal"), Category("Appearance")]
        public Color DefaultPlaceholderColor { get; set; } = Color.Gray;

        /// <summary>
        /// Gets or sets the default color for terminal input
        /// </summary>
        [Description("Gets or sets the default color for terminal input"), Category("Appearance")]
        public Color InputColor { get; set; } = Color.LightGray;

        /// <summary>
        /// Gets or sets the default color for terminal highlighting during selection
        /// </summary>
        [Description("Gets or sets the default color for terminal highlighting during selection"), Category("Appearance")]
        public Color HighlightColor { get; set; } = Color.FromArgb(44,44,44);
        #endregion

        /// <summary>
        /// Gets the dictionary of colors to brush mappings that persists
        /// for the lifetime of the app.
        /// </summary>
        public static Dictionary<Color, SolidBrush> TextBrushes { get; private set; } = new Dictionary<Color, SolidBrush>();

        #region Buffer & Modification (Write and Read functions)
        public const int MAX_BUFFER_SIZE = 16000;

        /// <summary>
        /// Get or set the characters stored in the buffer.
        /// </summary>
        private char[] _Buffer = new char[MAX_BUFFER_SIZE];

        /// <summary>
        /// Get or set the colours set on the buffer
        /// </summary>
        private SolidBrush[] _BufferColours = new SolidBrush[MAX_BUFFER_SIZE];

        /// <summary>
        /// Get or set the location (char index) of the starting line to display.
        /// </summary>
        private int _BufferDisplayLine = 0;

        /// <summary>
        /// Get or set the current position in the buffer.
        /// </summary>
        private int _BufferCurrentPosition = 0;

        /// <summary>
        /// Get or set the select position in the buffer
        /// </summary>
        private int _BufferSelectPosition = -1;
        
        /// <summary>
        /// Get or set the select length in the buffer
        /// </summary>
        private int _BufferSelectLength = 0;

        /// <summary>
        /// Gets the solid brush, that is given by the color.
        /// </summary>
        /// <param name="color">the color to find the brush for</param>
        /// <returns>the brush o</returns>
        private SolidBrush FindBrush(Color color)
        {
            if (TextBrushes.ContainsKey(color))
                return TextBrushes[color];

            // Add new entry of brush into cache.
            SolidBrush brush = new SolidBrush(color);
            TextBrushes.Add(color, brush);
            return brush;
        }

        /// <summary>
        /// Clears everything on the terminal
        /// </summary>
        /// <param name="refreshDisplay">
        /// if true, the control is immediately refresh after the operation is completed.
        /// </param>
        public void Clear(bool refreshDisplay=true)
        {
            EndInput();
            _BufferCurrentPosition = 0;
            _Placeholder = null;
            _PlaceholderIndex = -1;
            _BufferDisplayLine = 0;
            _BufferSelectPosition = -1;
            _BufferSelectLength = 0;
            _Buffer[0] = '\0';
            _Buffer[1] = '\0';
            _Buffer[MAX_BUFFER_SIZE - 1] = '\0';
            if (refreshDisplay)
                Refresh();
        }

        /// <summary>
        /// Inserts the given text at the current position.
        /// Only usable in input mode (call function after read line)
        /// </summary>
        /// <remarks>
        /// If the input is invisible (i.e. password), then it will only append
        /// to the end of the current input.
        /// </remarks>
        /// <param name="text">the text to write to the console</param> 
        /// <param name="refreshDisplay">
        /// if true, the control is immediately refresh after the operation is completed.
        /// </param>
        public void Put(string text, bool refreshDisplay=true)
        {
            Put(text, InputColor, refreshDisplay);
        }

        /// <summary>
        /// Inserts the given text at the current position.
        /// Only usable in input mode.
        /// </summary>
        /// <remarks>
        /// If the input is invisible (i.e. password), then it will only append
        /// to the end of the current input.
        /// </remarks>
        /// <param name="text">the text to write to the console</param>
        /// <param name="color">the color to write the text</param> 
        /// <param name="refreshDisplay">
        /// if true, the control is immediately refresh after the operation is completed.
        /// </param>
        public void Put(string text, Color color, bool refreshDisplay=true)
        {
            if (!UserInputMode) return;
            if (UserTypingVisible)
            {
                _Insert(text, color, refreshDisplay);
            }
            else
            {
                _StoredInvisibleUserInput += _StoredInvisibleUserInput;
                if (refreshDisplay)
                    Refresh();
            }
        }

        /// <summary>
        /// Writes the given line to the console at the current position.
        /// </summary>
        /// <param name="text">the text to write to the console</param>
        /// <param name="refreshDisplay">
        /// if true, the control is immediately refresh after the operation is completed.
        /// </param>
        public void Write(string text, bool refreshDisplay = true)
        {
            Write(text, ForeColor, refreshDisplay);
        }

        /// <summary>
        /// Writes the given line to the console at the current position.
        /// </summary>
        /// <param name="text">the text to write to the console</param>
        /// <param name="color">the color to write the text</param>
        /// <param name="refreshDisplay">
        /// if true, the control is immediately refresh after the operation is completed.
        /// </param>
        public void Write(string text, Color color, bool refreshDisplay = true)
        {
            if (UserInputMode) return; // No typing can be done when input is expected.

            _Write(text, color, refreshDisplay);
        }

        /// <summary>
        /// Deletes the last character and shifts all characters backwards
        /// </summary>
        /// <param name="refreshDisplay">
        /// if true, the control is immediately refresh after the operation is completed.
        /// </param>
        private void _Delete(bool refreshDisplay=true)
        {
            int readPos = _BufferCurrentPosition - 1;
            if (readPos < 0)
                readPos = MAX_BUFFER_SIZE - 1;

            int lastPos = readPos;
            while(true)
            {
                char c = _Buffer[readPos];

                if(lastPos != readPos)
                {
                    _Buffer[lastPos] = c;
                }

                lastPos = readPos;
                readPos++;
                if(readPos >= MAX_BUFFER_SIZE)
                {
                    readPos = 0;
                }


                if (c == '\0')
                {
                    // End of buffer, just add one null char after (keep 2)
                    _Buffer[readPos] = '\0';
                    break;
                }
            }

            
            _BufferCurrentPosition--;

            if(refreshDisplay)
                Refresh();
        }

        /// <summary>
        /// Writes the given line to the console at the current position.
        /// </summary>
        /// <param name="text">the text to write to the console</param>
        /// <param name="color">the color to write the text</param>
        /// <param name="refreshDisplay">
        /// if true, the control is immediately refresh after the operation is completed.
        /// </param>
        private void _Write(string text, Color color, bool refreshDisplay = true)
        {
            for (int i = 0; i < text.Length; i++)
            {
                _Buffer[_BufferCurrentPosition] = text[i];
                _BufferColours[_BufferCurrentPosition] = FindBrush(color);

                // Increment buffer position and reset to zero if needed.
                _BufferCurrentPosition++;
                if (_BufferCurrentPosition >= MAX_BUFFER_SIZE)
                {
                    _BufferCurrentPosition = 0;
                }
            }

            // Write two positions ahead to avoid reading past this position
            _Buffer[_BufferCurrentPosition] = '\0';

            if (_BufferCurrentPosition + 1 >= MAX_BUFFER_SIZE)
                _Buffer[0] = '\0';
            else _Buffer[_BufferCurrentPosition + 1] = '\0';

            ScrollToCurrentPosition(false);

            if (refreshDisplay)
                Refresh();
        }

        /// <summary>
        /// Inserts the given line to the console at the current position.
        /// </summary>
        /// <param name="text">the text to write to the console</param>
        /// <param name="color">the color to write the text</param>
        /// <param name="refreshDisplay">
        /// if true, the control is immediately refresh after the operation is completed.
        /// </param>
        private void _Insert(string text, Color color, bool refreshDisplay = true)
        {
            int textToPush = 2;

            // Calculate how much text to push
            int readPos = _BufferCurrentPosition;
            while(true)
            {
                char c = _Buffer[readPos];

                if (c == '\0')
                {
                    break;
                }

                textToPush++;
                readPos++;
                if (readPos > MAX_BUFFER_SIZE)
                    readPos = 0;
            }

            // Push all text
            readPos = (_BufferCurrentPosition + textToPush - 1) % MAX_BUFFER_SIZE;
            int writePos = (_BufferCurrentPosition + text.Length + textToPush - 1) % MAX_BUFFER_SIZE;

            while(textToPush > 0)
            {
                _Buffer[writePos] = _Buffer[readPos];
                _BufferColours[writePos--] = _BufferColours[readPos--];
                
                if (writePos < 0)
                    writePos = MAX_BUFFER_SIZE - 1;

                if (readPos < 0)
                    readPos = MAX_BUFFER_SIZE - 1;

                textToPush--;
            }

            // Write text directly at position

            for (int i = 0; i < text.Length; i++)
            {
                _Buffer[_BufferCurrentPosition] = text[i];
                _BufferColours[_BufferCurrentPosition] = FindBrush(color);

                // Increment buffer position and reset to zero if needed.
                _BufferCurrentPosition++;
                if (_BufferCurrentPosition >= MAX_BUFFER_SIZE)
                {
                    _BufferCurrentPosition = 0;
                }
            }

            // No need to write two nulls ahead as this should have been
            // pushed along with the other text

            ScrollToCurrentPosition(false);

            if (refreshDisplay)
                Refresh();
        }

        /// <summary>
        /// Writes the given line to the console at the current position.
        /// </summary>
        /// <param name="text">the text to write</param>
        /// <param name="color">a custom color to print the terminal</param>
        /// <param name="refreshDisplay">
        /// if true, the control is immediately refresh after the operation is completed.
        /// </param>
        public void WriteLine(string text="", bool refreshDisplay=true)
        {
            Write($"{text}\n", refreshDisplay);
        }

        /// <summary>
        /// Writes the given line to the console at the current position.
        /// </summary>
        /// <param name="text">the text to write</param>
        /// <param name="color">the color to write the text</param>
        /// <param name="refreshDisplay">
        /// if true, the control is immediately refresh after the operation is completed.
        /// </param>
        public void WriteLine(string text, Color color, bool refreshDisplay=true)
        {
            Write($"{text}\n", color, refreshDisplay);
        }

        /// <summary>
        /// Creates a place holder at the current write position
        /// using the default placeholder color.
        /// </summary>
        /// <param name="text">the text to write as a placeholder</param>
        /// <param name="refreshDisplay">
        /// if true, the control is immediately refresh after the operation is completed.
        /// </param>
        public void SetPlaceholder(string text, bool refreshDisplay = true)
        {
            SetPlaceholder(text, DefaultPlaceholderColor, refreshDisplay);
        }
        /// <summary>
        /// Creates a place holder at the current write position
        /// using the given color.
        /// </summary>
        /// <param name="text">the text to write as a placeholder</param>
        /// <param name="color">the color to write the placeholder</param>
        /// <param name="refreshDisplay">
        /// if true, the control is immediately refresh after the operation is completed.
        /// </param>
        public void SetPlaceholder(string text, Color color, bool refreshDisplay = true)
        {
            _Placeholder = text;
            _PlaceholderIndex = _BufferCurrentPosition;
            _PlaceholderBrush = FindBrush(color);

            if (refreshDisplay)
                Refresh();
        }
        #endregion

        #region Buffer Scroll & Selection
        /// <summary>
        /// Scrolls the current display position to the next line.
        /// </summary>
        /// <param name="refreshDisplay">
        /// if true, the control is immediately refresh after the operation is completed.
        /// </param>
        public void ScrollToNextLine(bool refreshDisplay = true)
        {
            int readPos = _BufferDisplayLine;
            while (true)
            {
                char c = _Buffer[readPos];
                if (c == '\0')
                {
                    _BufferDisplayLine = readPos;
                    return;
                }
                else if (c == '\n')
                {
                    _BufferDisplayLine = readPos + 1;
                    if (_BufferDisplayLine >= MAX_BUFFER_SIZE)
                        _BufferDisplayLine = 0;
                    return;
                }
                readPos++;
                if (readPos >= MAX_BUFFER_SIZE)
                    readPos = 0;
            }

            if (refreshDisplay)
                Refresh();
        }


        /// <summary>
        /// Scrolls the current display position to the last line.
        /// </summary>
        /// <param name="refreshDisplay">
        /// if true, the control is immediately refresh after the operation is completed.
        /// </param>
        public void ScrollToLastLine(bool refreshDisplay=true)
        {
            int readPos = _BufferDisplayLine - 1;
            if (readPos < 0)
                readPos = MAX_BUFFER_SIZE - 1;
            bool foundFirstLine = false;
            while (true)
            {
                char c = _Buffer[readPos];
                if (c == '\0')
                {
                    _BufferDisplayLine = readPos + 1;
                    if (_BufferDisplayLine >= MAX_BUFFER_SIZE)
                        _BufferDisplayLine = 0;
                    break;
                }
                else if (c == '\n')
                {
                    if(foundFirstLine)
                    {
                        _BufferDisplayLine = readPos + 1;
                        if (_BufferDisplayLine >= MAX_BUFFER_SIZE)
                            _BufferDisplayLine = 0;
                        break;
                    }
                    else
                    {
                        foundFirstLine = true;
                    }
                }
                readPos--;
                if (readPos < 0)
                    readPos = MAX_BUFFER_SIZE - 1;
            }

            if (refreshDisplay)
                Refresh();
        }

        /// <summary>
        /// Scrolls to the current position or within ScrollViewKeepPercentage lines
        /// of the view.
        /// </summary>
        /// <param name="refreshDisplay">
        /// if true, the control is immediately refresh after the operation is completed.
        /// </param>
        public void ScrollToCurrentPosition(bool refreshDisplay=true)
        {
            int lines = NumberOfLinesToCurrentPosition;
            int targetLines = (int)(Rows * ScrollViewKeepPosition);
            
            while (lines > targetLines)
            {
                ScrollToNextLine(false);
                lines--;
            }

            if (refreshDisplay)
                Refresh();
        }

        /// <summary>
        /// Gets the number of lines till the current position
        /// </summary>
        public int NumberOfLinesToCurrentPosition
        {
            get
            {
                int readPos = _BufferDisplayLine;
                int lines = 0;
                while (readPos != _BufferCurrentPosition)
                {
                    char c = _Buffer[readPos];
                    if (c == '\0')
                        break;

                    if (c == '\n')
                        lines++;

                    readPos++;
                    if (readPos >= MAX_BUFFER_SIZE)
                        readPos = 0;
                }

                return lines;
            }
        }

        /// <summary>
        /// Moves the current position to the last word or the beginning of the current word.
        /// User input must be enabled
        /// </summary>
        /// <param name="refreshDisplay">
        /// if true, the control is immediately refresh after the operation is completed.
        /// </param>
        public void MoveLastWord(bool refreshDisplay=true)
        {
            if (!UserInputMode)
                return;

            int lastPos = _BufferCurrentPosition;

            int readPos = _BufferCurrentPosition - 1;
            if (readPos < 0)
                readPos = MAX_BUFFER_SIZE - 1;

            while(true)
            {
                if(readPos == _UserInputStartIndex)
                {
                    _BufferCurrentPosition = _UserInputStartIndex;
                    break;
                }    
                char c = _Buffer[readPos];
                if (!char.IsLetterOrDigit(c))
                {
                    _BufferCurrentPosition = readPos + 1;
                    break;
                }

                readPos--;
                if (readPos < 0)
                    readPos = MAX_BUFFER_SIZE - 1;
            }

            // If same position at least attempt to move one behind.
            if (lastPos == _BufferCurrentPosition)
                MoveLast(false);

            if (refreshDisplay)
                Refresh();
        }

        /// <summary>
        /// Moves the current position to the next word or the end of the current word.
        /// User Input must be enabled.
        /// </summary>
        /// <param name="refreshDisplay">
        /// if true, the control is immediately refresh after the operation is completed.
        /// </param>
        public void MoveNextWord(bool refreshDisplay=true)
        {
            if (!UserInputMode)
                return;

            int lastPos = _BufferCurrentPosition;

            int readPos = _BufferCurrentPosition + 1;
            if (readPos >= MAX_BUFFER_SIZE)
                readPos = 0;

            while (true)
            {
                char c = _Buffer[readPos];
                 
                if (!char.IsLetterOrDigit(c))
                {
                    _BufferCurrentPosition = readPos - 1;
                    break;
                }

                readPos++;
                if (readPos >= MAX_BUFFER_SIZE)
                    readPos = 0;
            }

            // If same position at least attempt to move one ahead.
            if (lastPos == _BufferCurrentPosition)
                MoveNext(false);


            if (refreshDisplay)
                Refresh();
        }

        /// <summary>
        /// Moves the current position by one behind if applicable.
        /// User Input must be enabled.
        /// </summary>
        /// <param name="refreshDisplay">
        /// if true, the control is immediately refresh after the operation is completed.
        /// </param>
        public void MoveLast(bool refreshDisplay = true)
        {
            if (!UserInputMode)
                return;
            
            if (_BufferCurrentPosition == _UserInputStartIndex)
                return; // At beginning of input

            int readPos = _BufferCurrentPosition - 1;
            if (readPos < 0)
                readPos = MAX_BUFFER_SIZE - 1;

            _BufferCurrentPosition = readPos;

            if (refreshDisplay)
                Refresh();
        }

        /// <summary>
        /// Moves the current position by one ahead if applicable.
        /// User Input must be enabled.
        /// </summary>
        /// <param name="refreshDisplay">
        /// if true, the control is immediately refresh after the operation is completed.
        /// </param>
        public void MoveNext(bool refreshDisplay=true)
        {
            if (!UserInputMode)
                return;

            int readPos = _BufferCurrentPosition + 1;
            if (readPos >= MAX_BUFFER_SIZE)
                readPos = 0;

            if (_Buffer[readPos] == '\0' && _Buffer[_BufferCurrentPosition] == '\0')
                return; // At end of input.

            _BufferCurrentPosition = readPos;


            if (refreshDisplay)
                Refresh();
        }


        protected override void OnMouseWheel(MouseEventArgs e)
        {
            int times = Math.Abs(e.Delta) / DeltaWheelSensitivity;
            if (e.Delta < 0) // Scroll down
            {
                int lines = NumberOfLinesToCurrentPosition;
                while (times > 0 && lines > 1)
                {
                    ScrollToNextLine(false);
                    times--;
                    lines--;
                }
            }
            else // Scroll up
            {
                while (times > 0)
                {
                    ScrollToLastLine(false);
                    times--;
                }
            }
            Refresh();
            base.OnMouseWheel(e);
        }
        #endregion

        #region Control
        /// <summary>
        /// Gets or sets how much the wheel affects scrolling
        /// </summary>
        [Description("Depicts how delta wheel affects the scrolling"), Category("Appearance")]
        public int DeltaWheelSensitivity { get; set; } = 100;

        [Description("A percentage of how many lines (in comparison to window) should be kept on-screen while scrolling to current position.")
            , Category("Appearance")]
        /// <summary>
        /// A percentage of how many lines (in comparison to window) should be kept on-screen while scrolling to current position
        /// </summary>
        public double ScrollViewKeepPosition { get; set; } = 0.75;

        /// <summary>
        /// If true, then the terminal will also scroll to the current position (or within ScrollViewKeepPercentage)
        /// </summary>
        [Description("If true, then the terminal will also scroll to the current position (or within ScrollViewKeepPercentage)"), Category("Control")]
        public bool AllowsScrollToCurrentPosition { get; set; } = true;

        /// <summary>
        /// Gets the content of the current selection as an ASCII string
        /// </summary>
        public string Selection
        {
            get
            {
                if (_BufferSelectPosition == -1)
                    return null;

                byte[] chars = new byte[_BufferSelectLength];

                int readPos = _BufferSelectPosition;

                for (int i = 0; i < _BufferSelectLength; i++)
                {
                    chars[i] = (byte)_Buffer[readPos++];
                    if (readPos >= MAX_BUFFER_SIZE)
                        readPos = 0;
                }

                return Encoding.ASCII.GetString(chars);
            }
        }

        /// <summary>
        /// Gets the buffer position from the screen position (relative to control)
        /// </summary>
        /// <param name="px">the x position relative to the control (top-left starts at zero)</param>
        /// <param name="py">the y position relative to the control (top-left starts at zero)</param>
        /// <returns>the buffer index (or -1 if currently over a null character)</returns>
        public int GetBufferPositionFromScreen(int px, int py) {
            int x = 2;
            int y = 2;

            int rows = Rows;
            int readPos = _BufferDisplayLine;

            while (rows > 0)
            {
                char c = _Buffer[readPos];

                if (c == '\0')
                    break;

                if (px >= x && py >= y && px <= x + CellWidth && py <= y + CellHeight)
                    return readPos;

                // Increment cell position
                x += CellWidth;
                if (c == '\n' || x >= Width - 2)
                {
                    x = 2;
                    y += CellHeight;
                    rows--;
                }

                readPos++;
                if (readPos >= MAX_BUFFER_SIZE)
                {
                    readPos = 0;
                }
            }

            return -1;
        }
        
        /// <summary>
        /// Redirects the standard console output (System.Console) to this
        /// control if useTerminal is true
        /// </summary>
        public void RedirectStandardConsoleOut()
        {
            Console.SetOut(new TerminalStreamWriter(this));
        }
        #endregion

        #region Buffer Rendering
        /// <summary>
        /// Gets or sets the placeholder being draw at the index.
        /// </summary>
        private string _Placeholder = null;
        /// <summary>
        /// Gets or sets the brush used to draw the placeholder
        /// </summary>
        private Brush _PlaceholderBrush;
        /// <summary>
        /// Gets or sets where the placeholder occurs (i.e. if the buffer has been overwritten
        /// </summary>
        private int _PlaceholderIndex = 0; 
        /// <summary>
        /// If ShowCursorPosition and _CursorActive is true, then the
        /// _ character is shown on top of the current position.
        /// </summary>
        private bool _CursorActive = false;

        /// <summary>
        /// Gets the amount of columns (based on the cell size) that
        /// this terminal can show.
        /// </summary>
        public int Columns
        {
            get
            {
                return (Width - 4) / CellWidth;
            }
        }

        /// <summary>
        /// Gets the amount of rows (based on the cell size) that this
        /// terminal can show.
        /// </summary>
        public int Rows
        {
            get
            {
                return (Height - 4) / CellHeight;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            int x = 2;
            int y = 2;

            int rows = Rows;
            int readPos = _BufferDisplayLine;

            int placeholderIndex = -1; // No current placeholder at the moment.
            int placeholderReadPos = 0;

            // Draw all highlighting
            bool exitAfterPlaceholder = false;

            while (rows > 0)
            {
                char c = _Buffer[readPos];

                if (_BufferSelectLength > 0 && readPos >= _BufferSelectPosition && readPos < _BufferSelectPosition + _BufferSelectLength)
                {
                    // Draw slight background for highlighting
                    e.Graphics.FillRectangle(FindBrush(HighlightColor), x, y, CellWidth, CellHeight);
                }

                // Increment cell position
                x += CellWidth;
                if (c == '\n' || x >= Width - 2)
                {
                    x = 2;
                    y += CellHeight;
                    rows--;
                }

                readPos++;
                if (readPos >= MAX_BUFFER_SIZE)
                {
                    readPos = 0;
                }
            }

            x = 2;
            y = 2;

            // Draw all text in current screen
            rows = Rows;
            readPos = _BufferDisplayLine;
            while (rows > 0)
            {
                char c = _Buffer[readPos];

                // Check if we now have a placeholder.
                if (readPos == _PlaceholderIndex && _Buffer[readPos] == '\0')
                {
                    if (_Placeholder != null)
                    {
                        placeholderIndex = _PlaceholderIndex;
                        placeholderReadPos = 0;
                    }
                }

                if (readPos == _BufferCurrentPosition && ShowCursorPosition && _CursorActive)
                {
                    // Display _ current position
                    e.Graphics.DrawString("_", TerminalFont, FindBrush(ForeColor), x, y);
                    // Increment in case it is at the start of a placeholder
                    placeholderReadPos++;
                }
                else
                {
                    // Get text brush from position for later use
                    Brush textBrush = _BufferColours[readPos];

                    if (placeholderIndex != -1 && _Placeholder != null
                        && readPos >= placeholderIndex
                        && readPos < placeholderIndex + _Placeholder.Length)
                    {
                        if (c == '\0')
                            exitAfterPlaceholder = true;
                        c = _Placeholder[placeholderReadPos++];
                        textBrush = _PlaceholderBrush;
                    }
                    else if (exitAfterPlaceholder) break;

                    if (c == '\0')
                        break;

                    if (!char.IsWhiteSpace(c)
                        && char.IsAscii(c))
                    {
                        e.Graphics.DrawString(c.ToString(), TerminalFont, textBrush, x, y);
                    }
                }

                // Increment cell position
                x += CellWidth;
                if (c == '\n' || x >= Width - 2)
                {
                    x = 2;
                    y += CellHeight;
                    rows--;
                }

                readPos++;
                if (readPos >= MAX_BUFFER_SIZE)
                {
                    readPos = 0;
                }
            }

            base.OnPaint(e);
        }
        #endregion

        private void minRefreshTimer_Tick(object sender, EventArgs e)
        {
            _CursorActive = !_CursorActive;
            Refresh();
        }

        #region Key Handling
        private void Terminal_KeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
            if(e.KeyCode == Keys.C && e.Control) {
                // Copy to clipboard
                try
                {
                    if(Selection != null)
                        Clipboard.SetText(Selection);
                }
                catch { }
                return;
            }

            if (UserInputMode)
            {
                char conversion = GetUserKeyboardChar(e);
                if (UserTypingVisible)
                {
                    if (e.KeyCode == Keys.Left)
                    {
                        if (e.Control) MoveLastWord();
                        else MoveLast();
                        return;
                    }
                    else if (e.KeyCode == Keys.Right)
                    {
                        if (e.Control) MoveNextWord();
                        else MoveNext();
                        return;
                    }
                    else if (e.KeyCode == Keys.V && e.Control)
                    {
                        try
                        {
                            // Get line from text (terminal does not support multi-line read lines)
                            string txt = Clipboard.GetText();
                            int lineIndex = txt.IndexOf(txt);
                            if (lineIndex != -1)
                                txt = txt.Substring(0, lineIndex);

                            _Insert(Clipboard.GetText(), InputColor);
                        }
                        catch { }
                        return;
                    }
                    else
                    if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
                    {
                        if (_BufferCurrentPosition != _UserInputStartIndex)
                        {
                            _Delete();
                        }
                        return;
                    }
                    else if (conversion != '\0')
                        _Insert(conversion.ToString(), InputColor);
                }
                else
                {
                    if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
                    {
                        _StoredInvisibleUserInput = _StoredInvisibleUserInput.Substring(0, _StoredInvisibleUserInput.Length - 1);
                        return;
                    }
                    else if (e.KeyCode == Keys.V && e.Control)
                    {
                        // Get line from text (terminal does not support multi-line read lines)
                        try
                        {
                            string txt = Clipboard.GetText();
                            int lineIndex = txt.IndexOf(txt);
                            if (lineIndex != -1)
                                txt = txt.Substring(0, lineIndex);
                            _StoredInvisibleUserInput += txt;
                        }
                        catch { }
                        return;
                    }
                    else if (conversion != '\0')
                    {
                        _StoredInvisibleUserInput += conversion;
                        return;
                    }
                }


                if (e.KeyCode == Keys.Enter && _CurrentLineCallback != null)
                {
                    // Accept line input
                    TerminalLineCallback callback = _CurrentLineCallback;
                    string input = CurrentUserInput;
                    EndInput();
                    _Write("\n", InputColor);
                    callback(input);
                }

                if (conversion == '\0')
                    return;

                if (_CurrentKeyEventCallback != null)
                {
                    TerminalKeyEventCallback callback = _CurrentKeyEventCallback;
                    EndInput();
                    callback(e);
                }
                else if (_CurrentKeyCallback != null)
                {
                    TerminalKeyCallback callback = _CurrentKeyCallback;
                    EndInput();
                    callback(e.KeyCode);
                }
                else if (_CurrentCharCallback != null)
                {
                    TerminalCharCallback callback = _CurrentCharCallback;
                    EndInput();
                    callback(conversion);
                }
            }
        }

        /// <summary>
        /// Converts key event args into a readable character.
        /// </summary>
        /// <param name="e">the key event args to transform</param>
        /// <returns>a char resulting from the key event args</returns>
        public static char GetUserKeyboardChar(KeyEventArgs e)
        {
            bool caps = Control.IsKeyLocked(Keys.CapsLock) ^ e.Shift;
            bool numlock = Control.IsKeyLocked(Keys.NumLock);

            // Check number cases
            switch(e.KeyCode)
            {
                case Keys.Space:
                    return ' ';
                case Keys.D1:
                    return (e.Shift ? '!' : '1');
                case Keys.D2:
                    return (e.Shift ? '@' : '2');
                case Keys.D3:
                    return (e.Shift ? '#' : '3');
                case Keys.D4:
                    return (e.Shift ? '$' : '4');
                case Keys.D5:
                    return (e.Shift ? '%' : '5');
                case Keys.D6:
                    return (e.Shift ? '^' : '6');
                case Keys.D7:
                    return (e.Shift ? '&' : '7');
                case Keys.D8:
                    return (e.Shift ? '*' : '8');
                case Keys.D9:
                    return (e.Shift ? '(' : '9');
                case Keys.D0:
                    return (e.Shift ? ')' : '0');
                case Keys.OemMinus:
                    return (e.Shift ? '_' : '-');
                case Keys.Oemplus:
                    return (e.Shift ? '+' : '=');
                case Keys.Oemtilde:
                    return (e.Shift ? '~' : '`');
                case Keys.Q:
                    return (caps ? 'Q' : 'q');
                case Keys.W:
                    return (caps ? 'W' : 'w');
                case Keys.E:
                    return (caps ? 'E' : 'e');
                case Keys.R:
                    return (caps ? 'R' : 'r');
                case Keys.T:
                    return (caps ? 'T' : 't');
                case Keys.Y:
                    return (caps ? 'Y' : 'y');
                case Keys.U:
                    return (caps ? 'U' : 'u');
                case Keys.I:
                    return (caps ? 'I' : 'i');
                case Keys.O:
                    return (caps ? 'O' : 'o');
                case Keys.P:
                    return (caps ? 'P' : 'p');
                case Keys.OemOpenBrackets:
                    return (e.Shift ? '{' : '[');
                case Keys.OemCloseBrackets:
                    return (e.Shift ? '}' : ']');
                case Keys.OemBackslash or Keys.OemPipe:
                    return (e.Shift ? '|' : '\\');
                case Keys.A:
                    return (caps ? 'A' : 'a');
                case Keys.S:
                    return (caps ? 'S' : 's');
                case Keys.D:
                    return (caps ? 'D' : 'd');
                case Keys.F:
                    return (caps ? 'F' : 'f');
                case Keys.G:
                    return (caps ? 'G' : 'g');
                case Keys.H:
                    return (caps ? 'H' : 'h');
                case Keys.J:
                    return (caps ? 'J' : 'j');
                case Keys.K:
                    return (caps ? 'K' : 'k');
                case Keys.L:
                    return (caps ? 'L' : 'l');
                case Keys.OemSemicolon:
                    return (e.Shift ? ':' : ';');
                case Keys.OemQuotes:
                    return (e.Shift ? '"' : '\'');
                case Keys.Z:
                    return (caps ? 'Z' : 'z');
                case Keys.X:
                    return (caps ? 'X' : 'x');
                case Keys.C:
                    return (caps ? 'C' : 'c');
                case Keys.V:
                    return (caps ? 'V' : 'v');
                case Keys.B:
                    return (caps ? 'B' : 'b');
                case Keys.N:
                    return (caps ? 'N' : 'n');
                case Keys.M:
                    return (caps ? 'M' : 'm');
                case Keys.Oemcomma:
                    return (e.Shift ? '<' : ',');
                case Keys.OemPeriod:
                    return (e.Shift ? '>' : '.');
                case Keys.OemQuestion:
                    return (e.Shift ? '?' : '/');
                case Keys.NumPad0:
                    return (numlock ? '0' : '\0');
                case Keys.NumPad1:
                    return (numlock ? '1' : '\0');
                case Keys.NumPad2:
                    return (numlock ? '2' : '\0');
                case Keys.NumPad3:
                    return (numlock ? '3' : '\0');
                case Keys.NumPad4:
                    return (numlock ? '4' : '\0');
                case Keys.NumPad5:
                    return (numlock ? '5' : '\0');
                case Keys.NumPad6:
                    return (numlock ? '6' : '\0');
                case Keys.NumPad7:
                    return (numlock ? '7' : '\0');
                case Keys.NumPad8:
                    return (numlock ? '8' : '\0');
                case Keys.NumPad9:
                    return (numlock ? '9' : '\0');
                case Keys.Multiply:
                    return '*';
                case Keys.Divide:
                    return '/';
                case Keys.Subtract:
                    return '-';
                case Keys.Add:
                    return '+';
                case Keys.Decimal:
                    return '.';
                default:
                    break;
            }
            return '\0';
        }
        #endregion

        #region Selection Handling

        private bool _CapturingSelection = false;
        private int _InitialSelection = 0;
        private void Terminal_MouseDown(object sender, MouseEventArgs e)
        {
            _CapturingSelection = e.Button == MouseButtons.Left;
            _InitialSelection = _BufferSelectPosition = GetBufferPositionFromScreen(e.X, e.Y);
            _BufferSelectLength = 1;
            Refresh();
        }

        private void Terminal_MouseUp(object sender, MouseEventArgs e)
        {
            _CapturingSelection = false;
        }

        private void Terminal_MouseMove(object sender, MouseEventArgs e)
        {
            if (_CapturingSelection)
            {
                int lastPos = _BufferSelectPosition;
                int lastLength = _BufferSelectLength;
                int pos = GetBufferPositionFromScreen(e.X, e.Y);
                if(pos == -1)
                {
                    // Assume that the hover is over a null character (which should occur at end of output).
                    _BufferSelectPosition = _InitialSelection;
                    _BufferSelectLength = _BufferCurrentPosition - _BufferSelectPosition + 1;
                }
                else if (pos < _InitialSelection)
                {
                    _BufferSelectPosition = pos;
                    _BufferSelectLength = _InitialSelection - _BufferSelectPosition + 1;
                }
                else
                {
                    _BufferSelectPosition = _InitialSelection;
                    _BufferSelectLength = pos - _InitialSelection + 1;
                }

                if (lastLength != _BufferSelectLength || lastPos != _BufferSelectPosition)
                    Refresh();
            }
        }
        #endregion

        protected override void OnResize(EventArgs e)
        {
            Refresh();
            base.OnResize(e);
        }

        private void Terminal_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
                e.IsInputKey = true;
        }
    }
}