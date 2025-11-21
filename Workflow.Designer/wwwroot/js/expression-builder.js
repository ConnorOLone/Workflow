// Expression Builder Component
// Provides rich expression editing with variable picker, function library, and validation

class ExpressionBuilder {
    constructor(container, options = {}) {
        this.container = typeof container === 'string' ? document.querySelector(container) : container;
        this.options = {
            value: options.value || '',
            variables: options.variables || {},
            functions: options.functions || this.getDefaultFunctions(),
            language: options.language || 'csharp', // csharp, javascript, python
            onChange: options.onChange || (() => {}),
            onValidate: options.onValidate || (() => {}),
            readOnly: options.readOnly || false,
            height: options.height || '200px'
        };

        this.editor = null;
        this.render();
    }

    getDefaultFunctions() {
        return {
            'String Functions': [
                { name: 'ToUpper', signature: 'ToUpper(string)', description: 'Converts string to uppercase' },
                { name: 'ToLower', signature: 'ToLower(string)', description: 'Converts string to lowercase' },
                { name: 'Substring', signature: 'Substring(string, start, length)', description: 'Extracts substring' },
                { name: 'Contains', signature: 'Contains(string, value)', description: 'Check if string contains value' },
                { name: 'StartsWith', signature: 'StartsWith(string, value)', description: 'Check if string starts with value' }
            ],
            'Math Functions': [
                { name: 'Abs', signature: 'Abs(number)', description: 'Absolute value' },
                { name: 'Round', signature: 'Round(number, decimals)', description: 'Round to decimals' },
                { name: 'Max', signature: 'Max(a, b)', description: 'Returns maximum value' },
                { name: 'Min', signature: 'Min(a, b)', description: 'Returns minimum value' },
                { name: 'Sqrt', signature: 'Sqrt(number)', description: 'Square root' }
            ],
            'Date Functions': [
                { name: 'Now', signature: 'Now()', description: 'Current date/time' },
                { name: 'Today', signature: 'Today()', description: 'Current date' },
                { name: 'AddDays', signature: 'AddDays(date, days)', description: 'Add days to date' },
                { name: 'DateDiff', signature: 'DateDiff(date1, date2)', description: 'Difference in days' }
            ],
            'Logical Functions': [
                { name: 'If', signature: 'If(condition, trueValue, falseValue)', description: 'Conditional expression' },
                { name: 'And', signature: 'And(condition1, condition2)', description: 'Logical AND' },
                { name: 'Or', signature: 'Or(condition1, condition2)', description: 'Logical OR' },
                { name: 'Not', signature: 'Not(condition)', description: 'Logical NOT' }
            ]
        };
    }

    render() {
        this.container.innerHTML = `
            <div class="expression-builder">
                <div class="expression-toolbar">
                    <div class="toolbar-section">
                        <label class="toolbar-label">Variables:</label>
                        <select class="variable-picker" title="Insert variable">
                            <option value="">Select variable...</option>
                        </select>
                        <button class="btn-toolbar" id="insertVarBtn" title="Insert selected variable">Insert</button>
                    </div>
                    <div class="toolbar-section">
                        <label class="toolbar-label">Functions:</label>
                        <select class="function-category" title="Function category">
                            <option value="">Select category...</option>
                        </select>
                        <select class="function-picker" title="Select function">
                            <option value="">Select function...</option>
                        </select>
                        <button class="btn-toolbar" id="insertFuncBtn" title="Insert selected function">Insert</button>
                    </div>
                    <div class="toolbar-section ml-auto">
                        <button class="btn-toolbar" id="validateBtn" title="Validate expression">✓ Validate</button>
                        <button class="btn-toolbar" id="formatBtn" title="Format expression">Format</button>
                    </div>
                </div>
                <div class="expression-editor" id="expressionEditor" style="height: ${this.options.height}"></div>
                <div class="expression-status">
                    <span class="status-icon" id="statusIcon">●</span>
                    <span class="status-text" id="statusText">Ready</span>
                    <span class="status-hint" id="statusHint"></span>
                </div>
            </div>
        `;

        this.populateVariables();
        this.populateFunctions();
        this.initializeEditor();
        this.attachEventListeners();
    }

    populateVariables() {
        const variablePicker = this.container.querySelector('.variable-picker');
        Object.entries(this.options.variables).forEach(([key, value]) => {
            const option = document.createElement('option');
            option.value = key;
            const type = typeof value;
            option.textContent = `${key} (${type})`;
            variablePicker.appendChild(option);
        });
    }

    populateFunctions() {
        const categoryPicker = this.container.querySelector('.function-category');
        const functionPicker = this.container.querySelector('.function-picker');

        Object.keys(this.options.functions).forEach(category => {
            const option = document.createElement('option');
            option.value = category;
            option.textContent = category;
            categoryPicker.appendChild(option);
        });

        categoryPicker.addEventListener('change', (e) => {
            functionPicker.innerHTML = '<option value="">Select function...</option>';
            if (e.target.value) {
                const functions = this.options.functions[e.target.value];
                functions.forEach(func => {
                    const option = document.createElement('option');
                    option.value = func.signature;
                    option.textContent = `${func.name} - ${func.description}`;
                    option.dataset.signature = func.signature;
                    functionPicker.appendChild(option);
                });
            }
        });
    }

