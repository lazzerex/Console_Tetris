using System;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Collections.Generic;

namespace Tetris
{
    class Program1
    {
        static int GetShadowY()
        {
            int shadowY = currentY;
            while (!Collision(currentIndex, bg, currentX, shadowY + 1, currentRot))
            {
                shadowY++;
            }
            return shadowY;
        }

        static void TitleScreen()
        {
            Console.Clear();
            Console.OutputEncoding = System.Text.Encoding.UTF8; // Ensure encoding supports special characters

            // Larger, more detailed ASCII art for TETRIS with some depth and shadowing effect

            string[] tetrisArt = new string[]
            {
       "  TTTTTTT  EEEEEEE  TTTTTTT  RRRRR    IIIIII  SSSSSS ",
       "    TT     EE         TT     R    R     II    SS      ",
       "    TT     EEEEE      TT     RRRRR      II    SSSSSS  ",
       "    TT     EE         TT     R  RR      II        SS  ",
       "    TT     EEEEEEE    TT     R    RR  IIIIII  SSSSSS "
            };

            // Render frame around the title screen with new dimensions 45x25
            string[] frame = new string[]
            {
        "+---------------------------------------------+",
        "|                                             |",
        "|                                             |",
        "|                                             |",
        "|                                             |",
        "|                                             |",
        "|                                             |",
        "|                                             |",
        "|                                             |",
        "|                                             |",
        "|                                             |",
        "|                                             |",
        "|                                             |",
        "|                                             |",
        "|                                             |",
        "+---------------------------------------------+"
            };

            int frameWidth = 45;   // Frame width
            int frameHeight = 25;  // Frame height
            int windowWidth = 80;  // Console window width
            int windowHeight = 30; // Console window height

            int frameX = windowWidth / 2 - frameWidth / 2;  // Center the frame horizontally
            int frameY = windowHeight / 2 - frameHeight / 2;  // Center the frame vertically

            // Draw the frame
            foreach (string line in frame)
            {
                Console.SetCursorPosition(frameX, frameY++);
                foreach (char c in line)
                {
                    Console.Write(c);
                    Thread.Sleep(5);  // Fast typing effect for the frame
                }
                Console.WriteLine();
            }

            // Render ASCII art with a typing effect inside the frame
            int artWidth = tetrisArt[0].Length;  // Width of the Tetris ASCII art
            int artHeight = tetrisArt.Length;    // Height of the Tetris ASCII art

            int artCenterX = frameX + (frameWidth - artWidth) / 2;  // Horizontally center the ASCII art within the frame
            int artCenterY = frameY - frameHeight + (frameHeight - artHeight) / 2;  // Vertically center within the frame

            foreach (string line in tetrisArt)
            {
                Console.SetCursorPosition(artCenterX, artCenterY++);
                foreach (char c in line)
                {
                    Console.Write(c);
                    Thread.Sleep(30);  // Slower typing effect for the title
                }
                Console.WriteLine();
            }

            // Game Menu options with a slight delay for each letter
            string[] menuLines = new string[]
            {
                    "    |        +----------------------------+       |         ",
                    "    |        |  Press Enter to Play       |       |         ",
                    "    |        |  Press Esc to Exit         |       |         ",
                    "    |        +----------------------------+       |         "
            };

            int menuCenterY = artCenterY + 2; // Adjust Y position after ASCII art


            foreach (var line in menuLines)
            {
                Console.SetCursorPosition(artCenterX, menuCenterY++);
                foreach (char c in line)
                {
                    Console.Write(c);
                    Thread.Sleep(20);  // Slightly faster for menu options
                }
                Console.WriteLine();
            }

            // Wait for user input
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    // Clear the screen and start the game
                    Console.Clear();
                    break;
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    // Exit the game
                    Environment.Exit(0);
                }
            }
        }




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

        static void Main()
        {

            TitleScreen();
            LoadHighScore();
            Console.WindowHeight = 22;
            // Make the console cursor invisible
            Console.CursorVisible = false;
            blockColours = new ConsoleColor[7];
            // Title
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
            stopwatch.Start();
            while (true)
            {

                // Force block down
                if (timer >= maxTime)
                {
                    // If it doesn't collide, just move it down. If it does call BlockDownCollision
                    if (!Collision(currentIndex, bg, currentX, currentY + 1, currentRot)) currentY++;
                    else BlockDownCollision();

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
                    timer = maxTime;
                    break;

                case ConsoleKey.R:
                    Restart();
                    break;

                default:
                    break;
            }
        }
        static void BlockDownCollision()
        {

            // Add blocks from current to background
            for (int i = 0; i < positions.GetLength(2); i++)
            {
                bg[positions[currentIndex, currentRot, i, 1] + currentY, positions[currentIndex, currentRot, i, 0] + currentX] = '#';
                blockColoursOnScreen[positions[currentIndex, currentRot, i, 1] + currentY, positions[currentIndex, currentRot, i, 0] + currentX] = blockColours[currentIndex]; // Cập nhật màu sắc của các khối
            }

            // Loop 
            while (true)
            {
                // Check for line
                int lineY = Line(bg);

                // If a line is detected
                if (lineY != -1)
                {
                    ClearLine(lineY);

                    continue;
                }
                break;
            }
            // New block
            NewBlock();
        }
        static ConsoleColor[,] blockColoursOnScreen = new ConsoleColor[mapSizeY, mapSizeX];
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
                    Environment.Exit(0); // Exit the game
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

        static void Restart()
        {
            // Reset lại các biến trạng thái của trò chơi
            score = 0;
            removedLines = 0;
            level = 1;
            levelremovelines = 0;
            bagIndex = 0;
            holdIndex = -1;
            holdChar = ' ';
            amount = 0;
            maxTime = 22;

            // Làm mới lại bản đồ nền (bg)
            for (int y = 0; y < mapSizeY; y++)
            {
                for (int x = 0; x < mapSizeX; x++)
                {
                    bg[y, x] = '-';
                }
            }

            // Tạo túi khối mới
            bag = GenerateBag();
            nextBag = GenerateBag();

            // Tạo khối mới
            NewBlock();

            // Đặt lại đồng hồ
            stopwatch.Restart();

            // In ra thông tin về trạng thái mới của trò chơi
            Console.Clear();
            DrawBorderingame();
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


        static void Print(char[,] view, char[,] hold, char[,] next)
        {
            Level();
            for (int y = 0; y < mapSizeY; y++)
            {
                Console.SetCursorPosition(1, y + 1);
                for (int x = 0; x < holdSizeX + mapSizeX + upNextSize; x++)
                {
                    char i = ' ';
                    // Add hold + Main View + up next to view
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
                        else if (i == '░')  // Shadow character
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

                // Rest of the status information printing remains the same
                if (y == 1) Console.Write($"| High Score: {highScore}  ");
                if (y == 3) Console.Write($"| Score: {score}  ");
                if (y == 5) Console.Write($"| Lines: {removedLines} ");
                if (y == 7) Console.Write($"| Time: {stopwatch.Elapsed.Minutes}:{stopwatch.Elapsed.Seconds:D2} ");
                if (y == 9) Console.Write($"| Level: {level} ");
                Console.WriteLine();
            }

            // Reset cursor position
            Console.SetCursorPosition(0, Console.CursorTop - mapSizeY);
        }

        // Reset console cursor position

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
        static void Level()//themvao
        {
            if (levelremovelines >= 5 && level <= 10)
            {
                level++;
                maxTime -= 2;
                levelremovelines -= 5;
            }
        }

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
        static void GameOver()
        {
            // Kiểm tra và cập nhật điểm cao nhất nếu cần
            UpdateHighScore(score);

            // Hiển thị thông báo kết thúc trò chơi và điểm
            UpdateHighScore(score);

            // Vẽ khung xung quanh thông báo Game Over
            Console.Clear();
            DrawBorder(); // Hàm này sẽ vẽ khung bao quanh giao diện game

            int windowWidth = 40;
            int windowHeight = 20;
            int centerX = windowWidth / 2 - 9;  // Tâm X để căn giữa thông báo
            int centerY = windowHeight / 2 - 3;  // Tâm Y để căn giữa thông báo

            // In thông báo Game Over
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
                // Nhận input từ người dùng
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Y) // Nếu chọn 'Y', chơi lại
                {
                    Restart();
                    break;
                }
                else if (key.Key == ConsoleKey.N) // Nếu chọn 'N', thoát
                {
                    Environment.Exit(1);
                    break;
                }
            }
        }
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