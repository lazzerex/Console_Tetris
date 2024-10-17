using System;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Collections.Generic;
using System.Media;


namespace Tetris
{
    class Program1
    {

        static bool CheckForSkipInput()
        {
            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                return (key.Key == ConsoleKey.Spacebar || key.Key == ConsoleKey.Enter);
            }
            return false;
        }

        #region NhạcGame

        static SoundPlayer titlePlayer = new SoundPlayer();
        static SoundPlayer gamePlayer = new SoundPlayer();
        static SoundPlayer effectPlayer = new SoundPlayer();

        static void PlaySound(SoundPlayer player, string soundFile)
        {
            player.SoundLocation = soundFile;
            player.Play();
        }

        static void StopSound(SoundPlayer player)
        {
            player.Stop();
        }

        #endregion


        #region Chế Độ Game

        //---------------------Che Do Game va Enum--------------------//


        static GameMode gameMode = GameMode.Classic;
        static PuzzleDifficulty puzzleDifficulty;
        static int movesRemaining;
        static int targetLines;


        enum GameMode
        {
            Classic,
            Puzzle
        }

        enum PuzzleDifficulty
        {
            Easy,
            Medium,
            Hard
        }

        enum ClWBDifficulty
        {
            Easy,
            Medium,
            Hard
        }


        //-------------------------------Che Do Game va Enum-------------------------------------------//

        #endregion


        #region Assets


        /* Possible modification*/
        readonly static ConsoleColor[] colours =
        {
            ConsoleColor.Red,
            ConsoleColor.Blue,
            ConsoleColor.Green,
            ConsoleColor.Magenta,
            ConsoleColor.Yellow,
            ConsoleColor.White,
            ConsoleColor.Cyan
        };

        static ConsoleColor[] blockColours = new ConsoleColor[7];


        readonly static string characters = "#######";
        readonly static int[,,,] positions =
        {
        {
        {{0,0},{1,0},{0,1},{1,1}},
        {{0,0},{1,0},{0,1},{1,1}},
        {{0,0},{1,0},{0,1},{1,1}},
        {{0,0},{1,0},{0,1},{1,1}}
        },

        {
        {{2,0},{2,1},{2,2},{2,3}},
        {{0,2},{1,2},{2,2},{3,2}},
        {{1,0},{1,1},{1,2},{1,3}},
        {{0,1},{1,1},{2,1},{3,1}},
        },
        {
        {{1,0},{1,1},{1,2},{2,2}},
        {{1,2},{1,1},{2,1},{3,1}},
        {{1,1},{2,1},{2,2},{2,3}},
        {{2,1},{2,2},{1,2},{0,2}}
        },

        {
        {{2,0},{2,1},{2,2},{1,2}},
        {{1,1},{1,2},{2,2},{3,2}},
        {{2,1},{1,1},{1,2},{1,3}},
        {{0,1},{1,1},{2,1},{2,2}}
        },

        {
        {{2,1},{1,1},{1,2},{0,2}},
        {{1,0},{1,1},{2,1},{2,2}},
        {{2,1},{1,1},{1,2},{0,2}},
        {{1,0},{1,1},{2,1},{2,2}}
        },
        {
        {{0,1},{1,1},{1,2},{2,2}},
        {{1,0},{1,1},{0,1},{0,2}},
        {{0,1},{1,1},{1,2},{2,2}},
        {{1,0},{1,1},{0,1},{0,2}}
        },

        {
        {{0,1},{1,1},{1,0},{2,1}},
        {{1,0},{1,1},{2,1},{1,2}},
        {{0,1},{1,1},{1,2},{2,1}},
        {{1,0},{1,1},{0,1},{1,2}}
        }
        };
        #endregion


        #region BiếnTròChơi
        //---------------------------------------hằng số và các biến của trò chơi----------------------------------------//


        // Map / BG 
        const int mapSizeX = 10;
        const int mapSizeY = 20;
        static char[,] bg = new char[mapSizeY, mapSizeX];

        static int highScore = 0; // Biến lưu điểm cao nhất
        static string highScoreFile = "highscore.txt"; // Tên tệp để lưu điểm cao nhất
        static int score = 0;
        static int removedLines = 0; // Biến để đếm số dòng đã xóa
        static int levelremovelines;
        static int level = 1;

        // Hold variables
        const int holdSizeX = 6;
        const int holdSizeY = mapSizeY;
        static int holdIndex = -1;
        static char holdChar;

        const int upNextSize = 6;


        static ConsoleKeyInfo input;


        // Current info
        static int currentX = 0;
        static int currentY = 0;
        static char currentChar = 'O';

        static int currentRot = 0;



        // Block and Bogs        
        static int[] bag;
        static int[] nextBag;

        static int bagIndex;
        static int currentIndex;


        // misc
        static Stopwatch stopwatch = new Stopwatch(); // Đo thời gian chơi thêm vào
        static int maxTime = 20;
        static int timer = 0;
        static int amount = 0;

        static int movesUsed = 0;


        //--------------------------------hằng số và các biến của trò chơi-------------------------------------//

        #endregion


