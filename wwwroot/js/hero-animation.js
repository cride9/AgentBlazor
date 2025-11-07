// This function will be called from the Blazor component after it has rendered.
function initHeroAnimation() {
    // --- Canvas Setup ---
    const canvas = document.getElementById("hero-canvas");

    // Safety Check: If the canvas element isn't on the page yet, stop the script.
    if (!canvas) {
        console.error("Hero canvas element not found. Animation cannot start.");
        return;
    }

    const ctx = canvas.getContext("2d", { alpha: true });

    // Use a second canvas for accumulation/trails
    const accum = document.createElement("canvas");
    const actx = accum.getContext("2d", { alpha: true });

    let W, H; // Will be set by resize function

    // --- Constants & Utilities ---
    const TAU = Math.PI * 2;
    const clamp = (v, a, b) => (v < a ? a : v > b ? b : v);
    const lerp = (a, b, t) => a + (b - a) * t;
    const rand = (min = 0, max = 1) => Math.random() * (max - min) + min;
    const hash = (x) => {
        let t = Math.sin(x * 9283.133) * 43758.5453;
        return t - Math.floor(t);
    };

    // --- Dynamic Parameters ---
    let PARTICLE_COUNT; // Set in resize
    const STEP = 1.4;
    const SPEED = 0.9;
    const LINE_ALPHA = 1.2;
    const ACCUM_FADE = 0.1; // Controls trail length
    const BG_FADE = 0.7;
    const BLOOM_BLUR_PX = 12; // Controls glow size
    const BLOOM_ALPHA = 0.8;
    const CRISP_ALPHA = 0.9;
    const HUE_SHIFT_SPEED = 6;
    const FIELD_FREQ_BASE = 0.0015;
    const FIELD_OCTAVES = 6;

    // --- Interaction State ---
    const pointer = {
        x: 0, y: 0,
        radius: 0,
        spin: 0.0,
        targetSpin: 0.0,
        active: false,
    };

    // Scroll-based speed control
    let scrollSpeedBoost = 0;
    const MAX_BOOST = 3.0;
    const SCROLL_BOOST_AMOUNT = 2.5;
    const BOOST_DECAY = 0.97;

    // --- Event Handlers ---
    window.addEventListener('scroll', () => {
        scrollSpeedBoost = Math.min(scrollSpeedBoost + SCROLL_BOOST_AMOUNT, MAX_BOOST);
    });

    canvas.addEventListener("mousemove", (e) => {
        const rect = canvas.getBoundingClientRect();
        pointer.x = (e.clientX - rect.left) * (W / rect.width);
        pointer.y = (e.clientY - rect.top) * (H / rect.height);
        pointer.active = true;
    });

    canvas.addEventListener("mouseleave", () => { pointer.active = false; });
    canvas.addEventListener("mousedown", () => { pointer.targetSpin = rand(0.8, 1.4) * (Math.random() < 0.5 ? -1 : 1); });
    canvas.addEventListener("mouseup", () => { pointer.targetSpin = 0.0; });

    // --- Procedural Field ---
    function fbm(x, y, t) {
        let v = 0;
        let amp = 0.5;
        let freq = FIELD_FREQ_BASE;
        for (let i = 0; i < FIELD_OCTAVES; i++) {
            const s = Math.sin(x * freq + t * (0.1 + i * 0.03) + i * 11.2);
            const c = Math.cos(y * freq - t * (0.13 + i * 0.02) - i * 7.7);
            const s2 = Math.sin((x + y) * freq * 0.7 + t * (0.07 + i * 0.02) + i * 5.3);
            v += amp * (s * c + 0.5 * s2);
            amp *= 0.55;
            freq *= 1.9;
        }
        return clamp(v, -1, 1);
    }

    function fieldAngle(x, y, t) {
        const a = fbm(x, y, t);
        const b = fbm(y + 913.7, x + 122.4, t + 47.1);
        return (a * 1.8 + b * 1.2) * Math.PI;
    }

    // --- Particles ---
    class Particle {
        constructor(i) { this.reset(i); }

        reset(i = this.i) {
            this.i = i;
            this.x = rand(0, W);
            this.y = rand(0, H);
            this.px = this.x;
            this.py = this.y;
            this.life = 0;
            this.seed = hash(i + rand(0, 1000)) * 1000.0;
            this.speed = SPEED * lerp(0.6, 1.4, hash(this.seed + 17.3));
            this.width = lerp(0.45, 1.6, hash(this.seed + 71.9));
        }

        step(t, dt) {
            const a = fieldAngle(this.x, this.y, t * 0.001);
            let vx, vy;

            if (pointer.active || Math.abs(pointer.spin) > 0.001) {
                const dx = this.x - pointer.x;
                const dy = this.y - pointer.y;
                const r2 = dx * dx + dy * dy;
                const s = Math.exp(-r2 / (pointer.radius * pointer.radius));
                const swirl = s * pointer.spin * 0.9;
                const orbit = Math.atan2(dy, dx) + Math.PI * 0.5;
                const mixAngle = a + swirl + 0.25 * s * Math.sin(orbit);
                vx = Math.cos(mixAngle);
                vy = Math.sin(mixAngle);
            } else {
                vx = Math.cos(a);
                vy = Math.sin(a);
            }

            const sp = this.speed * (STEP + 0.6 * fbm(this.y + 100.0, this.x - 200.0, t * 0.0007) + 0.3);
            const nx = this.x + vx * sp * dt;
            const ny = this.y + vy * sp * dt;

            actx.lineWidth = this.width;
            const baseHue = ((a * 180) / Math.PI + t * 0.001 * HUE_SHIFT_SPEED + this.seed * 0.1) % 360;
            actx.strokeStyle = `hsla(${(baseHue + 360) % 360}, 70%, 50%, ${LINE_ALPHA})`;
            actx.beginPath();
            actx.moveTo(this.x, this.y);
            actx.lineTo(nx, ny);
            actx.stroke();

            this.x = nx;
            this.y = ny;
            this.life += dt;

            const m = 4;
            if (this.x < -m || this.x > W + m || this.y < -m || this.y > H + m) {
                this.reset();
            }
        }
    }

    let particles = [];

    // --- Drawing Functions ---
    function paintBackdrop(opacity = 1.0) {
        const grd = ctx.createRadialGradient(
            W * 0.5, H * 0.5, Math.min(W, H) * 0.1,
            W * 0.5, H * 0.5, Math.max(W, H) * 0.8
        );
        grd.addColorStop(0.0, `rgba(8,10,30,${0.95 * opacity})`);
        grd.addColorStop(0.5, `rgba(5,7,24,${0.96 * opacity})`);
        grd.addColorStop(1.0, `rgba(1,2,10,${1.0 * opacity})`);
        ctx.fillStyle = grd;
        ctx.fillRect(0, 0, W, H);
    }

    function paintVignette() {
        const v = ctx.createRadialGradient(
            W * 0.5, H * 0.5, Math.min(W, H) * 0.45,
            W * 0.5, H * 0.5, Math.max(W, H) * 0.65
        );
        v.addColorStop(0.0, "rgba(0,0,0,0.0)");
        v.addColorStop(1.0, "rgba(0,0,0,0.35)");
        ctx.globalCompositeOperation = "multiply";
        ctx.fillStyle = v;
        ctx.fillRect(0, 0, W, H);
        ctx.globalCompositeOperation = "source-over";
    }

    // --- Animation Loop ---
    let last = performance.now();
    function frame(now) {
        const dt = Math.min(0.05, (now - last) / 16.6667);
        last = now;

        const effectiveDt = dt * (1 + scrollSpeedBoost);
        scrollSpeedBoost *= BOOST_DECAY;
        if (scrollSpeedBoost < 0.01) scrollSpeedBoost = 0;

        pointer.spin = lerp(pointer.spin, pointer.targetSpin, 0.06);

        actx.globalCompositeOperation = "source-over";
        actx.fillStyle = `rgba(0,0,0,${ACCUM_FADE})`;
        actx.fillRect(0, 0, W, H);
        actx.globalCompositeOperation = "lighter";

        for (let i = 0; i < PARTICLE_COUNT; i++) {
            particles[i].step(now, effectiveDt);
        }

        ctx.globalCompositeOperation = "source-over";
        ctx.fillStyle = `rgba(2,4,12,${BG_FADE})`;
        ctx.fillRect(0, 0, W, H);
        ctx.globalAlpha = 0.08;
        paintBackdrop(0.5);
        ctx.globalAlpha = 1.0;

        ctx.save();
        ctx.filter = `blur(${BLOOM_BLUR_PX}px) saturate(130%) brightness(120%)`;
        ctx.globalCompositeOperation = "lighter";
        ctx.globalAlpha = BLOOM_ALPHA;
        ctx.drawImage(accum, 0, 0);
        ctx.restore();

        ctx.save();
        ctx.globalCompositeOperation = "lighter";
        ctx.globalAlpha = CRISP_ALPHA;
        ctx.drawImage(accum, 0, 0);
        ctx.restore();

        paintVignette();

        // The original `requestAnimationFrame(frame);` was removed from here
        // because we will control the loop from outside this function.
    }

    // --- Resize and Setup Function ---
    function setup() {
        W = canvas.width = window.innerWidth;
        H = canvas.height = window.innerHeight;
        accum.width = W;
        accum.height = H;

        pointer.x = W * 0.5;
        pointer.y = H * 0.5;
        pointer.radius = Math.min(W, H) * 0.25;

        PARTICLE_COUNT = Math.floor(W * H * 0.0007); // Reduced particle count slightly for performance

        particles = new Array(PARTICLE_COUNT);
        for (let i = 0; i < PARTICLE_COUNT; i++) {
            particles[i] = new Particle(i);
        }

        actx.lineCap = "round";
        actx.lineJoin = "round";
        actx.globalCompositeOperation = "screen";
        actx.clearRect(0, 0, W, H);

        ctx.clearRect(0, 0, W, H);
        paintBackdrop(1.0);
    }

    // --- Kickoff ---

    // Set up the resize event listener
    window.addEventListener('resize', setup);

    // Run the setup function for the first time
    setup();

    // This is the new, robust animation loop starter.
    // It prevents multiple animation loops from running if the component re-renders.
    let animationFrameId;
    function animationLoop(now) {
        frame(now);
        animationFrameId = requestAnimationFrame(animationLoop);
    }

    // Cancel any previous animation frame to be safe
    if (window.heroAnimationId) {
        cancelAnimationFrame(window.heroAnimationId);
    }

    // Start the animation loop and store its ID on the window object
    animationLoop(performance.now());
    window.heroAnimationId = animationFrameId;
}