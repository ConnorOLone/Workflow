// Property Editors for Different Activity Types
// Provides specialized UI for configuring each activity type

class PropertyEditorFactory {
    static createEditor(activity, workflowVariables) {
        switch (activity.type) {
            case 'ServiceTask':
                return new ServiceTaskEditor(activity, workflowVariables);
            case 'ScriptTask':
                return new ScriptTaskEditor(activity, workflowVariables);
            case 'Decision':
                return new DecisionEditor(activity, workflowVariables);
            case 'HumanTask':
                return new HumanTaskEditor(activity, workflowVariables);
            default:
                return new GenericEditor(activity, workflowVariables);
        }
    }
}

// Base Editor Class
class BaseEditor {
    constructor(activity, workflowVariables) {
        this.activity = activity;
        this.workflowVariables = workflowVariables || {};
        this.editors = [];
    }

    render() {
        // Override in subclasses
        return '<p>No editor available</p>';
    }

    getCommonFieldsHTML() {
        return `
            <div class="form-section">
                <h4>Basic Information</h4>
                <div class="form-group">
                    <label>Activity Name:</label>
                    <input type="text" id="propName" value="${this.activity.name}" class="form-control">
                </div>
                <div class="form-group">
                    <label>Description:</label>
                    <textarea id="propDescription" rows="2" class="form-control">${this.activity.description || ''}</textarea>
                </div>
            </div>
        `;
    }

    getTimeoutFieldsHTML() {
        const timeout = this.activity.timeoutSeconds || '';
        const maxRetries = this.activity.maxRetryAttempts || 0;
        const retryDelay = this.activity.retryDelaySeconds || 0;

        return `
            <div class="form-section">
                <h4>Execution Settings</h4>
                <div class="form-group">
                    <label>Timeout (seconds):</label>
                    <input type="number" id="propTimeout" value="${timeout}" class="form-control" placeholder="No timeout">
                </div>
                <div class="form-group">
                    <label>Max Retry Attempts:</label>
                    <input type="number" id="propMaxRetries" value="${maxRetries}" class="form-control" min="0">
                </div>
                <div class="form-group">
                    <label>Retry Delay (seconds):</label>
                    <input type="number" id="propRetryDelay" value="${retryDelay}" class="form-control" min="0">
                </div>
            </div>
        `;
    }

    applyCommonFields() {
        this.activity.name = document.getElementById('propName')?.value || this.activity.name;
        this.activity.description = document.getElementById('propDescription')?.value || '';
        this.activity.timeoutSeconds = parseInt(document.getElementById('propTimeout')?.value) || null;
        this.activity.maxRetryAttempts = parseInt(document.getElementById('propMaxRetries')?.value) || 0;
        this.activity.retryDelaySeconds = parseInt(document.getElementById('propRetryDelay')?.value) || 0;
    }

    apply() {
        this.applyCommonFields();
    }

    destroy() {
        this.editors.forEach(editor => {
            if (editor && editor.destroy) {
                editor.destroy();
            }
        });
        this.editors = [];
    }
}

// Service Task Editor
class ServiceTaskEditor extends BaseEditor {
    render() {
        const config = this.activity.configuration || {};
        const serviceName = config.serviceName || '';
        const methodName = config.methodName || '';
        const parameters = config.parameters || {};

        return `
            ${this.getCommonFieldsHTML()}

            <div class="form-section">
                <h4>Service Configuration</h4>
                <div class="form-group">
                    <label>Service Name:</label>
                    <input type="text" id="serviceName" value="${serviceName}" class="form-control"
                           placeholder="e.g., IEmailService">
                    <small class="form-hint">The interface or class name of the service to invoke</small>
                </div>
                <div class="form-group">
                    <label>Method Name:</label>
                    <input type="text" id="methodName" value="${methodName}" class="form-control"
                           placeholder="e.g., SendEmail">
                    <small class="form-hint">The method to call on the service</small>
                </div>
                <div class="form-group">
                    <label>Parameters (JSON):</label>
                    <textarea id="serviceParameters" rows="6" class="form-control">${JSON.stringify(parameters, null, 2)}</textarea>
                    <small class="form-hint">Parameter values can reference workflow variables using {{variableName}}</small>
                </div>
            </div>

            <div class="form-section">
                <h4>Input Mappings</h4>
                <div class="form-group">
                    <label>Map workflow variables to activity inputs:</label>
                    <div id="inputMappingsContainer"></div>
                    <button type="button" class="btn btn-sm" id="addInputMapping">+ Add Mapping</button>
                </div>
            </div>

            <div class="form-section">
                <h4>Output Mappings</h4>
                <div class="form-group">
                    <label>Map activity outputs to workflow variables:</label>
                    <div id="outputMappingsContainer"></div>
                    <button type="button" class="btn btn-sm" id="addOutputMapping">+ Add Mapping</button>
                </div>
            </div>

            ${this.getTimeoutFieldsHTML()}

            <div class="form-actions">
                <button class="btn btn-primary" id="applyPropertiesBtn">Apply Changes</button>
                <button class="btn btn-secondary" id="cancelPropertiesBtn">Cancel</button>
            </div>
        `;
    }

