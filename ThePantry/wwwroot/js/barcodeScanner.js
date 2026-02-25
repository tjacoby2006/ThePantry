
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
                        
                        // Visual feedback
                        const element = document.getElementById(elementId);
                        element.style.border = "5px solid #28a745";
                        setTimeout(() => element.style.border = "none", 500);

                        // Audio feedback (optional, but good for speed)
                        try {
                            const audio = new Audio('https://assets.mixkit.co/active_storage/sfx/2571/2571-preview.mp3');
                            audio.play();
                        } catch (e) { console.error("Audio play failed", e); }

                        dotNetHelper.invokeMethodAsync('HandleBarcodeDetected', decodedText);
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
