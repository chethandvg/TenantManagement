// Signature Pad Implementation
let signaturePad = {
    canvas: null,
    ctx: null,
    isDrawing: false,
    lastX: 0,
    lastY: 0,
    dotNetRef: null
};

window.initSignaturePad = (dotNetReference) => {
    signaturePad.dotNetRef = dotNetReference;
    signaturePad.canvas = document.getElementById('signatureCanvas');
    
    if (!signaturePad.canvas) {
        console.error('Signature canvas not found');
        return;
    }
    
    signaturePad.ctx = signaturePad.canvas.getContext('2d');
    
    // Set up canvas for drawing
    signaturePad.ctx.strokeStyle = '#000000';
    signaturePad.ctx.lineWidth = 2;
    signaturePad.ctx.lineCap = 'round';
    signaturePad.ctx.lineJoin = 'round';
    
    // Mouse events
    signaturePad.canvas.addEventListener('mousedown', startDrawing);
    signaturePad.canvas.addEventListener('mousemove', draw);
    signaturePad.canvas.addEventListener('mouseup', stopDrawing);
    signaturePad.canvas.addEventListener('mouseout', stopDrawing);
    
    // Touch events for mobile
    signaturePad.canvas.addEventListener('touchstart', handleTouchStart, { passive: false });
    signaturePad.canvas.addEventListener('touchmove', handleTouchMove, { passive: false });
    signaturePad.canvas.addEventListener('touchend', stopDrawing);
};

function startDrawing(e) {
    signaturePad.isDrawing = true;
    const rect = signaturePad.canvas.getBoundingClientRect();
    signaturePad.lastX = e.clientX - rect.left;
    signaturePad.lastY = e.clientY - rect.top;
}

function draw(e) {
    if (!signaturePad.isDrawing) return;
    
    const rect = signaturePad.canvas.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;
    
    signaturePad.ctx.beginPath();
    signaturePad.ctx.moveTo(signaturePad.lastX, signaturePad.lastY);
    signaturePad.ctx.lineTo(x, y);
    signaturePad.ctx.stroke();
    
    signaturePad.lastX = x;
    signaturePad.lastY = y;
}

function stopDrawing() {
    if (signaturePad.isDrawing) {
        signaturePad.isDrawing = false;
        
        // Get signature data URL
        const dataUrl = signaturePad.canvas.toDataURL('image/png');
        
        // Send to Blazor component
        if (signaturePad.dotNetRef) {
            signaturePad.dotNetRef.invokeMethodAsync('SetSignatureData', dataUrl);
        }
    }
}

function handleTouchStart(e) {
    e.preventDefault();
    const touch = e.touches[0];
    const rect = signaturePad.canvas.getBoundingClientRect();
    
    signaturePad.isDrawing = true;
    signaturePad.lastX = touch.clientX - rect.left;
    signaturePad.lastY = touch.clientY - rect.top;
}

function handleTouchMove(e) {
    e.preventDefault();
    if (!signaturePad.isDrawing) return;
    
    const touch = e.touches[0];
    const rect = signaturePad.canvas.getBoundingClientRect();
    const x = touch.clientX - rect.left;
    const y = touch.clientY - rect.top;
    
    signaturePad.ctx.beginPath();
    signaturePad.ctx.moveTo(signaturePad.lastX, signaturePad.lastY);
    signaturePad.ctx.lineTo(x, y);
    signaturePad.ctx.stroke();
    
    signaturePad.lastX = x;
    signaturePad.lastY = y;
}

window.clearSignaturePad = () => {
    if (signaturePad.canvas && signaturePad.ctx) {
        signaturePad.ctx.clearRect(0, 0, signaturePad.canvas.width, signaturePad.canvas.height);
        
        // Notify Blazor component
        if (signaturePad.dotNetRef) {
            signaturePad.dotNetRef.invokeMethodAsync('SetSignatureData', '');
        }
    }
};
