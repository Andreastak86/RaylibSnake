using System;
using System.Collections.Generic;
using Raylib_cs;

// ─── Konfigurasjon ───────────────────────────────────────────────────────────
// Brettet er delt inn i et rutenett av like store celler.
// Vinduestørrelsen beregnes automatisk ut fra antall celler og cellestørrelse.
const int cellCount    = 20;    // antall celler bortover og nedover (20x20 rutenett)
const int cellSize     = 30;    // bredde og høyde på én celle i piksler
const int windowSize   = cellCount * cellSize; // total bredde/høyde på spillområdet
const float moveInterval = 0.12f; // sekunder mellom hver gang slangen beveger seg

// ─── Spilltilstand ───────────────────────────────────────────────────────────
// Samme Queue-logikk som i konsollversjonen — halen er fremst, hodet er bakerst.
var snake     = new Queue<(int x, int y)>();
var direction = (dx: 1, dy: 0); // starter med å bevege seg mot høyre
var food      = (x: 0, y: 0);
var random    = new Random();
var score     = 0;
var moveTimer = 0f;   // teller opp sekunder siden siste bevegelse
var gameOver  = false;

// ─── Hjelpefunksjoner ────────────────────────────────────────────────────────

// Finner en tilfeldig ledig posisjon for maten.
// Bruker HashSet for å effektivt sjekke om posisjonen er opptatt av slangen.
(int x, int y) GetNewFoodPosition()
{
    var snakePositions = new HashSet<(int, int)>(snake);
    int x, y;
    do
    {
        x = random.Next(0, cellCount);
        y = random.Next(0, cellCount);
    } while (snakePositions.Contains((x, y)));
    return (x, y);
}

// Tegner én enkelt celle som et fylt rektangel.
// Vi trekker fra 2 piksler (1 på hver side) for å lage en liten avstand mellom cellene,
// slik at rutenettet blir synlig også uten å tegne egne linjer.
void DrawCell(int x, int y, Color color)
{
    Raylib.DrawRectangle(
        x * cellSize + 1,   // venstre kant + 1px avstand
        y * cellSize + 1,   // øvre kant + 1px avstand
        cellSize - 2,       // trekk fra 2px for å lage gap
        cellSize - 2,
        color
    );
}

// ─── Spilloppsett ─────────────────────────────────────────────────────────────
// Slangen starter midt på brettet med to segmenter, beveger seg mot høyre.
int startX = cellCount / 2;
int startY = cellCount / 2;
snake.Enqueue((startX - 1, startY)); // hale
snake.Enqueue((startX,     startY)); // hode

food = GetNewFoodPosition();

// Raylib åpner et grafisk vindu. Høyden er 40px ekstra for scorelinje nederst.
// SetTargetFPS(60) ber Raylib om å begrense løkken til 60 oppdateringer per sekund.
Raylib.InitWindow(windowSize, windowSize + 40, "Snake med Raylib");
Raylib.SetTargetFPS(60);

