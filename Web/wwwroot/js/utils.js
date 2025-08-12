window.getBoundingClientRect = function (element) {
    if (!element || typeof element.getBoundingClientRect !== 'function') {
        console.error('Invalid element passed to getBoundingClientRect');
        return null;
    }

    var rect = element.getBoundingClientRect();

    return {
        top: Math.round(rect.top),
        left: Math.round(rect.left),
        bottom: Math.round(rect.bottom),
        right: Math.round(rect.right),
        width: Math.round(rect.width),
        height: Math.round(rect.height)
    };
};

window.getTooltipSize = function () {
    var tooltip = document.querySelector('.tooltip');
    if (!tooltip) {
        console.warn('Tooltip not found');
        return { height: 0, width: 0 };
    }

    var rect = tooltip.getBoundingClientRect();
    return {
        height: Math.round(rect.height),
        width: Math.round(rect.width)
    };
};

window.getViewportSize = function () {
    return {
        height: window.innerHeight,
        width: window.innerWidth
    };
};

// Custom selector utilities
window.addOutsideClickListener = function (element, dotNetRef, methodName) {
    var handleOutsideClick = function (event) {
        if (element && !element.contains(event.target)) {
            dotNetRef.invokeMethodAsync(methodName);
        }
    };
    
    document.addEventListener('click', handleOutsideClick);
    return handleOutsideClick;
};

window.removeOutsideClickListener = function (handler) {
    if (handler) {
        document.removeEventListener('click', handler);
    }
};

