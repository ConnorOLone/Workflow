// API Client for Workflow Engine
class WorkflowAPI {
    constructor(baseUrl = '/api') {
        this.baseUrl = baseUrl;
    }

    async getWorkflowDefinitions() {
        const response = await fetch(`${this.baseUrl}/workflowdefinitions`);
        return await response.json();
    }

    async getWorkflowDefinition(id) {
        const response = await fetch(`${this.baseUrl}/workflowdefinitions/${id}`);
        return await response.json();
    }

    async createWorkflowDefinition(definition) {
        const response = await fetch(`${this.baseUrl}/workflowdefinitions`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(definition)
        });
        return await response.json();
    }

    async updateWorkflowDefinition(id, definition) {
        await fetch(`${this.baseUrl}/workflowdefinitions/${id}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(definition)
        });
    }

    async deleteWorkflowDefinition(id) {
        await fetch(`${this.baseUrl}/workflowdefinitions/${id}`, {
            method: 'DELETE'
        });
    }

    async startWorkflow(workflowDefinitionId, initialVariables, initiatedBy) {
        const response = await fetch(`${this.baseUrl}/workflowinstances/start`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                workflowDefinitionId,
                initialVariables,
                initiatedBy
            })
        });
        return await response.json();
    }

    async getWorkflowInstance(id) {
        const response = await fetch(`${this.baseUrl}/workflowinstances/${id}`);
        return await response.json();
    }

    async completeActivity(activityId, output, completedBy) {
        await fetch(`${this.baseUrl}/workflowinstances/activities/${activityId}/complete`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ output, completedBy })
        });
    }
}