    attachEventListeners() {
        this.renderMappings();

        document.getElementById('addInputMapping')?.addEventListener('click', () => {
            this.addMapping('input');
        });

        document.getElementById('addOutputMapping')?.addEventListener('click', () => {
            this.addMapping('output');
        });
    }

    renderMappings() {
        const inputContainer = document.getElementById('inputMappingsContainer');
        const outputContainer = document.getElementById('outputMappingsContainer');

        if (inputContainer) {
            inputContainer.innerHTML = this.renderMappingsList(this.activity.inputMappings || {}, 'input');
        }

        if (outputContainer) {
            outputContainer.innerHTML = this.renderMappingsList(this.activity.outputMappings || {}, 'output');
        }
    }

    renderMappingsList(mappings, type) {
        const entries = Object.entries(mappings);
        if (entries.length === 0) {
            return '<p class="text-muted">No mappings defined</p>';
        }

        return entries.map(([key, value], index) => `
            <div class="mapping-row">
                <input type="text" class="mapping-key" value="${key}" placeholder="${type === 'input' ? 'Activity Input' : 'Variable Name'}">
                <span class="mapping-arrow">→</span>
                <input type="text" class="mapping-value" value="${value}" placeholder="${type === 'input' ? 'Workflow Variable' : 'Activity Output'}">
                <button type="button" class="btn-remove" data-type="${type}" data-key="${key}">×</button>
            </div>
        `).join('');
    }

    addMapping(type) {
        const mappings = type === 'input' ? this.activity.inputMappings : this.activity.outputMappings;
        mappings[`newKey${Date.now()}`] = '';
        this.renderMappings();
    }

    apply() {
        super.applyCommonFields();

        // Service configuration
        this.activity.configuration = {
            serviceName: document.getElementById('serviceName')?.value || '',
            methodName: document.getElementById('methodName')?.value || '',
            parameters: {}
        };

        try {
            const paramsText = document.getElementById('serviceParameters')?.value;
            if (paramsText) {
                this.activity.configuration.parameters = JSON.parse(paramsText);
            }
        } catch (e) {
            alert('Invalid JSON in parameters: ' + e.message);
            return false;
        }

        // Input/Output mappings
        this.activity.inputMappings = this.extractMappings('inputMappingsContainer');
        this.activity.outputMappings = this.extractMappings('outputMappingsContainer');

        return true;
    }

    extractMappings(containerId) {
        const container = document.getElementById(containerId);
        if (!container) return {};

        const mappings = {};
        const rows = container.querySelectorAll('.mapping-row');
        rows.forEach(row => {
            const key = row.querySelector('.mapping-key')?.value.trim();
            const value = row.querySelector('.mapping-value')?.value.trim();
            if (key && value) {
                mappings[key] = value;
            }
        });
        return mappings;
    }
}

