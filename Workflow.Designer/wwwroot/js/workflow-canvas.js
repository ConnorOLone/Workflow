// Workflow Canvas - Excalidraw-style with Pan/Zoom and Connection Handles
class WorkflowCanvas {
    constructor(canvasId) {
        this.canvas = document.getElementById(canvasId);
        this.container = this.canvas.parentElement;
        this.ctx = this.canvas.getContext('2d');

        // Data
        this.activities = [];
        this.transitions = [];
        this.selectedActivity = null;

        // Interaction state
        this.draggedActivity = null;
        this.offsetX = 0;
        this.offsetY = 0;
        this.hoveredActivity = null;
        this.connectingFrom = null;
        this.hoveredConnectionHandle = null;

        // Pan & Zoom state
        this.panX = 0;
        this.panY = 0;
        this.zoom = 1.0;
        this.minZoom = 0.1;
        this.maxZoom = 5.0;
        this.isPanning = false;
        this.panStartX = 0;
        this.panStartY = 0;

        // Connection handle state
        this.isDraggingConnection = false;
        this.connectionStartHandle = null;
        this.connectionMouseX = 0;
        this.connectionMouseY = 0;

        // Theme support
        this.currentTheme = 'light';
        this.initializeTheme();

        this.setupCanvas();
        this.setupEventListeners();
        this.setupResizeObserver();
        this.draw();
    }

    initializeTheme() {
        // Check if theme manager is available
        if (window.themeManager) {
            this.currentTheme = window.themeManager.getTheme();
        } else {
            // Fallback: check body class
            this.currentTheme = document.body.classList.contains('dark-mode') ? 'dark' : 'light';
        }

        // Listen for theme changes
        window.addEventListener('themechange', (e) => {
            this.updateTheme(e.detail.theme);
        });
    }

    updateTheme(theme) {
        this.currentTheme = theme;
        this.draw(); // Redraw canvas with new colors
    }

    getThemeColors() {
        if (this.currentTheme === 'dark') {
            return {
                grid: '#3a3a3a',
                activityStroke: '#e0e0e0',
                activityStrokeHover: '#ffffff',
                activityText: '#e0e0e0',
                transitionLine: '#666666',
                connectionHandle: '#4a9eff',
                connectionHandleHover: '#60b0ff',
                selectedStroke: '#4a9eff'
            };
        } else {
            return {
                grid: '#8a7777ff',
                activityStroke: '#2c3e50',
                activityStrokeHover: '#34495e',
                activityText: '#2c3e50',
                transitionLine: '#95a5a6',
                connectionHandle: '#3498db',
                connectionHandleHover: '#2ecc71',
                selectedStroke: '#3498db'
            };
        }
    }

    setupCanvas() {
        // Get container dimensions
        const rect = this.container.getBoundingClientRect();
        const displayWidth = rect.width;
        const displayHeight = rect.height;

        // Set up high DPI rendering
        const dpr = window.devicePixelRatio || 1;

        // Set the actual canvas size (accounting for device pixel ratio)
        this.canvas.width = displayWidth * dpr;
        this.canvas.height = displayHeight * dpr;

        // Set the display size (CSS pixels)
        this.canvas.style.width = displayWidth + 'px';
        this.canvas.style.height = displayHeight + 'px';

        // Scale the context to account for device pixel ratio
        this.ctx.scale(dpr, dpr);

        // Store display size
        this.displayWidth = displayWidth;
        this.displayHeight = displayHeight;

        console.log(`Canvas resized to: ${displayWidth} x ${displayHeight} (display), ${this.canvas.width} x ${this.canvas.height} (actual)`);
        console.log(`Rendering context size: ${this.canvas.width} x ${this.canvas.height}`);
    }

    setupResizeObserver() {
        let resizeTimeout;
        const resizeObserver = new ResizeObserver(() => {
            // Debounce the resize to avoid excessive redraws
            clearTimeout(resizeTimeout);
            resizeTimeout = setTimeout(() => {
                this.setupCanvas();
                this.draw();
            }, 16); // ~60fps
        });
        resizeObserver.observe(this.container);
    }