    initializeEditor() {
        require(['vs/editor/editor.main'], () => {
            const editorContainer = this.container.querySelector('#expressionEditor');

            this.editor = monaco.editor.create(editorContainer, {
                value: this.options.value,
                language: this.options.language === 'csharp' ? 'csharp' : this.options.language,
                theme: 'vs',
                minimap: { enabled: false },
                lineNumbers: 'on',
                scrollBeyondLastLine: false,
                automaticLayout: true,
                readOnly: this.options.readOnly,
                fontSize: 14,
                folding: true,
                wordWrap: 'on',
                suggestOnTriggerCharacters: true,
                quickSuggestions: true
            });

            // Register custom completion provider for variables
            monaco.languages.registerCompletionItemProvider(this.options.language, {
                provideCompletionItems: (model, position) => {
                    const suggestions = [];

                    // Add variables
                    Object.keys(this.options.variables).forEach(varName => {
                        suggestions.push({
                            label: varName,
                            kind: monaco.languages.CompletionItemKind.Variable,
                            insertText: varName,
                            documentation: `Variable: ${varName}`
                        });
                    });

                    // Add functions
                    Object.values(this.options.functions).flat().forEach(func => {
                        suggestions.push({
                            label: func.name,
                            kind: monaco.languages.CompletionItemKind.Function,
                            insertText: func.signature.replace(/\(.*\)/, '($0)'),
                            insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                            documentation: func.description,
                            detail: func.signature
                        });
                    });

                    return { suggestions };
                }
            });

            // Listen for changes
            this.editor.onDidChangeModelContent(() => {
                const value = this.editor.getValue();
                this.options.onChange(value);
                this.updateStatus('modified', 'Modified', 'Press Ctrl+S to validate');
            });

            this.updateStatus('ready', 'Ready', 'Start typing or insert variables/functions');
        });
    }

    attachEventListeners() {
        // Insert variable
        this.container.querySelector('#insertVarBtn').addEventListener('click', () => {
            const variablePicker = this.container.querySelector('.variable-picker');
            if (variablePicker.value) {
                this.insertText(variablePicker.value);
            }
        });

        // Insert function
        this.container.querySelector('#insertFuncBtn').addEventListener('click', () => {
            const functionPicker = this.container.querySelector('.function-picker');
            if (functionPicker.value) {
                this.insertText(functionPicker.value);
            }
        });

        // Validate
        this.container.querySelector('#validateBtn').addEventListener('click', () => {
            this.validate();
        });

        // Format
        this.container.querySelector('#formatBtn').addEventListener('click', () => {
            if (this.editor) {
                this.editor.getAction('editor.action.formatDocument').run();
            }
        });
    }

    insertText(text) {
        if (this.editor) {
            const selection = this.editor.getSelection();
            const id = { major: 1, minor: 1 };
            const op = {
                identifier: id,
                range: selection,
                text: text,
                forceMoveMarkers: true
            };
            this.editor.executeEdits('insert-text', [op]);
            this.editor.focus();
        }
    }

    validate() {
        const value = this.getValue();

        // Basic validation (in production, call backend API for real validation)
        if (!value.trim()) {
            this.updateStatus('error', 'Error', 'Expression cannot be empty');
            this.options.onValidate(false, 'Expression cannot be empty');
            return false;
        }

        // Check for balanced parentheses
        const openCount = (value.match(/\(/g) || []).length;
        const closeCount = (value.match(/\)/g) || []).length;
        if (openCount !== closeCount) {
            this.updateStatus('error', 'Error', 'Unbalanced parentheses');
            this.options.onValidate(false, 'Unbalanced parentheses');
            return false;
        }

        // Check for undefined variables (basic check)
        const varNames = Object.keys(this.options.variables);
        const words = value.match(/\b[a-zA-Z_][a-zA-Z0-9_]*\b/g) || [];
        const unknownVars = words.filter(word => {
            // Check if it's not a variable, function, or keyword
            const isVariable = varNames.includes(word);
            const isFunction = Object.values(this.options.functions).flat().some(f => f.name === word);
            const isKeyword = ['if', 'else', 'return', 'var', 'let', 'const', 'true', 'false', 'null'].includes(word.toLowerCase());
            return !isVariable && !isFunction && !isKeyword;
        });

        if (unknownVars.length > 0) {
            const uniqueVars = [...new Set(unknownVars)];
            this.updateStatus('warning', 'Warning', `Unknown identifiers: ${uniqueVars.join(', ')}`);
            this.options.onValidate(true, `Warning: Unknown identifiers: ${uniqueVars.join(', ')}`);
            return true;
        }

        this.updateStatus('success', 'Valid', 'Expression is valid');
        this.options.onValidate(true, 'Expression is valid');
        return true;
    }

    updateStatus(type, text, hint) {
        const icon = this.container.querySelector('#statusIcon');
        const statusText = this.container.querySelector('#statusText');
        const statusHint = this.container.querySelector('#statusHint');

        icon.className = `status-icon status-${type}`;
        statusText.textContent = text;
        statusHint.textContent = hint || '';
    }

    getValue() {
        return this.editor ? this.editor.getValue() : '';
    }

    setValue(value) {
        if (this.editor) {
            this.editor.setValue(value);
        }
    }

    updateVariables(variables) {
        this.options.variables = variables;
        // Repopulate variable picker
        const variablePicker = this.container.querySelector('.variable-picker');
        variablePicker.innerHTML = '<option value="">Select variable...</option>';
        this.populateVariables();
    }

    destroy() {
        if (this.editor) {
            this.editor.dispose();
        }
    }
}
