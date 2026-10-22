// User-agent sniffing is fine here - this only decides which upload controls to show (a camera
// button vs. a plain file picker), never anything security- or correctness-sensitive.
export function isMobileDevice(): boolean {
    return /Android|iPhone|iPad|iPod|Mobile/i.test(navigator.userAgent);
}
