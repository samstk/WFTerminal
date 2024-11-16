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
using System.Security.Principal;
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
    public delegate void TerminalLineCallback(string? line);

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
        /// If true, then output (via Write function) can be appended during UserInputMode.
        /// Sometimes necessary due to asynchronous/multithread operations where the messages for the input
        /// may be detected after the prompt for input itself.
        /// </summary>
        /// <remarks>
        /// All outputs during UserInput will be appended at the before the input start
        /// </remarks>
        [Description("If true, then output (via Write function) can be appended during UserInputMode.\r\n" +
            "Sometimes necessary due to asynchronous/multithread operations where the messages for the input\r\n" +
            "may be detected after the prompt for input itself.\r\nAll outputs during UserInput will be appended at the before the input start"),
            Category("Behavior")]
        public bool AllowOutputOnUserInputMode { get; set; } = false;

        /// <summary>
        /// If true, then the user may use the array keys to move their selection
        /// </summary>
        [Description("If true, then the user may use the array keys to move their selection"),
            Category("Behaviour")]
        public bool AllowInputPositionMovement { get; set; } = true;
        /// <summary>
        /// If true, then the software has requested for user input.
        /// </summary>
        private bool _UserInputMode = false;

        [Description("If true, then the terminal is always is UserInputMode. Ensure AllowOutputOnUserInputMode is enabled."),
            Category("Behavior")]
        /// <summary>
        /// If true, then the terminal is always is UserInputMode.
        /// </summary>
        /// <remarks>
        /// Ensure AllowOutputOnUserInputMode is enabled.
        /// </remarks>
        public bool FreeInputMode { get; set; } = false;

        /// <summary>
        /// If false, then the user must pressed enter to end the input, and the input
        /// is managed entirely by the WFTerminal control (and sent to the input stream on enter).<br/>
        /// 
        /// If true, then all keys pressed are sent to the input stream, and it is managed entirely by
        /// the receiving end of the input stream.
        /// </summary>
        /// <remarks>
        /// Set to false for Shell setups to allow the shell to handle the input.
        /// </remarks>
        public bool DirectInputStreamWriting { get; set; } = false;

        [Description("If true and FreeInputMode is true, then the terminal attempts to get the current writing position at first input.\r\n Due to the nature of free input mode, this may be necessary to avoid allowing deletion of previous prompts."),
            Category("Behavior")]
        /// <summary>
        /// If true, then the terminal attempts to get the current writing position at first input.
        /// Due to the nature of free input mode, this may be necessary to avoid allowing deletion of previous prompts.
        /// </summary>
        /// <remarks>
        /// FreeInputMode must be enabled.
        /// </remarks>
        public bool RestrictFreeInputMode { get; set; } = true;

        /// <summary>
        /// Gets whether the terminal is currently in user input mode.
        /// If so, then nothing can be written to the terminal.
        /// </summary>
        /// <remarks>
        /// If FreeInputMode is set to true, then this will be fixed
        /// to true.
        /// </remarks>
        public bool UserInputMode
        {
            get
            {
                return FreeInputMode || _UserInputMode;
            }
            set
            {
                _UserInputMode = value;
            }
        }

        /// <summary>
        /// Gets whether the current input is visible.
        /// </summary>
        public bool UserTypingVisible { get; private set; } = false;

        /// <summary>
        /// Gets the stored invisible input when UserTypingVisible is false,
        /// and the user has typed.
        /// </summary>
        private string? _StoredInvisibleUserInput = null;

        /// <summary>
        /// Gets the index where the user input started.
        /// </summary>
        private int _UserInputStartIndex = 0;

        /// <summary>
        /// Gets the current user input (or null if not in input mode or it is directly written to the input stream)
        /// </summary>
        public string? CurrentUserInput
        {
            get
            {
                if (!UserTypingVisible)
                {
                    return _StoredInvisibleUserInput;
                }
                else
                {
                    if (_UserInputStartIndex < 0)
                    {
                        return null;
                    }

                    string line = "";
                    int readPos = _UserInputStartIndex;
                    while (true)
                    {
                        char c = _Buffer[readPos++];

                        if (c == '\0')
                            break;

                        line += c;

                        if (readPos >= MaxBufferSize)
                            readPos = 0;
                    }

                    return line;
                }
            }
        }

        // Callbacks for read functions
        private TerminalKeyEventCallback? _CurrentKeyEventCallback;

        [Description("Occurs when a line has been processed in FreeInputMode.")]
        /// <summary>
        /// Occurs when a line has been processed in FreeInputMode
        /// </summary>
        public event TerminalLineCallback? OnProcessFreeInputLine;

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
        public void ReadLine(TerminalLineCallback callback, bool visibleTyping = true)
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
            _UserInputStartIndex = -1;
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
        public uint CellWidth { get; set; } = 9;

        /// <summary>
        /// Sets the height of a single cell in the terminal
        /// </summary>
        [Description("Sets the height of a single cell in the terminal"), Category("Appearance")]
        public uint CellHeight { get; set; } = 13;

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
        public Color HighlightColor { get; set; } = Color.FromArgb(44, 44, 44);
        #endregion

        /// <summary>
        /// Gets the dictionary of colors to brush mappings that persists
        /// for the lifetime of the app.
        /// </summary>
        public static Dictionary<Color, SolidBrush> TextBrushes { get; private set; } = new Dictionary<Color, SolidBrush>();

        #region Buffer & Modification (Write and Read functions)
        /// <summary>
        /// The max buffer size (in byte length).
        /// </summary>
        /// <remarks>
        /// Changing this after terminal initialization will cause errors. Change it in (e.g.) Program.cs
        /// </remarks>
        public static int MaxBufferSize = 48000;

        /// <summary>
        /// Get or set the characters stored in the buffer.
        /// </summary>
        private char[] _Buffer = new char[MaxBufferSize];

        /// <summary>
        /// Get or set the colours set on the buffer
        /// </summary>
        private SolidBrush?[] _BufferColours = new SolidBrush[MaxBufferSize];

        /// <summary>
        /// Gets or sets the back colours set on the buffer.
        /// </summary>
        private SolidBrush?[] _BufferBackColours = new SolidBrush[MaxBufferSize];

        /// <summary>
        /// Get or set the location (char index) of the starting line to display.
        /// </summary>
        private int _BufferDisplayLine = 0;

        /// <summary>
        /// Get or set the current position in the buffer.
        /// </summary>
        /// <remarks>
        /// Set <see cref="_BufferDisplayLine"/> alongside to prevent display errors.
        /// </remarks>
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
        private SolidBrush? FindBrush(Color color)
        {
            if (color == Color.Transparent)
                return null;

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
        public void Clear(bool refreshDisplay = true)
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
            _Buffer[MaxBufferSize - 1] = '\0';
            if (refreshDisplay)
                Refresh();
        }

        /// <summary>
        /// Determines if the index was after the beginning index given the parameters.
        /// </summary>
        /// <param name="index">The index to determine that was after the beginIndex.</param>
        /// <param name="beginIndex">The starting index inside the buffer.</param>
        /// <param name="bufferEnd">The end index of the buffer</param>
        /// <returns></returns>
        private bool IsOnOrAfterIndex(int index, int beginIndex, int bufferEnd)
        {
            if (bufferEnd < beginIndex) // Circularly wrapped.
            {
                if (index < beginIndex && index < bufferEnd)
                    return true;

                return index >= beginIndex;
            }
            else // Default.
            {
                return index >= beginIndex && index <= bufferEnd;
            }
        }

        /// <summary>
        /// Erases characters in the display from <paramref name="bufferStart"/>
        /// to <paramref name="bufferEnd"/> inclusive.
        /// </summary>
        /// <param name="bufferStart">The position to start erasing from.</param>
        /// <param name="bufferEnd">The position to stop erasing after.</param>
        private void EraseInDisplay(int bufferStart, int bufferEnd)
        {
            // Must shift all characters after actual bufferEnd to bufferStart.
            int endOfBuffer = SeekEndOfBuffer(bufferStart);
            int shiftPosition = endOfBuffer;
            
            int writePosition = bufferStart;
            bufferEnd++;
            if (bufferEnd >= MaxBufferSize)
                bufferEnd = 0;
            int clearLength = bufferEnd - bufferStart;
            if (clearLength < 0) clearLength = MaxBufferSize + clearLength;
            int length = clearLength;

            while (length-- > 0)
            {
                _Buffer[writePosition++] = _Buffer[shiftPosition++]; 

                if (shiftPosition >= MaxBufferSize)
                {
                    shiftPosition = 0;
                }

                if (writePosition >= MaxBufferSize)
                {
                    writePosition = 0;
                }
            }

            // Write the null character to the end of the buffer (indicating end)
            bufferEnd -= clearLength;
            if (bufferEnd < 0)
                bufferEnd += MaxBufferSize;

            _Buffer[bufferEnd] = '\0';

            // Clear any selection
            _BufferSelectPosition = -1;
            _BufferSelectLength = 0;

            // Adjust the start display line if it was ahead of the start.
            if (IsOnOrAfterIndex(_BufferDisplayLine, bufferStart, endOfBuffer))
            {
                // Must move display line to last line.
                int lastLine = SeekLastLineStartInBuffer();
                if (lastLine != -1)
                    _BufferDisplayLine = lastLine;
            }

            // Adjust the current cursor position by the length of what was cleared if 
            // it was ahead of the start area.
            if (IsOnOrAfterIndex(_BufferCurrentPosition, bufferStart, endOfBuffer))
            {
                _BufferCurrentPosition = bufferStart;
            }
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
        public void Put(string text, bool refreshDisplay = true)
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
        public void Put(string text, Color color, bool refreshDisplay = true)
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
        /// Writes the given text to the console at the current position.
        /// </summary>
        /// <param name="text">the text to write to the console</param>
        /// <param name="refreshDisplay">
        /// if true, the control is immediately refresh after the operation is completed.
        /// </param>
        public void Write(string text, bool refreshDisplay = true)
        {
            Write(text, ForeColor, refreshDisplay);
        }



        #region ANSI Color Converters
        private Color _ANSI8ColorForeground(int code) => code switch
        {
            30 => Color.Black,
            31 => Color.Red,
            32 => Color.Green,
            33 => Color.Yellow,
            34 => Color.Blue,
            35 => Color.Magenta,
            36 => Color.Cyan,
            37 => Color.WhiteSmoke,
            0 => ForeColor,
            _ => Color.Transparent
        };

        private Color _ANSI16ColorForeground(int code) => code switch
        {
            30 => Color.FromArgb(12, 12, 12),
            31 => Color.FromArgb(255, 34, 34),
            32 => Color.LightGreen,
            33 => Color.LightYellow,
            34 => Color.LightBlue,
            35 => Color.FromArgb(255, 34, 255),
            36 => Color.LightCyan,
            37 => Color.White,
            0 => ForeColor,
            _ => Color.Transparent
        };

        private Color _ANSI8ColorBackground(int code) => code switch
        {
            40 => Color.Black,
            41 => Color.Red,
            42 => Color.Green,
            43 => Color.Yellow,
            44 => Color.Blue,
            45 => Color.Magenta,
            46 => Color.Cyan,
            47 => Color.WhiteSmoke,
            _ => Color.Transparent // No change
        };

        private Color _ANSI16ColorBackground(int code) => code switch
        {
            40 => Color.FromArgb(12, 12, 12),
            41 => Color.FromArgb(255, 34, 34),
            42 => Color.LightGreen,
            43 => Color.LightYellow,
            44 => Color.LightBlue,
            45 => Color.FromArgb(255, 34, 255),
            46 => Color.LightCyan,
            47 => Color.White,
            _ => Color.Transparent // No change
        };

        private static Color[] _ANSI256Colors = new Color[256]
        {
            Color.Transparent,
            Color.FromArgb(128,0,0),
        Color.FromArgb(0,128,0),
        Color.FromArgb(128,128,0),
        Color.FromArgb(0,0,128),
        Color.FromArgb(128,0,128),
        Color.FromArgb(0,128,128),
        Color.FromArgb(192,192,192),
        Color.FromArgb(128,128,128),
        Color.FromArgb(255,0,0),
        Color.FromArgb(0,255,0),
        Color.FromArgb(255,255,0),
        Color.FromArgb(0,0,255),
        Color.FromArgb(255,0,255),
        Color.FromArgb(0,255,255),
        Color.FromArgb(255,255,255),
        Color.FromArgb(0,0,0),
        Color.FromArgb(0,0,95),
        Color.FromArgb(0,0,135),
        Color.FromArgb(0,0,175),
        Color.FromArgb(0,0,215),
        Color.FromArgb(0,0,255),
        Color.FromArgb(0,95,0),
        Color.FromArgb(0,95,95),
        Color.FromArgb(0,95,135),
        Color.FromArgb(0,95,175),
        Color.FromArgb(0,95,215),
        Color.FromArgb(0,95,255),
        Color.FromArgb(0,135,0),
        Color.FromArgb(0,135,95),
        Color.FromArgb(0,135,135),
        Color.FromArgb(0,135,175),
        Color.FromArgb(0,135,215),
        Color.FromArgb(0,135,255),
        Color.FromArgb(0,175,0),
        Color.FromArgb(0,175,95),
        Color.FromArgb(0,175,135),
        Color.FromArgb(0,175,175),
        Color.FromArgb(0,175,215),
        Color.FromArgb(0,175,255),
        Color.FromArgb(0,215,0),
        Color.FromArgb(0,215,95),
        Color.FromArgb(0,215,135),
        Color.FromArgb(0,215,175),
        Color.FromArgb(0,215,215),
        Color.FromArgb(0,215,255),
        Color.FromArgb(0,255,0),
        Color.FromArgb(0,255,95),
        Color.FromArgb(0,255,135),
        Color.FromArgb(0,255,175),
        Color.FromArgb(0,255,215),
        Color.FromArgb(0,255,255),
        Color.FromArgb(95,0,0),
        Color.FromArgb(95,0,95),
        Color.FromArgb(95,0,135),
        Color.FromArgb(95,0,175),
        Color.FromArgb(95,0,215),
        Color.FromArgb(95,0,255),
        Color.FromArgb(95,95,0),
        Color.FromArgb(95,95,95),
        Color.FromArgb(95,95,135),
        Color.FromArgb(95,95,175),
        Color.FromArgb(95,95,215),
        Color.FromArgb(95,95,255),
        Color.FromArgb(95,135,0),
        Color.FromArgb(95,135,95),
        Color.FromArgb(95,135,135),
        Color.FromArgb(95,135,175),
        Color.FromArgb(95,135,215),
        Color.FromArgb(95,135,255),
        Color.FromArgb(95,175,0),
        Color.FromArgb(95,175,95),
        Color.FromArgb(95,175,135),
        Color.FromArgb(95,175,175),
        Color.FromArgb(95,175,215),
        Color.FromArgb(95,175,255),
        Color.FromArgb(95,215,0),
        Color.FromArgb(95,215,95),
        Color.FromArgb(95,215,135),
        Color.FromArgb(95,215,175),
        Color.FromArgb(95,215,215),
        Color.FromArgb(95,215,255),
        Color.FromArgb(95,255,0),
        Color.FromArgb(95,255,95),
        Color.FromArgb(95,255,135),
        Color.FromArgb(95,255,175),
        Color.FromArgb(95,255,215),
        Color.FromArgb(95,255,255),
        Color.FromArgb(135,0,0),
        Color.FromArgb(135,0,95),
        Color.FromArgb(135,0,135),
        Color.FromArgb(135,0,175),
        Color.FromArgb(135,0,215),
        Color.FromArgb(135,0,255),
        Color.FromArgb(135,95,0),
        Color.FromArgb(135,95,95),
        Color.FromArgb(135,95,135),
        Color.FromArgb(135,95,175),
        Color.FromArgb(135,95,215),
        Color.FromArgb(135,95,255),
        Color.FromArgb(135,135,0),
        Color.FromArgb(135,135,95),
        Color.FromArgb(135,135,135),
        Color.FromArgb(135,135,175),
        Color.FromArgb(135,135,215),
        Color.FromArgb(135,135,255),
        Color.FromArgb(135,175,0),
        Color.FromArgb(135,175,95),
        Color.FromArgb(135,175,135),
        Color.FromArgb(135,175,175),
        Color.FromArgb(135,175,215),
        Color.FromArgb(135,175,255),
        Color.FromArgb(135,215,0),
        Color.FromArgb(135,215,95),
        Color.FromArgb(135,215,135),
        Color.FromArgb(135,215,175),
        Color.FromArgb(135,215,215),
        Color.FromArgb(135,215,255),
        Color.FromArgb(135,255,0),
        Color.FromArgb(135,255,95),
        Color.FromArgb(135,255,135),
        Color.FromArgb(135,255,175),
        Color.FromArgb(135,255,215),
        Color.FromArgb(135,255,255),
        Color.FromArgb(175,0,0),
        Color.FromArgb(175,0,95),
        Color.FromArgb(175,0,135),
        Color.FromArgb(175,0,175),
        Color.FromArgb(175,0,215),
        Color.FromArgb(175,0,255),
        Color.FromArgb(175,95,0),
        Color.FromArgb(175,95,95),
        Color.FromArgb(175,95,135),
        Color.FromArgb(175,95,175),
        Color.FromArgb(175,95,215),
        Color.FromArgb(175,95,255),
        Color.FromArgb(175,135,0),
        Color.FromArgb(175,135,95),
        Color.FromArgb(175,135,135),
        Color.FromArgb(175,135,175),
        Color.FromArgb(175,135,215),
        Color.FromArgb(175,135,255),
        Color.FromArgb(175,175,0),
        Color.FromArgb(175,175,95),
        Color.FromArgb(175,175,135),
        Color.FromArgb(175,175,175),
        Color.FromArgb(175,175,215),
        Color.FromArgb(175,175,255),
        Color.FromArgb(175,215,0),
        Color.FromArgb(175,215,95),
        Color.FromArgb(175,215,135),
        Color.FromArgb(175,215,175),
        Color.FromArgb(175,215,215),
        Color.FromArgb(175,215,255),
        Color.FromArgb(175,255,0),
        Color.FromArgb(175,255,95),
        Color.FromArgb(175,255,135),
        Color.FromArgb(175,255,175),
        Color.FromArgb(175,255,215),
        Color.FromArgb(175,255,255),
        Color.FromArgb(215,0,0),
        Color.FromArgb(215,0,95),
        Color.FromArgb(215,0,135),
        Color.FromArgb(215,0,175),
        Color.FromArgb(215,0,215),
        Color.FromArgb(215,0,255),
        Color.FromArgb(215,95,0),
        Color.FromArgb(215,95,95),
        Color.FromArgb(215,95,135),
        Color.FromArgb(215,95,175),
        Color.FromArgb(215,95,215),
        Color.FromArgb(215,95,255),
        Color.FromArgb(215,135,0),
        Color.FromArgb(215,135,95),
        Color.FromArgb(215,135,135),
        Color.FromArgb(215,135,175),
        Color.FromArgb(215,135,215),
        Color.FromArgb(215,135,255),
        Color.FromArgb(215,175,0),
        Color.FromArgb(215,175,95),
        Color.FromArgb(215,175,135),
        Color.FromArgb(215,175,175),
        Color.FromArgb(215,175,215),
        Color.FromArgb(215,175,255),
        Color.FromArgb(215,215,0),
        Color.FromArgb(215,215,95),
        Color.FromArgb(215,215,135),
        Color.FromArgb(215,215,175),
        Color.FromArgb(215,215,215),
        Color.FromArgb(215,215,255),
        Color.FromArgb(215,255,0),
        Color.FromArgb(215,255,95),
        Color.FromArgb(215,255,135),
        Color.FromArgb(215,255,175),
        Color.FromArgb(215,255,215),
        Color.FromArgb(215,255,255),
        Color.FromArgb(255,0,0),
        Color.FromArgb(255,0,95),
        Color.FromArgb(255,0,135),
        Color.FromArgb(255,0,175),
        Color.FromArgb(255,0,215),
        Color.FromArgb(255,0,255),
        Color.FromArgb(255,95,0),
        Color.FromArgb(255,95,95),
        Color.FromArgb(255,95,135),
        Color.FromArgb(255,95,175),
        Color.FromArgb(255,95,215),
        Color.FromArgb(255,95,255),
        Color.FromArgb(255,135,0),
        Color.FromArgb(255,135,95),
        Color.FromArgb(255,135,135),
        Color.FromArgb(255,135,175),
        Color.FromArgb(255,135,215),
        Color.FromArgb(255,135,255),
        Color.FromArgb(255,175,0),
        Color.FromArgb(255,175,95),
        Color.FromArgb(255,175,135),
        Color.FromArgb(255,175,175),
        Color.FromArgb(255,175,215),
        Color.FromArgb(255,175,255),
        Color.FromArgb(255,215,0),
        Color.FromArgb(255,215,95),
        Color.FromArgb(255,215,135),
        Color.FromArgb(255,215,175),
        Color.FromArgb(255,215,215),
        Color.FromArgb(255,215,255),
        Color.FromArgb(255,255,0),
        Color.FromArgb(255,255,95),
        Color.FromArgb(255,255,135),
        Color.FromArgb(255,255,175),
        Color.FromArgb(255,255,215),
        Color.FromArgb(255,255,255),
        Color.FromArgb(8,8,8),
        Color.FromArgb(18,18,18),
        Color.FromArgb(28,28,28),
        Color.FromArgb(38,38,38),
        Color.FromArgb(48,48,48),
        Color.FromArgb(58,58,58),
        Color.FromArgb(68,68,68),
        Color.FromArgb(78,78,78),
        Color.FromArgb(88,88,88),
        Color.FromArgb(98,98,98),
        Color.FromArgb(108,108,108),
        Color.FromArgb(118,118,118),
        Color.FromArgb(128,128,128),
        Color.FromArgb(138,138,138),
        Color.FromArgb(148,148,148),
        Color.FromArgb(158,158,158),
        Color.FromArgb(168,168,168),
        Color.FromArgb(178,178,178),
        Color.FromArgb(188,188,188),
        Color.FromArgb(198,198,198),
        Color.FromArgb(208,208,208),
        Color.FromArgb(218,218,218),
        Color.FromArgb(228,228,228),
        Color.FromArgb(238,238,238),
        };

        private Color _ANSIRGBColorForeground(List<int> code)
        {
            if (code.Count == 5)
            {
                if (code[0] != 38)
                    return Color.Transparent;
                if (code[1] != 2)
                    return Color.Transparent;
                return Color.FromArgb(code[2], code[3], code[4]);
            }
            return Color.Transparent;
        }

        private Color _ANSIRGBColorBackground(List<int> code)
        {
            if (code.Count == 5)
            {
                if (code[0] != 48)
                    return Color.Transparent;
                if (code[1] != 2)
                    return Color.Transparent;
                return Color.FromArgb(code[2], code[3], code[4]);
            }
            return Color.Transparent;
        }
        #endregion

        /// <summary>
        /// Writes the given text to the console at the current position.
        /// If ANSI colours are detected, then draws them in the
        /// given colours. If special characters are detected, then
        /// acts accordingly.
        /// </summary>
        /// <param name="text">the text to write to the console</param>
        /// <param name="refreshDisplay">
        /// if true, the control is immediately refresh after the operation is completed.
        /// </param>
        /// <returns>number of characters written to terminal</returns>
        public int WriteAnsi(string text, bool refreshDisplay = true)
        {
            Color color = ForeColor;
            Color backColor = Color.Transparent;
            List<int> escapeSections = new List<int>();
            int writeCount = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\u001b' || c == '\x1b')
                {
                    escapeSections.Clear();
                    string code = "";
                    char codeType = '\0';
                    char codeStarter = '\0';
                    try
                    {
                        i++;
                        if (i < text.Length && text[i] != '[')
                            continue; // invalid or unsupported ansi code.
                        codeStarter = text[i];
                        i++;

                        // Find all code sections
                        while (i < text.Length)
                        {
                            char c2 = text[i++];
                            if (c2 == 'm' ||
                                (c2 >= 'A' && c2 <= 'H') || 
                                (c2 >= 'J' && c2 <= 'K') ||
                                c2 == 'S' || c2 == 'T' || c2 == 'f')
                            {
                                codeType = c2;
                                escapeSections.Add(code == string.Empty ? -1 : int.Parse(code));
                                code = "";
                                i--;
                                // At the end of ansi code
                                break;
                            }
                            else if (c2 == ';')
                            {
                                escapeSections.Add(code == string.Empty ? -1 : int.Parse(code));
                                code = "";
                            }
                            else code += c2;
                        }
                        string txt = "ANSI Code: \\x"+codeStarter;
                        foreach (int codex in escapeSections)
                            txt += " " + codex.ToString();
                        txt += codeType;

                        System.Diagnostics.Debug.WriteLine(txt);
                        if (codeStarter == '[')
                        {
                            if (codeType == 'm')
                            { // color
                                if (escapeSections.Count == 1)
                                {
                                    Color foreground = _ANSI8ColorForeground(escapeSections[0]);
                                    Color background = _ANSI8ColorBackground(escapeSections[0]);

                                    if (foreground != Color.Transparent)
                                        color = foreground;
                                    if (background != Color.Transparent)
                                        backColor = background;
                                }
                                else if (escapeSections.Count == 2 && escapeSections[1] == 1)
                                {
                                    Color foreground = _ANSI16ColorForeground(escapeSections[0]);
                                    Color background = _ANSI16ColorBackground(escapeSections[0]);

                                    if (foreground != Color.Transparent)
                                        color = foreground;
                                    if (background != Color.Transparent)
                                        backColor = background;
                                }
                                else if (escapeSections.Count == 3 && escapeSections[1] == 5)
                                {
                                    if (escapeSections[0] == 38)
                                    {
                                        Color foreground = _ANSI256Colors[escapeSections[2]];
                                        if (foreground != Color.Transparent)
                                            color = foreground;
                                    }
                                    else if (escapeSections[0] == 48)
                                    {
                                        Color background = _ANSI256Colors[escapeSections[2]];
                                        if (background != Color.Transparent)
                                            backColor = background;

                                    }


                                }
                                else
                                {
                                    Color foreground = _ANSIRGBColorForeground(escapeSections);
                                    Color background = _ANSIRGBColorBackground(escapeSections);

                                    if (foreground != Color.Transparent)
                                        color = foreground;
                                    if (background != Color.Transparent)
                                        backColor = background;
                                }
                            }
                            else if (codeType == 'A') // Cursor Up 
                            {

                            }
                            else if (codeType == 'B') // Cursor Down
                            {

                            }
                            else if (codeType == 'C') // Cursor Forward
                            {

                            }
                            else if (codeType == 'D') // Cursor Back
                            {

                            }
                            else if (codeType == 'E') // Cursor Next Line
                            {

                            }
                            else if (codeType == 'F') // Cursor Previous Line
                            {

                            }
                            else if (codeType == 'G') // Cursor Horizontal Absolute
                            {

                            }
                            else if (codeType == 'H') // Set Cursor Position
                            {
                                int row = -1;
                                int column = -1;

                                if (escapeSections.Count == 1)
                                {
                                    row = escapeSections[0];
                                }
                                else if (escapeSections.Count == 2)
                                {
                                    column = escapeSections[1];
                                }

                                // Restore defaults
                                if (row == -1) row = 1;
                                if (column == -1) column = 1;

                                int actualIndex = (SeekStartOfBuffer() + ((row-1) * (int)Columns) + (column-1)) % MaxBufferSize;

                                _BufferCurrentPosition = actualIndex;
                            }
                            else if (codeType == 'J') // Erase in Display
                            {
                                int mode = -1;
                                if (escapeSections.Count == 1)
                                {
                                    mode = 0;
                                }

                                if (mode == -1 || mode == 0) // Clear from Cursor to End of Screen
                                {
                                    EraseInDisplay(_BufferCurrentPosition, SeekEndOfBuffer());
                                }
                                else if (mode == 1) // Clear from cursor to beginning of the screen
                                {
                                    int startOfBuffer = SeekStartOfBuffer();
                                    EraseInDisplay(startOfBuffer, _BufferCurrentPosition);
                                }
                                else if (mode == 2) // Clear entire screen.
                                {
                                    Clear(false);
                                }
                                else if (mode == 3) // Clear entire screen and delete all lines saved in the scrollback buffer.
                                {
                                    // Not supported
                                }
                            }
                            else if (codeType == 'K') // Erase in Line
                            {

                            }
                            else if (codeType == 'S') // Scroll Up
                            {

                            }
                            else if (codeType == 'T') // Scroll Down
                            {

                            }
                            else if (codeType == 'f') // Horizontal Vertical Position
                            {

                            }
                        }
                    }
                    catch { /* error in ansi code */ }
                }
                else if (c == '\x08') // Backspace, remove last character.
                {
                    _Delete(false);
                    writeCount--;
                }
                else if (c == '\r') // Move to column zero if double
                {
                    int actualIndex = SeekCurrentLineStartInBuffer();
                    _BufferCurrentPosition = actualIndex;
                }
                else if (c == '\n') // Handles it differently from normal, just move cursor down (and append \n if needed)
                {
                    int nextLineIndex = SeekNextLineStartInBuffer(_BufferCurrentPosition, true);
                    int newlineIndex = nextLineIndex - 1;
                    if (newlineIndex < 0)
                        newlineIndex = MaxBufferSize - 1;

                    if (_Buffer[newlineIndex] != '\n')
                    {
                        _Buffer[nextLineIndex] = '\n';
                        nextLineIndex++;
                        if (nextLineIndex >= MaxBufferSize)
                            nextLineIndex = 0;
                    }
                    _BufferCurrentPosition = nextLineIndex;
                }
                else
                {
                    Write(c.ToString(), color, backColor, false);
                    writeCount++;
                }
            }

            if (refreshDisplay)
                Refresh();

            return writeCount;
        }

        /// <summary>
        /// Writes the given text to the console at the current position.
        /// If linux ANSI colours are detected (up to 16 - or rgb), then draws them in the
        /// given colours.
        /// </summary>
        /// <param name="line">the text to write to the console</param>
        /// <param name="refreshDisplay">
        /// if true, the control is immediately refresh after the operation is completed.
        /// </param>
        /// <returns>number of characters written to terminal</returns>
        public int WriteLinuxLine(string line, bool refreshDisplay = true)
        {
            return WriteAnsi($"{line}\n", refreshDisplay);
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
            if (UserInputMode && !DirectInputStreamWriting)
            {
                if (AllowOutputOnUserInputMode)
                {
                    if (_UserInputStartIndex == -1)
                    {
                        // Simply write at end of location
                        _Write(text, color, Color.Transparent, false);
                    }
                    else
                    {
                        _Insert(text, color, _UserInputStartIndex, false);
                        _UserInputStartIndex = (_UserInputStartIndex + text.Length) % MaxBufferSize;
                        if (_PlaceholderIndex != -1)
                            _PlaceholderIndex = (_PlaceholderIndex + text.Length) % MaxBufferSize;
                    }
                    if (refreshDisplay)
                        Refresh();
                }
                return; // No typing can be done when input is expected.
            }
            _Write(text, color, Color.Transparent, refreshDisplay);
        }

        /// <summary>
        /// Writes the given line to the console at the current position.
        /// </summary>
        /// <param name="text">the text to write to the console</param>
        /// <param name="color">the color to write the text</param>
        /// <param name="backColor">the color to draw the background</param>
        /// <param name="refreshDisplay">
        /// if true, the control is immediately refresh after the operation is completed.
        /// </param>
        public void Write(string text, Color color, Color backColor, bool refreshDisplay = true)
        {
            if (UserInputMode && !DirectInputStreamWriting)
            {
                if (AllowOutputOnUserInputMode)
                {
                    if (_UserInputStartIndex > 0)
                    {
                        _Insert(text, color, _UserInputStartIndex, false);
                        _UserInputStartIndex = (_UserInputStartIndex + text.Length) % MaxBufferSize;
                        if (_PlaceholderIndex != -1)
                            _PlaceholderIndex = (_PlaceholderIndex + text.Length) % MaxBufferSize;
                    }
                    else if (FreeInputMode)
                    {
                        _Write(text, color, backColor, false);
                    }
                    if (refreshDisplay)
                        Refresh();
                }
                return; // No typing can be done when input is expected.
            }
            _Write(text, color, backColor, refreshDisplay);
        }

        /// <summary>
        /// Deletes the last character and shifts all characters backwards
        /// </summary>
        /// <param name="refreshDisplay">
        /// if true, the control is immediately refresh after the operation is completed.
        /// </param>
        private void _Delete(bool refreshDisplay = true)
        {
            int readPos = _BufferCurrentPosition - 1;
            if (readPos < 0)
                readPos = MaxBufferSize - 1;

            int lastPos = readPos;
            while (true)
            {
                char c = _Buffer[readPos];

                if (lastPos != readPos)
                {
                    _Buffer[lastPos] = c;
                }

                lastPos = readPos;
                readPos++;
                if (readPos >= MaxBufferSize)
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

            if (refreshDisplay)
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
        private void _Write(string text, Color color, Color backColor, bool refreshDisplay = true)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\a')
                    continue; // Do not write special character \a

                _Buffer[_BufferCurrentPosition] = text[i];
                _BufferColours[_BufferCurrentPosition] = FindBrush(color);
                _BufferBackColours[_BufferCurrentPosition] = FindBrush(backColor);
                // Increment buffer position and reset to zero if needed.
                _BufferCurrentPosition++;
                if (_BufferCurrentPosition >= MaxBufferSize)
                {
                    _BufferCurrentPosition = 0;
                }
            }

            // Write two positions ahead to avoid reading past this position
            _Buffer[_BufferCurrentPosition] = '\0';

            if (_BufferCurrentPosition + 1 >= MaxBufferSize)
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
        /// <param name="position">the position to insert in the buffer</param>
        /// <param name="refreshDisplay">
        /// if true, the control is immediately refresh after the operation is completed.
        /// </param>
        private void _Insert(string text, Color color, int position, bool refreshDisplay = true)
        {
            if (text == "\0") return;

            int textToPush = 2;

            if (_BufferCurrentPosition >= position)
            {
                _BufferCurrentPosition = (_BufferCurrentPosition + text.Length) % MaxBufferSize;
            }
            // Calculate how much text to push
            int readPos = position;
            while (true)
            {
                char c = _Buffer[readPos];

                if (c == '\0')
                {
                    break;
                }

                textToPush++;
                readPos++;
                if (readPos > MaxBufferSize)
                    readPos = 0;
            }

            // Push all text
            readPos = (position + textToPush - 1) % MaxBufferSize;
            int writePos = (position + text.Length + textToPush - 1) % MaxBufferSize;

            while (textToPush > 0)
            {
                _Buffer[writePos] = _Buffer[readPos];
                _BufferColours[writePos] = _BufferColours[readPos];
                _BufferBackColours[writePos--] = _BufferBackColours[readPos--];

                if (writePos < 0)
                    writePos = MaxBufferSize - 1;

                if (readPos < 0)
                    readPos = MaxBufferSize - 1;

                textToPush--;
            }

            // Write text directly at position

            for (int i = 0; i < text.Length; i++)
            {
                _Buffer[position] = text[i];
                _BufferColours[position] = FindBrush(color);
                _BufferBackColours[position] = null; // default brush
                // Increment buffer position and reset to zero if needed.
                position++;
                if (position >= MaxBufferSize)
                {
                    position = 0;
                }
            }

            // No need to write two nulls ahead as this should have been
            // pushed along with the other text

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
            _Insert(text, color, _BufferCurrentPosition, refreshDisplay);
        }

        /// <summary>
        /// Writes the given line to the console at the current position.
        /// </summary>
        /// <param name="text">the text to write</param>
        /// <param name="color">a custom color to print the terminal</param>
        /// <param name="refreshDisplay">
        /// if true, the control is immediately refresh after the operation is completed.
        /// </param>
        public void WriteLine(string text = "", bool refreshDisplay = true)
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
        public void WriteLine(string text, Color color, bool refreshDisplay = true)
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
        /// Seeks the end index of the last character of the buffer
        /// </summary>
        /// <param name="startingFrom">The index to start searching from, or if -1 then the current cursor position.</param>
        /// <returns>The end index of the last character of the buffer</returns>
        public int SeekEndOfBuffer(int startingFrom = -1, int resultOffset=0)
        {
            if (startingFrom == -1) startingFrom = _BufferCurrentPosition;

            int readPos = startingFrom;
            while (true)
            {
                char c = _Buffer[readPos];
                if (c == '\0') // End of buffer
                {
                    readPos -= resultOffset;
                    if (readPos <= 0)
                    {
                        readPos += MaxBufferSize;
                    }

                    return readPos;
                }

                readPos++;
                if (readPos >= MaxBufferSize)
                    readPos = 0;
            }
        }

        /// <summary>
        /// Seeks the start index of the first character of the buffer
        /// </summary>
        /// <param name="startingFrom">The index to start searching from, or if -1 then the current cursor position.</param>
        /// <returns>The start index of the first character of the buffer</returns>
        public int SeekStartOfBuffer(int startingFrom = -1)
        {
            if (startingFrom == -1) startingFrom = _BufferCurrentPosition;
            if (startingFrom < 0) startingFrom = MaxBufferSize - 1;

            int readPos = startingFrom;
            bool started = false;
            while (true)
            {
                char c = _Buffer[readPos];
                if (c == '\0') // Start of buffer
                {
                    if (!started)
                    {
                        if (readPos == startingFrom)
                        {
                            readPos--;
                            if (readPos < 0)
                                readPos = MaxBufferSize - 1;
                            continue;
                        }
                        else // We don't have anything in the buffer
                        {
                            return _BufferCurrentPosition;
                        }
                    }

                    readPos += 1;
                    if (readPos >= MaxBufferSize)
                        readPos = 0;

                    return readPos;
                }
                else started = true;

                readPos--;
                if (readPos < 0)
                    readPos = MaxBufferSize - 1;
            }
        }

        /// <summary>
        /// Seeks the start index of the last line of the buffer
        /// </summary>
        /// <param name="startingFrom">The index to start searching from, or if -1 then the current display line start position.</param>
        /// <returns>The end index of the first character of the last line in the buffer or -1 if it does not exist.</returns>
        public int SeekLastLineStartInBuffer(int startingFrom = -1)
        {
            if (startingFrom == -1) startingFrom = _BufferDisplayLine;

            int readPos = startingFrom - 1;
            if (readPos < 0)
                readPos = MaxBufferSize - 1;

            bool foundFirstLine = false;
            while (true)
            {
                char c = _Buffer[readPos];
                if (c == '\0')
                {
                    return -1;
                }
                else if (c == '\n')
                {
                    if (foundFirstLine)
                    {
                        readPos = readPos + 1;
                        if (readPos >= MaxBufferSize)
                            readPos = 0;
                        return readPos;
                    }
                    else
                    {
                        foundFirstLine = true;
                    }
                }
                readPos--;
                if (readPos < 0)
                    readPos = MaxBufferSize - 1;
            }
        }

        /// <summary>
        /// Seeks the start index of the current line of the buffer
        /// </summary>
        /// <param name="startingFrom">The index to start searching from, or if -1 then the current buffer position.</param>
        /// <returns>The start index of the current line.</returns>
        /// <remarks>
        /// Unlike <see cref="SeekLastLineStartInBuffer(int)"/> and <see cref="SeekNextLineStartInBuffer(int)"/>, this
        /// method defaults to searching from the current buffer position.
        /// </remarks>
        public int SeekCurrentLineStartInBuffer(int startingFrom = -1)
        {
            if (startingFrom == -1) startingFrom = _BufferCurrentPosition;

            int readPos = startingFrom - 1;
            if (readPos < 0)
                readPos = MaxBufferSize - 1;

            while (true)
            {
                char c = _Buffer[readPos];
                if (c == '\0')
                {
                    return readPos;
                }
                else if (c == '\n')
                {
                    readPos = readPos + 1;
                    if (readPos >= MaxBufferSize)
                        readPos = 0;
                    return readPos;
                }

                readPos--;
                if (readPos < 0)
                    readPos = MaxBufferSize - 1;
            }
        }

        /// <summary>
        /// Seeks the start index of the next line of the buffer
        /// </summary>
        /// <param name="startingFrom">The index to start searching from, or if -1 then the current display line start position.</param>
        /// <returns>The end index of the first character of the next line in the buffer or -1 if it does not exist.</returns>
        public int SeekNextLineStartInBuffer(int startingFrom = -1, bool returnEndIfNotFound=false)
        {
            if (startingFrom == -1) startingFrom = _BufferDisplayLine;

            int readPos = startingFrom;
            while (true)
            {
                char c = _Buffer[readPos];
                if (c == '\0')
                {
                    if (returnEndIfNotFound)
                        return readPos;

                    return -1;
                }
                else if (c == '\n')
                {
                    readPos += 1;
                    if (readPos >= MaxBufferSize)
                        readPos = 0;
                    return readPos;
                }

                readPos++;
                if (readPos >= MaxBufferSize)
                    readPos = 0;
            }
        }

        /// <summary>
        /// Scrolls the current display position to the next line.
        /// </summary>
        /// <param name="refreshDisplay">
        /// if true, the control is immediately refresh after the operation is completed.
        /// </param>
        public void ScrollToNextLine(bool refreshDisplay = true)
        {
            int nextLine = SeekNextLineStartInBuffer();

            if (nextLine != -1)
            {
                _BufferDisplayLine = nextLine;
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
        public void ScrollToLastLine(bool refreshDisplay = true)
        {
            int lastLine = SeekLastLineStartInBuffer();

            if (lastLine != -1)
            {
                _BufferDisplayLine = lastLine;
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
        public void ScrollToCurrentPosition(bool refreshDisplay = true)
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
                    if (readPos >= MaxBufferSize)
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
        public void MoveLastWord(bool refreshDisplay = true)
        {
            if (!UserInputMode)
                return;

            int lastPos = _BufferCurrentPosition;

            int readPos = _BufferCurrentPosition - 1;
            if (readPos < 0)
                readPos = MaxBufferSize - 1;

            while (true)
            {
                if (readPos == _UserInputStartIndex)
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
                    readPos = MaxBufferSize - 1;
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
        public void MoveNextWord(bool refreshDisplay = true)
        {
            if (!UserInputMode)
                return;

            int lastPos = _BufferCurrentPosition;

            int readPos = _BufferCurrentPosition + 1;
            if (readPos >= MaxBufferSize)
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
                if (readPos >= MaxBufferSize)
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
                readPos = MaxBufferSize - 1;

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
        public void MoveNext(bool refreshDisplay = true)
        {
            if (!UserInputMode)
                return;

            int readPos = _BufferCurrentPosition + 1;
            if (readPos >= MaxBufferSize)
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
        public string? Selection
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
                    if (readPos >= MaxBufferSize)
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
        public int GetBufferPositionFromScreen(int px, int py)
        {
            int x = 2;
            int y = 2;

            uint rows = Rows;
            int readPos = _BufferDisplayLine;

            while (rows > 0)
            {
                char c = _Buffer[readPos];

                if (c == '\0')
                    break;

                if (px >= x && py >= y && px <= x + CellWidth && py <= y + CellHeight)
                    return readPos;

                // Increment cell position
                x += (int)CellWidth;
                if (c == '\n' || x >= Width - 2 - CellWidth)
                {
                    x = 2;
                    y += (int)CellHeight;
                    rows--;
                }

                readPos++;
                if (readPos >= MaxBufferSize)
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

        private Stream? _InputStream;

        /// <summary>
        /// Gets or creates an input stream that results
        /// from this terminal. If called, then AllowOutputOnUserInputMode
        /// is set to true, and FreeInputMode is also set to true.
        /// </summary>
        /// <remarks>
        /// Only one line is sent at a time (meaning that stream must be handled before another line is sent, else it
        /// gets overriden)
        /// </remarks>
        /// <returns>the stream that is written to</returns>
        public Stream GetInputStream()
        {
            AllowOutputOnUserInputMode = true;
            FreeInputMode = true;
            if (_InputStream == null)
            {
                _InputStream = new MemoryStream();
            }
            streamTimer.Enabled = true;
            return _InputStream;
        }

        private Stream? _OutputStream;

        /// <summary>
        /// Gets or creates an output stream that may
        /// unconditionally write to this terminal at any time.
        /// </summary>
        /// <remarks>
        /// The contents of the stream is treated as if it was linux (with ansi color supported)
        /// </remarks>
        /// <returns>the stream that is read into the terminal</returns>
        public Stream GetOutputStream()
        {
            if (_OutputStream == null)
            {
                _OutputStream = new MemoryStream();
            }
            streamTimer.Enabled = true;
            return _OutputStream;
        }

        /// <summary>
        /// Sets both streams to a shell stream from an external package.
        /// </summary>
        /// <param name="stream">The shell stream which can be written to and read from.</param>
        /// <returns>The created shell</returns>
        /// <remarks>
        /// <see cref="DirectInputStreamWriting"/> mode is turned on, so that all input is handled
        /// with the system stream.<br/>
        /// If stream is null, then disables relevant modes.
        /// </remarks>
        public void SetSystemStream(Stream? stream)
        {
            if (stream == null)
            {
                DirectInputStreamWriting = false;
                UserInputMode = false;
                _InputStream = _OutputStream = null;
            }
            else
            {
                DirectInputStreamWriting = true;
                UserInputMode = true;
                _InputStream = _OutputStream = stream;
            }
        }
        #endregion

        #region Buffer Rendering
        /// <summary>
        /// Gets or sets the placeholder being draw at the index.
        /// </summary>
        private string? _Placeholder = null;
        /// <summary>
        /// Gets or sets the brush used to draw the placeholder
        /// </summary>
        private Brush? _PlaceholderBrush;
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
        public uint Columns
        {
            get
            {
                return ((uint)Width - 4) / CellWidth;
            }
        }

        /// <summary>
        /// Gets the amount of rows (based on the cell size) that this
        /// terminal can show.
        /// </summary>
        public uint Rows
        {
            get
            {
                return ((uint)Height - 4) / CellHeight;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            int x = 2;
            int y = 2;

            int rows = (int)Rows;
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
                    SolidBrush? drawingBrush = FindBrush(HighlightColor);
                    if (drawingBrush != null)
                    {
                        // Draw slight background for highlighting
                        e.Graphics.FillRectangle(drawingBrush, x, y, CellWidth, CellHeight);
                    }
                }
                else
                {
                    // Draw given background
                    SolidBrush? backgroundBrush = _BufferBackColours[readPos];
                    if (backgroundBrush != null)
                    {
                        e.Graphics.FillRectangle(backgroundBrush, x, y, CellWidth, CellHeight);
                    }
                }

                // Increment cell position
                x += (int)CellWidth;
                if (c == '\n' || x >= Width - 2 - CellWidth)
                {
                    x = 2;
                    y += (int)CellHeight;
                    rows--;
                }

                readPos++;
                if (readPos >= MaxBufferSize)
                {
                    readPos = 0;
                }
            }

            x = 2;
            y = 2;

            // Draw all text in current screen
            rows = (int)Rows;
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
                    SolidBrush? drawingBrush = FindBrush(ForeColor);

                    if (drawingBrush != null)
                    {
                        // Display _ current position
                        e.Graphics.DrawString("_", TerminalFont, drawingBrush, x, y);
                    }
                    
                    // Increment in case it is at the start of a placeholder
                    placeholderReadPos++;
                }
                else
                {
                    // Get text brush from position for later use
                    Brush? textBrush = _BufferColours[readPos];

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
                        && char.IsAscii(c) && textBrush != null)
                    {
                        e.Graphics.DrawString(c.ToString(), TerminalFont, textBrush, x, y);
                    }
                }

                // Increment cell position
                x += (int)CellWidth;
                if (c == '\n' || x >= Width - 2 - CellWidth)
                {
                    x = 2;
                    y += (int)CellHeight;
                    rows--;
                }

                readPos++;
                if (readPos >= MaxBufferSize)
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
            if (e.KeyCode == Keys.C && e.Control && (!DirectInputStreamWriting || e.Shift))
            {
                // Copy to clipboard
                try
                {
                    if (Selection != null)
                        Clipboard.SetText(Selection);
                }
                catch { }
                return;
            }

            if (UserInputMode)
            {
                if (FreeInputMode && RestrictFreeInputMode && _UserInputStartIndex < 0)
                {
                    _UserInputStartIndex = _BufferCurrentPosition;
                }
                try // Fallback in case unexpected crash.
                {
                    string conversion = GetUserKeyboardText(e, DirectInputStreamWriting);
                    if (DirectInputStreamWriting)
                    {
                        if (_InputStream != null && conversion != "\0")
                        {
                            foreach(char c in conversion)
                            _InputStream.WriteByte((byte)c);
                            _InputStream.Flush();
                        }
                    }
                    else
                    {
                        if (UserTypingVisible)
                        {

                            if (AllowInputPositionMovement && e.KeyCode == Keys.Left)
                            {
                                if (e.Control) MoveLastWord();
                                else MoveLast();
                                return;
                            }
                            else if (AllowInputPositionMovement && e.KeyCode == Keys.Right)
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
                            else if (conversion != "\0")
                                _Insert(conversion, InputColor);
                        }
                        else
                        {
                            if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
                            {
                                if (_StoredInvisibleUserInput != null)
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
                            else if (conversion != "\0")
                            {
                                _StoredInvisibleUserInput += conversion;
                                return;
                            }
                        }


                        if (e.KeyCode == Keys.Enter)
                        {
                            // Accept line input
                            TerminalLineCallback? callback = _CurrentLineCallback;
                            string? input = CurrentUserInput;
                            EndInput();
                            _Write("\n", InputColor, Color.Transparent);
                            if (callback != null)
                                callback(input);

                            OnProcessFreeInputLine?.Invoke(input);

                            if (_InputStream != null)
                            {
                                byte[] bytes = Encoding.ASCII.GetBytes(input + "\n");
                                _InputStream.Write(bytes, 0, bytes.Length);
                                _InputStream.Flush();
                                _InputStream.Position = 0;
                                _InputStream.SetLength(bytes.Length);
                            }
                        }

                        if (conversion == "\0")
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
                            callback(conversion[0]);
                        }
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Converts key event args into a readable character.
        /// </summary>
        /// <param name="e">the key event args to transform</param>
        /// <param name="allowSystemChars">If true, then allows system chars to be returned (e.g. Ctrl+C = \cc) including returns</param>
        /// <returns>a char resulting from the key event args</returns>
        private string GetUserKeyboardText(KeyEventArgs e, bool allowSystemChars=false)
        {
            bool caps = Control.IsKeyLocked(Keys.CapsLock) ^ e.Shift;
            bool numlock = Control.IsKeyLocked(Keys.NumLock);

            if (e.Control && (!DirectInputStreamWriting || e.Shift) && e.KeyCode == Keys.V)
            {
                // Paste
                try {
                    return Clipboard.GetText(TextDataFormat.Text);
                } catch { } 
                return "\0";
            }

            // Check number cases
            if (!e.Control)
            {
                switch (e.KeyCode)
                {
                    case Keys.Space:
                        return " ";
                    case Keys.D1:
                        return (e.Shift ? "!" : "1");
                    case Keys.D2:
                        return (e.Shift ? "@" : "2");
                    case Keys.D3:
                        return (e.Shift ? "#" : "3");
                    case Keys.D4:
                        return (e.Shift ? "$" : "4");
                    case Keys.D5:
                        return (e.Shift ? "%" : "5");
                    case Keys.D6:
                        return (e.Shift ? "^" : "6");
                    case Keys.D7:
                        return (e.Shift ? "&" : "7");
                    case Keys.D8:
                        return (e.Shift ? "*" : "8");
                    case Keys.D9:
                        return (e.Shift ? "(" : "9");
                    case Keys.D0:
                        return (e.Shift ? ")" : "0");
                    case Keys.OemMinus:
                        return (e.Shift ? "_" : "-");
                    case Keys.Oemplus:
                        return (e.Shift ? "+" : "=");
                    case Keys.Oemtilde:
                        return (e.Shift ? "~" : "`");
                    case Keys.Q:
                        return (caps ? "Q" : "q");
                    case Keys.W:
                        return (caps ? "W" : "w");
                    case Keys.E:
                        return (caps ? "E" : "e");
                    case Keys.R:
                        return (caps ? "R" : "r");
                    case Keys.T:
                        return (caps ? "T" : "t");
                    case Keys.Y:
                        return (caps ? "Y" : "y");
                    case Keys.U:
                        return (caps ? "U" : "u");
                    case Keys.I:
                        return (caps ? "I" : "i");
                    case Keys.O:
                        return (caps ? "O" : "o");
                    case Keys.P:
                        return (caps ? "P" : "p");
                    case Keys.OemOpenBrackets:
                        return (e.Shift ? "{" : "[");
                    case Keys.OemCloseBrackets:
                        return (e.Shift ? "}" : "]");
                    case Keys.OemBackslash or Keys.OemPipe:
                        return (e.Shift ? "|" : "\\");
                    case Keys.A:
                        return (caps ? "A" : "a");
                    case Keys.S:
                        return (caps ? "S" : "s");
                    case Keys.D:
                        return (caps ? "D" : "d");
                    case Keys.F:
                        return (caps ? "F" : "f");
                    case Keys.G:
                        return (caps ? "G" : "g");
                    case Keys.H:
                        return (caps ? "H" : "h");
                    case Keys.J:
                        return (caps ? "J" : "j");
                    case Keys.K:
                        return (caps ? "K" : "k");
                    case Keys.L:
                        return (caps ? "L" : "l");
                    case Keys.OemSemicolon:
                        return (e.Shift ? ":" : ";");
                    case Keys.OemQuotes:
                        return (e.Shift ? "\"" : "'");
                    case Keys.Z:
                        return (caps ? "Z" : "z");
                    case Keys.X:
                        return (caps ? "X" : "x");
                    case Keys.C:
                        return (caps ? "C" : "c");
                    case Keys.V:
                        return (caps ? "V" : "v");
                    case Keys.B:
                        return (caps ? "B" : "b");
                    case Keys.N:
                        return (caps ? "N" : "n");
                    case Keys.M:
                        return (caps ? "M" : "m");
                    case Keys.Oemcomma:
                        return (e.Shift ? "<" : ",");
                    case Keys.OemPeriod:
                        return (e.Shift ? ">" : ".");
                    case Keys.OemQuestion:
                        return (e.Shift ? "?" : "/");
                    case Keys.NumPad0:
                        return (numlock ? "0" : "\0");
                    case Keys.NumPad1:
                        return (numlock ? "1" : "\0");
                    case Keys.NumPad2:
                        return (numlock ? "2" : "\0");
                    case Keys.NumPad3:
                        return (numlock ? "3" : "\0");
                    case Keys.NumPad4:
                        return (numlock ? "4" : "\0");
                    case Keys.NumPad5:
                        return (numlock ? "5" : "\0");
                    case Keys.NumPad6:
                        return (numlock ? "6" : "\0");
                    case Keys.NumPad7:
                        return (numlock ? "7" : "\0");
                    case Keys.NumPad8:
                        return (numlock ? "8" : "\0");
                    case Keys.NumPad9:
                        return (numlock ? "9" : "\0");
                    case Keys.Multiply:
                        return "*";
                    case Keys.Divide:
                        return "/";
                    case Keys.Subtract:
                        return "-";
                    case Keys.Add:
                        return "+";
                    case Keys.Decimal:
                        return ".";
                    default:
                        break;
                }
            }

            if (allowSystemChars)
            {
                if (!e.Control && !e.Shift && !e.Alt) // Single keys
                {
                    switch(e.KeyCode)
                    {
                        case Keys.Enter:
                            return "\n";
                        case Keys.Delete:
                            return "\x7F";
                        case Keys.Back:
                            return "\x08";
                        case Keys.Up:
                            return "\x0b[A";
                        case Keys.Down:
                            return "\x0b[B";
                        case Keys.Right:
                            return "\x0b[C";
                        case Keys.Left:
                            return "\x0b[D";
                    }
                }
                else if (e.Control && !e.Shift && !e.Alt) // Control
                {
                    switch(e.KeyCode)
                    {
                        case Keys.C:
                            return "\x03";
                        case Keys.D:
                            return "\x04";
                    }
                }
            }

            return "\0";
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
                if (pos == -1)
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

        private void streamTimer_Tick(object sender, EventArgs e)
        {
            if (_OutputStream != null)
            {
                string text = new StreamReader(_OutputStream).ReadToEnd();

                int length = this.WriteAnsi(text);

                if (_UserInputStartIndex != -1)
                    _UserInputStartIndex += length;
            }
        }
    }
}