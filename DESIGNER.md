# Workflow Visual Designer

A modern web-based visual workflow designer built with ASP.NET Core and HTML5 Canvas.

## Features

### **Visual Workflow Design**
- 🎨 Drag-and-drop activity placement
- 🔗 Visual connection drawing between activities
- ✏️ Real-time property editing
- 📐 Grid-based canvas with snap-to-grid
- 🎯 Context menus for quick actions

### **Activity Types**
- ▶️ **Start** - Workflow entry point
- 👤 **Human Task** - Manual approval/review tasks
- ⚙️ **Service Task** - Automated service calls
- 📝 **Script Task** - Inline script execution
- 🔀 **Decision** - Conditional routing
- ⏹️ **End** - Workflow completion

### **Workflow Management**
- 💾 Save and load workflow definitions
- 🚀 Start workflow instances directly from designer
- 📋 List and manage existing workflows
- 🔄 Real-time workflow execution monitoring

## Getting Started

### **Prerequisites**
- .NET 9.0 SDK
- SQL Server (optional - can use in-memory storage)

### **Running the Designer**

```bash
cd Workflow.Designer
dotnet run
```

The designer will be available at: **http://localhost:5000** (or https://localhost:5001)

### **API Documentation**
Swagger UI is available at: **http://localhost:5000/swagger**

## Using the Designer

### **Creating a Workflow**

1. **Add Activities**
   - Drag activity types from the left toolbox onto the canvas
   - Position them as needed

2. **Connect Activities**
   - Right-click an activity → "Connect To..."
   - Click the target activity to create a transition

3. **Configure Properties**
   - Click on an activity to select it
   - Edit properties in the right panel:
     - Name and description
     - Configuration (JSON)
     - Input/Output mappings

4. **Save Workflow**
   - Click "Save" button
   - Enter workflow name and description

### **Loading a Workflow**

1. Click "Load" button
2. Select a workflow from the list
3. The workflow will appear on the canvas

### **Starting a Workflow Instance**

1. Save your workflow first
2. Click "Start Instance" button
3. Enter initial variables (JSON format)
4. Specify who initiated the workflow
5. Click "Start"

## Architecture

### **Frontend (wwwroot/)**

```
├── index.html              # Main UI layout
├── css/
│   └── designer.css        # Styling
└── js/
    ├── workflow-api.js     # REST API client
    ├── workflow-canvas.js  # Canvas drawing & interactions
    └── designer.js         # Main application logic
```

### **Backend (Controllers/)**

```
├── WorkflowDefinitionsController.cs   # CRUD for workflows
└── WorkflowInstancesController.cs     # Runtime execution
```

## Canvas Features

### **Mouse Interactions**
- **Left Click** - Select activity
- **Right Click** - Context menu
- **Drag** - Move activity
- **Drop** - Add new activity from toolbox

### **Context Menu**
- **Properties** - View/edit activity properties
- **Delete** - Remove activity (and its connections)
- **Connect To...** - Create transition to another activity

### **Visual Elements**
- **Grid** - 20px grid for alignment
- **Colors** - Activity type-specific colors:
  - Green: Start
  - Red: End
  - Blue: Human Task
  - Purple: Service Task
  - Orange: Script Task
  - Yellow: Decision
- **Arrows** - Directional flow indicators on transitions
- **Selection** - Bold border on selected activity

## API Endpoints

### **Workflow Definitions**

```
GET    /api/workflowdefinitions           # List all workflows
GET    /api/workflowdefinitions/{id}      # Get workflow by ID
POST   /api/workflowdefinitions           # Create workflow
PUT    /api/workflowdefinitions/{id}      # Update workflow
DELETE /api/workflowdefinitions/{id}      # Delete workflow
```

### **Workflow Instances**

```
GET  /api/workflowinstances                      # List active instances
GET  /api/workflowinstances/{id}                 # Get instance by ID
POST /api/workflowinstances/start                # Start new instance
POST /api/workflowinstances/{id}/suspend         # Suspend instance
POST /api/workflowinstances/{id}/resume          # Resume instance
POST /api/workflowinstances/{id}/cancel          # Cancel instance
POST /api/workflowinstances/activities/{id}/complete  # Complete activity
```

## Configuration

Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "WorkflowDatabase": "Server=(localdb)\\mssqllocaldb;Database=WorkflowEngine;..."
  },
  "UseInMemoryDatabase": true  // Set to false for SQL Server
}
```

## Example Workflow JSON

```json
{
  "name": "Purchase Order Approval",
  "description": "Automated PO approval workflow",
  "version": "1.0.0",
  "activities": [
    {
      "id": "a1",
      "name": "Start",
      "type": "Start",
      "position": { "x": 100, "y": 100 }
    },
    {
      "id": "a2",
      "name": "Manager Review",
      "type": "HumanTask",
      "position": { "x": 100, "y": 200 },
      "configuration": {
        "assignedToGroup": "Managers"
      },
      "inputMappings": {
        "poId": "purchaseOrderId"
      }
    }
  ],
  "transitions": [
    {
      "id": "t1",
      "name": "Start to Review",
      "fromActivityId": "a1",
      "toActivityId": "a2"
    }
  ]
}
```

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| Delete | Delete selected activity |
| Escape | Deselect activity |
| Ctrl+S | Save workflow |
| Ctrl+N | New workflow |

## Browser Compatibility

- Chrome/Edge (recommended)
- Firefox
- Safari

## Development

### **Adding New Activity Types**

1. Add to toolbox in `index.html`:
```html
<div class="activity-item" draggable="true" data-type="NewType">
    <span class="activity-icon">🎯</span>
    <span>New Type</span>
</div>
```

2. Add color mapping in `workflow-canvas.js`:
```javascript
const colors = {
    'NewType': '#color-hex',
    // ...
};
```

3. Implement handler in Core project:
```csharp
public class NewTypeHandler : IActivityHandler {
    public ActivityType SupportedType => ActivityType.NewType;
    // ...
}
```

### **Customizing Canvas**

Edit `workflow-canvas.js`:
- `drawActivity()` - Change activity appearance
- `drawTransition()` - Modify connection style
- `drawGrid()` - Adjust grid size/style

## Troubleshooting

### **Canvas Not Loading**
- Check browser console for JavaScript errors
- Ensure static files are enabled in `Program.cs`

### **API Errors**
- Verify CORS is configured for your domain
- Check Swagger UI for API status

### **Workflow Not Saving**
- Ensure at least one Start activity exists
- Check JSON in browser developer tools network tab

## Future Enhancements

- [ ] Undo/Redo functionality
- [ ] Zoom and pan controls
- [ ] Workflow templates library
- [ ] Real-time collaboration
- [ ] Workflow versioning UI
- [ ] Activity library/marketplace
- [ ] Export to image/PDF
- [ ] Workflow validation before save
- [ ] Execution history visualization
- [ ] Dark mode

## Contributing

To add new features:
1. Frontend changes go in `wwwroot/`
2. Backend controllers in `Controllers/`
3. Core engine logic in `Workflow.Core`

## License

Part of the Workflow Engine project.