        #region Vòng Lặp Chính Của Game

        //---------------------------------Main Game Loop----------------------------------------------//
        static void Main()
        {
            Console.CursorVisible = false;
            TitleScreen();
            TransitionScreen(); // Add this line after TitleScreen
            LoadHighScore();
            Console.WindowHeight = 22;

            blockColours = new ConsoleColor[7];
            Console.Title = "doannhom8";
            DrawBorderingame();
            // Start the inputthread to get live inputs
            Thread inputThread = new Thread(Input);
            inputThread.Start();

            // Generate bag / current block
            bag = GenerateBag();
            nextBag = GenerateBag();
            NewBlock();

            // Generate an empty bg
            for (int y = 0; y < mapSizeY; y++)
                for (int x = 0; x < mapSizeX; x++)
                    bg[y, x] = '-';

            PlaySound(gamePlayer, @"e:\TetrisMusic\Main.wav");
            stopwatch.Start();
            while (true)
            {
                // Force block down
                if (timer >= maxTime)
                {
                    // If it doesn't collide, just move it down. If it does call BlockDownCollision
                    if (!Collision(currentIndex, bg, currentX, currentY + 1, currentRot))
                    {
                        currentY++;
                    }
                    else
                    {
                        BlockDownCollision(); // This will increment movesUsed in puzzle mode
                    }

                    timer = 0;
                }
                timer++;

                // INPUT
                InputHandler(); // Call InputHandler
                input = new ConsoleKeyInfo(); // Reset input var


                // RENDER CURRENT
                char[,] view = RenderView(); // Render view (Playing field)

                // RENDER HOLD
                char[,] hold = RenderHold(); // Render hold (the current held block)


                //RENDER UP NEXT
                char[,] next = RenderUpNext(); // Render the next three blocks as an 'up next' feature

                // PRINT VIEW
                Print(view, hold, next); // Print everything to the screen

                Thread.Sleep(15); // Wait to not overload the processor (I think it's better because it has no impact on game feel)
            }

        }
        //---------------------------------Main Game Loop----------------------------------------------//

        #endregion


        #region Rest Trạng Thái Game
        static void ResetGameState()
        {
            // Reset game variables
            score = 0;
            removedLines = 0;
            level = 1;
            levelremovelines = 0;
            bagIndex = 0;
            holdIndex = -1;
            holdChar = ' ';
            amount = 0;
            maxTime = 22;
            currentRot = 0;
            currentX = 4;
            currentY = 0;

            for (int y = 0; y < mapSizeY; y++)
            {
                for (int x = 0; x < mapSizeX; x++)
                {
                    bg[y, x] = '-';
                    blockColoursOnScreen[y, x] = ConsoleColor.DarkGray;
                }
            }

            // Clear shadow colors
            for (int y = 0; y < mapSizeY; y++)
            {
                for (int x = 0; x < mapSizeX; x++)
                {
                    shadowColorsOnScreen[y, x] = ConsoleColor.DarkGray;
                }
            }

            // Clear hold area colors
            holdColour = ConsoleColor.DarkGray;

            // Clear next block colors
            for (int y = 0; y < mapSizeY; y++)
            {
                for (int x = 0; x < upNextSize; x++)
                {
                    nextBlockColours[y, x] = ConsoleColor.DarkGray;
                }
            }

            // Generate new bags
            bag = GenerateBag();
            nextBag = GenerateBag();

            // Restart stopwatch
            stopwatch.Restart();
        }

        #endregion


        #region Game Mode thứ 3: Classic With Blocks
        static void SetupClassicWithBlocks(ClWBDifficulty difficulty)
        {
            ResetGameState();
            Console.Clear();
            gameMode = GameMode.Classic;

            Random random = new Random();
            int blockHeight = 0;

            switch (difficulty)
            {
                case ClWBDifficulty.Easy:
                    blockHeight = (int)(mapSizeY * 0.2);
                    break;
                case ClWBDifficulty.Medium:
                    blockHeight = (int)(mapSizeY * 0.3);
                    break;
                case ClWBDifficulty.Hard:
                    blockHeight = (int)(mapSizeY * 0.5);
                    break;
            }

            // Generate random blocks
            for (int y = mapSizeY - blockHeight; y < mapSizeY; y++)
            {
                for (int x = 0; x < mapSizeX; x++)
                {
                    if (random.NextDouble() < 0.5) // 50% chance to place a block
                    {
                        bg[y, x] = '#';
                        blockColoursOnScreen[y, x] = colours[random.Next(0, colours.Length)];
                    }
                    else
                    {
                        bg[y, x] = '-';
                        blockColoursOnScreen[y, x] = ConsoleColor.DarkGray;
                    }
                }
            }

            DrawBorderingame();

            // Redraw the game board to show the random blocks
            char[,] view = RenderView();
            char[,] hold = RenderHold();
            char[,] next = RenderUpNext();
            Print(view, hold, next);

            NewBlock();
            PlaySound(gamePlayer, @"e:\TetrisMusic\Main.wav");
        }