    setupEventListeners() {
        // Mouse events
        this.canvas.addEventListener('mousedown', this.onMouseDown.bind(this));
        this.canvas.addEventListener('mousemove', this.onMouseMove.bind(this));
        this.canvas.addEventListener('mouseup', this.onMouseUp.bind(this));
        this.canvas.addEventListener('contextmenu', this.onContextMenu.bind(this));
        this.canvas.addEventListener('wheel', this.onWheel.bind(this), { passive: false });

        // Drag and drop from toolbox
        this.canvas.addEventListener('dragover', (e) => e.preventDefault());
        this.canvas.addEventListener('drop', this.onDrop.bind(this));

        // Window mouse up (for dragging outside canvas)
        window.addEventListener('mouseup', this.onMouseUp.bind(this));
    }

    // Coordinate transformation
    screenToWorld(screenX, screenY) {
        return {
            x: (screenX - this.panX) / this.zoom,
            y: (screenY - this.panY) / this.zoom
        };
    }

    worldToScreen(worldX, worldY) {
        return {
            x: worldX * this.zoom + this.panX,
            y: worldY * this.zoom + this.panY
        };
    }

    onMouseDown(e) {
        const rect = this.canvas.getBoundingClientRect();
        const screenX = e.clientX - rect.left;
        const screenY = e.clientY - rect.top;

        // Right mouse button = pan
        if (e.button === 2) {
            this.isPanning = true;
            this.panStartX = screenX;
            this.panStartY = screenY;
            this.canvas.classList.add('panning');
            return;
        }

        // Left mouse button
        if (e.button === 0) {
            const worldPos = this.screenToWorld(screenX, screenY);

            // Check if clicking on a connection handle
            const handle = this.getConnectionHandleAt(screenX, screenY);
            if (handle) {
                this.isDraggingConnection = true;
                this.connectionStartHandle = handle;
                this.connectionMouseX = screenX;
                this.connectionMouseY = screenY;
                return;
            }

            // Check if clicking on an activity
            const activity = this.getActivityAt(worldPos.x, worldPos.y);
            if (activity) {
                this.draggedActivity = activity;
                this.offsetX = worldPos.x - activity.position.x;
                this.offsetY = worldPos.y - activity.position.y;

                // Select the activity
                if (this.selectedActivity !== activity) {
                    this.selectActivity(activity);
                }
            } else {
                // Clicked on empty space - deselect
                if (this.selectedActivity) {
                    this.selectActivity(null);
                }
            }
        }
    }

    onMouseMove(e) {
        const rect = this.canvas.getBoundingClientRect();
        const screenX = e.clientX - rect.left;
        const screenY = e.clientY - rect.top;
        const worldPos = this.screenToWorld(screenX, screenY);

        // Panning
        if (this.isPanning) {
            const dx = screenX - this.panStartX;
            const dy = screenY - this.panStartY;
            this.panX += dx;
            this.panY += dy;
            this.panStartX = screenX;
            this.panStartY = screenY;
            this.draw();
            return;
        }

        // Dragging connection
        if (this.isDraggingConnection) {
            this.connectionMouseX = screenX;
            this.connectionMouseY = screenY;
            this.draw();
            return;
        }

        // Dragging activity
        if (this.draggedActivity) {
            this.draggedActivity.position.x = worldPos.x - this.offsetX;
            this.draggedActivity.position.y = worldPos.y - this.offsetY;
            this.draw();
            return;
        }

        // Hovering - update hover state
        this.hoveredActivity = this.getActivityAt(worldPos.x, worldPos.y);
        this.hoveredConnectionHandle = this.getConnectionHandleAt(screenX, screenY);

        // Update cursor
        if (this.hoveredConnectionHandle) {
            this.canvas.style.cursor = 'crosshair';
        } else if (this.hoveredActivity) {
            this.canvas.style.cursor = 'move';
        } else {
            this.canvas.style.cursor = 'default';
        }

        this.draw();
    }

