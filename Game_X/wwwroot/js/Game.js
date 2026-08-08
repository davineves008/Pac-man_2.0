document.addEventListener("DOMContentLoaded", () => {
    const canvas = document.getElementById("gameCanvas");
    if (!canvas) return;

    const ctx = canvas.getContext("2d");
    const tileSize = 30;

    let gameState = null;
    let currentDirection = "Right"; // Guarda a última direção apertada
    let mouthAngle = 0.2; // Abertura inicial da boca
    let mouthSpeed = 0.015; // Velocidade da mastigação constante
    let pulseTime = 0; // Para animação dos Power Pellets
    let isFetching = false; // Flag para evitar requisições encavaladas no polling
    let animationFrameId = null; // Variável declarada corretamente para o loop

    let floatingTexts = []; // Armazena os textos de pontos flutuantes
    let lastFruitState = false; // Guardar o estado do frame anterior da fruta

    // Tabela de Frutas
    const FRUITS = [
        { name: "Cereja", score: 100, color: "#ff0000", stemColor: "#00ff00" },
        { name: "Morango", score: 300, color: "#ff3333", stemColor: "#00cc00" },
        { name: "Laranja", score: 500, color: "#ffa500", stemColor: "#009900" },
        { name: "Maçã", score: 700, color: "#cc0000", stemColor: "#006600" },
        { name: "Melancia", score: 1000, color: "#009933", stemColor: "#ff3366" }
    ];

    // --- SISTEMA DE TEXTO FLUTUANTE ---

    function addFloatingText(text, tileX, tileY, color = "#ffb703") {
        floatingTexts.push({
            text: text,
            x: tileX * tileSize + tileSize / 2,
            y: tileY * tileSize,
            alpha: 1.0,
            color: color
        });
    }

    function drawFloatingTexts() {
        for (let i = floatingTexts.length - 1; i >= 0; i--) {
            let ft = floatingTexts[i];

            ctx.save();
            ctx.globalAlpha = ft.alpha;
            ctx.font = "bold 16px monospace";
            ctx.fillStyle = ft.color;
            ctx.textAlign = "center";

            ctx.shadowColor = "#000000";
            ctx.shadowBlur = 4;

            ctx.fillText(ft.text, ft.x, ft.y);
            ctx.restore();

            ft.y -= 0.8;
            ft.alpha -= 0.02;

            if (ft.alpha <= 0) {
                floatingTexts.splice(i, 1);
            }
        }
    }

    function checkFruitEaten() {
        if (!gameState) return;

        let bonus = gameState.bonusFruit || gameState.BonusFruit;
        if (!bonus) return;

        let isActive = bonus.active !== undefined ? bonus.active : bonus.Active;
        let bx = bonus.x !== undefined ? bonus.x : bonus.X;
        let by = bonus.y !== undefined ? bonus.y : bonus.Y;
        let type = bonus.type !== undefined ? bonus.type : bonus.Type;

        // Se no frame anterior estava ativa e agora sumiu: Pac-Man comeu!
        if (lastFruitState && !isActive) {
            let fruitInfo = FRUITS[type] || FRUITS[0];
            addFloatingText(`+${fruitInfo.score}`, bx, by, "#00ffcc");
        }

        lastFruitState = isActive;
    }

    // --- REQUISIÇÕES AO BACKEND ---

    async function fetchGameState() {
        if (isFetching) return;
        isFetching = true;

        try {
            let response = await fetch('/Game/State');

            if (response.status === 400) {
                await fetch('/Game/NewGame');
                response = await fetch('/Game/State');
            }

            if (response.ok) {
                const data = await response.json();

                if (!gameState && data.width && data.height) {
                    canvas.width = data.width * tileSize;
                    canvas.height = data.height * tileSize;
                }

                gameState = data;
            }
        } catch (error) {
            console.error("Erro ao carregar o estado do jogo:", error);
        } finally {
            isFetching = false;
        }
    }

    // --- FUNÇÕES DE DESENHO (MAPA, FRUTA, PACMAN, HUD) ---

    function drawMap() {
        if (!gameState) return;

        pulseTime += 0.05;

        // Paredes
        if (gameState.tiles) {
            gameState.tiles.forEach(tile => {
                let x = tile.x * tileSize;
                let y = tile.y * tileSize;

                if (tile.type === 1 || tile.type === "Wall" || tile.type === "wall") {
                    ctx.fillStyle = "#0d1b2a";
                    ctx.fillRect(x, y, tileSize, tileSize);

                    ctx.strokeStyle = "#1e90ff";
                    ctx.lineWidth = 2;
                    ctx.strokeRect(x + 2, y + 2, tileSize - 4, tileSize - 4);
                }
            });
        }

        // Moedas
        if (gameState.coins) {
            gameState.coins.forEach(coin => {
                let isCollected = coin.collected !== undefined ? coin.collected : coin.Collected;

                if (!isCollected) {
                    let cx = (coin.x !== undefined ? coin.x : coin.X) * tileSize + tileSize / 2;
                    let cy = (coin.y !== undefined ? coin.y : coin.Y) * tileSize + tileSize / 2;

                    ctx.save();
                    ctx.shadowColor = "#ffb703";
                    ctx.shadowBlur = 6;

                    ctx.beginPath();
                    ctx.fillStyle = "#ffb703";
                    ctx.arc(cx, cy, 3, 0, Math.PI * 2);
                    ctx.fill();
                    ctx.restore();
                }
            });
        }

        // Power Pellets
        if (gameState.pellets) {
            gameState.pellets.forEach(pellet => {
                let isCollected = pellet.collected !== undefined ? pellet.collected : pellet.Collected;

                if (!isCollected) {
                    let px = (pellet.x !== undefined ? pellet.x : pellet.X) * tileSize + tileSize / 2;
                    let py = (pellet.y !== undefined ? pellet.y : pellet.Y) * tileSize + tileSize / 2;

                    let radius = 6.5 + Math.sin(pulseTime * 2) * 1.5;

                    ctx.save();
                    ctx.shadowColor = "#ffffff";
                    ctx.shadowBlur = 10;

                    ctx.beginPath();
                    ctx.fillStyle = "#ffffff";
                    ctx.arc(px, py, radius, 0, Math.PI * 2);
                    ctx.fill();
                    ctx.restore();
                }
            });
        }
    }

    function drawBonusFruit() {
        if (!gameState) return;

        let bonus = gameState.bonusFruit || gameState.BonusFruit;
        if (!bonus) return;

        let isActive = bonus.active !== undefined ? bonus.active : bonus.Active;
        if (!isActive) return;

        let bx = bonus.x !== undefined ? bonus.x : bonus.X;
        let by = bonus.y !== undefined ? bonus.y : bonus.Y;
        let type = bonus.type !== undefined ? bonus.type : bonus.Type;

        let fx = bx * tileSize + tileSize / 2;
        let fy = by * tileSize + tileSize / 2;
        let fruit = FRUITS[type] || FRUITS[0];

        ctx.save();

        ctx.shadowColor = fruit.color;
        ctx.shadowBlur = 8;

        ctx.fillStyle = fruit.color;
        ctx.beginPath();
        ctx.arc(fx - 3, fy + 2, 5, 0, Math.PI * 2);
        ctx.arc(fx + 3, fy + 2, 5, 0, Math.PI * 2);
        ctx.fill();

        ctx.strokeStyle = fruit.stemColor;
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.moveTo(fx - 3, fy - 2);
        ctx.quadraticCurveTo(fx, fy - 7, fx + 2, fy - 7);
        ctx.moveTo(fx + 3, fy - 2);
        ctx.quadraticCurveTo(fx, fy - 7, fx + 2, fy - 7);
        ctx.stroke();

        ctx.restore();
    }

    function drawHUD() {
        if (!gameState || !gameState.player) return;

        let score = gameState.player.score !== undefined ? gameState.player.score : gameState.player.Score;
        let lives = gameState.player.lives !== undefined ? gameState.player.lives : gameState.player.Lives;

        ctx.fillStyle = "rgba(0, 0, 0, 0.6)";
        ctx.fillRect(0, 0, canvas.width, 26);

        ctx.font = "bold 14px monospace";
        ctx.fillStyle = "#ffb703";
        ctx.fillText(`SCORE: ${score.toString().padStart(5, '0')}`, 10, 18);

        ctx.fillStyle = "#e0e1dd";
        ctx.fillText(`VIDAS:`, canvas.width - 110, 18);

        for (let i = 0; i < lives; i++) {
            let lx = canvas.width - 50 + (i * 16);
            let ly = 13;

            ctx.beginPath();
            ctx.fillStyle = "yellow";
            ctx.arc(lx, ly, 6, 0.2 * Math.PI, 1.8 * Math.PI);
            ctx.lineTo(lx, ly);
            ctx.fill();
        }
    }

    function drawPlayer() {
        if (!gameState || !gameState.player) return;

        let pxVal = gameState.player.x !== undefined ? gameState.player.x : gameState.player.X;
        let pyVal = gameState.player.y !== undefined ? gameState.player.y : gameState.player.Y;

        let px = pxVal * tileSize + tileSize / 2;
        let py = pyVal * tileSize + tileSize / 2;

        mouthAngle += mouthSpeed;
        if (mouthAngle > 0.35 || mouthAngle < 0.03) {
            mouthSpeed = -mouthSpeed;
        }

        let rotationAngle = 0;
        if (currentDirection === "Down") rotationAngle = Math.PI / 2;
        if (currentDirection === "Left") rotationAngle = Math.PI;
        if (currentDirection === "Up") rotationAngle = (3 * Math.PI) / 2;

        ctx.save();
        ctx.translate(px, py);
        ctx.rotate(rotationAngle);

        ctx.beginPath();
        ctx.arc(0, 0, 12, mouthAngle * Math.PI, (2 - mouthAngle) * Math.PI);
        ctx.lineTo(0, 0);
        ctx.fillStyle = "#ffe600";
        ctx.fill();

        ctx.restore();
    }

    function drawGhosts() {
        if (!gameState || !gameState.ghosts) return;

        const defaultColors = ["#ff0000", "#ffb8ff", "#00ffff", "#ffb852", "#a020f0", "#00ff00"];

        gameState.ghosts.forEach((ghost, index) => {
            let gxVal = ghost.x !== undefined ? ghost.x : ghost.X;
            let gyVal = ghost.y !== undefined ? ghost.y : ghost.Y;

            let x = Math.floor(gxVal * tileSize + tileSize / 2);
            let y = Math.floor(gyVal * tileSize + tileSize / 2);
            let r = 11;

            let ghostColor = ghost.color || ghost.originalColor || defaultColors[index % defaultColors.length];

            let isFrightened = ghost.state === 1 || ghost.state === "Frightened" ||
                ghost.isFrightened || ghost.IsFrightened;

            if (isFrightened) {
                ghostColor = "#0000FF";
            }

            ctx.save();
            ctx.shadowBlur = 0;

            // Corpo
            ctx.beginPath();
            ctx.arc(x, y - 2, r, Math.PI, 0, false);
            ctx.lineTo(x + r, y + r - 2);
            ctx.quadraticCurveTo(x + r * 0.6, y + r + 3, x + r * 0.3, y + r - 2);
            ctx.quadraticCurveTo(x, y + r + 3, x - r * 0.3, y + r - 2);
            ctx.quadraticCurveTo(x - r * 0.6, y + r + 3, x - r, y + r - 2);
            ctx.lineTo(x - r, y - 2);
            ctx.closePath();

            ctx.fillStyle = ghostColor;
            ctx.fill();

            // Olhos
            if (!isFrightened) {
                ctx.fillStyle = "#ffffff";
                ctx.beginPath();
                ctx.arc(x - 4, y - 3, 3.5, 0, Math.PI * 2);
                ctx.arc(x + 4, y - 3, 3.5, 0, Math.PI * 2);
                ctx.fill();

                ctx.fillStyle = "#0000d1";
                ctx.beginPath();
                ctx.arc(x - 3, y - 3, 1.8, 0, Math.PI * 2);
                ctx.arc(x + 5, y - 3, 1.8, 0, Math.PI * 2);
                ctx.fill();
            } else {
                ctx.fillStyle = "#ffb852";
                ctx.beginPath();
                ctx.arc(x - 4, y - 2, 2, 0, Math.PI * 2);
                ctx.arc(x + 4, y - 2, 2, 0, Math.PI * 2);
                ctx.fill();
            }

            ctx.restore();
        });
    }

    function checkGameEnd() {
        if (!gameState) return false;

        let status = gameState.status !== undefined ? gameState.status : gameState.Status;

        if (status === "GameOver" || status === 2 || status === "gameOver") {
            ctx.fillStyle = "rgba(0, 0, 0, 0.85)";
            ctx.fillRect(0, 0, canvas.width, canvas.height);

            ctx.fillStyle = "#ff4d4d";
            ctx.font = "bold 28px monospace";
            ctx.textAlign = "center";
            ctx.fillText("GAME OVER", canvas.width / 2, canvas.height / 2 - 10);

            ctx.fillStyle = "#ffffff";
            ctx.font = "14px monospace";
            ctx.fillText("Pressione Enter ou Espaço para reiniciar", canvas.width / 2, canvas.height / 2 + 30);

            ctx.textAlign = "start";
            return true;
        }

        if (status === "Victory" || status === 3 || status === "victory") {
            ctx.fillStyle = "rgba(0, 0, 0, 0.85)";
            ctx.fillRect(0, 0, canvas.width, canvas.height);

            ctx.fillStyle = "#2ec4b6";
            ctx.font = "bold 28px monospace";
            ctx.textAlign = "center";
            ctx.fillText("VOCÊ VENCEU! 🎉", canvas.width / 2, canvas.height / 2 - 10);

            ctx.fillStyle = "#ffffff";
            ctx.font = "14px monospace";
            ctx.fillText("Pressione Enter ou Espaço para jogar novamente", canvas.width / 2, canvas.height / 2 + 30);

            ctx.textAlign = "start";
            return true;
        }

        return false;
    }

    // --- INPUTS E CONTROLES ---

    document.addEventListener("keydown", async (e) => {
        if (["Space", "Enter", "ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight"].includes(e.code) ||
            [" ", "Enter"].includes(e.key)) {
            e.preventDefault();
        }

        let status = gameState ? (gameState.status !== undefined ? gameState.status : gameState.Status) : null;
        let isEnded = status === "GameOver" || status === 2 || status === "gameOver" ||
            status === "Victory" || status === 3 || status === "victory";

        if (isEnded && (e.key === "Enter" || e.key === " " || e.code === "Space" || e.code === "Enter")) {
            try {
                await fetch('/Game/NewGame', { method: 'POST' });
                gameState = null;
                await fetchGameState();
                requestAnimationFrame(renderLoop);
            } catch (err) {
                console.error("Erro ao reiniciar jogo:", err);
            }
            return;
        }

        if (isEnded) return;

        let direction = null;
        if (e.key === "ArrowUp" || e.code === "ArrowUp") direction = "Up";
        if (e.key === "ArrowDown" || e.code === "ArrowDown") direction = "Down";
        if (e.key === "ArrowLeft" || e.code === "ArrowLeft") direction = "Left";
        if (e.key === "ArrowRight" || e.code === "ArrowRight") direction = "Right";

        if (direction) {
            currentDirection = direction;

            try {
                await fetch('/Game/Move', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ direction: direction })
                });

                await fetchGameState();
            } catch (err) {
                console.error("Erro ao mover:", err);
            }
        }
    });

    // --- LOOP PRINCIPAL DE RENDERIZAÇÃO ---

    function renderLoop() {
        ctx.clearRect(0, 0, canvas.width, canvas.height);

        checkFruitEaten();

        drawMap();
        drawBonusFruit();
        drawPlayer();
        drawGhosts();
        drawHUD();

        drawFloatingTexts();

        if (checkGameEnd()) {
            return;
        }

        animationFrameId = requestAnimationFrame(renderLoop);
    }

    // --- INICIALIZAÇÃO E POLLING ---

    async function init() {
        await fetchGameState();
        renderLoop();
    }

    init();

    setInterval(async () => {
        let status = gameState ? (gameState.status !== undefined ? gameState.status : gameState.Status) : null;

        if (status !== "GameOver" && status !== 2 && status !== "gameOver" &&
            status !== "Victory" && status !== 3 && status !== "victory") {
            await fetchGameState();
        }
    }, 250);
});