        static void SelectClassicWithBlocks()
        {
            Console.Clear();
            Console.SetCursorPosition(1, 1);
            DrawBorder();

            string[] difficultyMenu = new string[]
            {
        "+---------------------+",
        "|  Select Block Height|",
        "| 1. Easy             |",
        "| 2. Medium           |",
        "| 3. Hard             |",
        "| Esc to Return       |",
        "+---------------------+"
            };

            int startY = 14 - difficultyMenu.Length;
            foreach (string line in difficultyMenu)
            {
                int startX = 34 - line.Length;
                Console.SetCursorPosition(startX, startY++);
                Console.WriteLine(line);
            }

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.D1:
                        SetupClassicWithBlocks(ClWBDifficulty.Easy);
                        return;
                    case ConsoleKey.D2:
                        SetupClassicWithBlocks(ClWBDifficulty.Medium);
                        return;
                    case ConsoleKey.D3:
                        SetupClassicWithBlocks(ClWBDifficulty.Hard);
                        return;
                    case ConsoleKey.Escape:
                        TitleScreen();
                        return;
                }
            }
        }
        #endregion


        #region Game Mode thứ 2: Puzzle
        static void SelectPuzzleDifficulty()
        {
            Console.Clear();
            Console.SetCursorPosition(1, 1);
            DrawBorder();

            string[] difficultyMenu = new string[]
            {
                "+---------------------+",
                "|  Select Difficulty  |",
                "| 1. Easy             |",
                "| 2. Medium           |",
                "| 3. Hard             |",
                "| Esc to Return       |",
                "+---------------------+"
            };

            int startY = 14 - difficultyMenu.Length;
            foreach (string line in difficultyMenu)
            {
                int startX = 34 - line.Length;
                Console.SetCursorPosition(startX, startY++);
                Console.WriteLine(line);
            }

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.D1:
                        SetupPuzzleMode(PuzzleDifficulty.Easy);
                        return;
                    case ConsoleKey.D2:
                        SetupPuzzleMode(PuzzleDifficulty.Medium);
                        return;
                    case ConsoleKey.D3:
                        SetupPuzzleMode(PuzzleDifficulty.Hard);
                        return;
                    case ConsoleKey.Escape:
                        TitleScreen();
                        return;
                }
            }
        }

        // Add method to setup puzzle mode
        static void SetupPuzzleMode(PuzzleDifficulty difficulty)
        {
            ResetGameState();
            Console.Clear();
            gameMode = GameMode.Puzzle;
            puzzleDifficulty = difficulty;
            movesUsed = 0; // Reset moves used when starting a new puzzle

            switch (difficulty)
            {
                case PuzzleDifficulty.Easy:
                    movesRemaining = 20;
                    targetLines = 3;
                    break;
                case PuzzleDifficulty.Medium:
                    movesRemaining = 15;
                    targetLines = 4;
                    break;
                case PuzzleDifficulty.Hard:
                    movesRemaining = 10;
                    targetLines = 5;
                    break;
            }

            DrawBorderingame();
            NewBlock();
            PlaySound(gamePlayer, @"e:\TetrisMusic\Main.wav");
        }

        static void PuzzleGameOver(bool won)
        {
            StopSound(gamePlayer);
            if (won)
            {
                PlaySound(effectPlayer, @"e:\TetrisMusic\GameWon.wav");
            }
            else
            {
                PlaySound(effectPlayer, @"e:\TetrisMusic\GameOver.wav");
            }

            Console.Clear();
            DrawBorder();

            int windowWidth = 40;
            int windowHeight = 20;
            int centerX = windowWidth / 2 - 9;
            int centerY = windowHeight / 2 - 3;

            string resultMessage = won ? "PUZZLE COMPLETE!" : "PUZZLE FAILED";
            ConsoleColor messageColor = won ? ConsoleColor.Green : ConsoleColor.Red;

            // Draw game over box
            Console.SetCursorPosition(centerX, centerY);
            Console.WriteLine("+--------------------+");
            Console.SetCursorPosition(centerX, centerY + 1);
            Console.ForegroundColor = messageColor;
            Console.WriteLine($"|  {resultMessage}  |");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.SetCursorPosition(centerX, centerY + 2);
            Console.WriteLine("+--------------------+");
            Console.SetCursorPosition(centerX, centerY + 3);
            Console.WriteLine($"| Lines: {removedLines}/{targetLines}       |");
            Console.SetCursorPosition(centerX, centerY + 4);
            Console.WriteLine($"| Moves Used: {movesUsed}     |");
            Console.SetCursorPosition(centerX, centerY + 5);
            Console.WriteLine("+--------------------+");
            Console.SetCursorPosition(centerX, centerY + 6);
            Console.WriteLine("| Play again? (Y/N)  |");
            Console.SetCursorPosition(centerX, centerY + 7);
            Console.WriteLine("+--------------------+");

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                switch (char.ToUpper(key.KeyChar))
                {
                    case 'Y':
                        StopSound(effectPlayer);
                        SelectPuzzleDifficulty();
                        return;
                    case 'N':
                        StopSound(effectPlayer);
                        TitleScreen(); // Return to the main menu instead of exiting
                        return;
                }
            }
        }

        #endregion


        static int GetShadowY()
        {
            int shadowY = currentY;
            while (!Collision(currentIndex, bg, currentX, shadowY + 1, currentRot))
            {
                shadowY++;
            }
            return shadowY;
        }


        #region Màn hình chính và màn hình chuyển tiếp
        static void TransitionScreen()
        {
            Console.Clear();
            DrawBorder();

            int windowWidth = 40;
            int windowHeight = 22;
            int centerX = windowWidth / 2 - 10; // Center alignment for 20 character width loading bar
            int centerY = windowHeight / 2 - 3;

            int barWidth = 20; // Width of the loading bar
            string emptyBar = new string(' ', barWidth); // Empty spaces in the bar
            string fullBar = new string('#', barWidth);  // Full bar representation

            // Loading animation with progress bar
            for (int i = 0; i <= barWidth; i++)
            {
                string currentBar = new string('#', i) + new string(' ', barWidth - i); // Dynamic bar filling
                Console.CursorVisible = false;
                // Display the loading bar frame
                Console.SetCursorPosition(centerX, centerY);
                Console.WriteLine("+----------------------+");
                Console.SetCursorPosition(centerX, centerY + 1);
                Console.WriteLine("|      LOADING...      |");
                Console.SetCursorPosition(centerX, centerY + 2);
                Console.WriteLine($"|[{currentBar}]| "); // Display the progress bar
                Console.SetCursorPosition(centerX, centerY + 3);
                Console.WriteLine("|                      |");
                Console.SetCursorPosition(centerX, centerY + 4);
                Console.WriteLine("+----------------------+");

                Thread.Sleep(100); // Simulate loading time
            }

            // Final countdown
            for (int i = 3; i > 0; i--)
            {
                Console.SetCursorPosition(centerX, centerY);
                Console.WriteLine("+----------------------+");
                Console.SetCursorPosition(centerX, centerY + 1);
                Console.WriteLine($"|    Starting in {i}...  |");
                Console.SetCursorPosition(centerX, centerY + 2);
                Console.WriteLine("|                      |");
                Console.SetCursorPosition(centerX, centerY + 3);
                Console.WriteLine("|     GET  READY!      |");
                Console.SetCursorPosition(centerX, centerY + 4);
                Console.WriteLine("+----------------------+");

                Thread.Sleep(1000); // 1-second countdown
            }

            Console.Clear();
        }

        static void TitleScreen()
        {
            PlaySound(titlePlayer, @"e:\TetrisMusic\Menu.wav");
            Console.WindowHeight = 22;
            Console.Clear();
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            DrawBorder();

            string[] tetrisArt = new string[]
            {
                "TTTTTT EEEEEE TTTTTT  RRRRR   IIII  SSSSS",
                "  TT   EE       TT    R    R   II   SS    ",
                "  TT   EEEE     TT    RRRRR    II   SSSSS",
                "  TT   EE       TT    R  RR    II      SS",
                "  TT   EEEEEE   TT    R    RR IIII  SSSSS"
            };

            int windowWidth = 40;
            int windowHeight = 20;
            int artStartY = 4;



            // Display the TETRIS art
            for (int i = 0; i < tetrisArt.Length; i++)
            {
                int artWidth = tetrisArt[i].Length;
                int startX = Math.Max(1, (windowWidth - artWidth) / 2);
                Console.SetCursorPosition(startX, artStartY + i);

                foreach (char c in tetrisArt[i])
                {
                    Console.Write(c);
                    Thread.Sleep(5);
                }
            }

            string[] menuLines = new string[]
            {
               "+-----------------------+",
               "| 1. Classic Mode       |",
               "| 2. Puzzle Mode        |",
               "| 3. Classic with Blocks|",
               "| Esc to Exit           |",
               "+-----------------------+"
            };

            int menuStartY = artStartY + tetrisArt.Length + 2;

            foreach (string line in menuLines)
            {
                int menuX = Math.Max(1, (windowWidth - line.Length) / 2);
                Console.SetCursorPosition(menuX, menuStartY++);
                foreach (char c in line)
                {
                    Console.Write(c);
                    Thread.Sleep(10);
                }
            }

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.D1)
                {
                    ResetGameState();
                    gameMode = GameMode.Classic;
                    Console.Clear();
                    DrawBorderingame();
                    NewBlock();
                    break;
                }
                else if (key.Key == ConsoleKey.D2)
                {
                    ResetGameState();
                    SelectPuzzleDifficulty();
                    break;
                }
                else if (key.Key == ConsoleKey.D3)
                {
                    ResetGameState();
                    SelectClassicWithBlocks();
                    break;
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    Environment.Exit(0);
                }
            }
        }

        #endregion


        #region Xử Lý Input từ Người Chơi
        static void InputHandler()
        {
            switch (input.Key)
            {
                // Left arrow = move left (if it doesn't collide)
                case ConsoleKey.A:
                case ConsoleKey.LeftArrow:
                    if (!Collision(currentIndex, bg, currentX - 1, currentY, currentRot)) currentX -= 1;
                    break;

                // Right arrow = move right (if it doesn't collide)
                case ConsoleKey.D:
                case ConsoleKey.RightArrow:
                    if (!Collision(currentIndex, bg, currentX + 1, currentY, currentRot)) currentX += 1;
                    break;

                // Rotate block (if it doesn't collide)
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    int newRot = currentRot + 1;
                    if (newRot >= 4) newRot = 0;
                    if (!Collision(currentIndex, bg, currentX, currentY, newRot)) currentRot = newRot;

                    break;

                // Move the block instantly down (hard drop)
                case ConsoleKey.Spacebar:
                    int i = 0;
                    while (true)
                    {
                        i++;
                        if (Collision(currentIndex, bg, currentX, currentY + i, currentRot))
                        {
                            currentY += i - 1;
                            BlockDownCollision(); // This will increment movesUsed in puzzle mode
                            break;
                        }
                    }
                    break;


                // Quit
                case ConsoleKey.Escape:
                    esc();
                    break;

                // Hold block
                case ConsoleKey.Enter:

                    // If there isnt a current held block:
                    if (holdIndex == -1)
                    {
                        holdIndex = currentIndex;
                        holdChar = currentChar;
                        NewBlock();
                    }
                    // If there is:
                    else
                    {
                        if (!Collision(holdIndex, bg, currentX, currentY, 0)) // Check for collision
                        {

                            // Switch current and hold
                            int c = currentIndex;
                            char ch = currentChar;
                            currentIndex = holdIndex;
                            currentChar = holdChar;
                            holdIndex = c;
                            holdChar = ch;
                        }

                    }
                    break;

                // Move down faster
                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    if (!Collision(currentIndex, bg, currentX, currentY + 1, currentRot))
                    {
                        currentY++;
                    }
                    else
                    {
                        BlockDownCollision(); // This will increment movesUsed in puzzle mode
                    }
                    break;

            }
        }

        #endregion

        static void BlockDownCollision()
        {
            // Add blocks from current to background
            for (int i = 0; i < positions.GetLength(2); i++)
            {
                int x = positions[currentIndex, currentRot, i, 0] + currentX;
                int y = positions[currentIndex, currentRot, i, 1] + currentY;
                if (y >= 0 && y < mapSizeY && x >= 0 && x < mapSizeX)
                {
                    bg[y, x] = '#';
                    blockColoursOnScreen[y, x] = blockColours[currentIndex];
                }
            }

            if (gameMode == GameMode.Puzzle)
            {
                movesUsed++; // Increment moves used when a piece is placed
                movesRemaining--;
            }

            // Check for lines
            bool linesCleared = false;
            int linesThisMove = 0;
            while (true)
            {
                int lineY = Line(bg);
                if (lineY != -1)
                {
                    ClearLine(lineY);
                    linesCleared = true;
                    linesThisMove++;
                    continue;
                }
                break;
            }

            // Check win/lose conditions for puzzle mode
            if (gameMode == GameMode.Puzzle)
            {
                // Update removedLines count
                

                if (removedLines >= targetLines)
                {
                    PuzzleGameOver(true);
                    return;
                }
                else if (movesRemaining <= 0)
                {
                    PuzzleGameOver(false);
                    return;
                }
            }

            NewBlock();
        }
        static ConsoleColor[,] blockColoursOnScreen = new ConsoleColor[mapSizeY, mapSizeX];


        #region Màn Hình Thoát Game
        static void esc()
        {
            Console.Clear();
            DrawBorder();
            int windowWidth = 40;
            int windowHeight = 20;
            int centerX = windowWidth / 2 - 9;  // Tâm X để căn giữa thông báo
            int centerY = windowHeight / 2 - 3;  // Tâm Y để căn giữa thông báo

            // In thông báo Game Over
            Console.SetCursorPosition(centerX, centerY);
            Console.WriteLine("+--------------------+");
            Console.SetCursorPosition(centerX, centerY + 1);
            Console.WriteLine("|   DO YOU WANT TO   |");
            Console.SetCursorPosition(centerX, centerY + 2);
            Console.WriteLine("|   EXIT THE GAME?   |");
            Console.SetCursorPosition(centerX, centerY + 3);
            Console.WriteLine("+--------------------+");
            Console.SetCursorPosition(centerX, centerY + 4);
            Console.WriteLine("|   YES OR NO(Y/N)   |");
            Console.SetCursorPosition(centerX, centerY + 5);
            Console.WriteLine("+--------------------+");
            Console.SetCursorPosition(centerX, centerY + 6);
            Console.WriteLine("|                    |");
            Console.SetCursorPosition(centerX, centerY + 7);
            Console.WriteLine("+--------------------+");

            // Wait for user input
            ConsoleKeyInfo keyInfo;
            do
            {
                keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Y)
                {
                    TitleScreen(); // Exit the game
                }
                else if (keyInfo.Key == ConsoleKey.N)
                {
                    Console.Clear();
                    DrawBorderingame();
                    return; // Resume the game
                }
            }
            while (keyInfo.Key != ConsoleKey.Y && keyInfo.Key != ConsoleKey.N);
        }

        #endregion


        static void Restart()
        {
            ResetGameState();
            Console.Clear();

            if (gameMode == GameMode.Classic)
            {
                DrawBorderingame();
                NewBlock();
            }
            else if (gameMode == GameMode.Puzzle)
            {
                SelectPuzzleDifficulty();
            }
            else // Classic with Blocks
            {
                SelectClassicWithBlocks();
            }

            PlaySound(gamePlayer, @"e:\TetrisMusic\Main.wav");
        }


        static void ClearLine(int lineY)
        {
            score += 40;
            removedLines++;
            levelremovelines++;
            // Clear said line
            for (int x = 0; x < mapSizeX; x++)
            {
                bg[lineY, x] = '-';
                blockColoursOnScreen[lineY, x] = ConsoleColor.DarkGray;
            }

            // Loop through all blocks above line
            for (int y = lineY - 1; y > 0; y--)
            {
                for (int x = 0; x < mapSizeX; x++)
                {
                    // Move each character down
                    char character = bg[y, x];
                    if (character != '-')
                    {
                        bg[y, x] = '-';
                        bg[y + 1, x] = character;
                        blockColoursOnScreen[y + 1, x] = blockColoursOnScreen[y, x];
                        blockColoursOnScreen[y, x] = ConsoleColor.DarkGray;
                    }

                }
            }
        }



        #region Render cho Game
        static char[,] RenderView()
        {
            char[,] view = new char[mapSizeY, mapSizeX];

            // Make view equal to bg
            for (int y = 0; y < mapSizeY; y++)
                for (int x = 0; x < mapSizeX; x++)
                    view[y, x] = bg[y, x];

            // Calculate shadow position
            int shadowY = GetShadowY();

            // Render shadow first (so it appears behind the actual piece)
            if (shadowY != currentY)
            {
                for (int i = 0; i < positions.GetLength(2); i++)
                {
                    int x = positions[currentIndex, currentRot, i, 0] + currentX;
                    int y = positions[currentIndex, currentRot, i, 1] + shadowY;
                    if (y >= 0 && y < mapSizeY && x >= 0 && x < mapSizeX)
                    {
                        if (view[y, x] == '-')  // Only draw shadow if space is empty
                        {
                            view[y, x] = '░';  // Using a different character for shadow
                                               // Store shadow color (darker version of the piece color)
                            shadowColorsOnScreen[y, x] = ConsoleColor.DarkGray;
                        }
                    }
                }
            }

            // Overlay current piece (same as before)
            for (int i = 0; i < positions.GetLength(2); i++)
            {
                int x = positions[currentIndex, currentRot, i, 0] + currentX;
                int y = positions[currentIndex, currentRot, i, 1] + currentY;
                if (y >= 0 && y < mapSizeY && x >= 0 && x < mapSizeX)
                {
                    view[y, x] = '#';
                    blockColoursOnScreen[y, x] = blockColours[currentIndex];
                }
            }
            return view;
        }

        static ConsoleColor[,] shadowColorsOnScreen = new ConsoleColor[mapSizeY, mapSizeX];


        static char[,] RenderHold()
        {
            char[,] hold = new char[holdSizeY, holdSizeX];
            // Hold = ' ' array
            for (int y = 0; y < holdSizeY; y++)
                for (int x = 0; x < holdSizeX; x++)
                    hold[y, x] = ' ';


            // If there is a held block
            if (holdIndex != -1)
            {
                // Overlay blocks from hold
                for (int i = 0; i < positions.GetLength(2); i++)
                {
                    hold[positions[holdIndex, 0, i, 1] + 1, positions[holdIndex, 0, i, 0] + 1] = '#';
                }
                holdColour = blockColours[holdIndex];
            }
            return hold;
        }

        static ConsoleColor holdColour;
        static char[,] RenderUpNext()
        {
            // Up next = ' ' array   
            char[,] next = new char[mapSizeY, upNextSize];
            for (int y = 0; y < mapSizeY; y++)
                for (int x = 0; x < upNextSize; x++)
                    next[y, x] = ' ';


            // Gán màu sắc cho các khối trong phần chuẩn bị xuất hiện
            // Gán màu sắc cho các khối trong phần chuẩn bị xuất hiện
            int nextBagIndex = 0;
            for (int i = 0; i < 3; i++) // Next 3 blocks
            {
                for (int l = 0; l < positions.GetLength(2); l++)
                {
                    if (i + bagIndex >= 7) // If we need to acces the next bag
                    {
                        next[positions[nextBag[nextBagIndex], 0, l, 1] + 5 * i, positions[nextBag[nextBagIndex], 0, l, 0] + 1] = '#';
                        nextBlockColours[positions[nextBag[nextBagIndex], 0, l, 1] + 5 * i, positions[nextBag[nextBagIndex], 0, l, 0] + 1] = blockColours[nextBag[nextBagIndex]];
                    }
                    else
                    {
                        next[positions[bag[bagIndex + i], 0, l, 1] + 5 * i, positions[bag[bagIndex + i], 0, l, 0] + 1] = '#';
                        nextBlockColours[positions[bag[bagIndex + i], 0, l, 1] + 5 * i, positions[bag[bagIndex + i], 0, l, 0] + 1] = blockColours[bag[bagIndex + i]];
                    }
                }
                if (i + bagIndex >= 7) nextBagIndex++;
            }

            return next;
        }

        static ConsoleColor[,] nextBlockColours = new ConsoleColor[mapSizeY, upNextSize];
        #endregion



        static void Print(char[,] view, char[,] hold, char[,] next)
        {
            Level();
            Console.ForegroundColor = ConsoleColor.Gray; // Reset color for border

            for (int y = 0; y < mapSizeY; y++)
            {
                Console.SetCursorPosition(1, y + 1);
                for (int x = 0; x < holdSizeX + mapSizeX + upNextSize; x++)
                {
                    char i = ' ';
                    if (x < holdSizeX) i = hold[y, x];
                    else if (x >= holdSizeX + mapSizeX) i = next[y, x - mapSizeX - upNextSize];
                    else i = view[y, (x - holdSizeX)];

                    // Colors handling
                    if (x >= holdSizeX && x < holdSizeX + mapSizeX)
                    {
                        int gameX = x - holdSizeX;
                        if (i == '#')
                        {
                            Console.ForegroundColor = blockColoursOnScreen[y, gameX];
                        }
                        else if (i == '░')
                        {
                            Console.ForegroundColor = shadowColorsOnScreen[y, gameX];
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                        }
                    }
                    else if (i == '#')
                    {
                        if (x < holdSizeX)
                        {
                            Console.ForegroundColor = holdColour;
                        }
                        else
                        {
                            int indexX = x - holdSizeX - mapSizeX;
                            Console.ForegroundColor = nextBlockColours[y, indexX];
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                    }
                    Console.Write(i);
                }

                Console.ForegroundColor = ConsoleColor.Gray; // Reset color for status text
                if (gameMode == GameMode.Classic)
                {
                    if (y == 1) Console.Write($"| High Score: {highScore}  ");
                    if (y == 3) Console.Write($"| Score: {score}  ");
                    if (y == 5) Console.Write($"| Lines: {removedLines} ");
                    if (y == 7) Console.Write($"| Time: {stopwatch.Elapsed.Minutes}:{stopwatch.Elapsed.Seconds:D2} ");
                    if (y == 9) Console.Write($"| Level: {level} ");
                }
                else // Puzzle Mode
                {
                    if (y == 1) Console.Write($"| Difficulty: {puzzleDifficulty}");
                    if (y == 3) Console.Write($"| Moves Left: {movesRemaining}    ");
                    if (y == 5) Console.Write($"| Lines: {removedLines}/{targetLines}     ");
                    if (y == 7) Console.Write($"| Time: {stopwatch.Elapsed.Minutes}:{stopwatch.Elapsed.Seconds:D2} ");
                }
                Console.WriteLine();
            }
        }

        #region Thuật toán để tạo các mảnh Tetris ngẫu nhiên
        static int[] GenerateBag()
        {
            // Not my code, source https://stackoverflow.com/questions/108819/best-way-to-randomize-an-array-with-net
            Random random = new Random();
            int n = 7;
            int[] ret = { 0, 1, 2, 3, 4, 5, 6 };
            while (n > 1)
            {
                int k = random.Next(n--);
                int temp = ret[n];
                ret[n] = ret[k];
                ret[k] = temp;

            }
            return ret;

        }

        #endregion

        static bool Collision(int index, char[,] bg, int x, int y, int rot)
        {

            for (int i = 0; i < positions.GetLength(2); i++)
            {
                // Check if out of bounds
                if (positions[index, rot, i, 1] + y >= mapSizeY || positions[index, rot, i, 0] + x < 0 || positions[index, rot, i, 0] + x >= mapSizeX)
                {
                    return true;
                }
                // Check if not '-'
                if (bg[positions[index, rot, i, 1] + y, positions[index, rot, i, 0] + x] != '-')
                {
                    return true;
                }
            }

            return false;
        }

        static int Line(char[,] bg)
        {
            for (int y = 0; y < mapSizeY; y++)
            {
                bool i = true;
                for (int x = 0; x < mapSizeX; x++)
                {
                    if (bg[y, x] == '-')
                    {
                        i = false;
                    }
                }
                if (i) return y;
            }

            // If no line return -1
            return -1;
        }

        static void NewBlock()
        {
            // Check if new bag is necessary
            if (bagIndex >= 7)
            {
                bagIndex = 0;
                bag = nextBag;
                nextBag = GenerateBag();
            }

            // Reset everything
            currentY = 0;
            currentX = 4;
            currentChar = '#';
            currentIndex = bag[bagIndex];

            // Gán màu sắc cho khối mới
            if (currentIndex >= 0 && currentIndex < colours.Length)
            {
                blockColours[currentIndex] = colours[currentIndex];
            }

            // Gán màu sắc cho các khối trong phần chuẩn bị xuất hiện
            for (int i = 0; i < 3; i++)
            {
                if (i + bagIndex >= 7)
                {
                    if (i - (7 - bagIndex) >= 0 && i - (7 - bagIndex) < blockColours.Length)
                    {
                        blockColours[nextBag[i - (7 - bagIndex)]] = colours[nextBag[i - (7 - bagIndex)]];
                    }
                }
                else
                {
                    if (bagIndex + i >= 0 && bagIndex + i < blockColours.Length)
                    {
                        blockColours[bag[bagIndex + i]] = colours[bag[bagIndex + i]];
                    }
                }
            }

            // Check for collision
            if (Collision(currentIndex, bg, currentX, currentY, currentRot) && amount > 0)
            {
                GameOver();
            }
            bagIndex++;
            amount++;
        }

        #region Vẽ Viền Cho Game
        static void DrawBorder()
        {
            int width = 40;
            int height = 20;

            // Vẽ viền trên
            Console.SetCursorPosition(0, 0);
            Console.Write("+" + new string('-', width + 2) + "+");

            // Vẽ các cạnh
            for (int i = 1; i < height + 1; i++)
            {
                Console.SetCursorPosition(0, i);
                Console.Write("|");

                Console.SetCursorPosition(width + 3, i);
                Console.Write("|");
            }

            // Vẽ viền dưới
            Console.SetCursorPosition(0, height + 1);
            Console.Write("+" + new string('-', width + 2) + "+");
        }
        static void DrawBorderingame()
        {
            int width = 40;
            int height = 20;

            // Vẽ viền trên
            Console.SetCursorPosition(0, 0);
            Console.Write("+" + new string('-', width + 2) + "+");

            // Vẽ các cạnh
            for (int i = 1; i < height + 1; i++)
            {
                Console.SetCursorPosition(0, i);
                Console.Write("|");
                Console.SetCursorPosition(width - 17, i);
                Console.Write("|");
                Console.SetCursorPosition(width + 3, i);
                Console.Write("|");
            }
            Console.SetCursorPosition(23, height - 8);
            Console.Write("+" + new string('-', 19) + "+");
            // Vẽ viền dưới
            Console.SetCursorPosition(0, height + 1);
            Console.Write("+" + new string('-', width + 2) + "+");
        }

        #endregion


        #region Tăng Level Game
        static void Level()//themvao
        {
            if (levelremovelines >= 5 && level <= 10)
            {
                level++;
                maxTime -= 2;
                levelremovelines -= 5;
            }
        }

        #endregion


        #region Đọc Và Lưu Điểm Cao Nhất Trong File
        // Phương thức đọc điểm cao nhất từ tệp
        static void LoadHighScore()
        {
            if (File.Exists(highScoreFile))
            {
                string scoreText = File.ReadAllText(highScoreFile);
                int.TryParse(scoreText, out highScore); // Chuyển đổi giá trị trong tệp thành số
            }
        }
        // Phương thức cập nhật điểm cao nhất
        static void UpdateHighScore(int currentScore)
        {
            if (currentScore > highScore)
            {
                highScore = currentScore;
                File.WriteAllText(highScoreFile, highScore.ToString()); // Lưu điểm cao nhất vào tệp
            }
        }

        #endregion


        #region hàm Game Over
        static void GameOver()
        {
            StopSound(gamePlayer);
            PlaySound(effectPlayer, @"e:\TetrisMusic\GameOver.wav");

            // Update high score if needed
            UpdateHighScore(score);

            // Clear the console and draw the border
            Console.Clear();
            DrawBorder();

            int windowWidth = 40;
            int windowHeight = 20;
            int centerX = windowWidth / 2 - 9;
            int centerY = windowHeight / 2 - 3;

            // Display Game Over message
            Console.SetCursorPosition(centerX, centerY);
            Console.WriteLine("+--------------------+");
            Console.SetCursorPosition(centerX, centerY + 1);
            Console.WriteLine("|     GAME OVER      |");
            Console.SetCursorPosition(centerX, centerY + 2);
            Console.WriteLine("+--------------------+");
            Console.SetCursorPosition(centerX, centerY + 3);
            Console.WriteLine("| Your Score: " + score.ToString("D4") + "   |");
            Console.SetCursorPosition(centerX, centerY + 4);
            Console.WriteLine("| High Score: " + highScore.ToString("D4") + "   |");
            Console.SetCursorPosition(centerX, centerY + 5);
            Console.WriteLine("+--------------------+");
            Console.SetCursorPosition(centerX, centerY + 6);
            Console.WriteLine("| Play again? (Y/N)  |");
            Console.SetCursorPosition(centerX, centerY + 7);
            Console.WriteLine("+--------------------+");

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Y)
                {
                    StopSound(effectPlayer);
                    Restart();
                    break;
                }
                else if (key.Key == ConsoleKey.N)
                {
                    StopSound(effectPlayer);
                    TitleScreen(); // Return to the main menu instead of exiting
                    break;
                }
            }
        }

        #endregion

        static void Input()
        {
            while (true)
            {
                // Get input
                input = Console.ReadKey(true);
            }
        }
    }

}