    onMouseUp(e) {
        const rect = this.canvas.getBoundingClientRect();
        const screenX = e.clientX - rect.left;
        const screenY = e.clientY - rect.top;
        const worldPos = this.screenToWorld(screenX, screenY);

        // End panning
        if (this.isPanning) {
            this.isPanning = false;
            this.canvas.classList.remove('panning');
        }

        // End connection dragging
        if (this.isDraggingConnection) {
            const targetActivity = this.getActivityAt(worldPos.x, worldPos.y);
            if (targetActivity && targetActivity !== this.connectionStartHandle.activity) {
                this.connectActivities(this.connectionStartHandle.activity, targetActivity);
            }
            this.isDraggingConnection = false;
            this.connectionStartHandle = null;
            this.draw();
        }

        // End activity dragging
        if (this.draggedActivity) {
            this.draggedActivity = null;
        }
    }

    // #BUG001
    // RMB click context menu not appearing at cursor event site location.
    onContextMenu(e) {
        e.preventDefault();
        const rect = this.container.getBoundingClientRect();    // HxW from rect correspond to Canvas HxW
        const screenX = e.clientX - rect.left;
        const screenY = e.clientY - rect.top;   // Adjust canvas position based on canvas offset
        const worldPos = this.screenToWorld(screenX, screenY);
        console.log(`Canvas bounding rect: left=${rect.left}, top=${rect.top}, width=${rect.width}, height=${rect.height}`);
        console.log(`Canvas element size: ${this.canvas.style.width} x ${this.canvas.style.height}`);
        console.log(`Rendering context size: ${this.canvas.width} x ${this.canvas.height}`);
        const activity = this.getActivityAt(worldPos.x, worldPos.y);
        if (activity) {
            this.selectActivity(activity);
            // FIX [BUG001]: e.client(x/y) not accounting for canvas offset. Use Screen(X,Y) instead.
            this.showContextMenu(screenX, screenY, activity);
        }
    }

    onWheel(e) {
        // Ctrl+Wheel = zoom
        if (e.ctrlKey || e.metaKey) {
            e.preventDefault();

            const rect = this.canvas.getBoundingClientRect();
            const mouseX = e.clientX - rect.left;
            const mouseY = e.clientY - rect.top;

            // Calculate zoom
            const zoomFactor = e.deltaY < 0 ? 1.1 : 0.9;
            const newZoom = Math.max(this.minZoom, Math.min(this.maxZoom, this.zoom * zoomFactor));

            // Zoom toward mouse position
            const worldBefore = this.screenToWorld(mouseX, mouseY);
            this.zoom = newZoom;
            const worldAfter = this.screenToWorld(mouseX, mouseY);

            this.panX += (worldAfter.x - worldBefore.x) * this.zoom;
            this.panY += (worldAfter.y - worldBefore.y) * this.zoom;

            this.updateZoomDisplay();
            this.draw();
        }
    }

    onDrop(e) {
        e.preventDefault();
        const activityType = e.dataTransfer.getData('activityType');
        if (activityType) {
            const rect = this.canvas.getBoundingClientRect();
            const screenX = e.clientX - rect.left;
            const screenY = e.clientY - rect.top;
            const worldPos = this.screenToWorld(screenX, screenY);

            this.addActivity({
                id: this.generateId(),
                name: `${activityType} ${this.activities.length + 1}`,
                type: activityType,
                position: {
                    x: worldPos.x - 60,
                    y: worldPos.y - 30
                },
                configuration: {},
                inputMappings: {},
                outputMappings: {}
            });
        }
    }

    // Zoom controls
    zoomIn() {
        const centerX = this.displayWidth / 2;
        const centerY = this.displayHeight / 2;
        const worldBefore = this.screenToWorld(centerX, centerY);

        this.zoom = Math.min(this.maxZoom, this.zoom * 1.2);

        const worldAfter = this.screenToWorld(centerX, centerY);
        this.panX += (worldAfter.x - worldBefore.x) * this.zoom;
        this.panY += (worldAfter.y - worldBefore.y) * this.zoom;

        this.updateZoomDisplay();
        this.draw();
    }

    zoomOut() {
        const centerX = this.displayWidth / 2;
        const centerY = this.displayHeight / 2;
        const worldBefore = this.screenToWorld(centerX, centerY);

        this.zoom = Math.max(this.minZoom, this.zoom / 1.2);

        const worldAfter = this.screenToWorld(centerX, centerY);
        this.panX += (worldAfter.x - worldBefore.x) * this.zoom;
        this.panY += (worldAfter.y - worldBefore.y) * this.zoom;

        this.updateZoomDisplay();
        this.draw();
    }

