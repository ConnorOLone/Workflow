// Main Designer Application
class WorkflowDesigner {
    constructor() {
        this.api = new WorkflowAPI();
        this.canvas = new WorkflowCanvas('workflowCanvas');
        this.currentDefinition = null;
        this.currentPropertyEditor = null;

        this.setupEventListeners();
        this.setupDragAndDrop();
        this.setupResizablePanel();
        this.setupZoomControls();
    }

    setupEventListeners() {
        // Toolbar buttons
        document.getElementById('newWorkflowBtn').addEventListener('click', () => this.newWorkflow());
        document.getElementById('saveWorkflowBtn').addEventListener('click', () => this.saveWorkflow());
        document.getElementById('loadWorkflowBtn').addEventListener('click', () => this.loadWorkflows());
        document.getElementById('startWorkflowBtn').addEventListener('click', () => this.showStartModal());

        // Activity selection
        document.addEventListener('activitySelected', (e) => this.showProperties(e.detail));

        // Close modals
        document.querySelectorAll('.close-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                document.querySelectorAll('.modal').forEach(modal => {
                    modal.style.display = 'none';
                });
            });
        });

        // Start workflow
        document.getElementById('confirmStartBtn').addEventListener('click', () => this.startWorkflow());
    }

    setupDragAndDrop() {
        document.querySelectorAll('.activity-item').forEach(item => {
            item.addEventListener('dragstart', (e) => {
                e.dataTransfer.setData('activityType', item.dataset.type);
            });
        });
    }

    setupResizablePanel() {
        const resizeHandle = document.getElementById('resizeHandle');
        const propertiesPanel = document.getElementById('propertiesPanel');
        let isResizing = false;
        let startX = 0;
        let startWidth = 0;

        resizeHandle.addEventListener('mousedown', (e) => {
            isResizing = true;
            startX = e.clientX;
            startWidth = propertiesPanel.offsetWidth;
            resizeHandle.classList.add('resizing');
            document.body.style.cursor = 'col-resize';
            document.body.style.userSelect = 'none';
            e.preventDefault();
        });

        document.addEventListener('mousemove', (e) => {
            if (!isResizing) return;

            const deltaX = startX - e.clientX;
            const newWidth = startWidth + deltaX;

            // Respect min and max width
            const minWidth = parseInt(getComputedStyle(propertiesPanel).minWidth);
            const maxWidth = parseInt(getComputedStyle(propertiesPanel).maxWidth);

            if (newWidth >= minWidth && newWidth <= maxWidth) {
                propertiesPanel.style.width = newWidth + 'px';
            }
        });

        document.addEventListener('mouseup', () => {
            if (isResizing) {
                isResizing = false;
                resizeHandle.classList.remove('resizing');
                document.body.style.cursor = '';
                document.body.style.userSelect = '';
            }
        });

        // Close properties button
        const closeBtn = document.getElementById('closePropertiesBtn');
        if (closeBtn) {
            closeBtn.addEventListener('click', () => {
                propertiesPanel.classList.add('hidden');
                resizeHandle.style.display = 'none';
            });
        }
    }

    setupZoomControls() {
        document.getElementById('zoomInBtn')?.addEventListener('click', () => {
            this.canvas.zoomIn();
        });

        document.getElementById('zoomOutBtn')?.addEventListener('click', () => {
            this.canvas.zoomOut();
        });

        document.getElementById('resetZoomBtn')?.addEventListener('click', () => {
            this.canvas.resetView();
        });
    }

    newWorkflow() {
        if (confirm('Create a new workflow? Unsaved changes will be lost.')) {
            this.canvas.clear();
            this.currentDefinition = null;
            this.clearProperties();
        }
    }

    async saveWorkflow() {
        const name = prompt('Workflow Name:', this.currentDefinition?.name || 'New Workflow');
        if (!name) return;

        const description = prompt('Description:', this.currentDefinition?.description || '');
        const definition = this.canvas.toWorkflowDefinition(name, description, '1.0.0');

        try {
            if (this.currentDefinition?.id) {
                await this.api.updateWorkflowDefinition(this.currentDefinition.id, definition);
                alert('Workflow updated successfully!');
            } else {
                const result = await this.api.createWorkflowDefinition(definition);
                this.currentDefinition = result;
                alert('Workflow created successfully!');
            }
        } catch (error) {
            alert('Error saving workflow: ' + error.message);
        }
    }

    async loadWorkflows() {
        try {
            const workflows = await this.api.getWorkflowDefinitions();
            this.showWorkflowsList(workflows);
        } catch (error) {
            alert('Error loading workflows: ' + error.message);
        }
    }

    showWorkflowsList(workflows) {
        const modal = document.getElementById('workflowsModal');
        const list = document.getElementById('workflowsList');

        list.innerHTML = '';
        workflows.forEach(workflow => {
            const item = document.createElement('div');
            item.className = 'workflow-item';
            item.innerHTML = `
                <h4>${workflow.name}</h4>
                <p>${workflow.description || 'No description'}</p>
                <p style="font-size: 0.75rem; color: #95a5a6;">Version: ${workflow.version} | Created: ${new Date(workflow.createdAt).toLocaleDateString()}</p>
            `;
            item.addEventListener('click', () => {
                this.loadWorkflow(workflow.id);
                modal.style.display = 'none';
            });
            list.appendChild(item);
        });

        modal.style.display = 'flex';
    }

    async loadWorkflow(id) {
        try {
            const definition = await this.api.getWorkflowDefinition(id);
            this.currentDefinition = definition;
            this.canvas.loadWorkflowDefinition(definition);
            alert(`Loaded workflow: ${definition.name}`);
        } catch (error) {
            alert('Error loading workflow: ' + error.message);
        }
    }

    showStartModal() {
        if (!this.currentDefinition) {
            alert('Please save the workflow first before starting an instance.');
            return;
        }
        document.getElementById('startModal').style.display = 'flex';
        document.getElementById('initialVariables').value = JSON.stringify({
            exampleVar: "value"
        }, null, 2);
    }

    async startWorkflow() {
        const variablesText = document.getElementById('initialVariables').value;
        const initiatedBy = document.getElementById('initiatedBy').value;

        try {
            const variables = variablesText ? JSON.parse(variablesText) : {};
            const instance = await this.api.startWorkflow(
                this.currentDefinition.id,
                variables,
                initiatedBy || 'designer@example.com'
            );

            document.getElementById('startModal').style.display = 'none';
            alert(`Workflow instance started!\nInstance ID: ${instance.id}\nState: ${instance.state}`);
        } catch (error) {
            alert('Error starting workflow: ' + error.message);
        }
    }

    showProperties(activity) {
        // Show properties panel if hidden
        const propertiesPanel = document.getElementById('propertiesPanel');
        const resizeHandle = document.getElementById('resizeHandle');
        if (propertiesPanel.classList.contains('hidden')) {
            propertiesPanel.classList.remove('hidden');
            resizeHandle.style.display = 'block';
        }

        // Clean up previous editor
        if (this.currentPropertyEditor) {
            this.currentPropertyEditor.destroy();
            this.currentPropertyEditor = null;
        }

        const panel = document.getElementById('propertiesContent');

        // Get workflow variables for expression builder
        const workflowVariables = this.currentDefinition?.variables || {};

        // Create activity-specific property editor
        const editor = PropertyEditorFactory.createEditor(activity, workflowVariables);
        this.currentPropertyEditor = editor;

        // Render the editor
        panel.innerHTML = editor.render();

        // Attach any event listeners the editor needs
        if (editor.attachEventListeners) {
            editor.attachEventListeners();
        }

        // Handle Apply button
        const applyBtn = document.getElementById('applyPropertiesBtn');
        if (applyBtn) {
            applyBtn.addEventListener('click', () => {
                const success = editor.apply();
                if (success !== false) {
                    this.canvas.draw();
                    this.showNotification('Properties updated successfully', 'success');
                }
            });
        }

        // Handle Cancel button
        const cancelBtn = document.getElementById('cancelPropertiesBtn');
        if (cancelBtn) {
            cancelBtn.addEventListener('click', () => {
                this.clearProperties();
                this.canvas.selectedActivity = null;
                this.canvas.draw();
            });
        }
    }

    showNotification(message, type = 'info') {
        // Simple notification - could be enhanced with a toast library
        const style = type === 'success' ? 'background: #2ecc71; color: white;' :
                      type === 'error' ? 'background: #e74c3c; color: white;' :
                      'background: #3498db; color: white;';

        const notification = document.createElement('div');
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 15px 20px;
            border-radius: 4px;
            ${style}
            z-index: 10000;
            animation: slideIn 0.3s ease;
        `;
        notification.textContent = message;
        document.body.appendChild(notification);

        setTimeout(() => {
            notification.style.animation = 'slideOut 0.3s ease';
            setTimeout(() => notification.remove(), 300);
        }, 3000);
    }

    clearProperties() {
        // Clean up current editor
        if (this.currentPropertyEditor) {
            this.currentPropertyEditor.destroy();
            this.currentPropertyEditor = null;
        }
        document.getElementById('propertiesContent').innerHTML = '<p class="no-selection">Select an activity to view properties</p>';
    }
}

// Initialize the designer when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    new WorkflowDesigner();
});
