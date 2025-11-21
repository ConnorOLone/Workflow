// Main Designer Application
class WorkflowDesigner {
    constructor() {
        this.api = new WorkflowAPI();
        this.canvas = new WorkflowCanvas('workflowCanvas');
        this.currentDefinition = null;

        this.setupEventListeners();
        this.setupDragAndDrop();
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
        const panel = document.getElementById('propertiesContent');
        panel.innerHTML = `
            <div class="form-group">
                <label>Name:</label>
                <input type="text" id="propName" value="${activity.name}">
            </div>
            <div class="form-group">
                <label>Type:</label>
                <input type="text" value="${activity.type}" readonly>
            </div>
            <div class="form-group">
                <label>Description:</label>
                <textarea id="propDescription" rows="3">${activity.description || ''}</textarea>
            </div>
            <div class="form-group">
                <label>Configuration (JSON):</label>
                <textarea id="propConfig" rows="5">${JSON.stringify(activity.configuration, null, 2)}</textarea>
            </div>
            <div class="form-group">
                <label>Input Mappings (JSON):</label>
                <textarea id="propInputMappings" rows="3">${JSON.stringify(activity.inputMappings, null, 2)}</textarea>
            </div>
            <div class="form-group">
                <label>Output Mappings (JSON):</label>
                <textarea id="propOutputMappings" rows="3">${JSON.stringify(activity.outputMappings, null, 2)}</textarea>
            </div>
            <button class="btn btn-primary" id="applyPropertiesBtn">Apply</button>
        `;

        document.getElementById('applyPropertiesBtn').addEventListener('click', () => {
            activity.name = document.getElementById('propName').value;
            activity.description = document.getElementById('propDescription').value;
            try {
                activity.configuration = JSON.parse(document.getElementById('propConfig').value);
                activity.inputMappings = JSON.parse(document.getElementById('propInputMappings').value);
                activity.outputMappings = JSON.parse(document.getElementById('propOutputMappings').value);
                this.canvas.draw();
                alert('Properties updated!');
            } catch (error) {
                alert('Error parsing JSON: ' + error.message);
            }
        });
    }

    clearProperties() {
        document.getElementById('propertiesContent').innerHTML = '<p class="no-selection">Select an activity to view properties</p>';
    }
}

// Initialize the designer when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    new WorkflowDesigner();
});