    resetView() {
        this.zoom = 1.0;
        this.panX = 0;
        this.panY = 0;
        this.updateZoomDisplay();
        this.draw();
    }

    updateZoomDisplay() {
        const zoomLevel = document.getElementById('zoomLevel');
        if (zoomLevel) {
            zoomLevel.textContent = Math.round(this.zoom * 100) + '%';
        }
    }

    // Activity management
    addActivity(activity) {
        this.activities.push(activity);
        this.selectActivity(activity);
        this.draw();
    }

    deleteActivity(activity) {
        const index = this.activities.indexOf(activity);
        if (index > -1) {
            this.activities.splice(index, 1);
            this.transitions = this.transitions.filter(
                t => t.fromActivityId !== activity.id && t.toActivityId !== activity.id
            );
            if (this.selectedActivity === activity) {
                this.selectActivity(null);
            }
            this.draw();
        }
    }

    duplicateActivity(activity) {
        const newActivity = JSON.parse(JSON.stringify(activity));
        newActivity.id = this.generateId();
        newActivity.name = activity.name + ' (Copy)';
        newActivity.position.x += 50;
        newActivity.position.y += 50;
        this.addActivity(newActivity);
    }

    connectActivities(from, to) {
        // Check if connection already exists
        const exists = this.transitions.some(
            t => t.fromActivityId === from.id && t.toActivityId === to.id
        );

        if (!exists) {
            this.transitions.push({
                id: this.generateId(),
                name: `${from.name} → ${to.name}`,
                fromActivityId: from.id,
                toActivityId: to.id,
                condition: null,
                priority: 0,
                isDefault: false
            });
            this.draw();
        }
    }

    selectActivity(activity) {
        this.selectedActivity = activity;
        this.draw();

        // Notify designer
        if (activity) {
            document.dispatchEvent(new CustomEvent('activitySelected', { detail: activity }));
        }
    }

    getActivityAt(x, y) {
        for (let i = this.activities.length - 1; i >= 0; i--) {
            const activity = this.activities[i];
            const bounds = this.getActivityBounds(activity);
            if (x >= bounds.x && x <= bounds.x + bounds.width &&
                y >= bounds.y && y <= bounds.y + bounds.height) {
                return activity;
            }
        }
        return null;
    }

    getActivityBounds(activity) {
        return {
            x: activity.position.x,
            y: activity.position.y,
            width: 120,
            height: 60
        };
    }

    getConnectionHandleAt(screenX, screenY) {
        if (!this.selectedActivity) return null;

        const handles = this.getConnectionHandles(this.selectedActivity);

        for (const handle of handles) {
            const dx = screenX - handle.screenX;
            const dy = screenY - handle.screenY;
            const distance = Math.sqrt(dx * dx + dy * dy);

            if (distance <= handle.radius + 2) {
                return handle;
            }
        }

        return null;
    }

    getConnectionHandles(activity) {
        const bounds = this.getActivityBounds(activity);
        const centerX = bounds.x + bounds.width / 2;
        const centerY = bounds.y + bounds.height / 2;

        const positions = [
            { x: centerX, y: bounds.y, position: 'top' },
            { x: bounds.x + bounds.width, y: centerY, position: 'right' },
            { x: centerX, y: bounds.y + bounds.height, position: 'bottom' },
            { x: bounds.x, y: centerY, position: 'left' }
        ];

        return positions.map(pos => {
            const screen = this.worldToScreen(pos.x, pos.y);
            return {
                activity: activity,
                worldX: pos.x,
                worldY: pos.y,
                screenX: screen.x,
                screenY: screen.y,
                position: pos.position,
                radius: 6
            };
        });
    }

    // Drawing
    draw() {
        // Clear canvas
        this.ctx.clearRect(0, 0, this.displayWidth, this.displayHeight);

        // Draw grid
        this.drawGrid();

        // Save context state
        this.ctx.save();

        // Apply pan and zoom transform
        this.ctx.translate(this.panX, this.panY);
        this.ctx.scale(this.zoom, this.zoom);

        // Draw transitions
        this.transitions.forEach(transition => this.drawTransition(transition));

        // Draw connection line being dragged
        if (this.isDraggingConnection && this.connectionStartHandle) {
            this.drawConnectionDrag();
        }

        // Draw activities
        this.activities.forEach(activity => this.drawActivity(activity));

        // Restore context state
        this.ctx.restore();

        // Draw connection handles (in screen space)
        if (this.selectedActivity && !this.isDraggingConnection) {
            this.drawConnectionHandles(this.selectedActivity);
        }
    }