// Script Task Editor
class ScriptTaskEditor extends BaseEditor {
    render() {
        const config = this.activity.configuration || {};
        const scriptLanguage = config.scriptLanguage || 'csharp';
        const script = config.script || '';

        return `
            ${this.getCommonFieldsHTML()}

            <div class="form-section">
                <h4>Script Configuration</h4>
                <div class="form-group">
                    <label>Script Language:</label>
                    <select id="scriptLanguage" class="form-control">
                        <option value="csharp" ${scriptLanguage === 'csharp' ? 'selected' : ''}>C#</option>
                        <option value="javascript" ${scriptLanguage === 'javascript' ? 'selected' : ''}>JavaScript</option>
                        <option value="python" ${scriptLanguage === 'python' ? 'selected' : ''}>Python</option>
                    </select>
                </div>
                <div class="form-group">
                    <label>Script Code:</label>
                    <div id="scriptEditorContainer"></div>
                    <small class="form-hint">Available variables: ${Object.keys(this.workflowVariables).join(', ') || 'None defined'}</small>
                </div>
            </div>

            <div class="form-section">
                <h4>Output Variable</h4>
                <div class="form-group">
                    <label>Store result in variable:</label>
                    <input type="text" id="scriptOutputVar" value="${config.outputVariable || ''}" class="form-control"
                           placeholder="e.g., scriptResult">
                </div>
            </div>

            ${this.getTimeoutFieldsHTML()}

            <div class="form-actions">
                <button class="btn btn-primary" id="applyPropertiesBtn">Apply Changes</button>
                <button class="btn btn-secondary" id="cancelPropertiesBtn">Cancel</button>
            </div>
        `;
    }

    attachEventListeners() {
        const scriptLanguage = this.activity.configuration?.scriptLanguage || 'csharp';
        const script = this.activity.configuration?.script || this.getDefaultScript(scriptLanguage);

        // Initialize Monaco editor for script
        setTimeout(() => {
            require(['vs/editor/editor.main'], () => {
                const container = document.getElementById('scriptEditorContainer');
                if (container) {
                    this.scriptEditor = monaco.editor.create(container, {
                        value: script,
                        language: scriptLanguage === 'csharp' ? 'csharp' : scriptLanguage,
                        theme: 'vs',
                        minimap: { enabled: false },
                        lineNumbers: 'on',
                        scrollBeyondLastLine: false,
                        automaticLayout: true,
                        fontSize: 14,
                        folding: true
                    });

                    this.editors.push(this.scriptEditor);

                    // Update language when changed
                    document.getElementById('scriptLanguage')?.addEventListener('change', (e) => {
                        const newLang = e.target.value;
                        monaco.editor.setModelLanguage(this.scriptEditor.getModel(), newLang === 'csharp' ? 'csharp' : newLang);

                        // Update with default template if empty
                        if (!this.scriptEditor.getValue().trim()) {
                            this.scriptEditor.setValue(this.getDefaultScript(newLang));
                        }
                    });
                }
            });
        }, 100);
    }

    getDefaultScript(language) {
        const templates = {
            csharp: `// Access workflow variables via the 'variables' dictionary
// Return result or update variables
return variables["someVariable"];`,
            javascript: `// Access workflow variables via the 'variables' object
// Return result or update variables
return variables.someVariable;`,
            python: `# Access workflow variables via the 'variables' dictionary
# Return result or update variables
return variables["someVariable"]`
        };
        return templates[language] || templates.csharp;
    }

    apply() {
        super.applyCommonFields();

        const scriptLanguage = document.getElementById('scriptLanguage')?.value || 'csharp';
        const scriptCode = this.scriptEditor ? this.scriptEditor.getValue() : '';
        const outputVariable = document.getElementById('scriptOutputVar')?.value || '';

        this.activity.configuration = {
            scriptLanguage: scriptLanguage,
            script: scriptCode,
            outputVariable: outputVariable
        };

        return true;
    }
}

// Decision Editor
class DecisionEditor extends BaseEditor {
    render() {
        const config = this.activity.configuration || {};
        const conditions = config.conditions || [];

        return `
            ${this.getCommonFieldsHTML()}

            <div class="form-section">
                <h4>Decision Configuration</h4>
                <p class="form-hint">Define conditions that will be evaluated to determine the next activity.
                   Conditions are evaluated in order, and the first true condition determines the path.</p>

                <div id="conditionsContainer"></div>
                <button type="button" class="btn btn-sm" id="addConditionBtn">+ Add Condition</button>
            </div>

            <div class="form-section">
                <h4>Default Path</h4>
                <div class="form-group">
                    <label>
                        <input type="checkbox" id="hasDefaultPath" ${config.hasDefaultPath ? 'checked' : ''}>
                        Use default path if no conditions match
                    </label>
                </div>
            </div>

            ${this.getTimeoutFieldsHTML()}

            <div class="form-actions">
                <button class="btn btn-primary" id="applyPropertiesBtn">Apply Changes</button>
                <button class="btn btn-secondary" id="cancelPropertiesBtn">Cancel</button>
            </div>
        `;
    }