// Enhanced window dragging with mouse-only handling (keyboard integration removed)
window.initMovableWindow = function(windowElement, titlebarElement) {
    console.log('initMovableWindow called with:', windowElement, titlebarElement);
    
    if (!windowElement || !titlebarElement) {
        console.error('Invalid elements for movable window');
        return null;
    }
    
    var isDragging = false;
    var dragStartX = 0;
    var dragStartY = 0;
    var windowStartX = 0;
    var windowStartY = 0;
    var animationFrame = null;
    var currentX = 0;
    var currentY = 0;
    
    console.log('Setting up movable window...');
    
    // Initialize window position
    var initializePosition = function () {
        var viewport = {
            width: window.innerWidth,
            height: window.innerHeight
        };
        
        var rect = windowElement.getBoundingClientRect();
        
        // Center the window initially if not positioned
        if (windowElement.style.left === '' && windowElement.style.top === '') {
            currentX = (viewport.width - rect.width) / 2;
            currentY = (viewport.height - rect.height) / 2;
            setWindowPosition(currentX, currentY);
        } else {
            currentX = rect.left;
            currentY = rect.top;
        }
        
        console.log('Window initialized at position:', currentX, currentY);
    };
    
    // Set window position with bounds checking
    var setWindowPosition = function (x, y) {
        var viewport = {
            width: window.innerWidth,
            height: window.innerHeight
        };
        
        var rect = windowElement.getBoundingClientRect();
        
        // Apply boundaries - keep some part of window visible
        var minVisibleWidth = Math.min(200, rect.width * 0.3);
        var minVisibleHeight = 50; // Keep at least titlebar visible
        
        var minX = -rect.width + minVisibleWidth;
        var maxX = viewport.width - minVisibleWidth;
        var minY = 0;
        var maxY = viewport.height - minVisibleHeight;
        
        x = Math.max(minX, Math.min(maxX, x));
        y = Math.max(minY, Math.min(maxY, y));
        
        // Update current position
        currentX = x;
        currentY = y;
        
        // Apply position
        windowElement.style.position = 'absolute';
        windowElement.style.left = x + 'px';
        windowElement.style.top = y + 'px';
        windowElement.style.transform = 'none';
        
        return { x: x, y: y };
    };
    
    // Get current window position
    var getWindowPosition = function () {
        return { x: currentX, y: currentY };
    };
    
    // Move window by delta amounts
    var moveWindow = function (deltaX, deltaY) {
        var newX = currentX + deltaX;
        var newY = currentY + deltaY;
        return setWindowPosition(newX, newY);
    };
    
    // Move window to specific position
    var moveWindowTo = function (x, y) {
        return setWindowPosition(x, y);
    };
    
    // Center window in viewport
    var centerWindow = function () {
        var viewport = {
            width: window.innerWidth,
            height: window.innerHeight
        };
        
        var rect = windowElement.getBoundingClientRect();
        var centerX = (viewport.width - rect.width) / 2;
        var centerY = (viewport.height - rect.height) / 2;
        
        return setWindowPosition(centerX, centerY);
    };
    
    // Animate window movement
    var animateWindowTo = function (targetX, targetY, duration = 300) {
        var startX = currentX;
        var startY = currentY;
        var deltaX = targetX - startX;
        var deltaY = targetY - startY;
        var startTime = performance.now();
        
        var animate = function (currentTime) {
            var elapsed = currentTime - startTime;
            var progress = Math.min(elapsed / duration, 1);
            
            // Easing function (ease-out)
            var easeOut = 1 - Math.pow(1 - progress, 3);
            
            var newX = startX + (deltaX * easeOut);
            var newY = startY + (deltaY * easeOut);
            
            setWindowPosition(newX, newY);
            
            if (progress < 1) {
                requestAnimationFrame(animate);
            }
        };
        
        requestAnimationFrame(animate);
    };
    
    // Enhanced drag functionality with better mouse handling
    var startDrag = function (e) {
        // Only respond to left mouse button
        if (e.button !== 0) return;
        
        // Don't drag if clicking on window controls or interactive elements
        if (e.target.closest('.macos-window-controls') || 
            e.target.closest('button') || 
            e.target.closest('input') || 
            e.target.closest('select') ||
            e.target.closest('label')) {
            return;
        }
        
        console.log('Starting drag...');
        
        e.preventDefault();
        e.stopPropagation();
        
        isDragging = true;
        dragStartX = e.clientX;
        dragStartY = e.clientY;
        windowStartX = currentX;
        windowStartY = currentY;
        
        // Add event listeners to document for better mouse tracking
        document.addEventListener('mousemove', handleDrag, { passive: false, capture: true });
        document.addEventListener('mouseup', stopDrag, { passive: false, capture: true });
        
        // Visual feedback
        document.body.style.userSelect = 'none';
        document.body.style.webkitUserSelect = 'none';
        document.body.style.cursor = 'grabbing';
        titlebarElement.style.cursor = 'grabbing';
        
        windowElement.classList.add('macos-window--dragging');
        windowElement.style.zIndex = '1000';
        
        console.log('Drag started, isDragging:', isDragging);
    };
    
    var handleDrag = function (e) {
        if (!isDragging) return;
        
        e.preventDefault();
        e.stopPropagation();
        
        // Cancel any pending animation frame
        if (animationFrame) {
            cancelAnimationFrame(animationFrame);
        }
        
        // Calculate movement
        var deltaX = e.clientX - dragStartX;
        var deltaY = e.clientY - dragStartY;
        
        var newX = windowStartX + deltaX;
        var newY = windowStartY + deltaY;
        
        // Apply movement immediately for responsiveness
        setWindowPosition(newX, newY);
    };
    
    var stopDrag = function (e) {
        if (!isDragging) return;
        
        console.log('Stopping drag...');
        
        isDragging = false;
        
        // Cancel any pending animation frame
        if (animationFrame) {
            cancelAnimationFrame(animationFrame);
            animationFrame = null;
        }
        
        // Remove event listeners
        document.removeEventListener('mousemove', handleDrag, { capture: true });
        document.removeEventListener('mouseup', stopDrag, { capture: true });
        
        // Restore styles
        document.body.style.userSelect = '';
        document.body.style.webkitUserSelect = '';
        document.body.style.cursor = '';
        titlebarElement.style.cursor = 'grab';
        
        windowElement.classList.remove('macos-window--dragging');
        windowElement.style.zIndex = '';
        
        e.preventDefault();
        e.stopPropagation();
    };
    
    // Touch events for mobile support
    var handleTouchStart = function (e) {
        if (e.touches.length !== 1) return;
        
        var touch = e.touches[0];
        var mouseEvent = new MouseEvent('mousedown', {
            clientX: touch.clientX,
            clientY: touch.clientY,
            button: 0
        });
        
        startDrag(mouseEvent);
    };
    
    var handleTouchMove = function (e) {
        if (!isDragging || e.touches.length !== 1) return;
        
        e.preventDefault();
        
        var touch = e.touches[0];
        var mouseEvent = new MouseEvent('mousemove', {
            clientX: touch.clientX,
            clientY: touch.clientY
        });
        
        handleDrag(mouseEvent);
    };
    
    var handleTouchEnd = function (e) {
        if (!isDragging) return;
        
        var mouseEvent = new MouseEvent('mouseup', {
            clientX: 0,
            clientY: 0,
            button: 0
        });
        
        stopDrag(mouseEvent);
    };
    
    // Window resize handler
    var handleResize = function () {
        if (!isDragging) {
            // Reposition window to stay within bounds
            setWindowPosition(currentX, currentY);
        }
    };
    
    // Initialize position
    initializePosition();
    
    // Add event listeners
    titlebarElement.addEventListener('mousedown', startDrag, { passive: false });
    titlebarElement.addEventListener('touchstart', handleTouchStart, { passive: false });
    titlebarElement.addEventListener('touchmove', handleTouchMove, { passive: false });
    titlebarElement.addEventListener('touchend', handleTouchEnd, { passive: false });
    titlebarElement.addEventListener('contextmenu', function(e) { e.preventDefault(); });
    
    window.addEventListener('resize', handleResize);
    
    console.log('Window controller created successfully');
    
    // Return API object with cleanup function
    return {
        moveWindow: moveWindow,
        moveWindowTo: moveWindowTo,
        centerWindow: centerWindow,
        snapToEdge: snapToEdge,
        animateWindowTo: animateWindowTo,
        getPosition: getWindowPosition,
        cleanup: function() {
            console.log('Cleaning up window controller...');
            titlebarElement.removeEventListener('mousedown', startDrag);
            titlebarElement.removeEventListener('touchstart', handleTouchStart);
            titlebarElement.removeEventListener('touchmove', handleTouchMove);
            titlebarElement.removeEventListener('touchend', handleTouchEnd);
            titlebarElement.removeEventListener('contextmenu', function() {});
            
            window.removeEventListener('resize', handleResize);
            
            if (isDragging) {
                document.removeEventListener('mousemove', handleDrag, { capture: true });
                document.removeEventListener('mouseup', stopDrag, { capture: true });
                stopDrag({ preventDefault: function() {}, stopPropagation: function() {} });
            }
            
            if (animationFrame) {
                cancelAnimationFrame(animationFrame);
            }
        }
    };
};

