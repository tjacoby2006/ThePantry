
let html5QrCode;
let lastScannedCode = "";
let lastScannedTime = 0;
const COOLDOWN_MS = 2000;

window.barcodeScanner = {
    start: async function (dotNetHelper, elementId) {
        if (html5QrCode) {
            await this.stop();
        }

        html5QrCode = new Html5Qrcode(elementId);
        const config = { fps: 10, qrbox: { width: 250, height: 150 } };

        try {
            await html5QrCode.start(
                { facingMode: "environment" },
                config,
                (decodedText, decodedResult) => {
                    const now = Date.now();
                    if (decodedText !== lastScannedCode || (now - lastScannedTime) > COOLDOWN_MS) {
                        lastScannedCode = decodedText;
                        lastScannedTime = now;
                        
                        // Audio feedback
                        const playBeep = () => {
                            try {
                                const audioCtx = new (window.AudioContext || window.webkitAudioContext)();
                                const oscillator = audioCtx.createOscillator();
                                const gainNode = audioCtx.createGain();
                                oscillator.connect(gainNode);
                                gainNode.connect(audioCtx.destination);
                                oscillator.type = 'sine';
                                oscillator.frequency.setValueAtTime(880, audioCtx.currentTime);
                                gainNode.gain.setValueAtTime(0.1, audioCtx.currentTime);
                                gainNode.gain.exponentialRampToValueAtTime(0.01, audioCtx.currentTime + 0.1);
                                oscillator.start();
                                oscillator.stop(audioCtx.currentTime + 0.1);
                            } catch (e) { console.error("Web Audio fallback failed", e); }
                        };

                        try {
                            const audio = new Audio('/beep.mp3');
                            audio.play().catch(err => {
                                console.warn("MP3 play failed, using fallback", err);
                                playBeep();
                            });
                        } catch (e) { 
                            playBeep();
                        }

                        // Capture image from video stream
                        let imageData = null;
                        try {
                            const video = document.querySelector(`#${elementId} video`);
                            if (video) {
                                const canvas = document.createElement('canvas');
                                canvas.width = video.videoWidth;
                                canvas.height = video.videoHeight;
                                const ctx = canvas.getContext('2d');
                                ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
                                // Use lower quality and smaller size to reduce payload
                                imageData = canvas.toDataURL('image/jpeg', 0.3);
                            }
                        } catch (e) {
                            console.error("Failed to capture image", e);
                        }

                        console.log("Barcode detected:", decodedText);
                        dotNetHelper.invokeMethodAsync('HandleBarcodeDetected', decodedText, imageData)
                            .then(() => {
                                console.log("Successfully invoked HandleBarcodeDetected");
                                // Visual feedback
                                const element = document.getElementById(elementId);
                                if (element) {
                                    element.style.border = "5px solid #28a745";
                                    setTimeout(() => element.style.border = "none", 500);
                                }
                            })
                            .catch(err => {
                                console.error("Error invoking HandleBarcodeDetected:", err);
                                const element = document.getElementById(elementId);
                                if (element) {
                                    element.style.border = "5px solid #dc3545";
                                    setTimeout(() => element.style.border = "none", 500);
                                }
                            });
                    }
                },
                (errorMessage) => {
                    // parse error, ignore it.
                }
            );
        } catch (err) {
            console.error("Unable to start scanning", err);
            throw err;
        }
    },
    stop: async function () {
        if (html5QrCode && html5QrCode.isScanning) {
            await html5QrCode.stop();
            html5QrCode = null;
        }
    }
};
