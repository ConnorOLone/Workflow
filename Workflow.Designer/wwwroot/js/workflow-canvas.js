// Workflow Canvas - Visual Designer with Drag & Drop
class WorkflowCanvas {
    constructor(canvasId) {
        this.canvas = document.getElementById(canvasId);
        this.ctx = this.canvas.getContext('2d');
        this.activities = [];
        this.transitions = [];
        this.selectedActivity = null;
        this.connectingFrom = null;
        this.draggedActivity = null;
        this.offsetX = 0;
        this.offsetY = 0;
        this.hoveredActivity = null;

        this.setupEventListeners();
        this.draw();
    }

    setupEventListeners() {
        // Mouse events
        this.canvas.addEventListener('mousedown', this.onMouseDown.bind(this));
        this.canvas.addEventListener('mousemove', this.onMouseMove.bind(this));
        this.canvas.addEventListener('mouseup', this.onMouseUp.bind(this));
        this.canvas.addEventListener('contextmenu', this.onContextMenu.bind(this));

        // Drag and drop from toolbox
        this.canvas.addEventListener('dragover', (e) => e.preventDefault());
        this.canvas.addEventListener('drop', this.onDrop.bind(this));
    }

    onMouseDown(e) {
        const rect = this.canvas.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;

        const activity = this.getActivityAt(x, y);
        if (activity) {
            this.draggedActivity = activity;
            this.offsetX = x - activity.position.x;
            this.offsetY = y - activity.position.y;
        }
    }

    onMouseMove(e) {
        const rect = this.canvas.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;

        // Update hovered activity
        this.hoveredActivity = this.getActivityAt(x, y);

        // Drag activity
        if (this.draggedActivity) {
            this.draggedActivity.position.x = x - this.offsetX;
            this.draggedActivity.position.y = y - this.offsetY;
            this.draw();
        }

        // Show hover cursor
        this.canvas.style.cursor = this.hoveredActivity ? 'pointer' : 'crosshair';
    }

    onMouseUp(e) {
        if (this.draggedActivity) {
            this.draggedActivity = null;
        }
    }

    onContextMenu(e) {
        e.preventDefault();
        const rect = this.canvas.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;

        const activity = this.getActivityAt(x, y);
        if (activity) {
            this.showContextMenu(e.clientX, e.clientY, activity);
        }
    }

    onDrop(e) {
        e.preventDefault();
        const activityType = e.dataTransfer.getData('activityType');
        if (activityType) {
            const rect = this.canvas.getBoundingClientRect();
            this.addActivity({
                id: this.generateId(),
                name: `${activityType} ${this.activities.length + 1}`,
                type: activityType,
                position: {
                    x: e.clientX - rect.left - 40,
                    y: e.clientY - rect.top - 30
                },
                configuration: {},
                inputMappings: {},
                outputMappings: {}
            });
        }
    }

    addActivity(activity) {
        this.activities.push(activity);
        this.draw();
    }

    deleteActivity(activity) {
        const index = this.activities.indexOf(activity);
        if (index > -1) {
            this.activities.splice(index, 1);
            // Remove related transitions
            this.transitions = this.transitions.filter(
                t => t.fromActivityId !== activity.id && t.toActivityId !== activity.id
            );
            this.draw();
        }
    }

    connectActivities(from, to) {
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

    draw() {
        // Clear canvas
        this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);

        // Draw grid
        this.drawGrid();

        // Draw transitions
        this.transitions.forEach(transition => this.drawTransition(transition));

        // Draw activities
        this.activities.forEach(activity => this.drawActivity(activity));
    }

    drawGrid() {
        this.ctx.strokeStyle = '#e0e0e0';
        this.ctx.lineWidth = 1;

        for (let x = 0; x < this.canvas.width; x += 20) {
            this.ctx.beginPath();
            this.ctx.moveTo(x, 0);
            this.ctx.lineTo(x, this.canvas.height);
            this.ctx.stroke();
        }

        for (let y = 0; y < this.canvas.height; y += 20) {
            this.ctx.beginPath();
            this.ctx.moveTo(0, y);
            this.ctx.lineTo(this.canvas.width, y);
            this.ctx.stroke();
        }
    }

    drawActivity(activity) {
        const bounds = this.getActivityBounds(activity);
        const isSelected = activity === this.selectedActivity;
        const isHovered = activity === this.hoveredActivity;

        // Get color based on activity type
        const colors = {
            'Start': '#2ecc71',
            'End': '#e74c3c',
            'HumanTask': '#3498db',
            'ServiceTask': '#9b59b6',
            'ScriptTask': '#e67e22',
            'Decision': '#f39c12'
        };
        const color = colors[activity.type] || '#95a5a6';

        // Draw shadow
        if (isHovered || isSelected) {
            this.ctx.fillStyle = 'rgba(0,0,0,0.1)';
            this.ctx.fillRect(bounds.x + 3, bounds.y + 3, bounds.width, bounds.height);
        }

        // Draw rectangle
        this.ctx.fillStyle = color;
        this.ctx.fillRect(bounds.x, bounds.y, bounds.width, bounds.height);

        // Draw border
        this.ctx.strokeStyle = isSelected ? '#2c3e50' : '#34495e';
        this.ctx.lineWidth = isSelected ? 3 : 2;
        this.ctx.strokeRect(bounds.x, bounds.y, bounds.width, bounds.height);

        // Draw icon
        this.ctx.font = '24px Arial';
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
        this.ctx.font = '12px Arial';
        this.ctx.fillText(activity.name, bounds.x + bounds.width / 2, bounds.y + 45);
    }

    drawTransition(transition) {
        const from = this.activities.find(a => a.id === transition.fromActivityId);
        const to = this.activities.find(a => a.id === transition.toActivityId);

        if (!from || !to) return;

        const fromBounds = this.getActivityBounds(from);
        const toBounds = this.getActivityBounds(to);

        const startX = fromBounds.x + fromBounds.width / 2;
        const startY = fromBounds.y + fromBounds.height;
        const endX = toBounds.x + toBounds.width / 2;
        const endY = toBounds.y;

        // Draw line
        this.ctx.strokeStyle = '#34495e';
        this.ctx.lineWidth = 2;
        this.ctx.beginPath();
        this.ctx.moveTo(startX, startY);
        this.ctx.lineTo(endX, endY);
        this.ctx.stroke();

        // Draw arrow
        const angle = Math.atan2(endY - startY, endX - startX);
        const arrowLength = 10;
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
            } else if (action === 'connect') {
                if (!this.connectingFrom) {
                    this.connectingFrom = activity;
                    alert('Click on the target activity to create connection');
                } else {
                    this.connectActivities(this.connectingFrom, activity);
                    this.connectingFrom = null;
                }
            }
            menu.style.display = 'none';
        };

        document.addEventListener('click', () => {
            menu.style.display = 'none';
        }, { once: true });
    }

    selectActivity(activity) {
        this.selectedActivity = activity;
        this.draw();
        // Trigger event for properties panel
        document.dispatchEvent(new CustomEvent('activitySelected', { detail: activity }));
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
        this.draw();
    }

    clear() {
        this.activities = [];
        this.transitions = [];
        this.selectedActivity = null;
        this.draw();
    }
}