// ─── Spillsløyfe ─────────────────────────────────────────────────────────────
// WindowShouldClose() returnerer true når spilleren lukker vinduet eller trykker Escape.
while (!Raylib.WindowShouldClose())
{
    // ── Input og spilloppdatering ─────────────────────────────────────────────
    if (!gameOver)
    {
        // Les retningstasten som holdes inne.
        // Vi hindrer 180°-snuing ved å sjekke at ny retning ikke er stikk motsatt.
        var newDirection = direction;
        if (Raylib.IsKeyDown(KeyboardKey.Up)    && direction.dy !=  1) newDirection = ( 0, -1);
        if (Raylib.IsKeyDown(KeyboardKey.Down)  && direction.dy != -1) newDirection = ( 0,  1);
        if (Raylib.IsKeyDown(KeyboardKey.Left)  && direction.dx !=  1) newDirection = (-1,  0);
        if (Raylib.IsKeyDown(KeyboardKey.Right) && direction.dx != -1) newDirection = ( 1,  0);
        direction = newDirection;

        // GetFrameTime() returnerer tiden (i sekunder) siden forrige frame.
        // Ved å summere denne over tid kan vi flytte slangen med jevne intervaller
        // uavhengig av hvor mange frames per sekund datamaskinen klarer å tegne.
        moveTimer += Raylib.GetFrameTime();

        if (moveTimer >= moveInterval)
        {
            moveTimer = 0f; // nullstill timeren

            // Beregn ny hodeposisjon basert på nåværende hode og retning.
            var snakeArray  = snake.ToArray();
            var currentHead = snakeArray[^1];
            var newHead     = (x: currentHead.x + direction.dx, y: currentHead.y + direction.dy);

            // ── Kollisjondeteksjon ────────────────────────────────────────────

            bool hitWall = newHead.x < 0 || newHead.x >= cellCount ||
                           newHead.y < 0 || newHead.y >= cellCount;
            bool hitSelf = new HashSet<(int, int)>(snake).Contains(newHead);

            if (hitWall || hitSelf)
            {
                gameOver = true;
            }
            else
            {
                // ── Flytt slangen ─────────────────────────────────────────────
                snake.Enqueue(newHead);

                if (newHead == food)
                {
                    // Spiste maten — slangen vokser og ny mat plasseres.
                    score++;
                    food = GetNewFoodPosition();
                }
                else
                {
                    // Vanlig bevegelse — fjern halen så lengden holder seg konstant.
                    snake.Dequeue();
                }
            }
        }
    }
    else
    {
        // Når spillet er over kan spilleren trykke R for å starte på nytt.
        if (Raylib.IsKeyPressed(KeyboardKey.R))
        {
            snake.Clear();
            snake.Enqueue((startX - 1, startY));
            snake.Enqueue((startX,     startY));
            direction = (1, 0);
            food      = GetNewFoodPosition();
            score     = 0;
            moveTimer = 0f;
            gameOver  = false;
        }
    }

    // ── Tegning ───────────────────────────────────────────────────────────────
    // Alt som tegnes må ligge mellom BeginDrawing() og EndDrawing().
    // ClearBackground fyller hele vinduet med én farge før vi tegner oppå.
    Raylib.BeginDrawing();
    Raylib.ClearBackground(Color.Black);

    // Tegn rutenettet som bakgrunn — mørke linjer for å vise cellegrensene.
    for (int column = 0; column < cellCount; column++)
        for (int row = 0; row < cellCount; row++)
            Raylib.DrawRectangleLines(column * cellSize, row * cellSize, cellSize, cellSize, new Color(30, 30, 30, 255));

    // Tegn slangen — hodet (siste element) får en lysere farge enn kroppen.
    var snakeSegments = snake.ToArray();
    for (int index = 0; index < snakeSegments.Length; index++)
    {
        bool isHead = index == snakeSegments.Length - 1;
        Color segmentColor = isHead
            ? new Color(100, 220, 100, 255)  // hode: lys grønn
            : new Color( 50, 160,  50, 255); // kropp: mørkere grønn
        DrawCell(snakeSegments[index].x, snakeSegments[index].y, segmentColor);
    }

    // Tegn maten som en rød celle.
    DrawCell(food.x, food.y, new Color(220, 60, 60, 255));

    // Tegn scorelinjen — en mørk stripe under spillbrettet med tekst.
    Raylib.DrawRectangle(0, windowSize, windowSize, 40, new Color(20, 20, 20, 255));
    Raylib.DrawText($"Poeng: {score}   Lengde: {snake.Count}", 10, windowSize + 10, 20, Color.White);

    // Vis et halvgjennomsiktig Game Over-overlay oppå spillet når det er tapt.
    if (gameOver)
    {
        Raylib.DrawRectangle(0, 0, windowSize, windowSize, new Color(0, 0, 0, 160));
        Raylib.DrawText("GAME OVER",               windowSize / 2 - 90,  windowSize / 2 - 30, 40, Color.Red);
        Raylib.DrawText($"Poeng: {score}",          windowSize / 2 - 55,  windowSize / 2 + 20, 26, Color.White);
        Raylib.DrawText("Trykk R for å starte igjen", windowSize / 2 - 120, windowSize / 2 + 60, 20, Color.Gray);
    }

    Raylib.EndDrawing();
}

// Frigjør Raylib-ressurser og lukker vinduet ryddig.
Raylib.CloseWindow();