    drawGrid() {
        const gridSize = 20 * this.zoom;
        const offsetX = this.panX % gridSize;
        const offsetY = this.panY % gridSize;
        const colors = this.getThemeColors();

        this.ctx.strokeStyle = colors.grid;
        this.ctx.lineWidth = 0.5;

        for (let x = offsetX; x < this.displayWidth; x += gridSize) {
            this.ctx.beginPath();
            this.ctx.moveTo(x, 0);
            this.ctx.lineTo(x, this.displayHeight);
            this.ctx.stroke();
        }

        for (let y = offsetY; y < this.displayHeight; y += gridSize) {
            this.ctx.beginPath();
            this.ctx.moveTo(0, y);
            this.ctx.lineTo(this.displayWidth, y);
            this.ctx.stroke();
        }
    }

    drawActivity(activity) {
        const bounds = this.getActivityBounds(activity);
        const isSelected = activity === this.selectedActivity;
        const isHovered = activity === this.hoveredActivity;

        // Get color based on activity type
        const activityColors = {
            'Start': '#2ecc71',
            'End': '#e74c3c',
            'HumanTask': '#3498db',
            'ServiceTask': '#9b59b6',
            'ScriptTask': '#e67e22',
            'Decision': '#f39c12'
        };
        const color = activityColors[activity.type] || '#95a5a6';

        // Draw shadow
        if (isHovered || isSelected) {
            this.ctx.fillStyle = 'rgba(0,0,0,0.1)';
            this.ctx.fillRect(bounds.x + 2, bounds.y + 2, bounds.width, bounds.height);
        }

        // Draw rectangle
        this.ctx.fillStyle = color;
        this.ctx.fillRect(bounds.x, bounds.y, bounds.width, bounds.height);

        // Draw border
        const themeColors = this.getThemeColors();
        if (isSelected) {
            this.ctx.strokeStyle = themeColors.selectedStroke;
            this.ctx.lineWidth = 3 / this.zoom;
        } else {
            this.ctx.strokeStyle = isHovered ? themeColors.activityStrokeHover : themeColors.activityStroke;
            this.ctx.lineWidth = 2 / this.zoom;
        }
        this.ctx.strokeRect(bounds.x, bounds.y, bounds.width, bounds.height);

        // Draw icon
        this.ctx.font = `${24 / this.zoom}px Arial`;
        this.ctx.textAlign = 'center';
        this.ctx.textBaseline = 'middle';
        this.ctx.fillStyle = 'white';
        const icons = {
            'Start': '▶️',
            'End': '⏹️',
            'HumanTask': '👤',
            'ServiceTask': '⚙️',
            'ScriptTask': '📝',
            'Decision': '🔀'
        };
        this.ctx.fillText(icons[activity.type] || '?', bounds.x + bounds.width / 2, bounds.y + 20);

        // Draw name
        this.ctx.font = `${12 / this.zoom}px Arial`;
        this.ctx.fillText(activity.name, bounds.x + bounds.width / 2, bounds.y + 45);
    }

    drawTransition(transition) {
        const from = this.activities.find(a => a.id === transition.fromActivityId);
        const to = this.activities.find(a => a.id === transition.toActivityId);

        if (!from || !to) return;

        if (from && to) {
            const fromBounds = this.getActivityBounds(from);
            const toBounds = this.getActivityBounds(to);

            const startX = fromBounds.x + fromBounds.width / 2;
            const startY = fromBounds.y + fromBounds.height;
            const endX = toBounds.x + toBounds.width / 2;
            const endY = toBounds.y;

            // Draw line
            const colors = this.getThemeColors();
            this.ctx.strokeStyle = colors.transitionLine;
            this.ctx.lineWidth = 2 / this.zoom;
            this.ctx.beginPath();
            this.ctx.moveTo(startX, startY);
            this.ctx.lineTo(endX, endY);
            this.ctx.stroke();

            // Draw arrow
            const angle = Math.atan2(endY - startY, endX - startX);
            const arrowLength = 10 / this.zoom;
            this.ctx.beginPath();
            this.ctx.moveTo(endX, endY);
            this.ctx.lineTo(
                endX - arrowLength * Math.cos(angle - Math.PI / 6),
                endY - arrowLength * Math.sin(angle - Math.PI / 6)
            );
            this.ctx.moveTo(endX, endY);
            this.ctx.lineTo(
                endX - arrowLength * Math.cos(angle + Math.PI / 6),
                endY - arrowLength * Math.sin(angle + Math.PI / 6)
            );
            this.ctx.stroke();
        }
    }