// Improved dropzone functionality with better mouse feedback
window.initializeDropzone = function (element, dotNetRef) {
    if (!element || !dotNetRef) {
        console.error('Invalid parameters for dropzone initialization');
        return null;
    }

    var dragCounter = 0;
    var isDragOver = false;

    var handleDragEnter = function (e) {
        e.preventDefault();
        e.stopPropagation();
        dragCounter++;
        
        if (!isDragOver) {
            isDragOver = true;
            element.classList.add('dropzone-active');
            try {
                dotNetRef.invokeMethodAsync('OnDragEnter');
            } catch (error) {
                console.error('Error calling OnDragEnter:', error);
            }
        }
    };

    var handleDragLeave = function (e) {
        e.preventDefault();
        e.stopPropagation();
        dragCounter--;
        
        if (dragCounter <= 0) {
            isDragOver = false;
            element.classList.remove('dropzone-active');
            try {
                dotNetRef.invokeMethodAsync('OnDragLeave');
            } catch (error) {
                console.error('Error calling OnDragLeave:', error);
            }
            dragCounter = 0;
        }
    };

    var handleDragOver = function (e) {
        e.preventDefault();
        e.stopPropagation();
        e.dataTransfer.dropEffect = 'copy';
    };

    var handleDrop = function (e) {
        e.preventDefault();
        e.stopPropagation();
        
        dragCounter = 0;
        isDragOver = false;
        element.classList.remove('dropzone-active');
        
        var files = Array.from(e.dataTransfer.files);
        if (files.length > 0) {
            try {
                // Pass file information to Blazor
                dotNetRef.invokeMethodAsync('OnFileDropped', files[0].name, files[0].size, files[0].type);
                
                // Create a FileList-like object and trigger the file input
                var fileInput = element.querySelector('input[type="file"]');
                if (fileInput) {
                    var dataTransfer = new DataTransfer();
                    files.forEach(function(file) { 
                        dataTransfer.items.add(file); 
                    });
                    fileInput.files = dataTransfer.files;
                    
                    var event = new Event('change', { bubbles: true });
                    fileInput.dispatchEvent(event);
                }
            } catch (error) {
                console.error('Error handling file drop:', error);
            }
        }
        
        try {
            dotNetRef.invokeMethodAsync('OnDragLeave');
        } catch (error) {
            console.error('Error calling OnDragLeave after drop:', error);
        }
    };

    // Add event listeners
    element.addEventListener('dragenter', handleDragEnter);
    element.addEventListener('dragleave', handleDragLeave);
    element.addEventListener('dragover', handleDragOver);
    element.addEventListener('drop', handleDrop);

    // Return cleanup function
    return function () {
        element.removeEventListener('dragenter', handleDragEnter);
        element.removeEventListener('dragleave', handleDragLeave);
        element.removeEventListener('dragover', handleDragOver);
        element.removeEventListener('drop', handleDrop);
        element.classList.remove('dropzone-active');
    };
};

window.cleanupDropzone = function (element) {
    if (element && element._dropzoneCleanup) {
        element._dropzoneCleanup();
        delete element._dropzoneCleanup;
    }
};

// Prevent default drag behavior on images to avoid browser's built-in image dragging
document.addEventListener('DOMContentLoaded', function () {
    document.addEventListener('dragstart', function (e) {
        if (e.target.tagName === 'IMG' && e.target.closest('.media-item')) {
            e.preventDefault();
        }
    });
    
    // Prevent text selection on draggable elements
    document.addEventListener('selectstart', function (e) {
        if (e.target.closest('.macos-titlebar')) {
            e.preventDefault();
        }
    });
});


// Ensure functions are available when Blazor loads
if (typeof window.blazorJSReadyCallbacks === 'undefined') {
    window.blazorJSReadyCallbacks = [];
}