    attachEventListeners() {
        this.renderConditions();

        document.getElementById('addConditionBtn')?.addEventListener('click', () => {
            const config = this.activity.configuration || {};
            if (!config.conditions) {
                config.conditions = [];
            }
            config.conditions.push({
                name: `Condition ${config.conditions.length + 1}`,
                expression: '',
                outputPath: ''
            });
            this.renderConditions();
        });
    }

    renderConditions() {
        const container = document.getElementById('conditionsContainer');
        if (!container) return;

        const config = this.activity.configuration || {};
        const conditions = config.conditions || [];

        if (conditions.length === 0) {
            container.innerHTML = '<p class="text-muted">No conditions defined. Add conditions to create decision branches.</p>';
            return;
        }

        container.innerHTML = conditions.map((condition, index) => `
            <div class="condition-card" data-index="${index}">
                <div class="condition-header">
                    <input type="text" class="condition-name" value="${condition.name}" placeholder="Condition name">
                    <button type="button" class="btn-remove" data-index="${index}">×</button>
                </div>
                <div class="form-group">
                    <label>Expression:</label>
                    <div class="condition-expression" data-index="${index}"></div>
                </div>
                <div class="form-group">
                    <label>Output Path (transition name):</label>
                    <input type="text" class="condition-output" value="${condition.outputPath || ''}"
                           placeholder="Name of transition to follow">
                </div>
            </div>
        `).join('');

        // Initialize expression builders for each condition
        setTimeout(() => {
            conditions.forEach((condition, index) => {
                const expressionContainer = container.querySelector(`.condition-expression[data-index="${index}"]`);
                if (expressionContainer && !expressionContainer.classList.contains('initialized')) {
                    expressionContainer.classList.add('initialized');
                    const builder = new ExpressionBuilder(expressionContainer, {
                        value: condition.expression || '',
                        variables: this.workflowVariables,
                        height: '150px',
                        onChange: (value) => {
                            condition.expression = value;
                        }
                    });
                    this.editors.push(builder);
                }
            });
        }, 100);

        // Attach remove handlers
        container.querySelectorAll('.btn-remove').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const index = parseInt(e.target.dataset.index);
                conditions.splice(index, 1);
                this.renderConditions();
            });
        });

        // Attach name change handlers
        container.querySelectorAll('.condition-name').forEach((input, index) => {
            input.addEventListener('change', (e) => {
                conditions[index].name = e.target.value;
            });
        });

        // Attach output path handlers
        container.querySelectorAll('.condition-output').forEach((input, index) => {
            input.addEventListener('change', (e) => {
                conditions[index].outputPath = e.target.value;
            });
        });
    }

    apply() {
        super.applyCommonFields();

        const hasDefaultPath = document.getElementById('hasDefaultPath')?.checked || false;

        this.activity.configuration = {
            conditions: this.activity.configuration?.conditions || [],
            hasDefaultPath: hasDefaultPath
        };

        return true;
    }
}

// Human Task Editor
class HumanTaskEditor extends BaseEditor {
    render() {
        const config = this.activity.configuration || {};
        const assignees = config.assignees || [];
        const formFields = config.formFields || [];
        const dueInHours = config.dueInHours || '';

        return `
            ${this.getCommonFieldsHTML()}

            <div class="form-section">
                <h4>Assignment</h4>
                <div class="form-group">
                    <label>Assignees (comma-separated):</label>
                    <input type="text" id="taskAssignees" value="${assignees.join(', ')}" class="form-control"
                           placeholder="e.g., user@example.com, group:managers">
                    <small class="form-hint">Use 'user:email' or 'group:name' format</small>
                </div>
                <div class="form-group">
                    <label>Due In (hours):</label>
                    <input type="number" id="taskDueInHours" value="${dueInHours}" class="form-control"
                           placeholder="Hours until task is due">
                </div>
            </div>

            <div class="form-section">
                <h4>Form Configuration</h4>
                <p class="form-hint">Define fields that users must complete:</p>
                <div id="formFieldsContainer"></div>
                <button type="button" class="btn btn-sm" id="addFormFieldBtn">+ Add Field</button>
            </div>

            <div class="form-section">
                <h4>Instructions</h4>
                <div class="form-group">
                    <label>Task Instructions:</label>
                    <textarea id="taskInstructions" rows="4" class="form-control">${config.instructions || ''}</textarea>
                </div>
            </div>

            ${this.getTimeoutFieldsHTML()}

            <div class="form-actions">
                <button class="btn btn-primary" id="applyPropertiesBtn">Apply Changes</button>
                <button class="btn btn-secondary" id="cancelPropertiesBtn">Cancel</button>
            </div>
        `;
    }

