document.addEventListener("DOMContentLoaded", () => {
    const canvas = document.getElementById("gameCanvas");
    if (!canvas) return;

    const ctx = canvas.getContext("2d");
    const tileSize = 30;

    let gameState = null;
    let currentDirection = "Right";
    let mouthAngle = 0.2;
    let mouthSpeed = 0.015;
    let pulseTime = 0;
    let isFetching = false;
    let animationFrameId = null;
    let introTimerId = null;

    let floatingTexts = [];
    let particles = [];
    let lastFruitState = false;
    let previousLives = null;

    // --- RASTREAMENTO E ESTADOS DE JOGO ---
    const collectedCoinIds = new Set();
    const collectedPelletIds = new Set();

    let isGameStarted = false;
    let isIntroPlaying = false;

    // Controle da animação de Morte
    let isDying = false;
    let deathProgress = 0;
    let deathCallback = null;
    let deathX = 0;
    let deathY = 0;

    // Tabela de Frutas
    const FRUITS = [
        { name: "Cereja", score: 100, color: "#ff0000", stemColor: "#00ff00" },
        { name: "Morango", score: 300, color: "#ff3333", stemColor: "#00cc00" },
        { name: "Laranja", score: 500, color: "#ffa500", stemColor: "#009900" },
        { name: "Maçã", score: 700, color: "#cc0000", stemColor: "#006600" },
        { name: "Melancia", score: 1000, color: "#009933", stemColor: "#ff3366" }
    ];

    // --- SISTEMA DE ÁUDIO (SINTETIZADOR CLÁSSICO 8-BIT) ---
    const AudioContext = window.AudioContext || window.webkitAudioContext;
    let audioCtx = null;
    let wakaToggle = false;

    function initAudio() {
        if (!audioCtx) {
            audioCtx = new AudioContext();
        }
        if (audioCtx.state === 'suspended') {
            audioCtx.resume();
        }
    }

    const NOTES = {
        B4: 493.88, C5: 523.25, Csharp5: 554.37, D5: 587.33,
        Dsharp5: 622.25, E5: 659.25, F5: 698.46, Fsharp5: 739.99, G5: 783.99,
        Gsharp5: 830.61, A5: 880.00, B5: 987.77, C6: 1046.50
    };

    const INTRO_MELODY = [
        { note: NOTES.B4, duration: 0.12 },
        { note: NOTES.B5, duration: 0.12 },
        { note: NOTES.Fsharp5, duration: 0.12 },
        { note: NOTES.Dsharp5, duration: 0.12 },
        { note: NOTES.B5, duration: 0.06 },
        { note: NOTES.Fsharp5, duration: 0.18 },
        { note: NOTES.Dsharp5, duration: 0.24 },

        { note: NOTES.C5, duration: 0.12 },
        { note: NOTES.C6, duration: 0.12 },
        { note: NOTES.G5, duration: 0.12 },
        { note: NOTES.E5, duration: 0.12 },
        { note: NOTES.C6, duration: 0.06 },
        { note: NOTES.G5, duration: 0.18 },
        { note: NOTES.E5, duration: 0.24 },

        { note: NOTES.B4, duration: 0.12 },
        { note: NOTES.B5, duration: 0.12 },
        { note: NOTES.Fsharp5, duration: 0.12 },
        { note: NOTES.Dsharp5, duration: 0.12 },
        { note: NOTES.B5, duration: 0.06 },
        { note: NOTES.Fsharp5, duration: 0.18 },
        { note: NOTES.Dsharp5, duration: 0.24 },

        { note: NOTES.Dsharp5, duration: 0.06 },
        { note: NOTES.E5, duration: 0.06 },
        { note: NOTES.F5, duration: 0.06 },
        { note: NOTES.Fsharp5, duration: 0.12 },
        { note: NOTES.G5, duration: 0.06 },
        { note: NOTES.Gsharp5, duration: 0.06 },
        { note: NOTES.A5, duration: 0.06 },
        { note: NOTES.B5, duration: 0.30 }
    ];

    function stopIntroTheme() {
        if (introTimerId) {
            clearTimeout(introTimerId);
            introTimerId = null;
        }
    }

    function playIntroTheme(onComplete) {
        initAudio();
        if (!audioCtx) return;

        stopIntroTheme();

        let time = audioCtx.currentTime + 0.05;

        INTRO_MELODY.forEach(step => {
            const osc = audioCtx.createOscillator();
            const gain = audioCtx.createGain();

            osc.type = "square";
            osc.frequency.setValueAtTime(step.note, time);

            gain.gain.setValueAtTime(0.12, time);
            gain.gain.exponentialRampToValueAtTime(0.001, time + step.duration - 0.01);

            osc.connect(gain);
            gain.connect(audioCtx.destination);

            osc.start(time);
            osc.stop(time + step.duration);

            time += step.duration;
        });

        if (onComplete) {
            const delay = Math.max(0, (time - audioCtx.currentTime) * 1000);
            introTimerId = setTimeout(() => {
                introTimerId = null;
                onComplete();
            }, delay);
        }
    }

    // --- EFEITOS SONOROS DE JOGO ---
    function playWakaSound() {
        initAudio();
        if (!audioCtx) return;

        const osc = audioCtx.createOscillator();
        const gain = audioCtx.createGain();

        osc.type = "triangle";
        const now = audioCtx.currentTime;
        const duration = 0.07;

        if (wakaToggle) {
            osc.frequency.setValueAtTime(140, now);
            osc.frequency.exponentialRampToValueAtTime(440, now + duration);
        } else {
            osc.frequency.setValueAtTime(440, now);
            osc.frequency.exponentialRampToValueAtTime(140, now + duration);
        }

        wakaToggle = !wakaToggle;

        gain.gain.setValueAtTime(0.18, now);
        gain.gain.exponentialRampToValueAtTime(0.001, now + duration);

        osc.connect(gain);
        gain.connect(audioCtx.destination);

        osc.start(now);
        osc.stop(now + duration);
    }

    function playBonusSound() {
        initAudio();
        if (!audioCtx) return;

        const osc = audioCtx.createOscillator();
        const gain = audioCtx.createGain();

        osc.type = "sine";
        const now = audioCtx.currentTime;

        osc.frequency.setValueAtTime(300, now);
        osc.frequency.exponentialRampToValueAtTime(1200, now + 0.25);

        gain.gain.setValueAtTime(0.25, now);
        gain.gain.exponentialRampToValueAtTime(0.01, now + 0.25);

        osc.connect(gain);
        gain.connect(audioCtx.destination);

        osc.start(now);
        osc.stop(now + 0.25);
    }

    function playPowerPelletSound() {
        initAudio();
        if (!audioCtx) return;

        const osc = audioCtx.createOscillator();
        const gain = audioCtx.createGain();

        osc.type = "sawtooth";
        const now = audioCtx.currentTime;

        osc.frequency.setValueAtTime(150, now);
        osc.frequency.exponentialRampToValueAtTime(600, now + 0.18);

        gain.gain.setValueAtTime(0.15, now);
        gain.gain.exponentialRampToValueAtTime(0.01, now + 0.18);

        osc.connect(gain);
        gain.connect(audioCtx.destination);

        osc.start(now);
        osc.stop(now + 0.18);
    }

    function playDeathSound() {
        initAudio();
        if (!audioCtx) return;

        const osc = audioCtx.createOscillator();
        const gain = audioCtx.createGain();

        osc.type = "sawtooth";
        const now = audioCtx.currentTime;
        const duration = 0.8;

        osc.frequency.setValueAtTime(500, now);
        osc.frequency.exponentialRampToValueAtTime(40, now + duration);

        gain.gain.setValueAtTime(0.2, now);
        gain.gain.exponentialRampToValueAtTime(0.001, now + duration);

        osc.connect(gain);
        gain.connect(audioCtx.destination);

        osc.start(now);
        osc.stop(now + duration);
    }

    // --- SISTEMA DE PARTÍCULAS E TEXTOS FLUTUANTES ---
    function addParticles(x, y, color = "#ffb703", count = 8) {
        for (let i = 0; i < count; i++) {
            const angle = Math.random() * Math.PI * 2;
            const speed = Math.random() * 2 + 1;
            particles.push({
                x: x,
                y: y,
                vx: Math.cos(angle) * speed,
                vy: Math.sin(angle) * speed,
                alpha: 1.0,
                color: color,
                size: Math.random() * 3 + 2
            });
        }
    }

    function drawParticles() {
        for (let i = particles.length - 1; i >= 0; i--) {
            let p = particles[i];

            ctx.save();
            ctx.globalAlpha = p.alpha;
            ctx.fillStyle = p.color;
            ctx.beginPath();
            ctx.arc(p.x, p.y, p.size, 0, Math.PI * 2);
            ctx.fill();
            ctx.restore();

            p.x += p.vx;
            p.y += p.vy;
            p.alpha -= 0.04;

            if (p.alpha <= 0) {
                particles.splice(i, 1);
            }
        }
    }

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

        if (lastFruitState && !isActive) {
            let fruitInfo = FRUITS[type] || FRUITS[0];
            let px = bx * tileSize + tileSize / 2;
            let py = by * tileSize + tileSize / 2;

            addFloatingText(`+${fruitInfo.score}`, bx, by, "#00ffcc");
            addParticles(px, py, fruitInfo.color, 12);
            playBonusSound();
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
                await fetch('/Game/NewGame', { method: 'POST' });
                response = await fetch('/Game/State');
            }

            if (response.ok) {
                const data = await response.json();

                if (data.width && data.height) {
                    const targetWidth = data.width * tileSize;
                    const targetHeight = data.height * tileSize;
                    if (canvas.width !== targetWidth) canvas.width = targetWidth;
                    if (canvas.height !== targetHeight) canvas.height = targetHeight;
                }

                if (data.player) {
                    let currentLives = data.player.lives !== undefined ? data.player.lives : data.player.Lives;
                    if (previousLives !== null && currentLives < previousLives && currentLives >= 0) {
                        triggerPlayerDeath();
                    }
                    previousLives = currentLives;
                }

                gameState = data;
            }
        } catch (error) {
            console.error("Erro ao carregar o estado do jogo:", error);
        } finally {
            isFetching = false;
        }
    }

    // --- FUNÇÕES DE DESENHO ---
    function drawMap() {
        if (!gameState) return;

        pulseTime += 0.05;

        if (gameState.tiles) {
            gameState.tiles.forEach(tile => {
                let x = (tile.x !== undefined ? tile.x : tile.X) * tileSize;
                let y = (tile.y !== undefined ? tile.y : tile.Y) * tileSize;
                let type = tile.type !== undefined ? tile.type : tile.Type;

                if (type === 1 || type === "Wall" || type === "wall") {
                    ctx.save();
                    ctx.fillStyle = "#091322";
                    ctx.beginPath();
                    if (ctx.roundRect) {
                        ctx.roundRect(x, y, tileSize, tileSize, 4);
                    } else {
                        ctx.rect(x, y, tileSize, tileSize);
                    }
                    ctx.fill();

                    ctx.strokeStyle = "#1e90ff";
                    ctx.lineWidth = 2;
                    ctx.shadowColor = "#1e90ff";
                    ctx.shadowBlur = 4;
                    ctx.stroke();
                    ctx.restore();
                }
            });
        }

        if (gameState.coins) {
            gameState.coins.forEach((coin, index) => {
                let isCollected = coin.collected !== undefined ? coin.collected : coin.Collected;
                let coinId = coin.id !== undefined ? coin.id : index;

                let cx = (coin.x !== undefined ? coin.x : coin.X) * tileSize + tileSize / 2;
                let cy = (coin.y !== undefined ? coin.y : coin.Y) * tileSize + tileSize / 2;

                if (isCollected && !collectedCoinIds.has(coinId)) {
                    collectedCoinIds.add(coinId);
                    if (isGameStarted) {
                        playWakaSound();
                        addParticles(cx, cy, "#ffb703", 5);
                    }
                }

                if (!isCollected) {
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

        if (gameState.pellets) {
            gameState.pellets.forEach((pellet, index) => {
                let isCollected = pellet.collected !== undefined ? pellet.collected : pellet.Collected;
                let pelletId = pellet.id !== undefined ? pellet.id : index;

                let px = (pellet.x !== undefined ? pellet.x : pellet.X) * tileSize + tileSize / 2;
                let py = (pellet.y !== undefined ? pellet.y : pellet.Y) * tileSize + tileSize / 2;

                if (isCollected && !collectedPelletIds.has(pelletId)) {
                    collectedPelletIds.add(pelletId);
                    if (isGameStarted) {
                        playPowerPelletSound();
                        addParticles(px, py, "#ffffff", 10);
                    }
                }

                if (!isCollected) {
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

        ctx.fillStyle = "rgba(0, 0, 0, 0.75)";
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

    function triggerPlayerDeath(onDeathComplete) {
        if (!gameState || !gameState.player) return;

        deathX = gameState.player.x !== undefined ? gameState.player.x : gameState.player.X;
        deathY = gameState.player.y !== undefined ? gameState.player.y : gameState.player.Y;

        isDying = true;
        deathProgress = 0;
        deathCallback = onDeathComplete || null;
        playDeathSound();
    }

    function drawPlayer() {
        if (!gameState || !gameState.player) return;

        let pxVal = isDying ? deathX : (gameState.player.x !== undefined ? gameState.player.x : gameState.player.X);
        let pyVal = isDying ? deathY : (gameState.player.y !== undefined ? gameState.player.y : gameState.player.Y);

        let px = pxVal * tileSize + tileSize / 2;
        let py = pyVal * tileSize + tileSize / 2;

        ctx.save();
        ctx.translate(px, py);

        // 1. ANIMAÇÃO DE MORTE
        if (isDying) {
            let deathAngle = deathProgress * Math.PI;

            ctx.beginPath();
            ctx.arc(0, 0, 12, deathAngle, Math.PI * 2 - deathAngle);
            ctx.lineTo(0, 0);
            ctx.fillStyle = "#ffe600";
            ctx.fill();
            ctx.restore();

            deathProgress += 0.025;
            if (deathProgress >= 1) {
                isDying = false;
                if (deathCallback) deathCallback();
            }
            return;
        }

        // 2. DESENHO NORMAL DO PAC-MAN (Boca animada)
        mouthAngle += mouthSpeed;
        if (mouthAngle > 0.35 || mouthAngle < 0.05) {
            mouthSpeed = -mouthSpeed;
        }

        let rotation = 0;
        if (currentDirection === "Up") rotation = -Math.PI / 2;
        if (currentDirection === "Down") rotation = Math.PI / 2;
        if (currentDirection === "Left") rotation = Math.PI;

        ctx.rotate(rotation);

        ctx.shadowColor = "#ffe600";
        ctx.shadowBlur = 6;

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
                let isFlashing = Math.floor(pulseTime * 6) % 2 === 0;
                ghostColor = isFlashing ? "#ffffff" : "#0000FF";
            }

            ctx.save();
            ctx.shadowBlur = 0;

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

            // Direção dos olhos
            let eyeOffsetX = 0;
            let eyeOffsetY = 0;

            let dir = ghost.direction || ghost.Direction;
            if (dir === "Left") eyeOffsetX = -2;
            if (dir === "Right") eyeOffsetX = 2;
            if (dir === "Up") eyeOffsetY = -2;
            if (dir === "Down") eyeOffsetY = 2;

            if (!isFrightened) {
                ctx.fillStyle = "#ffffff";
                ctx.beginPath();
                ctx.arc(x - 4 + eyeOffsetX, y - 3 + eyeOffsetY, 3.5, 0, Math.PI * 2);
                ctx.arc(x + 4 + eyeOffsetX, y - 3 + eyeOffsetY, 3.5, 0, Math.PI * 2);
                ctx.fill();

                ctx.fillStyle = "#0000d1";
                ctx.beginPath();
                ctx.arc(x - 4 + eyeOffsetX * 1.5, y - 3 + eyeOffsetY * 1.5, 1.8, 0, Math.PI * 2);
                ctx.arc(x + 4 + eyeOffsetX * 1.5, y - 3 + eyeOffsetY * 1.5, 1.8, 0, Math.PI * 2);
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

    function drawStartOverlay() {
        ctx.fillStyle = "rgba(0, 0, 0, 0.85)";
        ctx.fillRect(0, 0, canvas.width, canvas.height);

        ctx.textAlign = "center";
        pulseTime += 0.08;

        if (isIntroPlaying) {
            const showReady = Math.floor(pulseTime * 4) % 2 === 0;

            if (showReady) {
                ctx.fillStyle = "#ffe600";
                ctx.font = "bold 28px monospace";
                ctx.shadowColor = "#ffe600";
                ctx.shadowBlur = 10;
                ctx.fillText("READY!", canvas.width / 2, canvas.height / 2);
                ctx.shadowBlur = 0;
            }
        } else {
            ctx.fillStyle = "#ffe600";
            ctx.font = "bold 26px monospace";
            ctx.shadowColor = "#ffe600";
            ctx.shadowBlur = 8;
            ctx.fillText("PAC-MAN", canvas.width / 2, canvas.height / 2 - 30);
            ctx.shadowBlur = 0;

            const textAlpha = Math.abs(Math.sin(pulseTime * 2));
            ctx.save();
            ctx.globalAlpha = textAlpha;
            ctx.fillStyle = "#ffffff";
            ctx.font = "14px monospace";
            ctx.fillText("PRESSIONE ENTER, ESPAÇO OU TOQUE", canvas.width / 2, canvas.height / 2 + 15);
            ctx.fillText("PARA INICIAR", canvas.width / 2, canvas.height / 2 + 35);
            ctx.restore();
        }

        ctx.textAlign = "start";
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

    // --- CONTROLES (TECLADO + WASD + TOUCH/SWIPE) ---
    async function sendMoveRequest(direction) {
        if (!direction || isDying || !isGameStarted) return;
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

    async function handleStartOrRestart() {
        initAudio();

        if (!isGameStarted && !isIntroPlaying) {
            isIntroPlaying = true;
            playIntroTheme(() => {
                isIntroPlaying = false;
                isGameStarted = true;
            });
            return;
        }

        let status = gameState ? (gameState.status !== undefined ? gameState.status : gameState.Status) : null;
        let isEnded = status === "GameOver" || status === 2 || status === "gameOver" ||
            status === "Victory" || status === 3 || status === "victory";

        if (isEnded) {
            try {
                await fetch('/Game/NewGame', { method: 'POST' });
                gameState = null;
                previousLives = null;
                collectedCoinIds.clear();
                collectedPelletIds.clear();
                floatingTexts = [];
                particles = [];
                isGameStarted = false;
                isIntroPlaying = true;

                await fetchGameState();

                playIntroTheme(() => {
                    isIntroPlaying = false;
                    isGameStarted = true;
                });

                startRenderLoop();
            } catch (err) {
                console.error("Erro ao reiniciar jogo:", err);
            }
        }
    }

    document.addEventListener("keydown", async (e) => {
        initAudio();

        if (["Space", "Enter", "ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight", "KeyW", "KeyA", "KeyS", "KeyD"].includes(e.code) ||
            [" ", "Enter"].includes(e.key)) {
            e.preventDefault();
        }

        if (e.key === "Enter" || e.key === " " || e.code === "Space" || e.code === "Enter") {
            await handleStartOrRestart();
            return;
        }

        let direction = null;
        if (e.key === "ArrowUp" || e.code === "KeyW") direction = "Up";
        if (e.key === "ArrowDown" || e.code === "KeyS") direction = "Down";
        if (e.key === "ArrowLeft" || e.code === "KeyA") direction = "Left";
        if (e.key === "ArrowRight" || e.code === "KeyD") direction = "Right";

        if (direction) {
            sendMoveRequest(direction);
        }
    });

    // --- CONTROLE POR GESTOS DE TOQUE (MOBILE) ---
    let touchStartX = 0;
    let touchStartY = 0;

    canvas.addEventListener("touchstart", (e) => {
        initAudio();
        if (e.touches.length > 0) {
            touchStartX = e.touches[0].clientX;
            touchStartY = e.touches[0].clientY;
        }
        if (!isGameStarted || checkGameEnd()) {
            handleStartOrRestart();
        }
    }, { passive: true });

    canvas.addEventListener("touchend", (e) => {
        if (!touchStartX || !touchStartY || !isGameStarted) return;

        let touchEndX = e.changedTouches[0].clientX;
        let touchEndY = e.changedTouches[0].clientY;

        let dx = touchEndX - touchStartX;
        let dy = touchEndY - touchStartY;

        if (Math.abs(dx) > 20 || Math.abs(dy) > 20) {
            if (Math.abs(dx) > Math.abs(dy)) {
                sendMoveRequest(dx > 0 ? "Right" : "Left");
            } else {
                sendMoveRequest(dy > 0 ? "Down" : "Up");
            }
        }

        touchStartX = 0;
        touchStartY = 0;
    }, { passive: true });

    // --- LOOP PRINCIPAL DE RENDERIZAÇÃO ---
    function renderLoop() {
        ctx.clearRect(0, 0, canvas.width, canvas.height);

        checkFruitEaten();

        drawMap();
        drawBonusFruit();
        drawPlayer();
        drawGhosts();
        drawHUD();

        drawParticles();
        drawFloatingTexts();

        if (!isGameStarted) {
            drawStartOverlay();
            animationFrameId = requestAnimationFrame(renderLoop);
            return;
        }

        if (checkGameEnd()) {
            return;
        }

        animationFrameId = requestAnimationFrame(renderLoop);
    }

    function startRenderLoop() {
        if (animationFrameId) {
            cancelAnimationFrame(animationFrameId);
        }
        animationFrameId = requestAnimationFrame(renderLoop);
    }

    // --- INICIALIZAÇÃO E POLLING (USANDO RECURSÃO CONTROLADA) ---
    async function pollingLoop() {
        if (isGameStarted && !isDying) {
            let status = gameState ? (gameState.status !== undefined ? gameState.status : gameState.Status) : null;

            if (status !== "GameOver" && status !== 2 && status !== "gameOver" &&
                status !== "Victory" && status !== 3 && status !== "victory") {
                await fetchGameState();
            }
        }
        setTimeout(pollingLoop, 250);
    }

    async function init() {
        await fetchGameState();
        startRenderLoop();
        pollingLoop();
    }

    init();
});