    drawConnectionHandles(activity) {
        const handles = this.getConnectionHandles(activity);

        handles.forEach(handle => {
            const isHovered = this.hoveredConnectionHandle === handle;

            // Draw in screen space
            this.ctx.save();
            this.ctx.setTransform(1, 0, 0, 1, 0, 0);

            const colors = this.getThemeColors();

            // Outer circle
            this.ctx.fillStyle = isHovered ? colors.connectionHandleHover : colors.connectionHandle;
            this.ctx.beginPath();
            this.ctx.arc(handle.screenX, handle.screenY, handle.radius + 2, 0, Math.PI * 2);
            this.ctx.fill();

            // Inner circle
            this.ctx.fillStyle = 'white';
            this.ctx.beginPath();
            this.ctx.arc(handle.screenX, handle.screenY, handle.radius - 1, 0, Math.PI * 2);
            this.ctx.fill();

            // Border
            this.ctx.strokeStyle = isHovered ? colors.connectionHandleHover : colors.connectionHandle;
            this.ctx.lineWidth = 2;
            this.ctx.beginPath();
            this.ctx.arc(handle.screenX, handle.screenY, handle.radius + 2, 0, Math.PI * 2);
            this.ctx.stroke();

            this.ctx.restore();
        });
    }

    drawConnectionDrag() {
        const handle = this.connectionStartHandle;
        const screen = this.worldToScreen(handle.worldX, handle.worldY);

        // Draw in screen space
        this.ctx.save();
        this.ctx.setTransform(1, 0, 0, 1, 0, 0);

        const colors = this.getThemeColors();
        this.ctx.strokeStyle = colors.connectionHandle;
        this.ctx.lineWidth = 2;
        this.ctx.setLineDash([5, 5]);
        this.ctx.beginPath();
        this.ctx.moveTo(screen.x, screen.y);
        this.ctx.lineTo(this.connectionMouseX, this.connectionMouseY);
        this.ctx.stroke();
        this.ctx.setLineDash([]);

        this.ctx.restore();
    }

    showContextMenu(x, y, activity) {
        const menu = document.getElementById('contextMenu');
        menu.style.left = x + 'px';
        menu.style.top = y + 'px';
        menu.style.display = 'block';

        menu.onclick = (e) => {
            const action = e.target.dataset.action;
            if (action === 'delete') {
                this.deleteActivity(activity);
            } else if (action === 'properties') {
                this.selectActivity(activity);
            } else if (action === 'duplicate') {
                this.duplicateActivity(activity);
            }
            menu.style.display = 'none';
        };

        document.addEventListener('click', () => {
            menu.style.display = 'none';
        }, { once: true });
    }

    generateId() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
            const r = Math.random() * 16 | 0;
            const v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }

    toWorkflowDefinition(name, description, version) {
        const startActivity = this.activities.find(a => a.type === 'Start');
        return {
            id: this.generateId(),
            name: name || 'New Workflow',
            description: description || '',
            version: version || '1.0.0',
            startActivityId: startActivity ? startActivity.id : this.activities[0]?.id || this.generateId(),
            activities: this.activities,
            transitions: this.transitions,
            variables: {},
            metadata: {},
            isActive: true
        };
    }

    loadWorkflowDefinition(definition) {
        this.activities = definition.activities || [];
        this.transitions = definition.transitions || [];
        this.selectedActivity = null;
        this.draw();
    }

    clear() {
        this.activities = [];
        this.transitions = [];
        this.selectedActivity = null;
        this.draw();
    }
}