    attachEventListeners() {
        this.renderFormFields();

        document.getElementById('addFormFieldBtn')?.addEventListener('click', () => {
            const config = this.activity.configuration || {};
            if (!config.formFields) {
                config.formFields = [];
            }
            config.formFields.push({
                name: '',
                label: '',
                type: 'text',
                required: false
            });
            this.renderFormFields();
        });
    }

    renderFormFields() {
        const container = document.getElementById('formFieldsContainer');
        if (!container) return;

        const config = this.activity.configuration || {};
        const formFields = config.formFields || [];

        if (formFields.length === 0) {
            container.innerHTML = '<p class="text-muted">No form fields defined</p>';
            return;
        }

        container.innerHTML = formFields.map((field, index) => `
            <div class="form-field-row">
                <input type="text" class="field-name" value="${field.name}" placeholder="Field name" data-index="${index}">
                <input type="text" class="field-label" value="${field.label}" placeholder="Label" data-index="${index}">
                <select class="field-type" data-index="${index}">
                    <option value="text" ${field.type === 'text' ? 'selected' : ''}>Text</option>
                    <option value="number" ${field.type === 'number' ? 'selected' : ''}>Number</option>
                    <option value="date" ${field.type === 'date' ? 'selected' : ''}>Date</option>
                    <option value="textarea" ${field.type === 'textarea' ? 'selected' : ''}>Text Area</option>
                    <option value="checkbox" ${field.type === 'checkbox' ? 'selected' : ''}>Checkbox</option>
                </select>
                <label>
                    <input type="checkbox" class="field-required" ${field.required ? 'checked' : ''} data-index="${index}">
                    Required
                </label>
                <button type="button" class="btn-remove" data-index="${index}">×</button>
            </div>
        `).join('');

        // Attach event listeners
        container.querySelectorAll('.field-name, .field-label, .field-type, .field-required').forEach(el => {
            el.addEventListener('change', (e) => {
                const index = parseInt(e.target.dataset.index);
                const field = formFields[index];
                if (e.target.classList.contains('field-name')) {
                    field.name = e.target.value;
                } else if (e.target.classList.contains('field-label')) {
                    field.label = e.target.value;
                } else if (e.target.classList.contains('field-type')) {
                    field.type = e.target.value;
                } else if (e.target.classList.contains('field-required')) {
                    field.required = e.target.checked;
                }
            });
        });

        container.querySelectorAll('.btn-remove').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const index = parseInt(e.target.dataset.index);
                formFields.splice(index, 1);
                this.renderFormFields();
            });
        });
    }

    apply() {
        super.applyCommonFields();

        const assigneesText = document.getElementById('taskAssignees')?.value || '';
        const assignees = assigneesText.split(',').map(a => a.trim()).filter(a => a);
        const dueInHours = parseInt(document.getElementById('taskDueInHours')?.value) || null;
        const instructions = document.getElementById('taskInstructions')?.value || '';

        this.activity.configuration = {
            assignees: assignees,
            dueInHours: dueInHours,
            formFields: this.activity.configuration?.formFields || [],
            instructions: instructions
        };

        return true;
    }
}

// Generic Editor (fallback)
class GenericEditor extends BaseEditor {
    render() {
        return `
            ${this.getCommonFieldsHTML()}

            <div class="form-section">
                <h4>Configuration</h4>
                <div class="form-group">
                    <label>Configuration (JSON):</label>
                    <textarea id="propConfig" rows="8" class="form-control">${JSON.stringify(this.activity.configuration, null, 2)}</textarea>
                </div>
            </div>

            ${this.getTimeoutFieldsHTML()}

            <div class="form-actions">
                <button class="btn btn-primary" id="applyPropertiesBtn">Apply Changes</button>
                <button class="btn btn-secondary" id="cancelPropertiesBtn">Cancel</button>
            </div>
        `;
    }

    apply() {
        super.applyCommonFields();

        try {
            const configText = document.getElementById('propConfig')?.value;
            if (configText) {
                this.activity.configuration = JSON.parse(configText);
            }
        } catch (e) {
            alert('Invalid JSON in configuration: ' + e.message);
            return false;
        }

        return true;
    }
}
